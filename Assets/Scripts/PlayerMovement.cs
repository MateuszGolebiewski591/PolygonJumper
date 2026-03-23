using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private PolygonCollider2D collider;

    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Movement Parameters")]
    private Vector2 horizontalVector = Vector2.zero;
    private Vector2 verticalVector = Vector2.zero;
    [SerializeField] float groundMovementSpeed = 7.5f;
    [SerializeField] float airMovementSpeed = 10f;
    private float movementSpeed = 0f;
    [SerializeField] private Vector2 gravity = -Vector2.up;
    [SerializeField] float gravityForce = 9.81f; 
    [SerializeField] float terminalVelocity = 10f;
    private bool jumping = false;
    private bool jumpButtonDown = false;
    private bool jumpAvalailable = false;
    private float jumpTimeElapsed = 0f;
    [SerializeField] float maxJumpTime = 2f;
    [SerializeField] float jumpScalar = 100f;
    private Coroutine jumpTimer;
    [SerializeField] private AnimationCurve jumpCurve;
    [SerializeField] private float groundCheckDistance = 0.1f;


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        collider = GetComponent<PolygonCollider2D>();
        movementSpeed = groundMovementSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOnSurface()) gravity = Vector2.down;
        if (jumping && jumpButtonDown) verticalVector = Vector2.up * jumpScalar * jumpCurve.Evaluate(jumpTimeElapsed);
        else if (!IsGrounded()) verticalVector += gravity * gravityForce;
        else verticalVector = Vector2.zero;
        if (verticalVector.y < -terminalVelocity) verticalVector = Vector2.ClampMagnitude(verticalVector, terminalVelocity);
        rb.linearVelocity = verticalVector + horizontalVector * movementSpeed;
        if (IsGrounded()) {
            jumpAvalailable = true;
            movementSpeed = groundMovementSpeed;
        }
        else {
            jumpAvalailable = false;
            movementSpeed = airMovementSpeed;
        }
        
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

            RaycastHit2D raycastHit = Physics2D.Raycast(midPoint, normal, groundCheckDistance, groundLayer);
            if (raycastHit)
            {
                if (raycastHit.distance < closestSide)
                {
                    foundSurface = true;
                    closestSide = raycastHit.distance;
                    gravity = normal;
                }
            }
        }
        return foundSurface;
    }

    private bool IsGrounded()
    {
        RaycastHit2D rayCastHit = Physics2D.BoxCast(collider.bounds.center, collider.bounds.size, 0, Vector2.down, 0.1f, groundLayer); 
        return rayCastHit.collider != null; 
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.started && jumpAvalailable)
        {
            jumpButtonDown = true;
            jumpAvalailable = false;
            jumpTimer = StartCoroutine(JumpTimer());
        }
        if (context.canceled)
        {
            jumpButtonDown = false;
        }
    }

    private IEnumerator JumpTimer()
    {
        jumping = true;
        jumpTimeElapsed = 0f;
        while (jumpTimeElapsed < maxJumpTime/2)
        {
            jumpTimeElapsed += Time.deltaTime;
            yield return null;
        }
        if (!jumpButtonDown) jumping = false;
        while (jumpTimeElapsed < maxJumpTime && jumping)
        {
            jumpTimeElapsed += Time.deltaTime;
            yield return null;
        }
        jumping = false;
        jumpTimeElapsed = 0f;
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (context.started || context.performed) {
            float value = context.ReadValue<float>();
            horizontalVector = value * Vector2.right;
        }
        if (context.canceled)
        {
            horizontalVector = Vector2.zero;
        }
    }
}
