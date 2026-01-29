using System;
using UnityEngine;

public class Collectable : MonoBehaviour
{
    [SerializeField]private bool SleighPiece = false;
    [SerializeField]private GameManager gameManager;
    [SerializeField]private float updownRange = 1f; 

    [SerializeField]private float rotationSpeed = 100f; // Degrees per second
    [SerializeField]private float updownspeed = 100f;
    
    private float startY;
    
    public void OnEnable()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
    }

    void Start()
    {
        startY = transform.position.y; 
    }
    
    void Update()
    {
        float yPosition = startY + Mathf.PingPong(Time.time * updownspeed, updownRange * 2f) - updownRange;
        transform.position = new Vector3(transform.position.x, yPosition, transform.position.z);
            
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0); // Rotate around Y-axis
    }
    
    public void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (SleighPiece)
                gameManager.sleighPartsCollected += 1;
            else
                gameManager.coinsCollected += 1;
            Destroy(gameObject);
        }
    }
    
    
}
