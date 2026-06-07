using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class ItemSO : ScriptableObject
{
    [field: SerializeField]
    public bool IsStackable { get; set; }

    /// <summary>
    /// Runtime-only identity for the loaded ScriptableObject instance.
    /// This is backed by GetInstanceID and must not be used for save/load persistence.
    /// </summary>
    public int ID => GetInstanceID();

    [field: SerializeField]
    public string ItemId { get; set; }

    [field: SerializeField]
    public int MaxStackSize { get; set; } = 1;

    [field: SerializeField]
    public string Name { get; set; }

    [field: SerializeField]
    [field: TextArea]
    public string Description { get; set; }

    [field: SerializeField]
    public Sprite ItemImage { get; set; }

    [SerializeField]
    public List<ModifierData> modifiersData = new List<ModifierData>();

    [SerializeField]
    private Rarity rarity;

    public Rarity Rarity => rarity;

    [field: SerializeField]
    public int BuyPrice { get; private set; } = 100;

    [field: SerializeField]
    public int SellPrice { get; private set; } = 50;

}

[Serializable]
public class ModifierData
{
    public PlayerStatModifierSO statModifier;
    public float value;

    public PlayerStatModifierSO StatModifier => statModifier;
}

public interface IDestroyableItem
{

}

public interface IItemAction
{
    public string ActionName { get; }
    //public AudioClip actionSFX { get; }
    bool PerformAction(GameObject character);
}
