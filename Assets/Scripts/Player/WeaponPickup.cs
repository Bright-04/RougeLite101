using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private WeaponDefinitionSO weaponDefinition;
    [SerializeField] private SpriteRenderer promptSprite;

    private EquipmentManager equipmentManager;
    private InputAction interactAction;
    private bool playerInRange;
    private bool isConsumed;

    private void Start()
    {
        equipmentManager = FindAnyObjectByType<EquipmentManager>();
        if (equipmentManager == null)
        {
            Debug.LogWarning("WeaponPickup: Could not find EquipmentManager in scene.", this);
        }

        if (InputManager.Instance != null)
        {
            interactAction = InputManager.Instance.Controls.asset.FindAction("Combat/Interact");
            if (interactAction != null)
            {
                interactAction.performed += OnInteractPerformed;
            }
            else
            {
                Debug.LogWarning("WeaponPickup: Combat/Interact action was not found.", this);
            }
        }

        SetPromptVisible(false);
    }

    private void OnDestroy()
    {
        if (interactAction != null)
        {
            interactAction.performed -= OnInteractPerformed;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || isConsumed)
        {
            return;
        }

        playerInRange = true;
        SetPromptVisible(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || isConsumed)
        {
            return;
        }

        playerInRange = false;
        SetPromptVisible(false);
    }

    private void OnInteractPerformed(InputAction.CallbackContext _)
    {
        if (!playerInRange || isConsumed || weaponDefinition == null || equipmentManager == null)
        {
            return;
        }

        bool pickedUpNow = equipmentManager.TryPickupWeapon(weaponDefinition, this);
        if (pickedUpNow)
        {
            ConsumeAfterSuccessfulPickup();
        }
    }

    public void ConsumeAfterSuccessfulPickup()
    {
        if (isConsumed)
        {
            return;
        }

        isConsumed = true;
        SetPromptVisible(false);
        Destroy(gameObject);
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptSprite != null)
        {
            promptSprite.enabled = visible;
        }
    }
}
