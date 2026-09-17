using UnityEngine;
using System.Collections;

public class RedirectPad : MonoBehaviour
{
    [SerializeField] private float triggerDistance = 0.2f;
    [SerializeField] private float cooldownTime = 1.5f;
    [SerializeField] private GameObject pointer;
    [SerializeField] private SpriteRenderer core;
    [SerializeField] private SpriteRenderer[] fins;
    [SerializeField] private RedirectionPadPulse pulseHandler;
    private Coroutine activeTracking = null;
    private bool cooldownActive = false;
    private Animator anim;
    private MaterialPropertyBlock propertyBlock;

    void Awake()
    {
        anim = GetComponent<Animator>();
        propertyBlock = new MaterialPropertyBlock();
        propertyBlock.SetFloat("_Activation", 0f);
        core.SetPropertyBlock(propertyBlock);
        foreach (SpriteRenderer fin in fins) fin.SetPropertyBlock(propertyBlock);
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

    private IEnumerator TrackPlayerPosition(Collider2D other) //If player is close enough trigger redirection states
    {
        while (Vector2.Distance(other.transform.position, transform.position) > triggerDistance) yield return null;
        other.GetComponent<PlayerMovement>().TriggerRedirection(this);
    }

    private IEnumerator Cooldown() //Prevents immediate reactivation of redirection pad
    {
        float elapsedTime = 0f;
        cooldownActive = true;
        while (elapsedTime < cooldownTime) {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        cooldownActive = false;
    }

    public void UpdatePointer(float angle) //Arrow update
    { 
        pointer.transform.rotation = Quaternion.Euler(new Vector3(0,0,angle));
    }

    public void LoadPad(float angle) //Activates the pad visually 
    {
        UpdatePointer(angle);
        pointer.gameObject.SetActive(true);
        propertyBlock.SetFloat("_Activation", 1f);
        core.SetPropertyBlock(propertyBlock);
        foreach (SpriteRenderer fin in fins) fin.SetPropertyBlock(propertyBlock);
    }

    public void UnloadPad() //Deactivates the pad visually
    {
        pointer.gameObject.SetActive(false);
        propertyBlock.SetFloat("_Activation", 0f);
        core.SetPropertyBlock(propertyBlock);
        foreach (SpriteRenderer fin in fins) fin.SetPropertyBlock(propertyBlock);
        pulseHandler.Pulse();
        anim.SetTrigger("Spin");
    }
}
