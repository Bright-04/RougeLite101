using UnityEngine;

public class SkeletonAimArm : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float angleOffset = 0f;
    [SerializeField] private SkeletonAI skeletonAI;

    private void Awake()
    {
        if (skeletonAI == null)
            skeletonAI = GetComponentInParent<SkeletonAI>();
    }

    private void Start()
    {
        if (target == null && PlayerMovement.Instance != null)
            target = PlayerMovement.Instance.transform;
    }

    private void Update()
    {
        if (target == null) return;
        if (skeletonAI != null && !skeletonAI.IsAttacking) return;

        Vector2 direction = target.position - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0f, 0f, angle + angleOffset);
    }
}