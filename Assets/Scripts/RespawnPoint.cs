using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    [SerializeField] private string playerTag;
    [SerializeField] private GlobalPlayerState globalPlayerState;
    [SerializeField] private GameEvent gameEventChannel; 
    [SerializeField] private Transform point; 
    private int respawnPointNumber;
    void OnTriggerEnter2D(Collider2D other)
    {
        globalPlayerState.respawnPoint = point.position;
        gameEventChannel.Raise(new EventData{eventType=EventType.CheckpointReached});
    }

    public void SetID(int id)
    {
        respawnPointNumber = id;
        if (id == 0) globalPlayerState.respawnPoint = point.position;
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
                        globalPlayerState.respawnPoint = point.position;
                    }
                    break;
                }
        }
    }
}
