using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Wake.UI
{
    public readonly struct SafeAreaAnchors
    {
        public SafeAreaAnchors(Vector2 minimum, Vector2 maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        public Vector2 Minimum { get; }
        public Vector2 Maximum { get; }
    }

    public static class SafeAreaUtility
    {
        public static SafeAreaAnchors ToAnchors(Rect safeArea, Vector2 screenSize)
        {
            if (screenSize.x <= 0f || screenSize.y <= 0f)
                throw new ArgumentOutOfRangeException(
                    nameof(screenSize), "Screen dimensions must be positive.");

            Vector2 minimum = new(
                Mathf.Clamp01(safeArea.xMin / screenSize.x),
                Mathf.Clamp01(safeArea.yMin / screenSize.y));
            Vector2 maximum = new(
                Mathf.Clamp01(safeArea.xMax / screenSize.x),
                Mathf.Clamp01(safeArea.yMax / screenSize.y));
            return new SafeAreaAnchors(minimum, maximum);
        }
    }

    /// <summary>
    /// Captured RectTransform state used as the layout's source of truth.
    /// Whatever is placed in the Inspector/scene at Initialize time wins;
    /// ApplyLayout only rescales it when the safe-area inset fraction
    /// actually changes (e.g. a notch), which never happens on desktop.
    /// </summary>
    public readonly struct RectBaseline
    {
        public RectBaseline(RectTransform rect)
        {
            AnchoredPosition = rect.anchoredPosition;
            SizeDelta = rect.sizeDelta;
        }

        public Vector2 AnchoredPosition { get; }
        public Vector2 SizeDelta { get; }
    }

    [DisallowMultipleComponent]
    public sealed class ResponsiveDialogueLayout : MonoBehaviour
    {
        public static readonly Vector2 ReferenceResolution = new(2880f, 1800f);
        public const float DialogueHeight = 460f;
        public const float EdgePadding = 28f;
        public const float NavigationTop = 184f;
        public const float NavigationButtonWidth = 260f;
        public const float NavigationButtonHeight = 76f;

        [Header("Portrait (code-created, no scene baseline to respect)")]
        [SerializeField] private float edgePadding = EdgePadding;
        [SerializeField] private Vector2 portraitSize = new(260f, 300f);
        [SerializeField] private Vector2 speakerPlateSize = new(460f, 68f);

        private Canvas canvas;
        private RectTransform ingameRoot;
        private RectTransform linePanel;
        private RectTransform textPanel;
        private RectTransform portrait;
        private RectTransform speakerPlate;
        private RectTransform nextButton;
        private RectTransform choices;
        private RectTransform evidenceBtn;
        private RectTransform mapBtn;
        private RectTransform settingsBtn;
        private TMP_Text lineText;
        private TMP_Text speakerText;
        private ScrollRect dialogueScroll;
        private GridLayoutGroup choiceGrid;
        private IReadOnlyList<Button> choiceButtons;
        private Rect lastSafeArea;
        private Vector2Int lastScreen;

        private bool baselineCaptured;
        private Rect baselineSafeArea;
        private Vector2 baselineScreen;
        private RectBaseline linePanelBaseline;
        private RectBaseline textPanelBaseline;
        private RectBaseline nextButtonBaseline;
        private RectBaseline choicesBaseline;
        private RectBaseline evidenceBtnBaseline;
        private RectBaseline mapBtnBaseline;
        private RectBaseline settingsBtnBaseline;

        public ScrollRect DialogueScroll => dialogueScroll;

        public void Initialize(
            Canvas targetCanvas,
            RectTransform targetLinePanel,
            RectTransform targetPortrait,
            TMP_Text targetLineText,
            TMP_Text targetSpeakerText,
            RectTransform targetNextButton,
            RectTransform targetChoices,
            IReadOnlyList<Button> targetChoiceButtons)
        {
            canvas = targetCanvas;
            linePanel = targetLinePanel;
            ingameRoot = linePanel != null
                ? linePanel.parent as RectTransform
                : null;
            portrait = targetPortrait;
            lineText = targetLineText;
            speakerText = targetSpeakerText;
            speakerPlate = speakerText != null
                ? speakerText.transform.parent as RectTransform
                : null;
            nextButton = targetNextButton;
            choices = targetChoices;
            choiceButtons = targetChoiceButtons;
            textPanel = lineText != null
                ? lineText.transform.parent as RectTransform
                : null;
            evidenceBtn = ingameRoot != null
                ? ingameRoot.Find("Evidence Btn") as RectTransform
                : null;
            mapBtn = ingameRoot != null
                ? ingameRoot.Find("Map Btn") as RectTransform
                : null;
            settingsBtn = ingameRoot != null
                ? ingameRoot.Find("Settings Btn") as RectTransform
                : null;

            ConfigureCanvasScaler();
            ConfigureIngameRoot();
            ConfigureText();
            ConfigurePortrait();
            ConfigureChoices();
            baselineCaptured = false;
            // The first ApplyLayout call (from LateUpdate on the next
            // tick, or from a caller that wants a specific starting
            // point) captures whatever is on the RectTransforms right
            // now as the baseline. We don't force that call here so
            // tests can control exactly what the baseline is.
        }

        /// <summary>
        /// Snapshots the current (Inspector/scene-authored) placement of
        /// every element that persists in the scene. This becomes the
        /// layout ApplyLayout reproduces; it is not overwritten with
        /// hardcoded positions.
        /// </summary>
        private void CaptureBaselines(Rect safeArea, Vector2 screenSize)
        {
            baselineSafeArea = safeArea;
            baselineScreen = screenSize;
            if (linePanel != null)
                linePanelBaseline = new RectBaseline(linePanel);
            if (textPanel != null)
                textPanelBaseline = new RectBaseline(textPanel);
            if (nextButton != null)
                nextButtonBaseline = new RectBaseline(nextButton);
            if (choices != null)
                choicesBaseline = new RectBaseline(choices);
            if (evidenceBtn != null)
                evidenceBtnBaseline = new RectBaseline(evidenceBtn);
            if (mapBtn != null)
                mapBtnBaseline = new RectBaseline(mapBtn);
            if (settingsBtn != null)
                settingsBtnBaseline = new RectBaseline(settingsBtn);
        }

        /// <summary>
        /// How much the safe-area inset (as a fraction of the screen)
        /// has changed since the baseline was captured. (1,1) whenever
        /// there is no notch/inset change, which is always true on
        /// desktop — baseline values then apply completely untouched.
        /// </summary>
        private Vector2 ComputeInsetScale(Rect safeArea, Vector2 screenSize)
        {
            Vector2 baselineFraction = new(
                baselineScreen.x > 0f ? baselineSafeArea.width / baselineScreen.x : 1f,
                baselineScreen.y > 0f ? baselineSafeArea.height / baselineScreen.y : 1f);
            Vector2 currentFraction = new(
                screenSize.x > 0f ? safeArea.width / screenSize.x : 1f,
                screenSize.y > 0f ? safeArea.height / screenSize.y : 1f);
            return new Vector2(
                baselineFraction.x > 0.0001f ? currentFraction.x / baselineFraction.x : 1f,
                baselineFraction.y > 0.0001f ? currentFraction.y / baselineFraction.y : 1f);
        }

        private static void ApplyBaseline(
            RectTransform rect, RectBaseline baseline, Vector2 scale)
        {
            if (rect == null)
                return;
            rect.anchoredPosition = new Vector2(
                baseline.AnchoredPosition.x * scale.x,
                baseline.AnchoredPosition.y * scale.y);
            rect.sizeDelta = new Vector2(
                baseline.SizeDelta.x * scale.x,
                baseline.SizeDelta.y * scale.y);
        }

        public void ApplyLayout(Rect safeArea, Vector2 screenSize)
        {
            if (linePanel == null)
                return;

            if (!baselineCaptured)
            {
                CaptureBaselines(safeArea, screenSize);
                baselineCaptured = true;
            }

            Vector2 scale = ComputeInsetScale(safeArea, screenSize);
            ApplyBaseline(linePanel, linePanelBaseline, scale);
            ApplyBaseline(textPanel, textPanelBaseline, scale);
            ApplyBaseline(nextButton, nextButtonBaseline, scale);
            ApplyBaseline(choices, choicesBaseline, scale);
            ApplyBaseline(evidenceBtn, evidenceBtnBaseline, scale);
            ApplyBaseline(mapBtn, mapBtnBaseline, scale);
            ApplyBaseline(settingsBtn, settingsBtnBaseline, scale);
            UpdateChoiceGrid();

            // Portrait/speaker plate are created fresh in code every run
            // (DialogueController.CreatePortrait) — there is no scene
            // placement to respect, so these stay formula-driven.
            if (portrait != null)
            {
                portrait.anchorMin = new Vector2(0f, 1f);
                portrait.anchorMax = new Vector2(0f, 1f);
                portrait.pivot = new Vector2(0f, 1f);
                portrait.anchoredPosition =
                    new Vector2(edgePadding, -edgePadding);
                portrait.sizeDelta = portraitSize;
            }

            if (speakerPlate != null)
            {
                speakerPlate.anchorMin = new Vector2(0f, 1f);
                speakerPlate.anchorMax = new Vector2(0f, 1f);
                speakerPlate.pivot = new Vector2(0f, 1f);
                speakerPlate.anchoredPosition = new Vector2(
                    edgePadding * 2f + portraitSize.x,
                    -edgePadding);
                speakerPlate.sizeDelta = speakerPlateSize;
            }

            lastSafeArea = safeArea;
            lastScreen = new Vector2Int(
                Mathf.RoundToInt(screenSize.x),
                Mathf.RoundToInt(screenSize.y));
        }

        private void ConfigureIngameRoot()
        {
            if (ingameRoot == null)
                return;

            ingameRoot.anchorMin = Vector2.zero;
            ingameRoot.anchorMax = Vector2.one;
            ingameRoot.pivot = new Vector2(0.5f, 0.5f);
            ingameRoot.anchoredPosition = Vector2.zero;
            ingameRoot.sizeDelta = Vector2.zero;
            ingameRoot.localScale = Vector3.one;
        }

        public void ResetTextScroll()
        {
            if (dialogueScroll == null)
                return;
            Canvas.ForceUpdateCanvases();
            dialogueScroll.verticalNormalizedPosition = 1f;
        }

        private void ConfigureCanvasScaler()
        {
            CanvasScaler scaler = canvas != null
                ? canvas.GetComponent<CanvasScaler>()
                : null;
            if (scaler == null)
                return;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private void ConfigureText()
        {
            ConfigureLabel(
                lineText,
                DialogueTypographyMetrics.LineMinimum,
                DialogueTypographyMetrics.LineMaximum,
                DialogueTypographyMetrics.BodyLineSpacing,
                TextOverflowModes.Overflow);
            ConfigureLabel(
                speakerText,
                DialogueTypographyMetrics.SpeakerMinimum,
                DialogueTypographyMetrics.SpeakerMaximum,
                DialogueTypographyMetrics.HeadingLineSpacing,
                TextOverflowModes.Ellipsis);
            if (textPanel == null || lineText == null)
                return;

            dialogueScroll = textPanel.GetComponent<ScrollRect>();
            if (dialogueScroll == null)
                dialogueScroll = textPanel.gameObject.AddComponent<ScrollRect>();
            if (textPanel.GetComponent<RectMask2D>() == null)
                textPanel.gameObject.AddComponent<RectMask2D>();

            RectTransform lineRect = lineText.rectTransform;
            lineRect.anchorMin = new Vector2(0f, 1f);
            lineRect.anchorMax = new Vector2(1f, 1f);
            lineRect.pivot = new Vector2(0.5f, 1f);
            lineRect.anchoredPosition = new Vector2(-76f, -72f);
            lineRect.sizeDelta = new Vector2(-232f, 0f);
            ContentSizeFitter fitter =
                lineText.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = lineText.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            dialogueScroll.viewport = textPanel;
            dialogueScroll.content = lineRect;
            dialogueScroll.horizontal = false;
            dialogueScroll.vertical = true;
            dialogueScroll.movementType = ScrollRect.MovementType.Clamped;
            dialogueScroll.scrollSensitivity = 36f;
        }

        private void ConfigurePortrait()
        {
            if (portrait == null)
                return;

            AspectRatioFitter fitter =
                portrait.GetComponent<AspectRatioFitter>();
            if (fitter != null)
            {
                fitter.aspectMode =
                    AspectRatioFitter.AspectMode.HeightControlsWidth;
            }
        }

        private void ConfigureChoices()
        {
            if (choices == null || choiceButtons == null)
                return;
            choiceGrid = choices.GetComponent<GridLayoutGroup>();
            if (choiceGrid == null)
                choiceGrid = choices.gameObject.AddComponent<GridLayoutGroup>();
            choiceGrid.constraint =
                GridLayoutGroup.Constraint.FixedColumnCount;
            choiceGrid.constraintCount = 2;
            choiceGrid.spacing = new Vector2(16f, 16f);
            choiceGrid.padding = new RectOffset(10, 10, 10, 10);
            choiceGrid.childAlignment = TextAnchor.MiddleCenter;
            foreach (Button button in choiceButtons)
            {
                if (button == null)
                    continue;
                LayoutElement element = button.GetComponent<LayoutElement>();
                if (element == null)
                    element = button.gameObject.AddComponent<LayoutElement>();
                element.minHeight = 72f;
                element.preferredHeight = 90f;
                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                ConfigureLabel(
                    label,
                    DialogueTypographyMetrics.ChoiceMinimum,
                    DialogueTypographyMetrics.ChoiceMaximum,
                    DialogueTypographyMetrics.ChoiceLineSpacing,
                    TextOverflowModes.Ellipsis);
            }
        }

        private void UpdateChoiceGrid()
        {
            if (choiceGrid == null)
                return;
            float availableWidth = choices.rect.width > 0f
                ? choices.rect.width
                : 1400f;
            float cellWidth =
                (availableWidth - choiceGrid.padding.horizontal -
                 choiceGrid.spacing.x) / 2f;
            choiceGrid.cellSize = new Vector2(
                Mathf.Max(280f, cellWidth),
                88f);
        }

        private static void ConfigureLabel(
            TMP_Text label,
            float minimumSize,
            float maximumSize,
            float lineSpacing,
            TextOverflowModes overflowMode)
        {
            if (label == null)
                return;
            label.enableAutoSizing = true;
            label.fontSizeMin = minimumSize;
            label.fontSizeMax = maximumSize;
            label.lineSpacing = lineSpacing;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = overflowMode;
        }

        private void LateUpdate()
        {
            Vector2Int currentScreen = new(Screen.width, Screen.height);
            if (currentScreen != lastScreen || Screen.safeArea != lastSafeArea)
                ApplyLayout(
                    Screen.safeArea,
                    new Vector2(Screen.width, Screen.height));
        }
    }
}
