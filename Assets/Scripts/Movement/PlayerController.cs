using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class PlayerController : MonoBehaviour
{
    /// <summary>
    /// MOVEMENT VARIABLES
    /// </summary>
    [Header("Obligatory")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform cameraTransform; // Referencia a la cámara
    
    [Header("Speed Values")]
    [SerializeField] private float minSpeed = 1.0f;
    [SerializeField] private float currentSpeed = 1.0f;
    [SerializeField] private float speedGain = 0.2f;
    [SerializeField] private float speedLose = 0.05f;
    [SerializeField] private float maxSpeed = 3.0f;
    
    [Header("On Slope Movement Rotation Settings")]
    [SerializeField] private float rotationSmoothSpeed = 8f;
    
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 720f; // Grados por segundo (más rápido = más responsivo)
    [SerializeField] private float rotationSpeedWhenSlow = 360f; // Rotación cuando está parado/lento
    [SerializeField] private float minimumSpeedToRotate = 0.1f; // Velocidad mínima para rotar
    
    [Header("Movement Style")]
    [SerializeField] private float movementBlendSpeed = 8f; // Qué tan rápido se adapta la dirección de movimiento
    [SerializeField] private bool useArcMovement = true; // Si false, movimiento más directo
    
    [Header("Gravity Values")]
    [SerializeField] private float Gravity = -9.81f;
    
    [Header("Inputs Handler")]
    [SerializeField] private bool isCrouching = false;
    [SerializeField] private bool isAction = false;
    [SerializeField] private bool isSwap = false;
    [SerializeField] private bool isJumping = false;
    
    [Header("DEBUG")]
    [SerializeField] private Vector3 moveDirection = Vector3.zero;
    [SerializeField] private Vector3 targetMoveDirection = Vector3.zero;
    [SerializeField] private Vector2 inputDirection = Vector2.zero;
    [SerializeField] private Vector3 SlopeNormal = Vector3.zero;

    /// <summary>
    /// PRIVATE VARIABLES
    /// </summary>
    private bool RisingSpeed = false;
    
    void Awake()
    {
        controller = GetComponent<CharacterController>();
        
        // Auto-asignar la cámara si no está asignada
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }
    
    void OnEnable()
    {
        EventBus.Subscribe<OnMoveInputEvent>(MoveInputUpdater);
        EventBus.Subscribe<OnJumpInputEvent>(JumpToggle);
        EventBus.Subscribe<OnActionInputEvent>(ActionToggle);
        EventBus.Subscribe<OnCrouchInputEvent>(CrouchToggle);
        EventBus.Subscribe<OnSwapInputEvent>(SwapToggle);
        
        EventBus.Subscribe<OnPlayerSlopeEvent>(OnSlope);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<OnMoveInputEvent>(MoveInputUpdater);
        EventBus.Unsubscribe<OnJumpInputEvent>(JumpToggle);
        EventBus.Unsubscribe<OnActionInputEvent>(ActionToggle);
        EventBus.Unsubscribe<OnCrouchInputEvent>(CrouchToggle);
        EventBus.Unsubscribe<OnSwapInputEvent>(SwapToggle);
        
        EventBus.Unsubscribe<OnPlayerSlopeEvent>(OnSlope);
    }
    private void Update()
    {
        controller.Move(new Vector3(0, Gravity, 0) * Time.deltaTime); // add gravity
        SpeedController();
        CalculateCameraRelativeMovement();
        RotatePlayer();
        MovementController();
    }



    private void OnSlope(OnPlayerSlopeEvent ev)
    {
        Quaternion targetRotation;

        if (ev.SlopeNormal != Vector3.zero)
        {
            // Obtener la rotación actual en el plano Y (yaw)
            Vector3 forward = transform.forward;
            Vector3 forwardProjected = Vector3.ProjectOnPlane(forward, ev.SlopeNormal).normalized;
        
            // Si la proyección es muy pequeña, mantener el forward actual
            if (forwardProjected.magnitude < 0.1f)
            {
                forwardProjected = Vector3.ProjectOnPlane(Vector3.forward, ev.SlopeNormal).normalized;
            }
        
            // Crear rotación que mira en la dirección forward preservada con el up del slope
            targetRotation = Quaternion.LookRotation(forwardProjected, ev.SlopeNormal);
        }
        else
        {
            // Volver a la rotación plana preservando el Y
            Vector3 currentForward = Vector3.ProjectOnPlane(transform.forward, transform.up).normalized;
            targetRotation = Quaternion.LookRotation(currentForward, transform.up);
        }

        // Suavizar la rotación
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothSpeed);

        SlopeNormal = ev.SlopeNormal;
    }
    
    private void CalculateCameraRelativeMovement()
    {
        // Solo actualizar la dirección objetivo cuando hay input
        if (inputDirection.magnitude > 0.1f)
        {
            // Obtener la dirección forward y right de la cámara
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;
            
            // Ignorar el componente Y para movimiento en plano horizontal
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();
            
            // Calcular dirección objetivo relativa a la cámara
            targetMoveDirection = (cameraForward * inputDirection.y + cameraRight * inputDirection.x);
            targetMoveDirection.Normalize();
        }
        
        // Blend suave hacia la dirección objetivo (esto da la sensación de curvas amplias)
        if (useArcMovement && moveDirection.magnitude > 0.01f)
        {
            // Cuando hay momentum, hacer blend suave (esto crea las curvas amplias)
            moveDirection = Vector3.Lerp(moveDirection, targetMoveDirection, movementBlendSpeed * Time.deltaTime);
            moveDirection.Normalize();
        }
        else if (targetMoveDirection.magnitude > 0.1f)
        {
            // Sin momentum o movimiento desactivado, usar dirección directa
            moveDirection = targetMoveDirection;
        }
    }

    private void RotatePlayer()
    {
        // Solo rotar si hay una dirección de movimiento
        if (moveDirection.magnitude > 0.1f)
        {
            // Crear una rotación hacia la dirección de movimiento
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            
            // Determinar velocidad de rotación basada en la velocidad actual
            // Más rápido = rotación más rápida (como Mario Odyssey)
            float speedFactor = Mathf.Clamp01(currentSpeed / maxSpeed);
            float currentRotationSpeed = Mathf.Lerp(rotationSpeedWhenSlow, rotationSpeed, speedFactor);
            
            // Interpolar suavemente hacia la rotación objetivo
            float rotationStep = currentRotationSpeed * Time.deltaTime;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationStep);
        }
    }

    private void SpeedController()
    {
        if (RisingSpeed)
        {
            if (currentSpeed > maxSpeed)
                currentSpeed -= speedLose;
            else if (currentSpeed < maxSpeed)
            {
                currentSpeed += speedGain;
                currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);
            }
        }
        else
        {
            if (currentSpeed > minSpeed)
            {
                currentSpeed -= speedLose;
                currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);
            }
        }
    }

    private void MovementController()
    {
        // do the actual moving
        if (moveDirection.magnitude > 0.1)
        {
            EventBus.Raise<OnPlayerMoveEvent>(new OnPlayerMoveEvent()
            {
                Player = this.gameObject,
                Direction = new Vector2(moveDirection.x, moveDirection.z),
                Rotation = Quaternion.LookRotation(moveDirection),
                speed = currentSpeed,
            });
        }
        else if (moveDirection.magnitude < 0.1 && currentSpeed < minSpeed)
        {
            EventBus.Raise<OnPlayerStopEvent>(new OnPlayerStopEvent()
            {
                Player = this.gameObject
            });
        }
        
        if(SlopeNormal != Vector3.zero)
            moveDirection = Vector3.ProjectOnPlane(moveDirection,SlopeNormal);
        
        controller.Move((moveDirection) * (currentSpeed * Time.deltaTime)); // Actually Move the character
    }

    private void JumpToggle(OnJumpInputEvent e)
    {
        isJumping = e.pressed;
    }
    
    private void ActionToggle(OnActionInputEvent e)
    {
        isAction = e.pressed;
    }

    private void SwapToggle(OnSwapInputEvent e)
    {
        isSwap = e.pressed;
    }
    
    private void CrouchToggle(OnCrouchInputEvent e)
    {
        isCrouching = e.pressed;
    }
    
    private void MoveInputUpdater(OnMoveInputEvent e)
    {
        if (e.Direction.magnitude > 0.1f)
        {
            inputDirection = e.Direction;
            RisingSpeed = true;
        }
        else
        {
            inputDirection = Vector2.zero;
            RisingSpeed = false;
        }
    }

}