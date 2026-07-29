using System.Collections;
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
        private const string DimSlot = "dialogue.dim";
        private const string FocusTextLeftSlot =
            "dialogue.focus-text-left";
        private const string FocusTextRightSlot =
            "dialogue.focus-text-right";
        private const string CompactTextSlot = "dialogue.compact-text";
        private const string NarrationTextSlot =
            "dialogue.narration-text";
        private const string SpeakerNameLeftSlot =
            "dialogue.speaker-name-left";
        private const string SpeakerNameRightSlot =
            "dialogue.speaker-name-right";
        private const string AdvanceLeftSlot = "dialogue.advance-left";
        private const string AdvanceRightSlot = "dialogue.advance-right";
        private const string AdvanceCenterSlot =
            "dialogue.advance-center";
        private const string ChoicesLeftSlot = "dialogue.choices-left";
        private const string ChoicesRightSlot = "dialogue.choices-right";
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
        private Image panelBackground;
        private Color panelBackgroundDefaultColor;
        private Color lineTextDefaultColor;
        private Image backgroundDim;
        private CanvasGroup backgroundDimGroup;
        private Coroutine dimRoutine;
        private Transform statusHud;
        private CanvasGroup locationContextGroup;
        private DialoguePresentationSpec active;
        private Vector2Int lastScreen;
        private bool choicesVisible;
        private bool initialized;

        public DialoguePresentationSpec Active => active;

        private void OnDisable()
        {
            ResetDimImmediate();
            RestoreNavigationVisibility();
        }

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
            panelBackground = linePanel != null
                ? linePanel.Find("Panel")?.GetComponent<Image>()
                : null;
            if (panelBackground != null)
                panelBackgroundDefaultColor = panelBackground.color;
            if (lineText != null)
                lineTextDefaultColor = lineText.color;
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

            ApplyTextLayout(active);
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
            ApplyTextLayout(presentation);
            ApplyNarrationStyle(presentation);
            // Portrait must resolve before the speaker plate - the plate
            // positions itself directly under the portrait's final rect.
            ApplyPortrait(presentation);
            ApplySpeakerPlate(presentation);
            ApplyNextButton(presentation);
            ApplyChoiceLayout();
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

        public static string TextSlotFor(
            DialoguePresentationSpec presentation) =>
            presentation.Mode switch
            {
                DialoguePresentationMode.Compact => CompactTextSlot,
                DialoguePresentationMode.Narration => NarrationTextSlot,
                DialoguePresentationMode.Focus =>
                    presentation.PortraitSide ==
                    DialoguePortraitSide.Left
                        ? FocusTextRightSlot
                        : FocusTextLeftSlot,
                _ => string.Empty
            };

        public static string SpeakerNameSlotFor(
            DialoguePresentationSpec presentation) =>
            presentation.PortraitSide == DialoguePortraitSide.Left
                ? SpeakerNameRightSlot
                : SpeakerNameLeftSlot;

        public static string AdvanceSlotFor(
            DialoguePresentationSpec presentation) =>
            presentation.Mode is DialoguePresentationMode.Compact or
                DialoguePresentationMode.Narration
                ? AdvanceCenterSlot
                : presentation.PortraitSide ==
                  DialoguePortraitSide.Left
                    ? AdvanceRightSlot
                    : AdvanceLeftSlot;

        public static string ChoicesSlotFor(
            DialoguePresentationSpec presentation) =>
            presentation.PortraitSide == DialoguePortraitSide.Left
                ? ChoicesRightSlot
                : ChoicesLeftSlot;

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
            if (!RuntimeUiLayoutRegistry.CopyWorldLayout(rect, DimSlot))
            {
                Debug.LogError(
                    $"DialoguePresentationView requires '{DimSlot}'.");
            }
            rect.SetAsFirstSibling();
            Image image = node.GetComponent<Image>();
            image.color = Color.clear;
            // Blocks click/hover to whatever's underneath (evidence and
            // inspectable hotspots on the background canvas don't hide
            // themselves during dialogue the way ambient characters do) -
            // it already renders full-screen above that canvas, it just
            // never intercepted the raycast.
            image.raycastTarget = true;
            backgroundDimGroup = node.GetComponent<CanvasGroup>();
            if (backgroundDimGroup == null)
                backgroundDimGroup = node.AddComponent<CanvasGroup>();
            backgroundDimGroup.alpha = 0f;
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
            backgroundDim.color = new Color(
                0.025f,
                0.035f,
                0.075f,
                presentation.BackgroundDimAlpha);
            if (dimRoutine != null)
                StopCoroutine(dimRoutine);
            backgroundDim.gameObject.SetActive(true);
            dimRoutine = StartCoroutine(FadeDim(
                visible ? 1f : 0f,
                visible));
        }

        private IEnumerator FadeDim(float target, bool keepVisible)
        {
            if (backgroundDimGroup == null)
                yield break;

            float start = backgroundDimGroup.alpha;
            float elapsed = 0f;
            const float duration = 0.18f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                backgroundDimGroup.alpha = Mathf.Lerp(
                    start,
                    target,
                    Mathf.SmoothStep(0f, 1f, elapsed / duration));
                yield return null;
            }
            backgroundDimGroup.alpha = target;
            backgroundDim.gameObject.SetActive(keepVisible);
            dimRoutine = null;
        }

        private void ResetDimImmediate()
        {
            if (dimRoutine != null)
            {
                StopCoroutine(dimRoutine);
                dimRoutine = null;
            }

            if (backgroundDimGroup != null)
                backgroundDimGroup.alpha = 0f;
            if (backgroundDim != null)
                backgroundDim.gameObject.SetActive(false);
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

        // Narration reads as plain white text over the (now much darker)
        // background dim instead of sitting inside the same boxed panel
        // used for character lines - the panel/text colors are shared
        // objects across every mode, so non-narration modes must be
        // restored explicitly here too.
        private void ApplyNarrationStyle(DialoguePresentationSpec presentation)
        {
            bool isNarration =
                presentation.Mode == DialoguePresentationMode.Narration;
            if (panelBackground != null)
            {
                panelBackground.color = isNarration
                    ? new Color(0f, 0f, 0f, 0f)
                    : panelBackgroundDefaultColor;
            }
            if (lineText != null)
            {
                lineText.color = isNarration
                    ? Color.white
                    : lineTextDefaultColor;
            }
        }

        private void ApplyTextLayout(
            DialoguePresentationSpec presentation)
        {
            if (textRect == null)
                return;

            RuntimeUiLayoutRegistry.CopyWorldLayout(
                textRect,
                TextSlotFor(presentation));

            if (lineText == null)
                return;

            lineText.enableAutoSizing = true;
            lineText.fontSizeMin = DialogueTypographyMetrics.LineMinimum;
            lineText.fontSizeMax = DialogueTypographyMetrics.LineMaximum;
            lineText.lineSpacing = DialogueTypographyMetrics.BodyLineSpacing;
            lineText.textWrappingMode = TextWrappingModes.Normal;
            lineText.overflowMode = TextOverflowModes.Truncate;
            lineText.margin = new Vector4(12f, 8f, 12f, 8f);
        }

        private void ApplySpeakerPlate(
            DialoguePresentationSpec presentation)
        {
            if (speakerPlate == null)
                return;

            bool visible = presentation.ShowSpeakerName;
            speakerPlate.gameObject.SetActive(visible);
            if (!visible)
                return;

            RuntimeUiLayoutRegistry.CopyWorldLayout(
                speakerPlate,
                SpeakerNameSlotFor(presentation));
            if (presentation.ShowPortrait)
                PositionSpeakerPlateBelowPortrait();
        }

        // Slot-copied layout puts the plate beside the dialogue text: pin
        // it to the portrait's own rect instead so it always sits directly
        // under the character image regardless of portrait size/position.
        private void PositionSpeakerPlateBelowPortrait()
        {
            if (speakerPlate == null ||
                portrait == null ||
                !portrait.gameObject.activeSelf)
            {
                return;
            }

            const float gap = 8f;
            Vector2 size = speakerPlate.rect.size;
            Vector2 portraitAnchorCenter =
                (portrait.anchorMin + portrait.anchorMax) * 0.5f;
            speakerPlate.anchorMin = portraitAnchorCenter;
            speakerPlate.anchorMax = portraitAnchorCenter;
            speakerPlate.pivot = new Vector2(0.5f, 1f);
            speakerPlate.sizeDelta = size;
            speakerPlate.anchoredPosition = new Vector2(
                portrait.anchoredPosition.x,
                portrait.anchoredPosition.y -
                    portrait.sizeDelta.y * 0.5f - gap);
        }

        private void ApplyNextButton(
            DialoguePresentationSpec presentation)
        {
            if (nextButton == null)
                return;

            RuntimeUiLayoutRegistry.CopyWorldLayout(
                nextButton,
                AdvanceSlotFor(presentation));
        }

        private void ApplyChoiceLayout()
        {
            if (choices == null)
                return;

            RuntimeUiLayoutRegistry.CopyWorldLayout(
                choices,
                ChoicesSlotFor(active));
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
            Vector2 slotSize = portrait.rect.size;
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

            Vector2 anchorCenter =
                (portrait.anchorMin + portrait.anchorMax) * 0.5f;
            portrait.anchorMin = anchorCenter;
            portrait.anchorMax = anchorCenter;
            portrait.anchoredPosition = new Vector2(
                0f,
                slotSize.y * PortraitBottomPadding);
            portrait.sizeDelta = new Vector2(width, height);
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

            // The authored buttons remain only as binding contracts for
            // older scene content. They must never become visible again
            // when a dialogue finishes.
            string[] names = { "Map Btn", "Evidence Btn", "Settings Btn" };
            foreach (string name in names)
            {
                Transform navigation = ingameRoot.Find(name);
                if (navigation != null)
                    navigation.gameObject.SetActive(false);
            }

            UIManager.Instance?.SetExplorationNavigationSuppressed(!visible);
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
