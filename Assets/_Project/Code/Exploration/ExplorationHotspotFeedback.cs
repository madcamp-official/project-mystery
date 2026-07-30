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

        [SerializeField] private Outline outline;
        [SerializeField] private Image accessibilityMarker;
        private bool outlineEnabled;
        private bool exactShapeMode;
        private bool pointerInside;
        private bool selected;

        public static bool AccessibilityIndicatorsEnabled =>
            accessibilityIndicatorsEnabled;
        public bool IsIndicatorVisible =>
            (accessibilityMarker != null &&
             accessibilityMarker.gameObject.activeSelf) ||
            (outlineEnabled &&
             outline != null &&
             outline.effectColor.a > 0f);

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

        public void Configure()
        {
            exactShapeMode = false;
            outlineEnabled = true;
            outline ??= GetComponent<Outline>();
            if (outline == null)
                outline = gameObject.AddComponent<Outline>();

            outline.effectDistance = new Vector2(3f, -3f);
            Refresh();
        }

        public void ConfigureExactShape()
        {
            ConfigureExactShape(new Vector2(0.5f, 0.5f));
        }

        public void ConfigureExactShape(Vector2 normalizedFocusAnchor)
        {
            exactShapeMode = true;
            outlineEnabled = false;
            accessibilityMarker ??= CreateAccessibilityMarker(transform);
            RectTransform markerRect =
                accessibilityMarker.rectTransform;
            Vector2 anchor = new(
                Mathf.Clamp01(normalizedFocusAnchor.x),
                Mathf.Clamp01(normalizedFocusAnchor.y));
            markerRect.anchorMin = anchor;
            markerRect.anchorMax = anchor;
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

        public void ResetTransientState()
        {
            pointerInside = false;
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
            if (accessibilityMarker != null)
            {
                accessibilityMarker.gameObject.SetActive(
                    exactShapeMode &&
                    (accessibilityIndicatorsEnabled || selected));
            }
            if (outline != null)
            {
                Color color =
                    UiVisualThemeService.Resolve(UiColorToken.Focus);
                color.a = outlineEnabled && visible ? 0.9f : 0f;
                outline.effectColor = color;
            }
        }

        private static Image CreateAccessibilityMarker(Transform parent)
        {
            GameObject target = new(
                "Accessibility Focus Marker",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            target.transform.SetParent(parent, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(14f, 14f);
            rect.localRotation = Quaternion.Euler(0f, 0f, 45f);

            Image marker = target.GetComponent<Image>();
            marker.color = new Color32(218, 170, 78, 245);
            marker.raycastTarget = false;
            target.SetActive(false);
            return marker;
        }

    }
}
