using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Wake.Exploration;

namespace Wake.Tests
{
    public sealed class AmbientRoomParticleOverlayTests
    {
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

                Image[] images =
                    contentObject.GetComponentsInChildren<Image>(true);
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

                Image[] images =
                    contentObject.GetComponentsInChildren<Image>(true);
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
    }
}
