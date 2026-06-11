using System.Collections;
using UnityEngine;

public class TribalMageAttack : TribalAttackBase
{
    [SerializeField] private GameObject energyBallPrefab;
    [SerializeField] private Transform castPoint;

    [SerializeField] private int projectileCount = 7;
    [SerializeField] private float projectileSpeed = 4f;
    [SerializeField] private float delayBetweenShots = 0.15f;

    [SerializeField] private Animator animator;
    [SerializeField] private float attackDuration = 0.55f;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public override IEnumerator AttackRoutine(Transform player)
    {
        animator.SetTrigger("Attack");

        yield return new WaitForSeconds(attackDuration);

        for (int i = 0; i < projectileCount; i++)
        {   if(i>=1)
            {
                Shoot(player);
                yield return new WaitForSeconds(delayBetweenShots);
            }
            else
            {
                Shoot(player);
            }
        }
    }

    public override void FinishAttackAnimation()
    {
        animator.SetTrigger("Finished");
    }

    private void Shoot(Transform player)
    {
        if (energyBallPrefab == null || castPoint == null || player == null) return;

        Vector2 direction = (player.position - castPoint.position).normalized;

        GameObject ball = Instantiate(
            energyBallPrefab,
            castPoint.position,
            Quaternion.identity
        );

        EnergyBall energyBall = ball.GetComponent<EnergyBall>();

        if (energyBall != null)
            energyBall.Launch(direction, projectileSpeed);
    }
}