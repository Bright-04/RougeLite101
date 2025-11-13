using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ExitDoor : MonoBehaviour
{
    private Collider2D _col;
    private Animator _animator;
    private bool _locked = true;
    private bool _consumed;
    private DungeonManager _mgr;

    void Awake()
    {
        _col = GetComponent<Collider2D>();
        _animator = GetComponent<Animator>(); // Get animator if present
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
        
        // Play close animation if animator exists
        if (_animator) _animator.Play("close");
    }

    public void Unlock()
    {
        _locked = false;
        if (_col) _col.enabled = true;  // <- critical: ensure collider is ON
        
        // Play open animation if animator exists
        if (_animator) _animator.Play("open");
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
