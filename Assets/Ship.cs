using StylizedWater3;
using UnityEngine;
using UnityEngine.InputSystem;

public class Ship : MonoBehaviour
{
    private InputAction moveAction;
    [SerializeField] private AlignToWater aligner;
    private Rigidbody rb;
    

    public float maxSpeed = 7f;
    public float acceleration = 2f;
    public float deceleration = 1f;
    public float turnSpeed = 30f;

    private float currentSpeed = 0f;
    private Vector2 moveInput;

    private void Awake()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        moveAction.performed += ReadInput;
        moveAction.canceled += ResetInput;
    }

    private void OnDisable()
    {
        moveAction.performed -= ReadInput;
        moveAction.canceled -= ResetInput;
    }

    private void FixedUpdate()
    {
        HandleSteering();
        HandleMovement();
    }

    private void HandleMovement()
    {
        if (moveInput.y > 0)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleration * Time.fixedDeltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, deceleration * Time.fixedDeltaTime);
        }

        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 newPosition = rb.position + (forward * currentSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPosition);

    }

    private void HandleSteering()
    {
        if(moveInput.x != 0)
        {
            aligner.rotation += moveInput.x * turnSpeed * Time.fixedDeltaTime;
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
}
