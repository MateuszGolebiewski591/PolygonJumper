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
    [SerializeField] private float groundMovementSpeed = 7.5f;
    [SerializeField] private float gravityForce = 9.81f; 
    [SerializeField] private float terminalVelocity = 15f;
    private Vector2 horizontalVector = Vector2.zero;
    private Vector2 verticalVector = Vector2.zero;
    private Vector2 additionalVector = Vector2.zero;
    private Vector2 groundSurface =  Vector2.zero;
    private Vector2 inputVector = Vector2.zero;
    private float movementSpeed = 0f;    
    private Vector2 gravity = Vector2.down;
    
    
    [Header("Jump Parameters")]
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float maxJumpTime = 2f;
    [SerializeField] private AnimationCurve jumpCurve;
    private bool jumpAvalailable = false;
    private bool jumpButtonDown = false;


    [Header("Dash Parameters")]
    [SerializeField] private float airDashTime = 0.2f;
    [SerializeField] private AnimationCurve airDashCurve;
    private bool airDashAvailable = false;
    private bool airDashButtonDown = false;


    [Header("edge detection")]
    [SerializeField] private float edgeThickness = 0.1f;
    [SerializeField] private float groundCheckDistance = 0.1f;
    private PolygonCollider2D currentSurface; 
    private Vector2 contactPoint;

    private PlayerStateNum state = PlayerStateNum.Falling;
    private PlayerState playerState;

    private enum PlayerStateNum
    {
        Grounded,
        Jumping,
        Falling,
        Sticking,
        AirDash,
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
        private float elapsedFallTime = 0;
        private float maxFallTime;
        private Vector2 initialAdditionalVector;
        private Vector2 currentAdditionalVector;
        
        public FallingState(PlayerMovement movement, float elapsedFall)
        {
            player = movement;
            player.jumpAvalailable = false;
            player.state = state;
            maxFallTime = player.maxJumpTime;
            player.verticalVector = Vector2.Dot(Vector2.down, player.additionalVector) * Vector2.down;
            elapsedFallTime = elapsedFall;
            initialAdditionalVector = player.additionalVector;
            currentAdditionalVector = player.additionalVector;
        }

        override public void CheckConditions()
        {
            if (player.IsOnSurface()) player.playerState = new StickingState(player);
            if (player.airDashAvailable && player.airDashButtonDown && player.inputVector.x != 0) player.playerState = new AirDashState(player);
        }
        override public void UpdatePlayer()
        {
            elapsedFallTime += Time.deltaTime;
            float fallStrength = player.jumpCurve.Evaluate(Mathf.Clamp01(1 - elapsedFallTime/maxFallTime));
            player.verticalVector += fallStrength * player.gravityForce * Vector2.down;
            if (player.verticalVector.magnitude > player.terminalVelocity)
            {
                Vector2.Normalize(player.verticalVector);
                player.verticalVector *= player.terminalVelocity;
            }
            player.horizontalVector = player.movementSpeed * Vector2.Dot(player.inputVector, Vector2.left) * Vector2.left;
            if (Vector2.Dot(player.horizontalVector, currentAdditionalVector) < 0) initialAdditionalVector = Vector2.zero;
            if (player.horizontalVector.magnitude == 0) currentAdditionalVector *= 0.95f;
            else currentAdditionalVector = initialAdditionalVector;
            player.additionalVector = currentAdditionalVector;
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
            player.airDashAvailable = false;
            player.state = state; 
        }
        override public void CheckConditions()
        {
            elapsedJumpTime += Time.deltaTime;
            bool grounded = player.IsOnSurface();
            if (grounded && airborne) {
                player.playerState = new StickingState(player);
                return;
            }
            else if (!grounded && player.jumpButtonDown && !player.airDashAvailable) {
                airborne = true;
                player.airDashAvailable = true;
            }
            else if (player.airDashAvailable && player.airDashButtonDown && player.inputVector.x != 0)
            {
                player.playerState = new AirDashState(player);
                return;
            }
            else if (!grounded && !player.jumpButtonDown) {
                player.playerState = new FallingState(player, elapsedJumpTime);
                return;
            }
            if (elapsedJumpTime >= maxJumpTime) player.playerState = new FallingState(player, elapsedJumpTime);
        }
        override public void UpdatePlayer()
        {
            player.additionalVector = -player.gravity * player.jumpForce * player.jumpCurve.Evaluate(elapsedJumpTime/maxJumpTime/2);
            if (player.additionalVector.y <= 0.05)
            {
                player.additionalVector.y = 0;
                float fallStrength = player.jumpCurve.Evaluate(Mathf.Clamp01(1 - elapsedJumpTime/player.maxJumpTime));
                player.verticalVector += player.gravityForce * Vector2.down * fallStrength;
                if (player.verticalVector.magnitude > player.terminalVelocity)
                {
                    Vector2.Normalize(player.verticalVector);
                    player.verticalVector *= player.terminalVelocity;
                }
            }
            else player.verticalVector = Vector2.zero;
            player.horizontalVector = player.movementSpeed * Vector2.Dot(player.inputVector, Vector2.left) * Vector2.left;
            if (player.horizontalVector.magnitude > 0 && (player.additionalVector.x > 0.5 || player.additionalVector.x < -0.5)) player.horizontalVector *= 0.5f;
        }
    } 

    private class AirDashState : PlayerState
    {
        public PlayerStateNum state = PlayerStateNum.AirDash;
        PlayerMovement player;
        private float airDashTime;
        private float elapsedAirDashTime = 0;
        private Vector2 airDashDirection;

        public AirDashState(PlayerMovement movement)
        {
            player = movement;
            player.airDashAvailable = false;
            player.state = state;
            airDashTime = player.airDashTime;
            airDashDirection = player.inputVector;
            player.verticalVector = Vector2.zero;
            player.additionalVector = Vector2.zero;
        }

        override public void CheckConditions()
        {
            bool grounded = player.IsOnSurface();
            elapsedAirDashTime += Time.deltaTime;
            if (grounded) player.playerState = new StickingState(player);
            if (elapsedAirDashTime >= airDashTime) player.playerState = new FallingState(player, 0);
        }

        override public void UpdatePlayer()
        {
            if (Vector2.Dot(player.inputVector, airDashDirection) < 0 && elapsedAirDashTime < 0.8f * airDashTime) elapsedAirDashTime = 0.8f * airDashTime;
            float dashSpeed = player.airDashCurve.Evaluate(elapsedAirDashTime/airDashTime);
            player.horizontalVector = airDashDirection * dashSpeed * 3f * player.movementSpeed; 
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
        private float postCorrectionLimit = 5f;
        private float timeSincePostCorrectionStart = 0f;
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
            player.airDashAvailable = true;
            player.IsOnSurface();
            player.state = state;
        }

        public void DefineLocalFaceIndex(int face) {localFaceIndex = face;}
        override public void CheckConditions()
        {
            Vector2[] points = player.collider.points;
            Vector2 a = player.collider.transform.TransformPoint(points[localFaceIndex]);
            Vector2 b = player.collider.transform.TransformPoint(points[(localFaceIndex + 1) % points.Length]);
            Vector2 edge = b - a;
            Vector2 currentNormal = new Vector2(-edge.y, edge.x).normalized;
            Vector2 midpoint = (a + b) / 2f;
            if (Vector2.Dot(currentNormal, midpoint - (Vector2)player.transform.position) < 0f) currentNormal = -currentNormal;
            if (correctionState)
            {
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
            bool supported = HasSupport(midpoint);
            if (postCorrectionState) timeSincePostCorrectionStart += Time.deltaTime;
            if (supported && postCorrectionState || timeSincePostCorrectionStart >= postCorrectionLimit) {
                postCorrectionState = false;
                timeSincePostCorrectionStart = 0;
            }
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
                    if (Vector2.Dot(-currentNormal, nextNormal) > 0.99995f) {
                        targetNormal = prevNormal;
                        targetIndex = prevFace;
                    }
                    else
                    {
                        targetNormal = nextNormal;
                        targetIndex = nextFace;
                    }
                }
                else
                {
                    if (Vector2.Dot(-currentNormal, prevNormal) > 0.99995f) {
                        targetNormal = nextNormal;
                        targetIndex = nextFace;
                    }
                    else
                    {
                        targetNormal = prevNormal;
                        targetIndex = prevFace;
                    }
                }
                stickingPoint = player.currentSurface.transform.TransformPoint(surfacePoints[closestCorner]);
                correctionState = true;
                return;
            }
            if (player.jumpButtonDown && !waitingOnJumpUp) player.playerState = new JumpingState(player);
        }
        override public void UpdatePlayer()
        {
            player.additionalVector = Vector2.zero;
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

        private bool HasSupport(Vector2 midpoint)
        {
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
            player.airDashAvailable = false;

            ResolveStickingPrerequisites(player.currentSurface, player.contactPoint);
            player.state = state;
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
            player.additionalVector = Vector2.zero;
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
                int looseCornerIndex = -1;
                if (TryGetNearbyCorner(surfaceHitbox, contactPoint, out looseCornerIndex)) StabilizeTargetFace(looseCornerIndex);
            }
            else
            {
                //get adjacent corners on triangle, compute outward vectors, dot product with target vector, take highest raw value
                Vector2[] trianglePoints = player.collider.points; 
                Vector2 trianglePrevPoint = player.collider.transform.TransformPoint(trianglePoints[(cornerIndex - 1 + trianglePoints.Length) % trianglePoints.Length]);
                Vector2 triangleCurrentPoint = player.collider.transform.TransformPoint(trianglePoints[cornerIndex]);
                Vector2 triangleNextPoint = player.collider.transform.TransformPoint(trianglePoints[(cornerIndex + 1) % trianglePoints.Length]);
                Vector2 trianglePrevFace = trianglePrevPoint - triangleCurrentPoint; //outward vectors for triangle stemming from corner which we hit with
                Vector2 triangleNextFace = triangleNextPoint - triangleCurrentPoint;
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

                    float prevDot = Vector2.Dot(trianglePrevFace, targetVector); //dot product to see which face is more aligned with the object face
                    float nextDot = Vector2.Dot(triangleNextFace, targetVector);
                    if (prevDot >= nextDot) localFaceIndex = (cornerIndex - 1 + trianglePoints.Length) % trianglePoints.Length;
                    else localFaceIndex = cornerIndex;
                    int looseCornerIndex = -1;
                    if (TryGetNearbyCorner(surfaceHitbox, contactPoint, out looseCornerIndex)) StabilizeTargetFace(looseCornerIndex);
                }
                else // triangle corner - object face
                {
                    Vector2 prev = trianglePrevFace.normalized;//ensure normalized despite not being needed 
                    Vector2 next = triangleNextFace.normalized;
                    Vector2 targetEdge = new Vector2(-targetNormal.y, targetNormal.x).normalized; //target edge derived from normal, direction is irrelevant
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
                    if (prevDot >= nextDot) localFaceIndex = (cornerIndex - 1 + trianglePoints.Length) % trianglePoints.Length;
                    else localFaceIndex = cornerIndex;
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
            if (Vector2.Dot(normalPrev, toMidPrev) < 0f) normalPrev = -normalPrev; //ensures outward facing normal

            Vector2 next = nextPoint - currentPoint;
            Vector2 normalNext = new Vector2(-next.y, next.x).normalized;
            Vector2 midpointNext = (nextPoint + currentPoint) / 2f; // midpoint of vector
            Vector2 toMidNext = midpointNext - (Vector2)surfaceHitbox.bounds.center; //Vector from object center to the midpoint (should be parallel to normal)
            if (Vector2.Dot(normalNext, toMidNext) < 0f) normalNext = -normalNext; //ensures outward facing normal 

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

        private bool TryGetNearbyCorner(PolygonCollider2D hitbox, Vector2 contactPoint, out int cornerIndex)
        {
            cornerIndex = -1;
            Vector2[] points = hitbox.points;
            float closestDistance = float.MaxValue;
            int closestIndex = -1;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 worldPoint = hitbox.transform.TransformPoint(points[i]);
                float distance = Vector2.Distance(contactPoint, worldPoint);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = i;
                }
            }
            float cornerTolerance = 0.1f;
            if (closestDistance <= cornerTolerance)
            {
                cornerIndex = closestIndex;
                return true;
            }
            return false;
        }

        private void StabilizeTargetFace(int closestCornerIndex)
        {
            Vector2[] triPoints = player.collider.points;
            Vector2 faceA = player.collider.transform.TransformPoint(triPoints[localFaceIndex]);
            Vector2 faceB = player.collider.transform.TransformPoint(triPoints[(localFaceIndex + 1) % triPoints.Length]);
            Vector2 edge = faceB - faceA;
            Vector2 currentNormal = new Vector2(-edge.y, edge.x).normalized;
            Vector2 faceMidpoint = (faceA + faceB) * 0.5f;
            if (Vector2.Dot(currentNormal, faceMidpoint - (Vector2)player.transform.position) < 0f) currentNormal = -currentNormal;
            
            Vector2[] surfacePoints = targetHitbox.points;
            Vector2 prevPoint = targetHitbox.transform.TransformPoint(surfacePoints[(closestCornerIndex - 1 + surfacePoints.Length) % surfacePoints.Length]);
            Vector2 cornerPoint = targetHitbox.transform.TransformPoint(surfacePoints[closestCornerIndex]);
            Vector2 nextPoint = targetHitbox.transform.TransformPoint(surfacePoints[(closestCornerIndex + 1) % surfacePoints.Length]);
            Vector2 prevEdge = (prevPoint - cornerPoint).normalized;
            Vector2 nextEdge = (nextPoint - cornerPoint).normalized;

            Vector2 midpointVector = (faceMidpoint - cornerPoint).normalized;
            float prevDot = Vector2.Dot(midpointVector, prevEdge);
            float nextDot = Vector2.Dot(midpointVector, nextEdge);
            if (Mathf.Abs(nextDot - prevDot) < 0.4825f)
            {
                Vector2 prevNormal = new Vector2(-prevEdge.y, prevEdge.x).normalized;
                Vector2 nextNormal = new Vector2(-nextEdge.y, nextEdge.x).normalized;
                Vector2 prevMidpoint = (prevPoint + cornerPoint)/2f;
                Vector2 nextMidpoint = (nextPoint + cornerPoint)/2f;
                if (Vector2.Dot(prevNormal, prevMidpoint - (Vector2)player.currentSurface.bounds.center) < 0f) prevNormal = -prevNormal;
                if (Vector2.Dot(nextNormal, nextMidpoint - (Vector2)player.currentSurface.bounds.center) < 0f) nextNormal = -nextNormal;
                bool prevPrediction = PredictPosition(cornerPoint, faceMidpoint, currentNormal, prevNormal);
                bool nextPrediction = PredictPosition(cornerPoint, faceMidpoint, currentNormal, nextNormal);
                if (prevPrediction && nextPrediction) return;
                if (prevPrediction && !nextPrediction)
                {
                    if (closestCornerIndex == targetIndex)
                    {
                        targetIndex = (closestCornerIndex - 1 + surfacePoints.Length) % surfacePoints.Length;
                        targetNormal = prevNormal;
                    }
                    return;
                }
                if (nextPrediction && !prevPrediction)
                {
                    if (closestCornerIndex != targetIndex)
                    {
                        targetIndex = closestCornerIndex;
                        targetNormal = nextNormal;
                    }
                    return;
                }
            }  
            float currentDot, challengeDot;
            Vector2 challengeEdge;
            int challengeIndex;
            if (closestCornerIndex == targetIndex)
            {
                challengeDot = Vector2.Dot(midpointVector, prevEdge);
                currentDot = Vector2.Dot(midpointVector, nextEdge);
                challengeIndex = (closestCornerIndex - 1 + surfacePoints.Length) % surfacePoints.Length;
                challengeEdge = prevEdge;
            }
            else
            {
                challengeDot = Vector2.Dot(midpointVector, nextEdge);
                currentDot = Vector2.Dot(midpointVector, prevEdge);
                challengeEdge = nextEdge;
                challengeIndex = closestCornerIndex;
            }
            
            if (challengeDot > currentDot)
            {
                targetIndex = challengeIndex;
                Vector2 normal = new Vector2(-challengeEdge.y, challengeEdge.x).normalized; //compute normal
                Vector2 a = targetHitbox.transform.TransformPoint(surfacePoints[challengeIndex]);
                Vector2 b = targetHitbox.transform.TransformPoint(surfacePoints[(challengeIndex + 1) % surfacePoints.Length]);
                Vector2 midpoint = (a + b) / 2f; // midpoint of vector
                Vector2 toMid = midpoint - (Vector2)targetHitbox.bounds.center; //Vector from object center to the midpoint (should be parallel to normal)
                if (Vector2.Dot(normal, toMid) < 0f)normal = -normal; //ensures outward facing normal
                targetNormal = normal;
            }
        } 

        private bool PredictPosition(Vector2 cornerPoint, Vector2 midpoint,  Vector2 currentNormal, Vector2 candidateTargetNormal)
        {
            float angle = Vector2.SignedAngle(currentNormal, -candidateTargetNormal);
            Vector2 predictedMidpoint = RotatePointAroundPivot(midpoint, cornerPoint, angle);
            RaycastHit2D hit = Physics2D.Raycast(predictedMidpoint, -candidateTargetNormal, 1f, player.groundLayer);
            if (!hit) return false;
            return hit.collider == player.currentSurface;
        }

        Vector2 RotatePointAroundPivot(Vector2 point, Vector2 pivot, float angleDegrees)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            Vector2 offset = point - pivot;
            Vector2 rotated = new Vector2(offset.x * cos - offset.y * sin, offset.x * sin + offset.y * cos);
            return pivot + rotated;
        }  
    } 


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        collider = GetComponent<PolygonCollider2D>();
        movementSpeed = groundMovementSpeed;
        playerState = new FallingState(this, 0);
    }


    void FixedUpdate()
    {
        HandleMovement();
    }

    
    void HandleMovement()
    {
        playerState.CheckConditions();
        playerState.UpdatePlayer();
        rb.linearVelocity = verticalVector + horizontalVector + additionalVector;
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
        float alignment = Vector2.Dot(hit.normal, -gravity); // Ensure still aligned with gravity
        if (alignment < 0.8f) return false;
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

    public void Dash(InputAction.CallbackContext context)
    {
        if (context.started && airDashAvailable)
        {
            airDashButtonDown = true;
        }
        if (context.canceled)
        {
            airDashButtonDown = false;
        }
    }
}


//TODO 
/*
Configure the movement parameters
Configure the camera parameters
Add the movement tech
Add obstacles and checkpoints
*/
