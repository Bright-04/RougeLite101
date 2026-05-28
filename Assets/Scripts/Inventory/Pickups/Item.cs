using UnityEngine;
using System.Collections;
using TMPro;

public class Item : MonoBehaviour
{
    [field: SerializeField]
    public ItemSO InventoryItem { get; set; }

    [field: SerializeField]
    public int Quantity { get; set; } = 1;

    //[SerializeField]
    //private AudioSource audioSource;

    [SerializeField]
    private float duration = 0.3f;

    [SerializeField]
    private float targetSize = 1f;

    [SerializeField]
    private GameObject promptObject;

    [SerializeField]
    private TextMeshPro promptText;

    private SpriteRenderer spriteRenderer;
    private bool hasWarnedMissingPrompt;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        RefreshVisual();

        if (promptObject != null)
        {
            promptObject.SetActive(false);
        }
    }

    public void ShowPrompt(bool show)
    {
        if (promptObject == null)
        {
            if (!hasWarnedMissingPrompt)
            {
                Debug.LogWarning($"Item '{gameObject.name}' is missing promptObject. Pickup prompt will not be shown.", this);
                hasWarnedMissingPrompt = true;
            }
            return;
        }

        promptObject.SetActive(show);
    }

    private void RefreshVisual()
    {
        if (spriteRenderer == null || InventoryItem == null || InventoryItem.ItemImage == null)
        {
            return;
        }

        spriteRenderer.sprite = InventoryItem.ItemImage;
        NormalizeSpriteSize();
    }

    private void NormalizeSpriteSize()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return;
        }

        Bounds bounds = spriteRenderer.sprite.bounds;
        float largestDimension = Mathf.Max(bounds.size.x, bounds.size.y);
        if (largestDimension <= 0.0001f)
        {
            return;
        }

        float scale = targetSize / largestDimension;
        transform.localScale = Vector3.one * scale;
    }

    public void DestroyItem()
    {
        GetComponent<Collider2D>().enabled = false;
        StartCoroutine(AnimateItemPickup());

    }

    private IEnumerator AnimateItemPickup()
    {
        //audioSource.Play();
        Vector3 startScale = transform.localScale;
        Vector3 endScale = Vector3.zero;
        float currentTime = 0;
        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, endScale, currentTime / duration);
            yield return null;
        }
        Destroy(gameObject);
    }
}
