using System;
using UnityEngine;

public class LocationTrigger : MonoBehaviour
{
    [SerializeField]private int sceneIDtoLoad;
    [SerializeField]private GameManager gameManager;
    public bool loadingLevel = false;

    public void OnEnable()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
    }
    
    public void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if(!loadingLevel){
                gameManager.goToScene(sceneIDtoLoad);
                loadingLevel = true;
            }
        }
    }
}
