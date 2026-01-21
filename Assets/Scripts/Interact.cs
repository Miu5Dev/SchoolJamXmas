using System;
using UnityEngine;

public class Interact : MonoBehaviour
{
    private void OnEnable()
    {
        EventBus.Subscribe<onInteractInputEvent>(doStuff);
    }
    
    private void OnDisable()
    {
        EventBus.Unsubscribe<onInteractInputEvent>(doStuff);
    }
    
    private void doStuff(onInteractInputEvent ev)
    {
       
    }
        
    
}
