using UnityEngine;
using System.Collections;

public class DoorAnimationTrigger : MonoBehaviour
{
    public Animator doorAnimator;
    private bool isPlayerInFrontHitbox = false;
    private bool isPlayerInBackHitbox = false;
    public GameObject interactionIcon;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (isPlayerInFrontHitbox)
            {
                StartCoroutine(TriggerDoor(true));
            }
            else if (isPlayerInBackHitbox)
            {
                StartCoroutine(TriggerDoor(false));
            }
        }
    }

    IEnumerator TriggerDoor(bool isFrontSide)
    {
        doorAnimator.SetBool("FrontSide", isFrontSide);
        doorAnimator.SetTrigger("IsToggled");
        
        yield return new WaitForSeconds(0.1f);
        
        doorAnimator.ResetTrigger("IsToggled");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
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