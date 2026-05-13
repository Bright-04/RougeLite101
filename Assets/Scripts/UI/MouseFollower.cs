using UnityEngine;
using UnityEngine.InputSystem;

public class MouseFollower : MonoBehaviour
{
    [SerializeField]
    private Canvas canvas;
    [SerializeField]
    private Camera mainCam;
    [SerializeField]
    private InventoryItemUI item;

    public void Awake()
    {
        canvas = transform.root.GetComponent<Canvas>();
        mainCam = Camera.main;
        item = GetComponentInChildren<InventoryItemUI>();
    }

    public void SetData(Sprite sprite, int quantity)
    {
        item.SetData(sprite, quantity);
    }
    void Update()
    {
        //Vector2 position;
        //RectTransformUtility.ScreenPointToLocalPointInRectangle(
        //    (RectTransform)canvas.transform,
        //    Input.mousePosition,
        //    null,
        //    out position);
        //transform.position = canvas.transform.TransformPoint(position);
        transform.position = Mouse.current.position.ReadValue();
    }

    public void Toggle(bool val)
    {
        Debug.Log($"Item toggled {val}");
        gameObject.SetActive(val);
    }
}
