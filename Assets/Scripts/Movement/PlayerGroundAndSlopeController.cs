using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerGroundAndSlopeController : MonoBehaviour
{
    [Header("Detectors Config")]
    [SerializeField] private float DetectionLenght;
    [SerializeField] private Vector3 offset;
    [SerializeField] private float RaycastRadius = 1;
    [SerializeField] private Transform player;
    [SerializeField] private LayerMask layersToDetect;
    
    [Header("Grounded Config")]
    [SerializeField] private float minDistanceToGround;
    [SerializeField] private float coyoteTime = 0.15f;
    
    [Header("Slide Detection")]
    [SerializeField] private string slideTag = "Slide";
    [SerializeField] private float minSlideAngle = 46f;

    [Header("Debug")]
    [SerializeField] private Vector3 slopeNormal;
    [SerializeField] private float slopeAngle;
    [SerializeField] private bool grounded;
    [SerializeField] private GameObject currentGroundObject;
    [SerializeField] private Vector3 combinedSlideDirection;
    [SerializeField] private int slideHitCount;
    [SerializeField] private int totalHitCount; // NUEVO
    [SerializeField] private bool allHitsAreSlide; // NUEVO
    
    private float lastHitAngle;
    private RaycastHit[] hit = new RaycastHit[5];
    private float lastGroundedTime = 0f;
    
    private float DefaultDetectionLenght;
    private float DefaultMinDistanceToGround;
    
    private GameObject lastGroundObject;
    private Vector3 lastCombinedDirection;
    private bool lastAllHitsAreSlide; // NUEVO

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
        
        GroundObjectDetector();
        
        CalculateCombinedSlideDirection();
        
        EventSender();
        
        lastHitAngle = slopeAngle;
        lastGroundObject = currentGroundObject;
        lastCombinedDirection = combinedSlideDirection;
        lastAllHitsAreSlide = allHitsAreSlide; // NUEVO
    }

    private void CalculateCombinedSlideDirection()
    {
        combinedSlideDirection = Vector3.zero;
        slideHitCount = 0;
        totalHitCount = 0; // NUEVO
        
        for (int i = 0; i < hit.Length; i++)
        {
            if (hit[i].collider == null) continue;
            
            totalHitCount++; // NUEVO: Contar todos los hits
            
            bool isSlideByTag = hit[i].collider.CompareTag(slideTag);
            float hitAngle = Vector3.Angle(hit[i].normal, Vector3.up);
            bool isSlideByAngle = hitAngle >= minSlideAngle;
            
            if (isSlideByTag || isSlideByAngle)
            {
                Vector3 hitSlideDirection = Vector3.ProjectOnPlane(Vector3.down, hit[i].normal).normalized;
                
                float weight = isSlideByAngle ? Mathf.InverseLerp(minSlideAngle, 90f, hitAngle) + 0.5f : 0.5f;
                
                combinedSlideDirection += hitSlideDirection * weight;
                slideHitCount++;
            }
        }
        
        // NUEVO: Verificar si TODOS los hits son slide
        allHitsAreSlide = totalHitCount > 0 && slideHitCount == totalHitCount;
        
        if (slideHitCount > 0 && combinedSlideDirection.magnitude > 0.01f)
        {
            combinedSlideDirection.Normalize();
        }
        else
        {
            combinedSlideDirection = Vector3.zero;
        }
    }

    private void GroundObjectDetector()
    {
        currentGroundObject = null;
        float closestDistance = float.MaxValue;
        
        for (int i = 0; i < hit.Length; i++)
        {
            if (hit[i].collider != null && hit[i].distance < closestDistance)
            {
                closestDistance = hit[i].distance;
                currentGroundObject = hit[i].collider.gameObject;
            }
        }
    }

    private void EventSender()
    {
        bool groundChanged = currentGroundObject != lastGroundObject;
        bool directionChanged = Vector3.Distance(combinedSlideDirection, lastCombinedDirection) > 0.01f;
        bool allHitsChanged = allHitsAreSlide != lastAllHitsAreSlide; // NUEVO
        
        if (lastHitAngle == slopeAngle && !groundChanged && !directionChanged && !allHitsChanged) return;
        
        string groundTag = currentGroundObject != null ? currentGroundObject.tag : "";
        
        EventBus.Raise<OnPlayerSlopeEvent>(new OnPlayerSlopeEvent()
        {
            Player = this.gameObject,
            SlideDirection = Vector3.ProjectOnPlane(Vector3.down, slopeNormal).normalized,
            SlopeAngle = slopeAngle,
            SlopeNormal = slopeNormal,
            GroundObject = currentGroundObject,
            GroundTag = groundTag,
            CombinedSlideDirection = combinedSlideDirection,
            SlideHitCount = slideHitCount,
            TotalHitCount = totalHitCount, // NUEVO
            AllHitsAreSlide = allHitsAreSlide // NUEVO
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
    
        for (int i = 0; i < hit.Length; i++)
        {
            if (hit[i].transform != null)
            {
                normalSum += hit[i].normal;
                activeHits++;
            }
        }
    
        if (activeHits > 0)
        {
            slopeNormal = (normalSum / activeHits).normalized;
        }
        else
        {
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

        if (player.GetComponent<PlayerController>().verticalVelocity > 1f)
        {
            grounded = false;
            if (wasGrounded)
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

        for (int i = 0; i < hit.Length; i++)
        {
            if (hit[i].collider != null)
            {
                if (hit[i].distance <= minDistanceToGround)
                {
                    currentlyTouchingGround = true;
                    lastGroundedTime = Time.time;
                    break;
                }
            }
        }

        grounded = currentlyTouchingGround || (Time.time - lastGroundedTime) < coyoteTime;

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
        Physics.Raycast(player.position + offset, Vector3.down, out hit[0], DetectionLenght, layersToDetect);
        Physics.Raycast(player.position + player.forward * RaycastRadius + offset, Vector3.down, out hit[1], DetectionLenght, layersToDetect);
        Physics.Raycast(player.position - player.forward * RaycastRadius + offset, Vector3.down, out hit[2], DetectionLenght, layersToDetect);
        Physics.Raycast(player.position + player.right * RaycastRadius + offset, Vector3.down, out hit[3], DetectionLenght, layersToDetect);
        Physics.Raycast(player.position - player.right * RaycastRadius + offset, Vector3.down, out hit[4], DetectionLenght, layersToDetect);
    }

    public void OnDrawGizmos()
    {
        if (player == null) player = this.transform;
        
        Vector3[] startPositions = new Vector3[5];
        startPositions[0] = player.position + offset;
        startPositions[1] = player.position + player.forward * RaycastRadius + offset;
        startPositions[2] = player.position - player.forward * RaycastRadius + offset;
        startPositions[3] = player.position + player.right * RaycastRadius + offset;
        startPositions[4] = player.position - player.right * RaycastRadius + offset;
        
        for (int i = 0; i < startPositions.Length; i++)
        {
            if (hit != null && i < hit.Length && hit[i].transform != null)
            {
                // NUEVO: Color según si es slide o no
                bool isSlide = hit[i].collider.CompareTag(slideTag) || 
                               Vector3.Angle(hit[i].normal, Vector3.up) >= minSlideAngle;
                Gizmos.color = isSlide ? Color.yellow : Color.green;
                
                Gizmos.DrawLine(startPositions[i], hit[i].point);
                Gizmos.DrawSphere(hit[i].point, 0.1f);
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(startPositions[i], startPositions[i] + Vector3.down * DetectionLenght);
            }
        }
        
        if (combinedSlideDirection.magnitude > 0.1f)
        {
            Gizmos.color = allHitsAreSlide ? Color.cyan : Color.gray; // NUEVO: Gris si no todos son slide
            Gizmos.DrawRay(player.position, combinedSlideDirection * 2f);
        }
    }
}