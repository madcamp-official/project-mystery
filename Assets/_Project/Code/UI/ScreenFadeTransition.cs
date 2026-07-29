using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Wake.UI
{
    [DisallowMultipleComponent]
    public sealed class ScreenFadeTransition : MonoBehaviour
    {
        private CanvasGroup group;
        private Image blocker;
        private Coroutine transition;

        public bool IsRunning => transition != null;

        public static ScreenFadeTransition Ensure()
        {
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas == null)
                return null;
            ScreenFadeTransition existing =
                canvas.GetComponent<ScreenFadeTransition>();
            return existing != null
                ? existing
                : canvas.AddComponent<ScreenFadeTransition>();
        }

        public bool Run(
            Action midpoint,
            float fadeOutSeconds = .25f,
            float fadeInSeconds = .25f)
        {
            EnsureOverlay();
            if (transition != null)
                return false;

            transition = StartCoroutine(Animate(
                midpoint,
                fadeOutSeconds,
                fadeInSeconds));
            return true;
        }

        private void EnsureOverlay()
        {
            if (group != null)
                return;

            GameObject overlay = new(
                "Screen Travel Fade",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            overlay.transform.SetParent(transform, false);
            RectTransform rect =
                overlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            blocker = overlay.GetComponent<Image>();
            blocker.color = new Color32(3, 8, 18, 255);
            group = overlay.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            overlay.SetActive(false);
        }

        private IEnumerator Animate(
            Action midpoint,
            float fadeOutSeconds,
            float fadeInSeconds)
        {
            blocker.gameObject.SetActive(true);
            blocker.transform.SetAsLastSibling();
            group.blocksRaycasts = true;
            yield return Fade(0f, 1f, fadeOutSeconds);
            try
            {
                midpoint?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            blocker.transform.SetAsLastSibling();
            yield return Fade(1f, 0f, fadeInSeconds);
            group.blocksRaycasts = false;
            blocker.gameObject.SetActive(false);
            transition = null;
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            float safeDuration = Mathf.Max(0f, duration);
            float elapsed = 0f;
            group.alpha = from;
            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(
                    from,
                    to,
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.Clamp01(elapsed / safeDuration)));
                yield return null;
            }
            group.alpha = to;
        }
    }
}
