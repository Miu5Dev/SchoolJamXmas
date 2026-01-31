using System;
using System.Collections;
using UnityEngine;

public class AnimationTriggers : MonoBehaviour
{
    Animator animator;
    private float speed;
    private Coroutine procedeCoroutine;
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<OnPlayerMoveEvent>(Moving);
        EventBus.Subscribe<OnPlayerSlideStateEvent>(sliding);
        EventBus.Subscribe<OnPlayerGroundedEvent>(Land);
        EventBus.Subscribe<OnPlayerAirborneEvent>(Airborne);
        EventBus.Subscribe<OnExecuteJumpCommand>(jump);
        EventBus.Subscribe<OnPlayerGroundPoundEvent>(groundpound);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<OnPlayerMoveEvent>(Moving);
        EventBus.Unsubscribe<OnPlayerSlideStateEvent>(sliding);
        EventBus.Unsubscribe<OnPlayerGroundedEvent>(Land);
        EventBus.Unsubscribe<OnPlayerAirborneEvent>(Airborne);
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
        if (ev.JumpType.jumpType == JumpType.Double)
        {
            animator.SetTrigger("double");
        }

        if (ev.JumpType.jumpType == JumpType.Triple)
        {
            animator.SetTrigger("triple");
        }
        if (ev.JumpType.jumpType == JumpType.Backflip)
        {
            animator.SetTrigger("backflip");
            animator.SetBool("backflipState", true);

        }
    }
    private void Land(OnPlayerGroundedEvent ev)
    {
        animator.SetBool("landed", true);
        animator.SetBool("backflipState", false);
        animator.SetBool("procede", true);
    }

    private void Airborne(OnPlayerAirborneEvent ev)
    {
        animator.SetBool("procede", false);
    }
    private void groundpound(OnPlayerGroundPoundEvent ev)
    {
        animator.SetTrigger("groundpound");
    }

    private void sliding(OnPlayerSlideStateEvent ev)
    {
        animator.SetBool("sliding", ev.IsSliding);

        if (ev.IsSliding)
        {
            // Check slide direction (left is negative x, right is positive x)
            float slideX = ev.SlideDirection.x;

            if (slideX < -0.1f)
            {
                animator.SetBool("slideLeft", true);
                animator.SetBool("slideRight", false);
            }
            else if (slideX > 0.1f)
            {
                animator.SetBool("slideLeft", false);
                animator.SetBool("slideRight", true);
            }
            else
            {
                animator.SetBool("slideLeft", false);
                animator.SetBool("slideRight", false);
            }
        }
        else
        {
            animator.SetBool("slideLeft", false);
            animator.SetBool("slideRight", false);
        }
    }

    private IEnumerator SetProcedeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        animator.SetBool("procede", true);
    }
}
