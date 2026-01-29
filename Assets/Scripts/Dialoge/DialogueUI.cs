using System;
using UnityEngine;
using TMPro;
using System.Collections;

public class DialogueUI : MonoBehaviour
{
    [SerializeField] private GameObject dialogueBox;
    [SerializeField] private TMP_Text textLabel;

    [SerializeField] private bool actionKey;
    
    public bool IsOpen { get; private set; }
    
    private ResponseHandler responseHandler;
    private TypewriterEffect typewriterEffect;
    private void Start()
    {
        typewriterEffect = GetComponent<TypewriterEffect>();
        responseHandler = GetComponent<ResponseHandler>();
        CloseDialogueBox();
        
        EventBus.Subscribe<OnActionInputEvent>(onAction);
    }
    
    public void showDialogue(DialogueObject dialogueObject)
    {
        IsOpen = true;
        if (!dialogueBox.active)
        {
            EventBus.Raise<onDialogueOpen>(new onDialogueOpen());
        }
        dialogueBox.SetActive(true);
        StartCoroutine(StepThroughDilaogue(dialogueObject));
    }


    public void onAction(OnActionInputEvent inputEvent)
    {
        actionKey = inputEvent.pressed;
    }

    public void AddResponseEvents(ResponseEvent[] responseEvents)
    {
        responseHandler.AddResponse(responseEvents);
    }

    public void LateUpdate()
    {
        if(actionKey)
        actionKey = false;
    }

    private IEnumerator StepThroughDilaogue(DialogueObject dialogueObject)
    {

        for (int i = 0; i < dialogueObject.Dialogue.Length; i++)
        {
            string dialogue = dialogueObject.Dialogue[i];

            yield return RunTypingEffect(dialogue);

            textLabel.text = dialogue;

            if (i == dialogueObject.Dialogue.Length - 1 && dialogueObject.HasResponses) break;

            yield return null;
            
            yield return new WaitUntil(() => actionKey);
        }

        if (dialogueObject.HasResponses)
        {
            responseHandler.ShowResponse(dialogueObject.Responses);
        }
        else
        {
            CloseDialogueBox();
        }
    }

    private IEnumerator RunTypingEffect(string dialogue)
    {
        typewriterEffect.Run(dialogue, textLabel);

        while (typewriterEffect.IsRunning)
        {
            yield return null;

            if (actionKey)
            {
                typewriterEffect.Stop();
            }
        }
    }

    public void CloseDialogueBox()
    {
        IsOpen = false;
        dialogueBox.SetActive(false);
        textLabel.text = string.Empty;
        EventBus.Raise<onDialogueClose>(new onDialogueClose() );
    }
}
