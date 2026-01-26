using UnityEngine;

public class SlopeSlideController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private float minSlideAngle = 46f;
    [SerializeField] private string slideTag = "Slide";
    
    [Header("Slide Speed")]
    [SerializeField] private float slideSpeedGain = 5f;
    [SerializeField] private float maxSlideSpeed = 15f;
    
    [Header("Slide Delay")]
    [SerializeField] private float slideActivationDelay = 0.15f;
    
    [Header("Control")]
    [SerializeField] [Range(0f, 1f)] private float controlWhileSliding = 0.3f;
    
    [Header("Direction Smoothing")]
    [SerializeField] private float directionSmoothSpeed = 5f;
    
    [Header("Debug")]
    [SerializeField] private bool isSliding = false;
    [SerializeField] private float currentSlopeAngle = 0f;
    [SerializeField] private Vector3 slideDirection = Vector3.zero;
    [SerializeField] private Vector3 smoothedSlideDirection = Vector3.zero;
    [SerializeField] private Vector3 combinedSlideDirection = Vector3.zero;
    [SerializeField] private int slideHitCount = 0;
    [SerializeField] private int totalHitCount = 0;
    [SerializeField] private bool allHitsAreSlide = false;
    [SerializeField] private float slideContactTime = 0f;
    [SerializeField] private bool slideDelayPassed = false;
    
    private Vector3 slopeNormal = Vector3.up;
    private bool grounded = false;
    
    void OnEnable()
    {
        EventBus.Subscribe<OnPlayerSlopeEvent>(OnSlope);
        EventBus.Subscribe<OnPlayerGroundedEvent>(OnGrounded);
        EventBus.Subscribe<OnPlayerAirborneEvent>(OnAirborne);
    }
    
    void OnDisable()
    {
        EventBus.Unsubscribe<OnPlayerSlopeEvent>(OnSlope);
        EventBus.Unsubscribe<OnPlayerGroundedEvent>(OnGrounded);
        EventBus.Unsubscribe<OnPlayerAirborneEvent>(OnAirborne);
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
        
        if (isSliding)
        {
            isSliding = false;
            slideContactTime = 0f;
            slideDelayPassed = false;
            
            EventBus.Raise<OnPlayerSlideStateEvent>(new OnPlayerSlideStateEvent()
            {
                Player = gameObject,
                IsSliding = false,
                ControlMultiplier = 1f,
                SlideDirection = Vector3.zero,
                TargetSpeedGain = 0f,
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
        bool shouldSlide = grounded && allHitsAreSlide && totalHitCount > 0;
        
        if (shouldSlide)
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
            
            bool wasSliding = isSliding;
            isSliding = true;
            
            // Dirección
            Vector3 targetDirection;
            if (slideHitCount > 1 && combinedSlideDirection.magnitude > 0.1f)
            {
                targetDirection = combinedSlideDirection;
            }
            else
            {
                targetDirection = Vector3.ProjectOnPlane(Vector3.down, slopeNormal).normalized;
            }
            
            if (!wasSliding)
            {
                smoothedSlideDirection = targetDirection;
            }
            else
            {
                smoothedSlideDirection = Vector3.Lerp(
                    smoothedSlideDirection, 
                    targetDirection, 
                    directionSmoothSpeed * Time.deltaTime
                ).normalized;
            }
            
            slideDirection = smoothedSlideDirection;
            
            EventBus.Raise<OnPlayerSlideStateEvent>(new OnPlayerSlideStateEvent()
            {
                Player = gameObject,
                IsSliding = true,
                ControlMultiplier = controlWhileSliding,
                SlideDirection = slideDirection,
                TargetSpeedGain = slideSpeedGain,
                MaxSlideSpeed = maxSlideSpeed
            });
        }
        else
        {
            slideContactTime = 0f;
            slideDelayPassed = false;
            
            if (isSliding)
            {
                isSliding = false;
                smoothedSlideDirection = Vector3.zero;
                
                EventBus.Raise<OnPlayerSlideStateEvent>(new OnPlayerSlideStateEvent()
                {
                    Player = gameObject,
                    IsSliding = false,
                    ControlMultiplier = 1f,
                    SlideDirection = Vector3.zero,
                    TargetSpeedGain = 0f,
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