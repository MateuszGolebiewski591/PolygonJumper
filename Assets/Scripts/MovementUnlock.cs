using UnityEngine;

public class MovementUnlock : MonoBehaviour
{
    [SerializeField] private GlobalPlayerState globalPlayerState;
    [SerializeField] private MovementTech tech;
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            switch (tech)
            {
                case MovementTech.airDash :
                    {
                        globalPlayerState.hasAirDash = true;
                        SaveManager.Instance.Save();
                        break;
                    }
                case MovementTech.doubleJump :
                    {
                        globalPlayerState.hasDoubleJump = true;
                        SaveManager.Instance.Save();
                        break;
                    }
                case MovementTech.redirection :
                    {
                        globalPlayerState.hasRotation = true;
                        SaveManager.Instance.Save();
                        break;
                    }
            }
        }
    }

    private enum MovementTech
    {
        airDash,
        doubleJump,
        redirection,
    }
}
