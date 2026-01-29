using System;
using UnityEngine;

public class LocationTrigger : MonoBehaviour
{
    [SerializeField]private int sceneIDtoLoad;
    [SerializeField]private GameManager gameManager;

    public void OnEnable()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
    }
    
    public void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            gameManager.goToScene(sceneIDtoLoad);
        }
    }
}
