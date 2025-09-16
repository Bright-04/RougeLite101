using UnityEngine;
using UnityEditor;
using RougeLite.Player;
using RougeLite.World;

namespace RougeLite.Editor
{
    /// <summary>
    /// Editor window to help test infinite world generation
    /// Provides quick teleportation and testing tools
    /// </summary>
    public class InfiniteWorldTester : EditorWindow
    {
        private SimplePlayerMovement player;
        private InfiniteWorldGenerator worldGenerator;
        private Vector2Int targetChunk = Vector2Int.zero;
        private Vector3 targetPosition = Vector3.zero;

        [MenuItem("RougeLite/Infinite World Tester")]
        public static void ShowWindow()
        {
            GetWindow<InfiniteWorldTester>("World Tester");
        }

        private void OnGUI()
        {
            GUILayout.Label("🗺️ Infinite World Generation Tester", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Find components if not already found
            if (player == null)
                player = FindFirstObjectByType<SimplePlayerMovement>();
            if (worldGenerator == null)
                worldGenerator = FindFirstObjectByType<InfiniteWorldGenerator>();

            // Component status
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Component Status:", EditorStyles.boldLabel);
            
            if (player != null)
                EditorGUILayout.HelpBox("✅ Player Movement Found", MessageType.Info);
            else
                EditorGUILayout.HelpBox("❌ No SimplePlayerMovement found. Add to your player GameObject.", MessageType.Warning);
                
            if (worldGenerator != null)
                EditorGUILayout.HelpBox("✅ World Generator Found", MessageType.Info);
            else
                EditorGUILayout.HelpBox("❌ No InfiniteWorldGenerator found. Add to a GameObject in scene.", MessageType.Warning);
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Quick teleport to chunks
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("🚀 Quick Chunk Teleport:", EditorStyles.boldLabel);
            
            targetChunk = EditorGUILayout.Vector2IntField("Target Chunk", targetChunk);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Teleport to Chunk") && player != null && Application.isPlaying)
            {
                player.TeleportToChunk(targetChunk);
            }
            
            if (GUILayout.Button("Random Far Chunk") && player != null && Application.isPlaying)
            {
                Vector2Int randomChunk = new Vector2Int(
                    Random.Range(-20, 21),
                    Random.Range(-20, 21)
                );
                targetChunk = randomChunk;
                player.TeleportToChunk(randomChunk);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Quick teleport to positions
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("📍 Quick Position Teleport:", EditorStyles.boldLabel);
            
            targetPosition = EditorGUILayout.Vector3Field("Target Position", targetPosition);
            
            if (GUILayout.Button("Teleport to Position") && player != null && Application.isPlaying)
            {
                player.TeleportTo(targetPosition);
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Preset locations
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("🎯 Preset Test Locations:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Origin (0,0)") && player != null && Application.isPlaying)
            {
                player.TeleportTo(Vector3.zero);
            }
            
            if (GUILayout.Button("Far East (500,0)") && player != null && Application.isPlaying)
            {
                player.TeleportTo(new Vector3(500, 0, 0));
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Far North (0,500)") && player != null && Application.isPlaying)
            {
                player.TeleportTo(new Vector3(0, 500, 0));
            }
            
            if (GUILayout.Button("Far Corner (1000,1000)") && player != null && Application.isPlaying)
            {
                player.TeleportTo(new Vector3(1000, 1000, 0));
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Current status
            if (Application.isPlaying && player != null)
            {
                EditorGUILayout.BeginVertical("box");
                GUILayout.Label("📊 Current Status:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Player Position", player.transform.position.ToString("F1"));
                
                Vector2Int currentChunk = GetCurrentChunk(player.transform.position);
                EditorGUILayout.LabelField("Current Chunk", $"({currentChunk.x}, {currentChunk.y})");
                EditorGUILayout.LabelField("Current Speed", player.GetCurrentSpeed().ToString("F1"));
                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(10);

            // Instructions
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("📝 Testing Instructions:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. Enter Play Mode\n" +
                "2. Use WASD or arrows to move\n" +
                "3. Hold Shift for fast movement\n" +
                "4. Use teleport buttons to test far areas\n" +
                "5. Watch Scene view to see chunk generation\n" +
                "6. Check console for chunk generation logs",
                MessageType.Info);
            EditorGUILayout.EndVertical();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("⚠️ Enter Play Mode to use teleport functions", MessageType.Warning);
            }
        }

        private Vector2Int GetCurrentChunk(Vector3 position)
        {
            int chunkSize = 50; // Should match your InfiniteWorldGenerator
            int chunkX = Mathf.FloorToInt(position.x / chunkSize);
            int chunkY = Mathf.FloorToInt(position.y / chunkSize);
            return new Vector2Int(chunkX, chunkY);
        }

        private void OnInspectorUpdate()
        {
            // Repaint the window regularly when playing
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
    }
}