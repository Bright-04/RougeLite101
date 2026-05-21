using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    public float cooldown = 0.5f;
    protected float nextUseTime = 0f;
    protected WeaponDefinitionSO weaponDefinition;

    private bool capturedInitialTransform;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private Vector3 initialLocalScale;

    protected Vector3 InitialLocalPosition => initialLocalPosition;
    protected Quaternion InitialLocalRotation => initialLocalRotation;
    protected Vector3 InitialLocalScale => initialLocalScale;

    public virtual void Initialize(WeaponDefinitionSO definition)
    {
        CaptureInitialTransform();
        weaponDefinition = definition;
        if (definition != null)
        {
            cooldown = definition.Cooldown;
            ApplyDefinitionSprite(definition);
            ApplyDefinitionTransform(definition);
        }
    }

    private void ApplyDefinitionSprite(WeaponDefinitionSO definition)
    {
        if (definition.ItemImage == null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = definition.ItemImage;
        }
    }

    private void ApplyDefinitionTransform(WeaponDefinitionSO definition)
    {
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = initialLocalScale;
    }

    protected Quaternion GetLocalRotationOffset()
    {
        return weaponDefinition != null ? Quaternion.Euler(weaponDefinition.LocalRotationOffset) : Quaternion.identity;
    }

    protected Vector3 GetVisualPositionOffset()
    {
        return weaponDefinition != null ? weaponDefinition.VisualPositionOffset : Vector3.zero;
    }

    private void CaptureInitialTransform()
    {
        if (capturedInitialTransform)
        {
            return;
        }

        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
        initialLocalScale = transform.localScale;
        capturedInitialTransform = true;
    }

    public abstract void Use();
}
