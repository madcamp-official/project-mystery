using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wake.UI;

namespace Wake.Exploration
{
    [DisallowMultipleComponent]
    public sealed class ExplorationHotspotFeedback :
        MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        ISelectHandler,
        IDeselectHandler
    {
        private static bool accessibilityIndicatorsEnabled;

        [SerializeField] private TMP_Text label;
        [SerializeField] private Outline outline;
        private bool pointerInside;
        private bool selected;

        public static bool AccessibilityIndicatorsEnabled =>
            accessibilityIndicatorsEnabled;
        public bool IsIndicatorVisible =>
            label != null && label.gameObject.activeSelf;

        public static void SetAccessibilityIndicators(bool enabled)
        {
            accessibilityIndicatorsEnabled = enabled;
            foreach (ExplorationHotspotFeedback feedback in
                     FindObjectsByType<ExplorationHotspotFeedback>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                feedback?.Refresh();
            }
        }

        public void Configure(string displayName, TMP_Text existingLabel = null)
        {
            label = existingLabel != null
                ? existingLabel
                : CreateLabel(transform);
            outline ??= GetComponent<Outline>();
            if (outline == null)
                outline = gameObject.AddComponent<Outline>();

            label.text = string.IsNullOrWhiteSpace(displayName)
                ? "조사하기"
                : displayName.Trim();
            label.raycastTarget = false;
            outline.effectDistance = new Vector2(3f, -3f);
            Refresh();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
            Refresh();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            Refresh();
        }

        public void OnSelect(BaseEventData eventData)
        {
            selected = true;
            Refresh();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            selected = false;
            Refresh();
        }

        private void OnEnable() => Refresh();

        private void Refresh()
        {
            bool visible =
                accessibilityIndicatorsEnabled ||
                pointerInside ||
                selected;
            if (label != null)
                label.gameObject.SetActive(visible);
            if (outline != null)
            {
                Color color =
                    UiVisualThemeService.Resolve(UiColorToken.Focus);
                color.a = visible ? 0.9f : 0f;
                outline.effectColor = color;
            }
        }

        private static TMP_Text CreateLabel(Transform parent)
        {
            GameObject target = new(
                "Interaction Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            target.transform.SetParent(parent, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -8f);
            rect.sizeDelta = new Vector2(240f, 46f);
            UiVisualThemeService.ApplySurface(
                target.GetComponent<Image>(),
                UiSurfaceStyle.Overlay);

            GameObject textObject = new(
                "Label",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(target.transform, false);
            RectTransform textRect =
                textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 4f);
            textRect.offsetMax = new Vector2(-8f, -4f);

            TMP_Text text = textObject.GetComponent<TMP_Text>();
            UiVisualThemeService.ApplyText(text, UiTextStyle.Caption);
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 14f;
            text.fontSizeMax = 22f;
            return text;
        }
    }
}
