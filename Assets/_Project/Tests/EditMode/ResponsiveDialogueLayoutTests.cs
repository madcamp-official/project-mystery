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
        public void GameplayRoot_IsResetToFullStretchRegardlessOfAuthoredScale()
        {
            using LayoutRig rig = new(1920f, 1080f);

            Assert.That(rig.Ingame.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rig.Ingame.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(rig.Ingame.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(rig.Ingame.sizeDelta, Is.EqualTo(Vector2.zero));
            Assert.That(rig.Ingame.localScale, Is.EqualTo(Vector3.one));
        }

        [TestCase(1920f, 1080f)]
        [TestCase(1920f, 1200f)]
        public void SceneAuthoredPlacement_IsPreservedWhenSafeAreaIsUnchanged(
            float width,
            float height)
        {
            using LayoutRig rig = new(width, height);

            // The first ApplyLayout call captures whatever was on the
            // RectTransforms (the "Inspector placement") as the
            // baseline. A second call with the same safe area must
            // reproduce it byte-for-byte, not fall back to a formula.
            Rect fullSafeArea = new(0f, 0f, width, height);
            Vector2 screenSize = new(width, height);
            rig.Layout.ApplyLayout(fullSafeArea, screenSize);
            rig.Layout.ApplyLayout(fullSafeArea, screenSize);

            Assert.That(rig.LinePanel.anchorMin, Is.EqualTo(rig.AuthoredLinePanelAnchorMin));
            Assert.That(rig.LinePanel.anchorMax, Is.EqualTo(rig.AuthoredLinePanelAnchorMax));
            Assert.That(rig.LinePanel.anchoredPosition,
                Is.EqualTo(rig.AuthoredLinePanelPosition).Using(Vector2Comparer));
            Assert.That(rig.LinePanel.sizeDelta,
                Is.EqualTo(rig.AuthoredLinePanelSize).Using(Vector2Comparer));

            Assert.That(rig.NextButton.anchoredPosition,
                Is.EqualTo(rig.AuthoredNextButtonPosition).Using(Vector2Comparer));
            Assert.That(rig.NextButton.sizeDelta,
                Is.EqualTo(rig.AuthoredNextButtonSize).Using(Vector2Comparer));

            Assert.That(rig.EvidenceButton.anchoredPosition,
                Is.EqualTo(rig.AuthoredEvidenceButtonPosition).Using(Vector2Comparer));
            Assert.That(rig.EvidenceButton.sizeDelta,
                Is.EqualTo(rig.AuthoredEvidenceButtonSize).Using(Vector2Comparer));
            Assert.That(rig.SettingsButton.anchorMin,
                Is.EqualTo(rig.AuthoredSettingsButtonAnchor));
            Assert.That(rig.SettingsButton.anchoredPosition,
                Is.EqualTo(rig.AuthoredSettingsButtonPosition).Using(Vector2Comparer));
        }

        [Test]
        public void SafeAreaInsetChange_RescalesBaselineProportionally()
        {
            using LayoutRig rig = new(2000f, 1000f);

            // First call establishes the baseline against a full-bleed
            // safe area (no inset). Then simulate a notch that removes
            // 25% of the usable height - the vertically-offset elements
            // should scale down proportionally instead of staying fixed
            // or snapping to a hardcoded value.
            rig.Layout.ApplyLayout(
                new Rect(0f, 0f, 2000f, 1000f),
                new Vector2(2000f, 1000f));
            rig.Layout.ApplyLayout(
                new Rect(0f, 0f, 2000f, 750f),
                new Vector2(2000f, 1000f));

            float expectedScaleY = 0.75f;
            Assert.That(
                rig.NextButton.anchoredPosition.y,
                Is.EqualTo(rig.AuthoredNextButtonPosition.y * expectedScaleY)
                    .Within(0.01f));
            Assert.That(
                rig.LinePanel.sizeDelta.y,
                Is.EqualTo(rig.AuthoredLinePanelSize.y * expectedScaleY)
                    .Within(0.01f));
        }

        [Test]
        public void Portrait_UsesFixedTopLeftAnchorAndHeightControlledAspect()
        {
            using LayoutRig rig = new(1920f, 1080f);

            rig.Layout.ApplyLayout(
                new Rect(0f, 0f, 1920f, 1080f),
                new Vector2(1920f, 1080f));

            Assert.That(rig.Portrait.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(rig.Portrait.anchorMax, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(rig.Portrait.pivot, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(rig.Portrait.anchoredPosition.x,
                Is.EqualTo(ResponsiveDialogueLayout.EdgePadding));
            Assert.That(rig.Portrait.anchoredPosition.y,
                Is.EqualTo(-ResponsiveDialogueLayout.EdgePadding));
            Assert.That(rig.Portrait.sizeDelta.y, Is.EqualTo(300f));
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
        }

        [Test]
        public void DialogueLabels_KeepAuthoredFontSettingsUntouched()
        {
            using LayoutRig rig = new(1920f, 1080f);

            // Font size/wrapping/overflow are left exactly as the scene
            // (Inspector) authored them - ConfigureText must not force
            // any of these, unlike the scroll/mask plumbing above.
            Assert.That(
                rig.LineText.overflowMode,
                Is.EqualTo(rig.AuthoredLineTextOverflow));
            Assert.That(
                rig.LineText.fontSize,
                Is.EqualTo(rig.AuthoredLineTextFontSize));
            Assert.That(
                rig.LineText.enableAutoSizing,
                Is.EqualTo(rig.AuthoredLineTextAutoSizing));
            Assert.That(
                rig.SpeakerText.overflowMode,
                Is.EqualTo(rig.AuthoredSpeakerTextOverflow));
        }

        [TestCase(1920f, 1080f)]
        [TestCase(1920f, 1200f)]
        public void LineText_PlacementFollowsBaselineNotFormula(
            float width,
            float height)
        {
            using LayoutRig rig = new(width, height);
            Rect fullSafeArea = new(0f, 0f, width, height);
            Vector2 screenSize = new(width, height);

            rig.Layout.ApplyLayout(fullSafeArea, screenSize);

            // Only width is a baseline concern. Y (position and height)
            // belongs to the dialogue ScrollRect/ContentSizeFitter, since
            // this RectTransform doubles as the scroll content - baselining
            // it would fight the scroll-to-top reset on every line change.
            Assert.That(rig.LineText.rectTransform.anchoredPosition.x,
                Is.EqualTo(rig.AuthoredLineTextPosition.x).Within(0.01f));
            Assert.That(rig.LineText.rectTransform.sizeDelta.x,
                Is.EqualTo(rig.AuthoredLineTextSize.x).Within(0.01f));
        }

        [Test]
        public void LineText_YAxisIsNeverTouchedByBaselineReapplication()
        {
            using LayoutRig rig = new(1920f, 1080f);
            Rect fullSafeArea = new(0f, 0f, 1920f, 1080f);
            Vector2 screenSize = new(1920f, 1080f);

            // Simulate what happens on every new dialogue line: the
            // ScrollRect (or a test stand-in) moves the content's Y,
            // then layout gets reapplied - Y must survive untouched.
            rig.Layout.ApplyLayout(fullSafeArea, screenSize);
            rig.LineText.rectTransform.anchoredPosition = new Vector2(
                rig.LineText.rectTransform.anchoredPosition.x, 123.45f);
            rig.LineText.rectTransform.sizeDelta = new Vector2(
                rig.LineText.rectTransform.sizeDelta.x, 67.89f);

            rig.Layout.ApplyLayout(fullSafeArea, screenSize);

            Assert.That(
                rig.LineText.rectTransform.anchoredPosition.y,
                Is.EqualTo(123.45f).Within(0.01f));
            Assert.That(
                rig.LineText.rectTransform.sizeDelta.y,
                Is.EqualTo(67.89f).Within(0.01f));
        }

        private static readonly Vector2EqualityComparer Vector2Comparer =
            new(0.01f);

        private sealed class Vector2EqualityComparer :
            System.Collections.Generic.IEqualityComparer<Vector2>
        {
            private readonly float tolerance;

            public Vector2EqualityComparer(float tolerance)
            {
                this.tolerance = tolerance;
            }

            public bool Equals(Vector2 a, Vector2 b) =>
                Mathf.Abs(a.x - b.x) <= tolerance &&
                Mathf.Abs(a.y - b.y) <= tolerance;

            public int GetHashCode(Vector2 obj) => 0;
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
                AuthoredEvidenceButtonPosition = new Vector2(64f, -140f);
                AuthoredEvidenceButtonSize = new Vector2(210f, 82f);
                EvidenceButton.anchorMin = new Vector2(0f, 1f);
                EvidenceButton.anchorMax = new Vector2(0f, 1f);
                EvidenceButton.pivot = new Vector2(0f, 1f);
                EvidenceButton.anchoredPosition = AuthoredEvidenceButtonPosition;
                EvidenceButton.sizeDelta = AuthoredEvidenceButtonSize;

                MapButton = CreateRect("Map Btn", Ingame);
                MapButton.anchorMin = new Vector2(0f, 1f);
                MapButton.anchorMax = new Vector2(0f, 1f);
                MapButton.pivot = new Vector2(0f, 1f);
                MapButton.anchoredPosition = new Vector2(320f, -140f);
                MapButton.sizeDelta = new Vector2(210f, 82f);

                SettingsButton = CreateRect("Settings Btn", Ingame);
                AuthoredSettingsButtonAnchor = new Vector2(1f, 1f);
                AuthoredSettingsButtonPosition = new Vector2(-40f, -96f);
                SettingsButton.anchorMin = AuthoredSettingsButtonAnchor;
                SettingsButton.anchorMax = AuthoredSettingsButtonAnchor;
                SettingsButton.pivot = AuthoredSettingsButtonAnchor;
                SettingsButton.anchoredPosition = AuthoredSettingsButtonPosition;
                SettingsButton.sizeDelta = new Vector2(210f, 82f);

                LinePanel = CreateRect("Line Panel", Ingame);
                AuthoredLinePanelAnchorMin = new Vector2(0f, 0f);
                AuthoredLinePanelAnchorMax = new Vector2(1f, 0f);
                AuthoredLinePanelPosition = new Vector2(0f, 36f);
                AuthoredLinePanelSize = new Vector2(0f, 480f);
                LinePanel.anchorMin = AuthoredLinePanelAnchorMin;
                LinePanel.anchorMax = AuthoredLinePanelAnchorMax;
                LinePanel.pivot = new Vector2(0.5f, 0f);
                LinePanel.anchoredPosition = AuthoredLinePanelPosition;
                LinePanel.sizeDelta = AuthoredLinePanelSize;

                Panel = CreateRect("Panel", LinePanel);
                SpeakerPlate = CreateRect("Image", LinePanel);
                Choices = CreateRect("Select Btn", LinePanel);

                LineText = CreateText("line", Panel);
                AuthoredLineTextOverflow = TextOverflowModes.Linked;
                AuthoredLineTextFontSize = 31f;
                AuthoredLineTextAutoSizing = true;
                AuthoredLineTextPosition = new Vector2(-40f, -30f);
                AuthoredLineTextSize = new Vector2(-120f, 0f);
                LineText.overflowMode = AuthoredLineTextOverflow;
                LineText.fontSize = AuthoredLineTextFontSize;
                LineText.enableAutoSizing = AuthoredLineTextAutoSizing;
                LineText.rectTransform.anchorMin = new Vector2(0f, 1f);
                LineText.rectTransform.anchorMax = new Vector2(1f, 1f);
                LineText.rectTransform.pivot = new Vector2(0.5f, 1f);
                LineText.rectTransform.anchoredPosition = AuthoredLineTextPosition;
                LineText.rectTransform.sizeDelta = AuthoredLineTextSize;

                SpeakerText = CreateText("Text (TMP)", SpeakerPlate);
                AuthoredSpeakerTextOverflow = TextOverflowModes.Truncate;
                SpeakerText.overflowMode = AuthoredSpeakerTextOverflow;
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
                AuthoredNextButtonPosition = new Vector2(-44f, 44f);
                AuthoredNextButtonSize = new Vector2(224f, 80f);
                NextButton.anchorMin = new Vector2(1f, 0f);
                NextButton.anchorMax = new Vector2(1f, 0f);
                NextButton.pivot = new Vector2(1f, 0f);
                NextButton.anchoredPosition = AuthoredNextButtonPosition;
                NextButton.sizeDelta = AuthoredNextButtonSize;

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

            public Vector2 AuthoredEvidenceButtonPosition { get; }
            public Vector2 AuthoredEvidenceButtonSize { get; }
            public Vector2 AuthoredSettingsButtonAnchor { get; }
            public Vector2 AuthoredSettingsButtonPosition { get; }
            public Vector2 AuthoredLinePanelAnchorMin { get; }
            public Vector2 AuthoredLinePanelAnchorMax { get; }
            public Vector2 AuthoredLinePanelPosition { get; }
            public Vector2 AuthoredLinePanelSize { get; }
            public Vector2 AuthoredNextButtonPosition { get; }
            public Vector2 AuthoredNextButtonSize { get; }
            public TextOverflowModes AuthoredLineTextOverflow { get; private set; }
            public float AuthoredLineTextFontSize { get; private set; }
            public bool AuthoredLineTextAutoSizing { get; private set; }
            public Vector2 AuthoredLineTextPosition { get; private set; }
            public Vector2 AuthoredLineTextSize { get; private set; }
            public TextOverflowModes AuthoredSpeakerTextOverflow { get; private set; }

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
