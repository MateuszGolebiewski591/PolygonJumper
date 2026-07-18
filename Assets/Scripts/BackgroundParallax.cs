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
    private Vector2[] offsets;

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
        offsets = new Vector2[backgroundLayers.Length];
        for (int i = 0; i < backgroundLayers.Length; i++)
        {
            offsets[i] = backgroundLayers[i].transform.localPosition;
        }
    }

    void Update()
    {
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
            case EventType.LevelReset :
                {
                    for (int i = 0; i < backgroundLayers.Length; i++)
                    {
                        backgroundLayers[i].transform.localPosition = offsets[i];
                    }
                    break;
                }
        }
    }
}
