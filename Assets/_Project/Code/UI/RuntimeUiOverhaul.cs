using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wake.Core;
using Wake.Evidence;
using System.Text.RegularExpressions;

namespace Wake.UI
{
    public sealed class UiHoverFeedback :
        MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        private Vector3 baseScale;
        private Graphic graphic;
        private Color baseColor;

        private void Awake()
        {
            baseScale = transform.localScale;
            graphic = GetComponent<Graphic>();
            if (graphic != null)
            {
                baseColor = graphic.color;
            }
        }

        public void OnPointerEnter(PointerEventData _) => Apply(1.055f, 1.16f);
        public void OnPointerExit(PointerEventData _) => Apply(1f, 1f);
        public void OnPointerDown(PointerEventData _) => Apply(0.98f, 1.08f);
        public void OnPointerUp(PointerEventData _) => Apply(1.055f, 1.16f);

        private void Apply(float scale, float brightness)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }
            transform.localScale = baseScale * scale;
            if (graphic != null)
            {
                graphic.color = new Color(
                    Mathf.Min(1f, baseColor.r * brightness),
                    Mathf.Min(1f, baseColor.g * brightness),
                    Mathf.Min(1f, baseColor.b * brightness),
                    baseColor.a);
            }
        }

        private void OnDisable()
        {
            transform.localScale = baseScale;
            if (graphic != null)
            {
                graphic.color = baseColor;
            }
        }
    }

    public sealed class UiPanelEntranceAnimator : MonoBehaviour
    {
        [SerializeField] private bool excludeDialoguePanel;
        public bool ExcludeDialoguePanel
        {
            set => excludeDialoguePanel = value;
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                StartCoroutine(Animate());
            }
        }

        private IEnumerator Animate()
        {
            yield return null; // 배경을 먼저 한 프레임 렌더링한다.
            if (!isActiveAndEnabled)
            {
                yield break;
            }
            int visibleIndex = 0;
            foreach (Transform child in transform)
            {
                if (!child.gameObject.activeInHierarchy ||
                    IsBackground(child) ||
                    (excludeDialoguePanel && IsDialogue(child)))
                {
                    continue;
                }
                if (child is RectTransform rect)
                {
                    StartCoroutine(Slide(rect, visibleIndex++));
                }
            }
        }

        private static bool IsBackground(Transform target)
        {
            string value = target.name.ToLowerInvariant();
            return value.Contains("background") || value == "image" ||
                   value.Contains("backdrop");
        }

        private static bool IsDialogue(Transform target)
        {
            string value = target.name.ToLowerInvariant();
            return value.Contains("line panel") || value.Contains("dialogue");
        }

        private static IEnumerator Slide(RectTransform rect, int index)
        {
            if (rect == null)
            {
                yield break;
            }
            Vector2 end = rect.anchoredPosition;
            float direction = index % 2 == 0 ? -1f : 1f;
            Vector2 start = end + new Vector2(direction * 72f, 0f);
            CanvasGroup group = rect.GetComponent<CanvasGroup>() ??
                                rect.gameObject.AddComponent<CanvasGroup>();
            if (group == null)
            {
                yield break;
            }
            group.alpha = 0f;
            rect.anchoredPosition = start;
            float elapsed = 0f;
            const float duration = 0.32f;
            while (elapsed < duration)
            {
                if (rect == null || group == null ||
                    !rect.gameObject.activeInHierarchy)
                {
                    yield break;
                }
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                rect.anchoredPosition = Vector2.LerpUnclamped(start, end, t);
                group.alpha = t;
                yield return null;
            }
            if (rect != null && group != null)
            {
                rect.anchoredPosition = end;
                group.alpha = 1f;
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }
    }

    [DisallowMultipleComponent]
    public sealed class RuntimeUiOverhaulController : MonoBehaviour
    {
        private float nextScan;
        private static readonly Regex EvidenceCode =
            new(@"\bC[-_ ]?(\d{1,2})\b", RegexOptions.IgnoreCase);

        private void Start()
        {
            ConfigurePanels();
            ScanButtons();
            SanitizeVisibleText();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextScan)
            {
                return;
            }
            nextScan = Time.unscaledTime + 0.5f;
            ScanButtons();
            SanitizeVisibleText();
        }

        private static void ConfigurePanels()
        {
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                return;
            }
            AddAnimator(canvas.transform.Find("StartScene"), false);
            AddAnimator(canvas.transform.Find("Ingame"), true);
            AddAnimator(canvas.transform.Find("Map"), false);
            AddAnimator(canvas.transform.Find("Evidence"), false);

            // 전체 단서 수를 암시하는 우측 상단 표시는 숨긴다.
            Transform status = canvas.transform.Find("Status HUD");
            if (status == null)
            {
                return;
            }
            foreach (TMP_Text text in status.GetComponentsInChildren<TMP_Text>(true))
            {
                string value = text.text?.Trim() ?? string.Empty;
                if (value.StartsWith("Evidence", System.StringComparison.OrdinalIgnoreCase))
                {
                    text.gameObject.SetActive(false);
                }
            }
        }

        private static void AddAnimator(Transform panel, bool excludeDialogue)
        {
            if (panel == null)
            {
                return;
            }
            UiPanelEntranceAnimator animator =
                panel.GetComponent<UiPanelEntranceAnimator>() ??
                panel.gameObject.AddComponent<UiPanelEntranceAnimator>();
            animator.ExcludeDialoguePanel = excludeDialogue;
        }

        private static void ScanButtons()
        {
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                return;
            }
            foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
            {
                if (button.GetComponent<UiHoverFeedback>() == null)
                {
                    button.gameObject.AddComponent<UiHoverFeedback>();
                }
            }
        }

        private static void SanitizeVisibleText()
        {
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                return;
            }
            foreach (TMP_Text label in canvas.GetComponentsInChildren<TMP_Text>(true))
            {
                string original = label.text ?? string.Empty;
                string sanitized = original;
                sanitized = EvidenceCode.Replace(sanitized, match =>
                {
                    string id = $"C-{int.Parse(match.Groups[1].Value):00}";
                    return CanonicalEvidenceCatalog.TryGet(
                        id, out CanonicalEvidenceEntry entry)
                        ? entry.DisplayName
                        : "단서";
                });
                if (sanitized != original)
                {
                    label.text = sanitized;
                }
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class SaveSlotSelectionController : MonoBehaviour
    {
        private GameObject overlay;
        private GameObject confirmation;
        private int pendingSlot;
        private bool pendingContinue;
        private readonly TMP_Text[] slotLabels = new TMP_Text[3];

        public void Open()
        {
            EnsureBuilt();
            Refresh();
            overlay.SetActive(true);
            confirmation.SetActive(false);
        }

        private void EnsureBuilt()
        {
            if (overlay != null)
            {
                return;
            }
            overlay = Panel(transform, "Save Slot Selection", new Color32(7, 15, 29, 247));
            RectTransform root = overlay.GetComponent<RectTransform>();
            Stretch(root);
            MakeText(root, "저장 슬롯 선택", 42, new Vector2(0, 260), new Vector2(700, 70));

            for (int index = 0; index < 3; index++)
            {
                int slot = index + 1;
                Button button = MakeButton(
                    root, $"Save Slot {slot}",
                    new Vector2((index - 1) * 390f, 20f),
                    new Vector2(330f, 360f));
                slotLabels[index] = MakeText(
                    button.transform as RectTransform, string.Empty, 25,
                    Vector2.zero, new Vector2(285f, 280f));
                button.onClick.AddListener(() => Ask(slot));
            }
            Button close = MakeButton(root, "닫기", new Vector2(0, -260), new Vector2(210, 58));
            MakeText(close.transform as RectTransform, "돌아가기", 24, Vector2.zero, new Vector2(180, 48));
            close.onClick.AddListener(() => overlay.SetActive(false));

            confirmation = Panel(root, "Start Confirmation", new Color32(4, 10, 21, 252));
            RectTransform confirmRect = confirmation.GetComponent<RectTransform>();
            confirmRect.anchorMin = confirmRect.anchorMax = new Vector2(.5f, .5f);
            confirmRect.sizeDelta = new Vector2(650f, 330f);
            TMP_Text message = MakeText(confirmRect, string.Empty, 28, new Vector2(0, 55), new Vector2(560, 130));
            message.name = "Message";
            Button yes = MakeButton(confirmRect, "Confirm", new Vector2(-135, -95), new Vector2(220, 58));
            MakeText(yes.transform as RectTransform, "확인", 24, Vector2.zero, new Vector2(180, 48));
            yes.onClick.AddListener(Confirm);
            Button no = MakeButton(confirmRect, "Cancel", new Vector2(135, -95), new Vector2(220, 58));
            MakeText(no.transform as RectTransform, "취소", 24, Vector2.zero, new Vector2(180, 48));
            no.onClick.AddListener(() => confirmation.SetActive(false));
            confirmation.SetActive(false);
            overlay.SetActive(false);
        }

        private void Refresh()
        {
            for (int index = 0; index < slotLabels.Length; index++)
            {
                bool occupied = GameStateManager.HasSaveDataInSlot(index + 1);
                slotLabels[index].text =
                    $"SLOT {index + 1}\n\n" +
                    (occupied ? "저장된 수사\n이어하기" : "빈 슬롯\n새로 시작");
            }
        }

        private void Ask(int slot)
        {
            pendingSlot = slot;
            pendingContinue = GameStateManager.HasSaveDataInSlot(slot);
            confirmation.transform.Find("Message").GetComponent<TMP_Text>().text =
                pendingContinue
                    ? $"{slot}번 슬롯의 수사를 이어하시겠습니까?"
                    : $"{slot}번 슬롯에서 새 수사를 시작하시겠습니까?";
            confirmation.SetActive(true);
            confirmation.transform.SetAsLastSibling();
        }

        private void Confirm()
        {
            confirmation.SetActive(false);
            overlay.SetActive(false);
            if (pendingContinue)
            {
                UIManager.Instance?.ContinueGameInSlot(pendingSlot);
            }
            else
            {
                UIManager.Instance?.StartNewGameInSlot(pendingSlot);
            }
        }

        internal static GameObject Panel(Transform parent, string name, Color color)
        {
            GameObject value = new(name, typeof(RectTransform), typeof(Image));
            value.transform.SetParent(parent, false);
            value.GetComponent<Image>().color = color;
            return value;
        }

        internal static Button MakeButton(
            RectTransform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject value = Panel(parent, name, new Color32(174, 127, 48, 245));
            RectTransform rect = value.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Button button = value.AddComponent<Button>();
            button.targetGraphic = value.GetComponent<Image>();
            value.AddComponent<Outline>().effectColor = new Color32(241, 211, 145, 255);
            return button;
        }

        internal static TMP_Text MakeText(
            RectTransform parent, string text, float size,
            Vector2 position, Vector2 dimensions)
        {
            GameObject value = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            value.transform.SetParent(parent, false);
            RectTransform rect = value.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            TMP_Text label = value.GetComponent<TMP_Text>();
            label.text = text;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color32(247, 231, 194, 255);
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }

        internal static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }

    [DisallowMultipleComponent]
    public sealed class EvidenceAcquisitionNoticeController : MonoBehaviour
    {
        private RectTransform notice;
        private TMP_Text title;
        private EvidenceInventory boundInventory;
        private Coroutine noticeAnimation;

        private void Update()
        {
            if (boundInventory == EvidenceInventory.Instance)
            {
                return;
            }
            if (boundInventory != null)
            {
                boundInventory.EvidenceAdded -= Show;
            }
            boundInventory = EvidenceInventory.Instance;
            if (boundInventory != null)
            {
                boundInventory.EvidenceAdded += Show;
            }
        }

        private void OnDestroy()
        {
            if (boundInventory != null)
            {
                boundInventory.EvidenceAdded -= Show;
            }
        }

        private void EnsureBuilt()
        {
            if (notice != null)
            {
                return;
            }
            Canvas canvas = FindFirstObjectByType<Canvas>();
            GameObject panel = SaveSlotSelectionController.Panel(
                canvas.transform, "Evidence Acquired Notice",
                new Color32(8, 20, 38, 248));
            notice = panel.GetComponent<RectTransform>();
            notice.anchorMin = notice.anchorMax = new Vector2(1f, .72f);
            notice.pivot = new Vector2(1f, .5f);
            notice.sizeDelta = new Vector2(390f, 150f);
            title = SaveSlotSelectionController.MakeText(
                notice, string.Empty, 25f, Vector2.zero, new Vector2(340f, 115f));
            panel.AddComponent<Outline>().effectColor = new Color32(214, 166, 76, 255);
            panel.SetActive(false);
        }

        private void Show(EvidenceDefinition evidence)
        {
            EnsureBuilt();
            title.text = $"새로운 단서를 발견했습니다\n{evidence.DisplayName}";
            AudioManager.Instance?.PlayEvidencePickup();
            if (noticeAnimation != null)
            {
                StopCoroutine(noticeAnimation);
            }
            noticeAnimation = StartCoroutine(AnimateNotice());
        }

        private IEnumerator AnimateNotice()
        {
            notice.gameObject.SetActive(true);
            Vector2 shown = new Vector2(-24f, 0f);
            Vector2 hidden = new Vector2(notice.sizeDelta.x + 30f, 0f);
            notice.anchoredPosition = hidden;
            yield return Move(hidden, shown, .3f);
            yield return new WaitForSecondsRealtime(2.5f);
            yield return Move(shown, hidden, .28f);
            notice.gameObject.SetActive(false);
        }

        private IEnumerator Move(Vector2 from, Vector2 to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                notice.anchoredPosition = Vector2.Lerp(
                    from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                yield return null;
            }
            notice.anchoredPosition = to;
        }
    }
}
