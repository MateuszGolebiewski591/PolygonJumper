using UnityEngine;

public class CameraAnchor : MonoBehaviour
{
    [SerializeField] private PlayerMovement player;
    [SerializeField] float maxY = 5f;
    [SerializeField] private GameEvent gameEvent;
    [SerializeField] private GlobalPlayerState globalPlayerState;

    void LateUpdate()
    {
        float newX = player.transform.position.x;
        float newY = transform.position.y;
        if (Mathf.Abs(transform.position.y - player.transform.position.y) > maxY)
        {
            newY = player.transform.position.y + Mathf.Sign(transform.position.y - player.transform.position.y) * maxY;
        }
        transform.position = new Vector2(newX, newY);
    }

    void OnEnable()
    {
        gameEvent.OnEventRaised += HandleEvent;
    }

    void OnDisable()
    {
        gameEvent.OnEventRaised += HandleEvent; 
    }

    private void HandleEvent(EventData data)
    {
        switch (data.eventType)
        {
            case EventType.LevelReset :
                {
                    ResetCameraAnchor();
                    break;
                }
        }
    }

    void Awake()
    {
        ResetCameraAnchor();
    }

    private void ResetCameraAnchor()
    {
        transform.position = globalPlayerState.respawnPoint;
    }
}
