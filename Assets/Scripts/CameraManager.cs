using UnityEngine;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
   [SerializeField] private CinemachineCamera activeCamera;
   [SerializeField] private GameEvent gameEvent;
   [SerializeField] private GlobalPlayerState globalPlayerState;

    void OnEnable()
    {
        gameEvent.OnEventRaised += HandleEvent;
    }

    void OnDisable()
    {
        gameEvent.OnEventRaised += HandleEvent; 
    }

    void Awake()
    {
        ResetActiveCamera();
    }

    private void HandleEvent(EventData data)
    {
        switch (data.eventType)
        {
            case EventType.LevelReset :
                {
                    ResetActiveCamera();
                    break;
                }
        }
    }

    private void ResetActiveCamera()
    {
        activeCamera.PreviousStateIsValid = false;
        activeCamera.transform.position = globalPlayerState.respawnPoint;
    }
}
