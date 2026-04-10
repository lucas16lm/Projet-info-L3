using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovementController : MonoBehaviour
{
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    private Rigidbody rb;
    public float baseMaxSpeed = 2.5f;
    public float sprintMaxSpeed = 6f;

    public float baseMoveForce = 20f;
    public float sprintMoveForce = 30f;

    public float jumpForce = 1;
    public float rotationSpeed = 1;
    
    private Vector2 moveInput;

    private bool isSprinting = false;

    [SerializeField] private Transform raycastSource;
    [SerializeField] private float distanceToGround;


    private void Awake()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        sprintAction = InputSystem.actions.FindAction("Sprint");
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        moveAction.performed += ReadInput;
        moveAction.canceled += ResetInput;
        jumpAction.performed += OnJump;
        sprintAction.performed += StartSprint;
        sprintAction.canceled += CancelSprint;
    }

    private void OnDisable()
    {
        moveAction.performed -= ReadInput;
        moveAction.canceled -= ResetInput;
        jumpAction.performed -= OnJump;
        sprintAction.performed -= StartSprint;
        sprintAction.canceled -= CancelSprint;
    }

    private void FixedUpdate()
    {
        CalculateHorizontalMovement();
        ControlSpeed();
        LookAtMovement();
    }

    private void CalculateHorizontalMovement()
    {
        if (IsGrounded() && moveInput != Vector2.zero)
        {
            if(Physics.Raycast(raycastSource.position, Vector3.down, out RaycastHit hit, distanceToGround))
            {
                Vector3 camRight = Camera.main.transform.right;
                camRight.y = 0;
                camRight = Vector3.ProjectOnPlane(camRight, hit.normal);
                camRight.Normalize();


                Vector3 camForward = Camera.main.transform.forward;
                camForward.y = 0;
                camForward.Normalize();
                camForward = Vector3.ProjectOnPlane(camForward, hit.normal);
                camForward.Normalize();

                rb.AddForce((camForward * moveInput.y + camRight * moveInput.x) * baseMoveForce, ForceMode.Acceleration);
            }
        }
    }

    private void ControlSpeed()
    {
        Vector3 horizontalVelocity = rb.linearVelocity;
        horizontalVelocity.y = 0;

        if(horizontalVelocity.magnitude > baseMaxSpeed)
        {
            float verticalVelocity = rb.linearVelocity.y;
            rb.linearVelocity = horizontalVelocity.normalized * baseMaxSpeed + verticalVelocity * Vector3.up;
        }
    }

    private void LookAtMovement()
    {
        if (IsGrounded() && moveInput != Vector2.zero)
        {
            float targetAngle = Mathf.Atan2(rb.linearVelocity.x, rb.linearVelocity.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, targetAngle, 0f), rotationSpeed * Time.fixedDeltaTime);
        }
    }


    private bool IsGrounded()
    {
        if(Physics.Raycast(raycastSource.position, Vector3.down, out RaycastHit hit, distanceToGround))
        {
            return true;
        }
        return false;
    }

    

    private void OnJump(InputAction.CallbackContext context)
    {
        if (IsGrounded())
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }
    }

    private void ReadInput(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void ResetInput(InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;
    }

    private void StartSprint(InputAction.CallbackContext context)
    {
        isSprinting = true;
    }

    private void CancelSprint(InputAction.CallbackContext context)
    {
        isSprinting = false;
    }
}
