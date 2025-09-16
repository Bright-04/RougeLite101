using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace RougeLite.UI
{
    /// <summary>
    /// Compass system for infinite exploration
    /// Shows direction to points of interest and discovered locations
    /// </summary>
    public class CompassController : MonoBehaviour
    {
        [Header("Compass UI")]
        [SerializeField] private RectTransform compassBar;
        [SerializeField] private Transform compassIconsParent;
        [SerializeField] private GameObject compassIconPrefab;
        [SerializeField] private Text directionText;
        [SerializeField] private float compassWidth = 360f;

        [Header("Player")]
        [SerializeField] private Transform player;
        [SerializeField] private Transform playerCamera;
        [SerializeField] private bool autoFindPlayer = true;

        [Header("Compass Settings")]
        [SerializeField] private float maxCompassDistance = 200f;
        [SerializeField] private bool showCardinalDirections = true;
        [SerializeField] private bool showStructures = true;
        [SerializeField] private bool showEnemies = false; // Usually too cluttered
        [SerializeField] private bool showItems = true;

        [Header("Colors")]
        [SerializeField] private Color structureColor = Color.cyan;
        [SerializeField] private Color enemyColor = Color.red;
        [SerializeField] private Color itemColor = Color.yellow;
        [SerializeField] private Color waypointColor = Color.green;

        private List<CompassIcon> compassIcons = new List<CompassIcon>();
        private List<CompassWaypoint> waypoints = new List<CompassWaypoint>();

        #region Unity Lifecycle

        private void Start()
        {
            // Find player if not assigned
            if (autoFindPlayer && player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    player = playerObj.transform;
                    
                    // Use player's camera if available
                    if (playerCamera == null)
                    {
                        playerCamera = UnityEngine.Camera.main?.transform;
                    }
                }
            }

            // Create cardinal direction markers
            if (showCardinalDirections)
            {
                CreateCardinalDirections();
            }
        }

        private void Update()
        {
            if (player == null) return;

            UpdateCompass();
            UpdateDirectionText();
        }

        #endregion

        #region Compass Updates

        private void UpdateCompass()
        {
            // Clear existing icons
            ClearCompassIcons();

            // Get player rotation for compass orientation
            float playerRotation = GetPlayerRotation();

            // Find nearby objects to display on compass
            FindNearbyObjects();

            // Update compass icons
            foreach (var waypoint in waypoints)
            {
                UpdateCompassIcon(waypoint, playerRotation);
            }
        }

        private float GetPlayerRotation()
        {
            if (playerCamera != null)
            {
                return playerCamera.eulerAngles.z;
            }
            else if (player != null)
            {
                return player.eulerAngles.z;
            }
            return 0f;
        }

        private void FindNearbyObjects()
        {
            if (player == null) return;

            Vector3 playerPos = player.position;
            
            // Clear dynamic waypoints (keep manual waypoints)
            waypoints.RemoveAll(w => w.isDynamic);

            // Add structures
            if (showStructures)
            {
                GameObject[] structures = GameObject.FindGameObjectsWithTag("Structure");
                foreach (GameObject structure in structures)
                {
                    float distance = Vector3.Distance(playerPos, structure.transform.position);
                    if (distance <= maxCompassDistance && distance > 5f) // Not too close
                    {
                        AddDynamicWaypoint(structure.transform.position, structure.name, structureColor, CompassIconType.Structure);
                    }
                }
            }

            // Add enemies
            if (showEnemies)
            {
                GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
                foreach (GameObject enemy in enemies)
                {
                    float distance = Vector3.Distance(playerPos, enemy.transform.position);
                    if (distance <= maxCompassDistance && distance > 10f)
                    {
                        AddDynamicWaypoint(enemy.transform.position, "Enemy", enemyColor, CompassIconType.Enemy);
                    }
                }
            }

            // Add items
            if (showItems)
            {
                GameObject[] items = GameObject.FindGameObjectsWithTag("Item");
                foreach (GameObject item in items)
                {
                    float distance = Vector3.Distance(playerPos, item.transform.position);
                    if (distance <= maxCompassDistance && distance > 3f)
                    {
                        AddDynamicWaypoint(item.transform.position, "Item", itemColor, CompassIconType.Item);
                    }
                }
            }
        }

        private void UpdateCompassIcon(CompassWaypoint waypoint, float playerRotation)
        {
            if (player == null) return;

            Vector3 playerPos = player.position;
            Vector3 targetPos = waypoint.worldPosition;
            
            // Calculate direction to target
            Vector2 direction = new Vector2(targetPos.x - playerPos.x, targetPos.y - playerPos.y);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            // Adjust for player rotation
            angle -= playerRotation;
            
            // Normalize angle to 0-360
            while (angle < 0) angle += 360;
            while (angle >= 360) angle -= 360;
            
            // Calculate position on compass bar
            float compassPosition = (angle / 360f) * compassWidth;
            
            // Handle wrapping (show icons that wrap around the edges)
            CreateCompassIcon(compassPosition, waypoint);
            
            // Create wrapped icons if necessary
            if (compassPosition < 60)
            {
                CreateCompassIcon(compassPosition + compassWidth, waypoint);
            }
            else if (compassPosition > compassWidth - 60)
            {
                CreateCompassIcon(compassPosition - compassWidth, waypoint);
            }
        }

        private void CreateCompassIcon(float xPosition, CompassWaypoint waypoint)
        {
            if (compassIconPrefab == null || compassIconsParent == null) return;

            // Check if position is within visible area
            if (xPosition < -60 || xPosition > compassWidth + 60) return;

            GameObject iconObj = Instantiate(compassIconPrefab, compassIconsParent);
            CompassIcon icon = new CompassIcon
            {
                gameObject = iconObj,
                waypoint = waypoint
            };

            // Position the icon
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            if (iconRect != null)
            {
                iconRect.anchoredPosition = new Vector2(xPosition, 0);
            }

            // Set icon appearance
            Image iconImage = iconObj.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.color = waypoint.color;
            }

            // Set text if available
            Text iconText = iconObj.GetComponentInChildren<Text>();
            if (iconText != null)
            {
                iconText.text = waypoint.label;
                iconText.color = waypoint.color;
                
                // Calculate distance
                float distance = Vector3.Distance(player.position, waypoint.worldPosition);
                iconText.text += $"\n{distance:F0}m";
            }

            compassIcons.Add(icon);
        }

        private void ClearCompassIcons()
        {
            foreach (var icon in compassIcons)
            {
                if (icon.gameObject != null)
                {
                    Destroy(icon.gameObject);
                }
            }
            compassIcons.Clear();
        }

        private void CreateCardinalDirections()
        {
            // Add permanent waypoints for cardinal directions
            Vector3 playerPos = player != null ? player.position : Vector3.zero;
            float cardinalDistance = 1000f; // Far enough to always be visible
            
            AddPermanentWaypoint(playerPos + Vector3.right * cardinalDistance, "E", Color.white, CompassIconType.Cardinal);
            AddPermanentWaypoint(playerPos + Vector3.up * cardinalDistance, "N", Color.white, CompassIconType.Cardinal);
            AddPermanentWaypoint(playerPos + Vector3.left * cardinalDistance, "W", Color.white, CompassIconType.Cardinal);
            AddPermanentWaypoint(playerPos + Vector3.down * cardinalDistance, "S", Color.white, CompassIconType.Cardinal);
        }

        private void UpdateDirectionText()
        {
            if (directionText == null || player == null) return;

            float rotation = GetPlayerRotation();
            string direction = GetCardinalDirection(rotation);
            directionText.text = $"{direction} ({rotation:F0}°)";
        }

        private string GetCardinalDirection(float angle)
        {
            // Normalize angle
            while (angle < 0) angle += 360;
            while (angle >= 360) angle -= 360;

            if (angle >= 337.5f || angle < 22.5f) return "N";
            if (angle >= 22.5f && angle < 67.5f) return "NE";
            if (angle >= 67.5f && angle < 112.5f) return "E";
            if (angle >= 112.5f && angle < 157.5f) return "SE";
            if (angle >= 157.5f && angle < 202.5f) return "S";
            if (angle >= 202.5f && angle < 247.5f) return "SW";
            if (angle >= 247.5f && angle < 292.5f) return "W";
            if (angle >= 292.5f && angle < 337.5f) return "NW";
            
            return "N";
        }

        #endregion

        #region Waypoint Management

        public void AddWaypoint(Vector3 worldPosition, string label, Color color)
        {
            AddPermanentWaypoint(worldPosition, label, color, CompassIconType.Waypoint);
        }

        private void AddPermanentWaypoint(Vector3 worldPosition, string label, Color color, CompassIconType type)
        {
            CompassWaypoint waypoint = new CompassWaypoint
            {
                worldPosition = worldPosition,
                label = label,
                color = color,
                type = type,
                isDynamic = false
            };
            waypoints.Add(waypoint);
        }

        private void AddDynamicWaypoint(Vector3 worldPosition, string label, Color color, CompassIconType type)
        {
            CompassWaypoint waypoint = new CompassWaypoint
            {
                worldPosition = worldPosition,
                label = label,
                color = color,
                type = type,
                isDynamic = true
            };
            waypoints.Add(waypoint);
        }

        public void RemoveWaypoint(Vector3 worldPosition, float tolerance = 1f)
        {
            waypoints.RemoveAll(w => Vector3.Distance(w.worldPosition, worldPosition) <= tolerance);
        }

        public void ClearAllWaypoints()
        {
            waypoints.Clear();
            if (showCardinalDirections)
            {
                CreateCardinalDirections();
            }
        }

        public void ClearDynamicWaypoints()
        {
            waypoints.RemoveAll(w => w.isDynamic);
        }

        #endregion

        #region Public Methods

        public void SetPlayer(Transform newPlayer)
        {
            player = newPlayer;
        }

        public void SetMaxDistance(float distance)
        {
            maxCompassDistance = Mathf.Max(10f, distance);
        }

        public void ToggleStructures()
        {
            showStructures = !showStructures;
        }

        public void ToggleEnemies()
        {
            showEnemies = !showEnemies;
        }

        public void ToggleItems()
        {
            showItems = !showItems;
        }

        public void ToggleCardinalDirections()
        {
            showCardinalDirections = !showCardinalDirections;
            if (showCardinalDirections)
            {
                CreateCardinalDirections();
            }
            else
            {
                waypoints.RemoveAll(w => w.type == CompassIconType.Cardinal);
            }
        }

        #endregion
    }

    #region Data Structures

    [System.Serializable]
    public class CompassWaypoint
    {
        public Vector3 worldPosition;
        public string label;
        public Color color;
        public CompassIconType type;
        public bool isDynamic;
    }

    [System.Serializable]
    public class CompassIcon
    {
        public GameObject gameObject;
        public CompassWaypoint waypoint;
    }

    public enum CompassIconType
    {
        Structure,
        Enemy,
        Item,
        Waypoint,
        Cardinal
    }

    #endregion
}