using System;
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

        public void OnPointerEnter(PointerEventData _)
        {
            UiInteractionToken token = UiVisualThemeService.Interaction;
            Apply(token.HoverScale, token.HoverBrightness);
        }

        public void OnPointerExit(PointerEventData _) => Apply(1f, 1f);

        public void OnPointerDown(PointerEventData _)
        {
            UiInteractionToken token = UiVisualThemeService.Interaction;
            Apply(token.PressedScale, token.PressedBrightness);
        }

        public void OnPointerUp(PointerEventData _)
        {
            UiInteractionToken token = UiVisualThemeService.Interaction;
            Apply(token.HoverScale, token.HoverBrightness);
        }

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
                   value.Contains("backdrop") ||
                   value.Contains("title presentation");
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
            ConfigureCanvas();
            ConfigurePanels();
            ScanButtons();
            SanitizeVisibleText();
        }

        private static void ConfigureCanvas()
        {
            CanvasScaler scaler = GameObject.Find("Canvas")
                ?.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                return;
            }
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
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
    public sealed class TitleScreenPresentationController : MonoBehaviour
    {
        private GameObject presentation;

        private static readonly Color Ink = new(0.018f, 0.025f, 0.052f, 0.97f);
        private static readonly Color Gold = new(0.79f, 0.60f, 0.29f, 1f);

        private void Start()
        {
            Build();
        }

        private void OnEnable()
        {
            if (presentation != null)
            {
                presentation.SetActive(true);
            }
        }

        private void Build()
        {
            if (presentation != null)
            {
                return;
            }

            Button originalStart = transform.Find("Start Game Btn")
                ?.GetComponent<Button>();
            Button originalSettings = transform.Find("Settings Btn")
                ?.GetComponent<Button>();
            RectTransform host = transform as RectTransform;
            if (host != null)
            {
                host.anchorMin = Vector2.zero;
                host.anchorMax = Vector2.one;
                host.offsetMin = host.offsetMax = Vector2.zero;
                host.localScale = Vector3.one;
            }
            foreach (Transform child in transform)
            {
                if (child == originalStart?.transform ||
                    child == originalSettings?.transform)
                {
                    continue;
                }
                child.gameObject.SetActive(false);
            }
            KeepLegacyButtonContract(originalStart);
            KeepLegacyButtonContract(originalSettings);

            presentation = new GameObject(
                "Title Presentation",
                typeof(RectTransform),
                typeof(Image));
            presentation.transform.SetParent(transform, false);
            RectTransform root = presentation.GetComponent<RectTransform>();
            SaveSlotSelectionController.Stretch(root);
            Image background = presentation.GetComponent<Image>();
            background.sprite =
                Resources.Load<Sprite>("UiOverhaul/ui_title_background");
            background.preserveAspect = false;
            background.color = Color.white;

            CreateShade(root);
            CreateBorder(root);
            CreateLogo(root);
            CreateMenu(root, originalStart, originalSettings);
            CreateFooter(root);
            presentation.AddComponent<UiPanelEntranceAnimator>();
        }

        private static void KeepLegacyButtonContract(Button button)
        {
            if (button == null)
            {
                return;
            }
            button.gameObject.SetActive(true);
            button.transform.localScale = Vector3.zero;
            CanvasGroup group = button.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = button.gameObject.AddComponent<CanvasGroup>();
            }
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        private static void CreateShade(RectTransform root)
        {
            GameObject shade = SaveSlotSelectionController.Panel(
                root, "Left Readability Shade", Color.clear);
            RectTransform rect = shade.GetComponent<RectTransform>();
            SaveSlotSelectionController.Stretch(rect);
            Image image = shade.GetComponent<Image>();
            image.color = new Color(0.005f, 0.008f, 0.025f, 0.22f);
            image.raycastTarget = false;
        }

        private static void CreateBorder(RectTransform root)
        {
            GameObject border = SaveSlotSelectionController.Panel(
                root, "Nautical Border", Color.clear);
            RectTransform rect = border.GetComponent<RectTransform>();
            SaveSlotSelectionController.Stretch(rect);
            Image image = border.GetComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = false;
            Outline outline = border.AddComponent<Outline>();
            outline.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        private static void CreateLogo(RectTransform root)
        {
            GameObject logoObject = new(
                "Under the Horizon Logo",
                typeof(RectTransform),
                typeof(Image));
            logoObject.transform.SetParent(root, false);
            RectTransform logoRect = logoObject.GetComponent<RectTransform>();
            RuntimeUiLayoutRegistry.CopyWorldLayout(
                logoRect,
                ScreenRegionIds.ContextTopLeft);
            Image logo = logoObject.GetComponent<Image>();
            logo.sprite =
                Resources.Load<Sprite>("UiOverhaul/logo_transparent");
            logo.preserveAspect = true;
            logo.raycastTarget = false;
        }

        private static void CreateMenu(
            RectTransform root,
            Button originalStart,
            Button originalSettings)
        {
            GameObject menuObject = new(
                "Title Menu",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup));
            menuObject.transform.SetParent(root, false);
            RectTransform menu = menuObject.GetComponent<RectTransform>();
            RuntimeUiLayoutRegistry.CopyWorldLayout(
                menu,
                ScreenRegionIds.ToolsBottomLeft);
            VerticalLayoutGroup layout =
                menuObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing =
                UiVisualThemeService.Resolve(UiSpacingToken.Small);
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Button start = CreateTitleButton(
                menu, "시작하기", "시작", UiButtonStyle.Primary);
            start.onClick.AddListener(() => originalStart?.onClick.Invoke());

            Button settings = CreateTitleButton(
                menu, "설정", "설정", UiButtonStyle.Secondary);
            settings.onClick.AddListener(() => originalSettings?.onClick.Invoke());

            Button credits = CreateTitleButton(
                menu, "크레딧", "크레딧", UiButtonStyle.Secondary);
            credits.onClick.AddListener(
                () => UIManager.Instance?.ShowCredits());

            Button quit = CreateTitleButton(
                menu, "종료", "종료", UiButtonStyle.Danger);
            quit.gameObject.name = "Quit Game Button";
            quit.onClick.AddListener(
                () => UIManager.Instance?.RequestQuit());
        }

        public static void QuitGame()
        {
#if UNITY_EDITOR
            System.Type editorApplication = System.Type.GetType(
                "UnityEditor.EditorApplication, UnityEditor");
            editorApplication?.GetProperty("isPlaying")?.SetValue(null, false);
#else
            Application.Quit();
#endif
        }

        private static Button CreateTitleButton(
            RectTransform parent,
            string name,
            string label,
            UiButtonStyle style)
        {
            Button button = SaveSlotSelectionController.MakeButton(
                parent, name, Vector2.zero, Vector2.zero);
            LayoutElement element =
                button.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 52f;
            element.minHeight = 44f;
            TMP_Text text = SaveSlotSelectionController.MakeText(
                button.transform as RectTransform,
                label,
                26f,
                Vector2.zero,
                Vector2.zero);
            SaveSlotSelectionController.Stretch(text.rectTransform);
            UiVisualThemeService.ApplyButton(button, style);
            return button;
        }

        private static void CreateFooter(RectTransform root)
        {
            TMP_Text copyright = SaveSlotSelectionController.MakeText(
                root,
                "© MAD CAMP · UNDER THE HORIZON",
                18f,
                Vector2.zero,
                Vector2.zero);
            copyright.name = "Copyright";
            RuntimeUiLayoutRegistry.CopyWorldLayout(
                copyright.rectTransform,
                ScreenRegionIds.ReadingBottom);
            copyright.alignment = TextAlignmentOptions.BottomLeft;
            UiVisualThemeService.ApplyText(
                copyright,
                UiTextStyle.Caption);

            TMP_Text version = SaveSlotSelectionController.MakeText(
                root,
                $"버전 {Application.version}",
                18f,
                Vector2.zero,
                Vector2.zero);
            version.name = "Version";
            RuntimeUiLayoutRegistry.CopyWorldLayout(
                version.rectTransform,
                ScreenRegionIds.PrimaryBottomRight);
            version.alignment = TextAlignmentOptions.BottomRight;
            UiVisualThemeService.ApplyText(
                version,
                UiTextStyle.Technical);
        }

    }

    [DisallowMultipleComponent]
    public sealed class SaveSlotSelectionController : MonoBehaviour
    {
        private const float RevealDuration = 4f;
        private const float DiveDuration = 2.2f;
        private const float RiseDuration = RevealDuration - DiveDuration;
        private static readonly Vector3 WaterRevealStart = new(0f, -17f, 2f);
        private static readonly Vector3 WaterRevealEnd = new(0f, -4f, 2f);

        private GameObject overlay;
        private GameObject confirmation;
        private RectTransform contentRect;
        private RectTransform lobbyContent;
        private RectTransform ingamePanel;
        private Transform water;
        private LightShaftEffect lightShaft;
        private Coroutine revealRoutine;
        private int pendingSlot;
        private bool pendingContinue;
        private bool pendingDelete;
        private readonly TMP_Text[] slotLabels = new TMP_Text[3];
        private readonly Button[] deleteButtons = new Button[3];

        public void Open()
        {
            EnsureBuilt();
            Refresh();
            overlay.SetActive(true);
            confirmation.SetActive(false);
            PlayTransition(showing: true);
        }

        private void Close()
        {
            confirmation.SetActive(false);
            PlayTransition(showing: false);
            UIManager.Instance?.SetSystemScreenState(
                SystemScreenState.Title);
        }

        private void PlayTransition(bool showing)
        {
            if (revealRoutine != null)
            {
                StopCoroutine(revealRoutine);
            }
            revealRoutine = StartCoroutine(TransitionRoutine(showing));
        }

        private static float EaseInCubic(float t) => t * t * t;

        private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        private static IEnumerator RunSegment(
            float duration, Func<float, float> ease, Action<float> apply)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                apply(ease(Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }
            apply(1f);
        }

        // Two-stage motion: submerging accelerates into the dive (ease-in),
        // then the save slot panel decelerates as it settles into view
        // (ease-out). Closing plays the same two stages in reverse order
        // so the panel dives out first and the water settles back last.
        private IEnumerator TransitionRoutine(bool showing)
        {
            float travel = ((RectTransform)transform).rect.height;
            Vector2 shown = Vector2.zero;
            Vector2 hidden = new Vector2(0f, -travel);
            Vector2 slotFrom = showing ? hidden : shown;
            Vector2 slotTo = showing ? shown : hidden;
            Vector3 waterFrom = showing ? WaterRevealStart : WaterRevealEnd;
            Vector3 waterTo = showing ? WaterRevealEnd : WaterRevealStart;
            Vector2 lobbyShown = Vector2.zero;
            Vector2 lobbyExited = new Vector2(0f, travel);
            Vector2 lobbyFrom = showing ? lobbyShown : lobbyExited;
            Vector2 lobbyTo = showing ? lobbyExited : lobbyShown;

            contentRect.anchoredPosition = slotFrom;
            water = water != null ? water : GameObject.Find("water")?.transform;
            lightShaft = lightShaft != null
                ? lightShaft
                : water?.GetComponentInChildren<LightShaftEffect>(true);
            if (lightShaft != null && !lightShaft.gameObject.activeSelf)
            {
                lightShaft.gameObject.SetActive(true);
            }
            lobbyContent = lobbyContent != null
                ? lobbyContent
                : transform.Find("Title Presentation") as RectTransform;
            if (water != null)
            {
                water.position = waterFrom;
            }
            lightShaft?.SetIntensity(showing ? 0f : 1f);
            if (lobbyContent != null)
            {
                lobbyContent.anchoredPosition = lobbyFrom;
            }

            IEnumerator PanelStage(Func<float, float> ease, float duration)
            {
                return RunSegment(duration, ease, t =>
                {
                    contentRect.anchoredPosition =
                        Vector2.LerpUnclamped(slotFrom, slotTo, t);
                    if (lobbyContent != null)
                    {
                        lobbyContent.anchoredPosition =
                            Vector2.LerpUnclamped(lobbyFrom, lobbyTo, t);
                    }
                });
            }

            IEnumerator WaterStage(
                Func<float, float> ease, float duration, bool intensityRising)
            {
                return RunSegment(duration, ease, t =>
                {
                    if (water != null)
                    {
                        water.position =
                            Vector3.LerpUnclamped(waterFrom, waterTo, t);
                    }
                    lightShaft?.SetIntensity(intensityRising ? t : 1f - t);
                });
            }

            if (showing)
            {
                yield return WaterStage(EaseInCubic, DiveDuration, true);
                yield return PanelStage(EaseOutCubic, RiseDuration);
            }
            else
            {
                yield return PanelStage(EaseInCubic, DiveDuration);
                yield return WaterStage(EaseOutCubic, RiseDuration, false);
            }

            contentRect.anchoredPosition = slotTo;
            if (water != null)
            {
                water.position = waterTo;
            }
            if (lobbyContent != null)
            {
                lobbyContent.anchoredPosition = lobbyTo;
            }
            lightShaft?.SetIntensity(showing ? 1f : 0f);
            if (!showing)
            {
                overlay.SetActive(false);
            }
            revealRoutine = null;
        }

        private void OnDisable()
        {
            if (revealRoutine != null)
            {
                StopCoroutine(revealRoutine);
                revealRoutine = null;
            }
        }

        private void EnsureBuilt()
        {
            if (overlay != null)
            {
                return;
            }
            overlay = Panel(
                transform,
                "Save Slot Selection",
                UiVisualThemeService.Resolve(UiColorToken.Canvas));
            RectTransform root = overlay.GetComponent<RectTransform>();
            Stretch(root);
            contentRect = root;
            GameObject frame = Panel(
                root,
                "Slot Frame",
                UiVisualThemeService.Resolve(UiColorToken.Surface));
            RectTransform frameRect = frame.GetComponent<RectTransform>();
            RuntimeUiLayoutRegistry.CopyWorldLayout(
                frameRect,
                ScreenRegionIds.ContentCenter);
            frame.AddComponent<Outline>().effectColor =
                UiVisualThemeService.Resolve(UiColorToken.Brass);
            HorizontalLayoutGroup cards =
                frame.AddComponent<HorizontalLayoutGroup>();
            cards.spacing =
                UiVisualThemeService.Resolve(UiSpacingToken.Medium);
            int padding = Mathf.RoundToInt(
                UiVisualThemeService.Resolve(UiSpacingToken.Large));
            cards.padding =
                new RectOffset(padding, padding, padding, padding);
            cards.childAlignment = TextAnchor.MiddleCenter;
            cards.childControlWidth = true;
            cards.childControlHeight = true;
            cards.childForceExpandWidth = true;
            cards.childForceExpandHeight = true;

            TMP_Text heading = MakeText(
                root,
                "항해 기록 선택",
                46,
                Vector2.zero,
                Vector2.zero);
            heading.name = "Title";
            RuntimeUiLayoutRegistry.CopyWorldLayout(
                heading.rectTransform,
                ScreenRegionIds.ObjectiveTop);
            UiVisualThemeService.ApplyText(
                heading,
                UiTextStyle.Display);
            TMP_Text guide = MakeText(
                root,
                "저장된 기록은 이어서, 빈 기록은 처음부터 시작합니다.",
                21,
                Vector2.zero,
                Vector2.zero);
            guide.name = "Guide";
            RuntimeUiLayoutRegistry.CopyWorldLayout(
                guide.rectTransform,
                ScreenRegionIds.ReadingBottom);
            UiVisualThemeService.ApplyText(
                guide,
                UiTextStyle.Body);

            for (int index = 0; index < 3; index++)
            {
                int slot = index + 1;
                GameObject card = new(
                    $"Slot Card {slot}",
                    typeof(RectTransform),
                    typeof(VerticalLayoutGroup),
                    typeof(LayoutElement));
                card.transform.SetParent(frameRect, false);
                LayoutElement cardElement =
                    card.GetComponent<LayoutElement>();
                cardElement.minWidth = 220f;
                cardElement.flexibleWidth = 1f;
                VerticalLayoutGroup cardLayout =
                    card.GetComponent<VerticalLayoutGroup>();
                cardLayout.spacing =
                    UiVisualThemeService.Resolve(UiSpacingToken.Small);
                cardLayout.childAlignment = TextAnchor.MiddleCenter;
                cardLayout.childControlWidth = true;
                cardLayout.childControlHeight = true;
                cardLayout.childForceExpandWidth = true;
                cardLayout.childForceExpandHeight = false;

                Button button = MakeButton(
                    card.transform as RectTransform,
                    $"Save Slot {slot}",
                    Vector2.zero,
                    Vector2.zero);
                LayoutElement element =
                    button.gameObject.AddComponent<LayoutElement>();
                element.minHeight = 190f;
                element.flexibleHeight = 1f;
                slotLabels[index] = MakeText(
                    button.transform as RectTransform, string.Empty, 25,
                    Vector2.zero, Vector2.zero);
                Stretch(slotLabels[index].rectTransform);
                UiVisualThemeService.ApplyButton(
                    button,
                    UiButtonStyle.Secondary);
                button.onClick.AddListener(() => Ask(slot));

                Button delete = MakeButton(
                    card.transform as RectTransform,
                    $"Delete Slot {slot}",
                    Vector2.zero,
                    Vector2.zero);
                LayoutElement deleteElement =
                    delete.gameObject.AddComponent<LayoutElement>();
                deleteElement.preferredHeight = 52f;
                deleteElement.minHeight = 46f;
                TMP_Text deleteLabel = MakeText(
                    delete.transform as RectTransform,
                    "삭제",
                    21f,
                    Vector2.zero,
                    Vector2.zero);
                Stretch(deleteLabel.rectTransform);
                UiVisualThemeService.ApplyButton(
                    delete,
                    UiButtonStyle.Danger);
                delete.onClick.AddListener(() => AskDelete(slot));
                deleteButtons[index] = delete;
            }
            Button close = MakeButton(
                root, "닫기", Vector2.zero, Vector2.zero);
            RuntimeUiLayoutRegistry.CopyWorldLayout(
                close.transform as RectTransform,
                ScreenRegionIds.ToolsBottomLeft);
            TMP_Text closeLabel = MakeText(
                close.transform as RectTransform,
                "돌아가기",
                24,
                Vector2.zero,
                Vector2.zero);
            Stretch(closeLabel.rectTransform);
            UiVisualThemeService.ApplyButton(
                close,
                UiButtonStyle.Secondary);
            close.onClick.AddListener(Close);

            confirmation = Panel(
                root,
                "Start Confirmation",
                UiVisualThemeService.Resolve(UiColorToken.SurfaceOverlay));
            RectTransform confirmRect = confirmation.GetComponent<RectTransform>();
            Stretch(confirmRect);
            TMP_Text message = MakeText(
                confirmRect,
                string.Empty,
                28,
                Vector2.zero,
                Vector2.zero);
            message.name = "Message";
            RuntimeUiLayoutRegistry.CopyWorldLayout(
                message.rectTransform,
                ScreenRegionIds.ContentCenter);
            UiVisualThemeService.ApplyText(
                message,
                UiTextStyle.BodyLarge);
            Button yes = MakeButton(
                confirmRect,
                "Confirm",
                Vector2.zero,
                Vector2.zero);
            RuntimeUiLayoutRegistry.CopyWorldLayout(
                yes.transform as RectTransform,
                ScreenRegionIds.PrimaryBottomRight);
            TMP_Text yesLabel = MakeText(
                yes.transform as RectTransform,
                "확인",
                24,
                Vector2.zero,
                Vector2.zero);
            Stretch(yesLabel.rectTransform);
            UiVisualThemeService.ApplyButton(
                yes,
                UiButtonStyle.Primary);
            yes.onClick.AddListener(Confirm);
            Button no = MakeButton(
                confirmRect,
                "Cancel",
                Vector2.zero,
                Vector2.zero);
            RuntimeUiLayoutRegistry.CopyWorldLayout(
                no.transform as RectTransform,
                ScreenRegionIds.ToolsBottomLeft);
            TMP_Text noLabel = MakeText(
                no.transform as RectTransform,
                "취소",
                24,
                Vector2.zero,
                Vector2.zero);
            Stretch(noLabel.rectTransform);
            UiVisualThemeService.ApplyButton(
                no,
                UiButtonStyle.Secondary);
            no.onClick.AddListener(() => confirmation.SetActive(false));
            confirmation.SetActive(false);
            overlay.SetActive(false);
        }

        private void Refresh()
        {
            for (int index = 0; index < slotLabels.Length; index++)
            {
                bool occupied = GameStateManager.HasSaveDataInSlot(index + 1);
                Button button = slotLabels[index]
                    .GetComponentInParent<Button>();
                if (button != null)
                {
                    button.image.color = occupied
                        ? new Color32(51, 34, 78, 252)
                        : new Color32(18, 31, 52, 252);
                }
                deleteButtons[index].gameObject.SetActive(occupied);
                slotLabels[index].text =
                    $"항해 기록 {index + 1}\n\n" +
                    (occupied
                        ? "저장된 수사 기록\n\n이어하기"
                        : "비어 있는 기록\n\n새로하기");
            }
        }

        private void Ask(int slot)
        {
            pendingSlot = slot;
            pendingContinue = GameStateManager.HasSaveDataInSlot(slot);
            pendingDelete = false;
            confirmation.transform.Find("Message").GetComponent<TMP_Text>().text =
                pendingContinue
                    ? $"{slot}번 슬롯의 수사를 이어하시겠습니까?"
                    : $"{slot}번 슬롯에서 새 수사를 시작하시겠습니까?";
            confirmation.SetActive(true);
            confirmation.transform.SetAsLastSibling();
        }

        private void AskDelete(int slot)
        {
            pendingSlot = slot;
            pendingContinue = false;
            pendingDelete = true;
            confirmation.transform.Find("Message").GetComponent<TMP_Text>().text =
                $"{slot}번 슬롯의 저장 기록을 삭제하시겠습니까?\n" +
                "삭제한 기록은 복구할 수 없습니다.";
            confirmation.SetActive(true);
            confirmation.transform.SetAsLastSibling();
        }

        private void Confirm()
        {
            confirmation.SetActive(false);
            if (pendingDelete)
            {
                GameStateManager.DeleteSaveSlot(pendingSlot);
                pendingDelete = false;
                Refresh();
                return;
            }

            if (revealRoutine != null)
            {
                StopCoroutine(revealRoutine);
            }
            revealRoutine = StartCoroutine(
                EnterGameRoutine(pendingSlot, pendingContinue));
        }

        private IEnumerator EnterGameRoutine(int slot, bool continuing)
        {
            ingamePanel = ingamePanel != null
                ? ingamePanel
                : GameObject.Find("Canvas")?.transform.Find("Ingame")
                    as RectTransform;

            float travel = ((RectTransform)transform).rect.height;
            Vector2 ingameHidden = new Vector2(0f, -travel);
            Vector2 ingameShown = Vector2.zero;

            if (ingamePanel != null)
            {
                ingamePanel.gameObject.SetActive(true);
                ingamePanel.anchoredPosition = ingameHidden;
            }

            Coroutine exitRoutine = StartCoroutine(TransitionRoutine(showing: false));

            float elapsed = 0f;
            while (elapsed < RevealDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / RevealDuration);
                if (ingamePanel != null)
                {
                    ingamePanel.anchoredPosition =
                        Vector2.LerpUnclamped(ingameHidden, ingameShown, t);
                }
                yield return null;
            }
            if (ingamePanel != null)
            {
                ingamePanel.anchoredPosition = ingameShown;
            }
            yield return exitRoutine;

            revealRoutine = null;
            if (continuing)
            {
                UIManager.Instance?.ContinueGameInSlot(slot);
            }
            else
            {
                UIManager.Instance?.StartNewGameInSlot(slot);
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
            GameObject value = Panel(parent, name, new Color32(35, 24, 55, 248));
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
