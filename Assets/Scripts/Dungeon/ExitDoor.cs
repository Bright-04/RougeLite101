using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ExitDoor : MonoBehaviour
{
    private Collider2D _col;
    private bool _consumed;
    private DungeonManager _mgr;

    void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;
        _col.enabled = true;
    }

    public void Init(DungeonManager mgr)
    {
        _mgr = mgr;
        _consumed = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_consumed) return;
        if (!other.CompareTag("Player")) return;
        if (RunResultController.Instance != null && RunResultController.Instance.IsRunFinished) return;

        _consumed = true;

        Debug.Log("Player entered exit door.");

        if (_mgr != null)
        {
            _mgr.LoadNextFloor();
        }
        else
        {
            Debug.LogWarning("ExitDoor: DungeonManager reference is missing.");
        }
    }
}
