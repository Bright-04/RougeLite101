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

        // TODO: Add player performance tracking variables
        // TODO: Add difficulty scaling logic
        // TODO: Add methods to be called by other systems
    }
}
