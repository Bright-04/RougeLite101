using UnityEditor;
using UnityEngine;

public static class ApplyFatalDamageValidator {
    [MenuItem("Tools/Run Fatal Damage Validator")]
    public static void Execute() {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) {
            var stats = player.GetComponent<PlayerStats>();
            if (stats != null) {
                stats.TakeDamage(9999f);
                Debug.Log("ApplyFatalDamageValidator: Took fatal damage.");
                return;
            }
        }
        Debug.LogError("ApplyFatalDamageValidator: PlayerStats not found.");
    }
}
