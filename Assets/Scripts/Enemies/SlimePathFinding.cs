using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RougeLite.Misc;

namespace RougeLite.Enemies
{
public class SlimePathFinding : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;

    private Rigidbody2D rb;
    private Vector2 moveDir;
    private Knockback knockback;
    private Vector2 currentTargetPosition;
    private bool hasTarget = false;

    private void Awake()
    {
        knockback = GetComponent<Knockback>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (knockback.gettingKnockedBack || !hasTarget)
            return;

        Vector2 direction = (currentTargetPosition - rb.position).normalized;
        rb.MovePosition(rb.position + direction * (moveSpeed * Time.fixedDeltaTime));
    }

    public void MoveTo(Vector2 targetPosition)
    {
        currentTargetPosition = targetPosition;
        hasTarget = true;
    }

    public void StopMoving()
    {
        hasTarget = false;
    }
}
}
