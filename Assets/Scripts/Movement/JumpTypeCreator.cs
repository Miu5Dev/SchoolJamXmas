using UnityEngine;

[CreateAssetMenu(fileName = "New Jump Type", menuName = "Player/Jump Type")]
public class JumpTypeCreator : ScriptableObject
{
    [Header("Jump Type")]
    [Tooltip("What kind of jump is it?")]
    public JumpType jumpType = JumpType.Normal;
    
    [Header("Jump Properties")]
    [Tooltip("Jump Height")]
    public float jumpForce = 2f;
    
    [Tooltip("Speed on Jump")]
    public float extraSpeed = 5f;
    
    [Header("Hang Time")] // NUEVO
    [Tooltip("Flying time before executing jumpforce")]
    public float hangTime = 0f;
    
    [Header("Conditions")]
    public JumpCondition condition = JumpCondition.GroundedOnly;
    
    [Tooltip("Jump Cooldown (if 0 use default)")]
    public float customCooldown = 0f;
    
    [Header("Speed Control")] // NUEVO
    [Tooltip("if this is true, the player speed is set 0 on this jump is executed")]
    public bool resetSpeedOnJump = false;
    
    [Header("Rotation & Direction")] // NUEVO
    [Tooltip("if true player gets rotated on executing the jump")]
    public bool rotatePlayer = false;
    
    [Tooltip("Degrees to rate the player (looking back = 180)")]
    public float rotationDegrees = 0f;
    
    [Tooltip("if true, also inverts the push direction")]
    public bool invertMovementDirection = false;

    
    [Header("Advanced")]
    [Tooltip("Uphill speed multiplier")]
    [Range(0f, 1f)]
    public float uphillSpeedMultiplier = 0.3f;
    
    [Tooltip("Ignore Uphill penalty (if true, there is no penalty going uphill)")]
    public bool ignoreUphillPenalty = false;
}

public enum JumpCondition
{
    GroundedOnly,    // Solo en el suelo
    AirOnly,         // Solo en el aire
    Both             // En cualquier momento
}