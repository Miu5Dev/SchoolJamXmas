using UnityEngine;

public class ActiveButton : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private bool isTriggered = false;

    private void OnEnable()
    {
        EventBus.Subscribe<OnPlayerGroundPoundEvent>(groundpound);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<OnPlayerGroundPoundEvent>(groundpound);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the player enters the trigger
        if (other.CompareTag("Player") && isTriggered)
        {
            animator.SetTrigger("buttonOn");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && isTriggered)
        {
            animator.SetTrigger("buttonOn");
        }
    }

    private void groundpound(OnPlayerGroundPoundEvent ev)
    {
            isTriggered = true;
    }
}