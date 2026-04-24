using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterInputHandler : MonoBehaviour
{
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    private Transform camTransform;

    private Vector2 moveInput;

    private void Awake()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        sprintAction = InputSystem.actions.FindAction("Sprint");

        camTransform = Camera.main.transform;
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

    public Vector3 GetDirection()
    {
        Vector3 camForward = camTransform.forward;
        camForward.y = 0;

        Vector3 camRight = camTransform.right;
        camRight.y = 0;

        return (camRight*moveInput.x + camForward*moveInput.y).normalized;
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
