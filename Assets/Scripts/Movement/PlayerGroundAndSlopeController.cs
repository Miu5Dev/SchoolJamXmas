using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerGroundAndSlopeController : MonoBehaviour
{
    [Header("Detectors Config")]
    [SerializeField]private float DetectionLenght;
    [SerializeField]private Vector3 offset;
    [SerializeField]private float RaycastRadius = 1;
    [SerializeField]private Transform player;
    [SerializeField]private LayerMask layersToDetect;
    
    [Header("Grounded Config")]
    [SerializeField]private float minDistanceToGround;  //min distance between hit and player to be detected as grounded
    [SerializeField] private float coyoteTime = 0.15f;

    [Header("Debug")]
    [SerializeField] private Vector3 slopeNormal;
    [SerializeField]private float slopeAngle;
    [SerializeField]private bool grounded;
    
    private float lastHitAngle;
    private RaycastHit[] hit = new RaycastHit[5];
    private float lastGroundedTime = 0f;
    
    private float DefaultDetectionLenght;
    private float DefaultMinDistanceToGround;

    public void Awake()
    {
        player = this.transform;
        
        DefaultDetectionLenght = DetectionLenght;
        DefaultMinDistanceToGround = minDistanceToGround;
    }

    public void Update()
    {
        GroundedDetector();
        
        DetectionLenghtFixer();
        
        HitDetector();

        HitNormalDetector();

        AngleCalculation();
        
        EventSender();
        
        lastHitAngle = slopeAngle;
    }


    private void EventSender()
    {
        if(lastHitAngle == slopeAngle) return;
        
        
        EventBus.Raise<OnPlayerSlopeEvent>(new OnPlayerSlopeEvent()
        {
            Player = this.gameObject,
            SlideDirection = Vector3.ProjectOnPlane(Vector3.down, slopeNormal).normalized,
            SlopeAngle = slopeAngle,
            SlopeNormal = slopeNormal,

        });
    }
    
    
    private void AngleCalculation()
    {
        slopeAngle = Vector3.Angle(slopeNormal, Vector3.up);
    }

    private void HitNormalDetector()
    {
        Vector3 normalSum = Vector3.zero;
        int activeHits = 0;
    
        // Recorrer todos los hits
        for (int i = 0; i < hit.Length; i++)
        {
            if (hit[i].transform != null)
            {
                normalSum += hit[i].normal;
                activeHits++;
            }
        }
    
        // Si hay al menos un hit, calcular el promedio
        if (activeHits > 0)
        {
            slopeNormal = (normalSum / activeHits).normalized;
        }
        else
        {
            // Si no hay hits, usar un valor por defecto (por ejemplo, Vector3.up)
            slopeNormal = Vector3.up;
        }
    }

    private void DetectionLenghtFixer()
    {
        if (grounded && slopeAngle != 0)
        {
            DetectionLenght = DefaultDetectionLenght * (slopeAngle/4);
            minDistanceToGround = DefaultMinDistanceToGround * (slopeAngle/4);
        }
        else
        {
            DetectionLenght = DefaultDetectionLenght;
            minDistanceToGround = DefaultMinDistanceToGround;
        }
    }
    
    private void GroundedDetector()
    {
        bool wasGrounded = grounded;

        // Si está subiendo muy rápido, definitivamente no está grounded
        if (player.GetComponent<PlayerController>().verticalVelocity > 1f)
        {
            grounded = false;
            if (wasGrounded) // FIX: Cambiar de !wasGrounded a wasGrounded
            {
                EventBus.Raise<OnPlayerAirborneEvent>(new OnPlayerAirborneEvent()
                {
                    Player = this.gameObject,
                    YHeight = this.transform.position.y,
                });
            }
            return;
        }

        bool currentlyTouchingGround = false;

        // Check if any hit is within the minimum distance
        for (int i = 0; i < hit.Length; i++)
        {
            // FIX: Verificar que el hit sea válido primero
            if (hit[i].collider != null) // Usar collider en vez de transform
            {
                if (hit[i].distance <= minDistanceToGround)
                {
                    currentlyTouchingGround = true;
                    lastGroundedTime = Time.time;
                    break; // Con un solo hit es suficiente
                }
            }
        }

        // Aplicar coyote time
        grounded = currentlyTouchingGround || (Time.time - lastGroundedTime) < coyoteTime;

        // Raise events
        if (grounded && !wasGrounded)
        {
            EventBus.Raise<OnPlayerGroundedEvent>(new OnPlayerGroundedEvent()
            {
                Player = this.gameObject,
                IsGrounded = grounded,
            });
        }
        else if (!grounded && wasGrounded)
        {
            EventBus.Raise<OnPlayerAirborneEvent>(new OnPlayerAirborneEvent()
            {
                Player = this.gameObject,
                YHeight = this.transform.position.y,
            });
        }
    }
    
    private void HitDetector()
    {
        // Center hit (proyectado hacia adelante)
        Physics.Raycast(player.position  + offset, Vector3.down, out hit[0], DetectionLenght, layersToDetect);
        // Forward hit
        Physics.Raycast(player.position  + player.forward * RaycastRadius + offset, Vector3.down, out hit[1], DetectionLenght, layersToDetect);
        // Backward hit
        Physics.Raycast(player.position  - player.forward * RaycastRadius + offset, Vector3.down, out hit[2], DetectionLenght, layersToDetect);
        // Right hit
        Physics.Raycast(player.position  + player.right * RaycastRadius + offset, Vector3.down, out hit[3], DetectionLenght, layersToDetect);
        // Left hit
        Physics.Raycast(player.position  - player.right * RaycastRadius + offset, Vector3.down, out hit[4], DetectionLenght, layersToDetect);
    }

public void OnDrawGizmos()
{
    if (player == null) player = this.transform;
    
    Vector3[] startPositions = new Vector3[5];
    startPositions[0] = player.position + offset; // Center
    startPositions[1] = player.position + player.forward * RaycastRadius + offset; // Forward
    startPositions[2] = player.position - player.forward * RaycastRadius + offset; // Backward
    startPositions[3] = player.position + player.right * RaycastRadius + offset; // Right
    startPositions[4] = player.position - player.right * RaycastRadius + offset; // Left
    
    // Dibuja cada raycast
    for (int i = 0; i < startPositions.Length; i++)
    {
        // Color verde si hay hit, rojo si no
        if (hit != null && i < hit.Length && hit[i].transform != null)
        {
            Gizmos.color = Color.green;
            // Dibuja hasta el punto de impacto
            Gizmos.DrawLine(startPositions[i], hit[i].point);
            Gizmos.DrawSphere(hit[i].point, 0.1f);
        }
        else
        {
            Gizmos.color = Color.red;
            // Dibuja la longitud completa del raycast
            Gizmos.DrawLine(startPositions[i], startPositions[i] + Vector3.down * DetectionLenght);
        }
    }
}


    
}
