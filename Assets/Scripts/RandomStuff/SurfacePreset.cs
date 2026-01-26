using UnityEngine;

[CreateAssetMenu(fileName = "New Surface Preset", menuName = "Player Controller/Surface Preset")]
public class SurfacePreset : ScriptableObject
{
    [Header("Surface Properties")]
    public string surfaceName = "New Surface";
    public SurfaceType surfaceType = SurfaceType.Normal;
    
    [Header("Friction Settings")]
    [Tooltip("1 = normal, 0.1 = ice (very slippery), 2 = sticky")]
    [Range(0.01f, 3f)] 
    public float friction = 1f;
    
    [Tooltip("Acceleration multiplier - lower = harder to speed up")]
    [Range(0.1f, 3f)] 
    public float accelerationMultiplier = 1f;
    
    [Tooltip("Deceleration multiplier - higher = stops faster")]
    [Range(0.1f, 3f)] 
    public float decelerationMultiplier = 1f;
    
    [Tooltip("Max speed multiplier - lower = slower top speed")]
    [Range(0.5f, 2f)] 
    public float maxSpeedMultiplier = 1f;
    
    [Header("Slope Settings")]
    public bool affectsSliding = true;
    
    [Tooltip("How fast character slides on slopes - higher = faster")]
    [Range(0.5f, 3f)] 
    public float slideSpeedMultiplier = 1f;
}