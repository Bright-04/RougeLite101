using UnityEngine;
using System;

public class EnemyDeathNotifier : MonoBehaviour
{
    public event Action<EnemyDeathNotifier> Died;

    private bool _sent; // avoid double fire

    /// <summary>Call this exactly once when the enemy dies.</summary>
    public void NotifyDied()
    {
        if (_sent) return;
        _sent = true;
        Died?.Invoke(this);
    }

    // Safety fallback if something destroys the enemy without calling NotifyDied()
    private void OnDestroy()
    {
        if (_sent) return;
        _sent = true;
        Died?.Invoke(this);
    }
}
