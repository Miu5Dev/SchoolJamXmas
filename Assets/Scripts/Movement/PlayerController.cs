using System.Collections;
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
    
    [Header("Slide State")]
    [SerializeField] private bool isSliding = false;
    [SerializeField] private float slideControlMultiplier = 1f;
    [SerializeField] private Vector3 slideDirection = Vector3.zero;
    [SerializeField] private float slideTargetSpeed = 0f;
    [SerializeField] private float slideAcceleration = 0f;
    
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
    [SerializeField] private bool isInHangTime = false; // NUEVO
    [SerializeField] private Vector3 inputMoveDirection = Vector3.zero; // NUEVO - dirección pura del input

    

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
        EventBus.Subscribe<OnApplyJumpForceCommand>(OnApplyJumpForce); // NUEV
        EventBus.Subscribe<OnSetHangTimeState>(OnSetHangTimeState); // NUEVO
        EventBus.Subscribe<OnRotatePlayerCommand>(OnRotatePlayer); // NUEVO
        EventBus.Subscribe<OnPlayerSlideStateEvent>(OnSlideState);
        EventBus.Subscribe<OnPlayerStopSlidingEvent>(OnStopSliding);

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
        EventBus.Unsubscribe<OnApplyJumpForceCommand>(OnApplyJumpForce); // NUEVO
        EventBus.Unsubscribe<OnSetHangTimeState>(OnSetHangTimeState); // NUEVO
        EventBus.Unsubscribe<OnRotatePlayerCommand>(OnRotatePlayer); // NUEVO
        EventBus.Unsubscribe<OnPlayerSlideStateEvent>(OnSlideState);
        EventBus.Unsubscribe<OnPlayerStopSlidingEvent>(OnStopSliding);


    }
    
    private void OnSlideState(OnPlayerSlideStateEvent ev)
    {
        isSliding = ev.IsSliding;
        slideControlMultiplier = ev.ControlMultiplier;
        slideDirection = ev.SlideDirection;
        slideTargetSpeed = ev.TargetSpeed;
        slideAcceleration = ev.Acceleration;
    }

    private void OnStopSliding(OnPlayerStopSlidingEvent ev)
    {
        isSliding = false;
        slideControlMultiplier = 1f;
        slideDirection = Vector3.zero;
        slideTargetSpeed = 0f;
        slideAcceleration = 0f;
    }

    
    private void OnRotatePlayer(OnRotatePlayerCommand cmd) // NUEVO
    {
        // Si debe invertir la dirección del movimiento
        if (cmd.InvertMovementDirection)
        {
            // Invertir moveDirection y targetMoveDirection
            moveDirection = -moveDirection;
            targetMoveDirection = -targetMoveDirection;
        }
    }
    
    private void OnSetHangTimeState(OnSetHangTimeState state) // NUEVO
    {
        isInHangTime = state.IsInHangTime;
    }
    
    private void OnApplyJumpForce(OnApplyJumpForceCommand cmd) // NUEVO
    {
        verticalVelocity = cmd.Force;
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
    DirectionChanged = false;
    
    // NUEVO: Calcular inputMoveDirection SIEMPRE que haya input (antes de cualquier lógica de slide)
    if (inputDirection.magnitude > 0.1f)
    {
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;
        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();
        
        inputMoveDirection = (cameraForward * inputDirection.y + cameraRight * inputDirection.x).normalized;
    }
    else
    {
        inputMoveDirection = Vector3.zero;
    }
    
    // Si está deslizando
    if (isSliding)
    {
        Vector3 horizontalSlideDir = new Vector3(slideDirection.x, 0f, slideDirection.z).normalized;
        
        if (inputDirection.magnitude > 0.1f)
        {
            float dotAgainstSlide = Vector3.Dot(inputMoveDirection, -horizontalSlideDir);
            
            if (dotAgainstSlide > 0)
            {
                float playerStrength = maxSpeed;
                float slideStrength = slideTargetSpeed;
                
                float resistRatio = Mathf.Clamp01((playerStrength - slideStrength) / playerStrength);
                
                if (resistRatio <= 0)
                {
                    Vector3 perpendicularInput = inputMoveDirection - (dotAgainstSlide * -horizontalSlideDir);
                    
                    if (perpendicularInput.magnitude > 0.1f)
                    {
                        perpendicularInput.Normalize();
                        moveDirection = Vector3.Lerp(horizontalSlideDir, perpendicularInput, slideControlMultiplier).normalized;
                    }
                    else
                    {
                        moveDirection = horizontalSlideDir;
                    }
                }
                else
                {
                    Vector3 uphillComponent = dotAgainstSlide * -horizontalSlideDir * resistRatio;
                    Vector3 otherComponent = inputMoveDirection - (dotAgainstSlide * -horizontalSlideDir);
                    Vector3 adjustedInput = (uphillComponent + otherComponent).normalized;
                    
                    moveDirection = Vector3.Lerp(horizontalSlideDir, adjustedInput, slideControlMultiplier).normalized;
                }
            }
            else
            {
                moveDirection = Vector3.Lerp(horizontalSlideDir, inputMoveDirection, slideControlMultiplier).normalized;
            }
        }
        else
        {
            moveDirection = horizontalSlideDir;
        }
        
        moveDirection.y = 0f;
        if (moveDirection.magnitude > 0.1f)
        {
            moveDirection.Normalize();
        }
        
        targetMoveDirection = moveDirection;
        return;
    }
    
    // Código normal cuando NO está deslizando
    if (inputDirection.magnitude > 0.1f)
    {
        targetMoveDirection = inputMoveDirection;
        
        if (moveDirection.magnitude > 0.1f && targetMoveDirection.magnitude > 0.1f && currentSpeed > minSpeed + 0.5f)
        {
            float angleChange = Vector3.Angle(moveDirection, targetMoveDirection);
            
            if (angleChange > directionChangeThreshold)
            {
                DirectionChanged = true;
                
                float penaltyFactor = Mathf.InverseLerp(directionChangeThreshold, 180f, angleChange);
                
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
        
        if (useArcMovement && moveDirection.magnitude > 0.01f)
        {
            moveDirection = Vector3.Lerp(moveDirection, targetMoveDirection, movementBlendSpeed * Time.deltaTime);
            moveDirection.Normalize();
        }
        else
        {
            moveDirection = targetMoveDirection;
        }
    }
}

private void RotatePlayer()
{
    Vector3 rotationDirection;
    
    if (inputMoveDirection.magnitude > 0.1f)
    {
        // Hay input: verificar si puede rotar hacia donde quiere
        if (isSliding)
        {
            Vector3 horizontalSlideDir = new Vector3(slideDirection.x, 0f, slideDirection.z).normalized;
            float dotAgainstSlide = Vector3.Dot(inputMoveDirection, -horizontalSlideDir);
            
            // Si intenta mirar hacia arriba del slope
            if (dotAgainstSlide > 0)
            {
                float playerStrength = maxSpeed;
                float slideStrength = slideTargetSpeed;
                float resistRatio = Mathf.Clamp01((playerStrength - slideStrength) / playerStrength);
                
                if (resistRatio <= 0)
                {
                    // No puede mirar hacia arriba, filtrar componente
                    Vector3 perpendicularInput = inputMoveDirection - (dotAgainstSlide * -horizontalSlideDir);
                    
                    if (perpendicularInput.magnitude > 0.1f)
                    {
                        rotationDirection = perpendicularInput.normalized;
                    }
                    else
                    {
                        // Input es puramente hacia arriba, usar dirección del slide
                        rotationDirection = horizontalSlideDir;
                    }
                }
                else
                {
                    // Puede mirar parcialmente hacia arriba
                    Vector3 uphillComponent = dotAgainstSlide * -horizontalSlideDir * resistRatio;
                    Vector3 otherComponent = inputMoveDirection - (dotAgainstSlide * -horizontalSlideDir);
                    rotationDirection = (uphillComponent + otherComponent).normalized;
                }
            }
            else
            {
                // Mira hacia abajo o perpendicular, permitir
                rotationDirection = inputMoveDirection;
            }
        }
        else
        {
            // No está en slide, rotar libremente hacia el input
            rotationDirection = inputMoveDirection;
        }
    }
    else
    {
        // Sin input: rotar hacia donde se mueve
        rotationDirection = moveDirection;
    }
    
    if (rotationDirection.magnitude > 0.1f)
    {
        Quaternion targetRotation = Quaternion.LookRotation(rotationDirection);
        
        float speedFactor = Mathf.InverseLerp(0f, maxSpeed, currentSpeed);
        speedFactor = Mathf.Clamp01(speedFactor);
        
        float currentRotationSpeed = Mathf.Lerp(rotationSpeedWhenSlow, rotationSpeed, speedFactor);
        
        float rotationStep = currentRotationSpeed * Time.deltaTime;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationStep);
    }
}

    private void SpeedController()
    {
        float moveDirectionMultiplier = Mathf.Clamp(inputDirection.magnitude, 0.1f, 1f);
    
        // Si está deslizando, acelerar hacia la velocidad del slide
        if (isSliding)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, slideTargetSpeed, slideAcceleration * Time.deltaTime);
            return;
        }
    
        // Código normal cuando NO está deslizando
        if (grounded)
        {
            if (RisingSpeed)
            {
                if (currentSpeed > maxSpeed)
                    currentSpeed -= speedLose;
                else if (currentSpeed < maxSpeed)
                {
                    currentSpeed += speedGain * moveDirectionMultiplier;
                    currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);
                }
            }
            else
            {
                if (currentSpeed > minSpeed)
                {
                    currentSpeed -= speedLose;
                    if (currentSpeed <= maxSpeed)
                    {
                        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);
                    }
                }
            }
        }
        else
        {
            if (RisingSpeed)
            {
                if (currentSpeed > maxSpeed)
                    currentSpeed -= speedLose;
                else if (currentSpeed < maxSpeed)
                {
                    currentSpeed += (speedGain * moveDirectionMultiplier) / AirDivider;
                    currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);
                }
            }
            else
            {
                if (currentSpeed > minSpeed)
                {
                    currentSpeed -= speedLose;
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
    
    private void ExecuteJump(JumpTypeCreator jumpType)
    {
        float jumpSpeed;
        
        // NUEVO: Resetear velocidad si el salto lo requiere
        if (jumpType.resetSpeedOnJump)
        {
            currentSpeed = 0f;
        }
        
        // Si tiene hang time, iniciar con velocidad 0
        if (jumpType.hangTime > 0f)
        {
            jumpSpeed = 0f;
        }
        // Saltos hacia abajo sin hang time
        else if (jumpType.jumpForce <= 0)
        {
            jumpSpeed = jumpType.jumpForce * 10f;
        }
        // Saltos normales
        else
        {
            jumpSpeed = Mathf.Sqrt(jumpType.jumpForce * -2f * Gravity);
        }
        
        // Detectar slope
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
        
        // CAMBIO: Solo aplicar gravedad si NO está en hang time
        if (Time.time > lastJumpTime + 0.05f && !isInHangTime)
        {
            verticalVelocity += Gravity * Time.deltaTime;
        }
        
        Vector3 verticalMovement = new Vector3(0, verticalVelocity, 0);
        controller.Move(verticalMovement * Time.deltaTime);
    }
}