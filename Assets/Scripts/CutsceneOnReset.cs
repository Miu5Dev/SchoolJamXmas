using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CutsceneOnReset : MonoBehaviour
{
    

    [Header("Cutscene Settings")]
    public Animator playerAnimator;
    public string wakeUpTrigger = "WakeUp";
    public float cutsceneDuration = 2f;
    
    public bool animationIsPlaying = false;
    
    public MonoBehaviour playerController;

    public void Start()
    {
        PlayCutsceneAndReload();
    }
    
    public void PlayCutsceneAndReload()
    {
        animationIsPlaying = true;
        StartCoroutine(CutsceneRoutine());
    }

    public void Update()
    {
        if(animationIsPlaying)
            EventBus.Raise<onDialogueOpen>(new onDialogueOpen());

    }

    private IEnumerator CutsceneRoutine()
    {
        if (playerAnimator != null)
            playerAnimator.SetTrigger(wakeUpTrigger);
        

        // Wait for the animation
        yield return new WaitForSeconds(cutsceneDuration);
        animationIsPlaying = false;
        EventBus.Raise<onDialogueClose>(new onDialogueClose());
    }
}