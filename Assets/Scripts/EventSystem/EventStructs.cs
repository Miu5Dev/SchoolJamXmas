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
    public float JumpForce;
    public Vector3 Direction;
    public float Speed;
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

