using System;
using UnityEngine;

public class DialogueActivator : MonoBehaviour, Interactable
{
    [SerializeField] private DialogueObject dialogueObject;
    public GameObject interactionIcon;

    private PlayerController playerInRange;
    private DialogueUI dialogueUI;

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
        if (ev.pressed && playerInRange != null && dialogueUI != null)
        {
            Interact(playerInRange);
        }
    }

    public void UpdateDialogueObject(DialogueObject dialogueObject)
    {
        this.dialogueObject = dialogueObject;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent(out PlayerController player))
        {
            playerInRange = player;
            dialogueUI = player.DialogueUI;
            interactionIcon.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent(out PlayerController player))
        {
            if (playerInRange == player)
            {
                playerInRange = null;
                dialogueUI = null;
                interactionIcon.SetActive(false);
            }
        }
    }

    public void Interact(PlayerController player)
    {
        if (dialogueUI == null) return;

        foreach (DialogueResponseEvents responseEvents in GetComponents<DialogueResponseEvents>())
        {
            if (responseEvents.DialogueObject == dialogueObject)
            {
                dialogueUI.AddResponseEvents(responseEvents.Events);
                break;
            }
        }
        interactionIcon.SetActive(false);
        dialogueUI.showDialogue(dialogueObject);

        // Start a routine to wait for the dialogue to finish
        StartCoroutine(WaitUntilDialogueClosed(dialogueUI));
    }

        private System.Collections.IEnumerator WaitUntilDialogueClosed(DialogueUI dialogueUI)
        {
            // Wait while the dialogue is open
            yield return new WaitUntil(() => dialogueUI.IsOpen == false);
            
            // Turn the icon back on!
            interactionIcon.SetActive(true);
        }
    }
