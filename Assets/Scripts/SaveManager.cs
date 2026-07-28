using UnityEngine;
using System;
using System.IO;

public class SaveManager : MonoBehaviour
{
    [SerializeField] private GameEvent gameEventChannel;
    [SerializeField] private GlobalPlayerState globalPlayerState;
    private string path;

    public static SaveManager Instance {get; set;}

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        path = Path.Combine(Application.persistentDataPath,"save.json");
        if (!Load())
        {
            globalPlayerState.hasAirDash = false;
            globalPlayerState.hasDoubleJump = false;
            globalPlayerState.hasRotation = false;
            globalPlayerState.levelsUnlocked = new bool[16];
            globalPlayerState.levelsUnlocked[0] = true;
            for (int i = 1; i < 16; i++) globalPlayerState.levelsUnlocked[i] = false;
            Save();
        }
        else gameEventChannel.Raise(new EventData{eventType=EventType.LevelReset});
    }

    public void Save()
    {
        SaveData save = new SaveData{hasAirDash=globalPlayerState.hasAirDash, 
        hasDoubleJump=globalPlayerState.hasDoubleJump, 
        hasRotation=globalPlayerState.hasRotation,
        levelsUnlocked=globalPlayerState.levelsUnlocked,
        };

        string json = JsonUtility.ToJson(save);
        File.WriteAllText(path, json);
    }

    public void CreateSave()
    {
        SaveData save = new SaveData{hasAirDash=false, 
        hasDoubleJump=false, 
        hasRotation=false,
        levelsUnlocked=new bool[16],
        };
        save.levelsUnlocked[0] = true;
        for (int i = 1; i < 16; i++) save.levelsUnlocked[i] = false;
        string json = JsonUtility.ToJson(save);
        File.WriteAllText(path, json);
    }

    public bool Load()
    {
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            SaveData save = JsonUtility.FromJson<SaveData>(json);
            globalPlayerState.hasAirDash=save.hasAirDash;
            globalPlayerState.hasDoubleJump=save.hasDoubleJump;
            globalPlayerState.hasRotation=save.hasRotation;
            globalPlayerState.levelsUnlocked = save.levelsUnlocked;
            return true;
        }
        return false;
        
    }
    public void DeleteSave()
    {
        
    }
    public bool HasSave()
    {
        return false;
    }
}
