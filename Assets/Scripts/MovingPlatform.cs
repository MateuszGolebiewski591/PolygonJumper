using UnityEngine;
using System.Collections;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 1f;
    [SerializeField] private Transform pointsFolder;
    [SerializeField] private Transform platform;
    [SerializeField] private GameEvent gameEventChannel;
    private Vector2[] points;
    
    private Coroutine currentCycle;
    private int currentStartingIndex = 0;

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
            case EventType.LevelReset:
                {
                    ResetState();
                    break;
                }
            case EventType.ResetCoreState :
                {
                    ResetState();
                    break;
                }
        }
    }

    private void ResetState()
    {
        if (currentCycle != null) StopCoroutine(currentCycle);
        platform.position = points[0];
        currentStartingIndex = points.Length-1;
        BeginCycle();
    }

    void Awake()
    {
        points = new Vector2[pointsFolder.childCount];
        for (int i = 0; i < pointsFolder.childCount; i++)
        {
            points[i] = pointsFolder.GetChild(i).transform.position;
        }
        platform.position = points[0];
        currentStartingIndex = points.Length-1;
        pointsFolder.gameObject.SetActive(false);
        BeginCycle();
    }

    private void BeginCycle()
    {
        if (currentStartingIndex == 0) currentStartingIndex = points.Length-1;
        else currentStartingIndex = 0;
        currentCycle = StartCoroutine(Cycle());
    }

    private IEnumerator Cycle()
    {
        if (currentStartingIndex == 0)
        {
            for (int i = 1; i < points.Length; i++)
            {
                while (Vector2.Distance(platform.position, points[i]) > 0.01)
                {
                    platform.position = Vector2.MoveTowards(platform.position, points[i], movementSpeed * Time.deltaTime);
                    yield return null;
                }
                platform.position = points[i];
            }
        }
        else
        {
            for (int i = points.Length-2; i > -1; i--)
            {
                while (Vector2.Distance(platform.position, points[i]) > 0.01)
                {
                    platform.position = Vector2.MoveTowards(platform.position, points[i], movementSpeed * Time.deltaTime);
                    yield return null;
                }
                platform.position = points[i];
            }
        }
        BeginCycle();
        yield return null;
    }
}
