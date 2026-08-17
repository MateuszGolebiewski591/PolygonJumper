using UnityEngine;
using System.Collections;

public class CompletionParticleSystem : MonoBehaviour
{
    [SerializeField] private ParticleSystem largeCompletionParticles;
    [SerializeField] private ParticleSystem smallCompletionParticles;
    [SerializeField] private GameEvent gameEventChannel;
    [SerializeField] private Transform player;
    
    void OnEnable()
    {
        gameEventChannel.OnEventRaised += HandleEvent;
    }

    void OnDisable()
    {
        gameEventChannel.OnEventRaised -= HandleEvent;
    }

    private void PlayCompletion()
    {
        smallCompletionParticles.Play();
        largeCompletionParticles.Play();
    }

    private void OnParticleSystemStopped()
    {
        StartCoroutine(DelayCompletion());
    }

    private IEnumerator DelayCompletion()
    {
        yield return new WaitForSeconds(0.2f);
        gameEventChannel.Raise(new EventData{eventType=EventType.LevelComplete});
    }

    private void HandleEvent(EventData data)
    {
        if (largeCompletionParticles == null || smallCompletionParticles == null) return;
        switch (data.eventType)
        {
            case EventType.PortalEntered :
                {
                    transform.position = player.position;
                    smallCompletionParticles.transform.position = player.position;
                    PlayCompletion();
                    break;
                }
        }
    }
}
