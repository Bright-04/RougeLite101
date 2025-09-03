using UnityEngine;
using UnityEngine.InputSystem;

public class SpellCaster : MonoBehaviour
{
    public Spell[] spellSlots; // drag your 3 spells here in inspector

    private PlayerStats stats;
    private Animator animator;
    private PlayerControls playerControls;
    private float[] cooldownTimers;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        animator = GetComponent<Animator>();
        cooldownTimers = new float[spellSlots.Length];

        playerControls = new PlayerControls();
        playerControls.Combat.SpellCasting.performed += OnSpellCastingPerformed;

        Debug.Log("SpellCaster initialized.");
    }

    private void OnEnable() => playerControls.Enable();
    private void OnDisable() => playerControls.Disable();

    private void Update()
    {
        for (int i = 0; i < cooldownTimers.Length; i++)
        {
            if (cooldownTimers[i] > 0)
                cooldownTimers[i] -= Time.deltaTime;
        }
    }

    //private void OnSpellCastingPerformed(InputAction.CallbackContext context)
    //{
    //    var controlPath = context.control.path;
    //    Debug.Log($"SpellCasting triggered by control: {controlPath}");

    //    if (controlPath == "<Keyboard>/1")
    //        TryCastSpell(0);
    //    else if (controlPath == "<Keyboard>/2")
    //        TryCastSpell(1);
    //    else if (controlPath == "<Keyboard>/3")
    //        TryCastSpell(2);
    //}
    private void OnSpellCastingPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("SpellCasting input received");

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

        if (index >= spellSlots.Length) return;

        Spell spell = spellSlots[index];
        if (spell == null)
        {
            Debug.Log("No spell assigned to this slot.");
            return;
        }

        if (cooldownTimers[index] > 0)
        {
            Debug.Log($"{spell.spellName} is on cooldown.");
            return;
        }

        if (stats.currentMana < spell.manaCost)
        {
            Debug.Log("Not enough mana!");
            return;
        }

        CastSpell(spell);
        stats.UseMana(spell.manaCost);
        cooldownTimers[index] = spell.cooldown;
    }


    private void CastSpell(Spell spell)
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        if (!string.IsNullOrEmpty(spell.castAnimation))
            animator.SetTrigger(spell.castAnimation);

        if (spell.spellPrefab != null)
        {
            Vector2 spawnPos = (Vector2)transform.position + Vector2.up * 0.5f;
            GameObject proj = Instantiate(spell.spellPrefab, spawnPos, Quaternion.identity);

            var fireball = proj.GetComponent<FireballSpell>();
            if (fireball != null)
            {
                Vector2 dir = (mouseWorldPos - spawnPos).normalized;
                proj.transform.right = dir;
            }

            Debug.Log($"Spawned {spell.spellName} at {spawnPos} toward {mouseWorldPos}");
        }
        else
        {
            Debug.LogWarning("Spell prefab is NULL on cast!");
        }

        Debug.Log($"Cast {spell.spellName} toward {mouseWorldPos}");
    }


}
