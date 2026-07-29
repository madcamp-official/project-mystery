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
        private Coroutine fadeRoutine;

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
            Action completed = null,
            float holdSeconds = 0f)
        {
            if (transition != null)
                return false;

            started?.Invoke();
            transition = StartCoroutine(RunSequence(
                midpoint,
                fadeOutSeconds,
                fadeInSeconds,
                completed,
                holdSeconds));
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
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(Fade(group.alpha, 1f, duration));
            return fadeRoutine;
        }

        public Coroutine FadeOut(float duration)
        {
            EnsureOverlay();
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeOutSequence(duration));
            return fadeRoutine;
        }

        private IEnumerator FadeOutSequence(float duration)
        {
            // Callers often call this right after synchronous scene/asset
            // loading, which stalls a frame - Time.unscaledDeltaTime on the
            // next frame reports that whole stall, so starting the fade
            // immediately would consume the entire duration in one step.
            // Waiting a frame first lets that oversized delta land on a
            // frame we don't animate on.
            yield return null;
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
            Action completed,
            float holdSeconds)
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
            if (holdSeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(holdSeconds);
            }
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
                // Clamp so a single frame hitch (e.g. synchronous scene
                // loading right before this starts) can't consume the
                // whole fade in one step - the fade may lag briefly after
                // a hitch, but it never skips frames of animation.
                elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
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
