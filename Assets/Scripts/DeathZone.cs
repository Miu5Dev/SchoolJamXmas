using UnityEngine;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class DeathZone : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("Tag del objeto a detectar")]
    public string targetTag = "Player";
    
    [Header("Events")]
    public UnityEvent onObjectFullyInside;
    private Collider zoneCollider;

    void Awake()
    {
        zoneCollider = GetComponent<Collider>();
        zoneCollider.isTrigger = true;
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag(targetTag)) return;
        
        if (IsFullyInside(other))
        {
            onObjectFullyInside?.Invoke();
        }
    }

    private bool IsFullyInside(Collider target)
    {
        Bounds zoneBounds = zoneCollider.bounds;
        Bounds targetBounds = target.bounds;
        
        // Verificar si todos los puntos del objeto están dentro de la zona
        Vector3 min = targetBounds.min;
        Vector3 max = targetBounds.max;
        
        return zoneBounds.Contains(min) && zoneBounds.Contains(max);
    }

    void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }

    public void ResetLevel()
    {
        if(GameManager.Instance.CurrentLives > 0){
            FadeManager.Instance.LoadSceneWithFade(SceneManager.GetActiveScene().name);
            GameManager.Instance.CurrentLives -= 1;
        }
        else
        {
            FadeManager.Instance.LoadSceneWithFade(0);
        }
    }
}
