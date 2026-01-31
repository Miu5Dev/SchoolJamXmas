using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
   [SerializeField]public int coinsCollected = 0;
   [SerializeField]public int requiredCoins = 0;
   [SerializeField]public int sleighPartsCollected = 0;
   [SerializeField]public int requiredSleightParts = 4;
   [SerializeField]public int CurrentLives = 3;
   
   public static GameManager Instance;

   private void Awake()
   {
      if (Instance == null)
      {
         Instance = this;
         transform.parent = null;
         DontDestroyOnLoad(gameObject);
      }
      else
      {
         Destroy(gameObject);
      }
   }
   
   void OnEnable()
   {
      SceneManager.sceneLoaded += OnSceneLoaded;
   }

   void OnDisable()
   {
      SceneManager.sceneLoaded -= OnSceneLoaded;
   }

   void OnSceneLoaded(Scene scene, LoadSceneMode mode)
   {
      if (scene.buildIndex != 0 && scene.buildIndex != 1)
      {
         GameObject[] coins = GameObject.FindGameObjectsWithTag("Coin");
         int coinCount = coins.Length;
         requiredCoins = coinCount;
      }
      
      #if UNITY_EDITOR
            GameObject[] coin = GameObject.FindGameObjectsWithTag("Coin");
            int coinCounts = coin.Length;
            requiredCoins = coinCounts;
      #endif
   }
   
   public void goToScene(int sceneID)
   {
      FadeManager.Instance.LoadSceneWithFade(sceneID);

   }

}
