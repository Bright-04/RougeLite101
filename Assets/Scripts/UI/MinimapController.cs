using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using RougeLite.Events;
using RougeLite.World;

namespace RougeLite.UI
{
    /// <summary>
    /// Minimap system for infinite exploration
    /// Shows player position, discovered areas, enemies, and points of interest
    /// </summary>
    public class MinimapController : EventBehaviour, IEventListener<ChunkGeneratedEvent>, IEventListener<ChunkUnloadedEvent>
    {
        [Header("Minimap UI")]
        [SerializeField] private RawImage minimapImage;
        [SerializeField] private Transform minimapPlayerIcon;
        [SerializeField] private RectTransform minimapParent;
        [SerializeField] private float minimapScale = 0.1f;
        [SerializeField] private int minimapResolution = 512;

        [Header("Colors")]
        [SerializeField] private Color exploredColor = Color.gray;
        [SerializeField] private Color unexploredColor = Color.black;
        [SerializeField] private Color playerColor = Color.blue;
        [SerializeField] private Color enemyColor = Color.red;
        [SerializeField] private Color itemColor = Color.yellow;
        [SerializeField] private Color structureColor = Color.cyan;

        [Header("Settings")]
        [SerializeField] private Transform player;
        [SerializeField] private bool autoFindPlayer = true;
        [SerializeField] private bool showEnemies = true;
        [SerializeField] private bool showItems = true;
        [SerializeField] private bool showStructures = true;
        [SerializeField] private float updateInterval = 0.5f;

        private Texture2D minimapTexture;
        private Dictionary<Vector2Int, bool> exploredChunks = new Dictionary<Vector2Int, bool>();
        private List<MinimapIcon> minimapIcons = new List<MinimapIcon>();
        private float lastUpdateTime;
        private UnityEngine.Camera minimapCamera;

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            InitializeMinimap();
        }

        private void Start()
        {
            // Find player if not assigned
            if (autoFindPlayer && player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                    player = playerObj.transform;
            }

            // Subscribe to chunk events
            if (EventManager.Instance != null)
            {
                EventManager.Instance.Subscribe<ChunkGeneratedEvent>(this);
                EventManager.Instance.Subscribe<ChunkUnloadedEvent>(this);
            }
        }

        private void Update()
        {
            if (player == null) return;

            // Update minimap at intervals
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateMinimap();
                lastUpdateTime = Time.time;
            }

            UpdatePlayerIcon();
        }

        protected override void OnDestroy()
        {
            // Unsubscribe from events
            if (EventManager.Instance != null)
            {
                EventManager.Instance.Unsubscribe<ChunkGeneratedEvent>(this);
                EventManager.Instance.Unsubscribe<ChunkUnloadedEvent>(this);
            }

            base.OnDestroy();
        }

        #endregion

        #region Initialization

        private void InitializeMinimap()
        {
            // Create minimap texture
            minimapTexture = new Texture2D(minimapResolution, minimapResolution, TextureFormat.RGB24, false);
            minimapTexture.filterMode = FilterMode.Point;
            
            // Clear texture to unexplored color
            Color[] pixels = new Color[minimapResolution * minimapResolution];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = unexploredColor;
            }
            minimapTexture.SetPixels(pixels);
            minimapTexture.Apply();

            // Set texture to minimap image
            if (minimapImage != null)
            {
                minimapImage.texture = minimapTexture;
            }
        }

        #endregion

        #region Minimap Updates

        private void UpdateMinimap()
        {
            if (minimapTexture == null || player == null) return;

            // Update explored areas
            UpdateExploredAreas();
            
            // Update icons (enemies, items, etc.)
            UpdateMinimapIcons();
            
            // Apply changes to texture
            minimapTexture.Apply();
        }

        private void UpdateExploredAreas()
        {
            Vector2 playerPos = new Vector2(player.position.x, player.position.y);
            
            // Mark current area as explored
            Vector2Int currentChunk = WorldToChunkPosition(playerPos);
            MarkChunkAsExplored(currentChunk);
            
            // Mark nearby chunks as partially explored
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Vector2Int nearbyChunk = currentChunk + new Vector2Int(x, y);
                    if (!exploredChunks.ContainsKey(nearbyChunk))
                    {
                        MarkChunkAsExplored(nearbyChunk);
                    }
                }
            }
        }

        private void MarkChunkAsExplored(Vector2Int chunkPosition)
        {
            if (exploredChunks.ContainsKey(chunkPosition)) return;

            exploredChunks[chunkPosition] = true;
            
            // Update texture for this chunk
            Vector2Int texturePos = ChunkToTexturePosition(chunkPosition);
            int chunkSizeInTexture = 16; // Each chunk is 16x16 pixels on minimap
            
            for (int x = 0; x < chunkSizeInTexture; x++)
            {
                for (int y = 0; y < chunkSizeInTexture; y++)
                {
                    int texX = texturePos.x + x;
                    int texY = texturePos.y + y;
                    
                    if (texX >= 0 && texX < minimapResolution && texY >= 0 && texY < minimapResolution)
                    {
                        minimapTexture.SetPixel(texX, texY, exploredColor);
                    }
                }
            }
        }

        private void UpdateMinimapIcons()
        {
            // Clear old icons
            ClearMinimapIcons();

            if (!showEnemies && !showItems && !showStructures) return;

            // Find objects around player
            Vector3 playerPos = player.position;
            float searchRadius = 100f; // Search radius around player

            if (showEnemies)
            {
                GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
                foreach (GameObject enemy in enemies)
                {
                    float distance = Vector3.Distance(playerPos, enemy.transform.position);
                    if (distance <= searchRadius)
                    {
                        AddMinimapIcon(enemy.transform.position, enemyColor, MinimapIconType.Enemy);
                    }
                }
            }

            if (showItems)
            {
                GameObject[] items = GameObject.FindGameObjectsWithTag("Item");
                foreach (GameObject item in items)
                {
                    float distance = Vector3.Distance(playerPos, item.transform.position);
                    if (distance <= searchRadius)
                    {
                        AddMinimapIcon(item.transform.position, itemColor, MinimapIconType.Item);
                    }
                }
            }

            if (showStructures)
            {
                GameObject[] structures = GameObject.FindGameObjectsWithTag("Structure");
                foreach (GameObject structure in structures)
                {
                    float distance = Vector3.Distance(playerPos, structure.transform.position);
                    if (distance <= searchRadius)
                    {
                        AddMinimapIcon(structure.transform.position, structureColor, MinimapIconType.Structure);
                    }
                }
            }
        }

        private void UpdatePlayerIcon()
        {
            if (minimapPlayerIcon == null || player == null) return;

            // Update player icon position on minimap
            Vector2 playerWorldPos = new Vector2(player.position.x, player.position.y);
            Vector2 minimapPos = WorldToMinimapPosition(playerWorldPos);
            
            ((RectTransform)minimapPlayerIcon).anchoredPosition = minimapPos;
        }

        #endregion

        #region Icon Management

        private void AddMinimapIcon(Vector3 worldPosition, Color iconColor, MinimapIconType iconType)
        {
            Vector2 minimapPos = WorldToMinimapPosition(new Vector2(worldPosition.x, worldPosition.y));
            
            // Create icon data
            MinimapIcon icon = new MinimapIcon
            {
                worldPosition = worldPosition,
                minimapPosition = minimapPos,
                color = iconColor,
                type = iconType
            };
            
            minimapIcons.Add(icon);
            
            // Draw icon on texture
            DrawIconOnTexture(minimapPos, iconColor);
        }

        private void DrawIconOnTexture(Vector2 minimapPos, Color color)
        {
            // Convert minimap position to texture coordinates
            int texX = Mathf.RoundToInt(minimapPos.x * minimapResolution / minimapParent.rect.width + minimapResolution / 2f);
            int texY = Mathf.RoundToInt(minimapPos.y * minimapResolution / minimapParent.rect.height + minimapResolution / 2f);
            
            // Draw a small cross or dot
            int iconSize = 2;
            for (int x = -iconSize; x <= iconSize; x++)
            {
                for (int y = -iconSize; y <= iconSize; y++)
                {
                    int finalX = texX + x;
                    int finalY = texY + y;
                    
                    if (finalX >= 0 && finalX < minimapResolution && finalY >= 0 && finalY < minimapResolution)
                    {
                        if (Mathf.Abs(x) + Mathf.Abs(y) <= iconSize)
                        {
                            minimapTexture.SetPixel(finalX, finalY, color);
                        }
                    }
                }
            }
        }

        private void ClearMinimapIcons()
        {
            minimapIcons.Clear();
            
            // Redraw explored areas (this clears icons)
            foreach (var chunk in exploredChunks)
            {
                Vector2Int texturePos = ChunkToTexturePosition(chunk.Key);
                int chunkSizeInTexture = 16;
                
                for (int x = 0; x < chunkSizeInTexture; x++)
                {
                    for (int y = 0; y < chunkSizeInTexture; y++)
                    {
                        int texX = texturePos.x + x;
                        int texY = texturePos.y + y;
                        
                        if (texX >= 0 && texX < minimapResolution && texY >= 0 && texY < minimapResolution)
                        {
                            minimapTexture.SetPixel(texX, texY, exploredColor);
                        }
                    }
                }
            }
        }

        #endregion

        #region Helper Methods

        private Vector2Int WorldToChunkPosition(Vector2 worldPosition)
        {
            int chunkSize = 50; // Should match your world generator chunk size
            return new Vector2Int(
                Mathf.FloorToInt(worldPosition.x / chunkSize),
                Mathf.FloorToInt(worldPosition.y / chunkSize)
            );
        }

        private Vector2Int ChunkToTexturePosition(Vector2Int chunkPosition)
        {
            int chunkSizeInTexture = 16;
            int centerX = minimapResolution / 2;
            int centerY = minimapResolution / 2;
            
            return new Vector2Int(
                centerX + chunkPosition.x * chunkSizeInTexture,
                centerY + chunkPosition.y * chunkSizeInTexture
            );
        }

        private Vector2 WorldToMinimapPosition(Vector2 worldPosition)
        {
            Vector2 playerPos = new Vector2(player.position.x, player.position.y);
            Vector2 relativePos = worldPosition - playerPos;
            
            return relativePos * minimapScale;
        }

        #endregion

        #region Event Handlers

        public void OnEventReceived(ChunkGeneratedEvent eventData)
        {
            // Mark chunk as explored when generated
            MarkChunkAsExplored(eventData.ChunkPosition);
        }

        public void OnEventReceived(ChunkUnloadedEvent eventData)
        {
            // Keep chunk marked as explored even when unloaded
            // This maintains the exploration history
        }

        #endregion

        #region Public Methods

        public void SetPlayer(Transform newPlayer)
        {
            player = newPlayer;
        }

        /// <summary>
        /// Set the zoom level of the minimap
        /// </summary>
        public void SetZoom(float zoom)
        {
            minimapScale = Mathf.Clamp(zoom, 0.1f, 5f);
            // Force refresh of minimap
            UpdateMinimap();
        }

        /// <summary>
        /// Center the minimap on the player
        /// </summary>
        public void CenterOnPlayer()
        {
            if (player != null)
            {
                // The minimap already follows the player, so we just need to update display
                UpdateMinimap();
            }
        }

        public void SetMinimapScale(float scale)
        {
            minimapScale = Mathf.Max(0.01f, scale);
        }

        public void ToggleEnemyDisplay()
        {
            showEnemies = !showEnemies;
        }

        public void ToggleItemDisplay()
        {
            showItems = !showItems;
        }

        public void ToggleStructureDisplay()
        {
            showStructures = !showStructures;
        }

        public void ClearExploredAreas()
        {
            exploredChunks.Clear();
            InitializeMinimap();
        }

        public bool IsChunkExplored(Vector2Int chunkPosition)
        {
            return exploredChunks.ContainsKey(chunkPosition);
        }

        public int GetExploredChunkCount()
        {
            return exploredChunks.Count;
        }

        #endregion
    }

    #region Data Structures

    [System.Serializable]
    public class MinimapIcon
    {
        public Vector3 worldPosition;
        public Vector2 minimapPosition;
        public Color color;
        public MinimapIconType type;
    }

    public enum MinimapIconType
    {
        Enemy,
        Item,
        Structure,
        Player,
        Waypoint
    }

    #endregion
}