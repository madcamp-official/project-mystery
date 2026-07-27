using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class ResponsiveDialogueLayoutTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void FullScreenSafeArea_UsesFullAnchorsAtSixteenByNine()
        {
            SafeAreaAnchors anchors = SafeAreaUtility.ToAnchors(
                new Rect(0f, 0f, 1920f, 1080f),
                new Vector2(1920f, 1080f));

            Assert.That(anchors.Minimum, Is.EqualTo(Vector2.zero));
            Assert.That(anchors.Maximum, Is.EqualTo(Vector2.one));
        }

        [Test]
        public void FullScreenSafeArea_UsesFullAnchorsAtSixteenByTen()
        {
            SafeAreaAnchors anchors = SafeAreaUtility.ToAnchors(
                new Rect(0f, 0f, 1920f, 1200f),
                new Vector2(1920f, 1200f));

            Assert.That(anchors.Minimum, Is.EqualTo(Vector2.zero));
            Assert.That(anchors.Maximum, Is.EqualTo(Vector2.one));
        }

        [Test]
        public void DeviceInsets_AreConvertedToNormalizedAnchors()
        {
            SafeAreaAnchors anchors = SafeAreaUtility.ToAnchors(
                new Rect(100f, 60f, 1720f, 960f),
                new Vector2(1920f, 1080f));

            Assert.That(
                anchors.Minimum.x,
                Is.EqualTo(100f / 1920f).Within(Tolerance));
            Assert.That(
                anchors.Minimum.y,
                Is.EqualTo(60f / 1080f).Within(Tolerance));
            Assert.That(
                anchors.Maximum.x,
                Is.EqualTo(1820f / 1920f).Within(Tolerance));
            Assert.That(
                anchors.Maximum.y,
                Is.EqualTo(1020f / 1080f).Within(Tolerance));
        }

        [Test]
        public void OutOfBoundsSafeArea_IsClamped()
        {
            SafeAreaAnchors anchors = SafeAreaUtility.ToAnchors(
                new Rect(-100f, -50f, 2200f, 1300f),
                new Vector2(1920f, 1080f));

            Assert.That(anchors.Minimum, Is.EqualTo(Vector2.zero));
            Assert.That(anchors.Maximum, Is.EqualTo(Vector2.one));
        }

        [Test]
        public void InvalidScreenSize_IsRejected()
        {
            Assert.That(
                () => SafeAreaUtility.ToAnchors(
                    new Rect(0f, 0f, 1f, 1f),
                    Vector2.zero),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void Layout_AppliesReferenceScalerAndBottomSafePanel()
        {
            GameObject canvasObject = new(
                "Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(ResponsiveDialogueLayout));
            GameObject lineObject = new("Line Panel", typeof(RectTransform));
            lineObject.transform.SetParent(canvasObject.transform, false);
            try
            {
                ResponsiveDialogueLayout layout =
                    canvasObject.GetComponent<ResponsiveDialogueLayout>();
                layout.Initialize(
                    canvasObject.GetComponent<Canvas>(),
                    lineObject.GetComponent<RectTransform>(),
                    null,
                    null,
                    null,
                    null,
                    null);
                layout.ApplyLayout(
                    new Rect(96f, 54f, 1728f, 972f),
                    new Vector2(1920f, 1080f));

                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                Assert.That(
                    scaler.referenceResolution,
                    Is.EqualTo(ResponsiveDialogueLayout.ReferenceResolution));
                Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(0.5f));

                RectTransform panel =
                    lineObject.GetComponent<RectTransform>();
                Assert.That(panel.anchorMin.x, Is.EqualTo(0.05f).Within(Tolerance));
                Assert.That(panel.anchorMax.x, Is.EqualTo(0.95f).Within(Tolerance));
                Assert.That(panel.anchorMin.y, Is.EqualTo(0.05f).Within(Tolerance));
                Assert.That(panel.anchorMax.y, Is.EqualTo(0.05f).Within(Tolerance));
                Assert.That(
                    panel.sizeDelta.y,
                    Is.EqualTo(ResponsiveDialogueLayout.DialogueHeight));
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }
    }
}
