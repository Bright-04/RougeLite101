using UnityEngine;

/// <summary>
/// Add this to any GameObject in your scene to enable simple fade transitions.
/// No UI setup required - it creates everything automatically!
/// </summary>
public class SimpleFadeSetup : MonoBehaviour
{
    [Tooltip("Keep this enabled to automatically create the fade system on start")]
    [SerializeField] private bool autoSetup = true;

    [Header("Optional: Customize Timings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float holdDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    private void Start()
    {
        if (autoSetup)
        {
            SetupSimpleFade();
        }
    }

    [ContextMenu("Setup Simple Fade Transition")]
    public void SetupSimpleFade()
    {
        // Check if already exists
        if (SimpleFadeTransition.Instance != null)
        {
            Debug.Log("SimpleFadeTransition already exists in scene!");
            return;
        }

        // Create the fade system
        GameObject fadeObj = new GameObject("SimpleFadeTransition");
        var fade = fadeObj.AddComponent<SimpleFadeTransition>();
        
        // Use reflection to set the durations
        var type = typeof(SimpleFadeTransition);
        var fadeInField = type.GetField("fadeInDuration", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var holdField = type.GetField("holdDuration", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var fadeOutField = type.GetField("fadeOutDuration", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (fadeInField != null) fadeInField.SetValue(fade, fadeInDuration);
        if (holdField != null) holdField.SetValue(fade, holdDuration);
        if (fadeOutField != null) fadeOutField.SetValue(fade, fadeOutDuration);

        Debug.Log("<color=green>SimpleFadeTransition created! Fade transitions are now active.</color>");
    }
}
