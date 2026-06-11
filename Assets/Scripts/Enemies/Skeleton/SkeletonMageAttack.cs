using System.Collections;
using UnityEngine;

public class SkeletonMageAttack : SkeletonAttackBase
{
    [SerializeField] private GameObject energyBallPrefab;
    [SerializeField] private Transform castPoint;

    [SerializeField] private int projectileCount = 6;
    [SerializeField] private float projectileSpeed = 4f;

    [SerializeField] private Animator animator;
    [SerializeField] private float attackDuration = 1.15f;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public override IEnumerator AttackRoutine(Transform player)
    {
        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.SetTrigger("Attack");
        }

        yield return new WaitForSeconds(attackDuration);

        ShootCircle();
    }

    public override void FinishAttackAnimation()
    {
        if (animator != null)
            animator.SetTrigger("Finished");
    }

    private void ShootCircle()
    {
        if (energyBallPrefab == null || castPoint == null)
            return;

        float angleStep = 360f / projectileCount;

        for (int i = 0; i < projectileCount; i++)
        {
            float angle = angleStep * i;
            float rad = angle * Mathf.Deg2Rad;

            Vector2 direction = new Vector2(
                Mathf.Cos(rad),
                Mathf.Sin(rad)
            );

            GameObject projectile = Instantiate(
                energyBallPrefab,
                castPoint.position,
                Quaternion.identity
            );

            EnergyBall energyBall = projectile.GetComponent<EnergyBall>();

            if (energyBall != null)
            {
                energyBall.Launch(direction, projectileSpeed);
            }
        }
    }
}