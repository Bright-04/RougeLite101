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

    /// <summary>
    /// Data structure for player movement events
    /// </summary>
    [System.Serializable]
    public struct PlayerMovementData
    {
        public GameObject player;
        public Vector2 velocity;
        public Vector3 position;
        public Vector3 previousPosition;

        public PlayerMovementData(GameObject player, Vector2 velocity, Vector3 position, Vector3 previousPosition)
        {
            this.player = player;
            this.velocity = velocity;
            this.position = position;
            this.previousPosition = previousPosition;
        }
    }

    /// <summary>
    /// Fired when player moves
    /// </summary>
    public class PlayerMovementEvent : GameEvent<PlayerMovementData>
    {
        public PlayerMovementEvent(PlayerMovementData data, GameObject source = null) : base(data, source) { }
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

    // =========================
    // ENHANCED INPUT AND INTERACTION EVENTS
    // =========================

    /// <summary>
    /// Data structure for input events
    /// </summary>
    [System.Serializable]
    public struct InputData
    {
        public string inputName;
        public Vector2 inputValue;
        public float inputMagnitude;
        public bool isPressed;
        public bool isHeld;
        public bool isReleased;
    }

    /// <summary>
    /// Fired when player performs input actions (jump, dash, interact, etc.)
    /// </summary>
    public class PlayerInputEvent : GameEvent<InputData>
    {
        public PlayerInputEvent(InputData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Data structure for interaction events
    /// </summary>
    [System.Serializable]
    public struct InteractionData
    {
        public GameObject interactable;
        public string interactionType;
        public Vector3 interactionPosition;
        public bool successful;
        public string failureReason;
    }

    /// <summary>
    /// Fired when player interacts with objects
    /// </summary>
    public class PlayerInteractionEvent : GameEvent<InteractionData>
    {
        public PlayerInteractionEvent(InteractionData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Fired when player starts jumping
    /// </summary>
    public class PlayerJumpEvent : GameEvent
    {
        public float JumpHeight { get; private set; }
        public bool IsDoubleJump { get; private set; }

        public PlayerJumpEvent(float jumpHeight, bool isDoubleJump = false, GameObject source = null) : base(source)
        {
            JumpHeight = jumpHeight;
            IsDoubleJump = isDoubleJump;
        }

        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $" - Height: {JumpHeight:F2}, Double: {IsDoubleJump}";
        }
    }

    /// <summary>
    /// Fired when player lands on ground
    /// </summary>
    public class PlayerLandEvent : GameEvent
    {
        public float FallDistance { get; private set; }
        public bool TakeFallDamage { get; private set; }

        public PlayerLandEvent(float fallDistance, bool takeFallDamage = false, GameObject source = null) : base(source)
        {
            FallDistance = fallDistance;
            TakeFallDamage = takeFallDamage;
        }

        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $" - Fall: {FallDistance:F2}m, Damage: {TakeFallDamage}";
        }
    }

    /// <summary>
    /// Fired when player performs dash/dodge
    /// </summary>
    public class PlayerDashEvent : GameEvent
    {
        public Vector2 DashDirection { get; private set; }
        public float DashDistance { get; private set; }
        public bool InvulnerabilityFrames { get; private set; }

        public PlayerDashEvent(Vector2 direction, float distance, bool hasIFrames = true, GameObject source = null) : base(source)
        {
            DashDirection = direction;
            DashDistance = distance;
            InvulnerabilityFrames = hasIFrames;
        }

        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $" - Dir: {DashDirection}, Dist: {DashDistance:F2}, IFrames: {InvulnerabilityFrames}";
        }
    }

    // =========================
    // ENHANCED COMBAT EVENTS
    // =========================

    /// <summary>
    /// Data structure for detailed damage information
    /// </summary>
    [System.Serializable]
    public struct DetailedDamageData
    {
        public float baseDamage;
        public float finalDamage;
        public string damageType;
        public bool isCritical;
        public float criticalMultiplier;
        public Vector3 hitPosition;
        public Vector3 knockbackDirection;
        public float knockbackForce;
        public GameObject attacker;
        public GameObject target;
        public string weaponUsed;
    }

    /// <summary>
    /// Fired when detailed damage is dealt
    /// </summary>
    public class DetailedDamageEvent : GameEvent<DetailedDamageData>
    {
        public DetailedDamageEvent(DetailedDamageData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Data structure for status effects
    /// </summary>
    [System.Serializable]
    public struct StatusEffectData
    {
        public string effectName;
        public float duration;
        public float intensity;
        public bool isDebuff;
        public GameObject source;
        public GameObject target;
    }

    /// <summary>
    /// Fired when status effect is applied
    /// </summary>
    public class StatusEffectAppliedEvent : GameEvent<StatusEffectData>
    {
        public StatusEffectAppliedEvent(StatusEffectData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Fired when status effect is removed
    /// </summary>
    public class StatusEffectRemovedEvent : GameEvent<StatusEffectData>
    {
        public StatusEffectRemovedEvent(StatusEffectData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Fired when combo attack is performed
    /// </summary>
    public class ComboAttackEvent : GameEvent
    {
        public int ComboCount { get; private set; }
        public float ComboMultiplier { get; private set; }
        public string ComboName { get; private set; }

        public ComboAttackEvent(int comboCount, float multiplier, string comboName = "", GameObject source = null) : base(source)
        {
            ComboCount = comboCount;
            ComboMultiplier = multiplier;
            ComboName = comboName;
        }

        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $" - Combo: {ComboCount}x{ComboMultiplier:F2} '{ComboName}'";
        }
    }

    /// <summary>
    /// Fired when weapon is equipped/unequipped
    /// </summary>
    public class WeaponEquipEvent : GameEvent
    {
        public string WeaponName { get; private set; }
        public bool IsEquipping { get; private set; }
        public float Damage { get; private set; }

        public WeaponEquipEvent(string weaponName, bool isEquipping, float damage = 0f, GameObject source = null) : base(source)
        {
            WeaponName = weaponName;
            IsEquipping = isEquipping;
            Damage = damage;
        }

        public override string GetDebugInfo()
        {
            string action = IsEquipping ? "Equipped" : "Unequipped";
            return base.GetDebugInfo() + $" - {action} '{WeaponName}' (Dmg: {Damage})";
        }
    }

    // =========================
    // PROJECTILE AND POOLING EVENTS
    // =========================

    /// <summary>
    /// Data structure for projectile events
    /// </summary>
    [System.Serializable]
    public struct ProjectileData
    {
        public string projectileType;
        public Vector3 position;
        public Vector2 direction;
        public float speed;
        public float damage;
        public GameObject shooter;
        public GameObject target;
        public int targetsHit;
        public float remainingLifetime;
    }

    /// <summary>
    /// Fired when projectile is launched
    /// </summary>
    public class ProjectileLaunchedEvent : GameEvent
    {
        public RougeLite.Combat.Projectile Projectile { get; private set; }
        public GameObject Shooter { get; private set; }

        public ProjectileLaunchedEvent(RougeLite.Combat.Projectile projectile, GameObject shooter, GameObject source = null) : base(source)
        {
            Projectile = projectile;
            Shooter = shooter;
        }

        public override string GetDebugInfo()
        {
            string shooterName = Shooter != null ? Shooter.name : "Unknown";
            string projectileType = Projectile != null ? Projectile.ProjectileType : "Unknown";
            return base.GetDebugInfo() + $" - {projectileType} fired by {shooterName}";
        }
    }

    /// <summary>
    /// Fired when projectile hits a target
    /// </summary>
    public class ProjectileHitEvent : GameEvent
    {
        public RougeLite.Combat.Projectile Projectile { get; private set; }
        public GameObject Target { get; private set; }
        public float Damage { get; private set; }

        public ProjectileHitEvent(RougeLite.Combat.Projectile projectile, GameObject target, float damage, GameObject source = null) : base(source)
        {
            Projectile = projectile;
            Target = target;
            Damage = damage;
        }

        public override string GetDebugInfo()
        {
            string targetName = Target != null ? Target.name : "Unknown";
            string projectileType = Projectile != null ? Projectile.ProjectileType : "Unknown";
            return base.GetDebugInfo() + $" - {projectileType} hit {targetName} for {Damage} damage";
        }
    }

    /// <summary>
    /// Fired when projectile is destroyed/returned to pool
    /// </summary>
    public class ProjectileDestroyedEvent : GameEvent
    {
        public RougeLite.Combat.Projectile Projectile { get; private set; }
        public int TargetsHit { get; private set; }
        public float LifetimeRemaining { get; private set; }

        public ProjectileDestroyedEvent(RougeLite.Combat.Projectile projectile, int targetsHit, float lifetimeRemaining, GameObject source = null) : base(source)
        {
            Projectile = projectile;
            TargetsHit = targetsHit;
            LifetimeRemaining = lifetimeRemaining;
        }

        public override string GetDebugInfo()
        {
            string projectileType = Projectile != null ? Projectile.ProjectileType : "Unknown";
            return base.GetDebugInfo() + $" - {projectileType} destroyed (Hits: {TargetsHit}, Lifetime: {LifetimeRemaining:F2}s)";
        }
    }

    /// <summary>
    /// Data structure for object pool events
    /// </summary>
    [System.Serializable]
    public struct PoolEventData
    {
        public string poolName;
        public int availableCount;
        public int activeCount;
        public int totalCount;
        public int maxPoolSize;
        public bool allowsGrowth;
    }

    /// <summary>
    /// Fired when object pool is created
    /// </summary>
    public class PoolCreatedEvent : GameEvent<PoolEventData>
    {
        public PoolCreatedEvent(PoolEventData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Fired when object is retrieved from pool
    /// </summary>
    public class ObjectRetrievedFromPoolEvent : GameEvent<PoolEventData>
    {
        public System.Type ObjectType { get; private set; }

        public ObjectRetrievedFromPoolEvent(PoolEventData data, System.Type objectType, GameObject source = null) : base(data, source)
        {
            ObjectType = objectType;
        }

        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $" - {ObjectType?.Name ?? "Unknown"} retrieved from {Data.poolName}";
        }
    }

    /// <summary>
    /// Fired when object is returned to pool
    /// </summary>
    public class ObjectReturnedToPoolEvent : GameEvent<PoolEventData>
    {
        public System.Type ObjectType { get; private set; }

        public ObjectReturnedToPoolEvent(PoolEventData data, System.Type objectType, GameObject source = null) : base(data, source)
        {
            ObjectType = objectType;
        }

        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $" - {ObjectType?.Name ?? "Unknown"} returned to {Data.poolName}";
        }
    }

    /// <summary>
    /// Fired when pool reaches capacity and starts rejecting objects
    /// </summary>
    public class PoolCapacityReachedEvent : GameEvent<PoolEventData>
    {
        public PoolCapacityReachedEvent(PoolEventData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Fired when pool is cleared/destroyed
    /// </summary>
    public class PoolClearedEvent : GameEvent<PoolEventData>
    {
        public PoolClearedEvent(PoolEventData data, GameObject source = null) : base(data, source) { }
    }

    // =========================
    // PROGRESSION AND UPGRADE EVENTS
    // =========================

    /// <summary>
    /// Data structure for experience gain
    /// </summary>
    [System.Serializable]
    public struct ExperienceData
    {
        public int experienceGained;
        public int totalExperience;
        public int currentLevel;
        public int experienceToNextLevel;
        public string source;
    }

    /// <summary>
    /// Fired when player gains experience
    /// </summary>
    public class ExperienceGainedEvent : GameEvent<ExperienceData>
    {
        public ExperienceGainedEvent(ExperienceData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Data structure for skill upgrades
    /// </summary>
    [System.Serializable]
    public struct SkillUpgradeData
    {
        public string skillName;
        public int previousLevel;
        public int newLevel;
        public string description;
        public float cooldownReduction;
        public float damageIncrease;
    }

    /// <summary>
    /// Fired when skill is upgraded
    /// </summary>
    public class SkillUpgradeEvent : GameEvent<SkillUpgradeData>
    {
        public SkillUpgradeEvent(SkillUpgradeData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Data structure for stat upgrades
    /// </summary>
    [System.Serializable]
    public struct StatUpgradeData
    {
        public string statName;
        public float previousValue;
        public float newValue;
        public float upgradeAmount;
        public int upgradePointsSpent;
    }

    /// <summary>
    /// Fired when player stat is upgraded
    /// </summary>
    public class StatUpgradeEvent : GameEvent<StatUpgradeData>
    {
        public StatUpgradeEvent(StatUpgradeData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Fired when achievement is unlocked
    /// </summary>
    public class AchievementUnlockedEvent : GameEvent
    {
        public string AchievementName { get; private set; }
        public string Description { get; private set; }
        public int RewardPoints { get; private set; }

        public AchievementUnlockedEvent(string name, string description, int rewardPoints = 0, GameObject source = null) : base(source)
        {
            AchievementName = name;
            Description = description;
            RewardPoints = rewardPoints;
        }

        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $" - '{AchievementName}': {Description} (+{RewardPoints} pts)";
        }
    }

    // =========================
    // ENVIRONMENTAL AND WORLD EVENTS
    // =========================

    /// <summary>
    /// Data structure for door interactions
    /// </summary>
    [System.Serializable]
    public struct DoorData
    {
        public bool isOpening;
        public bool requiresKey;
        public string keyType;
        public Vector3 doorPosition;
    }

    /// <summary>
    /// Fired when door state changes
    /// </summary>
    public class DoorStateChangedEvent : GameEvent<DoorData>
    {
        public DoorStateChangedEvent(DoorData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Data structure for switch/lever interactions
    /// </summary>
    [System.Serializable]
    public struct SwitchData
    {
        public bool isActivated;
        public string switchType;
        public Vector3 switchPosition;
        public GameObject[] affectedObjects;
    }

    /// <summary>
    /// Fired when switch is toggled
    /// </summary>
    public class SwitchToggledEvent : GameEvent<SwitchData>
    {
        public SwitchToggledEvent(SwitchData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Data structure for collectible items
    /// </summary>
    [System.Serializable]
    public struct CollectibleData
    {
        public string itemName;
        public int quantity;
        public string rarity;
        public Vector3 pickupPosition;
        public float valuePoints;
    }

    /// <summary>
    /// Fired when collectible is picked up
    /// </summary>
    public class CollectiblePickedUpEvent : GameEvent<CollectibleData>
    {
        public CollectiblePickedUpEvent(CollectibleData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Fired when area/zone is entered
    /// </summary>
    public class AreaEnteredEvent : GameEvent
    {
        public string AreaName { get; private set; }
        public string AreaType { get; private set; }
        public bool IsHazardous { get; private set; }

        public AreaEnteredEvent(string areaName, string areaType, bool isHazardous = false, GameObject source = null) : base(source)
        {
            AreaName = areaName;
            AreaType = areaType;
            IsHazardous = isHazardous;
        }

        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $" - '{AreaName}' ({AreaType}) Hazardous: {IsHazardous}";
        }
    }

    /// <summary>
    /// Fired when area/zone is exited
    /// </summary>
    public class AreaExitedEvent : GameEvent
    {
        public string AreaName { get; private set; }
        public float TimeSpent { get; private set; }

        public AreaExitedEvent(string areaName, float timeSpent, GameObject source = null) : base(source)
        {
            AreaName = areaName;
            TimeSpent = timeSpent;
        }

        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $" - '{AreaName}' (Time: {TimeSpent:F2}s)";
        }
    }

    // =========================
    // AUDIO AND VISUAL EFFECT EVENTS
    // =========================

    /// <summary>
    /// Data structure for sound effects
    /// </summary>
    [System.Serializable]
    public struct SoundEffectData
    {
        public string soundName;
        public Vector3 position;
        public float volume;
        public float pitch;
        public bool isLooping;
        public float duration;
    }

    /// <summary>
    /// Fired when sound effect should be played
    /// </summary>
    public class PlaySoundEffectEvent : GameEvent<SoundEffectData>
    {
        public PlaySoundEffectEvent(SoundEffectData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Data structure for particle effects
    /// </summary>
    [System.Serializable]
    public struct ParticleEffectData
    {
        public string effectName;
        public Vector3 position;
        public Vector3 direction;
        public Color color;
        public float scale;
        public float duration;
    }

    /// <summary>
    /// Fired when particle effect should be spawned
    /// </summary>
    public class SpawnParticleEffectEvent : GameEvent<ParticleEffectData>
    {
        public SpawnParticleEffectEvent(ParticleEffectData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Fired when screen shake effect should occur
    /// </summary>
    public class ScreenShakeEvent : GameEvent
    {
        public float Intensity { get; private set; }
        public float Duration { get; private set; }
        public Vector3 Direction { get; private set; }

        public ScreenShakeEvent(float intensity, float duration, Vector3 direction = default, GameObject source = null) : base(source)
        {
            Intensity = intensity;
            Duration = duration;
            Direction = direction;
        }

        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $" - Intensity: {Intensity:F2}, Duration: {Duration:F2}s";
        }
    }

    /// <summary>
    /// Fired when camera should focus on specific target
    /// </summary>
    public class CameraFocusEvent : GameEvent
    {
        public Transform Target { get; private set; }
        public float ZoomLevel { get; private set; }
        public float TransitionTime { get; private set; }

        public CameraFocusEvent(Transform target, float zoomLevel = 1f, float transitionTime = 1f, GameObject source = null) : base(source)
        {
            Target = target;
            ZoomLevel = zoomLevel;
            TransitionTime = transitionTime;
        }

        public override string GetDebugInfo()
        {
            string targetName = Target != null ? Target.name : "null";
            return base.GetDebugInfo() + $" - Target: {targetName}, Zoom: {ZoomLevel:F2}, Time: {TransitionTime:F2}s";
        }
    }

    // =========================
    // SAVE/LOAD AND PERSISTENCE EVENTS
    // =========================

    /// <summary>
    /// Data structure for save game operations
    /// </summary>
    [System.Serializable]
    public struct SaveGameData
    {
        public string saveSlotName;
        public string sceneName;
        public Vector3 playerPosition;
        public int playerLevel;
        public float currentHealth;
        public float currentMana;
        public System.DateTime saveTime;
    }

    /// <summary>
    /// Fired when game is saved
    /// </summary>
    public class GameSavedEvent : GameEvent<SaveGameData>
    {
        public GameSavedEvent(SaveGameData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Fired when game is loaded
    /// </summary>
    public class GameLoadedEvent : GameEvent<SaveGameData>
    {
        public GameLoadedEvent(SaveGameData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Data structure for settings changes
    /// </summary>
    [System.Serializable]
    public struct SettingsData
    {
        public string settingName;
        public object previousValue;
        public object newValue;
        public string category;
    }

    /// <summary>
    /// Fired when game setting is changed
    /// </summary>
    public class SettingChangedEvent : GameEvent<SettingsData>
    {
        public SettingChangedEvent(SettingsData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Fired when data needs to be persisted
    /// </summary>
    public class DataPersistenceEvent : GameEvent
    {
        public string DataType { get; private set; }
        public object Data { get; private set; }
        public bool IsLoading { get; private set; }

        public DataPersistenceEvent(string dataType, object data, bool isLoading = false, GameObject source = null) : base(source)
        {
            DataType = dataType;
            Data = data;
            IsLoading = isLoading;
        }

        public override string GetDebugInfo()
        {
            string operation = IsLoading ? "Loading" : "Saving";
            return base.GetDebugInfo() + $" - {operation} '{DataType}' data";
        }
    }

    // =========================
    // LEVEL/GAME PROGRESSION EVENTS
    // =========================

    /// <summary>
    /// Data structure for level information
    /// </summary>
    [System.Serializable]
    public struct LevelData
    {
        public int levelNumber;
        public string levelName;
        public float timeLimit;
        public int enemyCount;
        public Vector3 playerStartPosition;
        
        // Game statistics
        public int enemiesKilled;
        public float totalDamageDealt;
        public float timeCompleted;
    }

    /// <summary>
    /// Fired when the game starts
    /// </summary>
    public class GameStartEvent : GameEvent
    {
        public GameStartEvent(GameObject source = null) : base(source) { }
    }

    /// <summary>
    /// Fired when a level is completed
    /// </summary>
    public class LevelCompleteEvent : GameEvent<LevelData>
    {
        public float CompletionTime { get; private set; }
        public int Score { get; private set; }

        public LevelCompleteEvent(LevelData levelData, float completionTime, int score, GameObject source = null) : base(levelData, source)
        {
            CompletionTime = completionTime;
            Score = score;
        }

        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $" - Level: {Data.levelName}, Time: {CompletionTime:F2}s, Score: {Score}";
        }
    }

    // =========================
    // ROGUELIKE-SPECIFIC EVENTS
    // =========================

    /// <summary>
    /// Data structure for procedural generation
    /// </summary>
    [System.Serializable]
    public struct ProceduralGenerationData
    {
        public string generationType;
        public int seed;
        public Vector2Int dimensions;
        public string difficulty;
        public int roomCount;
        public float complexity;
    }

    /// <summary>
    /// Fired when level is procedurally generated
    /// </summary>
    public class LevelGeneratedEvent : GameEvent<ProceduralGenerationData>
    {
        public LevelGeneratedEvent(ProceduralGenerationData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Data structure for loot drops
    /// </summary>
    [System.Serializable]
    public struct LootData
    {
        public string itemName;
        public string rarity;
        public int quantity;
        public Vector3 dropPosition;
        public float dropChance;
        public string droppedBy;
    }

    /// <summary>
    /// Fired when loot is dropped
    /// </summary>
    public class LootDroppedEvent : GameEvent<LootData>
    {
        public LootDroppedEvent(LootData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Data structure for treasure chests
    /// </summary>
    [System.Serializable]
    public struct TreasureData
    {
        public string chestType;
        public bool isLocked;
        public string keyRequired;
        public LootData[] contents;
        public Vector3 chestPosition;
    }

    /// <summary>
    /// Fired when treasure chest is opened
    /// </summary>
    public class TreasureOpenedEvent : GameEvent<TreasureData>
    {
        public TreasureOpenedEvent(TreasureData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Data structure for room transitions
    /// </summary>
    [System.Serializable]
    public struct RoomTransitionData
    {
        public string fromRoomName;
        public string toRoomName;
        public Vector3 entryPoint;
        public Vector3 exitPoint;
        public string transitionType;
    }

    /// <summary>
    /// Fired when player transitions between rooms
    /// </summary>
    public class RoomTransitionEvent : GameEvent<RoomTransitionData>
    {
        public RoomTransitionEvent(RoomTransitionData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Fired when all enemies in room are cleared
    /// </summary>
    public class RoomClearedEvent : GameEvent
    {
        public string RoomName { get; private set; }
        public int EnemiesDefeated { get; private set; }
        public float ClearTime { get; private set; }
        public bool PerfectClear { get; private set; }

        public RoomClearedEvent(string roomName, int enemiesDefeated, float clearTime, bool perfectClear = false, GameObject source = null) : base(source)
        {
            RoomName = roomName;
            EnemiesDefeated = enemiesDefeated;
            ClearTime = clearTime;
            PerfectClear = perfectClear;
        }

        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $" - Room: '{RoomName}', Enemies: {EnemiesDefeated}, Time: {ClearTime:F2}s, Perfect: {PerfectClear}";
        }
    }

    /// <summary>
    /// Data structure for boss encounters
    /// </summary>
    [System.Serializable]
    public struct BossData
    {
        public string bossName;
        public float maxHealth;
        public float currentHealth;
        public int phase;
        public Vector3 bossPosition;
        public bool isEnraged;
    }

    /// <summary>
    /// Fired when boss encounter begins
    /// </summary>
    public class BossEncounterStartEvent : GameEvent<BossData>
    {
        public BossEncounterStartEvent(BossData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Fired when boss changes phase
    /// </summary>
    public class BossPhaseChangeEvent : GameEvent<BossData>
    {
        public BossPhaseChangeEvent(BossData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Fired when boss is defeated
    /// </summary>
    public class BossDefeatedEvent : GameEvent<BossData>
    {
        public float DefeatTime { get; private set; }
        public int Attempts { get; private set; }

        public BossDefeatedEvent(BossData data, float defeatTime, int attempts, GameObject source = null) : base(data, source)
        {
            DefeatTime = defeatTime;
            Attempts = attempts;
        }

        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $" - Boss: {Data.bossName}, Time: {DefeatTime:F2}s, Attempts: {Attempts}";
        }
    }

    /// <summary>
    /// Data structure for power-ups
    /// </summary>
    [System.Serializable]
    public struct PowerUpData
    {
        public string powerUpName;
        public float duration;
        public float intensity;
        public string effectDescription;
        public bool stackable;
    }

    /// <summary>
    /// Fired when power-up is activated
    /// </summary>
    public class PowerUpActivatedEvent : GameEvent<PowerUpData>
    {
        public PowerUpActivatedEvent(PowerUpData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Fired when power-up expires
    /// </summary>
    public class PowerUpExpiredEvent : GameEvent<PowerUpData>
    {
        public PowerUpExpiredEvent(PowerUpData data, GameObject source = null) : base(data, source) { }
    }

    /// <summary>
    /// Data structure for run statistics (for roguelike runs)
    /// </summary>
    [System.Serializable]
    public struct RunStatistics
    {
        public int runNumber;
        public float totalTime;
        public int roomsCleared;
        public int enemiesKilled;
        public int bossesDefeated;
        public float damageDealt;
        public float damageTaken;
        public int itemsCollected;
        public int deathCount;
        public string endReason;
    }

    /// <summary>
    /// Fired when run ends (success or failure)
    /// </summary>
    public class RunEndedEvent : GameEvent<RunStatistics>
    {
        public bool WasSuccessful { get; private set; }

        public RunEndedEvent(RunStatistics stats, bool wasSuccessful, GameObject source = null) : base(stats, source)
        {
            WasSuccessful = wasSuccessful;
        }

        public override string GetDebugInfo()
        {
            string result = WasSuccessful ? "Victory" : "Defeat";
            return base.GetDebugInfo() + $" - Run #{Data.runNumber}: {result} ({Data.endReason})";
        }
    }

    /// <summary>
    /// Fired when new run begins
    /// </summary>
    public class RunStartedEvent : GameEvent
    {
        public int RunNumber { get; private set; }
        public string Difficulty { get; private set; }
        public int Seed { get; private set; }

        public RunStartedEvent(int runNumber, string difficulty, int seed, GameObject source = null) : base(source)
        {
            RunNumber = runNumber;
            Difficulty = difficulty;
            Seed = seed;
        }

        public override string GetDebugInfo()
        {
            return base.GetDebugInfo() + $" - Run #{RunNumber}, Difficulty: {Difficulty}, Seed: {Seed}";
        }
    }
}