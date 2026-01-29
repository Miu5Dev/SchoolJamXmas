using UnityEngine;

public class AnimationTriggers : MonoBehaviour
{
    Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<OnMoveInputEvent>(Moving);
        EventBus.Subscribe<OnPlayerStopEvent>(OnPlayerStop);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<OnMoveInputEvent>(Moving);
        EventBus.Unsubscribe<OnPlayerStopEvent>(OnPlayerStop);
    }

    private void Moving(OnMoveInputEvent ev)
    {
        animator.SetBool("moving", true);
    }

    private void OnPlayerStop(OnPlayerStopEvent ev)
    {
        animator.SetBool("moving", false);
    }
}
