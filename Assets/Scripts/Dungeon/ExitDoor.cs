using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ExitDoor : MonoBehaviour
{
    private Collider2D _col;
    private bool _locked = true;
    private bool _consumed;
    private DungeonManager _mgr;

    void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;
        _col.enabled = false;    // always start disabled; manager will Lock/Unlock
    }

    public void Init(DungeonManager mgr)
    {
        _mgr = mgr;
        _consumed = false;
        Lock();
    }

    public void Lock()
    {
        _locked = true;
        if (_col) _col.enabled = false;
    }

    public void Unlock()
    {
        _locked = false;
        if (_col) _col.enabled = true;  // <- critical: ensure collider is ON
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_locked || _consumed) return;
        if (!other.CompareTag("Player")) return;

        _consumed = true;
        _col.enabled = false; // hard debounce
        _mgr.TryLoadNextRoom();
    }
}
