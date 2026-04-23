using UnityEngine;

public class CharacterAnimationManager : MonoBehaviour
{
    private KinematicController controller;
    private Animator animator;

    private float runSpeed = 2.5f;
    private float sprintSpeed = 6f;

    private void Awake()
    {
        controller = GetComponent<KinematicController>();
        animator = GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        animator.SetBool("IsGrounded", controller.isGrounded);
        animator.SetBool("MovementInputHeld", controller.currentHorizontalVelocity.magnitude > 0.1f);
        animator.SetFloat("MoveSpeed", 4);
    }


}
