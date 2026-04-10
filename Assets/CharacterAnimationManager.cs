using UnityEngine;

public class CharacterAnimationManager : MonoBehaviour
{
    private PlayerMovementController controller;
    private Animator animator;

    private float runSpeed = 2.5f;
    private float sprintSpeed = 6f;

    private void Awake()
    {
        controller = GetComponent<PlayerMovementController>();
        animator = GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        //animator.SetBool("IsGrounded", controller.IsGrounded());
        //animator.SetBool("MovementInputHeld", controller.IsMoving());
        //animator.SetFloat("MoveSpeed", controller.GetHorizontalMoveSpeed());
    }


}
