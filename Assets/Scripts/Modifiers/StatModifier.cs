using System;

[Serializable]
public struct StatModifier
{
    public StatType stat;
    public ModifierType type;
    public float value;

    public StatModifier(StatType stat, ModifierType type, float value)
    {
        this.stat = stat;
        this.type = type;
        this.value = value;
    }
}
