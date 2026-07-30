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
        private bool pointerInside;
        private bool selected;

        public static bool AccessibilityIndicatorsEnabled =>
            accessibilityIndicatorsEnabled;
        public bool IsIndicatorVisible =>
            outline != null && outline.effectColor.a > 0f;

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
            outline ??= GetComponent<Outline>();
            if (outline == null)
                outline = gameObject.AddComponent<Outline>();

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
            if (outline != null)
            {
                Color color =
                    UiVisualThemeService.Resolve(UiColorToken.Focus);
                color.a = visible ? 0.9f : 0f;
                outline.effectColor = color;
            }
        }

    }
}
