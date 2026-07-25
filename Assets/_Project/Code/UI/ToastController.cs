using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Wake.UI
{
    public class ToastController : MonoBehaviour
    {
        public static ToastController Instance { get; private set; }

        [SerializeField] private float displaySeconds = 1.5f;

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
            toastText.font = StatusHUDController.RuntimeKoreanFont;

            toastRoot.SetActive(false);
        }

        public void Show(string message)
        {
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
            }
            activeRoutine = StartCoroutine(ShowRoutine(message));
        }

        private IEnumerator ShowRoutine(string message)
        {
            toastText.text = message;
            toastRoot.SetActive(true);
            yield return new WaitForSeconds(displaySeconds);
            toastRoot.SetActive(false);
            activeRoutine = null;
        }
    }
}
