using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    [SerializeField] private string playerTag;
    [SerializeField] public Transform point; 
    private int respawnPointNumber;
    void OnTriggerEnter2D(Collider2D other) //Forwards checkpoint handling to checkpoint manager
    {
        CheckpointManager.Instance.RespawnPointTriggered(respawnPointNumber, point);
    }

    public void SetID(int id)
    {
        respawnPointNumber = id;
    }
}
