using System;
using Unity.VisualScripting;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public class PlayerMovementController : MonoBehaviour
{
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;


    public CharacterController characterController;

    public float baseSpeed = 2.5f;
    public float sprintSpeed = 6f;

    public float gravity = -9.81f;
    public float jumpForce = 1;
    
    private Vector2 moveInput;

    private Vector3 velocity;
    private bool isSprinting = false;
    public float smoothness;

    private bool isOnShip = false;
    private Vector3 lastShipPosition;
    private Quaternion lastShipRotation;

    [SerializeField] private Transform raycastSource;
    [SerializeField] private float distanceToGround;


    private void Awake()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        sprintAction = InputSystem.actions.FindAction("Sprint");
    }

    private void OnEnable()
    {
        moveAction.performed += ReadInput;
        moveAction.canceled += ResetInput;
        jumpAction.performed += OnJump;
        sprintAction.performed += StartSprint;
        sprintAction.canceled += CancelSprint;
        characterController.enabled = true;
    }

    private void OnDisable()
    {
        moveAction.performed -= ReadInput;
        moveAction.canceled -= ResetInput;
        jumpAction.performed -= OnJump;
        sprintAction.performed -= StartSprint;
        sprintAction.canceled -= CancelSprint;
        characterController.enabled = false;

        isOnShip = false;
    }

    private void LateUpdate()
    {
        CalculateGravity();
        CalculateHorizontalMovement();
        LookAtMovement();

        characterController.Move(velocity * Time.deltaTime + ComputeDeltaShip());
    }

    private void CalculateGravity()
    {
        if (characterController.isGrounded && velocity.y <= 0)
        {
          
            if (isOnShip)
            {
                velocity.y = -4f;
            }
            else
            {
                velocity.y = -2f;
            }
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }
    }

    private void CalculateHorizontalMovement()
    {
        if (characterController.isGrounded)
        {
            Vector3 camRight = Camera.main.transform.right;
            camRight.y = 0;
            camRight.Normalize();

            Vector3 camForward = Camera.main.transform.forward;
            camForward.y = 0;
            camForward.Normalize();

            float speed = isSprinting ? sprintSpeed : baseSpeed;
            Vector3 targetDirection = (camRight * moveInput.x + camForward * moveInput.y) * speed;
            
            velocity.x = Mathf.Lerp(velocity.x, targetDirection.x, smoothness * Time.deltaTime);
            velocity.z = Mathf.Lerp(velocity.z, targetDirection.z, smoothness * Time.deltaTime);
        }
        else
        {
            velocity.x = Mathf.Lerp(velocity.x, 0, Time.deltaTime);
            velocity.z = Mathf.Lerp(velocity.z, 0, Time.deltaTime);
        }
    }

    private void LookAtMovement()
    {
        if (characterController.isGrounded && moveInput != Vector2.zero)
        {
            float targetAngle = Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, targetAngle, 0f), smoothness * Time.deltaTime);
        }
    }

    private Vector3 ComputeDeltaShip()
    {
        if (Physics.Raycast(transform.position + characterController.center, Vector3.down, out RaycastHit hit, characterController.center.y + 0.5f))
        {
            if (hit.transform.CompareTag("Ship"))
            {
                if (!isOnShip)
                {
                    isOnShip = true;
                    lastShipPosition = hit.transform.position;
                    lastShipRotation = hit.transform.rotation;
                }
                else
                {
                    Quaternion deltaRotation = hit.transform.rotation * Quaternion.Inverse(lastShipRotation);
                    Vector3 playerOffset = transform.position - lastShipPosition;
                    Vector3 targetPosition = hit.transform.position + (deltaRotation * playerOffset);

                    Vector3 deltaMove = targetPosition - transform.position;

                    transform.Rotate(0, deltaRotation.eulerAngles.y, 0, Space.World);

                    lastShipPosition = hit.transform.position;
                    lastShipRotation = hit.transform.rotation;

                    return deltaMove;
                }
            }
            else
            {
                isOnShip = false;
            }
            
        }
        else
        {
            isOnShip = false;
        }
        return Vector3.zero;
    }

    public bool IsGrounded()
    {
        return characterController.isGrounded;
    }

    public bool IsMoving()
    {
        return characterController.isGrounded && moveInput != Vector2.zero;
    }

    public float GetHorizontalMoveSpeed()
    {
        return Mathf.Sqrt(velocity.x * velocity.x + velocity.z * velocity.z);
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (characterController.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
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
