using System;
using UnityEngine;

namespace Wake.Exploration
{
    public readonly struct BackgroundCoverResult
    {
        public BackgroundCoverResult(Vector2 size, Vector2 offset)
        {
            Size = size;
            Offset = offset;
        }

        public Vector2 Size { get; }
        public Vector2 Offset { get; }
    }

    /// <summary>
    /// Calculates CSS-style cover geometry without changing the source aspect ratio.
    /// Focus is normalized from bottom-left (0, 0) to top-right (1, 1).
    /// </summary>
    public static class BackgroundCoverLayout
    {
        public static BackgroundCoverResult Calculate(
            Vector2 viewport,
            Vector2 source,
            Vector2 focus,
            float zoom = 1f)
        {
            if (viewport.x <= 0f || viewport.y <= 0f)
                throw new ArgumentOutOfRangeException(
                    nameof(viewport), "Viewport dimensions must be positive.");
            if (source.x <= 0f || source.y <= 0f)
                throw new ArgumentOutOfRangeException(
                    nameof(source), "Source dimensions must be positive.");

            focus = new Vector2(
                Mathf.Clamp01(focus.x),
                Mathf.Clamp01(focus.y));
            zoom = Mathf.Max(1f, zoom);
            float scale = Mathf.Max(
                viewport.x / source.x,
                viewport.y / source.y) * zoom;
            Vector2 covered = source * scale;
            Vector2 overflow = covered - viewport;

            // A centered pivot needs half the overflow at either extreme.
            Vector2 offset = new(
                (0.5f - focus.x) * overflow.x,
                (0.5f - focus.y) * overflow.y);
            return new BackgroundCoverResult(covered, offset);
        }
    }

    [DisallowMultipleComponent]
    public sealed class BackgroundCoverPresenter : MonoBehaviour
    {
        private RectTransform viewport;
        private RectTransform motionRect;
        private RectTransform imageRect;
        private UnityEngine.UI.Image image;
        private Vector2 focus = new(0.5f, 0.5f);
        private float zoom = 1f;
        private Vector2 lastViewportSize;

        public Sprite Sprite => image != null ? image.sprite : null;
        public Vector2 Focus => focus;
        public float Zoom => zoom;
        public RectTransform ViewportRect => viewport;
        public RectTransform MotionRect => motionRect;
        public RectTransform ContentRect => imageRect;

        public void Initialize(RectTransform parent)
        {
            viewport = GetComponent<RectTransform>();
            viewport.SetParent(parent, false);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            gameObject.AddComponent<UnityEngine.UI.RectMask2D>();

            GameObject motionObject = new(
                "Background Motion Root",
                typeof(RectTransform));
            motionObject.transform.SetParent(viewport, false);
            motionRect = motionObject.GetComponent<RectTransform>();
            motionRect.anchorMin = Vector2.zero;
            motionRect.anchorMax = Vector2.one;
            motionRect.offsetMin = Vector2.zero;
            motionRect.offsetMax = Vector2.zero;

            GameObject imageObject = new(
                "Cover Image",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(UnityEngine.UI.Image));
            imageObject.transform.SetParent(motionRect, false);
            imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0.5f, 0.5f);
            imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            image = imageObject.GetComponent<UnityEngine.UI.Image>();
            image.preserveAspect = false;
            image.raycastTarget = false;
            gameObject.SetActive(false);
        }

        public void Show(Sprite sprite, Vector2 normalizedFocus, float zoomFactor)
        {
            if (image == null)
                throw new InvalidOperationException(
                    "BackgroundCoverPresenter must be initialized before Show.");
            image.sprite = sprite;
            focus = new Vector2(
                Mathf.Clamp01(normalizedFocus.x),
                Mathf.Clamp01(normalizedFocus.y));
            zoom = Mathf.Max(1f, zoomFactor);
            gameObject.SetActive(sprite != null);
            Refresh();
        }

        public void Refresh()
        {
            if (image?.sprite == null || viewport == null || imageRect == null)
                return;
            Vector2 viewportSize = viewport.rect.size;
            if (viewportSize.x <= 0f || viewportSize.y <= 0f)
                return;
            Rect spriteRect = image.sprite.rect;
            BackgroundCoverResult result = BackgroundCoverLayout.Calculate(
                viewportSize,
                new Vector2(spriteRect.width, spriteRect.height),
                focus,
                zoom);
            imageRect.sizeDelta = result.Size;
            imageRect.anchoredPosition = result.Offset;
            lastViewportSize = viewportSize;
        }

        private void LateUpdate()
        {
            if (viewport != null && viewport.rect.size != lastViewportSize)
                Refresh();
        }
    }
}
