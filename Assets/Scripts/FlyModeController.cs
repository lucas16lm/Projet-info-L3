using UnityEngine;
using UnityEngine.InputSystem;

public class FlyModeController : MonoBehaviour
{
    private InputAction moveAction;
    private Transform camTransform;
    private Vector2 moveInput;
    public float moveSpeed = 1f;

    private void Awake()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        camTransform = Camera.main.transform;
    }

    private void FixedUpdate()
    {
        Vector3 camForward = camTransform.forward;
        camForward.Normalize();
        Vector3 camRight = camTransform.right;
        camRight.Normalize();

        Vector3 targetVelocity = (camRight * moveInput.x + camForward * moveInput.y) * moveSpeed;

        transform.position += targetVelocity * Time.fixedDeltaTime;
    }

    private void OnEnable()
    {
        moveAction.performed += ReadMoveInput;
        moveAction.canceled += ResetMoveInput;
    }

    private void OnDisable()
    {
        moveAction.performed -= ReadMoveInput;
        moveAction.canceled -= ResetMoveInput;
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
