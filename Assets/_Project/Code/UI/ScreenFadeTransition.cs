using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Wake.UI
{
    [DisallowMultipleComponent]
    public sealed class ScreenFadeTransition : MonoBehaviour
    {
        private static readonly Color DefaultColor = new Color32(3, 8, 18, 255);

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
            float fadeInSeconds = .25f,
            Action started = null,
            Action completed = null)
        {
            if (transition != null)
                return false;

            started?.Invoke();
            transition = StartCoroutine(RunSequence(
                midpoint,
                fadeOutSeconds,
                fadeInSeconds,
                completed));
            return true;
        }

        // Fades the overlay to fully covering the screen. Callers that
        // start this in parallel with some other animation (rather than
        // via Run(...)) are responsible for eventually calling FadeOut.
        public Coroutine FadeIn(float duration, Color color)
        {
            EnsureOverlay();
            blocker.color = color;
            blocker.gameObject.SetActive(true);
            blocker.transform.SetAsLastSibling();
            group.blocksRaycasts = true;
            return StartCoroutine(Fade(group.alpha, 1f, duration));
        }

        public Coroutine FadeOut(float duration)
        {
            EnsureOverlay();
            return StartCoroutine(FadeOutSequence(duration));
        }

        private IEnumerator FadeOutSequence(float duration)
        {
            yield return Fade(group.alpha, 0f, duration);
            group.blocksRaycasts = false;
            blocker.gameObject.SetActive(false);
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
            blocker.color = DefaultColor;
            group = overlay.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            overlay.SetActive(false);
        }

        private IEnumerator RunSequence(
            Action midpoint,
            float fadeOutSeconds,
            float fadeInSeconds,
            Action completed)
        {
            yield return FadeIn(fadeOutSeconds, DefaultColor);
            try
            {
                midpoint?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            blocker.transform.SetAsLastSibling();
            yield return FadeOut(fadeInSeconds);
            transition = null;
            completed?.Invoke();
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
