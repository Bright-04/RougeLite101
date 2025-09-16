using UnityEngine;

namespace RougeLite.World
{
    /// <summary>
    /// Scriptable Object for storing biome configuration data
    /// Defines what objects spawn in different biome types
    /// </summary>
    [CreateAssetMenu(fileName = "New Biome", menuName = "RougeLite/World/Biome Data")]
    public class BiomeDataSO : ScriptableObject
    {
        [Header("Biome Info")]
        public string biomeName = "Default";
        public Color biomeColor = Color.white;
        [TextArea(2, 4)]
        public string description = "A basic biome";

        [Header("Spawn Rates")]
        [Range(0f, 1f)] public float enemySpawnRate = 0.3f;
        [Range(0f, 1f)] public float itemSpawnRate = 0.1f;
        [Range(0f, 1f)] public float decorationDensity = 0.2f;
        [Range(0f, 1f)] public float structureSpawnRate = 0.05f;

        [Header("Terrain")]
        public GameObject[] terrainPrefabs;

        [Header("Enemies")]
        public GameObject[] enemyPrefabs;

        [Header("Items")]
        public GameObject[] itemPrefabs;

        [Header("Structures")]
        public GameObject[] structurePrefabs;

        [Header("Decorations (Trees, Bushes, etc.)")]
        public GameObject[] decorationPrefabs;

        /// <summary>
        /// Convert this ScriptableObject to the BiomeData struct used by the generator
        /// </summary>
        public BiomeData ToBiomeData()
        {
            return new BiomeData
            {
                biomeName = this.biomeName,
                biomeColor = this.biomeColor,
                enemySpawnRate = this.enemySpawnRate,
                itemSpawnRate = this.itemSpawnRate,
                decorationDensity = this.decorationDensity,
                terrainPrefabs = this.terrainPrefabs,
                enemyPrefabs = this.enemyPrefabs,
                itemPrefabs = this.itemPrefabs,
                structurePrefabs = this.structurePrefabs,
                decorationPrefabs = this.decorationPrefabs
            };
        }
    }
}