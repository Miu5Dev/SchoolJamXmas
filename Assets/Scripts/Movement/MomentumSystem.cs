using UnityEngine;

/// <summary>
/// Centralized momentum system that handles all velocity sources.
/// Provides fluid velocity blending and momentum fighting mechanics.
/// </summary>
public class MomentumSystem : MonoBehaviour
{
    [Header("Momentum Settings")]
    [SerializeField] private float baseDecay = 3f;
    [SerializeField] private float opposedDecay = 8f;
    [SerializeField] private float sideDecay = 5f;
    [SerializeField] private float alignedDecay = 2f;
    [SerializeField] private float maxMomentum = 30f;
    
    [Header("Momentum Fighting")]
    [SerializeField] private float fightStrength = 0.7f;
    [SerializeField] private float fightRampTime = 0.3f;
    
    [Header("Source Weights")]
    [SerializeField] private float movementWeight = 1f;
    [SerializeField] private float jumpWeight = 1f;
    [SerializeField] private float slideWeight = 1.2f;
    [SerializeField] private float groundPoundWeight = 0.5f;
    
    [Header("Debug Gizmos")]
    [SerializeField] private bool showMomentumVector = true;
    [SerializeField] private bool showFightIndicator = true;
    [SerializeField] private bool showAlignmentIndicator = true;
    
    [Header("Debug Info")]
    [SerializeField] private Vector3 currentMomentum;
    [SerializeField] private float momentumMagnitude;
    [SerializeField] private float fightProgress;
    [SerializeField] private float alignment;
    
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
            alignment = 0f;
            return;
        }
        
        // Calculate alignment with momentum
        alignment = 0f;
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
                
                if (inputSpeed > currentMomentum.magnitude * 0.5f)
                {
                    currentMomentum = Vector3.Lerp(currentMomentum, inputDirection * inputSpeed, Time.deltaTime * 2f);
                }
            }
        }
        else
        {
            // No input - faster decay on ground
            if (isGrounded)
            {
                decayRate = baseDecay * 2.5f; // Much faster decay on ground when no input
            }
            else
            {
                decayRate = baseDecay * 0.3f; // Slower decay in air when no input
            }
            fightTimer = 0f;
            isFighting = false;
        }
        
        // Apply grounded modifier (in addition to above changes)
        if (!isGrounded && inputDirection.magnitude > 0.1f)
        {
            decayRate *= 0.4f; // Air momentum decays slower when controlling
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
        
        Vector3 combined = inputVelocity + currentMomentum * blendFactor;
        
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
        
        if (currentMomentum.magnitude < 0.1f) return;
        
        Vector3 start = transform.position + Vector3.up;
        
        // Momentum vector
        if (showMomentumVector)
        {
            Gizmos.color = isFighting ? Color.red : Color.cyan;
            Gizmos.DrawLine(start, start + currentMomentum * 0.2f);
            Gizmos.DrawWireSphere(start + currentMomentum * 0.2f, 0.1f);
            
            // Momentum magnitude bar
            Gizmos.color = Color.Lerp(Color.green, Color.red, momentumMagnitude / maxMomentum);
            Vector3 barStart = transform.position + Vector3.up * 1.8f + Vector3.left * 0.3f;
            float barLength = (momentumMagnitude / maxMomentum) * 0.6f;
            Gizmos.DrawLine(barStart, barStart + Vector3.right * barLength);
        }
        
        // Fight indicator
        if (showFightIndicator && isFighting)
        {
            Gizmos.color = Color.Lerp(Color.yellow, Color.green, fightProgress);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.1f + fightProgress * 0.2f);
            
            // Fight progress bar
            Vector3 fightBarStart = transform.position + Vector3.up * 2.0f + Vector3.left * 0.3f;
            Gizmos.DrawLine(fightBarStart, fightBarStart + Vector3.right * (fightProgress * 0.6f));
        }
        
        // Alignment indicator
        if (showAlignmentIndicator && previousInputDirection.magnitude > 0.1f)
        {
            // Color based on alignment: red = opposed, yellow = perpendicular, green = aligned
            if (alignment < -0.3f)
                Gizmos.color = Color.red;
            else if (alignment < 0.3f)
                Gizmos.color = Color.yellow;
            else
                Gizmos.color = Color.green;
            
            Gizmos.DrawLine(start + Vector3.up * 0.3f, start + Vector3.up * 0.3f + previousInputDirection * 0.5f);
        }
    }
}
