using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance;
    
    [Header("Settings")]
    public float fadeDuration = 1f;
    public float holdDuration = 2f;
    public Color fadeColor = Color.black;
    
    private Image fadeImage;
    private bool isBusy = false;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        CreateUI();
    }

    private void CreateUI()
    {
        GameObject canvasGO = new GameObject("FadeCanvas");
        canvasGO.transform.SetParent(transform);
        
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        
        canvasGO.AddComponent<GraphicRaycaster>();
        
        GameObject imageGO = new GameObject("FadeImage");
        imageGO.transform.SetParent(canvasGO.transform);
        
        fadeImage = imageGO.AddComponent<Image>();
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        fadeImage.raycastTarget = true;
        
        RectTransform rect = fadeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    public void LoadSceneWithFade(string sceneName)
    {
        if (!isBusy)
        {
            StartCoroutine(TransitionRoutine(sceneName, -1));
        }
    }

    public void LoadSceneWithFade(int sceneIndex)
    {
        if (!isBusy)
        {
            StartCoroutine(TransitionRoutine(null, sceneIndex));
        }
    }

    private IEnumerator TransitionRoutine(string sceneName, int sceneIndex)
    {
        isBusy = true;
        
        // PASO 1: Fade a negro
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / fadeDuration;
            SetAlpha(Mathf.Lerp(0f, 1f, t));
            yield return null;
        }
        SetAlpha(1f);
        
        // PASO 2: Primera mitad del hold (antes de cargar)
        yield return new WaitForSecondsRealtime(holdDuration / 2f);
        
        // PASO 3: Cargar escena en el medio del hold
        AsyncOperation operation;
        if (sceneName != null)
        {
            operation = SceneManager.LoadSceneAsync(sceneName);
        }
        else
        {
            operation = SceneManager.LoadSceneAsync(sceneIndex);
        }
        
        while (!operation.isDone)
        {
            yield return null;
        }
        
        // PASO 4: Segunda mitad del hold (después de cargar)
        yield return new WaitForSecondsRealtime(holdDuration / 2f);
        
        // PASO 5: Fade a transparente
        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / fadeDuration;
            SetAlpha(Mathf.Lerp(1f, 0f, t));
            yield return null;
        }
        SetAlpha(0f);
        
        isBusy = false;
    }

    private void SetAlpha(float alpha)
    {
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
    }
}