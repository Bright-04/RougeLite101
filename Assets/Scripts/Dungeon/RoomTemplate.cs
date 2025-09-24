using UnityEngine;

public class RoomTemplate : MonoBehaviour
{
    [Header("Anchors in this room")]
    public Transform playerSpawn;  // where the player appears
    public Transform exitAnchor;   // where the exit door sits

    [Header("Camera Bounds for this room")]
    public float roomWidth = 120f;  // Doubled from 60 to 120 units - massive room
    public float roomHeight = 80f;  // Doubled from 40 to 80 units - massive room
    public Vector2 roomCenter = Vector2.zero; // center offset from this transform

    private void Start()
    {
        // Room template initialized - logging disabled for cleaner console
    }
    
    private System.Collections.IEnumerator DrawRuntimeBounds()
    {
        while (true)
        {
            // Center bounds around actual player position for accurate positioning
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Vector3 center;
            
            if (player != null)
            {
                center = player.transform.position + (Vector3)roomCenter;
            }
            else if (playerSpawn != null)
            {
                center = playerSpawn.position + (Vector3)roomCenter;
            }
            else
            {
                center = transform.position + (Vector3)roomCenter;
            }
            
            float halfWidth = roomWidth / 2f;
            float halfHeight = roomHeight / 2f;
            
            // Draw room boundary lines in runtime (visible in Scene view during play)
            Vector3 topLeft = center + new Vector3(-halfWidth, halfHeight, 0);
            Vector3 topRight = center + new Vector3(halfWidth, halfHeight, 0);
            Vector3 bottomRight = center + new Vector3(halfWidth, -halfHeight, 0);
            Vector3 bottomLeft = center + new Vector3(-halfWidth, -halfHeight, 0);
            
            // Draw the rectangle
            Debug.DrawLine(topLeft, topRight, Color.red, 0.1f);
            Debug.DrawLine(topRight, bottomRight, Color.red, 0.1f);
            Debug.DrawLine(bottomRight, bottomLeft, Color.red, 0.1f);
            Debug.DrawLine(bottomLeft, topLeft, Color.red, 0.1f);
            
            // Draw diagonal lines to make it more visible
            Debug.DrawLine(topLeft, bottomRight, Color.red, 0.1f);
            Debug.DrawLine(topRight, bottomLeft, Color.red, 0.1f);
            
            // Draw player position marker for debugging
            if (player != null)
            {
                Debug.DrawLine(player.transform.position + Vector3.up, player.transform.position + Vector3.down, Color.cyan, 0.1f);
                Debug.DrawLine(player.transform.position + Vector3.left, player.transform.position + Vector3.right, Color.cyan, 0.1f);
            }
            
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                // Draw camera viewport bounds
                float camHeight = mainCam.orthographicSize;
                float camWidth = camHeight * mainCam.aspect;
                Vector3 camPos = mainCam.transform.position;
                
                Vector3 camTL = camPos + new Vector3(-camWidth, camHeight, 0);
                Vector3 camTR = camPos + new Vector3(camWidth, camHeight, 0);
                Vector3 camBR = camPos + new Vector3(camWidth, -camHeight, 0);
                Vector3 camBL = camPos + new Vector3(-camWidth, -camHeight, 0);
                
                Debug.DrawLine(camTL, camTR, Color.yellow, 0.1f);
                Debug.DrawLine(camTR, camBR, Color.yellow, 0.1f);
                Debug.DrawLine(camBR, camBL, Color.yellow, 0.1f);
                Debug.DrawLine(camBL, camTL, Color.yellow, 0.1f);
            }
            
            yield return new WaitForSeconds(0.1f);
        }
    }

    public Bounds GetCameraBounds()
    {
        // Create bounds that define the room boundaries for camera constraints
        // Center around the room itself, not the player, so camera can follow player within the room
        Vector3 center;
        
        if (playerSpawn != null)
        {
            // Use player spawn as room center reference
            center = playerSpawn.position + (Vector3)roomCenter;
        }
        else
        {
            // Fallback to room transform
            center = transform.position + (Vector3)roomCenter;
        }
        
        // Create bounds that allow camera to follow player with minimal restrictions
        Camera mainCam = Camera.main;
        float cameraHeight = mainCam ? mainCam.orthographicSize : 12f;
        float cameraWidth = cameraHeight * (mainCam ? mainCam.aspect : 1.78f); // Full HD 16:9 aspect
        
        // Use almost the full room size for camera movement - just keep a small margin
        float boundsWidth = roomWidth - 4f; // Small 2-unit margin on each side
        float boundsHeight = roomHeight - 4f; // Small 2-unit margin on each side
        
        // Ensure we have reasonable bounds even if room is very small
        boundsWidth = Mathf.Max(boundsWidth, 10f);
        boundsHeight = Mathf.Max(boundsHeight, 10f);
        
        Bounds bounds = new Bounds(center, new Vector3(boundsWidth, boundsHeight, 0));
        
        // Camera bounds calculated - logging disabled for cleaner console
        
        return bounds;
    }
    
    public Bounds GetRoomBounds()
    {
        // Get the actual full room boundaries for wall placement
        Vector3 center;
        
        if (playerSpawn != null)
        {
            // Use player spawn as room center reference
            center = playerSpawn.position + (Vector3)roomCenter;
        }
        else
        {
            // Fallback to room transform
            center = transform.position + (Vector3)roomCenter;
        }
        
        // Return the full room size for wall boundaries
        Bounds roomBounds = new Bounds(center, new Vector3(roomWidth, roomHeight, 0));
        
        // Room bounds calculated - logging disabled for cleaner console
        
        return roomBounds;
    }
}
