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
    CheckpointReached,
    PortalEntered,
    NextLevel,
    MainMenu,
}

public struct EventData
{
    public EventType eventType;
}
