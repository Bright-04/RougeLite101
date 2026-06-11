using System.Collections;
using UnityEngine;

public class TribalArcherAttack : TribalAttackBase
{
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float arrowSpeed = 6f;

    [SerializeField] private Animator animator;
    [SerializeField] private float attackDuration = 0.8f;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public override IEnumerator AttackRoutine(Transform player)
    {
        animator.SetTrigger("Attack");

        yield return new WaitForSeconds(attackDuration);

        Shoot(player);
    }

    public override void FinishAttackAnimation()
    {
        animator.SetTrigger("Finished");
    }

    private void Shoot(Transform player)
    {
        if (arrowPrefab == null || shootPoint == null || player == null) return;

        Vector2 direction = (player.position - shootPoint.position).normalized;

        GameObject arrow = Instantiate(
            arrowPrefab,
            shootPoint.position,
            Quaternion.identity
        );

        ArrowProjectile projectile = arrow.GetComponent<ArrowProjectile>();

        if (projectile != null)
            projectile.Launch(direction, arrowSpeed);
    }
}