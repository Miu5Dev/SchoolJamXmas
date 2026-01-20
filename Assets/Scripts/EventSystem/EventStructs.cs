using UnityEngine;
using System;

public abstract class PlayerEventBase
{
    public GameObject Player;
}

public abstract class InputEventBase
{
    public bool pressed;
}


public class onMoveInputEvent : InputEventBase
{
    public Vector2 Direction;
}

public class onInteractInputEvent : InputEventBase
{
}

public class onRunInputEvent : InputEventBase
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

public class OnPlayerMoveEvent : PlayerEventBase
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector2 Direction;
    public int Speed;
}

public class OnPlayerStopEvent : PlayerEventBase
{
}

public class OnPlayerJumpEvent : PlayerEventBase
{
    public JumpType JumpType;
    public int JumpCount;
    public Vector3 JumpDirection;
}

public class OnPlayerInteractEvent : PlayerEventBase
{
    public GameObject InteractiveObject;
}

public class OnPlayerRunEvent : PlayerEventBase
{
    public float Speed;
}

public class OnPlayerCrouchEvent : PlayerEventBase
{
}

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

public class OnPlayerGroundedEvent : PlayerEventBase
{
    public bool IsGrounded;
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
}

public class OnPlayerSlopeEvent : PlayerEventBase
{
    public float SlopeAngle;
    public bool IsOnSteepSlope;
}

public class OnPlayerMomentumEvent : PlayerEventBase
{
    public Vector3 Momentum;
    public float MomentumMagnitude;
}

public class OnPlayerSpeedEvent : PlayerEventBase
{
    public float CurrentSpeed;
    public float SlideSpeed;
    public float TotalSpeed;
}

public class OnPlayerCrouchLockedEvent : PlayerEventBase
{
    public bool IsLocked;
}

public class OnPlayerLandEvent : PlayerEventBase
{
    public float FallSpeed;
    public bool HardLanding;
}

public class OnPlayerGroundPoundEvent : PlayerEventBase
{
    public bool IsStarting;
    public bool IsLanding;
}

public class OnPlayerLedgeGrabEvent : PlayerEventBase
{
    public bool IsGrabbing;
    public Vector3 LedgePosition;
}

public enum JumpType
{
    Normal,
    Double,
    Triple,
    Long,
    Backflip,
    SlopeJump,
    GroundPoundJump,
    LedgeJump
}