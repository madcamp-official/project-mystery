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

        protected virtual void Awake()
        {
            if (highlightRenderer == null)
            {
                highlightRenderer = GetComponent<SpriteRenderer>();
            }

            if (highlightRenderer != null)
            {
                baseColor = highlightRenderer.color;
            }
        }

        protected virtual void Update()
        {
            if (State != HotspotState.Unseen || highlightRenderer == null)
            {
                return;
            }

            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(pulseMinAlpha, 1f, t);
            highlightRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }

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
    }
}
