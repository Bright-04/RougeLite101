using System;

[Serializable]
public class PlayerMoneySaveData
{
    public int goldData;

    public PlayerMoneySaveData(PlayerMoney gold)
    {
        goldData = gold.Gold;
    }
}
