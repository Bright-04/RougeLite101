using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private EquipmentDefinitionSO equipmentDefinition;
    [SerializeField] private WeaponDefinitionSO weaponDefinition;
    [SerializeField] private SpriteRenderer promptSprite;

    private EquipmentManager equipmentManager;
    private EquipmentController equipmentController;
    private InputAction interactAction;
    private bool playerInRange;
    private bool isConsumed;

    private void Start()
    {
        equipmentManager = FindAnyObjectByType<EquipmentManager>();
        equipmentController = FindAnyObjectByType<EquipmentController>();
        if (equipmentManager == null)
        {
            Debug.LogWarning("WeaponPickup: Could not find EquipmentManager in scene.", this);
        }

        if (equipmentDefinition == null)
        {
            equipmentDefinition = weaponDefinition;
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
        if (!playerInRange || isConsumed || equipmentDefinition == null)
        {
            return;
        }

        bool pickedUpNow = TryPickupEquipment();
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

    private bool TryPickupEquipment()
    {
        if (equipmentDefinition is WeaponDefinitionSO weapon)
        {
            return equipmentManager != null && equipmentManager.TryPickupWeapon(weapon, this);
        }

        if (equipmentDefinition is ArmorDefinitionSO armor)
        {
            if (equipmentController == null)
            {
                return false;
            }

            equipmentController.EquipArmor(armor);
            return true;
        }

        return false;
    }
}
