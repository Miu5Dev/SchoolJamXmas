using UnityEngine;

// ============================================================================
// PLAYER STATE ENUMS
// ============================================================================

/// <summary>
/// Estados principales del jugador - Usados para la máquina de estados de animación
/// </summary>
public enum PlayerState
{
    // === GROUND STATES ===
    Idle,
    Walking,
    Running,
    Sprinting,
    
    // === CROUCH STATES ===
    CrouchIdle,
    CrouchWalk,
    
    // === SKID/BRAKE STATES (Mario 64 Style) ===
    Skidding,            // Frenando antes de un giro de 180
    SkidTurn,            // Girando rápido después del skid
    
    // === AIR STATES ===
    Jumping,
    DoubleJumping,
    TripleJumping,
    Falling,
    
    // === SPECIAL JUMP STATES ===
    LongJump,
    Backflip,
    GroundPoundJump,
    
    // === GROUND POUND STATES ===
    GroundPoundStart,    // Hang time antes del ground pound
    GroundPoundFall,     // Cayendo en ground pound
    GroundPoundLand,     // Impacto del ground pound
    
    // === DIVE STATES ===
    Diving,
    GroundDiving,
    DiveLand,
    
    // === SLIDE STATES ===
    SlopeSliding,
    
    // === TRANSITION STATES ===
    Landing,             // Aterrizaje normal
    HardLanding,         // Aterrizaje fuerte
    
    // === SPECIAL STATES ===
    HangTime,            // Suspendido en el aire antes de un salto
}

/// <summary>
/// Sub-estados para dar más contexto a las animaciones
/// </summary>
public enum PlayerMovementPhase
{
    None,
    Accelerating,
    AtMaxSpeed,
    Decelerating,
    Turning,
    SharpTurn,
    Skidding,          // NUEVO: Durante el derrape
}

/// <summary>
/// Tipo de giro que está ejecutando el jugador
/// </summary>
public enum TurnType
{
    None,              // Sin giro
    Instant,           // Giro instantáneo (desde quieto)
    Arc,               // Giro en arco/curva
    Skid,              // Giro con derrape (180°)
}

/// <summary>
/// Estado vertical del jugador
/// </summary>
public enum VerticalState
{
    Grounded,
    Rising,
    Apex,        // En el punto más alto del salto
    Falling,
}

// ============================================================================
// PLAYER STATE EVENT
// ============================================================================

/// <summary>
/// Evento principal que notifica cambios de estado del jugador.
/// El AnimationController debe suscribirse a este evento.
/// </summary>
public class OnPlayerStateChangedEvent : PlayerEventBase
{
    // Estado principal
    public PlayerState CurrentState;
    public PlayerState PreviousState;
    
    // Información de movimiento
    public float Speed;                      // Velocidad actual (0-maxSpeed)
    public float NormalizedSpeed;            // Velocidad normalizada (0-1)
    public Vector3 MoveDirection;            // Dirección de movimiento
    public float RotationState;              // Estado de rotación (-1 izq, 0 centro, 1 der)
    
    // Información vertical
    public VerticalState VerticalState;
    public float VerticalVelocity;
    public float NormalizedVerticalVelocity; // -1 (cayendo max) a 1 (subiendo max)
    
    // Información de fase
    public PlayerMovementPhase MovementPhase;
    public TurnType CurrentTurnType;         // NUEVO: Tipo de giro actual
    
    // Contexto adicional
    public JumpType? LastJumpType;           // Tipo del último salto ejecutado
    public bool IsCrouching;
    public bool IsSliding;
    public bool IsSkidding;                  // NUEVO: Si está en skid
    public float SlopeAngle;
    
    // Timestamps para blending de animaciones
    public float StateEnterTime;             // Time.time cuando entró al estado
    public float TimeSinceStateChange;       // Tiempo desde el último cambio
}

/// <summary>
/// Evento lightweight que se envía cada frame con datos de animación
/// Para parámetros que cambian constantemente (velocidad, rotación, etc.)
/// </summary>
public class OnPlayerAnimationDataEvent : PlayerEventBase
{
    // Datos que cambian cada frame
    public float Speed;
    public float NormalizedSpeed;
    public float VerticalVelocity;
    public float NormalizedVerticalVelocity;
    public float RotationState;              // Para blend trees de giro
    public Vector2 InputDirection;           // Input raw para blend trees
    public bool IsMoving;
    public bool IsGrounded;
    public bool IsSkidding;                  // NUEVO
    public TurnType CurrentTurnType;         // NUEVO
}