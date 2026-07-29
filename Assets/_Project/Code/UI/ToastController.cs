using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.Core;

namespace Wake.UI
{
    public enum ToastTypographyStyle
    {
        Normal,
        Alert
    }

    public class ToastController : MonoBehaviour
    {
        public static ToastController Instance { get; private set; }

        [SerializeField] private float displaySeconds = 1.5f;

        private readonly Queue<(string Message, ToastTypographyStyle Style)> pendingToasts =
            new();

        private GameObject toastRoot;
        private TMP_Text toastText;
        private Coroutine activeRoutine;

        private GameStateManager boundState;

        private void Awake()
        {
            Instance = this;
            BuildToastUi();
        }

        private void Start()
        {
            // Feedback (save confirmations, unlocks, etc.) is meant to
            // reach the player regardless of which gameplay panel happens
            // to be visible - it was previously relayed through
            // StatusHUDController, whose enabled state depends on the HUD
            // panel's own visibility (and which nothing currently ever
            // re-activates once hidden), so messages silently never
            // showed. ToastController lives for the whole session, so it
            // binds directly instead.
            boundState = GameStateManager.Instance;
            if (boundState != null)
            {
                boundState.FeedbackRequested += Show;
                boundState.BadEndTriggered += ShowBadEndFeedback;
            }
        }

        private void OnDestroy()
        {
            if (boundState != null)
            {
                boundState.FeedbackRequested -= Show;
                boundState.BadEndTriggered -= ShowBadEndFeedback;
            }
            if (Instance == this)
                Instance = null;
        }

        private void ShowBadEndFeedback(string message)
        {
            ShowAlert($"게임 종료 · {message}");
        }

        private void BuildToastUi()
        {
            Transform canvas = GameObject.Find("Canvas").transform;

            toastRoot = new GameObject("Toast", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            toastRoot.transform.SetParent(canvas, false);

            RectTransform rect = toastRoot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.82f);
            rect.anchorMax = new Vector2(0.5f, 0.82f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(480, 60);
            rect.anchoredPosition = Vector2.zero;
            RuntimeUiLayoutRegistry.CopyLayout(rect, "hud.toast");

            Image background = toastRoot.GetComponent<Image>();
            UiVisualThemeService.ApplySurface(
                background,
                UiSurfaceStyle.Toast);

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(toastRoot.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12, 6);
            textRect.offsetMax = new Vector2(-12, -6);

            toastText = textObject.GetComponent<TextMeshProUGUI>();
            toastText.alignment = TextAlignmentOptions.Center;
            UiVisualThemeService.ApplyText(
                toastText,
                UiTextStyle.Body);

            toastRoot.SetActive(false);
        }

        public void Show(string message)
        {
            Show(message, ToastTypographyStyle.Normal);
        }

        public void ShowAlert(string message)
        {
            Show(message, ToastTypographyStyle.Alert);
        }

        public void Show(
            string message,
            ToastTypographyStyle style)
        {
            pendingToasts.Enqueue((message, style));
            if (activeRoutine == null)
            {
                activeRoutine = StartCoroutine(ProcessQueue());
            }
        }

        public static TypographyRole ResolveRole(
            ToastTypographyStyle style)
        {
            return style == ToastTypographyStyle.Alert
                ? TypographyRole.SpecialAlert
                : TypographyRole.Body;
        }

        private IEnumerator ProcessQueue()
        {
            while (pendingToasts.Count > 0)
            {
                (string message, ToastTypographyStyle style) = pendingToasts.Dequeue();
                toastText.text = message;
                UiVisualThemeService.ApplyText(
                    toastText,
                    style == ToastTypographyStyle.Alert
                        ? UiTextStyle.Alert
                        : UiTextStyle.Body);
                toastRoot.SetActive(true);
                yield return new WaitForSeconds(displaySeconds);
                toastRoot.SetActive(false);
            }
            activeRoutine = null;
        }
    }
}
