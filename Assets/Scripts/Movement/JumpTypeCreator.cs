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
    
    [Header("Conditions")]
    public JumpCondition condition = JumpCondition.GroundedOnly;
    
    [Tooltip("Jump Cooldown (if 0 use default)")]
    public float customCooldown = 0f;
    
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