using UnityEngine;
using System;

public class PlayerMoney : MonoBehaviour
{
    [SerializeField]
    private int startingGold = 500;

    public int Gold { get; private set; }

    public event Action<int> OnGoldChanged;

    private void Awake()
    {
        Gold = startingGold;
        OnGoldChanged?.Invoke(Gold);
    }

    public void AddGold(int amount)
    {
        Gold += amount;
        OnGoldChanged?.Invoke(Gold);
    }

    public bool SpendGold(int amount)
    {
        if (Gold < amount)
            return false;

        Gold -= amount;

        OnGoldChanged?.Invoke(Gold);

        return true;
    }
}
