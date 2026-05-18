using UnityEngine;

public class Arrow : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float lifetime = 5f;

    public void Initialize(float projectileSpeed, int baseDamage, float projectileRange)
    {
        speed = projectileSpeed;
        damage = baseDamage;

        if (speed > 0f && projectileRange > 0f)
        {
            lifetime = projectileRange / speed;
        }
    }

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
        // Bỏ qua nếu chạm vào Player, hoặc chạm vào mũi tên khác
        if (other.CompareTag("Player") || other.GetComponent<Arrow>() != null) return;
        
        // Bỏ qua các trigger vô hình (như ranh giới phòng, cảm biến...)
        if (other.isTrigger) return;

        // Trừ máu enemy
        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage((int)damage);
            Destroy(gameObject); // Chạm là huỷ mũi tên
        }
        else 
        {
            // Bất kể là tường Default, Environment hay Obstacle, miễn là vật thể vật lý TRÙNG KHỚP -> tự rụng.
            Destroy(gameObject);
        }
    }
}
