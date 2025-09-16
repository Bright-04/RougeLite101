using UnityEngine;
using RougeLite.Events;

namespace RougeLite.Examples
{
    /// <summary>
    /// Example script demonstrating how to use the expanded event system
    /// This shows practical usage patterns for the new event types
    /// </summary>
    public class ExpandedEventUsageExample : EventBehaviour
    {
        [Header("Example Configuration")]
        [SerializeField] private bool enableExamples = false;
        [SerializeField] private float exampleDelay = 2f;

        private void Start()
        {
            if (enableExamples)
            {
                // Register for various events to demonstrate listening
                RegisterEventListeners();
                
                // Schedule example events to be fired
                InvokeRepeating(nameof(FireExampleEvents), exampleDelay, exampleDelay * 3f);
            }
        }

        private void RegisterEventListeners()
        {
            // Movement and Input Events
            RegisterForEvent<PlayerInputEvent>(OnPlayerInput);
            RegisterForEvent<PlayerJumpEvent>(OnPlayerJump);
            RegisterForEvent<PlayerDashEvent>(OnPlayerDash);
            RegisterForEvent<PlayerInteractionEvent>(OnPlayerInteraction);

            // Enhanced Combat Events
            RegisterForEvent<DetailedDamageEvent>(OnDetailedDamage);
            RegisterForEvent<StatusEffectAppliedEvent>(OnStatusEffectApplied);
            RegisterForEvent<ComboAttackEvent>(OnComboAttack);
            RegisterForEvent<WeaponEquipEvent>(OnWeaponEquip);

            // Progression Events
            RegisterForEvent<ExperienceGainedEvent>(OnExperienceGained);
            RegisterForEvent<SkillUpgradeEvent>(OnSkillUpgrade);
            RegisterForEvent<AchievementUnlockedEvent>(OnAchievementUnlocked);

            // Environmental Events
            RegisterForEvent<DoorStateChangedEvent>(OnDoorStateChanged);
            RegisterForEvent<CollectiblePickedUpEvent>(OnCollectiblePickedUp);
            RegisterForEvent<AreaEnteredEvent>(OnAreaEntered);

            // Audio/Visual Events
            RegisterForEvent<PlaySoundEffectEvent>(OnPlaySoundEffect);
            RegisterForEvent<SpawnParticleEffectEvent>(OnSpawnParticleEffect);
            RegisterForEvent<ScreenShakeEvent>(OnScreenShake);

            // Roguelike-Specific Events
            RegisterForEvent<LootDroppedEvent>(OnLootDropped);
            RegisterForEvent<RoomClearedEvent>(OnRoomCleared);
            RegisterForEvent<BossEncounterStartEvent>(OnBossEncounterStart);
            RegisterForEvent<PowerUpActivatedEvent>(OnPowerUpActivated);
            RegisterForEvent<RunStartedEvent>(OnRunStarted);

            Debug.Log("ExpandedEventUsageExample: Registered for all expanded events");
        }

        protected override void OnDestroy()
        {
            // Unregister all events to prevent memory leaks
            if (enableExamples)
            {
                UnregisterEventListeners();
            }
            
            base.OnDestroy();
        }

        private void UnregisterEventListeners()
        {
            // Movement and Input Events
            UnregisterFromEvent<PlayerInputEvent>(OnPlayerInput);
            UnregisterFromEvent<PlayerJumpEvent>(OnPlayerJump);
            UnregisterFromEvent<PlayerDashEvent>(OnPlayerDash);
            UnregisterFromEvent<PlayerInteractionEvent>(OnPlayerInteraction);

            // Enhanced Combat Events
            UnregisterFromEvent<DetailedDamageEvent>(OnDetailedDamage);
            UnregisterFromEvent<StatusEffectAppliedEvent>(OnStatusEffectApplied);
            UnregisterFromEvent<ComboAttackEvent>(OnComboAttack);
            UnregisterFromEvent<WeaponEquipEvent>(OnWeaponEquip);

            // Progression Events
            UnregisterFromEvent<ExperienceGainedEvent>(OnExperienceGained);
            UnregisterFromEvent<SkillUpgradeEvent>(OnSkillUpgrade);
            UnregisterFromEvent<AchievementUnlockedEvent>(OnAchievementUnlocked);

            // Environmental Events
            UnregisterFromEvent<DoorStateChangedEvent>(OnDoorStateChanged);
            UnregisterFromEvent<CollectiblePickedUpEvent>(OnCollectiblePickedUp);
            UnregisterFromEvent<AreaEnteredEvent>(OnAreaEntered);

            // Audio/Visual Events
            UnregisterFromEvent<PlaySoundEffectEvent>(OnPlaySoundEffect);
            UnregisterFromEvent<SpawnParticleEffectEvent>(OnSpawnParticleEffect);
            UnregisterFromEvent<ScreenShakeEvent>(OnScreenShake);

            // Roguelike-Specific Events
            UnregisterFromEvent<LootDroppedEvent>(OnLootDropped);
            UnregisterFromEvent<RoomClearedEvent>(OnRoomCleared);
            UnregisterFromEvent<BossEncounterStartEvent>(OnBossEncounterStart);
            UnregisterFromEvent<PowerUpActivatedEvent>(OnPowerUpActivated);
            UnregisterFromEvent<RunStartedEvent>(OnRunStarted);

            Debug.Log("ExpandedEventUsageExample: Unregistered from all events");
        }

        private void FireExampleEvents()
        {
            if (!enableExamples) return;

            // Example: Player Input Event
            var inputData = new InputData
            {
                inputName = "Jump",
                inputValue = Vector2.up,
                inputMagnitude = 1f,
                isPressed = true,
                isHeld = false,
                isReleased = false
            };
            BroadcastEvent(new PlayerInputEvent(inputData, gameObject));

            // Example: Detailed Damage Event
            var damageData = new DetailedDamageData
            {
                baseDamage = 25f,
                finalDamage = 37.5f,
                damageType = "Fire",
                isCritical = true,
                criticalMultiplier = 1.5f,
                hitPosition = transform.position,
                knockbackDirection = Vector3.right,
                knockbackForce = 10f,
                attacker = gameObject,
                target = null,
                weaponUsed = "Fire Sword"
            };
            BroadcastEvent(new DetailedDamageEvent(damageData, gameObject));

            // Example: Experience Gained Event
            var expData = new ExperienceData
            {
                experienceGained = 150,
                totalExperience = 2350,
                currentLevel = 5,
                experienceToNextLevel = 650,
                source = "Enemy Kill"
            };
            BroadcastEvent(new ExperienceGainedEvent(expData, gameObject));

            // Example: Loot Dropped Event
            var lootData = new LootData
            {
                itemName = "Magic Potion",
                rarity = "Rare",
                quantity = 1,
                dropPosition = transform.position + Vector3.up,
                dropChance = 0.15f,
                droppedBy = "Fire Slime"
            };
            BroadcastEvent(new LootDroppedEvent(lootData, gameObject));

            // Example: Screen Shake Event
            BroadcastEvent(new ScreenShakeEvent(0.8f, 0.3f, Vector3.right, gameObject));

            Debug.Log("ExpandedEventUsageExample: Fired example events");
        }

        // Event Listeners (showing how to handle expanded events)

        private void OnPlayerInput(PlayerInputEvent eventData)
        {
            Debug.Log($"Input Event: {eventData.Data.inputName} - {eventData.Data.inputValue}");
        }

        private void OnPlayerJump(PlayerJumpEvent eventData)
        {
            Debug.Log($"Jump Event: Height {eventData.JumpHeight}, Double: {eventData.IsDoubleJump}");
        }

        private void OnPlayerDash(PlayerDashEvent eventData)
        {
            Debug.Log($"Dash Event: Direction {eventData.DashDirection}, Distance {eventData.DashDistance}");
        }

        private void OnPlayerInteraction(PlayerInteractionEvent eventData)
        {
            var data = eventData.Data;
            Debug.Log($"Interaction Event: {data.interactionType} with {data.interactable?.name}");
        }

        private void OnDetailedDamage(DetailedDamageEvent eventData)
        {
            var data = eventData.Data;
            string critText = data.isCritical ? "CRITICAL " : "";
            Debug.Log($"{critText}Damage: {data.finalDamage} ({data.damageType}) with {data.weaponUsed}");
        }

        private void OnStatusEffectApplied(StatusEffectAppliedEvent eventData)
        {
            var data = eventData.Data;
            string type = data.isDebuff ? "Debuff" : "Buff";
            Debug.Log($"{type} Applied: {data.effectName} for {data.duration}s");
        }

        private void OnComboAttack(ComboAttackEvent eventData)
        {
            Debug.Log($"Combo Attack: {eventData.ComboCount}x {eventData.ComboMultiplier} '{eventData.ComboName}'");
        }

        private void OnWeaponEquip(WeaponEquipEvent eventData)
        {
            string action = eventData.IsEquipping ? "Equipped" : "Unequipped";
            Debug.Log($"Weapon {action}: {eventData.WeaponName} (Damage: {eventData.Damage})");
        }

        private void OnExperienceGained(ExperienceGainedEvent eventData)
        {
            var data = eventData.Data;
            Debug.Log($"Experience Gained: +{data.experienceGained} from {data.source} (Total: {data.totalExperience})");
        }

        private void OnSkillUpgrade(SkillUpgradeEvent eventData)
        {
            var data = eventData.Data;
            Debug.Log($"Skill Upgraded: {data.skillName} {data.previousLevel} → {data.newLevel}");
        }

        private void OnAchievementUnlocked(AchievementUnlockedEvent eventData)
        {
            Debug.Log($"Achievement Unlocked: '{eventData.AchievementName}' (+{eventData.RewardPoints} points)");
        }

        private void OnDoorStateChanged(DoorStateChangedEvent eventData)
        {
            var data = eventData.Data;
            string state = data.isOpening ? "opened" : "closed";
            Debug.Log($"Door {state} at {data.doorPosition}");
        }

        private void OnCollectiblePickedUp(CollectiblePickedUpEvent eventData)
        {
            var data = eventData.Data;
            Debug.Log($"Collected: {data.quantity}x {data.itemName} ({data.rarity}) +{data.valuePoints} points");
        }

        private void OnAreaEntered(AreaEnteredEvent eventData)
        {
            string hazard = eventData.IsHazardous ? " [HAZARDOUS]" : "";
            Debug.Log($"Entered Area: {eventData.AreaName} ({eventData.AreaType}){hazard}");
        }

        private void OnPlaySoundEffect(PlaySoundEffectEvent eventData)
        {
            var data = eventData.Data;
            Debug.Log($"Play Sound: {data.soundName} at {data.position} (Volume: {data.volume})");
        }

        private void OnSpawnParticleEffect(SpawnParticleEffectEvent eventData)
        {
            var data = eventData.Data;
            Debug.Log($"Spawn Particles: {data.effectName} at {data.position} for {data.duration}s");
        }

        private void OnScreenShake(ScreenShakeEvent eventData)
        {
            Debug.Log($"Screen Shake: Intensity {eventData.Intensity} for {eventData.Duration}s");
        }

        private void OnLootDropped(LootDroppedEvent eventData)
        {
            var data = eventData.Data;
            Debug.Log($"Loot Dropped: {data.quantity}x {data.itemName} ({data.rarity}) by {data.droppedBy}");
        }

        private void OnRoomCleared(RoomClearedEvent eventData)
        {
            string perfect = eventData.PerfectClear ? " [PERFECT]" : "";
            Debug.Log($"Room Cleared: {eventData.RoomName} - {eventData.EnemiesDefeated} enemies in {eventData.ClearTime:F2}s{perfect}");
        }

        private void OnBossEncounterStart(BossEncounterStartEvent eventData)
        {
            var data = eventData.Data;
            Debug.Log($"Boss Encounter: {data.bossName} Phase {data.phase} ({data.currentHealth}/{data.maxHealth} HP)");
        }

        private void OnPowerUpActivated(PowerUpActivatedEvent eventData)
        {
            var data = eventData.Data;
            Debug.Log($"Power-Up Activated: {data.powerUpName} for {data.duration}s - {data.effectDescription}");
        }

        private void OnRunStarted(RunStartedEvent eventData)
        {
            Debug.Log($"New Run Started: #{eventData.RunNumber} - {eventData.Difficulty} (Seed: {eventData.Seed})");
        }

        // Example methods to manually trigger events (for testing)
        [ContextMenu("Test Player Jump")]
        private void TestPlayerJump()
        {
            BroadcastEvent(new PlayerJumpEvent(5.5f, false, gameObject));
        }

        [ContextMenu("Test Critical Hit")]
        private void TestCriticalHit()
        {
            var damageData = new DetailedDamageData
            {
                baseDamage = 50f,
                finalDamage = 125f,
                damageType = "Lightning",
                isCritical = true,
                criticalMultiplier = 2.5f,
                hitPosition = transform.position,
                knockbackDirection = Vector3.up,
                knockbackForce = 15f,
                attacker = gameObject,
                weaponUsed = "Lightning Blade"
            };
            BroadcastEvent(new DetailedDamageEvent(damageData, gameObject));
        }

        [ContextMenu("Test Room Cleared")]
        private void TestRoomCleared()
        {
            BroadcastEvent(new RoomClearedEvent("Goblin Den", 8, 45.7f, true, gameObject));
        }

        [ContextMenu("Test Achievement Unlock")]
        private void TestAchievementUnlock()
        {
            BroadcastEvent(new AchievementUnlockedEvent(
                "First Blood", 
                "Defeat your first enemy", 
                100, 
                gameObject
            ));
        }
    }
}