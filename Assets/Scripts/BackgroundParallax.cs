using UnityEngine;

public class BackgroundParallax : MonoBehaviour
{
    [SerializeField] private GameObject[] backgroundLayers;
    [SerializeField] private float[] layerMovementSpeedsX;
    [SerializeField] private float[] layerMovementSpeedsY;
    [SerializeField] private GameEvent gameEventChannel;
    [SerializeField] private GlobalPlayerState globalPlayerState;
    [SerializeField] private Transform cameraPosition;
    private Vector3 previousCameraPosition;
    private Vector3[] offsets;
    private Vector3[] respawnOffsets;

    void OnEnable()
    {
        gameEventChannel.OnEventRaised += HandleEvent;
    }

    void OnDisable()
    {
        gameEventChannel.OnEventRaised -= HandleEvent;
    }

    void Awake()
    {
        offsets = new Vector3[backgroundLayers.Length]; //displacement of item based on current checkpoint
        respawnOffsets = new Vector3[backgroundLayers.Length]; //Used when checkpoint reached
        for (int i = 0; i < backgroundLayers.Length; i++) offsets[i] = backgroundLayers[i].transform.localPosition;    
    }

    void Update()
    { //Difference in camera position multiplied by how much we want the player to move in both the x and y axis
        Vector3 difference = cameraPosition.position - previousCameraPosition;
        for (int i = 0; i < backgroundLayers.Length; i++)
        {
            backgroundLayers[i].transform.localPosition += new Vector3(-difference.x*layerMovementSpeedsX[i], -difference.y*layerMovementSpeedsY[i], 0);
        }
        previousCameraPosition = cameraPosition.position;
    }

    private void HandleEvent(EventData eventData)
    {
        switch(eventData.eventType)
        {
            case EventType.LevelReset : //Resets positions to saved offsets
                {
                    previousCameraPosition = globalPlayerState.respawnPoint;
                    for (int i = 0; i < backgroundLayers.Length; i++) backgroundLayers[i].transform.localPosition = offsets[i];
                    break;
                }
            case EventType.CheckpointReached : //Computes remaining distance to checkpoint and therefore the correct offset
                {
                    Vector2 cam = cameraPosition.position;
                    Vector2 difference = globalPlayerState.respawnPoint - cam;
                    for (int i = 0; i < backgroundLayers.Length; i++)
                    {
                        respawnOffsets[i] = new Vector3(-difference.x*layerMovementSpeedsX[i], -difference.y*layerMovementSpeedsY[i], 0);
                        offsets[i] = new Vector3(backgroundLayers[i].transform.localPosition.x + respawnOffsets[i].x, backgroundLayers[i].transform.localPosition.y + respawnOffsets[i].y, offsets[i].z);
                    }
                    break;
                }
        }
    }
}
