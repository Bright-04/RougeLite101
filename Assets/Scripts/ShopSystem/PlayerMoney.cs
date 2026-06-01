using UnityEngine;

public class PlayerMoney : MonoBehaviour
{
    [SerializeField]
    private int startingGold = 500;

    public int Gold { get; private set; }

    private void Awake()
    {
        Gold = startingGold;
    }

    public void AddGold(int amount)
    {
        Gold += amount;
    }

    public bool SpendGold(int amount)
    {
        if (Gold < amount) return false;

        Gold -= amount;

        return true;
    }
}
