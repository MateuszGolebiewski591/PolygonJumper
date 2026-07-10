using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    [SerializeField] private string playerTag;
    [SerializeField] private GlobalPlayerState globalPlayerState;
    [SerializeField] private int respawnPointNumber;
    void OnTriggerEnter2D(Collider2D other)
    {
        globalPlayerState.respawnPoint = transform.position;
        SaveManager.Instance.Save();
    }

    public void SetID(int id)
    {
        respawnPointNumber = id;
    }
}
