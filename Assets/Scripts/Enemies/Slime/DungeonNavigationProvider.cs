using System;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonNavigationProvider : MonoBehaviour
{
    [Header("Walkable Sources")]
    [SerializeField] private Tilemap[] walkableTilemaps;
    [SerializeField] private bool autoDiscoverWalkableTilemaps = true;
    [SerializeField] private string[] walkableNameHints = { "Ground", "Floor" };

    [Header("Collision")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float walkableProbeRadius = 0.12f;

    [Header("Discovery")]
    [SerializeField] private float autoRefreshInterval = 1f;
    [SerializeField] private bool enableDebugLogs = false;

    private readonly Collider2D[] overlapResults = new Collider2D[24];
    private Tilemap[] resolvedWalkableTilemaps = Array.Empty<Tilemap>();
    private Bounds dungeonBounds;
    private float nextRefreshTime;
    private float nextWalkableFailureLogTime;
    private int lastResolvedTilemapCount = -1;

    public bool IsWalkable(Vector3 worldPosition)
    {
        return IsWalkable(worldPosition, null, walkableProbeRadius);
    }

    public bool HasLineOfSight(Vector2 from, Vector2 to)
    {
        EnsureCache();

        float maxDistance = Vector2.Distance(from, to);
        RaycastHit2D[] hits = obstacleMask.value != 0
            ? Physics2D.LinecastAll(from, to, obstacleMask)
            : Physics2D.LinecastAll(from, to);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || hit.collider.isTrigger)
            {
                continue;
            }

            if (hit.distance >= maxDistance - 0.01f)
            {
                continue;
            }

            if (IsActorCollider(hit.collider))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    public bool TryGetNearestWalkablePoint(Vector3 origin, float radius, out Vector3 point)
    {
        EnsureCache();

        if (IsWalkable(origin))
        {
            point = origin;
            return true;
        }

        const int radialSteps = 6;
        const int angularSteps = 12;

        for (int ring = 1; ring <= radialSteps; ring++)
        {
            float ringRadius = radius * (ring / (float)radialSteps);
            for (int i = 0; i < angularSteps; i++)
            {
                float angle = (Mathf.PI * 2f * i) / angularSteps;
                Vector3 candidate = origin + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * ringRadius;
                if (IsWalkable(candidate))
                {
                    point = candidate;
                    return true;
                }
            }
        }

        point = origin;
        return false;
    }

    public bool TryGetRandomWalkablePointNear(Vector3 origin, float radius, int attempts, out Vector3 point)
    {
        EnsureCache();

        for (int i = 0; i < attempts; i++)
        {
            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * radius;
            Vector3 candidate = origin + new Vector3(randomOffset.x, randomOffset.y, 0f);
            if (IsWalkable(candidate))
            {
                point = candidate;
                return true;
            }
        }

        return TryGetNearestWalkablePoint(origin, radius, out point);
    }

    public bool IsWalkable(Vector3 worldPosition, Collider2D ignoredCollider, float probeRadius)
    {
        EnsureCache();

        if (resolvedWalkableTilemaps.Length == 0)
        {
            return true;
        }

        if (!dungeonBounds.Contains(worldPosition))
        {
            LogWalkableFailure("OutsideBounds", worldPosition);
            return false;
        }

        if (!IsOnWalkableTile(worldPosition))
        {
            LogWalkableFailure("NoWalkableTile", worldPosition);
            return false;
        }

        bool blocked = HasBlockingCollider(worldPosition, ignoredCollider, probeRadius);
        if (blocked)
        {
            LogWalkableFailure("BlockingCollider", worldPosition);
        }

        return !blocked;
    }

    private void EnsureCache()
    {
        if (Time.time < nextRefreshTime && resolvedWalkableTilemaps.Length > 0)
        {
            return;
        }

        ResolveWalkableTilemaps();
        RecalculateBounds();
        nextRefreshTime = Time.time + Mathf.Max(0.25f, autoRefreshInterval);
    }

    private void ResolveWalkableTilemaps()
    {
        if (walkableTilemaps != null && walkableTilemaps.Length > 0)
        {
            resolvedWalkableTilemaps = Array.FindAll(walkableTilemaps, map => map != null);
            if (resolvedWalkableTilemaps.Length > 0)
            {
                return;
            }
        }

        if (!autoDiscoverWalkableTilemaps)
        {
            resolvedWalkableTilemaps = Array.Empty<Tilemap>();
            return;
        }

        Tilemap[] discoveredTilemaps = FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        resolvedWalkableTilemaps = Array.FindAll(discoveredTilemaps, IsWalkableTilemapCandidate);
        if (enableDebugLogs && lastResolvedTilemapCount != resolvedWalkableTilemaps.Length)
        {
            lastResolvedTilemapCount = resolvedWalkableTilemaps.Length;
            Debug.Log($"[DungeonNavigationProvider] resolvedWalkableTilemaps={resolvedWalkableTilemaps.Length}", this);
        }
        if (enableDebugLogs && resolvedWalkableTilemaps.Length == 0)
        {
            Debug.LogWarning("[DungeonNavigationProvider] No walkable tilemaps discovered.", this);
        }
    }

    private void RecalculateBounds()
    {
        bool hasBounds = false;
        Bounds combinedBounds = default;

        foreach (Tilemap tilemap in resolvedWalkableTilemaps)
        {
            if (tilemap == null)
            {
                continue;
            }

            Bounds localBounds = tilemap.localBounds;
            Bounds worldBounds = TransformBounds(tilemap.transform.localToWorldMatrix, localBounds);
            if (!hasBounds)
            {
                combinedBounds = worldBounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(worldBounds.min);
                combinedBounds.Encapsulate(worldBounds.max);
            }
        }

        dungeonBounds = hasBounds ? combinedBounds : new Bounds(Vector3.zero, Vector3.one * 10000f);
    }

    private bool IsOnWalkableTile(Vector3 worldPosition)
    {
        foreach (Tilemap tilemap in resolvedWalkableTilemaps)
        {
            if (tilemap == null)
            {
                continue;
            }

            Vector3Int cell = tilemap.WorldToCell(worldPosition);
            if (tilemap.HasTile(cell))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasBlockingCollider(Vector3 worldPosition, Collider2D ignoredCollider, float probeRadius)
    {
        int hitCount = obstacleMask.value != 0
            ? Physics2D.OverlapCircle(worldPosition, probeRadius, new ContactFilter2D { useLayerMask = true, layerMask = obstacleMask, useTriggers = false }, overlapResults)
            : Physics2D.OverlapCircle(worldPosition, probeRadius, new ContactFilter2D { useTriggers = false }, overlapResults);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = overlapResults[i];
            if (hit == null || hit == ignoredCollider || hit.isTrigger)
            {
                continue;
            }

            if (ignoredCollider != null && hit.attachedRigidbody != null && ignoredCollider.attachedRigidbody == hit.attachedRigidbody)
            {
                continue;
            }

            if (IsActorCollider(hit))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool IsWalkableTilemapCandidate(Tilemap tilemap)
    {
        if (tilemap == null)
        {
            return false;
        }

        string tilemapName = tilemap.name;
        foreach (string hint in walkableNameHints)
        {
            if (!string.IsNullOrWhiteSpace(hint) && tilemapName.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsActorCollider(Collider2D collider)
    {
        if (collider == null)
        {
            return false;
        }

        if (collider.GetComponentInParent<PlayerMovement>() != null)
        {
            return true;
        }

        if (collider.GetComponentInParent<SlimeAI>() != null)
        {
            return true;
        }

        Rigidbody2D body = collider.attachedRigidbody;
        return body != null && body.bodyType == RigidbodyType2D.Dynamic && collider.GetComponent<TilemapCollider2D>() == null && collider.GetComponent<CompositeCollider2D>() == null;
    }

    private static Bounds TransformBounds(Matrix4x4 matrix, Bounds bounds)
    {
        Vector3 center = matrix.MultiplyPoint3x4(bounds.center);
        Vector3 extents = bounds.extents;
        Vector3 axisX = matrix.MultiplyVector(new Vector3(extents.x, 0f, 0f));
        Vector3 axisY = matrix.MultiplyVector(new Vector3(0f, extents.y, 0f));
        Vector3 axisZ = matrix.MultiplyVector(new Vector3(0f, 0f, extents.z));
        Vector3 worldExtents = new Vector3(
            Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
            Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
            Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z));
        return new Bounds(center, worldExtents * 2f);
    }

    private void LogWalkableFailure(string reason, Vector3 worldPosition)
    {
        if (!enableDebugLogs || Time.time < nextWalkableFailureLogTime)
        {
            return;
        }

        nextWalkableFailureLogTime = Time.time + 0.5f;
        Debug.Log($"[DungeonNavigationProvider] IsWalkable=false reason={reason} pos={worldPosition:F2}", this);
    }
}
