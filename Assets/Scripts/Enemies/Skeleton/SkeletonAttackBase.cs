using System.Collections;
using UnityEngine;

public abstract class SkeletonAttackBase : MonoBehaviour
{
    [SerializeField] protected float attackCooldown = 5f;

    public float AttackCooldown => attackCooldown;

    public abstract IEnumerator AttackRoutine(Transform player);

    public virtual void FinishAttackAnimation() { }
}