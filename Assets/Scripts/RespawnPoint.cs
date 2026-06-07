using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    [SerializeField] private string playerTag;
    [SerializeField] private GlobalPlayerState globalPlayerState;
    void OnTriggerEnter2D(Collider2D other)
    {
        globalPlayerState.respawnPoint = transform.position;
    }
}
