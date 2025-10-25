using UnityEngine;
using RougeLite.Enemies;

namespace RougeLite.Combat
{
    public class FireProjectile : Projectile
    {
        [Header("Fire Properties")]
        [SerializeField] private float burnDuration = 3f;
        [SerializeField] private float burnDamagePerSecond = 5f;

        protected override void HandleHit(GameObject target)
        {
            base.HandleHit(target);

            // Apply burn effect if target has a health component
            var enemyHealth = target.GetComponent<SlimeHealth>();
            if (enemyHealth != null)
            {
                Debug.Log($"Applied burn effect to {target.name} for {burnDuration}s dealing {burnDamagePerSecond} dps");
                // Note: Burn effect implementation would require a status effect system
            }
        }
    }
}
