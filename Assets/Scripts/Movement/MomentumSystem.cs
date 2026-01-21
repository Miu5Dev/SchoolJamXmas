using UnityEngine;

/// <summary>
/// Centralized momentum system that handles all velocity sources.
/// Provides fluid velocity blending and momentum fighting mechanics.
/// </summary>
public class MomentumSystem : MonoBehaviour
{
    [Header("Momentum Settings")]
    [SerializeField] private float baseDecay = 3f;
    [SerializeField] private float opposedDecay = 8f;        // When moving against momentum
    [SerializeField] private float sideDecay = 5f;           // When moving perpendicular
    [SerializeField] private float alignedDecay = 2f;        // When moving with momentum
    [SerializeField] private float maxMomentum = 30f;
    
    [Header("Momentum Fighting")]
    [SerializeField] private float fightStrength = 0.7f;     // How much input fights momentum
    [SerializeField] private float fightRampTime = 0.3f;     // Time to reach full fight strength
    
    [Header("Source Weights")]
    [SerializeField] private float movementWeight = 1f;
    [SerializeField] private float jumpWeight = 1f;
    [SerializeField] private float slideWeight = 1.2f;
    [SerializeField] private float groundPoundWeight = 0.5f;
    
    [Header("Debug")]
    [SerializeField] private Vector3 currentMomentum;
    [SerializeField] private float momentumMagnitude;
    [SerializeField] private float fightProgress;
    
    private Vector3 previousInputDirection;
    private float fightTimer;
    private bool isFighting;
    
    public Vector3 CurrentMomentum => currentMomentum;
    public float MomentumMagnitude => momentumMagnitude;
    
    /// <summary>
    /// Add momentum from a specific source
    /// </summary>
    public void AddMomentum(Vector3 velocity, MomentumSource source)
    {
        float weight = GetSourceWeight(source);
        Vector3 addedMomentum = velocity * weight;
        
        currentMomentum += addedMomentum;
        
        // Clamp to max
        if (currentMomentum.magnitude > maxMomentum)
        {
            currentMomentum = currentMomentum.normalized * maxMomentum;
        }
        
        momentumMagnitude = currentMomentum.magnitude;
        
        EventBus.Raise(new OnPlayerMomentumEvent
        {
            Player = gameObject,
            Momentum = currentMomentum,
            MomentumMagnitude = momentumMagnitude,
            Source = source
        });
    }
    
    /// <summary>
    /// Set momentum directly (for jumps and special moves)
    /// </summary>
    public void SetMomentum(Vector3 velocity, MomentumSource source)
    {
        float weight = GetSourceWeight(source);
        currentMomentum = velocity * weight;
        
        if (currentMomentum.magnitude > maxMomentum)
        {
            currentMomentum = currentMomentum.normalized * maxMomentum;
        }
        
        momentumMagnitude = currentMomentum.magnitude;
        
        EventBus.Raise(new OnPlayerMomentumEvent
        {
            Player = gameObject,
            Momentum = currentMomentum,
            MomentumMagnitude = momentumMagnitude,
            Source = source
        });
    }
    
    /// <summary>
    /// Update momentum based on player input direction (call every frame)
    /// </summary>
    public void UpdateMomentum(Vector3 inputDirection, float inputSpeed, bool isGrounded)
    {
        if (currentMomentum.magnitude < 0.1f)
        {
            currentMomentum = Vector3.zero;
            momentumMagnitude = 0f;
            fightTimer = 0f;
            isFighting = false;
            return;
        }
        
        // Calculate alignment with momentum
        float alignment = 0f;
        if (inputDirection.magnitude > 0.1f && currentMomentum.magnitude > 0.1f)
        {
            alignment = Vector3.Dot(inputDirection.normalized, currentMomentum.normalized);
        }
        
        // Determine decay rate based on alignment
        float decayRate = baseDecay;
        
        if (inputDirection.magnitude > 0.1f)
        {
            if (alignment < -0.3f)
            {
                // Moving against momentum - fight it
                decayRate = opposedDecay;
                
                if (!isFighting)
                {
                    isFighting = true;
                    fightTimer = 0f;
                }
                
                fightTimer += Time.deltaTime;
                fightProgress = Mathf.Clamp01(fightTimer / fightRampTime);
                
                // Apply fight force
                float fightForce = fightStrength * fightProgress * inputSpeed;
                Vector3 fightDirection = -currentMomentum.normalized;
                currentMomentum += fightDirection * fightForce * Time.deltaTime;
            }
            else if (alignment < 0.3f)
            {
                // Moving perpendicular
                decayRate = sideDecay;
                fightTimer = Mathf.Max(0f, fightTimer - Time.deltaTime);
                isFighting = false;
            }
            else
            {
                // Moving with momentum
                decayRate = alignedDecay;
                fightTimer = 0f;
                isFighting = false;
                
                // Add some momentum when moving in same direction
                if (inputSpeed > currentMomentum.magnitude * 0.5f)
                {
                    currentMomentum = Vector3.Lerp(currentMomentum, inputDirection * inputSpeed, Time.deltaTime * 2f);
                }
            }
        }
        else
        {
            // No input - natural decay
            fightTimer = 0f;
            isFighting = false;
        }
        
        // Apply grounded modifier
        if (!isGrounded)
        {
            decayRate *= 0.5f; // Slower decay in air
        }
        
        // Apply decay
        currentMomentum = Vector3.MoveTowards(currentMomentum, Vector3.zero, decayRate * Time.deltaTime);
        momentumMagnitude = currentMomentum.magnitude;
        
        previousInputDirection = inputDirection;
    }
    
    /// <summary>
    /// Get the final velocity combining input and momentum
    /// </summary>
    public Vector3 GetCombinedVelocity(Vector3 inputVelocity, float blendFactor = 0.8f)
    {
        if (currentMomentum.magnitude < 0.1f)
        {
            return inputVelocity;
        }
        
        // Blend input with momentum
        Vector3 combined = inputVelocity + currentMomentum * blendFactor;
        
        // When fighting momentum, reduce momentum influence
        if (isFighting)
        {
            float reducedBlend = blendFactor * (1f - fightProgress * 0.5f);
            combined = inputVelocity + currentMomentum * reducedBlend;
        }
        
        return combined;
    }
    
    /// <summary>
    /// Clear all momentum
    /// </summary>
    public void ClearMomentum()
    {
        currentMomentum = Vector3.zero;
        momentumMagnitude = 0f;
        fightTimer = 0f;
        isFighting = false;
        fightProgress = 0f;
    }
    
    /// <summary>
    /// Reduce momentum by a factor (for landing, etc)
    /// </summary>
    public void ReduceMomentum(float factor)
    {
        currentMomentum *= (1f - factor);
        momentumMagnitude = currentMomentum.magnitude;
    }
    
    private float GetSourceWeight(MomentumSource source)
    {
        return source switch
        {
            MomentumSource.Movement => movementWeight,
            MomentumSource.Jump => jumpWeight,
            MomentumSource.Slide => slideWeight,
            MomentumSource.GroundPound => groundPoundWeight,
            MomentumSource.External => 1f,
            _ => 1f
        };
    }
    
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        if (currentMomentum.magnitude > 0.1f)
        {
            // Draw momentum vector
            Gizmos.color = isFighting ? Color.red : Color.cyan;
            Vector3 start = transform.position + Vector3.up;
            Gizmos.DrawLine(start, start + currentMomentum * 0.2f);
            Gizmos.DrawWireSphere(start + currentMomentum * 0.2f, 0.1f);
            
            // Fight progress indicator
            if (isFighting)
            {
                Gizmos.color = Color.Lerp(Color.yellow, Color.green, fightProgress);
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.1f + fightProgress * 0.2f);
            }
        }
    }
}
