using System.Collections;
using UnityEngine;

public class Knockback : MonoBehaviour
{
    public bool gettingKnockedBack { get; private set; }
    [SerializeField] private float knockBackTime = .2f;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void GetKnockedBack(Transform damageSource, float knockBacThrust)
    {
        gettingKnockedBack = true;
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // Smooth chuyển động, giảm lỗi xuyên thấu
            
            // Giảm bớt lực hất cực đoan gây nổ Physics
            Vector2 difference = (transform.position - damageSource.position).normalized * knockBacThrust * rb.mass;
            rb.AddForce(difference, ForceMode2D.Impulse);
            
            // KHOÁ TỐC ĐỘ: Ngăn quái bay quá nhanh trong 1 frame gây thủng map
            if (rb.linearVelocity.magnitude > 20f)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * 20f;
            }
            
            Debug.Log($"Knockback applied! Force: {difference}, Velocity: {rb.linearVelocity.magnitude}");
        }
        StartCoroutine(KnockRoutine());
    }

    private IEnumerator KnockRoutine()
    {
        yield return new WaitForSeconds(knockBackTime);
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        }
        gettingKnockedBack = false;
    }
}
