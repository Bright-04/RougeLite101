using UnityEngine;

public class PooledProjectile : MonoBehaviour
{
    private ProjectilePool owner;
    private bool released;

    public void Initialize(ProjectilePool pool)
    {
        owner = pool;
    }

    private void OnEnable()
    {
        released = false;
    }

    public void Release()
    {
        if (released)
        {
            return;
        }

        released = true;

        if (owner != null)
        {
            owner.Release(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
