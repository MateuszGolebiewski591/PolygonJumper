using UnityEngine;
using System.Collections;

public class CameraAnchor : MonoBehaviour
{
    [SerializeField] private PlayerMovement player;
    [SerializeField] float maxY = 5f;
    [SerializeField] private GameEvent gameEvent;
    [SerializeField] private GlobalPlayerState globalPlayerState;
    [SerializeField] private float cameraOffsetAmount = 3f;
    [SerializeField] private float timeBeforeAnchorReset = 1f;
    [SerializeField] private float centerSpeed = 1f;
    private Vector2 cameraOffset = Vector2.zero;
    private Vector2 lastPlayerLocation;
    private Vector2 referencePlayerPosition;
    private Vector2 savedAnchorLocation;
    private bool positionFreezing = false;
    private bool center = false;
    private Coroutine timer = null;
    private bool delayedCenter = false;
    private Vector2 referenceVector = Vector2.zero;

    void LateUpdate()
    {
        if (center) //Prioritise centering
        {
            transform.position = player.transform.position;
            center = false;
            delayedCenter = false;
            referencePlayerPosition = player.transform.position;
            if (timer != null) StopCoroutine(timer);
            return;
        }
        if (Vector2.Distance(referencePlayerPosition, player.transform.position) < 0.001f && Vector2.Distance(transform.position, player.transform.position) > 0.001f && !positionFreezing)
        {
            if (delayedCenter && !center) //Begins smoothly moving camera back towards player before the hard boundary which forcibly moves camera
            {
                transform.position = Vector2.SmoothDamp(transform.position, player.transform.position, ref referenceVector, centerSpeed);
                if (Vector2.Distance(transform.position, player.transform.position) < 0.01f) {
                    transform.position = player.transform.position;
                    delayedCenter = false;
                    referenceVector = Vector2.zero;
                }
                return;
            }
            referencePlayerPosition = player.transform.position;
            if (timer != null) return;
            timer = StartCoroutine(ResetDelay());
            return;
        }
        else if (timer != null) //cleanup after timer
        {
            StopCoroutine(timer);
            timer = null;
            delayedCenter = false;
            referenceVector = Vector2.zero;
        }
        if (positionFreezing) //Computes difference in player position and moves anchor accordingly, accounting for arrow keys offseting the camera
        {
            Vector2 currentPlayerLocation = player.transform.position;
            Vector2 difference = currentPlayerLocation - lastPlayerLocation;
            lastPlayerLocation = player.transform.position;
            savedAnchorLocation += difference;
            transform.position = savedAnchorLocation + cameraOffset * cameraOffsetAmount;
        }
        else //If anchor too far away from the player, move it closer to the player
        {
            float newX = player.transform.position.x;
            float newY = transform.position.y;
            if (Mathf.Abs(transform.position.y - player.transform.position.y) >= maxY)
            {
                newY = player.transform.position.y + Mathf.Sign(transform.position.y - player.transform.position.y) * maxY;
            }
            transform.position = new Vector2(newX, newY) + cameraOffset * cameraOffsetAmount;
        }
        referencePlayerPosition = player.transform.position;
    }

    void OnEnable()
    {
        gameEvent.OnEventRaised += HandleEvent;
    }

    void OnDisable()
    {
        gameEvent.OnEventRaised -= HandleEvent; 
    }

    private void HandleEvent(EventData data)
    {
        switch (data.eventType)
        {
            case EventType.LevelReset :
                {
                    ResetCameraAnchor();
                    break;
                }
        }
    }

    void Awake()
    {
        ResetCameraAnchor();
    }

    private void ResetCameraAnchor()
    {
        transform.position = globalPlayerState.respawnPoint;
        cameraOffset = Vector2.zero;
    }

    public void ApplyOffset(Vector2 anchorOffset) //Arrow key camera movement by player
    {
        cameraOffset = anchorOffset;
        if (cameraOffset != Vector2.zero) { //camera input
            if (!positionFreezing) { //locks in the camera offset and related variables if not already
                positionFreezing = true;
                savedAnchorLocation = transform.position;
                lastPlayerLocation = player.transform.position;
            }
        }
        else {//no input
            if (positionFreezing) //resets variables
            {
                positionFreezing = false;
                lastPlayerLocation = Vector2.zero;
                transform.position = savedAnchorLocation;
                savedAnchorLocation = Vector2.zero;
            }   
        }
    } 

    public void CenterAnchor() //Used by player for example during interaction with redirection pad
    {
        center = true;
    }  

    private IEnumerator ResetDelay() //Delay before camera is re-centred
    {
        yield return new WaitForSeconds(timeBeforeAnchorReset);
        delayedCenter = true;
        referenceVector = Vector2.zero;
        timer = null;
    }  
}
