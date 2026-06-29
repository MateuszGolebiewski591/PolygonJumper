using UnityEngine;
using System.Collections;

public class RedirectPad : MonoBehaviour
{
    [SerializeField] private float triggerDistance = 0.2f;
    [SerializeField] private float cooldownTime = 1.5f;
    [SerializeField] private GameObject pointer;
    private Coroutine activeTracking = null;
    private bool cooldownActive = false;
    private Animator anim;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || cooldownActive) return;
        activeTracking = StartCoroutine(TrackPlayerPosition(other));
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (activeTracking != null) StopCoroutine(activeTracking);
            StartCoroutine(Cooldown());
        }
    }

    private IEnumerator TrackPlayerPosition(Collider2D other)
    {
        while (Vector2.Distance(other.transform.position, transform.position) > triggerDistance) yield return null;
        other.GetComponent<PlayerMovement>().TriggerRedirection(this);
    }

    private IEnumerator Cooldown()
    {
        float elapsedTime = 0f;
        cooldownActive = true;
        while (elapsedTime < cooldownTime) {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        cooldownActive = false;
    }

    public void UpdatePointer(float angle)
    { 
        pointer.transform.rotation = Quaternion.Euler(new Vector3(0,0,angle));
    }

    public void LoadPad(float angle)
    {
        UpdatePointer(angle);
        pointer.gameObject.SetActive(true);
    }

    public void UnloadPad()
    {
        pointer.gameObject.SetActive(false);
    }
}
