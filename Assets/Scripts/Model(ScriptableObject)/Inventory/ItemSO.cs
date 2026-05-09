using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class ItemSO : ScriptableObject
{
    [field: SerializeField]
    public bool IsStackable { get; set; }

    public int ID => GetInstanceID();

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
}

[Serializable]
public class ModifierData
{
    public PlayerStatModifierSO statModifier;
    public float value;
}
