using System;
using TMPro;
using UnityEngine;

public class GemsCounter : MonoBehaviour
{
    public GameObject winPanel;
    public int currentGems;
    public int requiredGems;
    
    public TMP_Text gemsCounter;

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "gem")
        {
            currentGems++;
            Destroy(other.gameObject);
            
            gemsCounter.text = $"currentGems: {currentGems} / {requiredGems}";
        }
    }

    void Update()
    {
        if (currentGems >= requiredGems)
        {
            winPanel.SetActive(true);
        }
    }
    
}
