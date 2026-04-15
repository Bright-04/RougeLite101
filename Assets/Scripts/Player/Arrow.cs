using UnityEngine;

public class Arrow : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float lifetime = 5f;

    private void Start()
    {
        // Tự động hủy sau x giây để dọn dẹp bộ nhớ
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // Bay thẳng theo hướng mũi tên đang trỏ tới (right vector)
        transform.Translate(Vector3.right * speed * Time.deltaTime, Space.Self);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Bỏ qua nếu chạm vào Player
        if (other.CompareTag("Player")) return;

        // Trừ máu enemy
        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage((int)damage);
            Destroy(gameObject); // Chạm là huỷ mũi tên
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Environment") ||
                 other.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            // Khi chạm tường thì huỷ
            Destroy(gameObject);
        }
    }
}
