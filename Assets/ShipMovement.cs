using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Ship))]
public class ShipMovement : MonoBehaviour
{
    private Ship ship;
    private Rigidbody rb;
    [SerializeField] private Transform helm;

    [Header("Settings")]
    [SerializeField] private float moveForce = 40f;
    [SerializeField] private float turnTorque = 15f;
    [SerializeField] private float maxSpeed = 7f;
    [SerializeField] private float maxCruiseSpeed = 14f;

    private Vector2 moveInput;

    private bool cruiseMode = false;

    private void Awake()
    {
        ship = GetComponent<Ship>();
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        HandleSteering();
        HandleMovement();
        ControlSpeed();
        ApplyLateralDrag();
        Debug.Log(rb.linearVelocity.magnitude);
    }

    private void HandleMovement()
    {
        if (!cruiseMode)
        {
            float multiplier = moveInput.y >= 0 ? 1f : 0.25f;
            rb.AddForce(transform.forward * moveForce * moveInput.y * multiplier, ForceMode.Force);
        }
        else
        {
            rb.AddForce(transform.forward * moveForce, ForceMode.Force);
        }
    }

    private void HandleSteering()
    {
        if (moveInput.x != 0)
        {
            float multiplier = cruiseMode ? 2f : 1f;
            rb.AddTorque(transform.up * turnTorque * moveInput.x * multiplier, ForceMode.Force);
            helm.Rotate(helm.forward, moveInput.x * Time.fixedDeltaTime * 70, Space.World);
        }
    }

    private void ApplyLateralDrag()
    {
        Vector3 lateralVelocity = transform.right * Vector3.Dot(rb.linearVelocity, transform.right);
        float lateralFriction = 0.6f;
        rb.AddForce(-lateralVelocity * lateralFriction, ForceMode.Acceleration);
    }

    private void ControlSpeed()
    {
        float currentMaxSpeed = cruiseMode ? maxCruiseSpeed : maxSpeed;
        if (rb.linearVelocity.magnitude > currentMaxSpeed)
        {
            float speedExcess = rb.linearVelocity.magnitude - currentMaxSpeed;
            float resistanceFactor = 5f;
            rb.AddForce(-rb.linearVelocity * speedExcess * resistanceFactor, ForceMode.Acceleration);
        }
    }

    public void SetMoveInput(Vector2 moveInput)
    {
        this.moveInput = moveInput;
    }

    public void ToggleCruise()
    {
        cruiseMode = !cruiseMode;
        if (cruiseMode)
        {
            ship.OpenSails();
        }
        else
        {
            ship.CloseSails();
        }
    }
}
