using System.Collections.Generic;
using UnityEngine;

public class RoomTemplate : MonoBehaviour
{
    [Header("Layout")]
    public Transform center;

    [Header("Anchors")]
    public Transform playerSpawn;
    public Transform exitAnchor;

    [Header("Spawning")]
    public Transform[] enemySpawns;
    public RoomSpawnProfileSO spawnProfile;

    [Header("Connection Sockets")]
    public Transform northSocket;
    public Transform southSocket;
    public Transform eastSocket;
    public Transform westSocket;

    [Header("Connection Prefabs")]
    public GameObject doorPrefab;
    public GameObject wallPrefab;

    private readonly List<GameObject> _spawnedConnections = new List<GameObject>();
    private readonly Dictionary<Vector2Int, GameObject> _spawnedSideObjects = new Dictionary<Vector2Int, GameObject>();

    public void ApplyConnections(bool openNorth, bool openSouth, bool openEast, bool openWest)
    {
        ClearConnections();

        BuildSide(Vector2Int.up, northSocket, openNorth, 0f);
        BuildSide(Vector2Int.right, eastSocket, openEast, -90f);
        BuildSide(Vector2Int.down, southSocket, openSouth, 180f);
        BuildSide(Vector2Int.left, westSocket, openWest, 90f);
    }

    private void BuildSide(Vector2Int dir, Transform socket, bool open, float zRotation)
    {
        if (socket == null) return;

        GameObject prefab = open ? doorPrefab : wallPrefab;
        if (prefab == null) return;

        GameObject obj = Instantiate(prefab, Vector3.zero, Quaternion.Euler(0f, 0f, zRotation), socket);

        Transform centerChild = obj.transform.Find("Center");
        if (centerChild != null)
        {
            Vector3 offset = obj.transform.position - centerChild.position;
            obj.transform.position = socket.position + offset;
        }
        else
        {
            obj.transform.position = socket.position;
        }

        _spawnedConnections.Add(obj);
        _spawnedSideObjects[dir] = obj;
    }

    private void ClearConnections()
    {
        for (int i = _spawnedConnections.Count - 1; i >= 0; i--)
        {
            if (_spawnedConnections[i] != null)
                Destroy(_spawnedConnections[i]);
        }

        _spawnedConnections.Clear();
        _spawnedSideObjects.Clear();
    }

    public GameObject GetSpawnedSideObject(Vector2Int dir)
    {
        if (_spawnedSideObjects.TryGetValue(dir, out GameObject obj))
            return obj;

        return null;
    }

    public Transform GetSocket(Vector2Int dir)
    {
        if (dir == Vector2Int.up) return northSocket;
        if (dir == Vector2Int.down) return southSocket;
        if (dir == Vector2Int.right) return eastSocket;
        if (dir == Vector2Int.left) return westSocket;
        return null;
    }
}