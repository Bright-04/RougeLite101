using UnityEngine;

public class BnnyBigSlashProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifetime = 3f;

    private void Start()
    {
        // Auto destroy after some time so it doesn't live forever
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // Move straight DOWN in world space every frame
        transform.Translate(Vector3.down * speed * Time.deltaTime, Space.World);
    }
}
