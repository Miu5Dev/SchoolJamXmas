using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [SerializeField]private TMP_Text coinsCollected;
    [SerializeField]private TMP_Text sleighCollected;
    
    public void Update()
    {

        if (SceneManager.GetActiveScene().buildIndex == 0 || SceneManager.GetActiveScene().buildIndex == 1)
        {
            if (GameManager.Instance.CurrentLives != 3) GameManager.Instance.CurrentLives = 3;
            coinsCollected.text = "Coins: " + GameManager.Instance.coinsCollected;
            sleighCollected.text = "Sleigh Parts: " + GameManager.Instance.sleighPartsCollected + " / " + GameManager.Instance.requiredSleightParts;
        }
        else
        {
            coinsCollected.text = "Coins: " + GameManager.Instance.CoinsCollectedInLevel + " / " + GameManager.Instance.requiredCoins;
            sleighCollected.text = "Sleigh Parts: " + GameManager.Instance.sleighPartsCollected + " / " + GameManager.Instance.requiredSleightParts;
        }
    }
    
    
}
