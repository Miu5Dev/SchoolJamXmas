using UnityEngine;

public class SlopeSlideController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private float minSlideAngle = 46f;
    [SerializeField] private string slideTag = "Slide";
    
    [Header("Slide Speed")]
    [SerializeField] private float slideGravityForce = 15f;
    [SerializeField] private float maxSlideSpeed = 15f;
    
    [Header("Momentum Settings")]
    [SerializeField] private float minSpeedToResistSlide = 3f;
    [SerializeField] private float momentumDecayRate = 8f;
    [SerializeField] private float slideAccelerationRate = 5f;
    
    [Header("Slide Delay")]
    [SerializeField] private float slideActivationDelay = 0.15f;
    
    [Header("Control")]
    [SerializeField] [Range(0f, 1f)] private float controlWhileSliding = 0.3f;
    
    [Header("Direction Smoothing")]
    [SerializeField] private float directionSmoothSpeed = 5f;
    
    [Header("Debug")]
    [SerializeField] private bool isOnSlide = false;
    [SerializeField] private bool isBeingPushedDown = false;
    [SerializeField] private float currentSlopeAngle = 0f;
    [SerializeField] private Vector3 slideDirection = Vector3.zero;
    [SerializeField] private Vector3 smoothedSlideDirection = Vector3.zero;
    [SerializeField] private Vector3 combinedSlideDirection = Vector3.zero;
    [SerializeField] private int slideHitCount = 0;
    [SerializeField] private int totalHitCount = 0;
    [SerializeField] private bool allHitsAreSlide = false;
    [SerializeField] private float slideContactTime = 0f;
    [SerializeField] private bool slideDelayPassed = false;
    [SerializeField] private float currentPlayerSpeed = 0f;
    [SerializeField] private Vector3 currentPlayerDirection = Vector3.zero;
    [SerializeField] private float dotWithSlide = 0f;
    
    private Vector3 slopeNormal = Vector3.up;
    private bool grounded = false;
    
    void OnEnable()
    {
        EventBus.Subscribe<OnPlayerSlopeEvent>(OnSlope);
        EventBus.Subscribe<OnPlayerGroundedEvent>(OnGrounded);
        EventBus.Subscribe<OnPlayerAirborneEvent>(OnAirborne);
        EventBus.Subscribe<OnPlayerMoveEvent>(OnPlayerMove);
        EventBus.Subscribe<OnPlayerLaunchEvent>(OnPlayerLaunch);
    }
    
    void OnDisable()
    {
        EventBus.Unsubscribe<OnPlayerSlopeEvent>(OnSlope);
        EventBus.Unsubscribe<OnPlayerGroundedEvent>(OnGrounded);
        EventBus.Unsubscribe<OnPlayerAirborneEvent>(OnAirborne);
        EventBus.Unsubscribe<OnPlayerMoveEvent>(OnPlayerMove);
        EventBus.Unsubscribe<OnPlayerLaunchEvent>(OnPlayerLaunch);
    }
    
    private void OnPlayerMove(OnPlayerMoveEvent ev)
    {
        currentPlayerSpeed = ev.speed;
        currentPlayerDirection = new Vector3(ev.Direction.x, 0f, ev.Direction.y).normalized;
    }
    
    private void OnPlayerLaunch(OnPlayerLaunchEvent ev)
    {
        // Cuando el jugador se lanza, salir del estado de slide
        if (isOnSlide)
        {
            isOnSlide = false;
            isBeingPushedDown = false;
            slideContactTime = 0f;
            slideDelayPassed = false;
            smoothedSlideDirection = Vector3.zero;
            
            EventBus.Raise<OnPlayerSlideStateEvent>(new OnPlayerSlideStateEvent()
            {
                Player = gameObject,
                IsSliding = false,
                IsBeingPushedDown = false,
                ControlMultiplier = 1f,
                SlideDirection = Vector3.zero,
                TargetSpeedGain = 0f,
                MomentumDecay = 0f,
                MaxSlideSpeed = maxSlideSpeed
            });
        }
    }
    
    private void OnSlope(OnPlayerSlopeEvent ev)
    {
        slopeNormal = ev.SlopeNormal;
        currentSlopeAngle = ev.SlopeAngle;
        combinedSlideDirection = ev.CombinedSlideDirection;
        slideHitCount = ev.SlideHitCount;
        totalHitCount = ev.TotalHitCount;
        allHitsAreSlide = ev.AllHitsAreSlide;
    }
    
    private void OnGrounded(OnPlayerGroundedEvent ev)
    {
        grounded = true;
    }
    
    private void OnAirborne(OnPlayerAirborneEvent ev)
    {
        grounded = false;
        
        if (isOnSlide)
        {
            isOnSlide = false;
            isBeingPushedDown = false;
            slideContactTime = 0f;
            slideDelayPassed = false;
            
            EventBus.Raise<OnPlayerSlideStateEvent>(new OnPlayerSlideStateEvent()
            {
                Player = gameObject,
                IsSliding = false,
                IsBeingPushedDown = false,
                ControlMultiplier = 1f,
                SlideDirection = Vector3.zero,
                TargetSpeedGain = 0f,
                MomentumDecay = 0f,
                MaxSlideSpeed = maxSlideSpeed
            });
        }
    }
    
    void Update()
    {
        HandleSliding();
    }
    
    private void HandleSliding()
    {
        bool shouldBeOnSlide = grounded && allHitsAreSlide && totalHitCount > 0;
        
        if (shouldBeOnSlide)
        {
            slideContactTime += Time.deltaTime;
            
            if (!slideDelayPassed)
            {
                if (slideContactTime >= slideActivationDelay)
                {
                    slideDelayPassed = true;
                }
                else
                {
                    return;
                }
            }
            
            bool wasOnSlide = isOnSlide;
            isOnSlide = true;
            
            // Calcular dirección del slide (hacia abajo de la pendiente)
            Vector3 downhillDirection;
            if (slideHitCount > 1 && combinedSlideDirection.magnitude > 0.1f)
            {
                downhillDirection = combinedSlideDirection;
            }
            else
            {
                downhillDirection = Vector3.ProjectOnPlane(Vector3.down, slopeNormal).normalized;
            }
            
            // Suavizar dirección
            if (!wasOnSlide)
            {
                smoothedSlideDirection = downhillDirection;
            }
            else
            {
                smoothedSlideDirection = Vector3.Lerp(
                    smoothedSlideDirection, 
                    downhillDirection, 
                    directionSmoothSpeed * Time.deltaTime
                ).normalized;
            }
            
            slideDirection = smoothedSlideDirection;
            
            // === LÓGICA DE MOMENTUM ===
            Vector3 horizontalSlideDir = new Vector3(slideDirection.x, 0f, slideDirection.z).normalized;
            dotWithSlide = Vector3.Dot(currentPlayerDirection, horizontalSlideDir);
            
            // Calcular la fuerza de la pendiente basada en el ángulo
            float slopeStrength = Mathf.InverseLerp(minSlideAngle, 90f, currentSlopeAngle);
            float requiredSpeedToResist = Mathf.Lerp(minSpeedToResistSlide, minSpeedToResistSlide * 2f, slopeStrength);
            
            float speedGain = 0f;
            float momentumDecay = 0f;
            
            if (dotWithSlide >= -0.1f)
            {
                // Yendo cuesta abajo o perpendicular - acelerar con gravedad
                isBeingPushedDown = true;
                speedGain = slideAccelerationRate * (1f + slopeStrength);
            }
            else
            {
                // Yendo cuesta arriba - verificar si tiene suficiente momentum
                float effectiveMomentum = currentPlayerSpeed * Mathf.Abs(dotWithSlide);
                
                if (effectiveMomentum > requiredSpeedToResist)
                {
                    // Tiene suficiente momentum - mantiene dirección pero pierde velocidad
                    isBeingPushedDown = false;
                    momentumDecay = momentumDecayRate * (1f + slopeStrength);
                    speedGain = 0f;
                }
                else
                {
                    // No tiene suficiente momentum - la gravedad gana
                    isBeingPushedDown = true;
                    speedGain = slideAccelerationRate * slopeStrength;
                }
            }
            
            EventBus.Raise<OnPlayerSlideStateEvent>(new OnPlayerSlideStateEvent()
            {
                Player = gameObject,
                IsSliding = true,
                IsBeingPushedDown = isBeingPushedDown,
                ControlMultiplier = controlWhileSliding,
                SlideDirection = slideDirection,
                TargetSpeedGain = speedGain,
                MomentumDecay = momentumDecay,
                MaxSlideSpeed = maxSlideSpeed
            });
        }
        else
        {
            slideContactTime = 0f;
            slideDelayPassed = false;
            
            if (isOnSlide)
            {
                isOnSlide = false;
                isBeingPushedDown = false;
                smoothedSlideDirection = Vector3.zero;
                
                EventBus.Raise<OnPlayerSlideStateEvent>(new OnPlayerSlideStateEvent()
                {
                    Player = gameObject,
                    IsSliding = false,
                    IsBeingPushedDown = false,
                    ControlMultiplier = 1f,
                    SlideDirection = Vector3.zero,
                    TargetSpeedGain = 0f,
                    MomentumDecay = 0f,
                    MaxSlideSpeed = maxSlideSpeed
                });
                
                EventBus.Raise<OnPlayerStopSlidingEvent>(new OnPlayerStopSlidingEvent()
                {
                    Player = gameObject
                });
            }
        }
    }
}