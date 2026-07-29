using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wake.Exploration
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    public sealed class AlphaContourRaycastFilter :
        MonoBehaviour,
        ICanvasRaycastFilter
    {
        private const int MaskResolution = 512;
        private const byte DefaultAlphaThreshold = 18;

        private sealed class AlphaMask
        {
            public int Width;
            public int Height;
            public byte[] Alpha;
        }

        private static readonly Dictionary<int, AlphaMask> MaskCache = new();

        private RawImage image;
        private AlphaMask mask;
        private byte alphaThreshold = DefaultAlphaThreshold;

        public bool HasAlphaMask => mask?.Alpha != null;

        public void Configure(
            RawImage sourceImage,
            Texture sourceTexture,
            float threshold = DefaultAlphaThreshold / 255f)
        {
            image = sourceImage != null
                ? sourceImage
                : GetComponent<RawImage>();
            alphaThreshold = (byte)Mathf.Clamp(
                Mathf.RoundToInt(threshold * 255f),
                1,
                254);
            mask = GetOrCreateMask(sourceTexture);
        }

        public bool IsRaycastLocationValid(
            Vector2 screenPoint,
            Camera eventCamera)
        {
            image ??= GetComponent<RawImage>();
            RectTransform rectTransform = image?.rectTransform;
            if (rectTransform == null ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform,
                    screenPoint,
                    eventCamera,
                    out Vector2 localPoint))
            {
                return false;
            }

            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f)
                return false;

            float normalizedX =
                localPoint.x / rect.width + rectTransform.pivot.x;
            float normalizedY =
                localPoint.y / rect.height + rectTransform.pivot.y;
            if (normalizedX < 0f || normalizedX > 1f ||
                normalizedY < 0f || normalizedY > 1f)
            {
                return false;
            }

            if (!HasAlphaMask)
                return IsInsideFallbackSilhouette(
                    normalizedX,
                    normalizedY);

            Rect uvRect = image.uvRect;
            float textureU = uvRect.x + normalizedX * uvRect.width;
            float textureV = uvRect.y + normalizedY * uvRect.height;
            int x = Mathf.Clamp(
                Mathf.RoundToInt(textureU * (mask.Width - 1)),
                0,
                mask.Width - 1);
            int y = Mathf.Clamp(
                Mathf.RoundToInt(textureV * (mask.Height - 1)),
                0,
                mask.Height - 1);

            for (int offsetY = -1; offsetY <= 1; offsetY++)
            {
                int sampleY = Mathf.Clamp(y + offsetY, 0, mask.Height - 1);
                for (int offsetX = -1; offsetX <= 1; offsetX++)
                {
                    int sampleX = Mathf.Clamp(
                        x + offsetX,
                        0,
                        mask.Width - 1);
                    if (mask.Alpha[sampleY * mask.Width + sampleX] >=
                        alphaThreshold)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static AlphaMask GetOrCreateMask(Texture texture)
        {
            if (texture == null)
                return null;

            int key = texture.GetInstanceID();
            if (MaskCache.TryGetValue(key, out AlphaMask cached))
                return cached;

            AlphaMask created = CreateMask(texture);
            if (created != null)
                MaskCache[key] = created;
            return created;
        }

        private static AlphaMask CreateMask(Texture texture)
        {
            RenderTexture renderTexture = null;
            Texture2D readback = null;
            RenderTexture previous = RenderTexture.active;
            try
            {
                renderTexture = RenderTexture.GetTemporary(
                    MaskResolution,
                    MaskResolution,
                    0,
                    RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Linear);
                Graphics.Blit(texture, renderTexture);
                RenderTexture.active = renderTexture;
                readback = new Texture2D(
                    MaskResolution,
                    MaskResolution,
                    TextureFormat.RGBA32,
                    false,
                    true);
                readback.ReadPixels(
                    new Rect(0f, 0f, MaskResolution, MaskResolution),
                    0,
                    0,
                    false);
                readback.Apply(false, false);

                Color32[] pixels = readback.GetPixels32();
                var alpha = new byte[pixels.Length];
                for (int index = 0; index < pixels.Length; index++)
                    alpha[index] = pixels[index].a;

                return new AlphaMask
                {
                    Width = MaskResolution,
                    Height = MaskResolution,
                    Alpha = alpha
                };
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning(
                    $"Could not build character alpha hit mask for " +
                    $"{texture.name}: {exception.Message}");
                return null;
            }
            finally
            {
                RenderTexture.active = previous;
                if (renderTexture != null)
                    RenderTexture.ReleaseTemporary(renderTexture);
                if (readback != null)
                {
                    if (Application.isPlaying)
                        Destroy(readback);
                    else
                        DestroyImmediate(readback);
                }
            }
        }

        private static bool IsInsideFallbackSilhouette(float x, float y)
        {
            float centeredX = Mathf.Abs(x - 0.5f);
            if (y >= 0.72f)
            {
                float headX = (x - 0.5f) / 0.28f;
                float headY = (y - 0.85f) / 0.16f;
                return headX * headX + headY * headY <= 1f;
            }

            if (y >= 0.2f)
            {
                float halfWidth = Mathf.Lerp(0.2f, 0.35f, y / 0.72f);
                return centeredX <= halfWidth;
            }

            return Mathf.Abs(x - 0.39f) <= 0.14f ||
                   Mathf.Abs(x - 0.61f) <= 0.14f;
        }
    }
}
