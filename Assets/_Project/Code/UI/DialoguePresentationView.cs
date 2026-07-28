using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.Narrative;

namespace Wake.UI
{
    [DisallowMultipleComponent]
    public sealed class DialoguePresentationView : MonoBehaviour
    {
        private const string FocusPanelSlot = "dialogue.focus-panel";
        private const string CompactPanelSlot = "dialogue.compact-panel";
        private const string NarrationPanelSlot = "dialogue.narration-panel";
        private const string FocusPortraitLeftSlot =
            "dialogue.focus-portrait-left";
        private const string FocusPortraitRightSlot =
            "dialogue.focus-portrait-right";
        private const string CompactPortraitSlot =
            "dialogue.compact-portrait";

        private RectTransform ingameRoot;
        private RectTransform linePanel;
        private RectTransform portrait;
        private RectTransform textRect;
        private RectTransform speakerPlate;
        private RectTransform nextButton;
        private Image backgroundDim;
        private DialoguePresentationSpec active;
        private Vector2Int lastScreen;
        private bool initialized;

        public DialoguePresentationSpec Active => active;

        public void Initialize(
            RectTransform targetIngameRoot,
            RectTransform targetLinePanel,
            RectTransform targetPortrait,
            TMP_Text targetLineText,
            TMP_Text targetSpeakerText,
            RectTransform targetNextButton)
        {
            ingameRoot = targetIngameRoot;
            linePanel = targetLinePanel;
            portrait = targetPortrait;
            textRect = targetLineText != null
                ? targetLineText.rectTransform
                : null;
            speakerPlate = targetSpeakerText != null
                ? targetSpeakerText.transform.parent as RectTransform
                : null;
            nextButton = targetNextButton;
            backgroundDim = EnsureBackgroundDim();
            initialized = true;
            Apply(DialoguePresentationPolicy.Hidden);
        }

        public void Apply(DialoguePresentationSpec presentation)
        {
            active = presentation;
            if (!initialized)
                return;

            ApplyDim(presentation);
            if (!presentation.IsVisible ||
                presentation.Mode == DialoguePresentationMode.Investigation)
            {
                if (portrait != null)
                    portrait.gameObject.SetActive(false);
                return;
            }

            string panelSlot = PanelSlotFor(presentation.Mode);
            RuntimeUiLayoutRegistry.CopyLayout(linePanel, panelSlot);
            ApplyTextLayout(presentation.Mode);
            ApplySpeakerPlate(presentation.ShowSpeakerName);
            ApplyNextButton();
            ApplyPortrait(presentation);
            lastScreen = new Vector2Int(Screen.width, Screen.height);
        }

        public static string PanelSlotFor(DialoguePresentationMode mode) =>
            mode switch
            {
                DialoguePresentationMode.Compact => CompactPanelSlot,
                DialoguePresentationMode.Narration => NarrationPanelSlot,
                _ => FocusPanelSlot
            };

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

            RuntimeUiLayoutRegistry.CopyWorldLayout(
                portrait,
                PortraitSlotFor(presentation));
        }

        private void ApplyTextLayout(DialoguePresentationMode mode)
        {
            if (textRect == null)
                return;

            bool compact = mode == DialoguePresentationMode.Compact;
            textRect.anchorMin = compact
                ? new Vector2(0.30f, 0.18f)
                : new Vector2(0.09f, 0.17f);
            textRect.anchorMax = new Vector2(0.90f, 0.82f);
            textRect.pivot = new Vector2(0f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;
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

        private void LateUpdate()
        {
            if (!initialized || !active.IsVisible)
                return;

            Vector2Int screen = new(Screen.width, Screen.height);
            if (screen == lastScreen)
                return;

            Apply(active);
        }
    }
}
