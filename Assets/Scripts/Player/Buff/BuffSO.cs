using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBuff", menuName = "Buff/BuffSO")]
public class BuffSO : ScriptableObject
{
    public string buffName;
    public BuffModifierData[] modifiersData;
}

[Serializable]
public class BuffModifierData
{
    public PlayerStatModifierSO StatModifier;
    public float value;
}