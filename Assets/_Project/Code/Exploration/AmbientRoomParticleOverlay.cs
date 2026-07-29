using UnityEngine;
using UnityEngine.UI;

namespace Wake.Exploration
{
    [DisallowMultipleComponent]
    public sealed class AmbientRoomParticleOverlay : MonoBehaviour
    {
        private const int ParticleCount = 16;
        private const float MinSizePx = 6f;
        private const float MaxSizePx = 18f;
        private const int GlowTextureSize = 32;
        private const int SampleGridSize = 24;
        private const float BackgroundSaturationFactor = 0.5f;

        private static Sprite glowSprite;
        private static Material additiveMaterial;

        private readonly RectTransform[] particleRects =
            new RectTransform[ParticleCount];
        private readonly Image[] particleImages =
            new Image[ParticleCount];
        private readonly int[] particleSeeds =
            new int[ParticleCount];
        private RectTransform contentRect;
        private Color tint = new(1f, 0.95f, 0.85f, 0.5f);
        private Texture2D backgroundSampleCache;

        public void Initialize(RectTransform backgroundContentRect)
        {
            contentRect = backgroundContentRect;
            EnsureGlowSprite();
            EnsureAdditiveMaterial();

            for (int index = 0; index < ParticleCount; index++)
            {
                GameObject particle = new(
                    $"AmbientParticle_{index}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                particle.transform.SetParent(contentRect, false);

                RectTransform rect = particle.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                float size = Mathf.Lerp(
                    MinSizePx,
                    MaxSizePx,
                    (index + 0.5f) / ParticleCount);
                rect.sizeDelta = new Vector2(size, size);

                Image image = particle.GetComponent<Image>();
                image.sprite = glowSprite;
                image.raycastTarget = false;
                image.color = tint;
                if (additiveMaterial != null)
                {
                    image.material = additiveMaterial;
                }

                particleRects[index] = rect;
                particleImages[index] = image;
                particleSeeds[index] = index * 7919 + 104729;
            }
        }

        public void Show(Color locationTint, Sprite backgroundSprite)
        {
            tint = locationTint;
            CaptureBackgroundSample(backgroundSprite);
            for (int index = 0; index < particleImages.Length; index++)
            {
                if (particleImages[index] != null)
                {
                    particleImages[index].color = tint;
                }
            }
        }

        private void Update()
        {
            if (contentRect == null || particleRects[0] == null)
            {
                return;
            }

            Rect bounds = contentRect.rect;
            if (bounds.width <= 0f || bounds.height <= 0f)
            {
                return;
            }

            float time = Time.time;
            for (int index = 0; index < ParticleCount; index++)
            {
                AmbientParticleState state = AmbientRoomParticleDrift.Evaluate(
                    particleSeeds[index],
                    time,
                    bounds);
                particleRects[index].anchoredPosition = state.Position;

                Color backgroundColor = Color.white;
                if (backgroundSampleCache != null)
                {
                    float u = Mathf.InverseLerp(
                        bounds.xMin, bounds.xMax, state.Position.x);
                    float v = Mathf.InverseLerp(
                        bounds.yMin, bounds.yMax, state.Position.y);
                    backgroundColor = NormalizeForGlow(
                        backgroundSampleCache.GetPixelBilinear(u, v));
                }

                Color color = tint * backgroundColor;
                color.a = tint.a * state.Alpha01;
                particleImages[index].color = color;
            }
        }

        // Renders the background sprite into a small readable texture once
        // per location change (background sprites aren't Read/Write enabled,
        // so a GPU blit + ReadPixels is the only way to sample their pixels)
        // and caches it for per-particle sampling every frame in Update.
        private void CaptureBackgroundSample(Sprite sprite)
        {
            if (backgroundSampleCache != null)
            {
                Destroy(backgroundSampleCache);
                backgroundSampleCache = null;
            }

            if (sprite == null || sprite.texture == null)
            {
                return;
            }

            Texture2D source = sprite.texture;
            Rect texRect = sprite.textureRect;
            Vector2 scale = new(
                texRect.width / source.width,
                texRect.height / source.height);
            Vector2 offset = new(
                texRect.x / source.width,
                texRect.y / source.height);

            RenderTexture renderTexture = RenderTexture.GetTemporary(
                SampleGridSize, SampleGridSize, 0, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(source, renderTexture, scale, offset);
            RenderTexture.active = renderTexture;

            backgroundSampleCache = new Texture2D(
                SampleGridSize, SampleGridSize, TextureFormat.RGBA32, false)
            {
                name = "Ambient Particle Background Sample",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            backgroundSampleCache.ReadPixels(
                new Rect(0f, 0f, SampleGridSize, SampleGridSize), 0, 0);
            backgroundSampleCache.Apply(false, false);

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
        }

        // Multiplying the raw sampled background color into the particle
        // crushed brightness to near-invisible: photographic backgrounds
        // are rarely near-white, and that darkness compounded with the
        // twinkle alpha and the additive shader's alpha-premultiply (three
        // multiplicative darkening factors stacked). Keep the background's
        // hue (and a softened saturation) so particles still pick up local
        // room color, but force it back up to full brightness first.
        private static Color NormalizeForGlow(Color color)
        {
            Color.RGBToHSV(color, out float hue, out float saturation, out _);
            return Color.HSVToRGB(
                hue, saturation * BackgroundSaturationFactor, 1f);
        }

        private void OnDestroy()
        {
            if (backgroundSampleCache != null)
            {
                Destroy(backgroundSampleCache);
                backgroundSampleCache = null;
            }
        }

        private static void EnsureAdditiveMaterial()
        {
            if (additiveMaterial != null)
            {
                return;
            }

            Shader shader = Resources.Load<Shader>("Shaders/UIAdditiveGlow");
            if (shader == null)
            {
                return;
            }

            additiveMaterial = new Material(shader)
            {
                name = "Ambient Particle Additive (Runtime)",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        // Bright, tight core with a soft extended tail rather than a flat
        // disc, so the particle reads as a glow even without real bloom.
        private static void EnsureGlowSprite()
        {
            if (glowSprite != null)
            {
                return;
            }

            var texture = new Texture2D(
                GlowTextureSize,
                GlowTextureSize,
                TextureFormat.RGBA32,
                false)
            {
                name = "Ambient Particle Glow",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[GlowTextureSize * GlowTextureSize];
            for (int y = 0; y < GlowTextureSize; y++)
            {
                float ny = (y + 0.5f) / GlowTextureSize * 2f - 1f;
                for (int x = 0; x < GlowTextureSize; x++)
                {
                    float nx = (x + 0.5f) / GlowTextureSize * 2f - 1f;
                    float radius = Mathf.Sqrt(nx * nx + ny * ny);
                    float falloff = Mathf.Clamp01(1f - radius);
                    byte alpha = (byte)Mathf.RoundToInt(
                        Mathf.Pow(falloff, 1.8f) * 255f);
                    pixels[y * GlowTextureSize + x] =
                        new Color32(255, 255, 255, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            glowSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, GlowTextureSize, GlowTextureSize),
                new Vector2(0.5f, 0.5f));
            glowSprite.name = "Ambient Particle Glow Sprite";
        }
    }
}
