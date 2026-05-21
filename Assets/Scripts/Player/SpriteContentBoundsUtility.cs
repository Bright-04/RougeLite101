using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum SpriteBoundsCoordinateSpace
{
    FullSpriteBounds = 0,
    OpaqueContentBounds = 1
}

public readonly struct SpriteOpaqueBoundsAnalysis
{
    public readonly RectInt FullSpriteRectPixels;
    public readonly RectInt OpaquePixelBounds;
    public readonly Bounds FullLocalBounds;
    public readonly Bounds OpaqueLocalBounds;
    public readonly int OpaquePixelCount;
    public readonly bool HasOpaquePixels;
    public readonly bool UsedFallbackBounds;
    public readonly string Warning;

    public SpriteOpaqueBoundsAnalysis(
        RectInt fullSpriteRectPixels,
        RectInt opaquePixelBounds,
        Bounds fullLocalBounds,
        Bounds opaqueLocalBounds,
        int opaquePixelCount,
        bool hasOpaquePixels,
        bool usedFallbackBounds,
        string warning)
    {
        FullSpriteRectPixels = fullSpriteRectPixels;
        OpaquePixelBounds = opaquePixelBounds;
        FullLocalBounds = fullLocalBounds;
        OpaqueLocalBounds = opaqueLocalBounds;
        OpaquePixelCount = opaquePixelCount;
        HasOpaquePixels = hasOpaquePixels;
        UsedFallbackBounds = usedFallbackBounds;
        Warning = warning;
    }

    public float Coverage01 => FullSpriteRectPixels.width <= 0 || FullSpriteRectPixels.height <= 0
        ? 0f
        : Mathf.Clamp01(OpaquePixelCount / (float)(FullSpriteRectPixels.width * FullSpriteRectPixels.height));

    public float OpaqueBoundsAreaRatio01 => FullSpriteRectPixels.width <= 0 || FullSpriteRectPixels.height <= 0 || !HasOpaquePixels
        ? 0f
        : Mathf.Clamp01((OpaquePixelBounds.width * OpaquePixelBounds.height) / (float)(FullSpriteRectPixels.width * FullSpriteRectPixels.height));

    public Bounds GetLocalBounds(SpriteBoundsCoordinateSpace coordinateSpace)
    {
        return coordinateSpace == SpriteBoundsCoordinateSpace.OpaqueContentBounds && HasOpaquePixels
            ? OpaqueLocalBounds
            : FullLocalBounds;
    }
}

public static class SpriteContentBoundsUtility
{
    private static readonly Dictionary<int, SpriteOpaqueBoundsAnalysis> AnalysisCache = new();
    private static readonly Color32[] EmptyPixels = new Color32[0];

    public static SpriteOpaqueBoundsAnalysis Analyze(Sprite sprite)
    {
        if (sprite == null)
        {
            return default;
        }

        int key = sprite.GetInstanceID();
        if (AnalysisCache.TryGetValue(key, out SpriteOpaqueBoundsAnalysis cached))
        {
            return cached;
        }

        SpriteOpaqueBoundsAnalysis analysis = BuildAnalysis(sprite);
        AnalysisCache[key] = analysis;
        return analysis;
    }

    public static bool TryGetLocalBounds(Sprite sprite, SpriteBoundsCoordinateSpace coordinateSpace, out Bounds localBounds, out string warning)
    {
        warning = null;
        localBounds = default;
        if (sprite == null)
        {
            return false;
        }

        SpriteOpaqueBoundsAnalysis analysis = Analyze(sprite);
        localBounds = analysis.GetLocalBounds(coordinateSpace);
        warning = analysis.UsedFallbackBounds ? analysis.Warning : null;
        return true;
    }

    private static SpriteOpaqueBoundsAnalysis BuildAnalysis(Sprite sprite)
    {
        RectInt fullRect = GetSpritePixelRect(sprite);
        Bounds fullLocalBounds = BuildLocalBounds(sprite, new RectInt(0, 0, fullRect.width, fullRect.height));

        if (!TryGetSpritePixels(sprite, out Color32[] pixels, out int width, out int height, out string warning))
        {
            return new SpriteOpaqueBoundsAnalysis(
                fullRect,
                new RectInt(0, 0, fullRect.width, fullRect.height),
                fullLocalBounds,
                fullLocalBounds,
                0,
                false,
                true,
                warning);
        }

        if (pixels == null || pixels.Length == 0 || width <= 0 || height <= 0)
        {
            return new SpriteOpaqueBoundsAnalysis(
                fullRect,
                new RectInt(0, 0, fullRect.width, fullRect.height),
                fullLocalBounds,
                fullLocalBounds,
                0,
                false,
                true,
                "Sprite texture pixel data was unavailable. Falling back to full sprite bounds.");
        }

        int minX = width;
        int minY = height;
        int maxX = -1;
        int maxY = -1;
        int opaquePixelCount = 0;

        for (int y = 0; y < height; y++)
        {
            int rowStart = y * width;
            for (int x = 0; x < width; x++)
            {
                if (pixels[rowStart + x].a <= 0)
                {
                    continue;
                }

                opaquePixelCount++;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
        }

        if (opaquePixelCount <= 0 || maxX < minX || maxY < minY)
        {
            return new SpriteOpaqueBoundsAnalysis(
                fullRect,
                new RectInt(0, 0, fullRect.width, fullRect.height),
                fullLocalBounds,
                fullLocalBounds,
                0,
                false,
                false,
                "Sprite contains no opaque pixels. Using full sprite bounds.");
        }

        RectInt opaqueRect = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        Bounds opaqueLocalBounds = BuildLocalBounds(sprite, opaqueRect);
        return new SpriteOpaqueBoundsAnalysis(
            fullRect,
            opaqueRect,
            fullLocalBounds,
            opaqueLocalBounds,
            opaquePixelCount,
            true,
            false,
            warning);
    }

    private static RectInt GetSpritePixelRect(Sprite sprite)
    {
        Rect rect = sprite.rect;
        return new RectInt(
            Mathf.RoundToInt(rect.x),
            Mathf.RoundToInt(rect.y),
            Mathf.RoundToInt(rect.width),
            Mathf.RoundToInt(rect.height));
    }

    private static Bounds BuildLocalBounds(Sprite sprite, RectInt pixelBounds)
    {
        float pixelsPerUnit = Mathf.Max(0.0001f, sprite.pixelsPerUnit);
        Vector2 pivot = sprite.pivot;

        Vector2 min = new Vector2(
            (pixelBounds.xMin - pivot.x) / pixelsPerUnit,
            (pixelBounds.yMin - pivot.y) / pixelsPerUnit);
        Vector2 max = new Vector2(
            (pixelBounds.xMax - pivot.x) / pixelsPerUnit,
            (pixelBounds.yMax - pivot.y) / pixelsPerUnit);

        Vector3 min3 = new Vector3(min.x, min.y, 0f);
        Vector3 max3 = new Vector3(max.x, max.y, 0f);
        Bounds bounds = new Bounds((min3 + max3) * 0.5f, max3 - min3);
        if (bounds.size.x <= 0f)
        {
            bounds.size = new Vector3(1f / pixelsPerUnit, bounds.size.y, bounds.size.z);
        }

        if (bounds.size.y <= 0f)
        {
            bounds.size = new Vector3(bounds.size.x, 1f / pixelsPerUnit, bounds.size.z);
        }

        return bounds;
    }

    private static bool TryGetSpritePixels(Sprite sprite, out Color32[] pixels, out int width, out int height, out string warning)
    {
        if (sprite.texture != null && sprite.texture.isReadable)
        {
            Rect rect = sprite.rect;
            int x = Mathf.RoundToInt(rect.x);
            int y = Mathf.RoundToInt(rect.y);
            width = Mathf.RoundToInt(rect.width);
            height = Mathf.RoundToInt(rect.height);
            pixels = ExtractRegion(sprite.texture.GetPixels32(), sprite.texture.width, x, y, width, height);
            warning = null;
            return true;
        }

#if UNITY_EDITOR
        string texturePath = AssetDatabase.GetAssetPath(sprite.texture);
        if (string.IsNullOrEmpty(texturePath))
        {
            texturePath = AssetDatabase.GetAssetPath(sprite);
        }

        if (!string.IsNullOrEmpty(texturePath))
        {
            string absolutePath = Path.GetFullPath(texturePath);
            if (File.Exists(absolutePath))
            {
                byte[] bytes = File.ReadAllBytes(absolutePath);
                Texture2D decodedTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (ImageConversion.LoadImage(decodedTexture, bytes, false))
                {
                    Rect rect = sprite.rect;
                    int x = Mathf.RoundToInt(rect.x);
                    int y = Mathf.RoundToInt(rect.y);
                    width = Mathf.RoundToInt(rect.width);
                    height = Mathf.RoundToInt(rect.height);
                    pixels = ExtractRegion(decodedTexture.GetPixels32(), decodedTexture.width, x, y, width, height);
                    warning = "Sprite texture is not readable in Unity import settings. Editor preview decoded the source texture file instead.";
                    Object.DestroyImmediate(decodedTexture);
                    return true;
                }

                Object.DestroyImmediate(decodedTexture);
            }
        }
#endif

        pixels = null;
        width = 0;
        height = 0;
        warning = "Sprite texture is not readable. Falling back to full sprite bounds.";
        return false;
    }

    private static Color32[] ExtractRegion(Color32[] sourcePixels, int textureWidth, int startX, int startY, int width, int height)
    {
        if (sourcePixels == null || sourcePixels.Length == 0 || textureWidth <= 0 || width <= 0 || height <= 0)
        {
            return EmptyPixels;
        }

        Color32[] region = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            int sourceRowStart = (startY + y) * textureWidth + startX;
            int destinationRowStart = y * width;
            for (int x = 0; x < width; x++)
            {
                region[destinationRowStart + x] = sourcePixels[sourceRowStart + x];
            }
        }

        return region;
    }
}
