using UnityEngine;

[CreateAssetMenu(fileName = "GlobalPlayerState", menuName = "Scriptable Objects/GlobalPlayerState")]
public class GlobalPlayerState : ScriptableObject
{
    public bool hasAirDash;
    public bool hasDoubleJump;
    public bool hasRotation;
    public Vector2 respawnPoint;
}
