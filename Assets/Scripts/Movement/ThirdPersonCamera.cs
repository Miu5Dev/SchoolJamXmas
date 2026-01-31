using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target; // El jugador
    [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0); // Offset desde el centro del jugador
    
    [Header("Camera Distance")]
    [SerializeField] private float defaultDistance = 5f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float zoomSpeed = 2f;
    
    [Header("Camera Rotation")]
    [SerializeField] private float lookSensitivity = 0.5f; // Ajustado para input system
    [SerializeField] private float minVerticalAngle = -20f;
    [SerializeField] private float maxVerticalAngle = 80f;
    
    [Header("Camera Smoothing")]
    [SerializeField] private float rotationSmoothTime = 0.12f;
    [SerializeField] private float positionSmoothTime = 0.1f;
    
    [Header("Collision")]
    [SerializeField] private bool enableCollision = true;
    [SerializeField] private float collisionRadius = 0.3f;
    [SerializeField] private LayerMask collisionLayers = -1;
    
    // Private variables
    private float currentDistance;
    private float currentX = 0f;
    private float currentY = 20f;
    private Vector3 currentVelocity;
    private Vector3 targetPosition;
    
    private Vector2 lookDelta = Vector2.zero;

    public bool ableToMove = true;
    
    void Start()
    {
        currentDistance = defaultDistance;
        
        // Inicializar ángulos basados en la rotación actual de la cámara
        Vector3 angles = transform.eulerAngles;
        currentX = angles.y;
        currentY = angles.x;
        
        // Ocultar y bloquear el cursor (opcional - comentar si no quieres esto)
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
    }
    
    void OnEnable()
    {
        EventBus.Subscribe<OnLookInputEvent>(OnLookInput);
        EventBus.Subscribe<onDialogueOpen>(open => ableToMove = false);
        EventBus.Subscribe<onDialogueClose>(open => ableToMove = true);
    }
    
    void OnDisable()
    {
        EventBus.Unsubscribe<OnLookInputEvent>(OnLookInput);
        EventBus.Unsubscribe<onDialogueOpen>(open => ableToMove = false);
        EventBus.Unsubscribe<onDialogueClose>(open => ableToMove = true);
    }
    
    private void OnLookInput(OnLookInputEvent e)
    {
        lookDelta = e.Delta;
    }
    
    void LateUpdate()
    {
        if (target == null)
            return;
        
        HandleCameraRotation();
        HandleCameraZoom();
        CalculateCameraPosition();
        HandleCameraCollision();
        UpdateCameraPosition();
    }
    
    private void HandleCameraRotation()
    {
        // Aplicar el input de Look desde el evento
        float lookX = lookDelta.x * lookSensitivity;
        float lookY = lookDelta.y * lookSensitivity;
        
            currentX += lookX;
            currentY -= lookY;
        
        // Limitar ángulo vertical
        currentY = Mathf.Clamp(currentY, minVerticalAngle, maxVerticalAngle);
    }
    
    private void HandleCameraZoom()
    {
        // Zoom con la rueda del mouse
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        
        if (scrollInput != 0f)
        {
            currentDistance -= scrollInput * zoomSpeed;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
        }
    }
    
    private void CalculateCameraPosition()
    {
        // Calcular la rotación deseada
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        
        // Calcular la posición deseada basada en la rotación y distancia
        Vector3 direction = new Vector3(0, 0, -currentDistance);
        targetPosition = target.position + offset + rotation * direction;
    }
    
    private void HandleCameraCollision()
    {
        if (!enableCollision)
            return;
        
        Vector3 targetPoint = target.position + offset;
        Vector3 direction = targetPosition - targetPoint;
        float targetDistance = direction.magnitude;
        
        // Raycast para detectar colisiones
        RaycastHit hit;
        if (Physics.SphereCast(targetPoint, collisionRadius, direction.normalized, 
            out hit, targetDistance, collisionLayers))
        {
            // Si hay colisión, acercar la cámara
            targetPosition = hit.point + hit.normal * collisionRadius;
        }
    }
    
    private void UpdateCameraPosition()
    {
        if(!ableToMove) return;
        // Suavizar el movimiento de la cámara
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, 
            ref currentVelocity, positionSmoothTime);
        
        // Hacer que la cámara siempre mire al objetivo
        transform.LookAt(target.position + offset);
    }
    
    // Método público para cambiar el objetivo
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    // Método público para resetear la cámara detrás del jugador
    public void ResetCameraBehindPlayer()
    {
        if (target != null)
        {
            currentX = target.eulerAngles.y;
        }
    }
    
    // Método para habilitar/deshabilitar el cursor
    public void SetCursorLock(bool locked)
    {
        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}