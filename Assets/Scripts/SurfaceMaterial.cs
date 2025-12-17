using UnityEngine;

public class SurfaceMaterial : MonoBehaviour
{
    [Header("Preset (Optional)")]
    [Tooltip("Load settings from a preset. Leave empty to use custom values below.")]
    [SerializeField] private SurfacePreset preset;
    
    [Header("Surface Properties")]
    [SerializeField] private SurfaceType surfaceType = SurfaceType.Normal;
    
    [Header("Custom Friction Settings")]
    [Tooltip("1 = normal, 0.1 = ice (very slippery), 2 = sticky")]
    [Range(0.01f, 3f)]
    [SerializeField] private float customFriction = 1f;
    
    [Tooltip("Acceleration multiplier - lower = harder to speed up")]
    [Range(0.1f, 3f)]
    [SerializeField] private float customAcceleration = 1f;
    
    [Tooltip("Deceleration multiplier - higher = stops faster")]
    [Range(0.1f, 3f)]
    [SerializeField] private float customDeceleration = 1f;
    
    [Tooltip("Max speed multiplier - lower = slower top speed")]
    [Range(0.5f, 2f)]
    [SerializeField] private float customMaxSpeed = 1f;
    
    [Header("Slope Settings")]
    [SerializeField] private bool affectsSliding = true;
    
    [Tooltip("How fast character slides on slopes - higher = faster")]
    [Range(0.5f, 3f)]
    [SerializeField] private float slideSpeedMultiplier = 1f;
    
    // Properties that use preset if available, otherwise use custom values
    public SurfaceType SurfaceType => preset != null ? preset.surfaceType : surfaceType;
    public float Friction => preset != null ? preset.friction : customFriction;
    public float AccelerationMultiplier => preset != null ? preset.accelerationMultiplier : customAcceleration;
    public float DecelerationMultiplier => preset != null ? preset.decelerationMultiplier : customDeceleration;
    public float MaxSpeedMultiplier => preset != null ? preset.maxSpeedMultiplier : customMaxSpeed;
    public bool AffectsSliding => preset != null ? preset.affectsSliding : affectsSliding;
    public float SlideSpeedMultiplier => preset != null ? preset.slideSpeedMultiplier : slideSpeedMultiplier;
    
    // Button to load preset values into custom fields (optional)
    [ContextMenu("Load Preset Values")]
    private void LoadPresetValues()
    {
        if (preset != null)
        {
            surfaceType = preset.surfaceType;
            customFriction = preset.friction;
            customAcceleration = preset.accelerationMultiplier;
            customDeceleration = preset.decelerationMultiplier;
            customMaxSpeed = preset.maxSpeedMultiplier;
            affectsSliding = preset.affectsSliding;
            slideSpeedMultiplier = preset.slideSpeedMultiplier;
            
            Debug.Log($"Loaded values from preset: {preset.surfaceName}");
        }
        else
        {
            Debug.LogWarning("No preset assigned!");
        }
    }
}

public enum SurfaceType
{
    Normal,
    Ice,
    Sticky,
    Bouncy,
    Custom
}