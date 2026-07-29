using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Wake.UI
{
    [DisallowMultipleComponent]
    public sealed class ScreenCoverPresenter : MonoBehaviour
    {
        private Image image;
        private CanvasGroup group;
        public void SetInputBlocked(bool blocked)
        {
            if (!EnsureCover())
                return;
            image.gameObject.SetActive(blocked);
            image.transform.SetAsLastSibling();
            group.blocksRaycasts = blocked;
            group.interactable = blocked;
            if (!blocked && Mathf.Approximately(group.alpha, 0f))
                image.gameObject.SetActive(false);
        }

        public IEnumerator FadeTo(
            float target,
            float duration,
            Color color)
        {
            if (!EnsureCover())
                yield break;
            image.color = color;
            image.gameObject.SetActive(true);
            image.transform.SetAsLastSibling();
            float start = group.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(
                    start,
                    target,
                    Mathf.Clamp01(elapsed / Mathf.Max(.0001f, duration)));
                yield return null;
            }
            group.alpha = target;
        }

        public void ResetCover()
        {
            if (!EnsureCover())
                return;
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            image.gameObject.SetActive(false);
            image.transform.SetAsFirstSibling();
        }

        private bool EnsureCover()
        {
            if (this == null ||
                gameObject == null ||
                !gameObject.scene.isLoaded)
            {
                return false;
            }
            if (group != null)
                return true;

            GameObject cover = new(
                "UI Transition Cover",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            cover.transform.SetParent(transform, false);
            RectTransform rect = cover.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            image = cover.GetComponent<Image>();
            image.color = new Color32(3, 8, 18, 255);
            group = cover.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            cover.SetActive(false);
            return true;
        }
    }
}
