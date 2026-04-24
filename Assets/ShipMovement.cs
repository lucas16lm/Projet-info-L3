using StylizedWater3;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Ship))]
public class ShipMovement : MonoBehaviour
{

    [Header("Buoyancy")]
    [SerializeField] private HeightQuerySystem.Interface waterInterface = new HeightQuerySystem.Interface();
    [SerializeField] private List<Transform> buoys;
    [SerializeField] private float buoyancyForce = 15f;

    private HeightQuerySystem.Sampler heightSampler;

    [Header("Diverse")]
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

    void OnEnable()
    {
        // 1. Initialiser le sampler avec le nombre exact de points sous la coque
        heightSampler = new HeightQuerySystem.Sampler();
        if (buoys != null && buoys.Count > 0)
        {
            heightSampler.SetSampleCount(buoys.Count);
        }
    }

    void OnDisable()
    {
        // 2. CRUCIAL : Libérer la mémoire quand le bateau est détruit/désactivé
        // (Comme on l'a vu dans le code de Staggart, sinon cela crée des fuites de mémoire)
        if (heightSampler != null)
        {
            heightSampler.Dispose();
            heightSampler = null;
        }
    }

    private void Awake()
    {
        ship = GetComponent<Ship>();
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        HandleBuoyancy();
        HandleSteering();
        HandleMovement();
        ControlSpeed();
        ApplyLateralDrag();
        //Debug.Log(rb.linearVelocity.magnitude);
    }

    private void HandleBuoyancy()
    {
        if (buoys == null || buoys.Count == 0) return;
 
        waterInterface.GetWaterObject(transform.position);
        if (waterInterface.HasMissingReferences()) return;


        for (int i = 0; i < buoys.Count; i++)
        {
            heightSampler.SetSamplePosition(i, buoys[i].position);
        }

        Gerstner.ComputeHeight(heightSampler, waterInterface);

        for (int i = 0; i < buoys.Count; i++)
        {
            float waterLevelY = heightSampler.heightValues[i];
            float diff = waterLevelY - buoys[i].position.y;

            if (diff > 0)
            {
                float clampedDepth = Mathf.Clamp(diff, 0f, 1f);
                Vector3 force = Vector3.up * buoyancyForce * clampedDepth;
                rb.AddForceAtPosition(force, buoys[i].position, ForceMode.Force);
            }
        }
    }

    private void HandleMovement()
    {
        if (!cruiseMode)
        {
            float multiplier = moveInput.y >= 0 ? 1f : 0.25f;
            
            Vector3 forward = transform.forward;
            forward.y *= 0;

            rb.AddForce(forward * moveForce * moveInput.y * multiplier, ForceMode.Force);
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

        Vector3 horizontalVelocity = rb.linearVelocity;
        horizontalVelocity.y = 0;

        if (horizontalVelocity.magnitude > currentMaxSpeed)
        {
            float verticalVelocity = rb.linearVelocity.y;
            rb.linearVelocity = horizontalVelocity.normalized * currentMaxSpeed + verticalVelocity * Vector3.up;
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
