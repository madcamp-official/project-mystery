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
        public const float DialogueHeight = 460f;
        public const float EdgePadding = 28f;
        public const float NavigationTop = 184f;
        public const float NavigationButtonWidth = 260f;
        public const float NavigationButtonHeight = 76f;

        // Design-time canvas resolution used only as the denominator for
        // DialogueTypographyMetrics' per-screen font scaling math. Not
        // wired to CanvasScaler at runtime - that's handled upstream.
        public static readonly Vector2 ReferenceResolution = new(2880f, 1800f);

        [Header("Portrait (code-created every run, no scene baseline to " +
            "respect - the speaker plate/text next to it ARE scene " +
            "objects and follow their own Inspector placement instead)")]
        [SerializeField] private float edgePadding = EdgePadding;
        [SerializeField] private Vector2 portraitSize = new(320f, 400f);
        [Tooltip("Gap between the portrait's bottom edge and the dialogue " +
            "panel's top edge. Portrait sits above the panel, not inside it.")]
        [SerializeField] private float portraitTopGap = 20f;

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
        private RectBaseline lineTextBaseline;
        private RectBaseline speakerPlateBaseline;

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

            ConfigureIngameRoot();

            // Capture the baseline before ConfigureChoices runs: that
            // adds a GridLayoutGroup, and Unity's layout system can
            // nudge anchoredPosition/sizeDelta the moment such a
            // component is configured. Capturing after that would
            // baseline the post-mutation values instead of what was
            // actually authored in the scene.
            CaptureBaselines(
                Screen.safeArea, new Vector2(Screen.width, Screen.height));
            baselineCaptured = true;

            ConfigurePortrait();
            ConfigureChoices();
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
            if (lineText != null)
                lineTextBaseline = new RectBaseline(lineText.rectTransform);
            if (speakerPlate != null)
                speakerPlateBaseline = new RectBaseline(speakerPlate);
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
            ApplyBaseline(
                lineText != null ? lineText.rectTransform : null,
                lineTextBaseline,
                scale);
            ApplyBaseline(speakerPlate, speakerPlateBaseline, scale);
            RefreshChoiceLayout();

            // Portrait is created fresh in code every run
            // (DialogueController.CreatePortrait) — there is no scene
            // placement to respect, so it stays formula-driven. It's
            // parented to Line Panel but sits ABOVE it (positive Y,
            // since pivot/anchor are both 1 = the panel's top edge) so
            // it doesn't overlap the dialogue text inside the panel.
            if (portrait != null)
            {
                portrait.anchorMin = new Vector2(0f, 1f);
                portrait.anchorMax = new Vector2(0f, 1f);
                portrait.pivot = new Vector2(0f, 1f);
                portrait.anchoredPosition = new Vector2(
                    edgePadding, portraitSize.y + portraitTopGap);
                portrait.sizeDelta = portraitSize;
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

        // No-op: dialogue text no longer scrolls - there's no ScrollRect
        // to reset. DialogueController still calls this on every line
        // change; kept as a harmless no-op instead of touching that
        // call site.
        public void ResetTextScroll()
        {
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
            choiceGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            choiceGrid.constraintCount =
                DialogueChoiceLayoutPolicy.MaximumColumns;
            choiceGrid.spacing = Vector2.one *
                DialogueChoiceLayoutPolicy.Spacing;
            choiceGrid.padding = new RectOffset(
                DialogueChoiceLayoutPolicy.Padding,
                DialogueChoiceLayoutPolicy.Padding,
                DialogueChoiceLayoutPolicy.Padding,
                DialogueChoiceLayoutPolicy.Padding);
            choiceGrid.childAlignment = TextAnchor.MiddleCenter;
            foreach (Button button in choiceButtons)
            {
                if (button == null)
                    continue;
                LayoutElement element = button.GetComponent<LayoutElement>();
                if (element == null)
                    element = button.gameObject.AddComponent<LayoutElement>();
                element.minHeight =
                    DialogueChoiceLayoutPolicy.MinimumCellHeight;
                element.preferredHeight =
                    DialogueChoiceLayoutPolicy.MinimumCellHeight;
                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                ConfigureLabel(
                    label,
                    DialogueTypographyMetrics.ChoiceMinimum,
                    DialogueTypographyMetrics.ChoiceMaximum,
                    DialogueTypographyMetrics.ChoiceLineSpacing,
                    TextOverflowModes.Overflow);
            }
            RefreshChoiceLayout();
        }

        public void RefreshChoiceLayout()
        {
            if (choiceGrid == null || choices == null ||
                choiceButtons == null)
                return;

            var activeLabels = new List<TMP_Text>();
            foreach (Button button in choiceButtons)
            {
                if (button == null || !button.gameObject.activeSelf)
                    continue;
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                    activeLabels.Add(label);
            }

            int activeChoiceCount = activeLabels.Count;
            if (activeChoiceCount == 0)
                return;

            float availableWidth = choices.rect.width > 0f
                ? choices.rect.width
                : choices.sizeDelta.x;
            DialogueChoiceLayoutSpec widthSpec =
                DialogueChoiceLayoutPolicy.Calculate(
                    availableWidth,
                    activeChoiceCount);
            float labelWidth =
                DialogueChoiceLayoutPolicy.GetLabelWidth(widthSpec);
            float maximumPreferredHeight = 0f;
            foreach (TMP_Text label in activeLabels)
            {
                maximumPreferredHeight = Mathf.Max(
                    maximumPreferredHeight,
                    label.GetPreferredValues(
                        label.text,
                        labelWidth,
                        Mathf.Infinity).y);
            }

            DialogueChoiceLayoutSpec spec =
                DialogueChoiceLayoutPolicy.Calculate(
                    availableWidth,
                    activeChoiceCount,
                    maximumPreferredHeight);
            choiceGrid.constraintCount = spec.Columns;
            choiceGrid.cellSize = spec.CellSize;
            choices.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                spec.RequiredHeight);
            LayoutRebuilder.ForceRebuildLayoutImmediate(choices);
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
