using UnityEngine;

namespace RougeLite.Player
{
    /// <summary>
    /// Tracks the player's performance to be used by the DifficultyManager.
    /// </summary>
    public class PlayerPerformanceTracker : MonoBehaviour
    {
        #region Singleton
        public static PlayerPerformanceTracker Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        #endregion

        // Performance Metrics
        public int EnemiesKilled { get; private set; }
        public int DamageTaken { get; private set; }
        public int RoomsCleared { get; private set; }
        public float TimeInGame { get; private set; }

        private void Update()
        {
            TimeInGame += Time.deltaTime;
        }

        public void AddEnemyKilled() => EnemiesKilled++;
        public void AddDamageTaken(int amount) => DamageTaken += amount;
        public void AddRoomCleared() => RoomsCleared++;

        public void Reset()
        {
            EnemiesKilled = 0;
            DamageTaken = 0;
            RoomsCleared = 0;
            TimeInGame = 0;
        }
    }
}
