using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    private Rigidbody rb;
    private Vector3 velocity;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        
    }

    private bool IsGrounded()
    {
        throw new System.NotImplementedException();
    }

    private void ApplyGravity()
    {
        throw new System.NotImplementedException();
    }
}
