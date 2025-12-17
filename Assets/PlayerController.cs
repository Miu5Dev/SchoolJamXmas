using UnityEngine;

public class PlayerController : MonoBehaviour
{
 [Header("Movement")]
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float runSpeed = 12f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 12f;
    [SerializeField] private float rotationSpeed = 15f;
    
    [Header("Jumping")]
    [SerializeField] private float jumpHeight = 2.5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float coyoteTime = 0.15f;
    
    [Header("Air Control")]
    [SerializeField] private float airAcceleration = 6f;
    [SerializeField] private float airDeceleration = 2f;
    
    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;
    
    private CharacterController controller;
    private Vector3 currentVelocity;
    private Vector3 moveDirection;
    private float verticalVelocity;
    private float coyoteTimeCounter;
    private bool isGrounded;
    
    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
    }
    
    void Update()
    {
        HandleGroundCheck();
        HandleMovement();
        HandleJump();
        ApplyGravity();
        
        controller.Move((moveDirection + Vector3.up * verticalVelocity) * Time.deltaTime);
    }
    
    void HandleGroundCheck()
    {
        isGrounded = controller.isGrounded;
        
        if (isGrounded)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter -= Time.deltaTime;
    }
    
    void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;
        
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        
        Vector3 targetDirection = (forward * inputDir.z + right * inputDir.x);
        
        float targetSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        Vector3 targetVelocity = targetDirection * targetSpeed;
        
        float currentAccel = isGrounded ? acceleration : airAcceleration;
        float currentDecel = isGrounded ? deceleration : airDeceleration;
        
        if (targetDirection.magnitude > 0.1f)
        {
            moveDirection = Vector3.MoveTowards(moveDirection, targetVelocity, 
                currentAccel * Time.deltaTime);
            
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                rotationSpeed * Time.deltaTime);
        }
        else
        {
            moveDirection = Vector3.MoveTowards(moveDirection, Vector3.zero, 
                currentDecel * Time.deltaTime);
        }
    }
    
    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && coyoteTimeCounter > 0f)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            coyoteTimeCounter = 0f;
        }
    }
    
    void ApplyGravity()
    {
        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }
}