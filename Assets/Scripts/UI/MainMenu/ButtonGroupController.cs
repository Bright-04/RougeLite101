using UnityEngine;
using UnityEngine.UI;

public class ButtonGroupController : MonoBehaviour
{
    [Header("Buttons & Panels")]
    public Button[] buttons;   // gán 3 nút vào đây
    public GameObject[] panels; // gán 3 panel tương ứng

    [Header("Opacity Settings")]
    public float normalAlpha = 1f;
    public float fadedAlpha = 0.5f;

    private void Awake()
    {
        // Gán sự kiện click cho từng nút
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            buttons[i].onClick.AddListener(() => OnButtonClicked(index));
        }
    }

    private void OnEnable()
    {
        // Mỗi lần panel chính mở -> chọn mặc định nút đầu
        OnButtonClicked(0);
    }

    private void OnButtonClicked(int clickedIndex)
    {
        // Xử lý opacity của các nút
        for (int i = 0; i < buttons.Length; i++)
        {
            Image img = buttons[i].GetComponent<Image>();
            if (i == clickedIndex)
                img.color = new Color(img.color.r, img.color.g, img.color.b, normalAlpha); // sáng
            else
                img.color = new Color(img.color.r, img.color.g, img.color.b, fadedAlpha); // mờ
        }

        // Xử lý bật/tắt panel
        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].SetActive(i == clickedIndex);
        }
    }

}
