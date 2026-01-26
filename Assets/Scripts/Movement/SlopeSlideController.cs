using UnityEngine;

public class SlopeSlideController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private float minSlideAngle = 46f;
    [SerializeField] private float baseSlideSpeed = 5f;
    [SerializeField] private float maxSlideSpeed = 15f;
    [SerializeField] private float slideAcceleration = 3f;
    
    [Header("Control")]
    [SerializeField] [Range(0f, 1f)] private float controlWhileSliding = 0.3f;
    
    [Header("Debug")]
    [SerializeField] private bool isSliding = false;
    [SerializeField] private float currentSlopeAngle = 0f;
    [SerializeField] private Vector3 slideDirection = Vector3.zero;
    
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
            EventBus.Raise<OnPlayerStopSlidingEvent>(new OnPlayerStopSlidingEvent()
            {
                Player = gameObject
            });
        }
    }
    
    void Update()
    {
        HandleSliding();
    }
    
    private void HandleSliding()
    {
        bool shouldSlide = grounded && currentSlopeAngle >= minSlideAngle;
        
        if (shouldSlide)
        {
            isSliding = true;
            
            // Calcular velocidad objetivo proporcional al ángulo
            float angleRatio = Mathf.InverseLerp(minSlideAngle, 90f, currentSlopeAngle);
            float targetSpeed = Mathf.Lerp(baseSlideSpeed, maxSlideSpeed, angleRatio);
            
            slideDirection = Vector3.ProjectOnPlane(Vector3.down, slopeNormal).normalized;
            
            EventBus.Raise<OnPlayerSlideStateEvent>(new OnPlayerSlideStateEvent()
            {
                Player = gameObject,
                IsSliding = true,
                ControlMultiplier = controlWhileSliding,
                SlideDirection = slideDirection,
                TargetSpeed = targetSpeed,
                Acceleration = slideAcceleration
            });
        }
        else if (isSliding)
        {
            isSliding = false;
            
            EventBus.Raise<OnPlayerSlideStateEvent>(new OnPlayerSlideStateEvent()
            {
                Player = gameObject,
                IsSliding = false,
                ControlMultiplier = 1f,
                SlideDirection = Vector3.zero,
                TargetSpeed = 0f,
                Acceleration = 0f
            });
            
            EventBus.Raise<OnPlayerStopSlidingEvent>(new OnPlayerStopSlidingEvent()
            {
                Player = gameObject
            });
        }
    }
}