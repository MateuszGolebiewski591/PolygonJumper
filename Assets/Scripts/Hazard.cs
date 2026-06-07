using UnityEngine;

public class Hazard : MonoBehaviour
{
    [SerializeField] private string playerTag;
    [SerializeField] private GameEvent eventChannel;
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            eventChannel.Raise(new EventData{eventType=EventType.PlayerDeath});
        }
    }
}
