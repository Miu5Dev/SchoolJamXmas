using UnityEngine;

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
    [SerializeField] private float minVerticalAngle = -20f;
    [SerializeField] private float maxVerticalAngle = 60f;
    
    [Header("Smoothing")]
    [SerializeField] private float positionSmoothTime = 0.05f;
    
    [Header("Offset")]
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);
    
    private float horizontalAngle;
    private float verticalAngle = 20f;
    private Vector3 currentVelocity;
    private Vector2 lookInput;

    void OnEnable()
    {
        EventBus.Subscribe<onLookInputEvent>(OnLookInput);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<onLookInputEvent>(OnLookInput);
    }

    private void OnLookInput(onLookInputEvent ev)
    {
        lookInput = ev.Delta;
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
        horizontalAngle += lookInput.x * rotationSensitivity;
        verticalAngle -= lookInput.y * rotationSensitivity;
        verticalAngle = Mathf.Clamp(verticalAngle, minVerticalAngle, maxVerticalAngle);
        
        lookInput = Vector2.zero;
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
        
        // Solo suaviza la posición, no la rotación
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, positionSmoothTime);
        
        // Rotación directa sin suavizado
        transform.LookAt(targetPosition);
    }
}