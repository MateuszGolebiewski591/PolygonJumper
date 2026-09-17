using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject frontPanel;
    [SerializeField] private GameObject levelPanel;
    [SerializeField] private GameObject ackowledgementsPanel;
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

    void Start()
    {
        Button[] buttons = levelPanel.GetComponentsInChildren<Button>(true);
        ColorUtility.TryParseHtmlString("#B7AEAEFF", out Color lockedColour);
        ColorUtility.TryParseHtmlString("#FFFFFFFF", out Color unlockedColour);
        for (int i = 0; i < globalPlayerState.levelsUnlocked.Length; i++) //Level buttons updated according to individual level scriptable objects
        {
            TextMeshProUGUI[] text = buttons[i].gameObject.GetComponentsInChildren<TextMeshProUGUI>(true); //Text
            text[0].text = "Level " + (i+1);
            text[1].text = levelData[i].displayName;
            if (!globalPlayerState.levelsUnlocked[i]) {
                foreach (Transform child in buttons[i].transform) //Colour
                {
                    Image image = child.GetComponent<Image>();
                    if (image != null) image.color = lockedColour;
                }
            }    
            else buttons[i].gameObject.GetComponentInChildren<Image>().color = unlockedColour;
        }
    }

    //Simple Button functions
    public void QuitGame() 
    {
        Application.Quit();
    }

    public void LoadLevelPanel()
    {
        levelPanel.SetActive(true);
        frontPanel.SetActive(false);
        ackowledgementsPanel.SetActive(false);
    }
    
    public void LoadFrontPanel()
    {
        frontPanel.SetActive(true);
        levelPanel.SetActive(false);
        ackowledgementsPanel.SetActive(false);
    }

    public void LoadAcknowledgementsPanel()
    {
        frontPanel.SetActive(false);
        levelPanel.SetActive(false);
        ackowledgementsPanel.SetActive(true);
    }
}
