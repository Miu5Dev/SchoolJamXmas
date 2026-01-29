using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField]private TMP_Text coinsCollected;
    [SerializeField]private TMP_Text sleighCollected;

    [SerializeField]private GameManager gameManager;
    
    public void OnEnable()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
    }

    public void Update()
    {
        coinsCollected.text = "Coins: " + gameManager.coinsCollected + " / " + gameManager.requiredCoins;
        sleighCollected.text = "Sleight Parts: " + gameManager.sleighPartsCollected + " / " + gameManager.requiredSleightParts;
    }
    
    
}
