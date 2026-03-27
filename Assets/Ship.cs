using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Ship : MonoBehaviour
{
    private InputAction moveAction;

    private Rigidbody rb;

    public float maxSpeed = 7f;


    private float currentSpeed = 0f;
    private Vector2 moveInput;

    private void Awake()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        rb = GetComponent<Rigidbody>();
    }



    private void FixedUpdate()
    {
        ApplyLateralDrag();
        ControlSpeed();
    }

    private void ApplyLateralDrag()
    {
        Vector3 lateralVelocity = transform.right * Vector3.Dot(rb.linearVelocity, transform.right);
        float lateralFriction = 0.8f;
        rb.AddForce(-lateralVelocity * lateralFriction, ForceMode.Acceleration);
    }

    private void ControlSpeed()
    {
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }


    public void AddForce(Vector3 force)
    {
        rb.AddForce(force, ForceMode.Force);
    }

    public void AddTorque(Vector3 torque)
    {
        rb.AddTorque(torque, ForceMode.Force);
    }
}
