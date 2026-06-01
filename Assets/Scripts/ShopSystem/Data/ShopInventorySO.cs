using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopInventory", menuName = "Shop/Inventory")]
public class ShopInventorySO : ScriptableObject
{
    public string shopName;

    [TextArea]
    public string greeting;

    [Header("Portrait")]
    public Sprite portraitSprite;
    public RuntimeAnimatorController portraitAnimator;

    [Header("Items")]
    public List<ShopItem> items = new List<ShopItem>();
}
