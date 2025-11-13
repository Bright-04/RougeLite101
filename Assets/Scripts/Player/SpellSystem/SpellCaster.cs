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

    // Method để SpellCasterUI có thể truy cập cooldown
    public float GetCooldownRemaining(int index)
    {
        if (index >= 0 && index < cooldownTimers.Length)
            return Mathf.Max(0, cooldownTimers[index]);
        return 0;
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
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            TryCastSpell(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            TryCastSpell(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
            TryCastSpell(2);
    }


    private void TryCastSpell(int index)
    {
        if (index >= spellSlots.Length) return;

        Spell spell = spellSlots[index];
        if (spell == null)
        {
            return;
        }

        if (cooldownTimers[index] > 0)
        {
            return;
        }

        if (stats.currentMana < spell.manaCost)
        {
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
            if (spell.spellPrefab.GetComponent<FireballSpell>())
            {
                Vector2 spawnPos = (Vector2)transform.position + Vector2.up * 0.5f;
                Vector2 dir = (mouseWorldPos - spawnPos).normalized;
                GameObject proj = Instantiate(spell.spellPrefab, spawnPos, Quaternion.identity);

                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                proj.transform.rotation = Quaternion.Euler(0, 0, angle + 180);
            }
            else if (spell.spellPrefab.GetComponent<LightningSpell>())
            {
                Instantiate(spell.spellPrefab, mouseWorldPos, Quaternion.identity);
            }
        }
    }


}
