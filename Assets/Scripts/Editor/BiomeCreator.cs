using UnityEngine;
using UnityEditor;

namespace RougeLite.World
{
    /// <summary>
    /// Helper script to create default biome configurations
    /// Run this in the Editor to set up basic biomes with your existing prefabs
    /// </summary>
    public static class BiomeCreator
    {
        [MenuItem("RougeLite/Create Default Biomes")]
        public static void CreateDefaultBiomes()
        {
            // Create Forest Biome
            BiomeDataSO forestBiome = ScriptableObject.CreateInstance<BiomeDataSO>();
            forestBiome.biomeName = "Forest";
            forestBiome.biomeColor = new Color(0.2f, 0.8f, 0.2f);
            forestBiome.description = "A lush forest with many trees and bushes";
            
            forestBiome.enemySpawnRate = 0.25f;
            forestBiome.itemSpawnRate = 0.1f;
            forestBiome.decorationDensity = 0.4f; // More decorations in forest
            forestBiome.structureSpawnRate = 0.05f;

            // Load prefabs (you'll need to assign these manually in inspector)
            forestBiome.enemyPrefabs = LoadPrefabs("Slime");
            forestBiome.decorationPrefabs = LoadPrefabs("Tree", "Bush_0");

            AssetDatabase.CreateAsset(forestBiome, "Assets/Data/Biomes/Forest_Biome.asset");

            // Create Plains Biome
            BiomeDataSO plainsBiome = ScriptableObject.CreateInstance<BiomeDataSO>();
            plainsBiome.biomeName = "Plains";
            plainsBiome.biomeColor = new Color(0.8f, 0.8f, 0.2f);
            plainsBiome.description = "Open plains with scattered bushes";
            
            plainsBiome.enemySpawnRate = 0.3f;
            plainsBiome.itemSpawnRate = 0.15f;
            plainsBiome.decorationDensity = 0.15f; // Less decorations in plains
            plainsBiome.structureSpawnRate = 0.05f;

            plainsBiome.enemyPrefabs = LoadPrefabs("Slime");
            plainsBiome.decorationPrefabs = LoadPrefabs("Bush_0");

            AssetDatabase.CreateAsset(plainsBiome, "Assets/Data/Biomes/Plains_Biome.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("✅ Created default biomes! Check Assets/Data/Biomes/");
            Debug.Log("📝 Remember to assign these biomes to your InfiniteWorldGenerator component!");
        }

        private static GameObject[] LoadPrefabs(params string[] prefabNames)
        {
            var prefabs = new System.Collections.Generic.List<GameObject>();
            
            foreach (string prefabName in prefabNames)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/{prefabName}.prefab");
                if (prefab != null)
                {
                    prefabs.Add(prefab);
                }
                else
                {
                    Debug.LogWarning($"Could not find prefab: {prefabName}");
                }
            }
            
            return prefabs.ToArray();
        }
    }
}