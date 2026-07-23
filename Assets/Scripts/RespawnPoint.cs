using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    [SerializeField] private string playerTag;
    [SerializeField] private GlobalPlayerState globalPlayerState;
    [SerializeField] private GameEvent gameEventChannel; 
    private int respawnPointNumber;
    void OnTriggerEnter2D(Collider2D other)
    {
        globalPlayerState.respawnPoint = transform.position;
        SaveManager.Instance.Save();
    }

    public void SetID(int id)
    {
        respawnPointNumber = id;
    }

    void OnEnable()
    {
        gameEventChannel.OnEventRaised += HandleEvent;
    }


    void OnDisable()
    {
        gameEventChannel.OnEventRaised -= HandleEvent;
    }

    private void HandleEvent(EventData data)
    {
        switch (data.eventType) {
            case EventType.LevelComplete :
                {
                    if (respawnPointNumber == 0) {
                        globalPlayerState.respawnPoint = transform.position;
                        SaveManager.Instance.Save();
                    }
                    break;
                }
        }
    }
}
