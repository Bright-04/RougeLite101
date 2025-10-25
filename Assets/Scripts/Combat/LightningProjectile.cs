using UnityEngine;

namespace RougeLite.Combat
{
    public class LightningProjectile : Projectile
    {
        [Header("Lightning Properties")]
        [SerializeField] private float chainRange = 5f;
        [SerializeField] private int maxChainTargets = 3;

        protected override void HandleHit(GameObject target)
        {
            base.HandleHit(target);

            // Chain lightning to nearby enemies
            ChainToNearbyTargets(target.transform.position);
        }

        private void ChainToNearbyTargets(Vector3 hitPosition)
        {
            Collider2D[] nearbyTargets = Physics2D.OverlapCircleAll(hitPosition, chainRange);
            int chainedTargets = 0;

            foreach (var collider in nearbyTargets)
            {
                if (chainedTargets >= maxChainTargets) break;

                var enemyHealth = collider.GetComponent<SlimeHealth>();
                if (enemyHealth != null && collider.gameObject != transform.gameObject)
                {
                    // Deal reduced damage to chained targets
                    float chainDamage = damage * 0.5f;
                    enemyHealth.TakeDamage(Mathf.RoundToInt(chainDamage));
                    Debug.Log($"Lightning chained to {collider.name} for {chainDamage} damage");
                    chainedTargets++;
                }
            }
        }
    }
}

