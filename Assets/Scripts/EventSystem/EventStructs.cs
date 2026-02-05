using UnityEngine;

// ============================================================================
// BASE EVENT CLASSES
// ============================================================================

public abstract class PlayerEventBase
{
    public GameObject Player;
}

public abstract class InputEventBase
{
    public bool pressed;
}

// ============================================================================
// INPUT EVENTS
// ============================================================================

public class OnMoveInputEvent : InputEventBase
{
    public Vector2 Direction;
}

public class OnLookInputEvent : InputEventBase
{
    public Vector2 Delta;
}

public class OnActionInputEvent : InputEventBase
{
}

public class OnCrouchInputEvent : InputEventBase
{
}

public class OnJumpInputEvent : InputEventBase
{
}

public class OnSwapInputEvent : InputEventBase
{
}

// ============================================================================
// PLAYER MOVEMENT EVENTS
// ============================================================================

public class OnPlayerMoveEvent : PlayerEventBase
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector2 Direction;
    public bool isCrouching;
    
    public float speed;
    
    public float rotationState;
}

public class OnPlayerStopEvent : PlayerEventBase
{
}

public class OnDirectionChangeEvent
{
    public GameObject Player;
    public float AngleChange;
    public Vector3 OldDirection;
    public Vector3 NewDirection;
    public float PenaltyFactor;
}

// ============================================================================
// PLAYER JUMP EVENTS
// ============================================================================

public class OnPlayerLandEvent : PlayerEventBase
{
    public float YHeight;
    public bool HardLanding;
    public bool FromGroundPound;
}

public class OnExecuteJumpCommand : PlayerEventBase
{
    public JumpTypeCreator JumpType;
    public bool KeepTripleJumpAnimation;  // NUEVO
}

public class OnApplyJumpForceCommand : PlayerEventBase
{
    public float Force;
}

public class OnPlayerGroundedEvent : PlayerEventBase
{
    public bool IsGrounded;
}

public class OnSetHangTimeState : PlayerEventBase
{
    public bool IsInHangTime;
}

public class OnRotatePlayerCommand : PlayerEventBase
{
    public float Degrees;
    public bool InvertMovementDirection;
}

public class OnPlayerAirborneEvent : PlayerEventBase
{
    public float YHeight;
}

// ============================================================================
// PLAYER LAUNCH EVENT (NUEVO)
// ============================================================================

public class OnPlayerLaunchEvent : PlayerEventBase
{
    public float LaunchVelocity;
    public Vector3 LaunchDirection;
    public float SlopeAngle;
}

// ============================================================================
// PLAYER CROUCH AND SLIDE EVENTS
// ============================================================================

public class OnPlayerStopSlidingEvent : PlayerEventBase
{
}

public class OnPlayerSlideStateEvent
{
    public GameObject Player;
    public bool IsSliding;
    public bool IsBeingPushedDown;
    public float ControlMultiplier;
    public Vector3 SlideDirection;
    public float TargetSpeedGain;
    public float MomentumDecay;
    public float MaxSlideSpeed;
}

// ============================================================================
// PLAYER SLOPE EVENTS
// ============================================================================

public class OnPlayerSlopeEvent : PlayerEventBase
{
    public float SlopeAngle;
    public Vector3 SlopeNormal;
    public Vector3 SlideDirection;
    public GameObject GroundObject;
    public string GroundTag;
    public Vector3 CombinedSlideDirection;
    public int SlideHitCount;
    public int TotalHitCount;
    public bool AllHitsAreSlide;
}

// ============================================================================
// PLAYER GROUND POUND EVENTS
// ============================================================================

public class OnPlayerGroundPoundEvent : PlayerEventBase
{
    
}

public class OnPlayerDiveEvent : PlayerEventBase
{
    
}

// ============================================================================
// MARIO 64 STYLE MOVEMENT EVENTS
// ============================================================================

/// <summary>
/// Evento emitido cuando el jugador entra o sale del estado de skid/derrape.
/// Útil para activar animaciones de frenado.
/// </summary>
public class OnPlayerSkidEvent : PlayerEventBase
{
    /// <summary>
    /// True cuando empieza el skid, False cuando termina
    /// </summary>
    public bool IsSkidding;
    
    /// <summary>
    /// Dirección desde la que el jugador estaba moviéndose cuando empezó el skid.
    /// Útil para orientar la animación de derrape.
    /// </summary>
    public Vector3 SkidDirection;
}

/// <summary>
/// Evento emitido cuando el jugador completa el giro después del skid
/// y está listo para correr en la nueva dirección.
/// </summary>
public class OnPlayerSkidCompleteEvent : PlayerEventBase
{
    /// <summary>
    /// Nueva dirección hacia la que el jugador va a correr
    /// </summary>
    public Vector3 NewDirection;
}


// ============================================================================
// ENUMS
// ============================================================================

public enum JumpType
{
    Normal,
    Double,
    Triple,
    LongJump,
    Backflip,
    GroundPoundJump,
    GroundPound,
    Dive,
    GroundDive,
}
