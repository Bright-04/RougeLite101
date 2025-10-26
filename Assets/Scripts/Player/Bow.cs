using UnityEngine;

public class Bow : Weapon
{
    public GameObject arrowPrefab;
    public Transform shootPoint;

    public override void Use()
    {
        if (Time.time < nextUseTime) return;
        nextUseTime = Time.time + cooldown;
        Instantiate(arrowPrefab, shootPoint.position, shootPoint.rotation);
    }
}
