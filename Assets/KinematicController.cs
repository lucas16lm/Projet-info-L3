using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class KinematicController : MonoBehaviour
{
    [Header("Move settings")]
    public float moveSpeed = 5f;
    public float accelerationSmooth = 0.1f;

    [Header("Gravity and slope settings")]
    public float gravity = -15f;
    public float groundCheckDistance = 0.1f;
    public float maxSlopeAngle = 45f;

    [Header("Jump settings")]
    public float jumpForce = 8f;

    [Header("Rotation settings")]
    public float rotationSpeed = 15f;

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
    public Vector3 currentHorizontalVelocity;
    private Vector3 externalVelocity;
    public bool isGrounded;
    public bool isPiloting = false;

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
        Vector3 currentPos = rb.position;
        ResolveOverlaps(ref currentPos);
        CheckGrounded();

        Vector3 camForward = camTransform.forward;
        camForward.y = 0;
        camForward.Normalize();
        Vector3 camRight = camTransform.right;
        camRight.y = 0;
        camRight.Normalize();

        Vector3 targetHorizontalVelocity = (camRight * moveInput.x + camForward * moveInput.y) * moveSpeed;
        if (!isGrounded || isPiloting) targetHorizontalVelocity = Vector3.zero;

        float smooth = isGrounded ? accelerationSmooth : accelerationSmooth/10;
        currentHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity, targetHorizontalVelocity, smooth * Time.fixedDeltaTime);


        if (currentHorizontalVelocity.magnitude > 0.001f)
        {
            Vector3 horizontalMovement = CollideAndSlide(currentHorizontalVelocity * Time.fixedDeltaTime, currentPos, 0);
            currentPos += horizontalMovement;

            Vector3 lookDirection = new Vector3(currentHorizontalVelocity.x, 0, currentHorizontalVelocity.z);
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                Quaternion newRotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
                rb.MoveRotation(newRotation);
            }
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

        if (externalVelocity.magnitude > 0)
        {
            currentPos += externalVelocity * Time.fixedDeltaTime;
        }

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

            float angle = Vector3.Angle(Vector3.up, hit.normal);
            if (angle > maxSlopeAngle && leftoverVelocity.y >= 0)
            {
                leftoverVelocity.y = 0;
            }
            else if (angle <= maxSlopeAngle && leftoverVelocity.y < 0)
            {
                leftoverVelocity = Vector3.zero;
            }

            Vector3 projectedVelocity = Vector3.ProjectOnPlane(leftoverVelocity, hit.normal);

            return safeMovement + CollideAndSlide(projectedVelocity, currentPosition + safeMovement, depth + 1);
        }

        return velocity;
    }

    private void CheckGrounded()
    {
        float radius = collider.radius * 0.9f;
        float castOffset = radius;
        Vector3 origin = rb.position + collider.center + Vector3.down * ((collider.height / 2f) - radius - castOffset);

        if(Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit, groundCheckDistance + castOffset, collisionMask))
        {
            isGrounded = true;

            if (hit.rigidbody != null && !hit.rigidbody.isKinematic)
            {
                externalVelocity = hit.rigidbody.GetPointVelocity(hit.point);
            }
            else
            {
                externalVelocity = Vector3.zero;
            }
        }
        else
        {
            isGrounded = false;
            externalVelocity = Vector3.zero;
        }
    }

    private void ResolveOverlaps(ref Vector3 currentPos)
    {
        float radius = collider.radius;
        float heightOffset = (collider.height / 2f) - radius;
        Vector3 point1 = currentPos + collider.center + Vector3.up * heightOffset;
        Vector3 point2 = currentPos + collider.center - Vector3.up * heightOffset;

        Collider[] overlaps = Physics.OverlapCapsule(point1, point2, radius, collisionMask);

        foreach (Collider overlap in overlaps)
        {
            if (overlap == collider) continue; 

            if (Physics.ComputePenetration(
                    collider, currentPos, rb.rotation,
                    overlap, overlap.transform.position, overlap.transform.rotation,
                    out Vector3 pushDirection, out float pushDistance))
            {

                currentPos += pushDirection * (pushDistance + skinWidth);
            }
        }
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
