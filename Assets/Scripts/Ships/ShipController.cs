using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Ship))]
public class ShipController : MonoBehaviour
{
    private Ship ship;
    private ShipMovement mover;
    private Transform pilot;

    private InputAction moveAction;
    private InputAction exitAction;
    private InputAction cruiseAction;
    private InputAction attackAction;


    private Vector2 moveInput;

    private void OnEnable()
    {
        moveAction.performed += ReadInput;
        moveAction.canceled += ResetInput;
        exitAction.performed += ExitShip;
        cruiseAction.performed += ToggleCruise;
        attackAction.performed += Attack;
    }

    private void OnDisable()
    {
        moveInput = Vector2.zero;
        mover.SetMoveInput(moveInput);

        moveAction.performed -= ReadInput;
        moveAction.canceled -= ResetInput;
        exitAction.performed -= ExitShip;
        cruiseAction.performed -= ToggleCruise;
        attackAction.performed -= Attack;
    }

    private void Awake()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        exitAction = InputSystem.actions.FindAction("Interact");
        cruiseAction = InputSystem.actions.FindAction("Cruise");
        attackAction = InputSystem.actions.FindAction("Attack");

        ship = GetComponent<Ship>();
        mover = GetComponent<ShipMovement>();
    }

    private void FixedUpdate()
    {
        mover.SetMoveInput(moveInput);
    }

    

    public void EnterShip(Transform pilot)
    {
        this.pilot = pilot;
        pilot.GetComponent<KinematicController>().isPiloting = true;
        enabled = true;
        ship.SetCruiseView();
    }

    private void ExitShip(InputAction.CallbackContext context)
    {

        pilot.GetComponent<KinematicController>().isPiloting = false;

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

    private void ToggleCruise(InputAction.CallbackContext context)
    {
        mover.ToggleCruise();
    }

    private void Attack(InputAction.CallbackContext context)
    {
        Vector3 camForward = Camera.main.transform.forward;

        if(Vector3.Dot(camForward, transform.right) > 0)
        {
            GetComponent<CannonsManager>().ShootRight();
        }
        else
        {
            GetComponent<CannonsManager>().ShootLeft();
        }
    }
}
