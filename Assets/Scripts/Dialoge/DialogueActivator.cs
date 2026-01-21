using System;
using UnityEngine;

public class DialogueActivator : MonoBehaviour, Interactable
{
    [SerializeField] private DialogueObject dialogueObject;
    public GameObject interactionIcon;

    public void UpdateDialogueObject(DialogueObject dialogueObject)
    {
        this.dialogueObject = dialogueObject;
    }

    private void OnTriggerEnter(Collider other)
    {
        interactionIcon.SetActive(true);
        if (other.CompareTag("Player") && other.TryGetComponent(out PlayerControllerOld player))
        {
            player.interactable = this;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent(out PlayerControllerOld player))
        {
            interactionIcon.SetActive(false);
            if (player.interactable is DialogueActivator dialogueActivator && dialogueActivator == this)
            {
                player.interactable = null;
            }
        }

    }

    public void Interact(PlayerControllerOld player)
    {
        foreach (DialogueResponseEvents responseEvents in GetComponents<DialogueResponseEvents>())
        {
            if (responseEvents.DialogueObject == dialogueObject)
            {
                player.DialogueUI.AddResponseEvents(responseEvents.Events);
                break;
            }
        }
        interactionIcon.SetActive(false);
        player.DialogueUI.showDialogue(dialogueObject);
        
            // Start a routine to wait for the dialogue to finish
            StartCoroutine(WaitUntilDialogueClosed(player.DialogueUI));
        }

        private System.Collections.IEnumerator WaitUntilDialogueClosed(DialogueUI dialogueUI)
        {
            // Wait while the dialogue is open
            yield return new WaitUntil(() => dialogueUI.IsOpen == false);
            
            // Turn the icon back on!
            interactionIcon.SetActive(true);
        }
    }
