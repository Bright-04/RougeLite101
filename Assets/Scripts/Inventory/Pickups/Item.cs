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

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = InventoryItem.ItemImage;
        NormalizeSpriteSize();

        promptObject.SetActive(false);
    }

    public void ShowPrompt(bool show)
    {
        if (show)
        {
            promptText.text = $"[F] Pick Up item";
        }

        promptObject.SetActive(show);
    }

    public void InformInventoryIsFull()
    {
        promptText.text = $"Inventory is Full";
    }


    private void NormalizeSpriteSize()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return;
        }

        Bounds bounds = spriteRenderer.sprite.bounds;

        float largestDimension = Mathf.Max(bounds.size.x, bounds.size.y);

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
