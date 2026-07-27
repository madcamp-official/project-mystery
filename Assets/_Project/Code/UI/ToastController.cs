using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        private void Awake()
        {
            Instance = this;
            BuildToastUi();
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

            Image background = toastRoot.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.75f);

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(toastRoot.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12, 6);
            textRect.offsetMax = new Vector2(-12, -6);

            toastText = textObject.GetComponent<TextMeshProUGUI>();
            toastText.alignment = TextAlignmentOptions.Center;
            toastText.color = Color.white;
            toastText.fontSize = 22;
            TypographyService.Apply(toastText, TypographyRole.Body);

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
                TypographyService.Apply(toastText, ResolveRole(style));
                toastRoot.SetActive(true);
                yield return new WaitForSeconds(displaySeconds);
                toastRoot.SetActive(false);
            }
            activeRoutine = null;
        }
    }
}
