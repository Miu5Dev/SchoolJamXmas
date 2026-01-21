using UnityEngine;

/// <summary>
/// Third-person camera controller for Mario Odyssey-style gameplay.
/// Fixed to properly handle continuous controller input.
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
    [SerializeField] private float controllerSensitivity = 100f; // For continuous controller input
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
    
    private float horizontalAngle;
    private float verticalAngle = 20f;
    private Vector3 currentVelocity;
    private Vector2 lookInput;
    private bool hasLookInput;
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
        lookInput = ev.Delta;
        hasLookInput = ev.pressed || ev.Delta.sqrMagnitude > 0.0001f;
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
        if (!hasLookInput && lookInput.sqrMagnitude < 0.0001f) return;
        
        // Determine if input is from mouse (large delta) or controller (small continuous)
        float inputMagnitude = lookInput.magnitude;
        bool isControllerInput = inputMagnitude < 10f && inputMagnitude > 0.01f;
        
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
        
        // Don't reset lookInput here - it's updated every frame by InputSystem
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
        Vector3 desiredPosition = targetPosition - (rotation * Vector3.forward * distance);
        
        // Handle camera collision
        float finalDistance = distance;
        if (enableCollision)
        {
            finalDistance = HandleCameraCollision(targetPosition, rotation);
        }
        
        // Smooth distance transitions
        currentDistance = Mathf.Lerp(currentDistance, finalDistance, 10f * Time.deltaTime);
        
        // Calculate final position with smoothed distance
        desiredPosition = targetPosition - (rotation * Vector3.forward * currentDistance);
        
        // Smooth position only, not rotation
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, positionSmoothTime);
        
        // Direct rotation without smoothing
        transform.LookAt(targetPosition);
    }
    
    private float HandleCameraCollision(Vector3 targetPos, Quaternion rotation)
    {
        Vector3 direction = rotation * Vector3.back;
        
        if (Physics.SphereCast(targetPos, collisionRadius, direction, out RaycastHit hit, distance, collisionLayers))
        {
            return hit.distance - collisionRadius;
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
}
