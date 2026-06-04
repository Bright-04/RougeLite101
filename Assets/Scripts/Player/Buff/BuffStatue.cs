using UnityEngine;
using UnityEngine.InputSystem;

public class BuffStatue : MonoBehaviour
{
    [SerializeField] private BuffSO buff;

    private bool playerInRange;
    private bool used = false;
    private GameObject player;
    private PlayerControls playerControls;

    private void Start()
    {
        playerControls = InputManager.Instance.Controls;
        playerControls.Combat.Interact.performed += OnInteract;
    }

    private void OnDestroy()
    {
        if (playerControls != null)
        {
            playerControls.Combat.Interact.performed -= OnInteract;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        playerInRange = true;
        player = collision.gameObject;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        playerInRange = false;
        player = null;
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        Debug.Log("Tương tác được");
        Debug.Log("playerInRange: " + playerInRange);
        Debug.Log("used: " + used);
        Debug.Log("player null: " + (player));
        Debug.Log("buff null: " + (buff));

        if (!playerInRange || used == true || player == null || buff == null)
        {
            Debug.Log("Không cộng chi số được");
            return;
        }
        foreach (BuffModifierData modifier in buff.modifiersData)
        {
            if (modifier.StatModifier != null)
            {
                modifier.StatModifier.AffectCharacter(player, modifier.value);

            }
        }

        used = true;

    }
}