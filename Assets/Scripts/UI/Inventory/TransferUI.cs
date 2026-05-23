using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class TransferUI : MonoBehaviour
{
    [SerializeField] private Slider amountSlider;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Button transferButton;

    private InventorySO source;
    private InventorySO target;
    private int slotIndex;

    public Action<int> OnTransferFinished;

    private void Start()
    {
        amountSlider.onValueChanged.AddListener(UpdateText);
        transferButton.onClick.AddListener(Transfer);
    }

    public void Open(InventorySO sourceInv, InventorySO targetInv,int selectedSlot)
    {

        source = sourceInv;
        target = targetInv;
        slotIndex = selectedSlot;

        InventoryItem item = source.GetItemAt(slotIndex);
        // item hết -> đóng panel
        if (item.IsEmpty)
        {
            Close();
            return;
        }
        bool showAmountUI = item.quantity > 1;

        amountSlider.gameObject.SetActive(showAmountUI);
        amountText.gameObject.SetActive(showAmountUI);

        if (showAmountUI)
        {
            amountSlider.minValue = 1;
            amountSlider.maxValue = item.quantity;
            amountSlider.wholeNumbers = true;
            amountSlider.value = 1;

            UpdateText(1);
        }

        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void UpdateText(float value)
    {
        amountText.text = ((int)value).ToString();
    }

    private void Transfer()
    {
        source.TransferItemTo(target, slotIndex, (int)amountSlider.value);

        // refresh description + giữ select
        OnTransferFinished?.Invoke(slotIndex);
    }
}