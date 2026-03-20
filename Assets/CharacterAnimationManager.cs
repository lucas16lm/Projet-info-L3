using UnityEngine;

public class CharacterAnimationManager : MonoBehaviour
{
    private PlayerController controller;
    private Animator animator;

    private float runSpeed = 2.5f;
    private float sprintSpeed = 6f;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        animator.SetBool("IsGrounded", controller.IsGrounded());
        animator.SetBool("MovementInputHeld", controller.IsMoving());
        animator.SetFloat("MoveSpeed", GetAnimationMoveSpeed());
    }

    private float GetAnimationMoveSpeed()
    {
        return controller.GetMoveState() switch
        {
            PlayerController.MoveState.Idle => 0,
            PlayerController.MoveState.Run => runSpeed,
            PlayerController.MoveState.Sprint => sprintSpeed,
            _ => 0f
        };
    }

}
