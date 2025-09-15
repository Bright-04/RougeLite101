using System.Collections.Generic;
using UnityEngine;

namespace RougeLite.World
{
    /// <summary>
    /// Creates an infinite tiling background that follows the player
    /// Perfect for expanding your map without manually placing hundreds of background tiles
    /// </summary>
    public class InfiniteBackground : MonoBehaviour
    {
        [Header("Background Settings")]
        [SerializeField] private GameObject backgroundTilePrefab;
        [SerializeField] private float tileSize = 20f; // Size of each background tile
        [SerializeField] private int tilesAroundPlayer = 2; // How many tiles to keep around player
        [SerializeField] private Transform player;
        [SerializeField] private bool autoFindPlayer = true;

        [Header("Layer Settings")]
        [SerializeField] private int backgroundSortingOrder = -10;
        [SerializeField] private string backgroundLayerName = "Background";

        private Dictionary<Vector2, GameObject> activeTiles = new Dictionary<Vector2, GameObject>();
        private Vector2 lastPlayerGridPosition;

        #region Unity Lifecycle

        private void Start()
        {
            // Auto-find player if not assigned
            if (autoFindPlayer && player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                    player = playerObj.transform;
            }

            if (player == null)
            {
                Debug.LogWarning("InfiniteBackground: No player found! Please assign a player transform.");
                return;
            }

            // Generate initial tiles
            GenerateTilesAroundPlayer();
        }

        private void Update()
        {
            if (player == null) return;

            Vector2 currentPlayerGridPosition = GetGridPosition(player.position);

            // Only update tiles if player moved to a new grid position
            if (currentPlayerGridPosition != lastPlayerGridPosition)
            {
                GenerateTilesAroundPlayer();
                RemoveDistantTiles();
                lastPlayerGridPosition = currentPlayerGridPosition;
            }
        }

        #endregion

        #region Tile Management

        private void GenerateTilesAroundPlayer()
        {
            Vector2 playerGridPos = GetGridPosition(player.position);

            // Generate tiles in a square around the player
            for (int x = -tilesAroundPlayer; x <= tilesAroundPlayer; x++)
            {
                for (int y = -tilesAroundPlayer; y <= tilesAroundPlayer; y++)
                {
                    Vector2 tileGridPos = playerGridPos + new Vector2(x, y);
                    
                    // Only create tile if it doesn't exist
                    if (!activeTiles.ContainsKey(tileGridPos))
                    {
                        CreateTileAt(tileGridPos);
                    }
                }
            }
        }

        private void CreateTileAt(Vector2 gridPosition)
        {
            if (backgroundTilePrefab == null)
            {
                Debug.LogWarning("InfiniteBackground: No background tile prefab assigned!");
                return;
            }

            Vector3 worldPosition = GridToWorldPosition(gridPosition);
            GameObject newTile = Instantiate(backgroundTilePrefab, worldPosition, Quaternion.identity, transform);
            
            // Set sorting order for proper layering
            SpriteRenderer spriteRenderer = newTile.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = backgroundSortingOrder;
                spriteRenderer.sortingLayerName = backgroundLayerName;
            }

            // Name the tile for easier debugging
            newTile.name = $"BackgroundTile_{gridPosition.x}_{gridPosition.y}";
            
            activeTiles[gridPosition] = newTile;
        }

        private void RemoveDistantTiles()
        {
            Vector2 playerGridPos = GetGridPosition(player.position);
            List<Vector2> tilesToRemove = new List<Vector2>();

            foreach (var kvp in activeTiles)
            {
                Vector2 tileGridPos = kvp.Key;
                float distance = Vector2.Distance(playerGridPos, tileGridPos);

                // Remove tiles that are too far from player
                if (distance > tilesAroundPlayer + 1)
                {
                    tilesToRemove.Add(tileGridPos);
                }
            }

            // Actually remove the tiles
            foreach (Vector2 tilePos in tilesToRemove)
            {
                if (activeTiles.ContainsKey(tilePos))
                {
                    if (activeTiles[tilePos] != null)
                        DestroyImmediate(activeTiles[tilePos]);
                    activeTiles.Remove(tilePos);
                }
            }
        }

        #endregion

        #region Helper Methods

        private Vector2 GetGridPosition(Vector3 worldPosition)
        {
            int gridX = Mathf.FloorToInt(worldPosition.x / tileSize);
            int gridY = Mathf.FloorToInt(worldPosition.y / tileSize);
            return new Vector2(gridX, gridY);
        }

        private Vector3 GridToWorldPosition(Vector2 gridPosition)
        {
            float worldX = gridPosition.x * tileSize + (tileSize / 2f);
            float worldY = gridPosition.y * tileSize + (tileSize / 2f);
            return new Vector3(worldX, worldY, 0);
        }

        #endregion

        #region Public Methods

        public void SetPlayer(Transform newPlayer)
        {
            player = newPlayer;
        }

        public void SetTileSize(float newSize)
        {
            tileSize = Mathf.Max(1f, newSize);
        }

        public void SetTilesAroundPlayer(int count)
        {
            tilesAroundPlayer = Mathf.Max(1, count);
        }

        public void SetBackgroundPrefab(GameObject prefab)
        {
            backgroundTilePrefab = prefab;
        }

        public void ClearAllTiles()
        {
            foreach (var kvp in activeTiles)
            {
                if (kvp.Value != null)
                    DestroyImmediate(kvp.Value);
            }
            activeTiles.Clear();
        }

        public void RegenerateAllTiles()
        {
            ClearAllTiles();
            GenerateTilesAroundPlayer();
        }

        #endregion

        #region Debug

        private void OnDrawGizmos()
        {
            if (player == null) return;

            // Draw grid around player
            Gizmos.color = Color.green;
            Vector2 playerGridPos = GetGridPosition(player.position);

            for (int x = -tilesAroundPlayer; x <= tilesAroundPlayer; x++)
            {
                for (int y = -tilesAroundPlayer; y <= tilesAroundPlayer; y++)
                {
                    Vector2 tileGridPos = playerGridPos + new Vector2(x, y);
                    Vector3 worldPos = GridToWorldPosition(tileGridPos);
                    Gizmos.DrawWireCube(worldPos, Vector3.one * tileSize);
                }
            }

            // Draw player position
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(player.position, 0.5f);
        }

        #endregion
    }
}