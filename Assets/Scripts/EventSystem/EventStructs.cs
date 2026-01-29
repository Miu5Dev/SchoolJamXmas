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
    
    public float speed;
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

public class OnPlayerJumpEvent : PlayerEventBase
{
    public JumpType JumpType;
    public float JumpForce;

    public float accelerationMultiplier;
}

public class OnPlayerLandEvent : PlayerEventBase
{
    public float YHeight;
    public bool HardLanding;
    public bool FromGroundPound;
}

public class OnExecuteJumpCommand : PlayerEventBase
{
    public JumpTypeCreator JumpType;
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

public class OnPlayerCrouchEvent : PlayerEventBase
{
    public bool IsCrouching;
}

public class OnPlayerStopSlidingEvent : PlayerEventBase
{
}

public class OnPlayerSlidingEvent : PlayerEventBase
{
    public Vector3 SlideDirection;
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
// PLAYER LEDGE GRAB EVENTS
// ============================================================================

public class OnPlayerLedgeGrabEvent : PlayerEventBase
{
    public bool IsGrabbing;
    public Vector3 LedgePosition;
    public Vector3 LedgeNormal;
}

public class OnPlayerLedgeMoveEvent : PlayerEventBase
{
    public float MoveDirection;
    public Vector3 NewPosition;
}

public class OnPlayerLedgeClimbEvent : PlayerEventBase
{
    
}

// ============================================================================
// PLAYER INTERACT EVENT
// ============================================================================

public class OnPlayerInteractEvent : PlayerEventBase
{
    public GameObject InteractiveObject;
}

// ============================================================================
// PLAYER DAMAGE EVENTS
// ============================================================================

public class OnPlayerGetDamageEvent : PlayerEventBase
{
    public int Damage;
    public GameObject Attacker;
}

public class OnPlayerDealDamageEvent : PlayerEventBase
{
    public int Damage;
    public GameObject DamageReceiver;
}

// ============================================================================
// PLAYER STATE CHANGE EVENT (General purpose for animations)
// ============================================================================

public class OnPlayerStateChangeEvent : PlayerEventBase
{
    public PlayerState PreviousState;
    public PlayerState NewState;
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

public enum PlayerState
{
    Idle,
    Walking,
    Running,
    Crouching,
    CrouchWalking,
    CrouchSliding,
    SlopeSliding,
    Jumping,
    DoubleJumping,
    TripleJumping,
    LongJumping,
    Backflipping,
    Diving,
    GroundPounding,
    Falling,
    LedgeGrabbing,
    LedgeClimbing,
    Landing,
    HardLanding,
    Launching // NUEVO
}