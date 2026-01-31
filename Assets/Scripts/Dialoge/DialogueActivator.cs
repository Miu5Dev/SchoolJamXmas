using System;
using UnityEngine;

// Component that activates dialogue when the player interacts with an object
public class DialogueActivator : MonoBehaviour, Interactable
{
    // The dialogue to display when interacted with
    [SerializeField] private DialogueObject dialogueObject;

    // UI icon shown when player can interact (e.g., "Press E")
    public GameObject interactionIcon;
    private bool playerInRange = false;

    // Reference to the player's dialogue UI
    [SerializeField] private DialogueUI dialogueUI;

    // Subscribe to action input events when enabled
    private void OnEnable()
    {
        EventBus.Subscribe<OnActionInputEvent>(doStuff);
    }

    // Unsubscribe when disabled to prevent memory leaks
    private void OnDisable()
    {
        EventBus.Unsubscribe<OnActionInputEvent>(doStuff);
    }

    // Handles action input events (e.g., interaction button press)
    private void doStuff(OnActionInputEvent ev)
    {
        // ⭐ CHECK: Only interact if button pressed AND dialogue UI exists AND dialogue is not already open
        if (ev.pressed && playerInRange && dialogueUI != null && !dialogueUI.IsOpen)
        {
            Interact();
        }
    }

    // Allows updating the dialogue object at runtime
    public void UpdateDialogueObject(DialogueObject dialogueObject)
    {
        this.dialogueObject = dialogueObject;
    }

    // Called continuously while player is in trigger zone
    private void OnTriggerStay(Collider other)
    {
        Debug.Log("OnTriggerStay");

        // Check if the object in trigger is the player
        if (other.CompareTag("Player") && other.TryGetComponent(out PlayerDialogue player))
        {
            playerInRange = true;
            // Get reference to player's dialogue UI
            dialogueUI = player.DialogueUI;

            // ⭐ OPTIONAL: Only show interaction icon if dialogue is not already open
            interactionIcon.SetActive(!dialogueUI.IsOpen);
        }   
    }

    // Called when player exits trigger zone
    private void OnTriggerExit(Collider other)
    {
        // Check if the exiting object is the player
        if (other.CompareTag("Player") && other.TryGetComponent(out PlayerDialogue player))
        {
            playerInRange = false;
            // Clear dialogue UI reference
            dialogueUI = null;

            // Hide interaction icon
            interactionIcon.SetActive(false);
        }
    }

    // Main interaction method called when player presses interaction button
    public void Interact()
    {
        // ⭐ EXIT EARLY: Don't start dialogue if UI is null OR dialogue is already open
        if (dialogueUI == null || dialogueUI.IsOpen) return;

        // Find and add any response events associated with this dialogue
        foreach (DialogueResponseEvents responseEvents in GetComponents<DialogueResponseEvents>())
        {
            // Only add events if they match the current dialogue object
            if (responseEvents.DialogueObject == dialogueObject)
            {
                dialogueUI.AddResponseEvents(responseEvents.Events);
                break;
            }
        }

        // Hide interaction icon while dialogue is active
        interactionIcon.SetActive(false);

        // ⭐ START DIALOGUE HERE - This is where dialogues begin
        dialogueUI.showDialogue(dialogueObject);

        // Start coroutine to wait for dialogue to close, then re-enable icon
        StartCoroutine(WaitUntilDialogueClosed(dialogueUI));
    }

    // Coroutine that waits until dialogue is closed
    private System.Collections.IEnumerator WaitUntilDialogueClosed(DialogueUI dialogueUI)
    {
        // Wait while the dialogue is open
        yield return new WaitUntil(() => dialogueUI.IsOpen == false);

        // ⭐ Re-enable icon only if player is still in trigger zone (dialogueUI still referenced)
        if (this.dialogueUI != null)
        {
            interactionIcon.SetActive(true);
        }
    }
}