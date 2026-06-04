using TMPro;
using UnityEngine;

public class MoneyUI : MonoBehaviour
{
    [SerializeField]
    private TMP_Text goldText;

    [SerializeField]
    private PlayerMoney playerMoney;

    private void Start()
    {
        if (playerMoney == null)
        {
            playerMoney = FindFirstObjectByType<PlayerMoney>();
        }

        playerMoney.OnGoldChanged += UpdateUI;

        UpdateUI(playerMoney.Gold);
    }

    private void OnDestroy()
    {
        if (playerMoney != null)
        {
            playerMoney.OnGoldChanged -= UpdateUI;
        }
    }

    private void UpdateUI(int gold)
    {
        goldText.text = $"{FormatGold(gold)} G";
    }

    private string FormatGold(int value)
    {
        if (value >= 1000000000)
            return (value / 1000000000f).ToString("0.#") + "B";

        if (value >= 1000000)
            return (value / 1000000f).ToString("0.#") + "M";

        if (value >= 1000)
            return (value / 1000f).ToString("0.#") + "K";

        return value.ToString();
    }
}
