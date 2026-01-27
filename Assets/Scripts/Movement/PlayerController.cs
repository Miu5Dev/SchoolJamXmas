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
    [SerializeField] private Transform cameraTransform;
    
    [Header("Speed Values")]
    [SerializeField] private float minSpeed = 0.0f;
    [SerializeField] private float currentSpeed = 1.0f;
    [SerializeField] private float speedGain = 0.2f;
    [SerializeField] private float speedLose = 0.05f;
    [SerializeField] private float maxSpeed = 8.0f;
    [SerializeField] private float maxCrouchingSpeed = 8.0f;
    [SerializeField] private float AirDivider = 8f;
    [SerializeField] private float CrouchDivider = 8f;
    
    [Header("Jump Values")]
    [SerializeField] private float jumpCooldown = 0.2f;
    [SerializeField] private float noSlopeProjectionTime = 0.15f;
    
    [Header("On Slope Movement Rotation Settings")]
    [SerializeField] private float rotationSmoothSpeed = 8f;
    
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private float rotationSpeedWhenSlow = 360f;
    [SerializeField] private float minimumSpeedToRotate = 0.1f;
    
    [Header("Movement Style")]
    [SerializeField] private float movementBlendSpeed = 8f;
    [SerializeField] private bool useArcMovement = true;
    [SerializeField] private float directionChangePenalty = 0.5f;
    [SerializeField] private float directionChangeThreshold = 90f;
    
    [Header("Slide State")]
    [SerializeField] private bool isSliding = false;
    [SerializeField] private bool isBeingPushedDown = false;
    [SerializeField] private float slideControlMultiplier = 1f;
    [SerializeField] private Vector3 slideDirection = Vector3.zero;
    [SerializeField] private float slideSpeedGain = 0f;
    [SerializeField] private float slideMomentumDecay = 0f;
    [SerializeField] private float slideMaxSpeed = 15f;
    
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
    [SerializeField] private float currentSlopeAngle = 0f;
    [SerializeField] public float verticalVelocity = 0f;
    [SerializeField] private bool grounded = false;
    [SerializeField] private bool DirectionChanged = false;
    [SerializeField] private bool isInHangTime = false;
    [SerializeField] private Vector3 inputMoveDirection = Vector3.zero;

    /// <summary>
    /// PRIVATE VARIABLES
    /// </summary>
    private bool RisingSpeed = false;
    private float lastJumpTime = -1f;

    
    void Awake()
    {
        controller = GetComponent<CharacterController>();
        
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
        EventBus.Subscribe<OnExecuteJumpCommand>(OnExecuteJumpCommand);
        EventBus.Subscribe<OnApplyJumpForceCommand>(OnApplyJumpForce);
        EventBus.Subscribe<OnSetHangTimeState>(OnSetHangTimeState);
        EventBus.Subscribe<OnRotatePlayerCommand>(OnRotatePlayer);
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
        EventBus.Unsubscribe<OnExecuteJumpCommand>(OnExecuteJumpCommand);
        EventBus.Unsubscribe<OnApplyJumpForceCommand>(OnApplyJumpForce);
        EventBus.Unsubscribe<OnSetHangTimeState>(OnSetHangTimeState);
        EventBus.Unsubscribe<OnRotatePlayerCommand>(OnRotatePlayer);
        EventBus.Unsubscribe<OnPlayerSlideStateEvent>(OnSlideState);
        EventBus.Unsubscribe<OnPlayerStopSlidingEvent>(OnStopSliding);
    }
    
    private void OnSlideState(OnPlayerSlideStateEvent ev)
    {
        isSliding = ev.IsSliding;
        isBeingPushedDown = ev.IsBeingPushedDown;
        slideControlMultiplier = ev.ControlMultiplier;
        slideDirection = ev.SlideDirection;
        slideSpeedGain = ev.TargetSpeedGain;
        slideMomentumDecay = ev.MomentumDecay;
        slideMaxSpeed = ev.MaxSlideSpeed;
    }

    private void OnStopSliding(OnPlayerStopSlidingEvent ev)
    {
        isSliding = false;
        isBeingPushedDown = false;
        slideControlMultiplier = 1f;
        slideDirection = Vector3.zero;
        slideSpeedGain = 0f;
        slideMomentumDecay = 0f;
        slideMaxSpeed = 15f;
    }

    private void OnRotatePlayer(OnRotatePlayerCommand cmd)
    {
        if (cmd.InvertMovementDirection)
        {
            moveDirection = -moveDirection;
            targetMoveDirection = -targetMoveDirection;
        }
    }
    
    private void OnSetHangTimeState(OnSetHangTimeState state)
    {
        isInHangTime = state.IsInHangTime;
    }
    
    private void OnApplyJumpForce(OnApplyJumpForceCommand cmd)
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
        currentSlopeAngle = ev.SlopeAngle;
    }

    private void CalculateCameraRelativeMovement()
    {
        DirectionChanged = false;
        
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
        
        // Lógica de sliding con momentum
        if (isSliding)
        {
            Vector3 horizontalSlideDir = new Vector3(slideDirection.x, 0f, slideDirection.z).normalized;
            
            if (isBeingPushedDown)
            {
                // La gravedad gana - comportamiento con control limitado
                if (inputDirection.magnitude > 0.1f)
                {
                    float dotAgainstSlide = Vector3.Dot(inputMoveDirection, -horizontalSlideDir);
                    
                    if (dotAgainstSlide > 0)
                    {
                        // Intentando ir cuesta arriba - solo permitir movimiento perpendicular
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
                        // Yendo a favor o perpendicular - mezclar con input
                        moveDirection = Vector3.Lerp(horizontalSlideDir, inputMoveDirection, slideControlMultiplier).normalized;
                    }
                }
                else
                {
                    moveDirection = horizontalSlideDir;
                }
            }
            else
            {
                // El momentum gana - el jugador mantiene su dirección
                if (inputDirection.magnitude > 0.1f)
                {
                    moveDirection = Vector3.Lerp(moveDirection, inputMoveDirection, movementBlendSpeed * Time.deltaTime).normalized;
                }
                // Si no hay input, mantiene la dirección actual (momentum)
            }
            
            moveDirection.y = 0f;
            if (moveDirection.magnitude > 0.1f)
            {
                moveDirection.Normalize();
            }
            
            targetMoveDirection = moveDirection;
            return;
        }
        
        // Lógica normal (no sliding)
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
            if (isSliding)
            {
                Vector3 horizontalSlideDir = new Vector3(slideDirection.x, 0f, slideDirection.z).normalized;
                float dotAgainstSlide = Vector3.Dot(inputMoveDirection, -horizontalSlideDir);
                
                float playerStrength = maxSpeed;
                float slideStrength = currentSpeed;
                float resistRatio = Mathf.Clamp01((playerStrength - slideStrength) / playerStrength);
                
                if (dotAgainstSlide > 0)
                {
                    if (resistRatio <= 0)
                    {
                        Vector3 perpendicularInput = inputMoveDirection - (dotAgainstSlide * -horizontalSlideDir);
                        
                        if (perpendicularInput.magnitude > 0.1f)
                        {
                            perpendicularInput.Normalize();
                            
                            float maxLateralAngle = 45f;
                            float lateralAngle = Vector3.SignedAngle(horizontalSlideDir, perpendicularInput, Vector3.up);
                            lateralAngle = Mathf.Clamp(lateralAngle, -maxLateralAngle, maxLateralAngle);
                            
                            rotationDirection = Quaternion.Euler(0f, lateralAngle, 0f) * horizontalSlideDir;
                        }
                        else
                        {
                            rotationDirection = horizontalSlideDir;
                        }
                    }
                    else
                    {
                        Vector3 uphillComponent = dotAgainstSlide * -horizontalSlideDir * resistRatio;
                        Vector3 otherComponent = inputMoveDirection - (dotAgainstSlide * -horizontalSlideDir);
                        rotationDirection = (uphillComponent + otherComponent).normalized;
                    }
                }
                else
                {
                    float maxLateralAngle = 45f;
                    float inputAngle = Vector3.SignedAngle(horizontalSlideDir, inputMoveDirection, Vector3.up);
                    inputAngle = Mathf.Clamp(inputAngle, -maxLateralAngle, maxLateralAngle);
                    
                    rotationDirection = Quaternion.Euler(0f, inputAngle, 0f) * horizontalSlideDir;
                }
            }
            else
            {
                rotationDirection = inputMoveDirection;
            }
        }
        else
        {
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
        
        // Lógica de sliding con momentum
        if (isSliding)
        {
            if (isBeingPushedDown)
            {
                // Acelerando cuesta abajo
                currentSpeed += slideSpeedGain * Time.deltaTime;
                currentSpeed = Mathf.Min(currentSpeed, slideMaxSpeed);
            }
            else
            {
                // Subiendo con momentum - pierde velocidad gradualmente
                currentSpeed -= slideMomentumDecay * Time.deltaTime;
                currentSpeed = Mathf.Max(currentSpeed, minSpeed);
            }
            return;
        }
        
        // Código normal cuando NO está deslizando
        if (grounded)
        {
            float maxSpeed = this.maxSpeed;
            float speedLose = this.speedLose;
            if (isCrouching)
            {
                maxSpeed = maxCrouchingSpeed;
                speedLose = this.speedLose/CrouchDivider;
            }

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
    
        bool recentlyJumped = Time.time < lastJumpTime + noSlopeProjectionTime;
        
    
        // Solo procesar slopes si estamos grounded y no saltamos/lanzamos recientemente
        if (SlopeNormal != Vector3.zero && SlopeNormal != Vector3.up && grounded && !recentlyJumped)
        {
            // Calcular si estamos subiendo la rampa
            Vector3 slopeUpDirection = Vector3.ProjectOnPlane(Vector3.up, SlopeNormal).normalized;
            float uphillDot = Vector3.Dot(moveDirection, slopeUpDirection);
            bool isGoingUphill = uphillDot > 0.3f;
            
            if (moveDirection.magnitude > 0.1f)
            {
                // Comportamiento normal - proyectar sobre la pendiente para pegarse al suelo
                finalMoveDirection = Vector3.ProjectOnPlane(moveDirection, SlopeNormal);
            }
        }
    
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
        
        if (jumpType.resetSpeedOnJump)
        {
            currentSpeed = 0f;
        }
        
        if (jumpType.hangTime > 0f)
        {
            jumpSpeed = 0f;
        }
        else if (jumpType.jumpForce <= 0)
        {
            jumpSpeed = jumpType.jumpForce * 10f;
        }
        else
        {
            jumpSpeed = Mathf.Sqrt(jumpType.jumpForce * -2f * Gravity);
        }
        
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
        
        if (Time.time > lastJumpTime + 0.05f && !isInHangTime)
        {
            verticalVelocity += Gravity * Time.deltaTime;
        }
        
        Vector3 verticalMovement = new Vector3(0, verticalVelocity, 0);
        controller.Move(verticalMovement * Time.deltaTime);
    }
}