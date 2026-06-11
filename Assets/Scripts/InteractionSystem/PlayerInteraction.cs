using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.SceneManagement;
using System.Linq;

public interface IInteractable
{
    void Interact(GameObject interactor);

    string GetInteractionText(GameObject interactor);
}

public class PlayerInteraction : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float interactRadius = 1.5f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("UI")]
    private InteractionPromptUI promptUI;

    private PlayerControls playerControls;

    private IInteractable currentInteractable;

    private void FindPromptUI()
    {
        promptUI = FindObjectsByType<InteractionPromptUI>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault();
    }

    private void Start()
    {
        if (InputManager.Instance == null)
        {
            Debug.LogWarning("PlayerInteraction: InputManager not found.", this);
            return;
        }

        playerControls = InputManager.Instance.Controls;
        playerControls.Combat.Interact.performed += TryInteract;
        FindPromptUI();
    }

    private void Update()
    {
        DetectClosestInteractable();
    }


    private void DetectClosestInteractable()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRadius, interactableLayer);
        currentInteractable = null;

        float closestDistance = Mathf.Infinity;
        foreach (Collider2D hit in hits)
        {
            IInteractable interactable = hit.GetComponent<IInteractable>();
            if (interactable == null) continue;
            float distance = Vector2.Distance(transform.position, hit.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                currentInteractable = interactable;
            }
        }    
        UpdatePrompt();
    }

    private void UpdatePrompt()
    {
        if (promptUI == null) return;

        if (currentInteractable == null)
        {
            promptUI.Hide();
            return;
        }

        string text = currentInteractable.GetInteractionText(gameObject);

        if (string.IsNullOrEmpty(text))
        {
            promptUI.Hide();
            return;
        }

        promptUI.Show(text);
    }

    private void TryInteract(InputAction.CallbackContext ctx)
    {
        currentInteractable?.Interact(gameObject);      
    }

    //private void OnDrawGizmosSelected()
    //{
    //    Gizmos.color = Color.yellow;
    //    Gizmos.DrawWireSphere(transform.position, interactRadius);
    //}
}
