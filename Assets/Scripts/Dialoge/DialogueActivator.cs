using System;
using UnityEngine;

public class DialogueActivator : MonoBehaviour, Interactable
{
    [SerializeField] private DialogueObject dialogueObject;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent(out PlayerController player))
        {
            player.interactable = this;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent(out PlayerController player))
        {
            if (player.interactable is DialogueActivator dialogueActivator && dialogueActivator == this)
            {
                player.interactable = null;
            }
        }

    }

    public void Interact(PlayerController player)
    {
        player.DialogueUI.showDialogue(dialogueObject);
    }
}
