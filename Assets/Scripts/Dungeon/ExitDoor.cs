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
            if (RunResultRules.ShouldCompleteRunFromExitPortal(_mgr.currentFloor, _mgr.maxFloor))
            {
                RunResultController runResultController = RunResultController.Instance != null
                    ? RunResultController.Instance
                    : FindAnyObjectByType<RunResultController>(FindObjectsInactive.Include);

                if (runResultController == null)
                {
                    Debug.LogError("ExitDoor: RunResultController reference is missing for final portal completion.", this);
                    _consumed = false;
                    return;
                }

                if (!runResultController.TryCompleteRunFromExitPortal())
                {
                    Debug.LogError("ExitDoor: Final portal could not complete the run result handoff.", this);
                    _consumed = false;
                }

                return;
            }

            _mgr.LoadNextFloor();
        }
        else
        {
            Debug.LogWarning("ExitDoor: DungeonManager reference is missing.");
            _consumed = false;
        }
    }
}
