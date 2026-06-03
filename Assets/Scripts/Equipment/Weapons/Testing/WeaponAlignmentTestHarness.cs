using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public sealed class WeaponAlignmentTestHarness : MonoBehaviour
{
    [SerializeField] private WeaponDefinitionSO selectedWeapon;
    [SerializeField] private WeaponDefinitionSO secondaryWeapon;
    [SerializeField] private float previewRadius = 1.25f;
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private bool showSpritePreviews = true;

    private static readonly Vector2[] AimDirections =
    {
        Vector2.right,
        new Vector2(1f, 1f).normalized,
        Vector2.up,
        new Vector2(-1f, 1f).normalized,
        Vector2.left,
        new Vector2(-1f, -1f).normalized,
        Vector2.down,
        new Vector2(1f, -1f).normalized
    };

    public WeaponDefinitionSO SelectedWeapon
    {
        get => selectedWeapon;
        set => selectedWeapon = value;
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorApplication.delayCall += () => { if (this != null) SyncSpritePreviews(); };
        }
        else
        {
            SyncSpritePreviews();
        }
#else
        SyncSpritePreviews();
#endif
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorApplication.delayCall += () => { if (this != null) SyncSpritePreviews(); };
        }
        else
        {
            SyncSpritePreviews();
        }
#else
        SyncSpritePreviews();
#endif
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            SyncSpritePreviews();
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos || selectedWeapon == null)
        {
            return;
        }

        WeaponRig rig = selectedWeapon.WeaponPrefab != null
            ? selectedWeapon.WeaponPrefab.GetComponentInChildren<WeaponRig>(true)
            : null;

        Vector3 center = transform.position;
        for (int i = 0; i < AimDirections.Length; i++)
        {
            Vector2 aim = AimDirections[i];
            Vector3 anchor = center + (Vector3)(aim * previewRadius);
            WeaponAlignmentPose pose = WeaponAlignmentUtility.CalculateWeaponPose(anchor, aim, selectedWeapon, rig);
            DrawPose(pose);
        }
    }

    private void SyncSpritePreviews()
    {
        if (!showSpritePreviews)
        {
            return;
        }

        SyncWeaponPreviewSet(selectedWeapon, "Selected", Vector3.zero);
        SyncWeaponPreviewSet(secondaryWeapon, "Secondary", Vector3.down * previewRadius * 2.25f);
    }

    private void SyncWeaponPreviewSet(WeaponDefinitionSO definition, string groupName, Vector3 groupOffset)
    {
        Transform group = EnsureChild(transform, groupName);
        group.localPosition = groupOffset;

        for (int i = 0; i < AimDirections.Length; i++)
        {
            Transform slot = EnsureChild(group, $"Aim_{i}");
            SpriteRenderer renderer = slot.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = slot.gameObject.AddComponent<SpriteRenderer>();
            }

            if (definition == null || definition.ItemImage == null)
            {
                renderer.sprite = null;
                continue;
            }

            WeaponRig rig = definition.WeaponPrefab != null
                ? definition.WeaponPrefab.GetComponentInChildren<WeaponRig>(true)
                : null;

            Vector2 aim = AimDirections[i];
            Vector3 anchor = group.position + (Vector3)(aim * previewRadius);
            WeaponAlignmentPose pose = WeaponAlignmentUtility.CalculateWeaponPose(anchor, aim, definition, rig);
            renderer.sprite = definition.ItemImage;
            renderer.enabled = true;
            renderer.sortingOrder = 20;
            slot.position = pose.WeaponPosition;
            slot.rotation = pose.WeaponRotation;
            slot.localScale = pose.VisualScale;
        }
    }

    private static Transform EnsureChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            return child;
        }

        GameObject childObject = new GameObject(childName);
        child = childObject.transform;
        child.SetParent(parent, false);
        return child;
    }

    private static void DrawPose(WeaponAlignmentPose pose)
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(pose.WeaponAnchorPosition, 0.04f);
        Gizmos.DrawLine(pose.WeaponAnchorPosition, pose.WeaponAnchorPosition + (Vector3)(pose.AimDirection * 0.35f));

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pose.GripPoint, 0.035f);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(pose.MuzzleTipPoint, 0.035f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pose.ProjectileSpawnPoint, 0.03f);

        Gizmos.color = new Color(1f, 0.45f, 0.1f);
        Gizmos.DrawWireSphere(pose.SlashOrigin, 0.03f);
        DrawSlashArc(pose);
    }

    private static void DrawSlashArc(WeaponAlignmentPose pose)
    {
        Vector3 startOffset = pose.SlashArcStart - pose.SlashOrigin;
        Vector3 endOffset = pose.SlashArcEnd - pose.SlashOrigin;
        if (startOffset.sqrMagnitude < 0.0001f || endOffset.sqrMagnitude < 0.0001f)
        {
            Gizmos.DrawLine(pose.SlashArcStart, pose.SlashArcEnd);
            return;
        }

        const int segments = 18;
        Vector3 previous = pose.SlashArcStart;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 point = pose.SlashOrigin + Vector3.Slerp(startOffset, endOffset, t);
            Gizmos.DrawLine(previous, point);
            previous = point;
        }
    }
}
