using UnityEngine;
using System.Collections;

public class DeathParticleSystem : MonoBehaviour
{
    [SerializeField] private ParticleSystem largeDeathParticles;
    [SerializeField] private ParticleSystem smallDeathParticles;
    [SerializeField] private GameEvent gameEventChannel;
    [SerializeField] private Transform player;
    private bool hasDied = false;
    
    void OnEnable()
    {
        gameEventChannel.OnEventRaised += HandleEvent;
    }

    void OnDisable()
    {
        gameEventChannel.OnEventRaised -= HandleEvent;
    }

    private void PlayDeath()
    {
        smallDeathParticles.Play();
        largeDeathParticles.Play();
    }

    private void OnParticleSystemStopped()
    {
        StartCoroutine(DelayRespawn());
    }

    private IEnumerator DelayRespawn() //Relaying respawn while particles play
    {
        yield return new WaitForSeconds(0.2f);
        gameEventChannel.Raise(new EventData{eventType=EventType.LevelReset});
    }

    private void HandleEvent(EventData data)
    {
        switch (data.eventType)
        {
            case EventType.PlayerDeath :
                {
                    if (!hasDied)
                    {
                        transform.position = player.position;
                        smallDeathParticles.transform.position = player.position;
                        PlayDeath();
                        hasDied = true; //Prevents particle spamming
                    }
                    break;
                }
            case EventType.LevelReset :
                {
                    hasDied = false;
                    break;
                }
        }
    }
}
