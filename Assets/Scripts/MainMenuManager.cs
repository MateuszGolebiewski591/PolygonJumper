using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject frontPanel;
    [SerializeField] private GameObject levelPanel;

    public void LoadLevel(string levelName)
    {
        SceneManager.LoadSceneAsync(levelName);
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
