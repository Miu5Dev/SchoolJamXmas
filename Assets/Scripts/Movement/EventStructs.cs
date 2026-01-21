using UnityEngine;
using System;

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

public class onMoveInputEvent : InputEventBase
{
    public Vector2 Direction;
}

public class onInteractInputEvent : InputEventBase
{
}

public class onCrouchInputEvent : InputEventBase
{
}

public class onJumpInputEvent : InputEventBase
{
}

public class onLookInputEvent : InputEventBase
{
    public Vector2 Delta;
}

public class onDiveInputEvent : InputEventBase
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
    public float Speed;
    public bool IsAccelerating;
}

public class OnPlayerStopEvent : PlayerEventBase
{
}

public class OnPlayerTurnEvent : PlayerEventBase
{
    public float TurnAmount;
    public bool IsQuickTurn;
}

public class OnPlayerAccelerationChangeEvent : PlayerEventBase
{
    public float CurrentSpeed;
    public float TargetSpeed;
    public float AccelerationRate;
}

// ============================================================================
// PLAYER JUMP EVENTS
// ============================================================================

public class OnPlayerJumpEvent : PlayerEventBase
{
    public JumpType JumpType;
    public int JumpCount;
    public Vector3 JumpDirection;
    public float JumpForce;
}

public class OnPlayerLandEvent : PlayerEventBase
{
    public float FallSpeed;
    public bool HardLanding;
    public bool FromGroundPound;
}

public class OnPlayerGroundedEvent : PlayerEventBase
{
    public bool IsGrounded;
}

public class OnPlayerAirborneEvent : PlayerEventBase
{
    public float AirTime;
    public float VerticalVelocity;
}

// ============================================================================
// PLAYER CROUCH AND SLIDE EVENTS
// ============================================================================

public class OnPlayerCrouchEvent : PlayerEventBase
{
    public bool IsCrouching;
}

public class OnPlayerCrouchSlideStartEvent : PlayerEventBase
{
    public float InitialSpeed;
    public Vector3 SlideDirection;
}

public class OnPlayerCrouchSlideEndEvent : PlayerEventBase
{
    public float FinalSpeed;
    public CrouchSlideEndReason Reason;
}

public class OnPlayerSlidingEvent : PlayerEventBase
{
    public bool IsSliding;
    public float SlideSpeed;
    public Vector3 SlideDirection;
}

public class OnPlayerCrouchSlidingEvent : PlayerEventBase
{
    public bool IsCrouchSliding;
    public float Speed;
}

public class OnPlayerCrouchLockedEvent : PlayerEventBase
{
    public bool IsLocked;
}

// ============================================================================
// PLAYER SLOPE EVENTS
// ============================================================================

public class OnPlayerSlopeEvent : PlayerEventBase
{
    public float SlopeAngle;
    public bool IsOnSteepSlope;
    public Vector3 SlopeNormal;
}

public class OnPlayerSlopeSlideEvent : PlayerEventBase
{
    public bool IsSliding;
    public float SlideSpeed;
    public Vector3 SlideDirection;
    public float SlopeAngle;
}

// ============================================================================
// PLAYER MOMENTUM EVENTS
// ============================================================================

public class OnPlayerMomentumEvent : PlayerEventBase
{
    public Vector3 Momentum;
    public float MomentumMagnitude;
    public MomentumSource Source;
}

public class OnPlayerSpeedEvent : PlayerEventBase
{
    public float CurrentSpeed;
    public float SlideSpeed;
    public float TotalSpeed;
    public float MaxSpeed;
}

// ============================================================================
// PLAYER GROUND POUND EVENTS
// ============================================================================

public class OnPlayerGroundPoundEvent : PlayerEventBase
{
    public GroundPoundPhase Phase;
}

public class OnPlayerDiveEvent : PlayerEventBase
{
    public Vector3 DiveDirection;
    public float DiveSpeed;
    public bool FromGroundPound;
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
    public bool IsClimbing;
    public float Progress;
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
    Long,
    Backflip,
    SlopeJump,
    GroundPoundJump,
    LedgeJump,
    CrouchJump
}

public enum CrouchSlideEndReason
{
    SpeedTooLow,
    Released,
    Jumped,
    HitObstacle,
    SlopeChange
}

public enum MomentumSource
{
    Movement,
    Slide,
    Jump,
    GroundPound,
    External
}

public enum GroundPoundPhase
{
    Starting,    // Freeze in air
    Falling,     // Fast fall
    Landing,     // Hit ground
    Bouncing     // After ground pound jump
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
    HardLanding
}
