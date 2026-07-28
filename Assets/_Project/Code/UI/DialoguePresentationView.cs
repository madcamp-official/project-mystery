using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.Narrative;

namespace Wake.UI
{
    [DisallowMultipleComponent]
    public sealed class DialoguePresentationView : MonoBehaviour
    {
        private const string FocusPanelLeftSlot =
            "dialogue.focus-panel-left";
        private const string FocusPanelRightSlot =
            "dialogue.focus-panel-right";
        private const string CompactPanelSlot = "dialogue.compact-panel";
        private const string NarrationPanelSlot = "dialogue.narration-panel";
        private const string FocusPortraitLeftSlot =
            "dialogue.focus-portrait-left";
        private const string FocusPortraitRightSlot =
            "dialogue.focus-portrait-right";
        private const string CompactPortraitSlot =
            "dialogue.compact-portrait";
        private const float PortraitSafeInset = 0.90f;
        private const float PortraitBottomPadding = 0.025f;

        private RectTransform ingameRoot;
        private RectTransform linePanel;
        private RectTransform portrait;
        private RectTransform textRect;
        private RectTransform speakerPlate;
        private RectTransform nextButton;
        private RectTransform choices;
        private TMP_Text lineText;
        private Image backgroundDim;
        private Transform statusHud;
        private CanvasGroup locationContextGroup;
        private DialoguePresentationSpec active;
        private Vector2Int lastScreen;
        private bool choicesVisible;
        private bool initialized;

        public DialoguePresentationSpec Active => active;

        public void Initialize(
            RectTransform targetIngameRoot,
            RectTransform targetLinePanel,
            RectTransform targetPortrait,
            TMP_Text targetLineText,
            TMP_Text targetSpeakerText,
            RectTransform targetNextButton,
            RectTransform targetChoices)
        {
            ingameRoot = targetIngameRoot;
            linePanel = targetLinePanel;
            portrait = targetPortrait;
            lineText = targetLineText;
            textRect = targetLineText != null
                ? targetLineText.rectTransform
                : null;
            speakerPlate = targetSpeakerText != null
                ? targetSpeakerText.transform.parent as RectTransform
                : null;
            nextButton = targetNextButton;
            choices = targetChoices;
            backgroundDim = EnsureBackgroundDim();
            RestoreNavigationVisibility();
            initialized = true;
            Apply(DialoguePresentationPolicy.Hidden);
        }

        public void SetChoicesVisible(bool visible)
        {
            choicesVisible = visible;
            if (!initialized || !active.IsVisible)
                return;

            ApplyTextLayout(active.Mode);
            ApplyChoiceLayout();
        }

        public void Apply(DialoguePresentationSpec presentation)
        {
            active = presentation;
            if (!initialized)
                return;

            ApplyDim(presentation);
            SetHudVisible(ShouldShowHud(
                presentation,
                ResolvePrimaryPanel()));
            if (!presentation.IsVisible ||
                presentation.Mode == DialoguePresentationMode.Investigation)
            {
                if (portrait != null)
                    portrait.gameObject.SetActive(false);
                return;
            }

            string panelSlot = PanelSlotFor(presentation);
            RuntimeUiLayoutRegistry.CopyLayout(linePanel, panelSlot);
            ApplyTextLayout(presentation.Mode);
            ApplySpeakerPlate(presentation.ShowSpeakerName);
            ApplyNextButton();
            ApplyChoiceLayout();
            ApplyPortrait(presentation);
            lastScreen = new Vector2Int(Screen.width, Screen.height);
        }

        public static string PanelSlotFor(DialoguePresentationMode mode) =>
            mode switch
            {
                DialoguePresentationMode.Compact => CompactPanelSlot,
                DialoguePresentationMode.Narration => NarrationPanelSlot,
                _ => FocusPanelLeftSlot
            };

        public static string PanelSlotFor(
            DialoguePresentationSpec presentation)
        {
            if (presentation.Mode != DialoguePresentationMode.Focus)
                return PanelSlotFor(presentation.Mode);

            return presentation.PortraitSide == DialoguePortraitSide.Left
                ? FocusPanelRightSlot
                : FocusPanelLeftSlot;
        }

        public static string PortraitSlotFor(
            DialoguePresentationSpec presentation)
        {
            if (!presentation.ShowPortrait)
                return string.Empty;
            if (presentation.Mode == DialoguePresentationMode.Compact)
                return CompactPortraitSlot;
            return presentation.PortraitSide == DialoguePortraitSide.Left
                ? FocusPortraitLeftSlot
                : FocusPortraitRightSlot;
        }

        public static bool ShouldShowHud(
            DialoguePresentationSpec presentation,
            UiPrimaryPanel primaryPanel) =>
            !presentation.IsVisible &&
            primaryPanel is not UiPrimaryPanel.None and
                not UiPrimaryPanel.Start;

        private Image EnsureBackgroundDim()
        {
            if (ingameRoot == null)
                return null;

            Transform existing = ingameRoot.Find("Dialogue Focus Dim");
            GameObject node = existing != null
                ? existing.gameObject
                : new GameObject(
                    "Dialogue Focus Dim",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
            if (existing == null)
                node.transform.SetParent(ingameRoot, false);

            RectTransform rect = node.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsFirstSibling();
            Image image = node.GetComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = false;
            node.SetActive(false);
            return image;
        }

        private void ApplyDim(DialoguePresentationSpec presentation)
        {
            if (backgroundDim == null)
                return;

            bool visible =
                presentation.IsVisible &&
                presentation.BackgroundDimAlpha > 0f;
            backgroundDim.gameObject.SetActive(visible);
            backgroundDim.color = new Color(
                0.025f,
                0.035f,
                0.075f,
                presentation.BackgroundDimAlpha);
        }

        private void ApplyPortrait(DialoguePresentationSpec presentation)
        {
            if (portrait == null)
                return;

            if (!presentation.ShowPortrait)
            {
                portrait.gameObject.SetActive(false);
                return;
            }

            if (!RuntimeUiLayoutRegistry.CopyWorldLayout(
                    portrait,
                    PortraitSlotFor(presentation)))
            {
                portrait.gameObject.SetActive(false);
                return;
            }

            FitPortraitToSlot(presentation.PortraitHeightRatio);
        }

        private void ApplyTextLayout(DialoguePresentationMode mode)
        {
            if (textRect == null)
                return;

            bool compact = mode == DialoguePresentationMode.Compact;
            float textBottom = choicesVisible ? 0.58f : 0.15f;
            textRect.anchorMin = compact
                ? new Vector2(0.30f, textBottom)
                : new Vector2(0.08f, textBottom);
            textRect.anchorMax = new Vector2(0.91f, 0.86f);
            textRect.pivot = new Vector2(0f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;

            if (lineText == null)
                return;

            lineText.enableAutoSizing = true;
            lineText.fontSizeMin = DialogueTypographyMetrics.LineMinimum;
            lineText.fontSizeMax = DialogueTypographyMetrics.LineMaximum;
            lineText.textWrappingMode = TextWrappingModes.Normal;
            lineText.overflowMode = TextOverflowModes.Truncate;
            lineText.margin = new Vector4(8f, 4f, 8f, 4f);
        }

        private void ApplySpeakerPlate(bool visible)
        {
            if (speakerPlate == null)
                return;

            speakerPlate.gameObject.SetActive(visible);
            if (!visible)
                return;

            speakerPlate.anchorMin = new Vector2(0.06f, 1f);
            speakerPlate.anchorMax = new Vector2(0.06f, 1f);
            speakerPlate.pivot = new Vector2(0f, 0f);
            speakerPlate.anchoredPosition = new Vector2(0f, 12f);
            speakerPlate.sizeDelta = new Vector2(520f, 108f);
        }

        private void ApplyNextButton()
        {
            if (nextButton == null)
                return;

            nextButton.anchorMin = new Vector2(0.94f, 0.12f);
            nextButton.anchorMax = new Vector2(0.94f, 0.12f);
            nextButton.pivot = new Vector2(0.5f, 0.5f);
            nextButton.anchoredPosition = Vector2.zero;
            nextButton.sizeDelta = new Vector2(88f, 88f);
        }

        private void ApplyChoiceLayout()
        {
            if (choices == null)
                return;

            choices.anchorMin = new Vector2(0.05f, 0.06f);
            choices.anchorMax = new Vector2(0.95f, 0.06f);
            choices.pivot = new Vector2(0.5f, 0f);
            choices.anchoredPosition = Vector2.zero;
            float height = choices.sizeDelta.y > 0f
                ? choices.sizeDelta.y
                : 180f;
            choices.sizeDelta = new Vector2(0f, height);
        }

        private void FitPortraitToSlot(float heightRatio)
        {
            if (portrait == null ||
                portrait.parent is not RectTransform parent)
            {
                return;
            }

            AspectRatioFitter fitter =
                portrait.GetComponent<AspectRatioFitter>();
            float aspect = fitter != null && fitter.aspectRatio > 0f
                ? fitter.aspectRatio
                : 0.72f;
            Vector2 slotSize = portrait.sizeDelta;
            float maximumHeight =
                Mathf.Max(
                    1f,
                    parent.rect.height * heightRatio *
                    PortraitSafeInset);
            float availableHeight = slotSize.y * PortraitSafeInset;
            float availableWidth = slotSize.x * PortraitSafeInset;
            float height = Mathf.Min(availableHeight, maximumHeight);
            float width = height * aspect;
            if (width > availableWidth)
            {
                width = availableWidth;
                height = width / aspect;
            }

            portrait.sizeDelta = new Vector2(width, height);
            portrait.anchoredPosition += new Vector2(
                0f,
                slotSize.y * PortraitBottomPadding);
            if (fitter != null)
                fitter.aspectMode = AspectRatioFitter.AspectMode.None;
        }

        private void SetHudVisible(bool visible)
        {
            if (ingameRoot == null)
                return;

            Transform canvas = ingameRoot.parent;
            statusHud ??= canvas != null
                ? canvas.Find("Status HUD")
                : null;
            locationContextGroup ??= EnsureCanvasGroup(
                ingameRoot.Find("Narrative Location Context"));
            // The numeric gameplay HUD is retained as runtime state only.
            // Dialogue must never make the retired presentation visible.
            if (statusHud != null && statusHud.gameObject.activeSelf)
                statusHud.gameObject.SetActive(false);
            SetGroupVisible(locationContextGroup, visible);
            SetNavigationVisible(visible);
        }

        private void RestoreNavigationVisibility()
        {
            SetNavigationVisible(true);
        }

        private void SetNavigationVisible(bool visible)
        {
            if (ingameRoot == null)
                return;

            string[] names = { "Map Btn", "Evidence Btn", "Settings Btn" };
            foreach (string name in names)
            {
                Transform navigation = ingameRoot.Find(name);
                if (navigation != null &&
                    navigation.gameObject.activeSelf != visible)
                {
                    navigation.gameObject.SetActive(visible);
                }
            }
        }

        private static CanvasGroup EnsureCanvasGroup(Transform target)
        {
            if (target == null)
                return null;

            return target.GetComponent<CanvasGroup>() ??
                   target.gameObject.AddComponent<CanvasGroup>();
        }

        private static void SetGroupVisible(
            CanvasGroup group,
            bool visible)
        {
            if (group == null)
                return;

            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }

        private UiPrimaryPanel ResolvePrimaryPanel()
        {
            UIManager manager = UIManager.Instance;
            if (manager != null)
                return manager.ActivePanel;

            return ingameRoot != null &&
                   ingameRoot.gameObject.activeInHierarchy
                ? UiPrimaryPanel.Ingame
                : UiPrimaryPanel.Start;
        }

        private void LateUpdate()
        {
            if (!initialized)
                return;

            SetHudVisible(ShouldShowHud(
                active,
                ResolvePrimaryPanel()));
            if (!active.IsVisible)
                return;

            Vector2Int screen = new(Screen.width, Screen.height);
            if (screen == lastScreen)
                return;

            Apply(active);
        }
    }
}
