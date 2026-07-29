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

        private static Sprite glowSprite;

        private readonly RectTransform[] particleRects =
            new RectTransform[ParticleCount];
        private readonly Image[] particleImages =
            new Image[ParticleCount];
        private readonly int[] particleSeeds =
            new int[ParticleCount];
        private RectTransform contentRect;
        private Color tint = new(1f, 0.95f, 0.85f, 0.5f);

        public void Initialize(RectTransform backgroundContentRect)
        {
            contentRect = backgroundContentRect;
            EnsureGlowSprite();

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

                particleRects[index] = rect;
                particleImages[index] = image;
                particleSeeds[index] = index * 7919 + 104729;
            }
        }

        public void Show(Color locationTint)
        {
            tint = locationTint;
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
                Color color = tint;
                color.a = tint.a * state.Alpha01;
                particleImages[index].color = color;
            }
        }

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
                    float distance = nx * nx + ny * ny;
                    float falloff = Mathf.Clamp01(1f - distance);
                    byte alpha = (byte)Mathf.RoundToInt(
                        falloff * falloff * 255f);
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
