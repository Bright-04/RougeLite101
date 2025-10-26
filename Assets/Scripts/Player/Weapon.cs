using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    public float cooldown = 0.5f;
    protected float nextUseTime = 0f;

    public abstract void Use();
}
