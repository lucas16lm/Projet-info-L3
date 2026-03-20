using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction attackAction;
    private InputAction interactAction;

    public CharacterController characterController;

    public float baseSpeed = 2.5f;
    public float sprintSpeed = 6f;

    public float gravity = -9.81f;
    public float jumpForce = 1;
    
    private Vector2 moveInput;

    private Vector3 horizontalVelocity;
    private Vector3 verticalVelocity;
    private bool isSprinting = false;

    private bool isOnShip = false;
    private Vector3 lastShipPosition;
    private Quaternion lastShipRotation;

    [SerializeField] private Transform raycastSource;
    [SerializeField] private float distanceToGround;


    private void Awake()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        moveAction.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        moveAction.canceled += ctx => moveInput = Vector2.zero;

        jumpAction = InputSystem.actions.FindAction("Jump");
        jumpAction.performed += OnJump;

        sprintAction = InputSystem.actions.FindAction("Sprint");
        sprintAction.performed += ctx => isSprinting = true;
        sprintAction.canceled += ctx => isSprinting = false;

        attackAction = InputSystem.actions.FindAction("Attack");
        attackAction.performed += OnAttack;

        interactAction = InputSystem.actions.FindAction("Interact");
        interactAction.performed += OnInteraction;
    }

    private void FixedUpdate()
    {
        if (characterController.isGrounded)
        {
            horizontalVelocity = Camera.main.transform.right * moveInput.x + Camera.main.transform.forward * moveInput.y;
            
            if (verticalVelocity.y <= 0f)
            {
                verticalVelocity = Vector3.down * 3f;
            }

        }
        else
        {
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, Vector3.zero, Time.deltaTime);
            verticalVelocity.y += gravity * Time.deltaTime;
        }

        


        if (moveInput != Vector2.zero)
        {
            float targetAngle = Mathf.Atan2(horizontalVelocity.x, horizontalVelocity.z) * Mathf.Rad2Deg;

            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, targetAngle, 0f), 10 * Time.deltaTime);
        }

        Vector3 deltaShip = Vector3.zero;
        Quaternion deltaRotation = Quaternion.identity;

        if (Physics.Raycast(raycastSource.position, Vector3.down, out RaycastHit hit, 2.1f))
        {
            if (hit.transform.CompareTag("Ship"))
            {
                deltaRotation = hit.transform.rotation * Quaternion.Inverse(lastShipRotation);

                Vector3 newPlayerPosition = hit.transform.position + deltaRotation * (transform.position - lastShipPosition);

   
                if (isOnShip)
                {
                    deltaShip = newPlayerPosition - transform.position;

   
                    transform.rotation = deltaRotation * transform.rotation;
                }

                isOnShip = true;
                lastShipPosition = hit.transform.position;
                lastShipRotation = hit.transform.rotation;
            }
            else
            {
                isOnShip = false;
            }
        }

        float speed = isSprinting ? sprintSpeed : baseSpeed;
        Vector3 velocity = horizontalVelocity * speed + verticalVelocity;
        characterController.Move(velocity * Time.deltaTime + deltaShip);
    }

    public bool IsGrounded()
    {
        return characterController.isGrounded;
    }

    public bool IsMoving()
    {
        return characterController.isGrounded && moveInput != Vector2.zero;
    }

    public MoveState GetMoveState()
    {
        if (!IsMoving()) return MoveState.Idle;
        return isSprinting ? MoveState.Sprint : MoveState.Run;
    }



    private void OnJump(InputAction.CallbackContext context)
    {
        if (characterController.isGrounded)
        {
            verticalVelocity = Vector3.up * Mathf.Sqrt(jumpForce * -2f * gravity);
        }
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        GetComponent<Animator>().SetTrigger("Attack");
    }

    private void OnInteraction(InputAction.CallbackContext context)
    {
        Debug.Log("Interact pressed");
    }

    public enum MoveState
    {
        Idle,
        Run,
        Sprint
    }
}
