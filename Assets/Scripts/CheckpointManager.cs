using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    void Awake()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).GetComponent<RespawnPoint>().SetID(i);
        }
    }
}
