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
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<OnPlayerMoveEvent>(Moving);
        EventBus.Subscribe<OnPlayerSlideStateEvent>(OnSlideState);
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
        
    }

    private void OnSlideState(OnPlayerSlideStateEvent ev)
    {
        animator.SetBool("sliding", true);
    }
    
}
