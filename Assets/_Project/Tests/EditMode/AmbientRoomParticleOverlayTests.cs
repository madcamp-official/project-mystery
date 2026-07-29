using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Wake.Exploration;

namespace Wake.Tests
{
    public sealed class AmbientRoomParticleOverlayTests
    {
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

                overlay.Show(tint);

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
    }
}
