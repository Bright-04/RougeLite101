using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TransferUI : MonoBehaviour
{
    [SerializeField] private Slider amountSlider;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Button transferButton;

    private InventorySO source;
    private InventorySO target;
    private int slotIndex;

    public event Action<int> OnTransferFinished;

    private void Start()
    {
        if (amountSlider != null)
        {
            amountSlider.onValueChanged.AddListener(UpdateText);
        }

        if (transferButton != null)
        {
            transferButton.onClick.AddListener(Transfer);
        }
    }

    public void Open(InventorySO sourceInventory, InventorySO targetInventory, int selectedSlot)
    {
        source = sourceInventory;
        target = targetInventory;
        slotIndex = selectedSlot;

        if (source == null || target == null)
        {
            Close();
            return;
        }

        InventoryItem item = source.GetItemAt(slotIndex);
        if (item.IsEmpty)
        {
            Close();
            return;
        }

        bool showAmountControls = item.quantity > 1;
        if (amountSlider != null)
        {
            amountSlider.gameObject.SetActive(showAmountControls);
            amountSlider.minValue = 1;
            amountSlider.maxValue = item.quantity;
            amountSlider.wholeNumbers = true;
            amountSlider.value = 1;
        }

        if (amountText != null)
        {
            amountText.gameObject.SetActive(showAmountControls);
        }

        UpdateText(showAmountControls ? 1 : item.quantity);
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void UpdateText(float value)
    {
        if (amountText != null)
        {
            amountText.text = ((int)value).ToString();
        }
    }

    private void Transfer()
    {
        if (source == null || target == null)
        {
            return;
        }

        int amountToTransfer = amountSlider != null && amountSlider.gameObject.activeSelf
            ? (int)amountSlider.value
            : 1;

        source.TransferItemTo(target, slotIndex, amountToTransfer);
        OnTransferFinished?.Invoke(slotIndex);
    }
}
