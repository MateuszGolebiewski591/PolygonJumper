using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private PolygonCollider2D collider;

    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Movement Parameters")]
    private Vector2 horizontalVector = Vector2.zero;
    private Vector2 verticalVector = Vector2.zero;
    private Vector2 groundSurface =  Vector2.zero;
    private Vector2 inputVector = Vector2.zero;
    [SerializeField] float groundMovementSpeed = 7.5f;
    private float movementSpeed = 0f;

    [Header("edge detection")]
    private Vector2 gravity = Vector2.down;
    [SerializeField] float edgeThickness = 0.1f;
    private PolygonCollider2D currentSurface; 
    private Vector2 contactPoint;


    [SerializeField] float gravityForce = 9.81f; 

    [Header("Jump Parameters")]
    private bool jumpAvalailable = true;
    private float jumpTimeElapsed = 0f;
    private bool jumpButtonDown = false;
    [SerializeField] float maxJumpTime = 2f;
    [SerializeField] float coyoteTime = 0.2f;

    [Header("Other")]
    [SerializeField] private AnimationCurve jumpCurve;
    [SerializeField] private float groundCheckDistance = 0.1f;
    private PlayerStateNum state = PlayerStateNum.Falling;
    private PlayerState playerState;

    private enum PlayerStateNum
    {
        Grounded,
        Jumping,
        Falling,
        Sticking,
    }

    abstract private class PlayerState
    {
        public PlayerStateNum state;
        public PlayerState(){}
        abstract public void CheckConditions();
        abstract public void UpdatePlayer();
    }

    private class FallingState : PlayerState
    {
        public PlayerStateNum state = PlayerStateNum.Falling;
        PlayerMovement player;
        private float minimumFallTimer = 0.2f;
        private float elapsedFallTime = 0;
        public FallingState(PlayerMovement movement)
        {
            player = movement;
            player.jumpAvalailable = false;
            player.state = state;
        }
        override public void CheckConditions()
        {
            elapsedFallTime += Time.deltaTime;
            if (player.IsOnSurface() && elapsedFallTime >= minimumFallTimer) player.playerState = new StickingState(player);
        }
        override public void UpdatePlayer()
        {
            player.verticalVector = Vector2.down * player.gravityForce;
            player.horizontalVector = player.inputVector * player.movementSpeed;
        }
    }

    private class GroundedState : PlayerState
    {
        public PlayerStateNum state = PlayerStateNum.Grounded;
        PlayerMovement player;
        private bool waitingOnJumpUp;
        private int localFaceIndex;
        private bool correctionState = false;
        private Vector2 targetNormal = Vector2.zero;
        private int targetIndex = 0;
        private Vector2 stickingPoint = Vector2.zero;
        private bool postCorrectionState = false;
        public GroundedState(PlayerMovement movement)
        {
            player = movement;
            if (player.jumpButtonDown) {
                player.jumpAvalailable = false;
                waitingOnJumpUp = true;
            }
            else {
                player.jumpAvalailable = true;
                waitingOnJumpUp = false;
            }
            player.IsOnSurface();
        }

        public void DefineLocalFaceIndex(int face) {localFaceIndex = face;}
        override public void CheckConditions()
        {
            if (correctionState)
            {
                Vector2[] points = player.collider.points;
                Vector2 a = player.collider.transform.TransformPoint(points[localFaceIndex]);
                Vector2 b = player.collider.transform.TransformPoint(points[(localFaceIndex + 1) % points.Length]);
                Vector2 edge = b - a;
                Vector2 currentNormal = new Vector2(-edge.y, edge.x).normalized;
                Vector2 midpoint = (a + b) / 2f;
                if (Vector2.Dot(currentNormal, midpoint - (Vector2)player.transform.position) < 0f) currentNormal = -currentNormal;
                if (Vector2.Dot(-currentNormal, targetNormal) > 0.99995f) {
                    correctionState = false;
                    postCorrectionState = true;
                }
                else return;
            }
            if (waitingOnJumpUp)
            {
                if (!player.jumpButtonDown) {
                    player.jumpAvalailable = true;
                    waitingOnJumpUp = false;
                }
            }
            player.IsOnSurface();
            bool supported = HasSupport();
            if (supported && postCorrectionState) postCorrectionState = false;
            if (!supported && !postCorrectionState)
            {
                Vector2[] surfacePoints = player.currentSurface.points;
                int closestCorner = -1;
                float closestDistance = float.MaxValue;

                for (int i = 0; i < surfacePoints.Length; i++)
                {
                    Vector2 worldPoint = player.currentSurface.transform.TransformPoint(surfacePoints[i]);
                    float distance = Vector2.Distance(player.transform.position, worldPoint);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestCorner = i;
                    }
                }
                int prevFace = (closestCorner - 1 + surfacePoints.Length) % surfacePoints.Length;
                int nextFace = closestCorner;
                Vector2 prevNormal = GetSurfaceNormal(prevFace);
                Vector2 nextNormal = GetSurfaceNormal(nextFace);
                float prevDot = Vector2.Dot(prevNormal, -player.gravity);
                float nextDot = Vector2.Dot(nextNormal, -player.gravity);
                if (prevDot > nextDot)
                {
                    targetNormal = nextNormal;
                    targetIndex = nextFace;
                }
                else
                {
                    targetNormal = prevNormal;
                    targetIndex = prevFace;
                }
                stickingPoint = player.currentSurface.transform.TransformPoint(surfacePoints[closestCorner]);
                correctionState = true;
                return;
            }
            if (player.jumpButtonDown && !waitingOnJumpUp) player.playerState = new JumpingState(player);
        }
        override public void UpdatePlayer()
        {
            if (correctionState)
            {
                player.verticalVector = Vector2.zero;
                player.horizontalVector = Vector2.zero;
                Vector2[] points = player.collider.points;
                Vector2 a = player.collider.transform.TransformPoint(points[localFaceIndex]);
                Vector2 b = player.collider.transform.TransformPoint(points[(localFaceIndex + 1) % points.Length]);
                Vector2 edge = b - a;
                Vector2 currentNormal = new Vector2(-edge.y, edge.x).normalized;
                Vector2 midpoint = (a + b) / 2f;
                if (Vector2.Dot(currentNormal, midpoint - (Vector2)player.transform.position) < 0f) currentNormal = -currentNormal;
                Vector2 desiredNormal = -targetNormal;
                float angleDelta = Vector2.SignedAngle(currentNormal, desiredNormal);
                float rotateSpeed = 720f;
                float step = rotateSpeed * Time.deltaTime;
                float rotationThisFrame = Mathf.Clamp(angleDelta, -step, step);
                Vector2 offset = (Vector2)player.transform.position - stickingPoint;
                Vector2 rotatedOffset = Quaternion.Euler(0f, 0f, rotationThisFrame) * offset;
                Vector2 newPosition = stickingPoint + rotatedOffset;
                player.transform.Rotate(0f, 0f, rotationThisFrame);
                player.transform.position = newPosition;
            }
            else
            {
                player.verticalVector = player.gravity;
                player.horizontalVector = player.movementSpeed * Vector2.Dot(player.groundSurface, player.inputVector) * player.groundSurface;
            }
            
        }

        private bool HasSupport()
        {
            Vector2[] points = player.collider.points;
            Vector2 vA = player.collider.transform.TransformPoint(points[localFaceIndex]);
            Vector2 vB = player.collider.transform.TransformPoint(points[(localFaceIndex + 1) % points.Length]);
            Vector2 midpoint = (vA + vB) * 0.5f;
            Vector2 edge = vB - vA;
            Vector2 normal = player.gravity.normalized;
            RaycastHit2D hit = Physics2D.Raycast(midpoint, normal, 0.2f, player.groundLayer);
            return hit.collider != null;
        }

        private Vector2 GetSurfaceNormal(int index)
        {
            Vector2[] surfacePoints = player.currentSurface.points;
            Vector2 a = player.currentSurface.transform.TransformPoint(surfacePoints[index]);
            Vector2 b = player.currentSurface.transform.TransformPoint(surfacePoints[(index + 1) % surfacePoints.Length]);
            Vector2 edge = b - a;
            Vector2 currentNormal = new Vector2(-edge.y, edge.x).normalized;
            Vector2 midpoint = (a + b) / 2f;
            if (Vector2.Dot(currentNormal, midpoint - (Vector2)player.currentSurface.bounds.center) < 0f) currentNormal = -currentNormal;
            return currentNormal;
        }
    } 

    private class JumpingState : PlayerState
    {
        public PlayerStateNum state = PlayerStateNum.Jumping;
        PlayerMovement player;
        bool airborne = false;
        private float elapsedJumpTime = 0;
        private float maxJumpTime;
        public JumpingState(PlayerMovement movement)
        {
            player = movement;
            player.jumpAvalailable = false;
            maxJumpTime = player.maxJumpTime;
        }
        override public void CheckConditions()
        {
            elapsedJumpTime += Time.deltaTime;
            bool grounded = player.IsOnSurface();
            if (grounded && airborne) {
                player.playerState = new StickingState(player);
                return;
            }
            else if (!grounded && player.jumpButtonDown) airborne = true;
            else if (!grounded && !player.jumpButtonDown) {
                player.playerState = new FallingState(player);
                return;
            }
            if (elapsedJumpTime >= maxJumpTime) player.playerState = new FallingState(player);
        }
        override public void UpdatePlayer()
        {
            player.verticalVector = -player.gravity * player.gravityForce * player.jumpCurve.Evaluate(elapsedJumpTime/maxJumpTime); 
            player.horizontalVector = player.movementSpeed * Vector2.Dot(player.groundSurface, player.inputVector) * player.groundSurface;
        }
    } 

    private class StickingState : PlayerState
    {
        public PlayerStateNum state = PlayerStateNum.Sticking;
        private PlayerMovement player;
        private PolygonCollider2D targetHitbox;
        private Vector2 targetNormal;
        private int targetIndex;
        private int localFaceIndex;
        private Vector2 stickingPoint;
        private Vector2 lastMovementVector;
        public StickingState(PlayerMovement movement)
        {
            player = movement;
            player.jumpAvalailable = false;
            targetHitbox = player.currentSurface;
            stickingPoint = player.contactPoint;
            lastMovementVector = (player.verticalVector + player.horizontalVector).normalized;

            ResolveStickingPrerequisites(player.currentSurface, player.contactPoint);
            player.state = state;
        }

        public StickingState(PlayerMovement movement, Vector2 targetNorm, int targetIdx, int localFaceIdx, Vector2 stickPoint)
        {
            player = movement;
            player.jumpAvalailable = false;
            targetHitbox = player.currentSurface;

            targetNormal = targetNorm;
            targetIndex = targetIdx;
            localFaceIndex = localFaceIdx;
            stickingPoint = stickPoint;

            lastMovementVector = (player.verticalVector + player.horizontalVector).normalized;
        }
        override public void CheckConditions()
        {
            Vector2[] points = player.collider.points;
            Vector2 a = player.collider.transform.TransformPoint(points[localFaceIndex]);
            Vector2 b = player.collider.transform.TransformPoint(points[(localFaceIndex + 1) % points.Length]);
            Vector2 edge = b - a;
            Vector2 currentNormal = new Vector2(-edge.y, edge.x).normalized;
            Vector2 midpoint = (a + b) / 2f;
            if (Vector2.Dot(currentNormal, midpoint - (Vector2)player.transform.position) < 0f) currentNormal = -currentNormal;
            if (Vector2.Dot(-currentNormal, targetNormal) > 0.99995f) {
                GroundedState state = new GroundedState(player);
                state.DefineLocalFaceIndex(localFaceIndex);
                player.playerState = state;
            }
        }
        override public void UpdatePlayer() 
        {
            player.verticalVector = Vector2.zero;
            player.horizontalVector = Vector2.zero;
            Vector2[] points = player.collider.points;
            Vector2 a = player.collider.transform.TransformPoint(points[localFaceIndex]);
            Vector2 b = player.collider.transform.TransformPoint(points[(localFaceIndex + 1) % points.Length]);
            Vector2 edge = b - a;
            Vector2 currentNormal = new Vector2(-edge.y, edge.x).normalized;
            Vector2 midpoint = (a + b) / 2f;
            if (Vector2.Dot(currentNormal, midpoint - (Vector2)player.transform.position) < 0f) currentNormal = -currentNormal;
            Vector2 desiredNormal = -targetNormal;
            float angleDelta = Vector2.SignedAngle(currentNormal, desiredNormal);
            float rotateSpeed = 720f;
            float step = rotateSpeed * Time.deltaTime;
            float rotationThisFrame = Mathf.Clamp(angleDelta, -step, step);
            Vector2 offset = (Vector2)player.transform.position - stickingPoint;
            Vector2 rotatedOffset = Quaternion.Euler(0f, 0f, rotationThisFrame) * offset;
            Vector2 newPosition = stickingPoint + rotatedOffset;
            player.transform.Rotate(0f, 0f, rotationThisFrame);
            player.transform.position = newPosition;
        }

        private void ResolveStickingPrerequisites(PolygonCollider2D surfaceHitbox, Vector2 contactPoint)
        {
            int closestCornerIndex = -1; //which corner we've hit
            bool surfaceCorner = ResolveTargetFace(surfaceHitbox, contactPoint, out closestCornerIndex); //Have we hit object corner/which face to stick to
            int cornerIndex;//with which corner we hit
            bool triangleCorner = ResolveTriangleContactType(contactPoint, out cornerIndex); //did triangle hit with corner or with face
            int triangleFaceIndex;
            if (!triangleCorner) { //If hit with face, identify index of face
                localFaceIndex = ResolveTriangleContact(contactPoint);
            }
            else
            {
                if (surfaceCorner) //corner - corner
                {
                    //on object hitbox, get the corner and work out vector to adjacent adjacent, going out from the corner we hit to get target edge
                    Vector2[] surfacePoints = surfaceHitbox.points; 
                    int nextCornerIndex;
                    if (targetIndex == closestCornerIndex) nextCornerIndex = (closestCornerIndex + 1) % surfacePoints.Length; //target index can only be the index of the corner or the point before
                    else nextCornerIndex = (closestCornerIndex - 1 + surfacePoints.Length) % surfacePoints.Length; 
                    Vector2 nextPoint = surfaceHitbox.transform.TransformPoint(surfacePoints[nextCornerIndex]);
                    Vector2 currentPoint = surfaceHitbox.transform.TransformPoint(surfacePoints[closestCornerIndex]);
                    Vector2 targetVector = (nextPoint - currentPoint).normalized; //Compute vector pointing away from the corner

                    //get adjacent corners on triangle, compute outward vectors, dot product with target vector, take highest raw value
                    Vector2[] trianglePoints = player.collider.points; 
                    Vector2 tprevPoint = player.collider.transform.TransformPoint(trianglePoints[(cornerIndex - 1 + trianglePoints.Length) % trianglePoints.Length]);
                    Vector2 tcurrentPoint = player.collider.transform.TransformPoint(trianglePoints[cornerIndex]);
                    Vector2 tnextPoint = player.collider.transform.TransformPoint(trianglePoints[(cornerIndex + 1) % trianglePoints.Length]);
                    Vector2 tprev = tprevPoint - tcurrentPoint; //outward vectors for triangle stemming from corner which we hit with
                    Vector2 tnext = tnextPoint - tcurrentPoint;

                    float prevDot = Vector2.Dot(tprev, targetVector); //dot product to see which face is more aligned with the object face
                    float nextDot = Vector2.Dot(tnext, targetVector);
                    if (prevDot >= nextDot) {
                        localFaceIndex = (cornerIndex - 1 + trianglePoints.Length) % trianglePoints.Length;
                    }
                    else {
                        localFaceIndex = cornerIndex;
                    }

                }
                else // triangle corner - object face
                {
                    // get adjacent corners, compute outward vectors, dot product, take best magnitude 
                    Vector2[] points = player.collider.points; //retrieve outward vectors from triangle corner
                    Vector2 prevPoint = player.collider.transform.TransformPoint(points[(cornerIndex - 1 + points.Length) % points.Length]);
                    Vector2 currentPoint = player.collider.transform.TransformPoint(points[cornerIndex]);
                    Vector2 nextPoint = player.collider.transform.TransformPoint(points[(cornerIndex + 1) % points.Length]);
                    Vector2 prev = (prevPoint - currentPoint).normalized;//ensure normalized despite not being needed 
                    Vector2 next = (nextPoint - currentPoint).normalized;
                    Vector2 targetEdge = new Vector2(-targetNormal.y, targetNormal.x); //target edge derived from normal, direction is irrelevant
                    targetEdge.Normalize();
                    float prevDot = Mathf.Abs(Vector2.Dot(targetEdge, prev)); //take magnitude of dot product
                    float nextDot = Mathf.Abs(Vector2.Dot(targetEdge, next));

                    Vector2 movementAlongSurface = Vector2.Dot(lastMovementVector, targetEdge) * targetEdge;


                    if (movementAlongSurface.sqrMagnitude > 0.001f)
                    {
                        movementAlongSurface.Normalize();

                        float prevMoveDot = Vector2.Dot(prev, movementAlongSurface);
                        float nextMoveDot = Vector2.Dot(next, movementAlongSurface);
                        float movementBias = 0.25f;

                        prevDot += prevMoveDot * movementBias;
                        nextDot += nextMoveDot * movementBias;
                    }

                    if (prevDot >= nextDot) {
                        localFaceIndex = (cornerIndex - 1 + points.Length) % points.Length;
                    }
                    else {
                        localFaceIndex = cornerIndex;
                    }
                }
            }
            return;
        }

        private bool ResolveTargetFace(PolygonCollider2D surfaceHitbox, Vector2 contactPoint, out int closestCornerIndex) //Finds the face to snap to
        {
            closestCornerIndex = -1;
            List<Vector2> surfaceNormals = GetPolygonNormals(surfaceHitbox); //Gets all normals for object
            Vector2 receivedNormal = -player.gravity; //If we hit face, it should just be the normal
            if (FoundNormalMatch(receivedNormal, surfaceNormals)) return false; //Checks if we've hit face or corner (Have we hit corner?)
            closestCornerIndex = ResolveCornerFace(surfaceHitbox, contactPoint, surfaceNormals, receivedNormal); // Finds which face of the corner we should snap to
            return true; //returns that we've hit the corner and which corner it is
        }

        private List<Vector2> GetPolygonNormals(PolygonCollider2D surfaceHitbox) //Finds all normals for the target polygon
        {
            List<Vector2> normals = new List<Vector2>();
            Vector2[] points = surfaceHitbox.points;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 a = surfaceHitbox.transform.TransformPoint(points[i]); //get points and transform
                Vector2 b = surfaceHitbox.transform.TransformPoint(points[(i + 1) % points.Length]);
                Vector2 edge = b - a;
                Vector2 normal = new Vector2(-edge.y, edge.x).normalized; //compute normal
                Vector2 midpoint = (a + b) / 2f; // midpoint of vector
                Vector2 toMid = midpoint - (Vector2)surfaceHitbox.bounds.center; //Vector from object center to the midpoint (should be parallel to normal)
                if (Vector2.Dot(normal, toMid) < 0f)normal = -normal; //ensures outward facing normal
                normals.Add(normal);
            }
            return normals;
        }

        private bool FoundNormalMatch(Vector2 receivedNormal, List<Vector2> surfaceNormals) //Checks if we have a match
        {
            targetNormal = Vector2.zero;
            targetIndex = -1;
            float bestMatch = -1f;
            for (int i = 0; i < surfaceNormals.Count; i++)
            {
                float dot = Vector2.Dot(surfaceNormals[i], receivedNormal);
                if (dot > bestMatch)
                {
                    bestMatch = dot;
                    targetNormal = surfaceNormals[i]; //Sets target information we need for future updates
                    targetIndex = i; //current index and +1 gets you the face from the object polygon
                }
            }
            return bestMatch > 0.9995f; // Whether we actually found a match or just the most similar 
        } 

        private int ResolveCornerFace(PolygonCollider2D surfaceHitbox, Vector2 contactPoint, List<Vector2> surfaceNormals, Vector2 receivedNormal) //Resolves landing on corner
        {
            int closestPointIndex = ResolveCornerIndex(surfaceHitbox, contactPoint); //Finds index of corner
            Vector2[] cornerNormals = FindCornerNormals(surfaceHitbox, closestPointIndex); //Find normals of two sides that make up the corner
            Vector2[] trianglePoints = player.collider.points;
            Vector2 triangleCenter = ((trianglePoints[0] + trianglePoints[1] + trianglePoints[2])/3).normalized; //Center of triangle
            Vector2 centerOffset = triangleCenter - surfaceHitbox.points[closestPointIndex]; //Vector from corner to triangle center
            float prevSimilarity = Vector2.Dot(centerOffset, cornerNormals[0]); //Compare against both normals
            float nextSimilarity = Vector2.Dot(centerOffset, cornerNormals[1]);
            if (Mathf.Abs(prevSimilarity - nextSimilarity) <= 0.05f) //Uncertain region
            {
                prevSimilarity = Vector2.Dot(receivedNormal, cornerNormals[0]); //compare contact normal instead
                nextSimilarity = Vector2.Dot(receivedNormal, cornerNormals[1]);
                if (prevSimilarity >= nextSimilarity) FoundNormalMatch(cornerNormals[0], surfaceNormals); //FNM sets the values above.
                else FoundNormalMatch(cornerNormals[1], surfaceNormals);
            }
            else if (prevSimilarity > nextSimilarity) FoundNormalMatch(cornerNormals[0], surfaceNormals);
            else if (nextSimilarity < prevSimilarity) FoundNormalMatch(cornerNormals[1], surfaceNormals);
            return closestPointIndex;
        }

        private int ResolveCornerIndex(PolygonCollider2D surfaceHitbox, Vector2 contactPoint) //Finds index of the point related to the corner we hit
        {
            Vector2[] points = surfaceHitbox.points;
            int bestIndex = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 worldPoint = surfaceHitbox.transform.TransformPoint(points[i]); //transforms points
                float distance = Vector2.Distance(contactPoint, worldPoint);
                if (distance < bestDistance) //check if it's the closest distance wise from the point ofcontact
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }
            return bestIndex; //returns the index of the point corresponding to the corner that we hit 
        }

        private Vector2[] FindCornerNormals(PolygonCollider2D surfaceHitbox, int cornerIndex)
        {
            Vector2[] points = surfaceHitbox.points; //Finds previous and next points on polygon
            Vector2 prevPoint = surfaceHitbox.transform.TransformPoint(points[(cornerIndex - 1 + points.Length) % points.Length]);
            Vector2 currentPoint = surfaceHitbox.transform.TransformPoint(points[cornerIndex]);
            Vector2 nextPoint = surfaceHitbox.transform.TransformPoint(points[(cornerIndex + 1) % points.Length]);

            Vector2 prev = currentPoint - prevPoint; 
            Vector2 normalPrev = new Vector2(-prev.y, prev.x).normalized;
            Vector2 midpointPrev = (prevPoint + currentPoint) / 2f; // midpoint of vector
            Vector2 toMidPrev = midpointPrev - (Vector2)surfaceHitbox.bounds.center; //Vector from object center to the midpoint (should be parallel to normal)
            if (Vector2.Dot(normalPrev, toMidPrev) < 0f)normalPrev = -normalPrev; //ensures outward facing normal

            Vector2 next = nextPoint - currentPoint;
            Vector2 normalNext = new Vector2(-next.y, next.x).normalized;
            Vector2 midpointNext = (nextPoint + currentPoint) / 2f; // midpoint of vector
            Vector2 toMidNext = midpointNext - (Vector2)surfaceHitbox.bounds.center; //Vector from object center to the midpoint (should be parallel to normal)
            if (Vector2.Dot(normalNext, toMidNext) < 0f)normalNext = -normalNext; //ensures outward facing normal 

            return new Vector2[] {normalPrev, normalNext}; // Returns the normals of the sides that make up the corner
        }

        private bool ResolveTriangleContactType(Vector2 contactPoint, out int cornerIndex) //have we hit a corner or not, if so which corner (index)
        {
            cornerIndex = -1;
            Vector2[] points = player.collider.points;
            float cornerTolerance = 0.2f;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 worldPoint = player.collider.transform.TransformPoint(points[i]);
                float distance = Vector2.Distance(contactPoint, worldPoint); //Finds which corner is closest to the reported contact point
                if (distance <= cornerTolerance) //if close enough, we know it's that one, we save the index and return true
                {
                    cornerIndex = i;
                    return true;
                }
            }
            return false;
        }

        private int ResolveTriangleContact(Vector2 contactPoint) 
        {
            int faceIndex = -1;
            Vector2[] points = player.collider.points;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 a = player.collider.transform.TransformPoint(points[i]); //retrieve edge
                Vector2 b = player.collider.transform.TransformPoint(points[(i + 1) % points.Length]);
                Vector2 closest = ClosestPointOnSegment(contactPoint, a, b); //get closest point
                float distance = Vector2.Distance(contactPoint, closest); //check how far contact point is from closest point (hardly any difference if on line)
                if (distance < bestDistance) //take closest match, which should have a very small distance since it's the same point
                {
                    bestDistance = distance;
                    faceIndex = i;
                }
            }
            return faceIndex;
        }

        private Vector2 ClosestPointOnSegment(Vector2 contactPoint, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a; 
            Vector2 ap = contactPoint - a;
            float distanceAlongAB = Mathf.Clamp01(Vector2.Dot(ap, ab) / ab.sqrMagnitude); //projection of ap onto side ab
            return a + ab * distanceAlongAB;
        }
    } 


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        collider = GetComponent<PolygonCollider2D>();
        movementSpeed = groundMovementSpeed;
        playerState = new FallingState(this);
    }


    void FixedUpdate()
    {
        HandleMovement();
    }

    
    void HandleMovement()
    {
        playerState.CheckConditions();
        playerState.UpdatePlayer();
        rb.linearVelocity = verticalVector + horizontalVector;
    }


    private bool IsOnSurface()
    {
        Vector2[] colliderPoints = collider.points;
        float closestSide = float.MaxValue;
        bool foundSurface = false;

        for (int i = 0; i < colliderPoints.Length; i++)
        {
            Vector2 vA = transform.TransformPoint(colliderPoints[i]);
            Vector2 vB = transform.TransformPoint(colliderPoints[(i + 1) % colliderPoints.Length]);

            Vector2 midPoint = (vA + vB)/2f;
            Vector2 surfaceVector = vB - vA;

            Vector2 normal = new Vector2(-surfaceVector.y, surfaceVector.x).normalized;
            if (Vector2.Distance(transform.position, midPoint+normal) > Vector2.Distance(transform.position, midPoint)) normal = -normal;

            float edgeLength = Vector2.Distance(vA, vB);
            Vector2 boxSize = new Vector2(edgeLength, edgeThickness);
            Vector2 edgeDir = surfaceVector.normalized;
            float angle = Mathf.Atan2(edgeDir.y, edgeDir.x) * Mathf.Rad2Deg;
            RaycastHit2D hit = Physics2D.BoxCast(midPoint, boxSize, angle, normal, edgeThickness, groundLayer);
            if (hit)
            {
                if (hit.distance < closestSide)
                {
                    foundSurface = true;
                    gravity = -hit.normal;
                    closestSide = hit.distance;
                    contactPoint = hit.point;
                    groundSurface = new Vector2(hit.normal.y, -hit.normal.x).normalized;
                    currentSurface = hit.collider as PolygonCollider2D; 
                }
            }
        }
        return foundSurface;
    }

    private bool IsStillOnSurface(int localFaceIndex)
    {
        Vector2[] points = collider.points;
        Vector2 vA = transform.TransformPoint(points[localFaceIndex]);
        Vector2 vB = transform.TransformPoint(points[(localFaceIndex + 1) % points.Length]);
        Vector2 midPoint = (vA + vB) * 0.5f;
        Vector2 edge = vB - vA;

        float edgeLength = edge.magnitude;
        Vector2 edgeDir = edge.normalized;
        float angle = Mathf.Atan2(edgeDir.y, edgeDir.x) * Mathf.Rad2Deg;
        Vector2 boxSize = new Vector2(edgeLength, edgeThickness);

        RaycastHit2D hit = Physics2D.BoxCast(midPoint, boxSize, angle, gravity, edgeThickness, groundLayer);
        if (!hit) return false;
        if (hit.collider != currentSurface) return false;// Ensure same collider

        // Ensure still aligned with gravity
        float alignment = Vector2.Dot(hit.normal, -gravity);

        // tolerance
        if (alignment < 0.8f) return false;

        // Update contact info if desired
        //contactPoint = hit.point;

        return true;
    }


    private void OnDrawGizmos()
{
    if (collider == null) return;

    Vector2[] colliderPoints = collider.points;

    for (int i = 0; i < colliderPoints.Length; i++)
    {
        Vector2 vA = transform.TransformPoint(colliderPoints[i]);
        Vector2 vB = transform.TransformPoint(
            colliderPoints[(i + 1) % colliderPoints.Length]
        );

        Vector2 midPoint = (vA + vB) / 2f;
        Vector2 surfaceVector = vB - vA;

        Vector2 normal = new Vector2(
            -surfaceVector.y,
            surfaceVector.x
        ).normalized;

        Vector2 toMid = midPoint - (Vector2)transform.position;

        if (Vector2.Dot(normal, toMid) > 0f)
            normal = -normal;

        float edgeLength = surfaceVector.magnitude;

        float angle = Mathf.Atan2(
            surfaceVector.y,
            surfaceVector.x
        ) * Mathf.Rad2Deg;

        Matrix4x4 oldMatrix = Gizmos.matrix;

        // Starting box
        Gizmos.matrix = Matrix4x4.TRS(
            midPoint,
            Quaternion.Euler(0, 0, angle),
            Vector3.one
        );

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(
            Vector3.zero,
            new Vector3(edgeLength, edgeThickness, 0)
        );

        Gizmos.matrix = oldMatrix;

        // Cast direction
        Gizmos.color = Color.red;
        Gizmos.DrawLine(
            midPoint,
            midPoint + normal * groundCheckDistance
        );

        // End position box
        Vector2 endPoint = midPoint + normal * groundCheckDistance;

        Gizmos.matrix = Matrix4x4.TRS(
            endPoint,
            Quaternion.Euler(0, 0, angle),
            Vector3.one
        );

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(
            Vector3.zero,
            new Vector3(edgeLength, edgeThickness, 0)
        );

        Gizmos.matrix = oldMatrix;

        // Visualize actual hit
        RaycastHit2D hit = Physics2D.BoxCast(
            midPoint,
            new Vector2(edgeLength, edgeThickness),
            angle,
            normal,
            groundCheckDistance,
            groundLayer
        );

        if (hit)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(hit.point, 0.03f);

            Gizmos.DrawLine(
                hit.point,
                hit.point + hit.normal * 0.2f
            );
        }
    }
}


    /*private bool IsOnSurface()
    {
        Vector2[] colliderPoints = collider.points;
        float closestSide = float.MaxValue;
        bool foundSurface = false;

        for (int i = 0; i < colliderPoints.Length; i++)
        {
            Vector2 vA = transform.TransformPoint(colliderPoints[i]);
            Vector2 vB = transform.TransformPoint(colliderPoints[(i + 1) % colliderPoints.Length]);

            Vector2 midPoint = (vA + vB)/2f;
            Vector2 surfaceVector = vB - vA;

            Vector2 normal = new Vector2(-surfaceVector.y, surfaceVector.x).normalized;
            if (Vector2.Distance(transform.position, midPoint+normal) > Vector2.Distance(transform.position, midPoint)) normal = -normal;

            RaycastHit2D raycastHit = Physics2D.Raycast(midPoint, normal, groundCheckDistance, groundLayer);
            if (raycastHit)
            {
                if (raycastHit.distance < closestSide)
                {
                    foundSurface = true;
                    closestSide = raycastHit.distance;
                    activeEdgeA = vA;
                    activeEdgeB = vB;
                    gravitySource = midPoint;
                    gravity = normal;
                    groundSurface = new Vector2(raycastHit.normal.y, -raycastHit.normal.x).normalized;
                }
            }
        }
        return foundSurface;
    }*/

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.started && jumpAvalailable)
        {
            jumpButtonDown = true;
        }
        if (context.canceled)
        {
            jumpButtonDown = false;
        }
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (context.started || context.performed) {
            
            inputVector = context.ReadValue<Vector2>().normalized;
        }
        if (context.canceled)
        {
            inputVector = Vector2.zero;
        }
    }
}


//TODO 
/*
Not allowing sticking when too upside down
Get the camera following the player
Configure the movement parameters
Configure the camera parameters
*/
