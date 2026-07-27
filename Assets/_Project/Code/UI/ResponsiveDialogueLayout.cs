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

    [DisallowMultipleComponent]
    public sealed class ResponsiveDialogueLayout : MonoBehaviour
    {
        public static readonly Vector2 ReferenceResolution = new(2880f, 1800f);
        public const float DialogueHeight = 360f;
        public const float EdgePadding = 24f;
        public const float NavigationTop = 184f;
        public const float NavigationButtonWidth = 220f;
        public const float NavigationButtonHeight = 64f;

        [Header("Layout (Inspector-editable, used instead of the constants above)")]
        [SerializeField] private float dialogueHeight = DialogueHeight;
        [SerializeField] private float edgePadding = EdgePadding;
        [SerializeField] private float navigationTop = NavigationTop;
        [SerializeField] private float navigationButtonWidth = NavigationButtonWidth;
        [SerializeField] private float navigationButtonHeight = NavigationButtonHeight;
        [SerializeField] private Vector2 portraitSize = new(320f, 430f);
        [SerializeField] private Vector2 speakerPlateSize = new(420f, 56f);
        [SerializeField] private float speakerPlateOffsetX = 384f;
        [SerializeField] private Vector2 nextButtonSize = new(176f, 60f);

        private Canvas canvas;
        private RectTransform ingameRoot;
        private RectTransform linePanel;
        private RectTransform textPanel;
        private RectTransform portrait;
        private RectTransform speakerPlate;
        private RectTransform nextButton;
        private RectTransform choices;
        private TMP_Text lineText;
        private TMP_Text speakerText;
        private ScrollRect dialogueScroll;
        private GridLayoutGroup choiceGrid;
        private IReadOnlyList<Button> choiceButtons;
        private Rect lastSafeArea;
        private Vector2Int lastScreen;

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

            ConfigureCanvasScaler();
            ConfigureIngameRoot();
            ConfigureText();
            ConfigurePortrait();
            ConfigureChoices();
            ApplyLayout(Screen.safeArea, new Vector2(Screen.width, Screen.height));
        }

        public void ApplyLayout(Rect safeArea, Vector2 screenSize)
        {
            if (linePanel == null)
                return;
            SafeAreaAnchors anchors =
                SafeAreaUtility.ToAnchors(safeArea, screenSize);

            ApplyNavigationLayout();
            linePanel.anchorMin = new Vector2(
                anchors.Minimum.x,
                anchors.Minimum.y);
            linePanel.anchorMax = new Vector2(
                anchors.Maximum.x,
                anchors.Minimum.y);
            linePanel.pivot = new Vector2(0.5f, 0f);
            linePanel.anchoredPosition = new Vector2(0f, edgePadding);
            linePanel.sizeDelta = new Vector2(0f, dialogueHeight);

            if (textPanel != null)
            {
                textPanel.anchorMin = new Vector2(0.19f, 0f);
                textPanel.anchorMax = Vector2.one;
                textPanel.offsetMin = new Vector2(edgePadding, 12f);
                textPanel.offsetMax = new Vector2(-edgePadding, -12f);
            }

            if (portrait != null)
            {
                portrait.anchorMin = Vector2.zero;
                portrait.anchorMax = Vector2.zero;
                portrait.pivot = Vector2.zero;
                portrait.anchoredPosition = new Vector2(edgePadding, 18f);
                portrait.sizeDelta = portraitSize;
            }

            if (speakerPlate != null)
            {
                speakerPlate.anchorMin = new Vector2(0f, 1f);
                speakerPlate.anchorMax = new Vector2(0f, 1f);
                speakerPlate.pivot = new Vector2(0f, 1f);
                speakerPlate.anchoredPosition =
                    new Vector2(speakerPlateOffsetX, -edgePadding);
                speakerPlate.sizeDelta = speakerPlateSize;
            }

            if (nextButton != null)
            {
                nextButton.anchorMin = new Vector2(1f, 0f);
                nextButton.anchorMax = new Vector2(1f, 0f);
                nextButton.pivot = new Vector2(1f, 0f);
                nextButton.anchoredPosition =
                    new Vector2(-edgePadding, edgePadding);
                nextButton.sizeDelta = nextButtonSize;
            }

            if (choices != null)
            {
                choices.anchorMin = new Vector2(0.21f, 0.08f);
                choices.anchorMax = new Vector2(0.98f, 0.84f);
                choices.offsetMin = Vector2.zero;
                choices.offsetMax = Vector2.zero;
                UpdateChoiceGrid();
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

        private void ApplyNavigationLayout()
        {
            if (ingameRoot == null)
                return;

            PlaceNavigationButton(
                ingameRoot.Find("Evidence Btn") as RectTransform,
                false,
                edgePadding);
            PlaceNavigationButton(
                ingameRoot.Find("Map Btn") as RectTransform,
                false,
                edgePadding * 2f + navigationButtonWidth);
            PlaceNavigationButton(
                ingameRoot.Find("Settings Btn") as RectTransform,
                true,
                edgePadding);
        }

        private void PlaceNavigationButton(
            RectTransform button,
            bool alignRight,
            float horizontalInset)
        {
            if (button == null)
                return;

            float anchorX = alignRight ? 1f : 0f;
            button.anchorMin = new Vector2(anchorX, 1f);
            button.anchorMax = new Vector2(anchorX, 1f);
            button.pivot = new Vector2(anchorX, 1f);
            button.anchoredPosition = new Vector2(
                alignRight ? -horizontalInset : horizontalInset,
                -navigationTop);
            button.sizeDelta = new Vector2(
                navigationButtonWidth,
                navigationButtonHeight);
            button.localScale = Vector3.one;
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
            ConfigureLabel(lineText, 22f, 36f, TextOverflowModes.Overflow);
            ConfigureLabel(speakerText, 20f, 32f, TextOverflowModes.Ellipsis);
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
            choiceGrid.spacing = new Vector2(12f, 12f);
            choiceGrid.padding = new RectOffset(8, 8, 8, 8);
            choiceGrid.childAlignment = TextAnchor.MiddleCenter;
            foreach (Button button in choiceButtons)
            {
                if (button == null)
                    continue;
                LayoutElement element = button.GetComponent<LayoutElement>();
                if (element == null)
                    element = button.gameObject.AddComponent<LayoutElement>();
                element.minHeight = 58f;
                element.preferredHeight = 72f;
                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                ConfigureLabel(
                    label,
                    18f,
                    28f,
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
                68f);
        }

        private static void ConfigureLabel(
            TMP_Text label,
            float minimumSize,
            float maximumSize,
            TextOverflowModes overflowMode)
        {
            if (label == null)
                return;
            label.enableAutoSizing = true;
            label.fontSizeMin = minimumSize;
            label.fontSizeMax = maximumSize;
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
