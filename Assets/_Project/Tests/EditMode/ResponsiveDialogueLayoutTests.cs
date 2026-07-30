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

            Assert.That(rig.SpeakerPlate.anchoredPosition,
                Is.EqualTo(rig.AuthoredSpeakerPlatePosition).Using(Vector2Comparer));
            Assert.That(rig.SpeakerPlate.sizeDelta,
                Is.EqualTo(rig.AuthoredSpeakerPlateSize).Using(Vector2Comparer));
        }

        [Test]
        public void SafeAreaInsetChange_UsesSafeRootAndPreservesBaselines()
        {
            using LayoutRig rig = new(2000f, 1000f);

            // Simulate a top inset that removes 25% of the usable height.
            // The root follows the Safe Area while child coordinates
            // remain the exact values authored in the Inspector.
            rig.Layout.ApplyLayout(
                new Rect(0f, 0f, 2000f, 1000f),
                new Vector2(2000f, 1000f));
            rig.Layout.ApplyLayout(
                new Rect(0f, 0f, 2000f, 750f),
                new Vector2(2000f, 1000f));

            Assert.That(
                rig.Ingame.anchorMin,
                Is.EqualTo(Vector2.zero));
            Assert.That(
                rig.Ingame.anchorMax,
                Is.EqualTo(new Vector2(1f, 0.75f)));
            Assert.That(
                rig.NextButton.anchoredPosition,
                Is.EqualTo(rig.AuthoredNextButtonPosition)
                    .Using(Vector2Comparer));
            Assert.That(
                rig.LinePanel.sizeDelta.y,
                Is.EqualTo(rig.AuthoredLinePanelSize.y).Within(0.01f));
        }

        [Test]
        public void HorizontalSafeAreaInsets_AreAppliedByTheGameplayRoot()
        {
            using LayoutRig rig = new(2000f, 1000f);
            Vector2 screenSize = new(2000f, 1000f);

            rig.Layout.ApplyLayout(
                new Rect(0f, 0f, 2000f, 1000f),
                screenSize);
            rig.Layout.ApplyLayout(
                new Rect(100f, 0f, 1800f, 1000f),
                screenSize);

            Assert.That(
                rig.Ingame.anchorMin,
                Is.EqualTo(new Vector2(0.05f, 0f)));
            Assert.That(
                rig.Ingame.anchorMax,
                Is.EqualTo(new Vector2(0.95f, 1f)));
            Assert.That(
                rig.Choices.anchoredPosition,
                Is.EqualTo(new Vector2(280f, 20f))
                    .Using(Vector2Comparer));
            Assert.That(
                rig.Choices.sizeDelta,
                Is.EqualTo(new Vector2(-1120f, 132f))
                    .Using(Vector2Comparer));
        }

        [Test]
        public void Portrait_SitsAbovePanelTopEdgeNotInsideIt()
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
            // Positive Y (not negative) is the point: with a top anchor/
            // pivot, positive Y means the portrait's bottom edge sits at
            // or above the panel's top edge instead of inside the panel.
            Assert.That(rig.Portrait.anchoredPosition.y, Is.GreaterThan(0f));
            Assert.That(
                rig.Portrait.anchoredPosition.y -
                    rig.Portrait.sizeDelta.y,
                Is.GreaterThanOrEqualTo(-0.01f));
            Assert.That(
                rig.Portrait.GetComponent<AspectRatioFitter>().aspectMode,
                Is.EqualTo(
                    AspectRatioFitter.AspectMode.HeightControlsWidth));
        }

        [Test]
        public void DialogueText_HasNoScrollOrAutoSizeComponents()
        {
            using LayoutRig rig = new(1920f, 1080f);

            // Dialogue text no longer scrolls - long lines are left to
            // overflow per whatever overflowMode is authored in the
            // Inspector. Nothing should add a ScrollRect, RectMask2D, or
            // ContentSizeFitter to the panel/line text.
            Assert.That(rig.Panel.GetComponent<ScrollRect>(), Is.Null);
            Assert.That(rig.Panel.GetComponent<RectMask2D>(), Is.Null);
            Assert.That(
                rig.LineText.GetComponent<ContentSizeFitter>(), Is.Null);
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
            Assert.That(rig.SpeakerText.enableAutoSizing, Is.True);
            Assert.That(
                rig.SpeakerText.fontSizeMin,
                Is.EqualTo(DialogueTypographyMetrics.SpeakerMinimum));
            Assert.That(
                rig.SpeakerText.fontSizeMax,
                Is.EqualTo(DialogueTypographyMetrics.SpeakerMaximum));
            Assert.That(
                rig.SpeakerText.textWrappingMode,
                Is.EqualTo(TextWrappingModes.NoWrap));
            Assert.That(
                rig.SpeakerText.rectTransform.anchorMin,
                Is.EqualTo(Vector2.zero));
            Assert.That(
                rig.SpeakerText.rectTransform.anchorMax,
                Is.EqualTo(Vector2.one));
            Assert.That(rig.SpeakerText.raycastTarget, Is.False);
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

            // No ScrollRect owns Y anymore, so the line text is a full
            // baseline citizen like everything else: both axes of
            // position and size follow whatever was authored.
            Assert.That(rig.LineText.rectTransform.anchoredPosition,
                Is.EqualTo(rig.AuthoredLineTextPosition).Using(Vector2Comparer));
            Assert.That(rig.LineText.rectTransform.sizeDelta,
                Is.EqualTo(rig.AuthoredLineTextSize).Using(Vector2Comparer));
        }

        [Test]
        public void ResetTextScroll_DoesNotOverrideInspectorAuthoredLayout()
        {
            using LayoutRig rig = new(1920f, 1080f);
            RectTransform lineRect = rig.LineText.rectTransform;
            Vector2 position = lineRect.anchoredPosition;
            Vector2 size = lineRect.sizeDelta;
            TextOverflowModes overflow = rig.LineText.overflowMode;

            // DialogueController invokes this for every presented line.
            // The compatibility hook must remain layout-neutral now that
            // scrolling is intentionally disabled.
            rig.Layout.ResetTextScroll();

            Assert.That(lineRect.anchoredPosition,
                Is.EqualTo(position).Using(Vector2Comparer));
            Assert.That(lineRect.sizeDelta,
                Is.EqualTo(size).Using(Vector2Comparer));
            Assert.That(rig.LineText.overflowMode, Is.EqualTo(overflow));
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
                AuthoredSpeakerPlatePosition = new Vector2(348f, 420f);
                AuthoredSpeakerPlateSize = new Vector2(460f, 68f);
                SpeakerPlate.anchorMin = new Vector2(0f, 1f);
                SpeakerPlate.anchorMax = new Vector2(0f, 1f);
                SpeakerPlate.pivot = new Vector2(0f, 1f);
                SpeakerPlate.anchoredPosition = AuthoredSpeakerPlatePosition;
                SpeakerPlate.sizeDelta = AuthoredSpeakerPlateSize;
                Choices = CreateRect("Select Btn", LinePanel);
                Choices.anchorMin = new Vector2(0f, 1f);
                Choices.anchorMax = new Vector2(1f, 1f);
                Choices.pivot = new Vector2(0.5f, 0f);
                Choices.anchoredPosition = new Vector2(280f, 20f);
                Choices.sizeDelta = new Vector2(-1120f, 132f);

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
                RectTransform speakerTextRect =
                    SpeakerText.rectTransform;
                speakerTextRect.anchorMin = Vector2.zero;
                speakerTextRect.anchorMax = Vector2.one;
                speakerTextRect.anchoredPosition = Vector2.zero;
                speakerTextRect.sizeDelta = new Vector2(-64f, -4f);
                AuthoredSpeakerTextOverflow = TextOverflowModes.Ellipsis;
                SpeakerText.overflowMode = AuthoredSpeakerTextOverflow;
                SpeakerText.fontSize =
                    DialogueTypographyMetrics.SpeakerMaximum;
                SpeakerText.enableAutoSizing = true;
                SpeakerText.fontSizeMin =
                    DialogueTypographyMetrics.SpeakerMinimum;
                SpeakerText.fontSizeMax =
                    DialogueTypographyMetrics.SpeakerMaximum;
                SpeakerText.textWrappingMode =
                    TextWrappingModes.NoWrap;
                SpeakerText.alignment =
                    TextAlignmentOptions.Center;
                SpeakerText.maxVisibleLines = 1;
                SpeakerText.raycastTarget = false;
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
            public Vector2 AuthoredSpeakerPlatePosition { get; private set; }
            public Vector2 AuthoredSpeakerPlateSize { get; private set; }
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
