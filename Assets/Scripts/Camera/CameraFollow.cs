using UnityEngine;

public class CameraFollowStrict : MonoBehaviour
{
    [Header("Target")]
    public Transform player;               // Assign in Inspector (or enable auto-find)
    public bool autoFindPlayerByTag = true;
    public string playerTag = "Player";

    [Header("Follow")]
    public Vector2 offset = Vector2.zero;  // e.g. (0, 0)
    [Range(0.01f, 1f)]
    public float smoothTime = 0.2f;

    private bool hasSnapped = false;
    private Vector3 velocity = Vector3.zero;

    void Awake()
    {
        if (player == null && autoFindPlayerByTag)
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go) player = go.transform;
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Desired camera position (preserve current Z)
        Vector3 target = new Vector3(player.position.x + offset.x,
                                     player.position.y + offset.y,
                                     transform.position.z);

        if (!hasSnapped)
        {
            // 1) Snap EXACTLY to the player on the very first frame
            transform.position = target;
            hasSnapped = true;
            return; // don't smooth on this frame
        }

        // 2) Smooth follow from now on
        transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, smoothTime);
    }
}
