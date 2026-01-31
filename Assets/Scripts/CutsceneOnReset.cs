using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CutsceneOnReset : MonoBehaviour
{
    [Header("Cutscene Settings")]
    public Animator playerAnimator;
    public string wakeUpTrigger = "WakeUp";
    public float cutsceneDuration = 2f;
    public MonoBehaviour playerController;

    public void PlayCutsceneAndReload()
    {
        StartCoroutine(CutsceneRoutine());
    }

    private IEnumerator CutsceneRoutine()
    {
        // Disable controls
        // Here

        // Play wake-up animation
        if (playerAnimator != null)
            playerAnimator.SetTrigger(wakeUpTrigger);

        // Wait for the animation
        yield return new WaitForSeconds(cutsceneDuration);

        // Re-enable controls
        // HERE

        // Now actually reset level (you can move your GameManager logic here)
        if (GameManager.Instance.CurrentLives > 0)
        {
            FadeManager.Instance.LoadSceneWithFade(SceneManager.GetActiveScene().name);
            GameManager.Instance.CurrentLives -= 1;
        }
        else
        {
            FadeManager.Instance.LoadSceneWithFade(0);
        }
    }
}