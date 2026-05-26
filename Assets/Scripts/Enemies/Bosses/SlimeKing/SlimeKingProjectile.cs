using UnityEngine;

public class SlimeKingProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 6f;
    [SerializeField] private float lifetime = 6f;

    private bool launched;
    private Vector2 direction;

    private void Awake()
    {
        // Failsafe: projectile auto-destroys after `lifetime` seconds
        Destroy(gameObject, lifetime);
    }

    /// <summary>
    /// Reset state before reusing / launching.
    /// </summary>
    public void Prepare()
    {
        launched = false;
        direction = Vector2.zero;
    }

    /// <summary>
    /// Set direction and start moving toward target.
    /// </summary>
    public void LaunchTowards(Vector3 targetPos)
    {
        direction = ((Vector2)targetPos - (Vector2)transform.position).normalized;
        launched = true;
    }

    private void Update()
    {
        if (!launched) return;

        transform.position += (Vector3)direction * (speed * Time.deltaTime);
    }
}
