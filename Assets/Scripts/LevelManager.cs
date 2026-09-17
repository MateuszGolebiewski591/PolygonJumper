using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [SerializeField] public LevelData levelData;
    [SerializeField] private GameEvent gameEventChannel;
    [SerializeField] private GlobalPlayerState globalPlayerState;

    public static LevelManager Instance {get; set;}

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        gameEventChannel.Raise(new EventData{eventType=EventType.LevelReset});
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
        switch (data.eventType)
        {
            case EventType.LevelComplete : //Updates save file after completing a level
                {
                    globalPlayerState.levelsUnlocked[(levelData.levelIndex + 1) % globalPlayerState.levelsUnlocked.Length] = true;
                    SaveManager.Instance.Save();
                    break;
                }
        }
    }

    public int GetNextLevel() //Works out next level based on current level data
    {
        return (levelData.levelIndex + 1) % globalPlayerState.levelsUnlocked.Length;
    }
}
