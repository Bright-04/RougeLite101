using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    private const string RuntimePresetVisualName = "PresetRigSprite";

    public float cooldown = 0.5f;
    protected float nextUseTime = 0f;
    protected WeaponDefinitionSO weaponDefinition;

    private bool capturedInitialTransform;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private Vector3 initialLocalScale;
    private SpriteRenderer runtimePresetRenderer;

    protected Vector3 InitialLocalPosition => initialLocalPosition;
    protected Quaternion InitialLocalRotation => initialLocalRotation;
    protected Vector3 InitialLocalScale => initialLocalScale;
    public SpriteRenderer DisplayedSpriteRenderer => runtimePresetRenderer != null && runtimePresetRenderer.enabled
        ? runtimePresetRenderer
        : GetPrimaryRuntimeRenderer();

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

    public void ConfigureRigMode(WeaponRigPointSourceMode rigMode, WeaponDefinitionSO definition)
    {
        CaptureInitialTransform();
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        if (rigMode == WeaponRigPointSourceMode.UsePresetRig)
        {
            EnableNeutralPresetVisual(definition);
            return;
        }

        DisableNeutralPresetVisual();
        transform.localScale = initialLocalScale;
    }

    private void EnableNeutralPresetVisual(WeaponDefinitionSO definition)
    {
        SpriteRenderer sourceRenderer = GetPrimaryRuntimeRenderer();
        EnsureRuntimePresetRenderer();

        if (runtimePresetRenderer == null)
        {
            return;
        }

        Transform presetTransform = runtimePresetRenderer.transform;
        presetTransform.SetParent(GetPresetVisualParent(), false);
        presetTransform.localPosition = Vector3.zero;
        presetTransform.localRotation = Quaternion.identity;
        presetTransform.localScale = Vector3.one;

        runtimePresetRenderer.sprite = definition != null ? definition.ItemImage : null;
        runtimePresetRenderer.color = sourceRenderer != null ? sourceRenderer.color : Color.white;
        runtimePresetRenderer.sharedMaterial = sourceRenderer != null ? sourceRenderer.sharedMaterial : runtimePresetRenderer.sharedMaterial;
        runtimePresetRenderer.sortingLayerID = sourceRenderer != null ? sourceRenderer.sortingLayerID : runtimePresetRenderer.sortingLayerID;
        runtimePresetRenderer.sortingOrder = sourceRenderer != null ? sourceRenderer.sortingOrder : runtimePresetRenderer.sortingOrder;
        runtimePresetRenderer.maskInteraction = sourceRenderer != null ? sourceRenderer.maskInteraction : runtimePresetRenderer.maskInteraction;
        runtimePresetRenderer.enabled = true;
        runtimePresetRenderer.gameObject.SetActive(true);

        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer == runtimePresetRenderer)
            {
                continue;
            }

            renderer.enabled = false;
        }
    }

    private void DisableNeutralPresetVisual()
    {
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer == runtimePresetRenderer)
            {
                continue;
            }

            renderer.enabled = true;
        }

        if (runtimePresetRenderer != null)
        {
            runtimePresetRenderer.enabled = false;
            runtimePresetRenderer.gameObject.SetActive(false);
        }
    }

    private void EnsureRuntimePresetRenderer()
    {
        if (runtimePresetRenderer != null)
        {
            return;
        }

        Transform visualParent = GetPresetVisualParent();
        Transform existing = visualParent.Find(RuntimePresetVisualName);
        if (existing == null)
        {
            GameObject visualObject = new GameObject(RuntimePresetVisualName);
            existing = visualObject.transform;
            existing.SetParent(visualParent, false);
        }

        runtimePresetRenderer = existing.GetComponent<SpriteRenderer>();
        if (runtimePresetRenderer == null)
        {
            runtimePresetRenderer = existing.gameObject.AddComponent<SpriteRenderer>();
        }
    }

    private Transform GetPresetVisualParent()
    {
        return transform.parent != null ? transform.parent : transform;
    }

    private SpriteRenderer GetPrimaryRuntimeRenderer()
    {
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer != null && renderer.transform.name != RuntimePresetVisualName)
            {
                return renderer;
            }
        }

        return null;
    }

    protected Quaternion GetLocalRotationOffset()
    {
        return weaponDefinition != null ? Quaternion.Euler(weaponDefinition.LocalRotationOffset) : Quaternion.identity;
    }

    public virtual bool TryGetPoseAimDirectionOverride(out Vector2 aimDirection)
    {
        aimDirection = default;
        return false;
    }

    public virtual WeaponAlignmentPose AdjustPose(WeaponAlignmentPose pose)
    {
        return pose;
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
