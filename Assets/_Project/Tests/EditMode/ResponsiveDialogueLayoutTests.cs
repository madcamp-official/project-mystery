using NUnit.Framework;
using TMPro;
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

        [TestCase(1920f, 1080f)]
        [TestCase(1920f, 1200f)]
        public void GameplayRoot_UsesFullCanvasAtSupportedRatios(
            float width,
            float height)
        {
            using LayoutRig rig = new(width, height);

            rig.Layout.ApplyLayout(
                new Rect(0f, 0f, width, height),
                new Vector2(width, height));

            Assert.That(rig.Ingame.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rig.Ingame.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(rig.Ingame.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(rig.Ingame.sizeDelta, Is.EqualTo(Vector2.zero));
            Assert.That(rig.Ingame.localScale, Is.EqualTo(Vector3.one));
            Assert.That(rig.LinePanel.anchorMin.x, Is.Zero.Within(Tolerance));
            Assert.That(rig.LinePanel.anchorMax.x, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(rig.LinePanel.anchorMin.y, Is.Zero.Within(Tolerance));
            Assert.That(rig.LinePanel.sizeDelta.y,
                Is.EqualTo(ResponsiveDialogueLayout.DialogueHeight));
        }

        [TestCase(1920f, 1080f)]
        [TestCase(1920f, 1200f)]
        public void NavigationButtons_UseSafeTopAnchorsAtSupportedRatios(
            float width,
            float height)
        {
            using LayoutRig rig = new(width, height);

            rig.Layout.ApplyLayout(
                new Rect(0f, 0f, width, height),
                new Vector2(width, height));

            AssertLeftNavigationButton(
                rig.EvidenceButton,
                ResponsiveDialogueLayout.EdgePadding);
            AssertLeftNavigationButton(
                rig.MapButton,
                ResponsiveDialogueLayout.EdgePadding * 2f +
                ResponsiveDialogueLayout.NavigationButtonWidth);
            Assert.That(rig.SettingsButton.anchorMin,
                Is.EqualTo(new Vector2(1f, 1f)));
            Assert.That(rig.SettingsButton.anchorMax,
                Is.EqualTo(new Vector2(1f, 1f)));
            Assert.That(rig.SettingsButton.pivot,
                Is.EqualTo(new Vector2(1f, 1f)));
            Assert.That(rig.SettingsButton.anchoredPosition.x,
                Is.EqualTo(-ResponsiveDialogueLayout.EdgePadding));
            Assert.That(rig.SettingsButton.anchoredPosition.y,
                Is.EqualTo(-ResponsiveDialogueLayout.NavigationTop));
            Assert.That(
                rig.SettingsButton.sizeDelta,
                Is.EqualTo(new Vector2(
                    ResponsiveDialogueLayout.NavigationButtonWidth,
                    ResponsiveDialogueLayout.NavigationButtonHeight)));
        }

        [Test]
        public void Portrait_UsesFixedLeftAnchorAndHeightControlledAspect()
        {
            using LayoutRig rig = new(1920f, 1080f);

            rig.Layout.ApplyLayout(
                new Rect(0f, 0f, 1920f, 1080f),
                new Vector2(1920f, 1080f));

            Assert.That(rig.Portrait.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rig.Portrait.anchorMax, Is.EqualTo(Vector2.zero));
            Assert.That(rig.Portrait.pivot, Is.EqualTo(Vector2.zero));
            Assert.That(rig.Portrait.anchoredPosition.x,
                Is.EqualTo(ResponsiveDialogueLayout.EdgePadding));
            Assert.That(rig.Portrait.anchoredPosition.y, Is.EqualTo(18f));
            Assert.That(rig.Portrait.sizeDelta.y, Is.EqualTo(430f));
            Assert.That(
                rig.Portrait.GetComponent<AspectRatioFitter>().aspectMode,
                Is.EqualTo(
                    AspectRatioFitter.AspectMode.HeightControlsWidth));
        }

        [Test]
        public void DialogueText_IsMaskedScrollableAndCannotPaintOutsidePanel()
        {
            using LayoutRig rig = new(1920f, 1080f);

            ScrollRect scroll = rig.Panel.GetComponent<ScrollRect>();
            RectMask2D mask = rig.Panel.GetComponent<RectMask2D>();
            ContentSizeFitter fitter =
                rig.LineText.GetComponent<ContentSizeFitter>();

            Assert.That(scroll, Is.Not.Null);
            Assert.That(mask, Is.Not.Null);
            Assert.That(scroll.viewport, Is.SameAs(rig.Panel));
            Assert.That(scroll.content, Is.SameAs(rig.LineText.rectTransform));
            Assert.That(scroll.horizontal, Is.False);
            Assert.That(scroll.vertical, Is.True);
            Assert.That(
                scroll.movementType,
                Is.EqualTo(ScrollRect.MovementType.Clamped));
            Assert.That(fitter, Is.Not.Null);
            Assert.That(
                fitter.verticalFit,
                Is.EqualTo(ContentSizeFitter.FitMode.PreferredSize));
            Assert.That(
                rig.LineText.overflowMode,
                Is.EqualTo(TextOverflowModes.Overflow));
            Assert.That(
                rig.SpeakerText.overflowMode,
                Is.EqualTo(TextOverflowModes.Ellipsis));
        }

        [Test]
        public void NextButton_RemainsInsideDialoguePanel()
        {
            using LayoutRig rig = new(1920f, 1080f);

            rig.Layout.ApplyLayout(
                new Rect(0f, 0f, 1920f, 1080f),
                new Vector2(1920f, 1080f));

            Assert.That(rig.NextButton.anchorMin,
                Is.EqualTo(new Vector2(1f, 0f)));
            Assert.That(rig.NextButton.anchorMax,
                Is.EqualTo(new Vector2(1f, 0f)));
            Assert.That(rig.NextButton.pivot,
                Is.EqualTo(new Vector2(1f, 0f)));
            Assert.That(
                rig.NextButton.anchoredPosition,
                Is.EqualTo(new Vector2(
                    -ResponsiveDialogueLayout.EdgePadding,
                    ResponsiveDialogueLayout.EdgePadding)));
            Assert.That(
                rig.NextButton.sizeDelta,
                Is.EqualTo(new Vector2(176f, 60f)));
        }

        private static void AssertLeftNavigationButton(
            RectTransform button,
            float expectedInset)
        {
            Assert.That(button.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(button.anchorMax, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(button.pivot, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(button.anchoredPosition.x, Is.EqualTo(expectedInset));
            Assert.That(button.anchoredPosition.y,
                Is.EqualTo(-ResponsiveDialogueLayout.NavigationTop));
            Assert.That(
                button.sizeDelta,
                Is.EqualTo(new Vector2(
                    ResponsiveDialogueLayout.NavigationButtonWidth,
                    ResponsiveDialogueLayout.NavigationButtonHeight)));
        }

        private sealed class LayoutRig : System.IDisposable
        {
            private readonly GameObject canvasObject;

            public LayoutRig(float width, float height)
            {
                canvasObject = new GameObject(
                    "Canvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(ResponsiveDialogueLayout));
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                CanvasRect = canvasObject.GetComponent<RectTransform>();
                CanvasRect.sizeDelta = new Vector2(width, height);
                Ingame = CreateRect("Ingame", CanvasRect);
                Ingame.anchorMin = new Vector2(0.5f, 0.5f);
                Ingame.anchorMax = new Vector2(0.5f, 0.5f);
                Ingame.sizeDelta = new Vector2(100f, 100f);
                Ingame.localScale = Vector3.one * 3.5f;

                EvidenceButton = CreateRect("Evidence Btn", Ingame);
                MapButton = CreateRect("Map Btn", Ingame);
                SettingsButton = CreateRect("Settings Btn", Ingame);
                LinePanel = CreateRect("Line Panel", Ingame);
                Panel = CreateRect("Panel", LinePanel);
                SpeakerPlate = CreateRect("Image", LinePanel);
                Choices = CreateRect("Select Btn", LinePanel);

                LineText = CreateText("line", Panel);
                SpeakerText = CreateText("Text (TMP)", SpeakerPlate);
                Portrait = CreateRect(
                    "Speaker Portrait",
                    LinePanel,
                    typeof(RawImage),
                    typeof(AspectRatioFitter));
                NextButton = CreateRect(
                    "Next",
                    Panel,
                    typeof(Image),
                    typeof(Button));

                Layout = canvasObject.GetComponent<ResponsiveDialogueLayout>();
                Layout.Initialize(
                    canvas,
                    LinePanel,
                    Portrait,
                    LineText,
                    SpeakerText,
                    NextButton,
                    Choices,
                    System.Array.Empty<Button>());
            }

            public RectTransform CanvasRect { get; }
            public RectTransform Ingame { get; }
            public RectTransform EvidenceButton { get; }
            public RectTransform MapButton { get; }
            public RectTransform SettingsButton { get; }
            public RectTransform LinePanel { get; }
            public RectTransform Panel { get; }
            public RectTransform SpeakerPlate { get; }
            public RectTransform Portrait { get; }
            public RectTransform Choices { get; }
            public RectTransform NextButton { get; }
            public TMP_Text LineText { get; }
            public TMP_Text SpeakerText { get; }
            public ResponsiveDialogueLayout Layout { get; }

            public void Dispose()
            {
                Object.DestroyImmediate(canvasObject);
            }

            private static RectTransform CreateRect(
                string name,
                Transform parent,
                params System.Type[] extraComponents)
            {
                System.Type[] components = new System.Type[
                    extraComponents.Length + 1];
                components[0] = typeof(RectTransform);
                extraComponents.CopyTo(components, 1);
                GameObject target = new(name, components);
                target.transform.SetParent(parent, false);
                return target.GetComponent<RectTransform>();
            }

            private static TMP_Text CreateText(
                string name,
                Transform parent)
            {
                GameObject target = new(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
                target.transform.SetParent(parent, false);
                return target.GetComponent<TMP_Text>();
            }
        }
    }
}
