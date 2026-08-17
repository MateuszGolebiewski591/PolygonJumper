using UnityEngine;

public class LevelComplete : MonoBehaviour
{
    [SerializeField] private GameEvent gameEventChannel;
    [SerializeField] private GlobalPlayerState globalPlayerState;
    private BoxCollider2D collider;

    void Awake()
    {
        collider = GetComponent<BoxCollider2D>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) other.GetComponent<PlayerMovement>().TriggerPortal(this);
    }

    
}
