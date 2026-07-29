using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Wake.Exploration
{
    // Real bloom requires camera post-processing, but Screen Space - Overlay
    // canvases (which the room background/hotspots use) always render on
    // top of every camera's output, with no way to interleave them - a
    // camera-rendered particle layer would just get hidden underneath the
    // opaque background image. So the particles are rendered by a small
    // offscreen camera (with its own local Bloom volume, isolated to a
    // dedicated layer/culling mask) into a RenderTexture, and that texture
    // is displayed via one additive-blended RawImage inside the existing
    // Overlay hierarchy - from the Overlay canvas's point of view it's just
    // another texture, so none of the existing background/hotspot layering
    // changes.
    [DisallowMultipleComponent]
    public sealed class AmbientRoomParticleOverlay : MonoBehaviour
    {
        private const int ParticleCount = 16;
        private const float MinSizePx = 6f;
        private const float MaxSizePx = 18f;
        private const int GlowTextureSize = 32;
        private const int SampleGridSize = 24;
        private const float BackgroundSaturationFactor = 0.5f;
        private const int BloomTextureSize = 512;
        private const string ParticleLayerName = "AmbientParticles";

        private static Sprite glowSprite;
        private static Material additiveMaterial;

        private readonly RectTransform[] particleRects =
            new RectTransform[ParticleCount];
        private readonly Image[] particleImages =
            new Image[ParticleCount];
        private readonly int[] particleSeeds =
            new int[ParticleCount];
        private RectTransform contentRect;
        private RectTransform particleCanvasRect;
        private Color tint = new(1f, 0.95f, 0.85f, 0.5f);
        private Texture2D backgroundSampleCache;

        private RenderTexture bloomTexture;
        private GameObject bloomCameraObject;
        private GameObject bloomVolumeObject;
        private VolumeProfile bloomProfile;
        private GameObject particleCanvasObject;
        private GameObject compositeObject;

        public void Initialize(RectTransform backgroundContentRect)
        {
            contentRect = backgroundContentRect;
            EnsureGlowSprite();
            EnsureAdditiveMaterial();

            int particleLayer = LayerMask.NameToLayer(ParticleLayerName);
            if (particleLayer < 0)
            {
                Debug.LogError(
                    $"AmbientRoomParticleOverlay requires a '{ParticleLayerName}' " +
                    "layer (Project Settings > Tags and Layers).");
                return;
            }

            BuildBloomCamera(particleLayer);
            BuildParticleCanvas(particleLayer);
            BuildParticles();
            BuildCompositeImage();
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
            if (particleCanvasRect == null || particleRects[0] == null)
            {
                return;
            }

            Rect bounds = particleCanvasRect.rect;
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

        private void BuildBloomCamera(int particleLayer)
        {
            bloomTexture = new RenderTexture(
                BloomTextureSize, BloomTextureSize, 24, RenderTextureFormat.ARGB32)
            {
                name = "Ambient Particle Bloom RT",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            bloomCameraObject = new GameObject("Ambient Particle Bloom Camera")
            {
                layer = particleLayer
            };
            Camera bloomCamera = bloomCameraObject.AddComponent<Camera>();
            bloomCamera.clearFlags = CameraClearFlags.SolidColor;
            bloomCamera.backgroundColor = Color.black;
            bloomCamera.cullingMask = 1 << particleLayer;
            bloomCamera.targetTexture = bloomTexture;
            bloomCamera.allowHDR = true;
            bloomCamera.allowMSAA = false;

            UniversalAdditionalCameraData cameraData =
                bloomCameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            cameraData.volumeLayerMask = 1 << particleLayer;
            cameraData.antialiasing = AntialiasingMode.None;

            bloomVolumeObject = new GameObject("Ambient Particle Bloom Volume")
            {
                layer = particleLayer
            };
            Volume volume = bloomVolumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.weight = 1f;

            bloomProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            bloomProfile.name = "Ambient Particle Bloom Profile";
            Bloom bloom = bloomProfile.Add<Bloom>(true);
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.15f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 4f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.7f;
            volume.sharedProfile = bloomProfile;
        }

        private void BuildParticleCanvas(int particleLayer)
        {
            particleCanvasObject = new GameObject(
                "Ambient Particle Canvas", typeof(Canvas), typeof(CanvasScaler))
            {
                layer = particleLayer
            };
            Canvas canvas = particleCanvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = bloomCameraObject.GetComponent<Camera>();
            canvas.planeDistance = 1f;

            CanvasScaler scaler = particleCanvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            particleCanvasRect = particleCanvasObject.GetComponent<RectTransform>();
        }

        private void BuildParticles()
        {
            int particleLayer = particleCanvasObject.layer;
            for (int index = 0; index < ParticleCount; index++)
            {
                GameObject particle = new(
                    $"AmbientParticle_{index}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image))
                {
                    layer = particleLayer
                };
                particle.transform.SetParent(particleCanvasRect, false);

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

        private void BuildCompositeImage()
        {
            compositeObject = new(
                "Ambient Particle Composite",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));
            compositeObject.transform.SetParent(contentRect, false);
            RectTransform rect = compositeObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            RawImage compositeImage = compositeObject.GetComponent<RawImage>();
            compositeImage.texture = bloomTexture;
            compositeImage.raycastTarget = false;
            if (additiveMaterial != null)
            {
                compositeImage.material = additiveMaterial;
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
        // twinkle alpha and the additive shader's alpha-premultiply. Keep
        // the background's hue (and a softened saturation) so particles
        // still pick up local room color, but force it back up to full
        // brightness first.
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
            if (bloomTexture != null)
            {
                Destroy(bloomTexture);
                bloomTexture = null;
            }
            if (bloomProfile != null)
            {
                Destroy(bloomProfile);
                bloomProfile = null;
            }
            if (bloomCameraObject != null)
            {
                Destroy(bloomCameraObject);
            }
            if (bloomVolumeObject != null)
            {
                Destroy(bloomVolumeObject);
            }
            if (particleCanvasObject != null)
            {
                Destroy(particleCanvasObject);
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
