using UnityEngine;
using UnityEngine.InputSystem;
using RougeLite.Events;

public class SpellCaster : EventBehaviour
{
    public Spell[] spellSlots; // drag your 3 spells here in inspector

    private PlayerStats stats;
    private Animator animator;
    private PlayerControls playerControls;
    private float[] cooldownTimers;

    protected override void Awake()
    {
        // Call base class Awake to initialize event system
        base.Awake();
        
        // Get and validate critical components
        stats = GetComponent<PlayerStats>();
        if (stats == null)
        {
            Debug.LogError($"SpellCaster: PlayerStats component missing on {gameObject.name}! Spell damage calculations will not work.", this);
        }
        
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError($"SpellCaster: Animator component missing on {gameObject.name}! Spell animations will not work.", this);
        }
        
        // Validate spell slots array
        if (spellSlots == null || spellSlots.Length == 0)
        {
            Debug.LogWarning($"SpellCaster: No spells assigned to spell slots on {gameObject.name}. Configure spells in the inspector.", this);
            cooldownTimers = new float[0];
        }
        else
        {
            cooldownTimers = new float[spellSlots.Length];
        }

        // Initialize input system
        playerControls = new PlayerControls();
        if (playerControls != null)
        {
            playerControls.Combat.SpellCasting.performed += OnSpellCastingPerformed;
        }

        Debug.Log("SpellCaster initialized.");
    }

    private void OnEnable() => playerControls?.Enable();
    private void OnDisable() => playerControls?.Disable();

    protected override void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (playerControls != null)
        {
            playerControls.Combat.SpellCasting.performed -= OnSpellCastingPerformed;
            playerControls.Disable();
            playerControls.Dispose();
        }
        
        // Call base class OnDestroy for event system cleanup
        base.OnDestroy();
    }

    private void Update()
    {
        for (int i = 0; i < cooldownTimers.Length; i++)
        {
            if (cooldownTimers[i] > 0)
                cooldownTimers[i] -= Time.deltaTime;
        }
    }

    private void OnSpellCastingPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("SpellCasting input received");

        // Check which spell key was pressed and cast the corresponding spell
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            TryCastSpell(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            TryCastSpell(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
            TryCastSpell(2);
    }


    private void TryCastSpell(int index)
    {
        Debug.Log($"Trying to cast spell in slot {index}");

        // Validate array bounds and spell slot
        if (spellSlots == null || index >= spellSlots.Length || index < 0)
        {
            Debug.LogWarning($"SpellCaster: Invalid spell slot index {index} or spellSlots array is null.");
            return;
        }

        Spell spell = spellSlots[index];
        if (spell == null)
        {
            Debug.Log("No spell assigned to this slot.");
            return;
        }

        // Check cooldown (with bounds checking)
        if (cooldownTimers != null && index < cooldownTimers.Length && cooldownTimers[index] > 0)
        {
            Debug.Log($"{spell.spellName} is on cooldown.");
            return;
        }

        // Validate player stats for mana check
        if (stats == null)
        {
            Debug.LogError("SpellCaster: PlayerStats is null, cannot check mana.");
            return;
        }

        if (stats.currentMana < spell.manaCost)
        {
            Debug.Log("Not enough mana!");
            return;
        }

        CastSpell(spell);
        stats.UseMana(spell.manaCost);
        
        // Set cooldown (with bounds checking)
        if (cooldownTimers != null && index < cooldownTimers.Length)
        {
            cooldownTimers[index] = spell.cooldown;
        }
    }


    private void CastSpell(Spell spell)
    {
        if (spell == null)
        {
            Debug.LogError("SpellCaster: Cannot cast null spell.");
            return;
        }

        Vector2 mouseWorldPos = Vector2.zero;
        
        // Safely get mouse position
        if (Camera.main != null && Mouse.current != null)
        {
            mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        }
        else
        {
            Debug.LogWarning("SpellCaster: Main camera or mouse input is null, using default position.");
        }

        // Play animation if available
        if (animator != null && !string.IsNullOrEmpty(spell.castAnimation))
        {
            animator.SetTrigger(spell.castAnimation);
        }

        // Spawn spell prefab if available
        GameObject spellProjectile = null;
        if (spell.spellPrefab != null)
        {
            Vector2 spawnPos;
            
            // Different spawn positions for different spell types
            if (spell.spellName == "Lightning")
            {
                // Lightning spawns at mouse position
                spawnPos = mouseWorldPos;
            }
            else
            {
                // Other spells (like Fireball) spawn at player position
                spawnPos = (Vector2)transform.position + Vector2.up * 0.5f;
            }
            
            spellProjectile = Instantiate(spell.spellPrefab, spawnPos, Quaternion.identity);

            if (spellProjectile != null)
            {
                var fireball = spellProjectile.GetComponent<FireballSpell>();
                if (fireball != null)
                {
                    Vector2 dir = (mouseWorldPos - spawnPos).normalized;
                    
                    // Calculate the angle to face the target direction
                    // Add 180 degrees to show the back of the fireball as it flies away
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 180f;
                    spellProjectile.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                    
                    // Set the direction for the fireball to use
                    fireball.SetDirection(dir);
                }

                Debug.Log($"Spawned {spell.spellName} at {spawnPos} toward {mouseWorldPos}");
            }
        }
        else
        {
            Debug.LogWarning("Spell prefab is NULL on cast!");
        }

        // Broadcast spell cast event
        var spellData = new SpellCastData(
            caster: gameObject,
            spellName: spell.spellName,
            castPos: transform.position,
            targetPos: mouseWorldPos,
            cost: spell.manaCost
        );
        
        var spellEvent = new SpellCastEvent(spellData, gameObject);
        
        BroadcastEvent(spellEvent);

        Debug.Log($"Cast {spell.spellName} toward {mouseWorldPos}");
    }


}
