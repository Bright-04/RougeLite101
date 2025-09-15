using UnityEngine;

namespace RougeLite.Events
{
    // =========================
    // PLAYER EVENTS
    // =========================

    /// <summary>
    /// Data structure for player health-related events
    /// </summary>
    [System.Serializable]
    public struct PlayerHealthData
    {
        public float currentHealth;
        public float maxHealth;
        public float damage;
        public GameObject damageSource;

        public PlayerHealthData(float current, float max, float dmg = 0f, GameObject source = null)
        {
            currentHealth = current;
            maxHealth = max;
            damage = dmg;
            damageSource = source;
        }
    }

    /// <summary>
    /// Data structure for player mana-related events
    /// </summary>
    [System.Serializable]
    public struct PlayerManaData
    {
        public float currentMana;
        public float maxMana;
        public float manaCost;
        public string spellName;

        public PlayerManaData(float current, float max, float cost = 0f, string spell = "")
        {
            currentMana = current;
            maxMana = max;
            manaCost = cost;
            spellName = spell;
        }
    }

    /// <summary>
    /// Fired when player takes damage
    /// </summary>
    public class PlayerDamagedEvent : GameEvent<PlayerHealthData>
    {
        public PlayerDamagedEvent(PlayerHealthData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Fired when player dies
    /// </summary>
    public class PlayerDeathEvent : GameEvent<PlayerHealthData>
    {
        public PlayerDeathEvent(PlayerHealthData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Fired when player health is restored
    /// </summary>
    public class PlayerHealedEvent : GameEvent<PlayerHealthData>
    {
        public PlayerHealedEvent(PlayerHealthData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Fired when player uses mana (e.g., casting spells)
    /// </summary>
    public class PlayerManaUsedEvent : GameEvent<PlayerManaData>
    {
        public PlayerManaUsedEvent(PlayerManaData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Fired when player mana is restored
    /// </summary>
    public class PlayerManaRestoredEvent : GameEvent<PlayerManaData>
    {
        public PlayerManaRestoredEvent(PlayerManaData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Fired when player levels up
    /// </summary>
    public class PlayerLevelUpEvent : GameEvent<int>
    {
        public PlayerLevelUpEvent(int newLevel, GameObject source = null) : base(newLevel, source) { }
    }

    /// <summary>
    /// Fired when player respawns
    /// </summary>
    public class PlayerRespawnEvent : GameEvent
    {
        public Vector3 RespawnPosition { get; private set; }

        public PlayerRespawnEvent(Vector3 position, GameObject source = null) : base(source)
        {
            RespawnPosition = position;
        }

        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $" at position {RespawnPosition}";
        }
    }

    // =========================
    // COMBAT EVENTS
    // =========================

    /// <summary>
    /// Data structure for attack events
    /// </summary>
    [System.Serializable]
    public struct AttackData
    {
        public GameObject attacker;
        public GameObject target;
        public float damage;
        public Vector3 attackPosition;
        public string attackType;
        public bool isCritical;

        public AttackData(GameObject attacker, GameObject target, float damage, Vector3 position, string type = "melee", bool critical = false)
        {
            this.attacker = attacker;
            this.target = target;
            this.damage = damage;
            this.attackPosition = position;
            this.attackType = type;
            this.isCritical = critical;
        }
    }

    /// <summary>
    /// Data structure for spell casting events
    /// </summary>
    [System.Serializable]
    public struct SpellCastData
    {
        public GameObject caster;
        public string spellName;
        public Vector3 castPosition;
        public Vector3 targetPosition;
        public float manaCost;

        public SpellCastData(GameObject caster, string spellName, Vector3 castPos, Vector3 targetPos, float cost)
        {
            this.caster = caster;
            this.spellName = spellName;
            this.castPosition = castPos;
            this.targetPosition = targetPos;
            this.manaCost = cost;
        }
    }

    /// <summary>
    /// Fired when any entity performs an attack
    /// </summary>
    public class AttackPerformedEvent : GameEvent<AttackData>
    {
        public AttackPerformedEvent(AttackData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Fired when any entity takes damage
    /// </summary>
    public class DamageDealtEvent : GameEvent<AttackData>
    {
        public DamageDealtEvent(AttackData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Fired when a spell is cast
    /// </summary>
    public class SpellCastEvent : GameEvent<SpellCastData>
    {
        public SpellCastEvent(SpellCastData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Fired when a critical hit occurs
    /// </summary>
    public class CriticalHitEvent : GameEvent<AttackData>
    {
        public CriticalHitEvent(AttackData data, GameObject source = null) : base(data, source) { }
    }

    // =========================
    // ENEMY EVENTS
    // =========================

    /// <summary>
    /// Data structure for enemy-related events
    /// </summary>
    [System.Serializable]
    public struct EnemyData
    {
        public GameObject enemy;
        public string enemyType;
        public float health;
        public float maxHealth;
        public Vector3 position;

        public EnemyData(GameObject enemy, string type, float health, float maxHealth, Vector3 position)
        {
            this.enemy = enemy;
            this.enemyType = type;
            this.health = health;
            this.maxHealth = maxHealth;
            this.position = position;
        }
    }

    /// <summary>
    /// Fired when an enemy dies
    /// </summary>
    public class EnemyDeathEvent : GameEvent<EnemyData>
    {
        public EnemyDeathEvent(EnemyData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Fired when an enemy is spawned
    /// </summary>
    public class EnemySpawnedEvent : GameEvent<EnemyData>
    {
        public EnemySpawnedEvent(EnemyData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Fired when an enemy enters combat (starts chasing player)
    /// </summary>
    public class EnemyAggroEvent : GameEvent<EnemyData>
    {
        public EnemyAggroEvent(EnemyData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Fired when an enemy loses aggro (stops chasing player)
    /// </summary>
    public class EnemyLostAggroEvent : GameEvent<EnemyData>
    {
        public EnemyLostAggroEvent(EnemyData data, GameObject source = null) : base(data, source) { }
    }

    // =========================
    // GAME STATE EVENTS
    // =========================

    /// <summary>
    /// Fired when the game is paused
    /// </summary>
    public class GamePausedEvent : GameEvent
    {
        public GamePausedEvent(GameObject source = null) : base(source) { }
    }

    /// <summary>
    /// Fired when the game is resumed
    /// </summary>
    public class GameResumedEvent : GameEvent
    {
        public GameResumedEvent(GameObject source = null) : base(source) { }
    }

    /// <summary>
    /// Fired when the game is over
    /// </summary>
    public class GameOverEvent : GameEvent
    {
        public string Reason { get; private set; }

        public GameOverEvent(string reason, GameObject source = null) : base(source)
        {
            Reason = reason;
        }

        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $" - Reason: {Reason}";
        }
    }

    // =========================
    // UI EVENTS
    // =========================

    /// <summary>
    /// Fired when UI needs to be updated (health bars, mana bars, etc.)
    /// </summary>
    public class UIUpdateEvent : GameEvent
    {
        public string UIElement { get; private set; }

        public UIUpdateEvent(string uiElement, GameObject source = null) : base(source)
        {
            UIElement = uiElement;
        }

        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $" - UI Element: {UIElement}";
        }
    }

    // =========================
    // ITEM/INVENTORY EVENTS
    // =========================

    /// <summary>
    /// Data structure for item-related events
    /// </summary>
    [System.Serializable]
    public struct ItemData
    {
        public string itemName;
        public int quantity;
        public GameObject itemObject;

        public ItemData(string name, int qty = 1, GameObject obj = null)
        {
            itemName = name;
            quantity = qty;
            itemObject = obj;
        }
    }

    /// <summary>
    /// Fired when an item is collected
    /// </summary>
    public class ItemCollectedEvent : GameEvent<ItemData>
    {
        public ItemCollectedEvent(ItemData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Fired when an item is used
    /// </summary>
    public class ItemUsedEvent : GameEvent<ItemData>
    {
        public ItemUsedEvent(ItemData data, GameObject source = null) : base(data, source) { }
    }
}