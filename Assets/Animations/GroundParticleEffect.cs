using UnityEngine;

public class GroundParticleEffect : MonoBehaviour
{
    [SerializeField] private GameObject jump;
    [SerializeField] private GameObject walkpart;
    [SerializeField] private float effectDuration = 1f;
    [SerializeField] private Vector3 footOffset = Vector3.zero;
    private bool Airborn = true;
    private void OnEnable()
    { 
        EventBus.Subscribe<OnPlayerGroundedEvent>(Land); 
        EventBus.Subscribe<OnPlayerMoveEvent>(walk);
        EventBus.Subscribe<OnPlayerAirborneEvent>(inTheAir);
    }
    private void OnDisable()
    { 
        EventBus.Unsubscribe<OnPlayerGroundedEvent>(Land); 
        EventBus.Unsubscribe<OnPlayerMoveEvent>(walk);
        EventBus.Unsubscribe<OnPlayerAirborneEvent>(inTheAir);
    }
    
    
    public void SpawnFootParticle()
    {
        Vector3 spawnPosition = transform.position + footOffset;
        
        GameObject particleInstance = Instantiate(jump, spawnPosition, Quaternion.identity);
     
        Destroy(particleInstance, effectDuration);
    }
    public void walkParticle()
    {
        Vector3 spawnPosition = transform.position + footOffset;
        
        GameObject particleInstance = Instantiate(walkpart, spawnPosition, Quaternion.identity);
     
        Destroy(particleInstance, effectDuration);
    }
    private void Land(OnPlayerGroundedEvent ev)
    {
            SpawnFootParticle();
            Airborn = false;
    }
    private void walk(OnPlayerMoveEvent ev)
    {
        if (ev.speed >= 16 && !Airborn)
        {
            walkParticle();
        }
    }
    private void inTheAir(OnPlayerAirborneEvent ev)
    {
        Airborn = true;
    }
}
