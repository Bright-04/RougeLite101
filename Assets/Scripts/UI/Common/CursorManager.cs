using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [SerializeField] private Texture2D cursorTexture;
    [SerializeField] private Vector2 hotspot = Vector2.zero;

    private void Start()
    {
        SetCustomCursor();
    }

    public void SetCustomCursor()
    {
        if (cursorTexture != null)
        {
            // Nếu bạn muốn tâm con trỏ nằm ở CHÍNH GIỮA hình (như tâm ngắm), hãy dùng:
            // hotspot = new Vector2(cursorTexture.width / 2, cursorTexture.height / 2);
            
            Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
        }
        else
        {
            Debug.LogWarning("CursorManager: Chưa gán hình ảnh Cursor Texture!");
        }
    }

    // Nếu sau này bạn muốn hiện/ẩn chuột (ví dụ khi dùng Controller), bạn có thể gọi hàm này
    public void SetCursorVisibility(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Confined;
    }
}
