using UnityEngine;

public class RoomTemplate : MonoBehaviour
{
    [Header("Anchors")]
    public Transform playerSpawn;
    public Transform exitAnchor;

    [Header("Spawning")]
    public Transform[] enemySpawns;               // drop a few empties here
    public RoomSpawnProfileSO spawnProfile;       // assign per-room in Inspector

    private void OnDrawGizmos()
    {
        if (playerSpawn) { Gizmos.color = Color.cyan; Gizmos.DrawSphere(playerSpawn.position, 0.2f); }
        if (exitAnchor) { Gizmos.color = Color.yellow; Gizmos.DrawCube(exitAnchor.position, Vector3.one * 0.4f); }
        if (enemySpawns != null)
        {
            Gizmos.color = Color.red;
            foreach (var t in enemySpawns) if (t) Gizmos.DrawWireSphere(t.position, 0.2f);
        }
    }
}
