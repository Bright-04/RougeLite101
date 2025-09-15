using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using RougeLite.Exploration;

namespace RougeLite.UI
{
    /// <summary>
    /// Comprehensive UI for infinite exploration features
    /// Combines minimap, compass, exploration stats, and discovery log
    /// </summary>
    public class ExplorationUI : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject explorationPanel;
        [SerializeField] private GameObject minimapPanel;
        [SerializeField] private GameObject compassPanel;
        [SerializeField] private GameObject statsPanel;
        [SerializeField] private GameObject discoveryLogPanel;

        [Header("Minimap Components")]
        [SerializeField] private MinimapController minimap;
        [SerializeField] private Button minimapToggle;
        [SerializeField] private Slider zoomSlider;
        [SerializeField] private Button centerOnPlayerButton;

        [Header("Compass Components")]
        [SerializeField] private CompassController compass;
        [SerializeField] private Button compassToggle;
        [SerializeField] private Slider compassRangeSlider;
        [SerializeField] private Toggle showStructuresToggle;
        [SerializeField] private Toggle showItemsToggle;

        [Header("Stats Components")]
        [SerializeField] private Text totalDiscoveriesText;
        [SerializeField] private Text structuresText;
        [SerializeField] private Text enemyGroupsText;
        [SerializeField] private Text itemCachesText;
        [SerializeField] private Text biomesText;
        [SerializeField] private Text distanceTraveledText;
        [SerializeField] private Text chunksExploredText;
        [SerializeField] private Text explorationTimeText;

        [Header("Discovery Log Components")]
        [SerializeField] private ScrollRect discoveryScrollRect;
        [SerializeField] private Transform discoveryListParent;
        [SerializeField] private GameObject discoveryItemPrefab;
        [SerializeField] private Button clearLogButton;
        [SerializeField] private Dropdown discoveryFilterDropdown;

        [Header("Navigation")]
        [SerializeField] private Button waypointButton;
        [SerializeField] private InputField waypointNameInput;
        [SerializeField] private Button removeWaypointButton;

        [Header("References")]
        [SerializeField] private ExplorationTracker explorationTracker;
        [SerializeField] private Transform player;

        // State
        private bool minimapVisible = true;
        private bool compassVisible = true;
        private bool statsVisible = false;
        private bool discoveryLogVisible = false;
        private DiscoveryType? currentDiscoveryFilter = null;

        #region Unity Lifecycle

        private void Start()
        {
            InitializeUI();
            SetupEventListeners();
            
            // Find references if not assigned
            if (explorationTracker == null)
                explorationTracker = FindFirstObjectByType<ExplorationTracker>();
            if (minimap == null)
                minimap = FindFirstObjectByType<MinimapController>();
            if (compass == null)
                compass = FindFirstObjectByType<CompassController>();

            // Subscribe to exploration events
            if (explorationTracker != null)
            {
                explorationTracker.OnNewDiscovery += OnNewDiscovery;
                explorationTracker.OnStatsUpdated += OnStatsUpdated;
            }
        }

        private void Update()
        {
            HandleInput();
            UpdateDynamicElements();
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (explorationTracker != null)
            {
                explorationTracker.OnNewDiscovery -= OnNewDiscovery;
                explorationTracker.OnStatsUpdated -= OnStatsUpdated;
            }
        }

        #endregion

        #region Initialization

        private void InitializeUI()
        {
            // Set initial panel visibility
            SetPanelVisibility(minimapPanel, minimapVisible);
            SetPanelVisibility(compassPanel, compassVisible);
            SetPanelVisibility(statsPanel, statsVisible);
            SetPanelVisibility(discoveryLogPanel, discoveryLogVisible);

            // Initialize sliders
            if (zoomSlider != null && minimap != null)
            {
                zoomSlider.minValue = 0.5f;
                zoomSlider.maxValue = 3f;
                zoomSlider.value = 1f;
            }

            if (compassRangeSlider != null && compass != null)
            {
                compassRangeSlider.minValue = 50f;
                compassRangeSlider.maxValue = 500f;
                compassRangeSlider.value = 200f;
            }

            // Initialize discovery filter dropdown
            InitializeDiscoveryFilter();
        }

        private void SetupEventListeners()
        {
            // Minimap controls
            if (minimapToggle != null)
                minimapToggle.onClick.AddListener(ToggleMinimap);
            if (zoomSlider != null)
                zoomSlider.onValueChanged.AddListener(OnZoomChanged);
            if (centerOnPlayerButton != null)
                centerOnPlayerButton.onClick.AddListener(CenterOnPlayer);

            // Compass controls
            if (compassToggle != null)
                compassToggle.onClick.AddListener(ToggleCompass);
            if (compassRangeSlider != null)
                compassRangeSlider.onValueChanged.AddListener(OnCompassRangeChanged);
            if (showStructuresToggle != null)
                showStructuresToggle.onValueChanged.AddListener(OnShowStructuresChanged);
            if (showItemsToggle != null)
                showItemsToggle.onValueChanged.AddListener(OnShowItemsChanged);

            // Discovery log controls
            if (clearLogButton != null)
                clearLogButton.onClick.AddListener(ClearDiscoveryLog);
            if (discoveryFilterDropdown != null)
                discoveryFilterDropdown.onValueChanged.AddListener(OnDiscoveryFilterChanged);

            // Navigation controls
            if (waypointButton != null)
                waypointButton.onClick.AddListener(AddWaypoint);
            if (removeWaypointButton != null)
                removeWaypointButton.onClick.AddListener(RemoveNearestWaypoint);
        }

        private void InitializeDiscoveryFilter()
        {
            if (discoveryFilterDropdown == null) return;

            discoveryFilterDropdown.options.Clear();
            discoveryFilterDropdown.options.Add(new Dropdown.OptionData("All Discoveries"));
            discoveryFilterDropdown.options.Add(new Dropdown.OptionData("Structures"));
            discoveryFilterDropdown.options.Add(new Dropdown.OptionData("Enemy Groups"));
            discoveryFilterDropdown.options.Add(new Dropdown.OptionData("Item Caches"));
            discoveryFilterDropdown.options.Add(new Dropdown.OptionData("Biomes"));
            discoveryFilterDropdown.value = 0;
        }

        #endregion

        #region Input Handling

        private void HandleInput()
        {
            // Toggle exploration UI with M key
            if (Input.GetKeyDown(KeyCode.M))
            {
                ToggleExplorationPanel();
            }

            // Toggle minimap with N key
            if (Input.GetKeyDown(KeyCode.N))
            {
                ToggleMinimap();
            }

            // Quick waypoint with B key
            if (Input.GetKeyDown(KeyCode.B))
            {
                QuickAddWaypoint();
            }

            // Toggle stats with Tab key
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleStats();
            }
        }

        #endregion

        #region UI Panel Management

        public void ToggleExplorationPanel()
        {
            bool newVisibility = !explorationPanel.activeSelf;
            SetPanelVisibility(explorationPanel, newVisibility);
        }

        public void ToggleMinimap()
        {
            minimapVisible = !minimapVisible;
            SetPanelVisibility(minimapPanel, minimapVisible);
            
            if (minimap != null)
                minimap.enabled = minimapVisible;
        }

        public void ToggleCompass()
        {
            compassVisible = !compassVisible;
            SetPanelVisibility(compassPanel, compassVisible);
            
            if (compass != null)
                compass.enabled = compassVisible;
        }

        public void ToggleStats()
        {
            statsVisible = !statsVisible;
            SetPanelVisibility(statsPanel, statsVisible);
        }

        public void ToggleDiscoveryLog()
        {
            discoveryLogVisible = !discoveryLogVisible;
            SetPanelVisibility(discoveryLogPanel, discoveryLogVisible);
            
            if (discoveryLogVisible)
            {
                RefreshDiscoveryLog();
            }
        }

        private void SetPanelVisibility(GameObject panel, bool visible)
        {
            if (panel != null)
                panel.SetActive(visible);
        }

        #endregion

        #region Minimap Controls

        private void OnZoomChanged(float zoom)
        {
            if (minimap != null)
                minimap.SetZoom(zoom);
        }

        private void CenterOnPlayer()
        {
            if (minimap != null)
                minimap.CenterOnPlayer();
        }

        #endregion

        #region Compass Controls

        private void OnCompassRangeChanged(float range)
        {
            if (compass != null)
                compass.SetMaxDistance(range);
        }

        private void OnShowStructuresChanged(bool show)
        {
            if (compass != null)
                compass.ToggleStructures();
        }

        private void OnShowItemsChanged(bool show)
        {
            if (compass != null)
                compass.ToggleItems();
        }

        #endregion

        #region Stats Display

        private void UpdateDynamicElements()
        {
            if (statsVisible && explorationTracker != null)
            {
                UpdateStatsDisplay(explorationTracker.GetCurrentStats());
            }
        }

        private void UpdateStatsDisplay(ExplorationStats stats)
        {
            if (totalDiscoveriesText != null)
                totalDiscoveriesText.text = $"Total Discoveries: {stats.totalDiscoveries}";
            
            if (structuresText != null)
                structuresText.text = $"Structures: {stats.structuresDiscovered}";
            
            if (enemyGroupsText != null)
                enemyGroupsText.text = $"Enemy Groups: {stats.enemyGroupsDiscovered}";
            
            if (itemCachesText != null)
                itemCachesText.text = $"Item Caches: {stats.itemCachesDiscovered}";
            
            if (biomesText != null)
                biomesText.text = $"Biomes Explored: {stats.biomesExplored}";
            
            if (distanceTraveledText != null)
                distanceTraveledText.text = $"Distance: {stats.totalDistanceTraveled:F1}m";
            
            if (chunksExploredText != null)
                chunksExploredText.text = $"Chunks: {stats.chunksExplored}";
            
            if (explorationTimeText != null)
            {
                int minutes = Mathf.FloorToInt(stats.explorationTime / 60);
                int seconds = Mathf.FloorToInt(stats.explorationTime % 60);
                explorationTimeText.text = $"Time: {minutes}:{seconds:D2}";
            }
        }

        #endregion

        #region Discovery Log

        private void RefreshDiscoveryLog()
        {
            if (explorationTracker == null || discoveryListParent == null) return;

            // Clear existing items
            foreach (Transform child in discoveryListParent)
            {
                Destroy(child.gameObject);
            }

            // Get filtered discoveries
            var discoveries = explorationTracker.GetDiscoveries(currentDiscoveryFilter);
            discoveries = discoveries.OrderByDescending(d => d.discoveryTime).ToList();

            // Create discovery items
            foreach (var discovery in discoveries)
            {
                CreateDiscoveryItem(discovery);
            }
        }

        private void CreateDiscoveryItem(Discovery discovery)
        {
            if (discoveryItemPrefab == null) return;

            GameObject item = Instantiate(discoveryItemPrefab, discoveryListParent);
            
            // Set discovery info
            Text nameText = item.transform.Find("NameText")?.GetComponent<Text>();
            if (nameText != null)
                nameText.text = discovery.name;

            Text typeText = item.transform.Find("TypeText")?.GetComponent<Text>();
            if (typeText != null)
                typeText.text = discovery.type.ToString();

            Text positionText = item.transform.Find("PositionText")?.GetComponent<Text>();
            if (positionText != null)
                positionText.text = $"({discovery.position.x:F0}, {discovery.position.y:F0})";

            // Add click listener to navigate to discovery
            Button itemButton = item.GetComponent<Button>();
            if (itemButton != null)
            {
                itemButton.onClick.AddListener(() => NavigateToDiscovery(discovery));
            }
        }

        private void OnDiscoveryFilterChanged(int filterIndex)
        {
            switch (filterIndex)
            {
                case 0: currentDiscoveryFilter = null; break;
                case 1: currentDiscoveryFilter = DiscoveryType.Structure; break;
                case 2: currentDiscoveryFilter = DiscoveryType.EnemyGroup; break;
                case 3: currentDiscoveryFilter = DiscoveryType.ItemCache; break;
                case 4: currentDiscoveryFilter = DiscoveryType.Biome; break;
            }

            if (discoveryLogVisible)
            {
                RefreshDiscoveryLog();
            }
        }

        private void ClearDiscoveryLog()
        {
            if (explorationTracker != null)
            {
                explorationTracker.ClearDiscoveries();
                RefreshDiscoveryLog();
            }
        }

        #endregion

        #region Navigation & Waypoints

        private void AddWaypoint()
        {
            if (player == null || compass == null) return;

            string waypointName = waypointNameInput != null && !string.IsNullOrEmpty(waypointNameInput.text) 
                ? waypointNameInput.text 
                : "Waypoint";

            compass.AddWaypoint(player.position, waypointName, Color.green);
            
            if (waypointNameInput != null)
                waypointNameInput.text = "";
        }

        private void QuickAddWaypoint()
        {
            if (player == null || compass == null) return;

            string waypointName = $"Waypoint {System.DateTime.Now:HH:mm}";
            compass.AddWaypoint(player.position, waypointName, Color.green);
        }

        private void RemoveNearestWaypoint()
        {
            if (player == null || compass == null) return;

            compass.RemoveWaypoint(player.position, 10f);
        }

        private void NavigateToDiscovery(Discovery discovery)
        {
            if (compass == null) return;

            // Add temporary waypoint for navigation
            compass.AddWaypoint(discovery.position, $"→ {discovery.name}", Color.yellow);
        }

        #endregion

        #region Event Handlers

        private void OnNewDiscovery(Discovery discovery)
        {
            // Show notification or update UI
            Debug.Log($"New discovery: {discovery.name}");
            
            // Refresh log if visible
            if (discoveryLogVisible)
            {
                RefreshDiscoveryLog();
            }
        }

        private void OnStatsUpdated(ExplorationStats stats)
        {
            // Stats are updated automatically in UpdateDynamicElements
        }

        #endregion

        #region Public Methods

        public void SetPlayer(Transform newPlayer)
        {
            player = newPlayer;
            
            if (minimap != null)
                minimap.SetPlayer(newPlayer);
            if (compass != null)
                compass.SetPlayer(newPlayer);
            if (explorationTracker != null)
                explorationTracker.SetPlayer(newPlayer);
        }

        public void ShowNotification(string message)
        {
            // Could implement a notification system here
            Debug.Log($"Exploration Notification: {message}");
        }

        #endregion
    }
}