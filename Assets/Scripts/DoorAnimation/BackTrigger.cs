// BackTrigger.cs - Put on BackTrigger child
using UnityEngine;

public class BackTrigger : MonoBehaviour
{
    public DoorAnimationTrigger door;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            door.SetBackHitbox(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            door.SetBackHitbox(false);
        }
    }
}