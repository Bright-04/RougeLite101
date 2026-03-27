using UnityEngine;

namespace RougeLite.System
{
    /// <summary>
    /// Manages the adaptive difficulty of the game.
    /// </summary>
    public class DifficultyManager : MonoBehaviour
    {
        #region Singleton
        public static DifficultyManager Instance { get; private set; }

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

        // Trackers
        private int currentRoomIndex = 0;
        public float currentDifficultyMultiplier { get; private set; } = 1.0f;
        private const float MAX_DIFFICULTY = 3.0f;
        private const float MIN_DIFFICULTY = 0.5f;

        private float roomStartTime;
        private int roomDamageTaken;
        private int swingsTotal;
        private int swingsHit;

        public void StartRoomTracking(int index)
        {
            currentRoomIndex = index;
            roomStartTime = Time.time;
            roomDamageTaken = 0;
            swingsTotal = 0;
            swingsHit = 0;
            Debug.Log($"[Adaptive AI] Tracking started for Room {currentRoomIndex}. Current Difficulty: {currentDifficultyMultiplier:F2}x");
        }

        public void RecordDamageTaken(int damageAmount)
        {
            roomDamageTaken += damageAmount;
        }

        public void RecordSwing()
        {
            swingsTotal++;
        }

        public void RecordHit()
        {
            swingsHit++;
        }

        public void EvaluateRoomClear()
        {
            float clearTime = Time.time - roomStartTime;
            float hitAccuracy = swingsTotal > 0 ? (float)swingsHit / swingsTotal : 1.0f;

            Debug.Log($"[Adaptive AI] Room Cleared! Time: {clearTime:F1}s | Damage Taken: {roomDamageTaken} | Accuracy: {hitAccuracy * 100:F1}%");

            float performanceDelta = 0.05f; // Implicit small scale-up over time

            // Highly Skilled (Fast, Accurate, Flawless)
            if (roomDamageTaken == 0 && clearTime < 20f && hitAccuracy > 0.6f)
            {
                performanceDelta += 0.15f; 
                Debug.Log("[Adaptive AI] Flawless Clear. Ramping up difficulty!");
            }
            // Struggling (Took massive damage or excessively slow)
            else if (roomDamageTaken >= 20 || clearTime > 60f || hitAccuracy < 0.2f)
            {
                performanceDelta -= 0.2f; // Takes pity and drops difficulty
                Debug.Log("[Adaptive AI] Player Struggling. Dropping difficulty!");
            }
            else if (roomDamageTaken >= 10)
            {
                performanceDelta -= 0.05f; // Slight drop if decent hits taken
            }

            currentDifficultyMultiplier = Mathf.Clamp(currentDifficultyMultiplier + performanceDelta, MIN_DIFFICULTY, MAX_DIFFICULTY);
            Debug.Log($"[Adaptive AI] New Difficulty Multiplier: {currentDifficultyMultiplier:F2}x");
        }

        // --- Getters for Spawners & Enemies ---

        public float GetHealthMultiplier()
        {
            return currentDifficultyMultiplier; // 0.5x to 3.0x
        }

        public float GetDamageMultiplier()
        {
            // Damage scales slightly less aggressively so player isn't instantly one-shot
            return Mathf.Lerp(1.0f, currentDifficultyMultiplier, 0.7f); 
        }

        public int GetBonusSpawnCount()
        {
            // Hard cap +3 bonus enemies if multiplier is high
            int bonus = Mathf.FloorToInt((currentDifficultyMultiplier - 1.0f) * 2f);
            return Mathf.Clamp(bonus, 0, 3);
        }
    }
}
