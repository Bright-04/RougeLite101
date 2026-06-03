using UnityEngine;

[System.Serializable]
public class RoomNode
{
    public Vector2Int gridPos;
    public RoomType roomType;

    public bool openNorth;
    public bool openSouth;
    public bool openEast;
    public bool openWest;

    public GameObject chosenPrefab;
    public GameObject spawnedInstance;

    public RoomNode(Vector2Int pos)
    {
        gridPos = pos;
    }
}