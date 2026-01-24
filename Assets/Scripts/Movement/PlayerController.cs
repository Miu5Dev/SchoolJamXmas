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
    [SerializeField] private float AirDivider = 8f;
    
    [Header("Jump Values")]
    [SerializeField] private float jumpCooldown = 0.2f; // Tiempo antes de poder detectar grounded otra vez
    [SerializeField] private float noSlopeProjectionTime = 0.15f; // Tiempo sin proyección después de saltar
    
    [Header("On Slope Movement Rotation Settings")]
    [SerializeField] private float rotationSmoothSpeed = 8f;
    
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 720f; // Grados por segundo (más rápido = más responsivo)
    [SerializeField] private float rotationSpeedWhenSlow = 360f; // Rotación cuando está parado/lento
    [SerializeField] private float minimumSpeedToRotate = 0.1f; // Velocidad mínima para rotar
    
    [Header("Movement Style")]
    [SerializeField] private float movementBlendSpeed = 8f; // Qué tan rápido se adapta la dirección de movimiento
    [SerializeField] private bool useArcMovement = true; // Si false, movimiento más directo
    [SerializeField] private float directionChangePenalty = 0.5f; // Cuánto reduces velocidad al cambiar de dirección (0-1)
    [SerializeField] private float directionChangeThreshold = 90f; // Ángulo mínimo para considerar "cambio de dirección"

    
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
    [SerializeField] public float verticalVelocity = 0f;
    [SerializeField] private bool grounded = false;
    [SerializeField] private bool DirectionChanged = false; // NUEVO

    /// <summary>
    /// PRIVATE VARIABLES
    /// </summary>
    private bool RisingSpeed = false;
    private float lastJumpTime = -1f; // Cuando saltó por última vez

    
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
        EventBus.Subscribe<OnPlayerGroundedEvent>(OnGrounded);
        EventBus.Subscribe<OnPlayerAirborneEvent>(OnAirborne);
        EventBus.Subscribe<OnExecuteJumpCommand>(OnExecuteJumpCommand); // NUEVO
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<OnMoveInputEvent>(MoveInputUpdater);
        EventBus.Unsubscribe<OnJumpInputEvent>(JumpToggle);
        EventBus.Unsubscribe<OnActionInputEvent>(ActionToggle);
        EventBus.Unsubscribe<OnCrouchInputEvent>(CrouchToggle);
        EventBus.Unsubscribe<OnSwapInputEvent>(SwapToggle);
        
        EventBus.Unsubscribe<OnPlayerSlopeEvent>(OnSlope);
        EventBus.Unsubscribe<OnPlayerGroundedEvent>(OnGrounded);
        EventBus.Unsubscribe<OnPlayerAirborneEvent>(OnAirborne);
        EventBus.Unsubscribe<OnExecuteJumpCommand>(OnExecuteJumpCommand); // NUEVO

    }
    
    private void OnExecuteJumpCommand(OnExecuteJumpCommand cmd)
    {
        ExecuteJump(cmd.JumpType);
    }
    
    private void Update()
    {
        SpeedController();
        CalculateCameraRelativeMovement();
        RotatePlayer();
        MovementController();
        GravityHandler();
    }

    private void OnGrounded(OnPlayerGroundedEvent ev)
    {
        grounded = true;
    }

    private void OnAirborne(OnPlayerAirborneEvent ev)
    {
        grounded = false;
    }

    private void OnSlope(OnPlayerSlopeEvent ev)
    {
        SlopeNormal = ev.SlopeNormal;
    }

    private void CalculateCameraRelativeMovement()
    {
        // IMPORTANTE: Reset del flag al inicio del frame
        DirectionChanged = false;
        
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
            
            // CAMBIO: Solo detectar cambio si realmente estás moviéndote con velocidad significativa
            if (moveDirection.magnitude > 0.1f && targetMoveDirection.magnitude > 0.1f && currentSpeed > minSpeed + 0.5f)
            {
                float angleChange = Vector3.Angle(moveDirection, targetMoveDirection);
                
                // Si el cambio de ángulo es significativo, reducir velocidad
                if (angleChange > directionChangeThreshold)
                {
                    DirectionChanged = true;
                    
                    // Cuanto mayor el ángulo, mayor la penalización
                    float penaltyFactor = Mathf.InverseLerp(directionChangeThreshold, 180f, angleChange);
                    
                    // Enviar evento
                    EventBus.Raise<OnDirectionChangeEvent>(new OnDirectionChangeEvent()
                    {
                        Player = this.gameObject,
                        AngleChange = angleChange,
                        OldDirection = moveDirection,
                        NewDirection = targetMoveDirection,
                        PenaltyFactor = penaltyFactor
                    });
                    
                    currentSpeed *= Mathf.Lerp(1f, directionChangePenalty, penaltyFactor);
                    currentSpeed = Mathf.Max(currentSpeed, minSpeed);
                }
            }
            
            // Solo hacer blend cuando HAY input
            if (useArcMovement && moveDirection.magnitude > 0.01f)
            {
                // Cuando hay momentum, hacer blend suave
                moveDirection = Vector3.Lerp(moveDirection, targetMoveDirection, movementBlendSpeed * Time.deltaTime);
                moveDirection.Normalize();
            }
            else
            {
                // Sin momentum, usar dirección directa
                moveDirection = targetMoveDirection;
            }
        }
        // Si no hay input, moveDirection se mantiene como está
    }
    private void RotatePlayer()
    {
        // Solo rotar si hay una dirección de movimiento
        if (moveDirection.magnitude > 0.1f)
        {

            // Crear una rotación hacia la dirección de movimiento
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

            // Determinar velocidad de rotación basada en la velocidad actual
            // Usar InverseLerp para normalizar correctamente incluso con momentum
            float speedFactor = Mathf.InverseLerp(0f, maxSpeed, currentSpeed);
            speedFactor = Mathf.Clamp01(speedFactor); // Opcional: limitar a 1.0 máximo

            float currentRotationSpeed = Mathf.Lerp(rotationSpeedWhenSlow, rotationSpeed, speedFactor);

            // Interpolar suavemente hacia la rotación objetivo
            float rotationStep = currentRotationSpeed * Time.deltaTime;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationStep);
        }
    }

    private void SpeedController()
    {
        if (grounded)
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
                    // CAMBIO: Solo clampear si ya estás dentro del rango normal
                    if (currentSpeed <= maxSpeed)
                    {
                        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);
                    }
                }
            }
        }
        else //if airborne
        {
            if (RisingSpeed)
            {
                if (currentSpeed > maxSpeed)
                    currentSpeed -= speedLose;
                else if (currentSpeed < maxSpeed)
                {
                    currentSpeed += speedGain/AirDivider;
                    currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);
                }
            }
            else
            {
                if (currentSpeed > minSpeed)
                {
                    currentSpeed -= speedLose;
                    // CAMBIO: Solo clampear si ya estás dentro del rango normal
                    if (currentSpeed <= maxSpeed)
                    {
                        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);
                    }
                }
            }
        }
    }

    private void MovementController()
    {
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
    
        Vector3 finalMoveDirection = moveDirection;
    
        // CAMBIO: No proyectar sobre slope justo después de saltar
        bool recentlyJumped = Time.time < lastJumpTime + noSlopeProjectionTime;
    
        if(SlopeNormal != Vector3.zero && moveDirection.magnitude > 0.1 && grounded && !recentlyJumped)
            finalMoveDirection = Vector3.ProjectOnPlane(moveDirection, SlopeNormal);
    
        controller.Move(finalMoveDirection * (currentSpeed * Time.deltaTime));
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
    
    public void ExecuteJump(JumpTypeCreator jumpType)
    {
        // Calcular velocidad de salto
        float jumpSpeed = Mathf.Sqrt(jumpType.jumpForce * -2f * Gravity);
        
        // Detectar si está en un slope
        if (SlopeNormal != Vector3.zero && SlopeNormal != Vector3.up && !jumpType.ignoreUphillPenalty)
        {
            Vector3 slopeUpDirection = Vector3.ProjectOnPlane(Vector3.up, SlopeNormal).normalized;
            float upwardMovement = Vector3.Dot(moveDirection, slopeUpDirection);
            bool isGoingUphill = upwardMovement > 0.5f;
            
            verticalVelocity = jumpSpeed;
            
            if (isGoingUphill)
            {
                currentSpeed = Mathf.Min(currentSpeed * jumpType.uphillSpeedMultiplier, maxSpeed);
            }
            else
            {
                currentSpeed += jumpType.extraSpeed;
            }
        }
        else
        {
            verticalVelocity = jumpSpeed;
            currentSpeed += jumpType.extraSpeed;
        }
        
        EventBus.Raise<OnPlayerJumpEvent>(new OnPlayerJumpEvent()
        {
            Player = this.gameObject,
            accelerationMultiplier = jumpType.extraSpeed,
            JumpType = jumpType.jumpType,
            JumpForce = jumpType.jumpForce,
        });
        
        lastJumpTime = Time.time;
        grounded = false;
    }

    private void GravityHandler()
    {
        bool canLand = Time.time > lastJumpTime + jumpCooldown;
    
        if (grounded && verticalVelocity < 0 && canLand)
        {
            verticalVelocity = Mathf.Clamp(verticalVelocity, -9.81f, 1000);
        }
    
        // Solo aplicar gravedad si NO acabas de saltar O si ya pasó tiempo suficiente
        if (Time.time > lastJumpTime + 0.05f) // Pequeño delay para dar fuerza al salto
        {
            verticalVelocity += Gravity * Time.deltaTime;
        }
    
        Vector3 verticalMovement = new Vector3(0, verticalVelocity, 0);
        controller.Move(verticalMovement * Time.deltaTime);
    }
}