using UnityEngine;
using UnityEngine.InputSystem;

public class BuffStatue : MonoBehaviour, IInteractable
{
    [SerializeField] private BuffSO buff;

    private bool used = false;

    public void Interact(GameObject interactor)
    {
        Debug.Log("Tương tác được");

        if (used || buff == null)
        {
            Debug.Log("Không cộng chỉ số được");
            return;
        }

        foreach (BuffModifierData modifier in buff.modifiersData)
        {
            if (modifier.StatModifier != null)
            {
                modifier.StatModifier.AffectCharacter(
                    interactor,
                    modifier.value
                );
            }
        }

        used = true;

        Debug.Log("Đã nhận buff");
    }

    public string GetInteractionText(GameObject interactor)
    {
        if (used) return "";

        return $"[F] Receive {buff.buffName} Blessing";
    }
}