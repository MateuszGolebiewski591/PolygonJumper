using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private PolygonCollider2D playerCollider;

    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer;


    [Header("Movement Parameters")]
    [SerializeField] private float groundMovementSpeed = 7.5f;
    [SerializeField] private float maxAirMovementSpeed = 12f;
    [SerializeField] private float lateralMovementDecay = 0.95f;
    [SerializeField] private float gravityForce = 9.81f; 
    [SerializeField] private float terminalVelocity = 15f;
    [SerializeField] private float maxFallTime = 0.6f;
    [SerializeField] private AnimationCurve gravityCurve;
    [SerializeField] private float rotationSpeed = 10f;
    private Vector2 horizontalVector = Vector2.zero;
    private Vector2 verticalVector = Vector2.zero;
    private Vector2 additionalVector = Vector2.zero;
    private Vector2 groundSurface =  Vector2.zero;
    private Vector2 inputVector = Vector2.zero;
    private float movementSpeed = 0f;    
    private Vector2 gravity = Vector2.down;
    private bool rollLeftButtonDown = false;
    private bool rollRightButtonDown = false;
    private bool overrideMovement = false;
    private bool beingRedirected = false;
    private RedirectPad redirectPad;
    private bool beingTeleported = false;
    private LevelComplete portal;
    
    
    [Header("Jump Parameters")]
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float maxJumpTime = 2f;
    [SerializeField] private float maxJumpEndTime = 0.5f;
    [SerializeField] private AnimationCurve jumpCurve;
    [SerializeField] private float maxDoubleJumpTime = 0.3f;
    [SerializeField] private AnimationCurve doubleJumpCurve;
    private bool jumpAvalailable = false;
    private bool jumpButtonDown = false;
    private bool doubleJumpAvailable = false;


    [Header("Dash Parameters")] 
    [SerializeField] private float airDashTime = 0.2f;
    [SerializeField] private AnimationCurve airDashCurve;
    private bool airDashAvailable = false;
    private bool airDashButtonDown = false;

    [Header("Redirection Parameters")]
    [SerializeField] private float releaseTime = 0.3f;
    [SerializeField] private float correctionTime = 0.1f;


    [Header("Movement Tech Tracking")]
    private bool airDashUsed = true;
    private bool doubleJumpUsed = true;
    private bool hasAirDash;
    private bool hasDoubleJump;
    private bool hasRotation;


    [Header("Edge Detection")]
    [SerializeField] private float edgeThickness = 0.1f;
    private PolygonCollider2D currentSurface; 
    private Vector2 contactPoint;

    [Header("Animations")]
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private SpriteRenderer pulseSprite;
    [SerializeField] private AnimationCurve pulseSizeCurve;
    [SerializeField] private AnimationCurve pulseOpacityCurve;
    [SerializeField] private float pulseLength = 0.2f;
    [SerializeField] private float startingOpacity = 0.4f;
    [SerializeField] private float pulsePower = 1.5f;


    [Header("Other")]
    [SerializeField] private GameEvent gameEventChannel;
    [SerializeField] public GlobalPlayerState globalPlayerState;
    [SerializeField] private CameraAnchor cameraAnchor;
    [SerializeField] private GameObject goal;
    private Material shineMaterial;
    private bool gamePaused = false;
    private bool inputsAllowed = true;
    private Vector2 cameraInputVector = Vector2.zero;
    private bool levelCompleted = false;

    private PlayerStateNum state = PlayerStateNum.Falling;
    private PlayerState playerState;

    private enum PlayerStateNum
    {
        Grounded,
        Jumping,
        Falling,
        Sticking,
        AirDash,
        DoubleJump,
        Redirection,
        Completion,
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
        new public PlayerStateNum state = PlayerStateNum.Falling;
        PlayerMovement player;
        private float elapsedFallTime = 0;
        private float maxFallTime;
        private Vector2 initialAdditionalVector;
        private bool jumpButtonInitiallyPressed;
        private float cumulativeLateralDecay = 1f;
        private Vector2 cumulativeVerticalVector;
        private bool fromRedirection = false;
        
        public FallingState(PlayerMovement movement)
        {
            player = movement;
            player.jumpAvalailable = false;
            player.state = state;
            maxFallTime = player.maxFallTime;
            player.verticalVector = Vector2.Dot(Vector2.down, player.additionalVector) * Vector2.down;
            initialAdditionalVector = player.additionalVector;
            jumpButtonInitiallyPressed = player.jumpButtonDown;
            cumulativeVerticalVector = player.verticalVector;
            if (!jumpButtonInitiallyPressed) player.doubleJumpAvailable = true;
            else player.doubleJumpAvailable = false;
        }

        public void FromRedirection()
        {
            fromRedirection = true;
        }

        override public void CheckConditions()
        {
            if (player.beingTeleported) {
                player.playerState = new CompletionState(player);
                return;
            }
            elapsedFallTime += Time.deltaTime;
            if (jumpButtonInitiallyPressed && !player.jumpButtonDown) player.doubleJumpAvailable = true;
            if (player.IsOnSurface()) player.playerState = new StickingState(player);
            else if (player.beingRedirected && player.hasRotation) player.playerState = new RedirectionState(player);
            else if (!player.doubleJumpUsed && player.doubleJumpAvailable && player.jumpButtonDown && player.hasDoubleJump) player.playerState = new DoubleJumpState(player);
            else if (player.airDashAvailable && player.airDashButtonDown && player.inputVector.x != 0 && !player.airDashUsed && player.hasAirDash) player.playerState = new AirDashState(player);
        }
        override public void UpdatePlayer()
        {
            cumulativeVerticalVector += Vector2.down * player.gravityForce * player.gravityCurve.Evaluate(Mathf.Clamp01(elapsedFallTime/maxFallTime)); //Calculating forces and decays
            cumulativeLateralDecay *= player.lateralMovementDecay;
            cumulativeVerticalVector.y += initialAdditionalVector.y;
            player.additionalVector = new Vector2(initialAdditionalVector.x * cumulativeLateralDecay, 0);
            if (cumulativeVerticalVector.magnitude > player.terminalVelocity) //Limits falling speed to within terminal velocity
            {
                Vector2.Normalize(cumulativeVerticalVector);
                cumulativeVerticalVector *= player.terminalVelocity;
            }
            player.verticalVector = cumulativeVerticalVector;
            player.horizontalVector = player.movementSpeed * Vector2.Dot(player.inputVector, Vector2.left) * Vector2.left;
            if (player.horizontalVector.x * initialAdditionalVector.x < 0) {
                initialAdditionalVector.x = 0;
                player.additionalVector.x = 0;
            }
           
            if (player.horizontalVector.x + player.additionalVector.x > player.maxAirMovementSpeed && !fromRedirection) //Limits air movement speed
            {
                float total = player.horizontalVector.x + player.additionalVector.x; 
                float excess = total - player.maxAirMovementSpeed;
                player.horizontalVector.x -= excess;
            }
            else if (player.horizontalVector.x + player.additionalVector.x < -player.maxAirMovementSpeed)
            {
                float total = player.horizontalVector.x + player.additionalVector.x; 
                float excess = total + player.maxAirMovementSpeed;
                player.horizontalVector.x -= excess;
            }
            if (player.rollLeftButtonDown) player.transform.Rotate(0f, 0f, -player.rotationSpeed*Time.deltaTime);
            if (player.rollRightButtonDown) player.transform.Rotate(0f, 0f, player.rotationSpeed*Time.deltaTime);
        }
    }

    private class JumpingState : PlayerState
    {
        new public PlayerStateNum state = PlayerStateNum.Jumping;
        PlayerMovement player;
        bool airborne = false;
        private float elapsedJumpTime = 0;
        private float maxJumpTime;
        private float cumulativeLateralDecay = 1f;
        private bool jumpCompleted = false;
        private float timeSinceJumpEnd = 0;
        private float maxJumpEndTime;
        public JumpingState(PlayerMovement movement)
        {
            player = movement;
            player.jumpAvalailable = false;
            maxJumpTime = player.maxJumpTime;
            maxJumpEndTime = player.maxJumpEndTime;
            player.airDashAvailable = false;
            player.state = state; 
            player.doubleJumpAvailable = false;
            player.additionalVector = -player.gravity * player.jumpForce;
            player.verticalVector = Vector2.zero;
            AudioManager.Instance.PlayJumpSound();
        }
        override public void CheckConditions()
        {
            if (player.beingTeleported) {
                player.playerState = new CompletionState(player);
                return;
            }
            elapsedJumpTime += Time.deltaTime;
            bool grounded = player.IsOnSurface();
            if (!grounded && player.jumpButtonDown && !player.airDashAvailable)
            {
                airborne = true;
                player.airDashAvailable = true;
            }
            if (!grounded && !player.jumpButtonDown) {
                player.doubleJumpAvailable = true;
                jumpCompleted = true;
            }
            if (elapsedJumpTime >= maxJumpTime) jumpCompleted = true;
            if (jumpCompleted && player.additionalVector.y <= 0) timeSinceJumpEnd = maxJumpEndTime;

            if (grounded && airborne) player.playerState = new StickingState(player);
            else if (player.beingRedirected && player.hasRotation) player.playerState = new RedirectionState(player);
            else if (player.airDashAvailable && player.airDashButtonDown && player.inputVector.x != 0 && player.hasAirDash) player.playerState = new AirDashState(player);
            else if (player.doubleJumpAvailable && player.jumpButtonDown && player.hasDoubleJump) player.playerState = new DoubleJumpState(player);
            else if (timeSinceJumpEnd >= maxJumpEndTime) {
                player.playerState = new FallingState(player);
                player.additionalVector = Vector2.zero;
            }
        }
        override public void UpdatePlayer()
        {
            cumulativeLateralDecay *= player.lateralMovementDecay;
            Vector2 baseJumpVector = -player.gravity * player.jumpForce;
            if (jumpCompleted) //Handles difference between mid-jump and end of jump
            {
                timeSinceJumpEnd += Time.deltaTime;
                player.additionalVector = -player.gravity * player.jumpForce * player.jumpCurve.Evaluate(timeSinceJumpEnd/maxJumpEndTime);
                if (player.additionalVector.y < 0) player.additionalVector.y = baseJumpVector.y;
            }
            else player.additionalVector = baseJumpVector;
            player.additionalVector.x = baseJumpVector.x * cumulativeLateralDecay;
            
            player.horizontalVector = player.movementSpeed * Vector2.Dot(player.inputVector, Vector2.left) * Vector2.left;
            if (player.horizontalVector.x + player.additionalVector.x > player.maxAirMovementSpeed) //Limits how fast you can go in the air
            {
                float total = player.horizontalVector.x + player.additionalVector.x; 
                float excess = total - player.maxAirMovementSpeed;
                player.horizontalVector.x -= excess;
            }
            else if (player.horizontalVector.x + player.additionalVector.x < -player.maxAirMovementSpeed)
            {
                float total = player.horizontalVector.x + player.additionalVector.x; 
                float excess = total + player.maxAirMovementSpeed;
                player.horizontalVector.x -= excess;
            }
            if (airborne) //Air roll
            {
                if (player.rollLeftButtonDown) player.transform.Rotate(0f, 0f, -player.rotationSpeed*Time.deltaTime);
                if (player.rollRightButtonDown) player.transform.Rotate(0f, 0f, player.rotationSpeed*Time.deltaTime);
            }
        }
    } 

    private class DoubleJumpState : PlayerState
    {
        new public PlayerStateNum state = PlayerStateNum.DoubleJump;
        PlayerMovement player;
        private float timeElapsedSinceJump = 0;
        private float doubleJumpTime;

        public DoubleJumpState(PlayerMovement movement)
        {
            player = movement;
            player.doubleJumpAvailable = false;
            player.jumpAvalailable = false;
            player.airDashAvailable = true;
            doubleJumpTime = player.maxDoubleJumpTime;
            player.doubleJumpUsed = true;
            AudioManager.Instance.PlayJumpSound();
        }

        public override void CheckConditions()
        {
            if (player.beingTeleported) {
                player.playerState = new CompletionState(player);
                return;
            }
            timeElapsedSinceJump += Time.deltaTime;
            if (player.IsOnSurface()) player.playerState = new StickingState(player);
            else if (player.beingRedirected && player.hasRotation) player.playerState = new RedirectionState(player);
            else if (player.airDashAvailable && player.airDashButtonDown && !player.airDashUsed && player.hasAirDash) player.playerState = new AirDashState(player);
            else if (timeElapsedSinceJump >= doubleJumpTime) player.playerState = new FallingState(player);
        }

        public override void UpdatePlayer()
        {
            player.additionalVector.y = 0;
            player.horizontalVector = player.movementSpeed * Vector2.Dot(player.inputVector, Vector2.left) * Vector2.left;
            if (Vector2.Dot(player.inputVector, player.additionalVector) < 0) player.additionalVector = Vector2.zero;
            player.verticalVector = Vector2.up * player.gravityForce * 1.5f * player.doubleJumpCurve.Evaluate(timeElapsedSinceJump/doubleJumpTime);
            if (player.rollLeftButtonDown) player.transform.Rotate(0f, 0f, -player.rotationSpeed*Time.deltaTime);
            if (player.rollRightButtonDown) player.transform.Rotate(0f, 0f, player.rotationSpeed*Time.deltaTime);
        }
    }

    private class AirDashState : PlayerState
    {
        new public PlayerStateNum state = PlayerStateNum.AirDash;
        PlayerMovement player;
        private float airDashTime;
        private float elapsedAirDashTime = 0;
        private Vector2 airDashDirection;
        private bool jumpButtonInitiallyPressed;

        public AirDashState(PlayerMovement movement)
        {
            player = movement;
            player.airDashAvailable = false;
            player.state = state;
            airDashTime = player.airDashTime;
            airDashDirection = player.inputVector;
            airDashDirection.y = 0;
            airDashDirection.Normalize();
            player.verticalVector = Vector2.zero;
            player.additionalVector = Vector2.zero;
            jumpButtonInitiallyPressed = player.jumpButtonDown;
            if (!jumpButtonInitiallyPressed) player.doubleJumpAvailable = true;
            else player.doubleJumpAvailable = false;
            player.airDashUsed = true;
            AudioManager.Instance.PlayDashSound();
        }

        override public void CheckConditions()
        {
            if (player.beingTeleported) {
                player.playerState = new CompletionState(player);
                return;
            }
            bool grounded = player.IsOnSurface();
            elapsedAirDashTime += Time.deltaTime;
            if (jumpButtonInitiallyPressed && !player.jumpButtonDown) player.doubleJumpAvailable = true;
            if (grounded) player.playerState = new StickingState(player);
            else if (player.beingRedirected && player.hasRotation) player.playerState = new RedirectionState(player);
            else if (!player.doubleJumpUsed && player.doubleJumpAvailable && player.jumpButtonDown && player.hasDoubleJump) player.playerState = new DoubleJumpState(player);
            else if (elapsedAirDashTime >= airDashTime) player.playerState = new FallingState(player);
        }

        override public void UpdatePlayer()
        {
            if (Vector2.Dot(player.inputVector, airDashDirection) < 0 && elapsedAirDashTime < 0.8f * airDashTime) elapsedAirDashTime = 0.8f * airDashTime;
            float dashSpeed = player.airDashCurve.Evaluate(elapsedAirDashTime/airDashTime);
            player.horizontalVector = airDashDirection * dashSpeed * 3f * player.movementSpeed; 
            if (player.rollLeftButtonDown) player.transform.Rotate(0f, 0f, -player.rotationSpeed*Time.deltaTime);
            if (player.rollRightButtonDown) player.transform.Rotate(0f, 0f, player.rotationSpeed*Time.deltaTime);
        }
    }

    private class RedirectionState : PlayerState
    {
        new public PlayerStateNum state = PlayerStateNum.Redirection;
        PlayerMovement player;
        private Stage stage = Stage.correction;
        private Vector2 preservedMotion;
        private bool jumpButtonInitiallyPressed;
        private bool releaseAvailable;
        private float timeSinceRelease = 0f;
        private float cumulativeLateralDecay = 1f;
        private float elapsedCorrectionTime = 0f;
        private Vector3 initialPlayerPosition;

        public RedirectionState(PlayerMovement movement)
        {
            player = movement;
            preservedMotion = player.horizontalVector + player.verticalVector + player.additionalVector;
            player.horizontalVector = Vector2.zero;
            player.verticalVector = Vector2.zero;
            player.additionalVector = Vector2.zero;
            jumpButtonInitiallyPressed = player.jumpButtonDown;
            if (jumpButtonInitiallyPressed) releaseAvailable = false;
            else releaseAvailable = true;
            initialPlayerPosition = player.transform.position;
            player.redirectPad.LoadPad(Mathf.Atan2(preservedMotion.y, preservedMotion.x) * Mathf.Rad2Deg);
        }

        private enum Stage
        {
            correction,
            player, 
            ejection,
        }

        public override void CheckConditions()
        {
            if (player.beingTeleported) {
                player.playerState = new CompletionState(player);
                return;
            }
            if (jumpButtonInitiallyPressed && !player.jumpButtonDown) releaseAvailable = true;
            switch (stage)
            {
                case Stage.correction : {
                        elapsedCorrectionTime += Time.deltaTime;
                        if (elapsedCorrectionTime > player.correctionTime) {
                            stage = Stage.player;
                            player.transform.position = player.redirectPad.transform.position;
                            player.cameraAnchor.CenterAnchor();
                            AudioManager.Instance.StartPowerSound();
                        }
                        break;
                    }
                case Stage.player :
                    {
                        if (player.jumpButtonDown && releaseAvailable) {
                            player.additionalVector = preservedMotion;
                            player.beingRedirected = false;
                            stage = Stage.ejection;
                            player.redirectPad.UnloadPad();
                            player.redirectPad = null;
                            AudioManager.Instance.PlayDischargeSound();
                            AudioManager.Instance.EndPowerSound();
                        }
                        break;
                    }
                case Stage.ejection :
                    {
                        timeSinceRelease += Time.deltaTime;
                        if (timeSinceRelease > player.releaseTime) {
                            if (player.additionalVector.y > 0) player.additionalVector.y = 0;
                            FallingState state = new FallingState(player);
                            state.FromRedirection();
                            player.playerState = state;
                            
                        }
                        break;
                    }
            }
        }

        public override void UpdatePlayer()
        {
            switch (stage)
            {
                case Stage.correction : {
                        player.transform.position = Vector3.Lerp(initialPlayerPosition, player.redirectPad.transform.position, Mathf.Clamp01(elapsedCorrectionTime/player.correctionTime));
                        float angle = Mathf.Atan2(preservedMotion.y, preservedMotion.x) * Mathf.Rad2Deg;
                        player.redirectPad.UpdatePointer(angle);
                        break;
                    }
                case Stage.player :
                    {
                        if (player.rollLeftButtonDown) {
                            player.transform.Rotate(0f, 0f, -player.rotationSpeed*Time.deltaTime);
                            preservedMotion = Quaternion.Euler(0, 0, -player.rotationSpeed*Time.deltaTime) * preservedMotion;
                        }
                        if (player.rollRightButtonDown) {
                            player.transform.Rotate(0f, 0f, player.rotationSpeed*Time.deltaTime);
                            preservedMotion = Quaternion.Euler(0, 0, player.rotationSpeed*Time.deltaTime) * preservedMotion;
                        }
                        float angle = Mathf.Atan2(preservedMotion.y, preservedMotion.x) * Mathf.Rad2Deg;
                        player.redirectPad.UpdatePointer(angle);
                        break;
                    }
                case Stage.ejection :
                    {
                        cumulativeLateralDecay *= player.lateralMovementDecay;
                        player.additionalVector = preservedMotion;
                        player.additionalVector.x *= cumulativeLateralDecay;
                        if (preservedMotion.y > 0) player.additionalVector.y =  preservedMotion.y * (1 - timeSinceRelease/player.releaseTime);
                        break;
                    }
            }
        }
    }

    private class CompletionState : PlayerState
    {
        new public PlayerStateNum state = PlayerStateNum.Completion;
        PlayerMovement player;
        private float elapsedCorrectionTime = 0;
        private Vector3 initialPlayerPosition;
        private Vector3 initialPlayerScale;

        public CompletionState(PlayerMovement movement)
        {
            player = movement;
            initialPlayerPosition = player.transform.position;
            initialPlayerScale = player.transform.localScale;
            player.horizontalVector = Vector2.zero;
            player.verticalVector = Vector2.zero;
            player.additionalVector = Vector2.zero;
            AudioManager.Instance.PlayPortalSound();
        }

        override public void CheckConditions()
        {
            elapsedCorrectionTime += Time.deltaTime;
            if (elapsedCorrectionTime > player.correctionTime) player.gameEventChannel.Raise(new EventData{eventType=EventType.PortalEntered});
        }

        override public void UpdatePlayer()
        {
            float t = Mathf.Clamp01(elapsedCorrectionTime/player.correctionTime);
            player.transform.position = Vector3.Lerp(initialPlayerPosition, player.portal.transform.position, t);
            player.sprite.transform.localScale = Vector3.Lerp(initialPlayerScale, Vector3.zero, t);
        }
    }

    private class GroundedState : PlayerState
    {
        new public PlayerStateNum state = PlayerStateNum.Grounded;
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
        private Vector2 currentTravelVector = Vector2.zero;
        private Vector2 lastInputVector = Vector2.zero;
        public GroundedState(PlayerMovement movement, int face)
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
            player.doubleJumpAvailable = false;
            player.airDashUsed = false;
            player.doubleJumpUsed = false;
            player.IsOnSurface();
            player.state = state;
            localFaceIndex = face;
            player.additionalVector = Vector2.zero;
            player.currentSurface.transform.parent.GetComponentInChildren<PolygonPulseAppearance>().Pulse();
        }

        override public void CheckConditions()
        {
            if (player.beingTeleported) {
                player.playerState = new CompletionState(player);
                return;
            }
            Vector2[] points = player.playerCollider.points;
            Vector2 a = player.playerCollider.transform.TransformPoint(points[localFaceIndex]);
            Vector2 b = player.playerCollider.transform.TransformPoint(points[(localFaceIndex + 1) % points.Length]);
            Vector2 edge = b - a;
            Vector2 currentNormal = new Vector2(-edge.y, edge.x).normalized;
            Vector2 midpoint = (a + b) / 2f;
            if (Vector2.Dot(currentNormal, midpoint - (Vector2)player.transform.position) < 0f) currentNormal = -currentNormal;
            if (correctionState) //checking if we can come out of the correction state
            {
                if (Vector2.Dot(-currentNormal, targetNormal) > 0.99995f) {
                    correctionState = false;
                    postCorrectionState = true;
                    CalculateNewDirection();
                }
                else return;
            }
            if (waitingOnJumpUp) //Ensures holding down the jump button won't automatically trigger jumps
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
            if (!supported && !postCorrectionState) //Handles entering correction state
            {
                Vector2[] surfacePoints = player.currentSurface.points;
                int closestCorner = -1;
                float closestDistance = float.MaxValue;

                for (int i = 0; i < surfacePoints.Length; i++) //Find the closest corner which is the one we're pivoting around
                {
                    Vector2 worldPoint = player.currentSurface.transform.TransformPoint(surfacePoints[i]);
                    float distance = Vector2.Distance(player.transform.position, worldPoint);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestCorner = i;
                    }
                }
                int prevFace = (closestCorner - 1 + surfacePoints.Length) % surfacePoints.Length; //Get the indexes needed for the two faces
                int nextFace = closestCorner;
                Vector2 prevNormal = GetSurfaceNormal(prevFace); //Find normals of each face and dot product with current gravity
                Vector2 nextNormal = GetSurfaceNormal(nextFace);
                float prevDot = Vector2.Dot(prevNormal, -player.gravity);
                float nextDot = Vector2.Dot(nextNormal, -player.gravity);
                int nextCorner = 0;
                if (prevDot > nextDot) //Alligned to prev face currently
                {
                    if (Vector2.Dot(-currentNormal, nextNormal) > 0.99995f) { //Safety check/confirmation
                        targetNormal = prevNormal;
                        targetIndex = prevFace;
                    }
                    else //Expected outcome
                    {
                        targetNormal = nextNormal;
                        targetIndex = nextFace;
                    }
                }
                else //Currently alligned to the next face
                {
                    if (Vector2.Dot(-currentNormal, prevNormal) > 0.99995f) { //Safety check/final confirmation
                        targetNormal = nextNormal;
                        targetIndex = nextFace;
                    }
                    else //Expected outcome
                    {
                        targetNormal = prevNormal;
                        targetIndex = prevFace;
                        nextCorner = targetIndex;
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
            if (correctionState)
            {
                player.verticalVector = Vector2.zero;
                player.horizontalVector = Vector2.zero;
                Vector2[] points = player.playerCollider.points;
                Vector2 a = player.playerCollider.transform.TransformPoint(points[localFaceIndex]);
                Vector2 b = player.playerCollider.transform.TransformPoint(points[(localFaceIndex + 1) % points.Length]);
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
                if (player.inputVector != Vector2.zero) {
                    if (player.inputVector != lastInputVector) currentTravelVector = player.inputVector;
                    else if (currentTravelVector == Vector2.zero) currentTravelVector = player.inputVector;
                }
                else currentTravelVector = Vector2.zero;
                lastInputVector = player.inputVector;
                player.horizontalVector = player.movementSpeed * Vector2.Dot(player.groundSurface, currentTravelVector) * player.groundSurface;
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

        private void CalculateNewDirection()
        {
            Vector2[] surfacePoints = player.currentSurface.points;
            int closestCorner = -1;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < surfacePoints.Length; i++) //Find the closest corner which is the one we're pivoting around
            {
                Vector2 worldPoint = player.currentSurface.transform.TransformPoint(surfacePoints[i]);
                float distance = Vector2.Distance(player.transform.position, worldPoint);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestCorner = i;
                }
            }
            Vector2 currentPoint = player.currentSurface.transform.TransformPoint(surfacePoints[closestCorner]);
            Vector2 nextPoint = player.currentSurface.transform.TransformPoint(surfacePoints[(closestCorner + 1) % surfacePoints.Length]);
            Vector2 prevPoint = player.currentSurface.transform.TransformPoint(surfacePoints[(closestCorner - 1 + surfacePoints.Length) % surfacePoints.Length]);
            Vector2 nextEdge = nextPoint - currentPoint;
            Vector2 prevEdge = prevPoint - currentPoint; 
            if (Vector2.Dot(targetNormal, nextEdge) < 0.00005 && Vector2.Dot(targetNormal, nextEdge) > -0.00005) currentTravelVector = nextEdge;
            else if (Vector2.Dot(targetNormal, prevEdge) < 0.00005 && Vector2.Dot(targetNormal, prevEdge) > -0.00005) currentTravelVector = prevEdge;
            currentTravelVector.Normalize();
        }
    } 

    private class StickingState : PlayerState
    {
        new public PlayerStateNum state = PlayerStateNum.Sticking;
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
            player.doubleJumpAvailable = false;
            player.airDashUsed = true;
            player.doubleJumpUsed = true;

            ResolveStickingPrerequisites(player.currentSurface, player.contactPoint);
            player.state = state;
            player.additionalVector = Vector2.zero;
            player.verticalVector = Vector2.zero;
            player.horizontalVector = Vector2.zero;
            player.Pulse();
        }

        override public void CheckConditions()
        {
            if (player.beingTeleported) {
                player.playerState = new CompletionState(player);
                return;
            }
            Vector2[] points = player.playerCollider.points;
            Vector2 a = player.playerCollider.transform.TransformPoint(points[localFaceIndex]);
            Vector2 b = player.playerCollider.transform.TransformPoint(points[(localFaceIndex + 1) % points.Length]);
            Vector2 edge = b - a;
            Vector2 currentNormal = new Vector2(-edge.y, edge.x).normalized;
            Vector2 midpoint = (a + b) / 2f;
            if (Vector2.Dot(currentNormal, midpoint - (Vector2)player.transform.position) < 0f) currentNormal = -currentNormal;
            if (Vector2.Dot(-currentNormal, targetNormal) > 0.99995f) player.playerState = new GroundedState(player, localFaceIndex);
        }
        override public void UpdatePlayer() 
        {
            Vector2[] points = player.playerCollider.points;
            Vector2 a = player.playerCollider.transform.TransformPoint(points[localFaceIndex]);
            Vector2 b = player.playerCollider.transform.TransformPoint(points[(localFaceIndex + 1) % points.Length]);
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
            if (!triangleCorner) { //If hit with face, identify index of face
                localFaceIndex = ResolveTriangleContact(contactPoint);
                int looseCornerIndex = -1;
                if (TryGetNearbyCorner(surfaceHitbox, contactPoint, out looseCornerIndex)) StabilizeTargetFace(looseCornerIndex);
            }
            else
            {
                //get adjacent corners on triangle, compute outward vectors, dot product with target vector, take highest raw value
                Vector2[] trianglePoints = player.playerCollider.points; 
                Vector2 trianglePrevPoint = player.playerCollider.transform.TransformPoint(trianglePoints[(cornerIndex - 1 + trianglePoints.Length) % trianglePoints.Length]);
                Vector2 triangleCurrentPoint = player.playerCollider.transform.TransformPoint(trianglePoints[cornerIndex]);
                Vector2 triangleNextPoint = player.playerCollider.transform.TransformPoint(trianglePoints[(cornerIndex + 1) % trianglePoints.Length]);
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
            Vector2[] trianglePoints = player.playerCollider.points;
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
            Vector2[] points = player.playerCollider.points;
            float cornerTolerance = 0.2f;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 worldPoint = player.playerCollider.transform.TransformPoint(points[i]);
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
            Vector2[] points = player.playerCollider.points;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 a = player.playerCollider.transform.TransformPoint(points[i]); //retrieve edge
                Vector2 b = player.playerCollider.transform.TransformPoint(points[(i + 1) % points.Length]);
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
            Vector2[] triPoints = player.playerCollider.points;
            Vector2 faceA = player.playerCollider.transform.TransformPoint(triPoints[localFaceIndex]);
            Vector2 faceB = player.playerCollider.transform.TransformPoint(triPoints[(localFaceIndex + 1) % triPoints.Length]);
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

    void OnEnable()
    {
        gameEventChannel.OnEventRaised += HandleEvent;
    }

    void OnDisable()
    {
        gameEventChannel.OnEventRaised -= HandleEvent;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<PolygonCollider2D>();
        shineMaterial = GetComponentInChildren<SpriteRenderer>().material;
        movementSpeed = groundMovementSpeed;
        playerState = new FallingState(this);
        ResetPlayer();
    }


    void FixedUpdate()
    {
        HandleMovement();
    }

    void Update()
    {
        Vector2 lightDirection = (goal.transform.position - transform.position).normalized;
        Vector2 localLightDirection = transform.InverseTransformDirection(lightDirection);
        shineMaterial.SetVector("_PlayerToLightVector", localLightDirection);
    }
    
    private void HandleMovement()
    {
        if (overrideMovement) {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        playerState.CheckConditions();
        playerState.UpdatePlayer();
        rb.linearVelocity = verticalVector + horizontalVector + additionalVector;
    }

    

    private void HandleEvent(EventData data)
    {
        switch (data.eventType)
        {
            case EventType.PlayerDeath : 
                {
                    sprite.enabled = false;
                    overrideMovement = true;
                    AudioManager.Instance.PlayDeathSound();
                    break;
                }
            case EventType.LevelReset :
                {
                    ResetPlayer();
                    break;
                }
            case EventType.PauseGame :
                {
                    gamePaused = true;
                    inputsAllowed = false;
                    Time.timeScale = 0f;
                    break;
                }
            case EventType.ResumeGame :
                {
                    gamePaused = false;
                    inputsAllowed = true;
                    Time.timeScale = 1f;
                    break;
                }
            case EventType.ResetCoreState :
                {
                    Time.timeScale = 1f; 
                    break;
                }
            case EventType.PortalEntered :
                {
                    overrideMovement = true;
                    break;
                }
            case EventType.LevelComplete :
                {
                    levelCompleted = true;
                    inputsAllowed = false;
                    Time.timeScale = 0f;
                    break;
                }
        }
    }

    private void ResetPlayer()
    {
        sprite.enabled = true;
        overrideMovement = false;
        airDashAvailable = false;
        jumpAvalailable = false;
        doubleJumpAvailable = false;
        verticalVector = Vector2.zero;
        horizontalVector = Vector2.zero;
        additionalVector = Vector2.zero;
        gravity = Vector2.down;
        transform.position = globalPlayerState.respawnPoint;
        transform.rotation = Quaternion.Euler(Vector3.zero);
        transform.localScale = Vector3.one;
        sprite.transform.localScale = Vector3.one;
        inputsAllowed = true;
        redirectPad = null;
        beingRedirected = false;
        portal = null;
        beingTeleported = false;
        hasAirDash = globalPlayerState.hasAirDash;
        hasDoubleJump = globalPlayerState.hasDoubleJump;
        hasRotation = globalPlayerState.hasRotation;
        playerState = new FallingState(this);   
    }


    private bool IsOnSurface()
    {
        Vector2[] colliderPoints = playerCollider.points;
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

    public void TriggerRedirection(RedirectPad pad)
    {
        beingRedirected = true;
        redirectPad = pad;
    }

    public void TriggerPortal(LevelComplete completionPortal)
    {
        beingTeleported = true;
        portal = completionPortal;
    }

    private void Pulse()
    {
        StartCoroutine(PlayPulse());
    }

    private IEnumerator PlayPulse()
    {
        float timeElapsed = 0f;
        Color colour = pulseSprite.color;
        pulseSprite.transform.localScale = Vector3.one;
        pulseSprite.enabled = true;
        while (timeElapsed < pulseLength)
        {
            timeElapsed += Time.deltaTime;
            float newSize = pulsePower * pulseSizeCurve.Evaluate(timeElapsed/pulseLength);
            float newOpacity = pulseOpacityCurve.Evaluate(timeElapsed/pulseLength);
            pulseSprite.transform.localScale = new Vector3(newSize, newSize, newSize);
            pulseSprite.color = new Color(colour.r, colour.g, colour.b, newOpacity*startingOpacity);
            yield return null;
        }
        pulseSprite.enabled = false;
    }
    
    

    public void Jump(InputAction.CallbackContext context)
    {
        if (!inputsAllowed) return;
        if (context.started && (jumpAvalailable || doubleJumpAvailable || beingRedirected))
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
        if (!inputsAllowed) return;
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
        if (!inputsAllowed) return;
        if (context.started && airDashAvailable)
        {
            airDashButtonDown = true;
        }
        if (context.canceled)
        {
            airDashButtonDown = false;
        }
    }

    public void RollLeft(InputAction.CallbackContext context)
    {
        if (!inputsAllowed) return;
        if (context.started)
        {
            rollRightButtonDown = true;
        }
        if (context.canceled)
        {
            rollRightButtonDown = false;
        }
    }

    public void RollRight(InputAction.CallbackContext context)
    {
        if (!inputsAllowed) return;
        if (context.started)
        {
            rollLeftButtonDown = true;
        }
        if (context.canceled)
        {
            rollLeftButtonDown = false;
        }
    }

    public void PauseGame(InputAction.CallbackContext context)
    {
        if (levelCompleted) return;
        if (context.started)
        {
            if (gamePaused) gameEventChannel.Raise(new EventData{eventType=EventType.ResumeGame});
            else gameEventChannel.Raise(new EventData{eventType=EventType.PauseGame});
        }
    }

    public void MoveCamera(InputAction.CallbackContext context)
    {
        if (!inputsAllowed) return;
        if (context.started || context.performed) {
            
            cameraInputVector = context.ReadValue<Vector2>().normalized;   
        }
        if (context.canceled)
        {
            cameraInputVector = Vector2.zero;
        }
        cameraAnchor.ApplyOffset(cameraInputVector);
    }
}


//TODO 
/*
Make prefabs out of all the needed elements including the obsctale blocks and the core level components
Build the levels
Get screenshots of all the levels for the level images

UI:
Create main menu UI stylised and not the basic one
Make the game UI stylised and not the basic one
*/
