using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance {get; set;}
    [SerializeField] private GlobalPlayerState globalPlayerState;
    [SerializeField] private GameEvent gameEventChannel; 
    public Transform startPoint;
    private int currentCheckpoint = 0;
    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        for (int i = 0; i < transform.childCount; i++) //Gives and ID to each checkpoint
        {
            transform.GetChild(i).GetComponent<RespawnPoint>().SetID(i);
        }
        startPoint = transform.GetChild(0).GetComponent<RespawnPoint>().point;
        globalPlayerState.respawnPoint = startPoint.position; //Sets initial spawn point
    }

    public void RespawnPointTriggered(int id, Transform position)
    {
        if (id <= currentCheckpoint) return; //Ignore existing checkpoints
        globalPlayerState.respawnPoint = position.position; //Update to new checkpoint
        gameEventChannel.Raise(new EventData{eventType=EventType.CheckpointReached});
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
                    globalPlayerState.respawnPoint = startPoint.position;   
                    break;
                }
        }
    }
}
