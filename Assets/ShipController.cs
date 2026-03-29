using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Ship))]
public class ShipController : MonoBehaviour
{
    private Ship ship;
    private Transform pilot;
    [SerializeField] private Transform helm;

    private InputAction moveAction;
    private InputAction exitAction;

    public float moveForce = 40f;
    public float turnTorque = 15f;

    private Vector2 moveInput;

    private void OnEnable()
    {
        moveAction.performed += ReadInput;
        moveAction.canceled += ResetInput;
        exitAction.performed += ExitShip;
    }

    private void OnDisable()
    {
        moveAction.performed -= ReadInput;
        moveAction.canceled -= ResetInput;
        exitAction.performed -= ExitShip;
    }

    private void Awake()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        exitAction = InputSystem.actions.FindAction("Interact");
        ship = GetComponent<Ship>();
    }

    private void FixedUpdate()
    {
        HandleSteering();
        HandleMovement();
    }

    private void HandleMovement()
    {
        float multiplier = moveInput.y >= 0 ? 1f : 0.25f;
        ship.AddForce(transform.forward * moveForce * moveInput.y * multiplier);

    }

    private void HandleSteering()
    {
        if (moveInput.x != 0)
        {
            ship.AddTorque(transform.up * turnTorque * moveInput.x);
            helm.Rotate(helm.forward, moveInput.x * Time.fixedDeltaTime * 70, Space.World);
        }
    }

    public void EnterShip(Transform pilot)
    {
        this.pilot = pilot;
        pilot.parent = transform;
        enabled = true;
        ship.SetCruiseView();
    }

    private void ExitShip(InputAction.CallbackContext context)
    {

        pilot.parent = null;
        pilot.GetComponent<PlayerMovementController>().enabled = true;
        
        
        enabled = false;
        pilot = null;
        ship.UnsetCruiseView();
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
