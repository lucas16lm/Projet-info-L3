using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class KinematicController : MonoBehaviour
{
    [Header("Move settings")]
    public float moveSpeed = 5f;

    [Header("Gravity settings")]
    public float gravity = -15f;
    public float groundCheckDistance = 0.1f;

    [Header("Jump settings")]
    public float jumpForce = 8f;

    [Header("collide and slide settings")]
    public int maxBounces = 3;
    public float skinWidth = 0.015f;
    public LayerMask collisionMask;


    private InputAction moveAction;
    private InputAction jumpAction;
    private Rigidbody rb;
    private CapsuleCollider collider;
    private Vector2 moveInput;
    private Transform camTransform;

    
    private float currentVerticalVelocity;
    private bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        collider = GetComponent<CapsuleCollider>();
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        camTransform = Camera.main.transform;
    }

    private void FixedUpdate()
    {
        CheckGrounded();
        Vector3 currentPos = rb.position;

        Vector3 camForward = camTransform.forward;
        camForward.y = 0;
        Vector3 camRight = camTransform.right;
        camRight.y = 0;

        Vector3 horizontalVelocity = (camRight * moveInput.x + camForward * moveInput.y) * moveSpeed * Time.fixedDeltaTime;
        if (!isGrounded) horizontalVelocity = Vector3.zero;

        if (horizontalVelocity.magnitude > 0.001f)
        {
            Vector3 horizontalMovement = CollideAndSlide(horizontalVelocity, currentPos, 0);
            currentPos += horizontalMovement;
        }

        if (isGrounded && currentVerticalVelocity <= 0)
        {
            currentVerticalVelocity = -2f;
        }
        else
        {
            currentVerticalVelocity += gravity * Time.fixedDeltaTime;
        }

        Vector3 verticalVelocity = new Vector3(0, currentVerticalVelocity * Time.fixedDeltaTime, 0);
        Vector3 verticalMovement = CollideAndSlide(verticalVelocity, currentPos, 0);

        if (verticalMovement.y == 0 && !isGrounded)
        {
            currentVerticalVelocity = 0;
        }

        currentPos += verticalMovement;

        //TODO : delta ship

        rb.MovePosition(currentPos);

    }

    private Vector3 CollideAndSlide(Vector3 velocity, Vector3 currentPosition, int depth)
    {
        if (depth >= maxBounces || velocity.magnitude < 0.001f)
        {
            return Vector3.zero;
        }

        float radius = collider.radius;
        float heightOffset = (collider.height / 2f) - radius;
        Vector3 point1 = currentPosition + collider.center + Vector3.up * heightOffset;
        Vector3 point2 = currentPosition + collider.center - Vector3.up * heightOffset;

        float distance = velocity.magnitude;
        Vector3 direction = velocity.normalized;

        if (Physics.CapsuleCast(point1, point2, radius, direction, out RaycastHit hit, distance + skinWidth, collisionMask))
        {
            float safeDistance = hit.distance - skinWidth;
            Vector3 safeMovement = direction * safeDistance;
            Vector3 leftoverVelocity = velocity - safeMovement;

            Vector3 projectedVelocity = Vector3.ProjectOnPlane(leftoverVelocity, hit.normal);

            return safeMovement + CollideAndSlide(projectedVelocity, currentPosition + safeMovement, depth + 1);
        }

        return velocity;
    }

    private void CheckGrounded()
    {
        float radius = collider.radius * 0.9f;
        Vector3 origin = rb.position + collider.center + Vector3.down * ((collider.height / 2f) - radius);

        isGrounded = Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit, groundCheckDistance, collisionMask);
    }

    private void OnEnable()
    {
        moveAction.performed += ReadMoveInput;
        moveAction.canceled += ResetMoveInput;
        jumpAction.performed += OnJump;
    }

    private void OnDisable()
    {
        moveAction.performed -= ReadMoveInput;
        moveAction.canceled -= ResetMoveInput;
        jumpAction.performed -= OnJump;
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (isGrounded)
        {
            currentVerticalVelocity = jumpForce;
        }
    }

    private void ReadMoveInput(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void ResetMoveInput(InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;
    }

}
