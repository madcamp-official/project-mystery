using UnityEngine;

namespace Wake.Exploration
{
    public enum HotspotState
    {
        Unseen,
        Seen
    }

    [RequireComponent(typeof(Collider2D))]
    public abstract class Hotspot : MonoBehaviour
    {
        [SerializeField] private string hotspotId;
        [SerializeField] private SpriteRenderer highlightRenderer;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseMinAlpha = 0.55f;

        public string HotspotId => hotspotId;
        public HotspotState State { get; private set; } = HotspotState.Unseen;

        private Color baseColor;
        private bool pointerInside;

        protected virtual void Awake()
        {
            if (highlightRenderer == null)
            {
                highlightRenderer = GetComponent<SpriteRenderer>();
            }

            if (highlightRenderer != null)
            {
                baseColor = highlightRenderer.color;
                SetHighlightAlpha(
                    ExplorationHotspotFeedback
                        .AccessibilityIndicatorsEnabled
                        ? pulseMinAlpha
                        : 0f);
            }
        }

        protected virtual void Update()
        {
            if (highlightRenderer == null)
            {
                return;
            }

            if (State != HotspotState.Unseen)
            {
                SetHighlightAlpha(baseColor.a);
                return;
            }

            bool reveal =
                pointerInside ||
                ExplorationHotspotFeedback
                    .AccessibilityIndicatorsEnabled;
            if (!reveal)
            {
                SetHighlightAlpha(0f);
                return;
            }

            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            SetHighlightAlpha(
                Mathf.Lerp(pulseMinAlpha, baseColor.a, t));
        }

        private void OnMouseEnter() => pointerInside = true;

        private void OnMouseExit() => pointerInside = false;

        private void OnMouseDown() => Interact();

        public void Interact()
        {
            OnInteract();

            if (State == HotspotState.Unseen)
            {
                State = HotspotState.Seen;
                if (highlightRenderer != null)
                {
                    highlightRenderer.color = baseColor;
                }
            }
        }

        protected abstract void OnInteract();

        private void SetHighlightAlpha(float alpha)
        {
            highlightRenderer.color = new Color(
                baseColor.r,
                baseColor.g,
                baseColor.b,
                Mathf.Clamp01(alpha));
        }
    }
}
