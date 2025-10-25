using UnityEngine;

namespace RougeLite.Combat
{
    public class IceProjectile : Projectile
    {
        [Header("Ice Properties")]
        [SerializeField] private float slowDuration = 2f;
        [SerializeField] private float slowAmount = 0.5f;

        protected override void HandleHit(GameObject target)
        {
            base.HandleHit(target);

            // Apply slow effect if target has movement
            var pathfinding = target.GetComponent<SlimePathFinding>();
            if (pathfinding != null)
            {
                Debug.Log($"Applied slow effect to {target.name} for {slowDuration}s reducing speed by {slowAmount * 100}%");
                // Note: Slow effect implementation would require modifying movement components
            }
        }
    }
}

