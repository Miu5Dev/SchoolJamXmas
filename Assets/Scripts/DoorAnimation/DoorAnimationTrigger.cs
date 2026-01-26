using UnityEngine;
using System.Collections;

public class DoorAnimationTrigger : MonoBehaviour
{
    public Animator doorAnimator;
    [SerializeField] private bool isPlayerInFrontHitbox = false;
    [SerializeField] private bool isPlayerInBackHitbox = false;
    public GameObject interactionIcon;
    public bool oneTimeUse = false;
    [SerializeField]private bool opened = false;
    public float wait = 0.2f;

    private void OnEnable()
    {
        EventBus.Subscribe<OnActionInputEvent>(doStuff);
    }
    
    private void OnDisable()
    {
        EventBus.Unsubscribe<OnActionInputEvent>(doStuff);
    }
    
    private void doStuff(OnActionInputEvent ev)
    {
        
        if (isPlayerInFrontHitbox)
        {
            StartCoroutine(TriggerDoor(true));
            if (oneTimeUse)
            {
                interactionIcon.SetActive(false);
                opened = true;
            }
        }
        else if (isPlayerInBackHitbox)
        {
            StartCoroutine(TriggerDoor(false));
        }
    }
        

    IEnumerator TriggerDoor(bool isFrontSide)
    {
        doorAnimator.SetBool("FrontSide", isFrontSide);
        doorAnimator.SetTrigger("IsToggled");
        
        yield return new WaitForSeconds(wait);
        
        doorAnimator.ResetTrigger("IsToggled");
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && opened == false)
        {
            isPlayerInFrontHitbox = true;
            interactionIcon.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInFrontHitbox = false;
            interactionIcon.SetActive(false);
        }
    }

    public void SetBackHitbox(bool state)
    {
        isPlayerInBackHitbox = state;
        interactionIcon.SetActive(state);
    }
}