using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Ship : MonoBehaviour
{
    private InputAction moveAction;
    private Rigidbody rb;

    public float moveForce = 40f;
    public float maxSpeed = 7f;
    public float turnTorque = 15f;


    private float currentSpeed = 0f;
    private Vector2 moveInput;
    public List<Buoy> buoyes;

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
        foreach (Buoy buoy in buoyes) buoy.Move();
    }

    private void HandleMovement()
    {
        if (moveInput.y > 0)
        {
            rb.AddForce(transform.forward * moveForce * moveInput.y, ForceMode.Force);

            if (rb.velocity.magnitude > maxSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
            }
        }

    }

    private void HandleSteering()
    {
        if (moveInput.x != 0)
        {
            rb.AddTorque(transform.up * turnTorque * moveInput.x, ForceMode.Force);
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
