using UnityEngine;
using UnityEngine.InputSystem;

public class Bow : Weapon
{
    [Header("References")]
    public GameObject arrowPrefab;
    public Transform shootPoint;

    private SpriteRenderer mySprite;

    private void Awake()
    {
        mySprite = GetComponent<SpriteRenderer>();

        // Đảm bảo Bow được nằm đúng vị trí trong tay nhân vật (nếu bị lệch)
        transform.localPosition = Vector3.zero;
        transform.localScale = Vector3.one;
    }

    private void Update()
    {
        FollowPlayerDirection();
    }

    private void FollowPlayerDirection()
    {
        // 1. Lấy vị trí chuột trên màn hình
        Vector3 mouseScreenPosition = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition);
        mouseWorldPosition.z = 0f; // Gameplay 2D

        // 2. Tính toán vector hướng và góc quay 
        Vector3 direction = (mouseWorldPosition - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 3. Xoay cây cung hướng về chuột
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // 4. Lật sprite (Flip) để lúc nào nỏ/cung cũng đúng chiều
        if (mySprite != null)
        {
            mySprite.flipY = (mouseWorldPosition.x < transform.position.x);
        }
    }

    public override void Use()
    {
        if (Time.time < nextUseTime) return;
        nextUseTime = Time.time + cooldown;
        
        if (arrowPrefab == null || shootPoint == null)
        {
            Debug.LogWarning("Bow: arrowPrefab or shootPoint is null!", this);
            return;
        }

        // Đảm bảo Z=0 để nhìn thấy arrow (fix lỗi hiển thị trong Hierarchy nhưng mất trong Game)
        Vector3 spawnPos = new Vector3(shootPoint.position.x, shootPoint.position.y, 0f);
        GameObject arrow = Instantiate(arrowPrefab, spawnPos, shootPoint.rotation);
        
        // Tự động scale lên nếu quá nhỏ (giữ nguyên logic chữa cháy cũ)
        SpriteRenderer arrowSprite = arrow.GetComponentInChildren<SpriteRenderer>();
        if (arrowSprite != null)
        {
            if (arrow.transform.localScale.magnitude < 0.5f)
            {
                arrow.transform.localScale = new Vector3(2f, 2f, 1f);
            }
            
            // Cập nhật lại SortingLayer để mũi tên luôn nổi giống với Cây Cung
            if (mySprite != null)
            {
                arrowSprite.sortingLayerID = mySprite.sortingLayerID;
                arrowSprite.sortingOrder = mySprite.sortingOrder + 1;
            }
            else
            {
                arrowSprite.sortingOrder = 100; // Backup
            }
        }
    }
}
