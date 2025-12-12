using UnityEngine;

public class SlimeKingJumpShadow : MonoBehaviour
{
    private Transform target;   // player
    private bool isLocked = false;

    [SerializeField] private Vector3 offset = new Vector3(0f, -0.2f, 0f); // small offset under feet

    public void Init(Transform player)
    {
        target = player;
    }

    /// <summary>Stop following the player (e.g. right before boss lands).</summary>
    public void LockPosition()
    {
        isLocked = true;
    }

    private void Update()
    {
        if (isLocked || target == null) return;

        transform.position = target.position + offset;
    }
}
