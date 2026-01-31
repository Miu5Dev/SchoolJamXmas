using System;
using UnityEngine;

public class endgameTrigger : MonoBehaviour
{
    public bool playerIn = false;

    public GameObject winDisplay;

    public void OnEnable()
    {
        winDisplay.SetActive(false);
        EventBus.Subscribe<OnActionInputEvent>(win);
    }


    public void win(OnActionInputEvent ev)
    {
        if (!playerIn) return;

        if (ev.pressed)
        {
            winDisplay.SetActive(true);
        }
        
    }
    
    public void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIn = true;
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerIn = false;
    }
}
