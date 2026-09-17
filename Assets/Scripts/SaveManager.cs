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
        if (!Load()) //If save data not loaded correctly or not present, create new save file
        {
            globalPlayerState.hasAirDash = false;
            globalPlayerState.hasDoubleJump = false;
            globalPlayerState.hasRotation = false;
            globalPlayerState.levelsUnlocked = new bool[16];
            globalPlayerState.levelsUnlocked[0] = true;
            for (int i = 1; i < 16; i++) globalPlayerState.levelsUnlocked[i] = false;
            Save();
        }
    }

    public void Save() //Handles saving data
    {
        SaveData save = new SaveData{hasAirDash=globalPlayerState.hasAirDash, 
        hasDoubleJump=globalPlayerState.hasDoubleJump, 
        hasRotation=globalPlayerState.hasRotation,
        levelsUnlocked=globalPlayerState.levelsUnlocked,
        };

        string json = JsonUtility.ToJson(save);
        File.WriteAllText(path, json);
    }

    public bool Load() //Loads the file and checks for errors
    {
        if (File.Exists(path))
        {
            SaveData save;
            try
            {
                string json = File.ReadAllText(path);
                save = JsonUtility.FromJson<SaveData>(json);   
                globalPlayerState.hasAirDash=save.hasAirDash;
                globalPlayerState.hasDoubleJump=save.hasDoubleJump;
                globalPlayerState.hasRotation=save.hasRotation;
                globalPlayerState.levelsUnlocked = save.levelsUnlocked;
            }
            catch
            {
                return false;
            }
            if (save == null) return false;
            if (save.levelsUnlocked == null) return false;
            return true;
        }
        return false;
        
    }
}
