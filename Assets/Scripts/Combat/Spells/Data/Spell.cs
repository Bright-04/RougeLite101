using UnityEngine;

[CreateAssetMenu(fileName = "NewSpell", menuName = "Spells/Spell")]
public class Spell : ScriptableObject
{
    public string spellName;
    public float manaCost;
    public float damage;
    public float cooldown;
    public Sprite icon;
    public GameObject spellPrefab;
    public string castAnimation;

    public float castRange = 5f;
}
