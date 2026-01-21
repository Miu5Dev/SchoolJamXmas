using UnityEngine;

/// <summary>
/// Third-person camera controller for Mario Odyssey-style gameplay.
/// Fixed to properly handle continuous controller input and stop when released.
/// </summary>
public class OdysseyCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    
    [Header("Distance")]
    [SerializeField] private float distance = 8f;
    [SerializeField] private float minDistance = 3f;
    [SerializeField] private float maxDistance = 15f;
    [SerializeField] private float scrollSensitivity = 2f;
    
    [Header("Rotation")]
    [SerializeField] private float rotationSensitivity = 0.15f;
    [SerializeField] private float controllerSensitivity = 100f;
    [SerializeField] private float minVerticalAngle = -20f;
    [SerializeField] private float maxVerticalAngle = 60f;
    
    [Header("Smoothing")]
    [SerializeField] private float positionSmoothTime = 0.05f;
    
    [Header("Offset")]
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);
    
    [Header("Collision")]
    [SerializeField] private bool enableCollision = true;
    [SerializeField] private float collisionRadius = 0.3f;
    [SerializeField] private LayerMask collisionLayers = ~0;
    
    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private bool showInputDebug = false;
    
    private float horizontalAngle;
    private float verticalAngle = 20f;
    private Vector3 currentVelocity;
    private Vector2 lookInput;
    private float currentDistance;

    void OnEnable()
    {
        EventBus.Subscribe<onLookInputEvent>(OnLookInput);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        currentDistance = distance;
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<onLookInputEvent>(OnLookInput);
    }

    private void OnLookInput(onLookInputEvent ev)
    {
        // Directly use the delta from input system
        // It will be zero when analog is released
        lookInput = ev.Delta;
        
        if (showInputDebug)
        {
            Debug.Log($"[Camera] Look Input: {lookInput}, Pressed: {ev.pressed}");
        }
    }

    void LateUpdate()
    {
        if (target == null) return;
        
        HandleRotationInput();
        HandleZoom();
        UpdateCameraPosition();
    }

    private void HandleRotationInput()
    {
        // Only rotate if there's actual input
        if (lookInput.sqrMagnitude < 0.0001f) return;
        
        // Determine if input is from mouse (large delta) or controller (small continuous)
        float inputMagnitude = lookInput.magnitude;
        bool isControllerInput = inputMagnitude < 5f;
        
        if (isControllerInput)
        {
            // Controller: multiply by time and sensitivity for smooth rotation
            horizontalAngle += lookInput.x * controllerSensitivity * Time.deltaTime;
            verticalAngle -= lookInput.y * controllerSensitivity * Time.deltaTime;
        }
        else
        {
            // Mouse: direct delta application
            horizontalAngle += lookInput.x * rotationSensitivity;
            verticalAngle -= lookInput.y * rotationSensitivity;
        }
        
        verticalAngle = Mathf.Clamp(verticalAngle, minVerticalAngle, maxVerticalAngle);
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance -= scroll * scrollSensitivity;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    private void UpdateCameraPosition()
    {
        Quaternion rotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0f);
        
        Vector3 targetPosition = target.position + targetOffset;
        
        // Handle camera collision
        float finalDistance = distance;
        if (enableCollision)
        {
            finalDistance = HandleCameraCollision(targetPosition, rotation);
        }
        
        // Smooth distance transitions
        currentDistance = Mathf.Lerp(currentDistance, finalDistance, 10f * Time.deltaTime);
        
        // Calculate final position
        Vector3 desiredPosition = targetPosition - (rotation * Vector3.forward * currentDistance);
        
        // Smooth position
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, positionSmoothTime);
        
        // Direct rotation
        transform.LookAt(targetPosition);
    }
    
    private float HandleCameraCollision(Vector3 targetPos, Quaternion rotation)
    {
        Vector3 direction = rotation * Vector3.back;
        
        if (Physics.SphereCast(targetPos, collisionRadius, direction, out RaycastHit hit, distance, collisionLayers))
        {
            return Mathf.Max(hit.distance - collisionRadius, minDistance);
        }
        
        return distance;
    }
    
    /// <summary>
    /// Get the camera's horizontal angle for movement direction calculation
    /// </summary>
    public float GetHorizontalAngle()
    {
        return horizontalAngle;
    }
    
    /// <summary>
    /// Get forward direction on XZ plane
    /// </summary>
    public Vector3 GetFlatForward()
    {
        return Quaternion.Euler(0f, horizontalAngle, 0f) * Vector3.forward;
    }
    
    /// <summary>
    /// Get right direction on XZ plane
    /// </summary>
    public Vector3 GetFlatRight()
    {
        return Quaternion.Euler(0f, horizontalAngle, 0f) * Vector3.right;
    }
    
    private void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying || target == null) return;
        
        Vector3 targetPos = target.position + targetOffset;
        
        // Target position
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(targetPos, 0.2f);
        
        // Camera-to-target line
        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, targetPos);
        
        // Collision sphere path
        if (enableCollision)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Quaternion rotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0f);
            Vector3 direction = rotation * Vector3.back;
            Gizmos.DrawLine(targetPos, targetPos + direction * distance);
            Gizmos.DrawWireSphere(targetPos + direction * currentDistance, collisionRadius);
        }
        
        // Current look input indicator
        if (lookInput.sqrMagnitude > 0.01f)
        {
            Gizmos.color = Color.green;
            Vector3 inputIndicator = transform.position + transform.right * lookInput.x * 0.5f + transform.up * lookInput.y * 0.5f;
            Gizmos.DrawLine(transform.position, inputIndicator);
        }
    }
}
