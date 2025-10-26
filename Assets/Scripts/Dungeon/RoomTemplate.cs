using UnityEngine;

public class RoomTemplate : MonoBehaviour
{
    [Header("Anchors in this room")]
    public Transform playerSpawn;  // where the player appears
    public Transform exitAnchor;   // where the exit door sits

    private void OnDrawGizmos()
    {
        if (playerSpawn)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(playerSpawn.position, 0.2f);
        }
        if (exitAnchor)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawCube(exitAnchor.position, Vector3.one * 0.4f);
        }
    }
}
