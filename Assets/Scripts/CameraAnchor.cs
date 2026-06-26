using UnityEngine;

public class CameraAnchor : MonoBehaviour
{
    [SerializeField] private PlayerMovement player;
    [SerializeField] float maxY = 5f;
    [SerializeField] private GameEvent gameEvent;
    [SerializeField] private GlobalPlayerState globalPlayerState;
    [SerializeField] private float cameraOffsetAmount = 3f;
    private Vector2 cameraOffset = Vector2.zero;
    private Vector2 lastPlayerLocation;
    private Vector2 savedAnchorLocation;
    private bool positionFreezing = false;

    void LateUpdate()
    {
        if (positionFreezing)
        {
            Vector2 currentPlayerLocation = player.transform.position;
            Vector2 difference = currentPlayerLocation - lastPlayerLocation;
            lastPlayerLocation = player.transform.position;
            savedAnchorLocation += difference;
            transform.position = savedAnchorLocation + cameraOffset * cameraOffsetAmount;
        }
        else
        {
            float newX = player.transform.position.x;
            float newY = transform.position.y;
            if (Mathf.Abs(transform.position.y - player.transform.position.y) > maxY)
            {
                newY = player.transform.position.y + Mathf.Sign(transform.position.y - player.transform.position.y) * maxY;
            }
            transform.position = new Vector2(newX, newY) + cameraOffset * cameraOffsetAmount;
        }
        
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
        cameraOffset = Vector2.zero;
    }

    public void ApplyOffset(Vector2 anchorOffset)
    {
        cameraOffset = anchorOffset;
        if (cameraOffset != Vector2.zero) {
            if (!positionFreezing) {
                positionFreezing = true;
                savedAnchorLocation = transform.position;
                lastPlayerLocation = player.transform.position;
            }
        }
        else {
            if (positionFreezing)
            {
                positionFreezing = false;
                lastPlayerLocation = Vector2.zero;
                transform.position = savedAnchorLocation;
                savedAnchorLocation = Vector2.zero;
            }   
        }
    }    
}
