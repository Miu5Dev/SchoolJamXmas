using System;
using UnityEngine;

public class PieceController : MonoBehaviour
{
    public GameObject piece1;
    public GameObject piece2;
    public GameObject piece3;
    public GameObject piece4;

    public void Update()
    {
        if (GameManager.Instance.sleighPartsCollected >= 1 && piece1 != null && !piece1.active)
        {
            piece1.SetActive(true);
        }
        if (GameManager.Instance.sleighPartsCollected >= 2 && piece1 != null && !piece2.active)
        {
            piece2.SetActive(true);
        }if (GameManager.Instance.sleighPartsCollected >= 3 && piece1 != null && !piece3.active)
        {
            piece3.SetActive(true);
        }if (GameManager.Instance.sleighPartsCollected >= 4 && piece1 != null && !piece4.active)
        {
            piece4.SetActive(true);
        }
        
        
        ////Disabeling

        if (GameManager.Instance.sleighPartsCollected < 4 && piece1 != null && piece1.active && piece2.active &&
            piece3.active && piece4.active)
        {
            piece1.SetActive(false);
            piece2.SetActive(false);
            piece3.SetActive(false);
            piece4.SetActive(false);
        }
        
        
    }
}
