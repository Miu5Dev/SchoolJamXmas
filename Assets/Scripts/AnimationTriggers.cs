using System;
using UnityEngine;

public class AnimationTriggers : MonoBehaviour
{
    Animator animator;
    private float speed;
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<OnPlayerMoveEvent>(Moving);
        EventBus.Subscribe<OnPlayerSlideStateEvent>(OnSlideState);
        EventBus.Subscribe<OnPlayerGroundedEvent>(Land);
        EventBus.Subscribe<OnExecuteJumpCommand>(jump);
        EventBus.Subscribe<OnPlayerGroundPoundEvent>(groundpound);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<OnPlayerMoveEvent>(Moving);
        EventBus.Unsubscribe<OnPlayerSlideStateEvent>(OnSlideState);
        EventBus.Unsubscribe<OnPlayerGroundedEvent>(Land);
        EventBus.Unsubscribe<OnExecuteJumpCommand>(jump);
        EventBus.Unsubscribe<OnPlayerGroundPoundEvent>(groundpound);
    }

    private void Moving(OnPlayerMoveEvent ev)
    {

        animator.SetFloat("Direction", ev.rotationState);

        if (ev.speed <= 0.5)
        {
            animator.SetBool("moving", false);
            speed = ev.speed;
        }
        else{
            animator.SetBool("moving", true);
        }

        if (ev.isCrouching)
        {
            animator.SetBool("crouch", true);
        }
        else
        {
            animator.SetBool("crouch", false);
        }
    }

    private void OnSlideState(OnPlayerSlideStateEvent ev)
    {
        animator.SetBool("landed", false);
        animator.SetBool("sliding", true);
    }
    private void jump(OnExecuteJumpCommand ev)
    {
        animator.SetBool("landed", false);
        animator.SetTrigger("jump");
        if(ev.JumpType.jumpType == JumpType.Dive) animator.SetTrigger("dive");
        if(ev.JumpType.jumpType == JumpType.LongJump) animator.SetTrigger("longjump");
    }
    private void Land(OnPlayerGroundedEvent ev)
    {
        animator.SetBool("landed", true);
    }
    private void groundpound(OnPlayerGroundPoundEvent ev)
    {
        animator.SetTrigger("groundpound");
    }
    
}
