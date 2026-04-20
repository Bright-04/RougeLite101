using UnityEngine;

public class Bow : Weapon
{
    [Header("References")]
    public GameObject arrowPrefab;
    public Transform shootPoint;

    private SpriteRenderer mySprite;

    private void Awake()
    {
        mySprite = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        FollowPlayerDirection();
    }

    private void FollowPlayerDirection()
    {
        if (PlayerMovement.Instance == null)
        {
            return;
        }

        Vector2 aimDirection = PlayerMovement.Instance.LastAimDirection;
        if (aimDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

        transform.localRotation = Quaternion.Euler(0, 0, angle) * GetLocalRotationOffset();
        if (weaponDefinition != null)
        {
            // The position offset should rotate relative to the aim direction
            transform.localPosition = Quaternion.Euler(0, 0, angle) * weaponDefinition.LocalPositionOffset;
        }

        if (mySprite != null)
        {
            mySprite.flipY = PlayerMovement.Instance.FacingLeft;
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
