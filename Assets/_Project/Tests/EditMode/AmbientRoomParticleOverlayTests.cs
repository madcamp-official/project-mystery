using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Wake.Exploration;

namespace Wake.Tests
{
    public sealed class AmbientRoomParticleOverlayTests
    {
        private static readonly BindingFlags PrivateInstance =
            BindingFlags.NonPublic | BindingFlags.Instance;

        private static T GetPrivateField<T>(object target, string name) =>
            (T)typeof(AmbientRoomParticleOverlay)
                .GetField(name, PrivateInstance)
                .GetValue(target);

        [Test]
        public void Initialize_CreatesSixteenNonInteractiveGlowParticles()
        {
            GameObject contentObject =
                new("Content", typeof(RectTransform));
            GameObject overlayObject =
                new("Overlay", typeof(AmbientRoomParticleOverlay));
            try
            {
                AmbientRoomParticleOverlay overlay =
                    overlayObject.GetComponent<AmbientRoomParticleOverlay>();
                overlay.Initialize(
                    contentObject.GetComponent<RectTransform>());

                RectTransform particleCanvasRect =
                    GetPrivateField<RectTransform>(overlay, "particleCanvasRect");
                Image[] images =
                    particleCanvasRect.GetComponentsInChildren<Image>(true);
                Assert.That(images.Length, Is.EqualTo(16));
                foreach (Image image in images)
                {
                    Assert.That(image.raycastTarget, Is.False);
                    Assert.That(image.sprite, Is.Not.Null);
                }
            }
            finally
            {
                Object.DestroyImmediate(overlayObject);
                Object.DestroyImmediate(contentObject);
            }
        }

        [Test]
        public void Initialize_CreatesCompositeRawImageStretchedOverContentRect()
        {
            GameObject contentObject =
                new("Content", typeof(RectTransform));
            GameObject overlayObject =
                new("Overlay", typeof(AmbientRoomParticleOverlay));
            try
            {
                AmbientRoomParticleOverlay overlay =
                    overlayObject.GetComponent<AmbientRoomParticleOverlay>();
                overlay.Initialize(
                    contentObject.GetComponent<RectTransform>());

                RawImage composite =
                    contentObject.GetComponentInChildren<RawImage>(true);
                Assert.That(composite, Is.Not.Null);
                Assert.That(composite.raycastTarget, Is.False);
                Assert.That(composite.texture, Is.Not.Null);
                RectTransform rect = composite.GetComponent<RectTransform>();
                Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
            }
            finally
            {
                Object.DestroyImmediate(overlayObject);
                Object.DestroyImmediate(contentObject);
            }
        }

        [Test]
        public void Initialize_ConfiguresBloomCameraAndVolume()
        {
            GameObject contentObject =
                new("Content", typeof(RectTransform));
            GameObject overlayObject =
                new("Overlay", typeof(AmbientRoomParticleOverlay));
            try
            {
                AmbientRoomParticleOverlay overlay =
                    overlayObject.GetComponent<AmbientRoomParticleOverlay>();
                overlay.Initialize(
                    contentObject.GetComponent<RectTransform>());

                GameObject cameraObject =
                    GetPrivateField<GameObject>(overlay, "bloomCameraObject");
                Camera camera = cameraObject.GetComponent<Camera>();
                int particleLayer = LayerMask.NameToLayer("AmbientParticles");
                Assert.That(camera.cullingMask, Is.EqualTo(1 << particleLayer));
                Assert.That(camera.targetTexture, Is.Not.Null);

                UniversalAdditionalCameraData cameraData =
                    cameraObject.GetComponent<UniversalAdditionalCameraData>();
                Assert.That(cameraData.renderPostProcessing, Is.True);
                Assert.That(
                    cameraData.volumeLayerMask.value,
                    Is.EqualTo(1 << particleLayer));

                VolumeProfile profile =
                    GetPrivateField<VolumeProfile>(overlay, "bloomProfile");
                Assert.That(profile.TryGet(out Bloom bloom), Is.True);
                Assert.That(bloom.active, Is.True);
                Assert.That(bloom.intensity.value, Is.GreaterThan(0f));
            }
            finally
            {
                Object.DestroyImmediate(overlayObject);
                Object.DestroyImmediate(contentObject);
            }
        }

        [Test]
        public void Show_TintsEveryParticleImage()
        {
            GameObject contentObject =
                new("Content", typeof(RectTransform));
            GameObject overlayObject =
                new("Overlay", typeof(AmbientRoomParticleOverlay));
            try
            {
                AmbientRoomParticleOverlay overlay =
                    overlayObject.GetComponent<AmbientRoomParticleOverlay>();
                overlay.Initialize(
                    contentObject.GetComponent<RectTransform>());
                Color tint = new(0.2f, 0.4f, 0.9f, 0.5f);

                overlay.Show(tint, null);

                RectTransform particleCanvasRect =
                    GetPrivateField<RectTransform>(overlay, "particleCanvasRect");
                Image[] images =
                    particleCanvasRect.GetComponentsInChildren<Image>(true);
                foreach (Image image in images)
                {
                    Assert.That(image.color.r, Is.EqualTo(tint.r).Within(0.001f));
                    Assert.That(image.color.g, Is.EqualTo(tint.g).Within(0.001f));
                    Assert.That(image.color.b, Is.EqualTo(tint.b).Within(0.001f));
                }
            }
            finally
            {
                Object.DestroyImmediate(overlayObject);
                Object.DestroyImmediate(contentObject);
            }
        }

        [Test]
        public void Show_WithBackgroundSprite_DoesNotThrow()
        {
            GameObject contentObject =
                new("Content", typeof(RectTransform));
            GameObject overlayObject =
                new("Overlay", typeof(AmbientRoomParticleOverlay));
            Texture2D texture = new(4, 4, TextureFormat.RGBA32, false);
            texture.SetPixels(new[]
            {
                Color.red, Color.red, Color.red, Color.red,
                Color.red, Color.red, Color.red, Color.red,
                Color.red, Color.red, Color.red, Color.red,
                Color.red, Color.red, Color.red, Color.red
            });
            texture.Apply();
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 4f, 4f),
                new Vector2(0.5f, 0.5f));
            try
            {
                AmbientRoomParticleOverlay overlay =
                    overlayObject.GetComponent<AmbientRoomParticleOverlay>();
                overlay.Initialize(
                    contentObject.GetComponent<RectTransform>());

                Assert.That(
                    () => overlay.Show(Color.white, sprite),
                    Throws.Nothing);
            }
            finally
            {
                Object.DestroyImmediate(overlayObject);
                Object.DestroyImmediate(contentObject);
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void Pause_StopsBloomRenderingButKeepsFrozenCompositeVisible()
        {
            GameObject contentObject =
                new("Content", typeof(RectTransform));
            GameObject overlayObject =
                new("Overlay", typeof(AmbientRoomParticleOverlay));
            try
            {
                AmbientRoomParticleOverlay overlay =
                    overlayObject.GetComponent<AmbientRoomParticleOverlay>();
                overlay.Initialize(
                    contentObject.GetComponent<RectTransform>());

                overlay.SetPaused(true);

                Assert.That(
                    GetPrivateField<GameObject>(
                        overlay, "bloomCameraObject").activeSelf,
                    Is.False);
                Assert.That(
                    GetPrivateField<GameObject>(
                        overlay, "bloomVolumeObject").activeSelf,
                    Is.False);
                Assert.That(
                    GetPrivateField<GameObject>(
                        overlay, "particleCanvasObject").activeSelf,
                    Is.False);
                Assert.That(
                    GetPrivateField<GameObject>(
                        overlay, "compositeObject").activeSelf,
                    Is.True);

                overlay.SetPaused(false);

                Assert.That(
                    GetPrivateField<GameObject>(
                        overlay, "bloomCameraObject").activeSelf,
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(overlayObject);
                Object.DestroyImmediate(contentObject);
            }
        }

        // Regression test for a bug where multiplying the raw (dark)
        // sampled background color into the particle crushed brightness to
        // near-invisible. NormalizeForGlow must keep hue but force the
        // background sample back up to full brightness.
        [Test]
        public void NormalizeForGlow_ForcesDarkBackgroundToFullBrightness()
        {
            MethodInfo method = typeof(AmbientRoomParticleOverlay).GetMethod(
                "NormalizeForGlow",
                BindingFlags.NonPublic | BindingFlags.Static);
            Color darkBackground = new(0.049f, 0.047f, 0.063f);

            Color result = (Color)method.Invoke(
                null, new object[] { darkBackground });

            float maxChannel = Mathf.Max(result.r, result.g, result.b);
            Assert.That(maxChannel, Is.EqualTo(1f).Within(0.01f));
        }
    }
}
