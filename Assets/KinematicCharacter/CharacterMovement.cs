using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(CharacterInputHandler))]
public class CharacterMovement : MonoBehaviour
{
    private Rigidbody rb;
    private CharacterInputHandler controller;
    [SerializeField] private float baseSpeed = 1;
    [SerializeField] private float acceleration = 1;

    [SerializeField] private Transform raycastOrigin;
    [SerializeField] private float raycastLength = 1;
    [SerializeField] private LayerMask layerMask;

    private Vector3 horizontalVelocity;
    private Vector3 verticalVelocity;
    private bool isGrounded;
    private RaycastHit groundHit;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        controller = GetComponent<CharacterInputHandler>();
    }

    

    private void FixedUpdate()
    {
        CheckGround();
        HandleGravity();
        HandleHorizontalMovement();

        
        Vector3 direction = horizontalVelocity * baseSpeed + verticalVelocity;

        

        rb.MovePosition(rb.position + direction * Time.fixedDeltaTime);
    }

    private void HandleHorizontalMovement()
    {
        if (isGrounded)
        {
            Vector3 projected = Vector3.ProjectOnPlane(controller.GetDirection(), groundHit.normal).normalized;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, projected, acceleration * Time.fixedDeltaTime);
        }
        else
        {
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, acceleration * Time.fixedDeltaTime);
        }
    }

    private void HandleGravity()
    {
        if (!isGrounded)
        {
            verticalVelocity += Physics.gravity * Time.fixedDeltaTime;
        }
        else
        {
            verticalVelocity = Vector3.zero;
        }
    }

    private void CheckGround()
    {
        
        if (Physics.SphereCast(raycastOrigin.position, 0.2f, Vector3.down, out RaycastHit hit, raycastLength, layerMask))
        {
            GetComponent<CapsuleCollider>().bounds.ex
            isGrounded = true;
            groundHit = hit;
        }
        else
        {
            isGrounded = false;
        }
        
    }

}
