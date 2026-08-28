using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject frontPanel;
    [SerializeField] private GameObject levelPanel;
    [SerializeField] private GlobalPlayerState globalPlayerState;
    [SerializeField] private LevelData[] levelData;
    public void LoadLevel(string levelName)
    {
        foreach (LevelData data in levelData)
        {
            if (data.sceneName == levelName)
            {    
                if (globalPlayerState.levelsUnlocked[data.levelIndex]) SceneManager.LoadSceneAsync(levelName);
            }
        }
        
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void LoadLevelPanel()
    {
        levelPanel.SetActive(true);
        frontPanel.SetActive(false);
    }
    
    public void LoadFrontPanel()
    {
        frontPanel.SetActive(true);
        levelPanel.SetActive(false);
    }

}
