using UnityEngine;

public class CutsceneCamera : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;
    public float duration = 3f;
    
    private float timeElapsed = 0f;
    private bool isPlaying = false;
    
    void Update()
    {
        if (isPlaying && timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / duration;
            
            // Smooth movement
            transform.position = Vector3.Lerp(startPoint.position, endPoint.position, t);
            transform.rotation = Quaternion.Lerp(startPoint.rotation, endPoint.rotation, t);
        }
    }
    
    public void PlayCutscene()
    {
        isPlaying = true;
        timeElapsed = 0f;
    }
}