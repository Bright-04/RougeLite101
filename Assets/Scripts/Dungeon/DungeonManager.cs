using UnityEngine;
using System.Collections.Generic;

public class DungeonManager : MonoBehaviour
{
    [Min(1)] public int totalRooms = 10;
    [Min(1)] public int roomsPerTheme = 5;   // 2 blocks of 5 for 10 rooms
    public int seed = 0;                     // 0 => random each play

    [Header("Themes (order is used by blocks)")]
    public ThemeSO[] themes;                 // e.g., [Forest, Forest] for now

    [Header("Scene References")]
    public Transform roomsParent;            // empty object to hold rooms

    private System.Random _rng;
    private List<GameObject> _planPrefabs;   // chosen room prefab per index
    private int _index = -1;
    private GameObject _activeRoom;

    void Start()
    {
        int finalSeed = seed != 0 ? seed : Random.Range(int.MinValue, int.MaxValue);
        _rng = new System.Random(finalSeed);
        
        BuildPlan();
        LoadNextRoom();
    }
    
    void BuildPlan()
    {
        _planPrefabs = new List<GameObject>(totalRooms);

        for (int i = 0; i < totalRooms; i++)
        {
            int block = i / roomsPerTheme;
            var theme = themes[Mathf.Clamp(block, 0, themes.Length - 1)];

            var choice = theme.roomPrefabs[_rng.Next(0, theme.roomPrefabs.Length)];
            _planPrefabs.Add(choice);
        }
    }

    public void LoadNextRoom()
    {
        if (_activeRoom) 
        {
            Destroy(_activeRoom);
        }

        _index++;
        if (_index >= _planPrefabs.Count)
        {
            Debug.Log("Run complete!");
            // TODO: show victory screen
            return;
        }

        var prefab = _planPrefabs[_index];
        _activeRoom = Instantiate(prefab, Vector3.zero, Quaternion.identity, roomsParent);

        // Move player to this room's spawn
        var rt = _activeRoom.GetComponent<RoomTemplate>();
        if (rt && rt.playerSpawn)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player) 
            {
                player.transform.position = rt.playerSpawn.position;
                SetCameraBoundsImmediately(rt);
            }
        }
    }
    
    private void SetCameraBoundsImmediately(RoomTemplate rt)
    {
        var cameraController = FindFirstObjectByType<RougeLite.Camera.CameraController>();
        if (cameraController != null && rt != null)
        {
            var bounds = rt.GetCameraBounds();
            float halfWidth = bounds.size.x / 2f;
            float halfHeight = bounds.size.y / 2f;
            
            cameraController.SetBounds(
                bounds.center.x - halfWidth,  // minX
                bounds.center.x + halfWidth,  // maxX
                bounds.center.y - halfHeight, // minY
                bounds.center.y + halfHeight  // maxY
            );
            cameraController.EnableBounds(true);
            
            // Set camera to larger zoom for better POV
            Camera mainCam = Camera.main;
            if (mainCam != null && mainCam.orthographicSize < 12f)
            {
                mainCam.orthographicSize = 12f; // Match the defaultSize from CameraController
            }
        }
    }
}
