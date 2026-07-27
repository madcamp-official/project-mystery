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
        public static readonly Vector2 ReferenceResolution = new(1920f, 1080f);
        public const float DialogueHeight = 360f;
        public const float EdgePadding = 24f;

        private Canvas canvas;
        private RectTransform linePanel;
        private RectTransform textPanel;
        private RectTransform portrait;
        private RectTransform choices;
        private TMP_Text lineText;
        private TMP_Text speakerText;
        private ScrollRect dialogueScroll;
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
            RectTransform targetChoices,
            IReadOnlyList<Button> targetChoiceButtons)
        {
            canvas = targetCanvas;
            linePanel = targetLinePanel;
            portrait = targetPortrait;
            lineText = targetLineText;
            speakerText = targetSpeakerText;
            choices = targetChoices;
            choiceButtons = targetChoiceButtons;
            textPanel = lineText != null
                ? lineText.transform.parent as RectTransform
                : null;

            ConfigureCanvasScaler();
            ConfigureText();
            ConfigureChoices();
            ApplyLayout(Screen.safeArea, new Vector2(Screen.width, Screen.height));
        }

        public void ApplyLayout(Rect safeArea, Vector2 screenSize)
        {
            if (linePanel == null)
                return;
            SafeAreaAnchors anchors =
                SafeAreaUtility.ToAnchors(safeArea, screenSize);

            linePanel.anchorMin = new Vector2(
                anchors.Minimum.x,
                anchors.Minimum.y);
            linePanel.anchorMax = new Vector2(
                anchors.Maximum.x,
                anchors.Minimum.y);
            linePanel.pivot = new Vector2(0.5f, 0f);
            linePanel.anchoredPosition = new Vector2(0f, EdgePadding);
            linePanel.sizeDelta = new Vector2(0f, DialogueHeight);

            if (textPanel != null)
            {
                textPanel.anchorMin = new Vector2(0.20f, 0f);
                textPanel.anchorMax = Vector2.one;
                textPanel.offsetMin = new Vector2(EdgePadding, 0f);
                textPanel.offsetMax = new Vector2(-EdgePadding, 0f);
            }

            if (portrait != null)
            {
                portrait.anchorMin = Vector2.zero;
                portrait.anchorMax = Vector2.zero;
                portrait.pivot = Vector2.zero;
                portrait.anchoredPosition = new Vector2(EdgePadding, 12f);
                portrait.sizeDelta = new Vector2(320f, 420f);
            }

            if (choices != null)
            {
                choices.anchorMin = new Vector2(0.22f, 0.04f);
                choices.anchorMax = new Vector2(0.98f, 0.96f);
                choices.offsetMin = Vector2.zero;
                choices.offsetMax = Vector2.zero;
            }
            lastSafeArea = safeArea;
            lastScreen = new Vector2Int(
                Mathf.RoundToInt(screenSize.x),
                Mathf.RoundToInt(screenSize.y));
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
            ConfigureLabel(lineText, 20f, 34f);
            ConfigureLabel(speakerText, 18f, 30f);
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
            lineRect.anchoredPosition = Vector2.zero;
            lineRect.sizeDelta = new Vector2(-140f, 0f);
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

        private void ConfigureChoices()
        {
            if (choiceButtons == null)
                return;
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
                ConfigureLabel(label, 18f, 28f);
            }
        }

        private static void ConfigureLabel(
            TMP_Text label,
            float minimumSize,
            float maximumSize)
        {
            if (label == null)
                return;
            label.enableAutoSizing = true;
            label.fontSizeMin = minimumSize;
            label.fontSizeMax = maximumSize;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
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
