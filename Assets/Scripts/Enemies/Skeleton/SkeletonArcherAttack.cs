using System.Collections;
using UnityEngine;

public class SkeletonArcherAttack : SkeletonAttackBase
{
    [Header("Arrow Rain")]
    [SerializeField] private GameObject targetZonePrefab;
    [SerializeField] private GameObject arrowPrefab;

    [SerializeField] private float lockDuration = 0.5f;

    [SerializeField] private LayerMask playerLayer;

    [Header("Animation")]
    [SerializeField] private Animator bodyAnimator;
    [SerializeField] private Animator bowAnimator;

    [SerializeField] private float attackDuration = 1.15f;
    [SerializeField] private Transform bow;

    private void Awake()
    {
        if (bodyAnimator == null)
            bodyAnimator = GetComponent<Animator>();

        if (bow != null)
        {
            bowAnimator = bow.GetComponent<Animator>();
        }
    }

    public override IEnumerator AttackRoutine(Transform player)
    {
        if (bodyAnimator != null )
        {
            bodyAnimator.ResetTrigger("Attack");
            bowAnimator.ResetTrigger("Attack");

            bodyAnimator.SetTrigger("Attack");
            bowAnimator.SetTrigger("Attack");
        }

        yield return new WaitForSeconds(attackDuration);

        yield return StartCoroutine(ArrowRainAttack(player));

        if (bodyAnimator != null && bowAnimator != null)
        {
            bodyAnimator.SetTrigger("Finished");
            bowAnimator.SetTrigger("Finished");
        }

        yield return new WaitForSeconds(1f);

        if (bow != null)
        {
            bow.rotation = Quaternion.identity;
        }
    }

    private IEnumerator ArrowRainAttack(Transform player)
    {
        if (player == null) yield break; GameObject zone = Instantiate(targetZonePrefab, player.position, Quaternion.identity); float elapsed = 0f; // Follow player while (elapsed < followDuration)
        { 
            if (zone != null && player != null) 
            { 
                zone.transform.position = new Vector3( player.position.x, player.position.y - 1.5f, player.position.z ); 
            } 
            elapsed += Time.deltaTime; yield return null; 
        } // Lock position
            Vector3 strikePosition = zone.transform.position; // Give player time to dodge
            yield return new WaitForSeconds(lockDuration); 
        if (zone != null) 
            Destroy(zone);
        GameObject arrowObj = Instantiate(arrowPrefab, strikePosition, Quaternion.identity); 
        ArrowStrike strike = arrowObj.GetComponent<ArrowStrike>(); 
        Instantiate(arrowPrefab, strikePosition, Quaternion.identity);
    }



}