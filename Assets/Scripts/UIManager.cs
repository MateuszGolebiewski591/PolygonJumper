using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Menus")]
    [SerializeField] private GameObject PauseMenu;
    [SerializeField] private GameObject LevelCompleteMenu;

    [Header("References")]
    [SerializeField] private GameEvent gameEventChannel;

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
            case EventType.PauseGame :
                {
                    PauseMenu.SetActive(true);
                    break;
                }
            case EventType.ResumeGame :
                {
                    PauseMenu.SetActive(false);
                    break;
                }
            case EventType.LevelComplete :
                {
                    LevelCompleteMenu.SetActive(true);
                    AudioManager.Instance.PlayLevelCompleteSound();
                    break;
                }
        }
    }

    public void ResumeGame()
    {
        gameEventChannel.Raise(new EventData{eventType=EventType.ResumeGame});
    }

    public void ReturnToMainMenu()
    {
        gameEventChannel.Raise(new EventData{eventType=EventType.ResetCoreState});
        SceneManager.LoadSceneAsync(0);
    }

    public void NextLevel()
    {
        SceneManager.LoadSceneAsync(LevelManager.Instance.GetNextLevel()+1);
    }

}
