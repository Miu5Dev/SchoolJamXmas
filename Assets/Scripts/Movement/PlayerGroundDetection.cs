using UnityEngine;

/// <summary>
/// Handles ground detection, slope detection, and ground stick force calculation.
/// </summary>
public class PlayerGroundDetection : MonoBehaviour
{
    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.5f;
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private LayerMask groundLayer = ~0;
    
    [Header("Ground Stick")]
    [SerializeField] private float groundStickForceMin = 5f;
    [SerializeField] private float groundStickForceMax = 30f;
    [SerializeField] private float groundStickSpeedReference = 40f;
    
    [Header("Slope Settings")]
    [SerializeField] private float maxWalkableAngle = 45f;
    [SerializeField] private float slopeNormalSmoothSpeed = 15f;
    
    [Header("Debug")]
    [SerializeField] private bool isGrounded;
    [SerializeField] private bool isOnSteepSlope;
    [SerializeField] private float currentSlopeAngle;
    [SerializeField] private float currentGroundStickForce;
    [SerializeField] private string groundObjectName = "NONE";
    
    private CharacterController controller;
    private Vector3 slopeNormal = Vector3.up;
    private Vector3 smoothedSlopeNormal = Vector3.up;
    private Vector3 rayOrigin;
    private float rayLength;
    private bool wasGrounded;
    private float lastSlopeAngle;
    
    // Public properties
    public bool IsGrounded => isGrounded;
    public bool IsOnSteepSlope => isOnSteepSlope;
    public float SlopeAngle => currentSlopeAngle;
    public float MaxWalkableAngle => maxWalkableAngle;
    public Vector3 SlopeNormal => smoothedSlopeNormal;
    public float GroundStickForce => currentGroundStickForce;
    public bool WasGrounded => wasGrounded;
    
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }
    
    /// <summary>
    /// Perform ground detection. Call this in Update before movement.
    /// </summary>
    public GroundCheckResult CheckGround()
    {
        wasGrounded = isGrounded;
        
        rayOrigin = transform.position + Vector3.up * 0.1f;
        rayLength = 0.1f + groundCheckDistance;
        
        Vector3 averageNormal = Vector3.zero;
        int hitCount = 0;
        float closestDistance = float.MaxValue;
        string hitName = "NONE";
        RaycastHit closestHit = default;
        
        // Center ray
        if (CastGroundRay(rayOrigin, out RaycastHit centerHit))
        {
            averageNormal += centerHit.normal;
            hitCount++;
            if (centerHit.distance < closestDistance)
            {
                closestDistance = centerHit.distance;
                hitName = centerHit.collider.gameObject.name;
                closestHit = centerHit;
            }
        }
        
        // Surrounding rays
        if (controller != null)
        {
            float offset = controller.radius * 0.5f;
            Vector3[] offsets = new Vector3[]
            {
                Vector3.forward * offset,
                Vector3.back * offset,
                Vector3.left * offset,
                Vector3.right * offset
            };
            
            foreach (Vector3 off in offsets)
            {
                if (CastGroundRay(rayOrigin + off, out RaycastHit hit))
                {
                    averageNormal += hit.normal;
                    hitCount++;
                    if (hit.distance < closestDistance)
                    {
                        closestDistance = hit.distance;
                        hitName = hit.collider.gameObject.name;
                        closestHit = hit;
                    }
                }
            }
        }
        
        GroundCheckResult result = new GroundCheckResult();
        
        if (hitCount > 0)
        {
            slopeNormal = (averageNormal / hitCount).normalized;
            smoothedSlopeNormal = Vector3.Lerp(smoothedSlopeNormal, slopeNormal, slopeNormalSmoothSpeed * Time.deltaTime);
            currentSlopeAngle = Vector3.Angle(Vector3.up, smoothedSlopeNormal);
            isGrounded = true;
            isOnSteepSlope = currentSlopeAngle > maxWalkableAngle;
            groundObjectName = hitName;
            
            result.isGrounded = true;
            result.normal = smoothedSlopeNormal;
            result.angle = currentSlopeAngle;
            result.isOnSteepSlope = isOnSteepSlope;
            result.groundObject = closestHit.collider?.gameObject;
            result.hitPoint = closestHit.point;
            result.justLanded = !wasGrounded && isGrounded;
        }
        else
        {
            isGrounded = false;
            isOnSteepSlope = false;
            slopeNormal = Vector3.up;
            smoothedSlopeNormal = Vector3.Lerp(smoothedSlopeNormal, Vector3.up, 10f * Time.deltaTime);
            currentSlopeAngle = 0f;
            groundObjectName = "NONE";
            
            result.isGrounded = false;
            result.normal = Vector3.up;
            result.angle = 0f;
            result.isOnSteepSlope = false;
            result.justLeftGround = wasGrounded && !isGrounded;
        }
        
        // Raise slope event if angle changed significantly
        if (Mathf.Abs(currentSlopeAngle - lastSlopeAngle) > 2f)
        {
            EventBus.Raise(new OnPlayerSlopeEvent
            {
                Player = gameObject,
                SlopeAngle = currentSlopeAngle,
                IsOnSteepSlope = isOnSteepSlope,
                SlopeNormal = smoothedSlopeNormal
            });
            lastSlopeAngle = currentSlopeAngle;
        }
        
        // Raise grounded event if changed
        if (isGrounded != wasGrounded)
        {
            EventBus.Raise(new OnPlayerGroundedEvent
            {
                Player = gameObject,
                IsGrounded = isGrounded
            });
        }
        
        return result;
    }
    
    /// <summary>
    /// Calculate ground stick force based on current speed
    /// </summary>
    public float CalculateGroundStickForce(float speed)
    {
        float speedFactor = Mathf.Clamp01(speed / groundStickSpeedReference);
        currentGroundStickForce = Mathf.Lerp(groundStickForceMin, groundStickForceMax, speedFactor * speedFactor);
        return currentGroundStickForce;
    }
    
    /// <summary>
    /// Get the slide direction on current slope
    /// </summary>
    public Vector3 GetSlideDirection()
    {
        return Vector3.ProjectOnPlane(Vector3.down, smoothedSlopeNormal).normalized;
    }
    
    /// <summary>
    /// Get movement direction projected onto slope
    /// </summary>
    public Vector3 ProjectOnSlope(Vector3 direction)
    {
        if (currentSlopeAngle < 1f) return direction;
        return Vector3.ProjectOnPlane(direction, smoothedSlopeNormal).normalized;
    }
    
    /// <summary>
    /// Calculate how much of the movement is uphill
    /// </summary>
    public float GetUphillFactor(Vector3 movementDirection)
    {
        if (currentSlopeAngle < 1f) return 0f;
        
        Vector3 projectedMovement = Vector3.ProjectOnPlane(movementDirection, smoothedSlopeNormal);
        
        if (projectedMovement.y > 0.01f)
        {
            float verticalComponent = projectedMovement.y;
            float horizontalComponent = new Vector2(projectedMovement.x, projectedMovement.z).magnitude;
            float climbRatio = verticalComponent / (verticalComponent + horizontalComponent + 0.001f);
            return Mathf.Clamp01(climbRatio * 2f + 0.3f);
        }
        
        return 0f;
    }
    
    /// <summary>
    /// Get rotation to align with slope (for visual alignment)
    /// </summary>
    public Quaternion GetSlopeAlignedRotation(Quaternion currentRotation, float alignmentStrength = 1f)
    {
        if (currentSlopeAngle < 1f || !isGrounded)
        {
            return Quaternion.Euler(0f, currentRotation.eulerAngles.y, 0f);
        }
        
        Vector3 forward = currentRotation * Vector3.forward;
        Vector3 projectedForward = Vector3.ProjectOnPlane(forward, smoothedSlopeNormal).normalized;
        
        if (projectedForward.magnitude < 0.1f)
        {
            projectedForward = forward;
        }
        
        Quaternion targetRotation = Quaternion.LookRotation(projectedForward, smoothedSlopeNormal);
        return Quaternion.Slerp(currentRotation, targetRotation, alignmentStrength);
    }
    
    private bool CastGroundRay(Vector3 origin, out RaycastHit hit)
    {
        return Physics.Raycast(origin, Vector3.down, out hit, rayLength, groundLayer);
    }
    
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        // Ground rays
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * rayLength);
        
        if (controller != null)
        {
            float offset = controller.radius * 0.5f;
            Gizmos.color = Color.gray;
            Gizmos.DrawLine(rayOrigin + Vector3.forward * offset, rayOrigin + Vector3.forward * offset + Vector3.down * rayLength);
            Gizmos.DrawLine(rayOrigin + Vector3.back * offset, rayOrigin + Vector3.back * offset + Vector3.down * rayLength);
            Gizmos.DrawLine(rayOrigin + Vector3.left * offset, rayOrigin + Vector3.left * offset + Vector3.down * rayLength);
            Gizmos.DrawLine(rayOrigin + Vector3.right * offset, rayOrigin + Vector3.right * offset + Vector3.down * rayLength);
        }
        
        // Slope normal
        if (isGrounded)
        {
            Gizmos.color = isOnSteepSlope ? Color.red : Color.cyan;
            Vector3 groundPoint = transform.position;
            Gizmos.DrawLine(groundPoint, groundPoint + smoothedSlopeNormal * 2f);
            
            // Slide direction
            if (currentSlopeAngle > 1f)
            {
                Gizmos.color = Color.yellow;
                Vector3 slideDir = GetSlideDirection();
                Gizmos.DrawLine(groundPoint + Vector3.up * 0.5f, groundPoint + Vector3.up * 0.5f + slideDir * 2f);
            }
        }
    }
}

/// <summary>
/// Result of a ground check
/// </summary>
public struct GroundCheckResult
{
    public bool isGrounded;
    public bool isOnSteepSlope;
    public bool justLanded;
    public bool justLeftGround;
    public float angle;
    public Vector3 normal;
    public Vector3 hitPoint;
    public GameObject groundObject;
}
