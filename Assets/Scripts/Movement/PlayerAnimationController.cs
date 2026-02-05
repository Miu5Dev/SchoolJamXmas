using UnityEngine;

/// <summary>
/// Controlador de animaciones del jugador.
/// Se suscribe a eventos de estado y actualiza el Animator.
/// 
/// ACTUALIZADO: Soporte para animaciones de skid/derrape estilo Mario 64.
/// 
/// CONFIGURACIÓN DEL ANIMATOR:
/// - Crear estados para Skidding y SkidTurn
/// - El Skid debe tener la animación de frenado/derrape
/// - El SkidTurn es el giro rápido después del skid
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    
    [Header("Animation Smoothing")]
    [SerializeField] private float speedDampTime = 0.1f;
    [SerializeField] private float verticalVelocityDampTime = 0.1f;
    [SerializeField] private float rotationDampTime = 0.1f;
    
    [Header("Debug")]
    [SerializeField] private PlayerState currentState;
    [SerializeField] private TurnType currentTurnType;
    [SerializeField] private float animSpeed;
    [SerializeField] private bool isSkidding;
    
    // Animator parameter hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int NormalizedSpeedHash = Animator.StringToHash("NormalizedSpeed");
    private static readonly int VerticalVelocityHash = Animator.StringToHash("VerticalVelocity");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int StateIndexHash = Animator.StringToHash("StateIndex");
    private static readonly int RotationStateHash = Animator.StringToHash("RotationState");
    private static readonly int InputXHash = Animator.StringToHash("InputX");
    private static readonly int InputYHash = Animator.StringToHash("InputY");
    private static readonly int IsCrouchingHash = Animator.StringToHash("IsCrouching");
    private static readonly int IsSlidingHash = Animator.StringToHash("IsSliding");
    private static readonly int IsSkiddingHash = Animator.StringToHash("IsSkidding");
    private static readonly int TurnTypeHash = Animator.StringToHash("TurnType");
    
    // CHANGED: Critical states use Bools instead of Triggers for reliability
    private static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");
    private static readonly int IsFallingHash = Animator.StringToHash("IsFalling");
    private static readonly int IsLandingHash = Animator.StringToHash("IsLanding");
    private static readonly int IsHardLandingHash = Animator.StringToHash("IsHardLanding");
    private static readonly int IsDivingHash = Animator.StringToHash("IsDiving");
    private static readonly int IsGroundPoundingHash = Animator.StringToHash("IsGroundPounding");
    private static readonly int IsBackflipHash = Animator.StringToHash("IsBackflip");
    private static readonly int IsLongJumpHash = Animator.StringToHash("IsLongJump");
    
    // Triggers only for one-shot animations that need immediate response
    private static readonly int JumpTriggerHash = Animator.StringToHash("Jump");
    private static readonly int DoubleJumpTriggerHash = Animator.StringToHash("DoubleJump");
    private static readonly int TripleJumpTriggerHash = Animator.StringToHash("TripleJump");
    private static readonly int LongJumpTriggerHash = Animator.StringToHash("LongJump");
    private static readonly int BackflipTriggerHash = Animator.StringToHash("Backflip");
    private static readonly int DiveTriggerHash = Animator.StringToHash("Dive");
    private static readonly int GroundPoundTriggerHash = Animator.StringToHash("GroundPound");
    private static readonly int LandTriggerHash = Animator.StringToHash("Land");
    private static readonly int HardLandTriggerHash = Animator.StringToHash("HardLand");
    private static readonly int SkidTriggerHash = Animator.StringToHash("Skid");
    private static readonly int SkidTurnTriggerHash = Animator.StringToHash("SkidTurn");
    
    void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }
    
    void OnEnable()
    {
        EventBus.Subscribe<OnPlayerStateChangedEvent>(OnStateChanged);
        EventBus.Subscribe<OnPlayerAnimationDataEvent>(OnAnimationData);
        EventBus.Subscribe<OnPlayerSkidEvent>(OnSkidEvent);
    }
    
    void OnDisable()
    {
        EventBus.Unsubscribe<OnPlayerStateChangedEvent>(OnStateChanged);
        EventBus.Unsubscribe<OnPlayerAnimationDataEvent>(OnAnimationData);
        EventBus.Unsubscribe<OnPlayerSkidEvent>(OnSkidEvent);
    }
    
    /// <summary>
    /// Maneja los cambios de estado del jugador.
    /// Usa BOOLS para estados que necesitan persistir y TRIGGERS para feedback inmediato.
    /// </summary>
    private void OnStateChanged(OnPlayerStateChangedEvent ev)
    {
        currentState = ev.CurrentState;
        currentTurnType = ev.CurrentTurnType;
        isSkidding = ev.IsSkidding;
        
        // Update basic state parameters
        animator.SetInteger(StateIndexHash, (int)ev.CurrentState);
        animator.SetInteger(TurnTypeHash, (int)ev.CurrentTurnType);
        animator.SetBool(IsCrouchingHash, ev.IsCrouching);
        animator.SetBool(IsSlidingHash, ev.IsSliding);
        animator.SetBool(IsSkiddingHash, ev.IsSkidding);
        
        // === RESET ALL STATE BOOLS FIRST ===
        // This ensures clean transitions
        ResetAllStateBools();
        
        // === SET ACTIVE STATE BOOL + TRIGGER ===
        switch (ev.CurrentState)
        {
            // --- JUMP STATES ---
            case PlayerState.Jumping:
                animator.SetBool(IsJumpingHash, true);
                animator.SetTrigger(JumpTriggerHash);
                break;
                
            case PlayerState.DoubleJumping:
                animator.SetBool(IsJumpingHash, true);
                animator.SetTrigger(DoubleJumpTriggerHash);
                break;
                
            case PlayerState.TripleJumping:
                animator.SetBool(IsJumpingHash, true);
                animator.SetTrigger(TripleJumpTriggerHash);
                break;
                
            case PlayerState.LongJump:
                animator.SetBool(IsLongJumpHash, true);
                animator.SetTrigger(LongJumpTriggerHash);
                break;
                
            case PlayerState.Backflip:
                animator.SetBool(IsBackflipHash, true);
                animator.SetTrigger(BackflipTriggerHash);
                break;
            
            case PlayerState.GroundPoundJump:
                animator.SetBool(IsJumpingHash, true);
                animator.SetTrigger(JumpTriggerHash);
                break;
            
            // --- FALLING ---
            case PlayerState.Falling:
                animator.SetBool(IsFallingHash, true);
                // NO trigger - just set bool, let transition handle it
                break;
                
            // --- DIVE ---
            case PlayerState.Diving:
            case PlayerState.GroundDiving:
                animator.SetBool(IsDivingHash, true);
                animator.SetTrigger(DiveTriggerHash);
                break;
                
            // --- GROUND POUND ---
            case PlayerState.GroundPoundStart:
                animator.SetBool(IsGroundPoundingHash, true);
                animator.SetTrigger(GroundPoundTriggerHash);
                break;
                
            case PlayerState.GroundPoundFall:
                animator.SetBool(IsGroundPoundingHash, true);
                // No trigger, continues from GroundPoundStart
                break;
                
            case PlayerState.GroundPoundLand:
                animator.SetBool(IsHardLandingHash, true);
                animator.SetTrigger(HardLandTriggerHash);
                break;
                
            // --- LANDING ---
            case PlayerState.Landing:
                animator.SetBool(IsLandingHash, true);
                animator.SetTrigger(LandTriggerHash);
                Debug.Log("[Animator] Landing state - Bool: true, Trigger: Land");
                break;
                
            case PlayerState.HardLanding:
                animator.SetBool(IsHardLandingHash, true);
                animator.SetTrigger(HardLandTriggerHash);
                Debug.Log("[Animator] HardLanding state - Bool: true, Trigger: HardLand");
                break;
                
            // --- SKID ---
            case PlayerState.Skidding:
                animator.SetTrigger(SkidTriggerHash);
                break;
                
            case PlayerState.SkidTurn:
                animator.SetTrigger(SkidTurnTriggerHash);
                break;
                
            // --- GROUND STATES (Idle, Walking, etc.) ---
            // These don't need special bools, handled by Speed/IsMoving
            default:
                // All state bools are already reset
                break;
        }
    }
    
    /// <summary>
    /// Resetea todos los bools de estado para evitar conflictos.
    /// </summary>
    private void ResetAllStateBools()
    {
        animator.SetBool(IsJumpingHash, false);
        animator.SetBool(IsFallingHash, false);
        animator.SetBool(IsLandingHash, false);
        animator.SetBool(IsHardLandingHash, false);
        animator.SetBool(IsDivingHash, false);
        animator.SetBool(IsGroundPoundingHash, false);
        animator.SetBool(IsBackflipHash, false);
        animator.SetBool(IsLongJumpHash, false);
    }
    
    /// <summary>
    /// Handler directo para eventos de skid (respuesta más rápida).
    /// </summary>
    private void OnSkidEvent(OnPlayerSkidEvent ev)
    {
        animator.SetBool(IsSkiddingHash, ev.IsSkidding);
        
        if (ev.IsSkidding)
        {
            animator.SetTrigger(SkidTriggerHash);
        }
    }
    
    /// <summary>
    /// Recibe datos de animación cada frame.
    /// </summary>
    private void OnAnimationData(OnPlayerAnimationDataEvent ev)
    {
        animSpeed = ev.Speed;
        
        // Update continuous parameters
        animator.SetFloat(SpeedHash, ev.Speed, speedDampTime, Time.deltaTime);
        animator.SetFloat(NormalizedSpeedHash, ev.NormalizedSpeed, speedDampTime, Time.deltaTime);
        animator.SetFloat(VerticalVelocityHash, ev.NormalizedVerticalVelocity, verticalVelocityDampTime, Time.deltaTime);
        animator.SetFloat(RotationStateHash, ev.RotationState, rotationDampTime, Time.deltaTime);
        
        // Input for blend trees
        animator.SetFloat(InputXHash, ev.InputDirection.x);
        animator.SetFloat(InputYHash, ev.InputDirection.y);
        
        // Boolean states
        animator.SetBool(IsGroundedHash, ev.IsGrounded);
        animator.SetBool(IsMovingHash, ev.IsMoving);
        animator.SetBool(IsSkiddingHash, ev.IsSkidding);
        
        // Turn type
        animator.SetInteger(TurnTypeHash, (int)ev.CurrentTurnType);
    }
    
    public void PlayAnimation(string stateName, int layer = 0)
    {
        animator.Play(stateName, layer);
    }
    
    public void CrossFadeAnimation(string stateName, float duration = 0.1f, int layer = 0)
    {
        animator.CrossFade(stateName, duration, layer);
    }
}