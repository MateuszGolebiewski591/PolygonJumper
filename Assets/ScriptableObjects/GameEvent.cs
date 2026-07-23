using UnityEngine;

[CreateAssetMenu(fileName = "GameEvent", menuName = "Scriptable Objects/GameEvent")]
public class GameEvent : Event<EventData> {}

public enum EventType
{
    PlayerDeath,
    LevelReset,
    PauseGame,
    ResumeGame,
    ResetCoreState,
    LevelComplete,
}

public struct EventData
{
    public EventType eventType;
}
