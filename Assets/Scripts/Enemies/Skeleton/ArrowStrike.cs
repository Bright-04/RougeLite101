using UnityEngine;

public class ArrowStrike : MonoBehaviour
{
    [SerializeField] private float damage = 10f;

    private bool hasDamaged;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasDamaged)
            return;

        PlayerStats player = other.GetComponent<PlayerStats>();

        if (player != null)
        {
            player.TakeDamage(damage);
            hasDamaged = true;
            
        }


    }

    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}