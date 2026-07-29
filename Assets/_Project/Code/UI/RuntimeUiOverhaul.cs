using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wake.Core;
using Wake.Evidence;
using Wake.Exploration;

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

    [DisallowMultipleComponent]
    public sealed class RuntimeUiOverhaulController : MonoBehaviour
    {
        private float nextScan;

        private void Start()
        {
            ConfigureCanvas();
            ConfigurePanels();
            ScanButtons();
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
            UiPanelTransitionAnimator animator =
                panel.GetComponent<UiPanelTransitionAnimator>() ??
                panel.gameObject.AddComponent<UiPanelTransitionAnimator>();
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
            ApplyLogoLayout();
        }

        private void OnEnable()
        {
            Build();
            ApplyLogoLayout();
            if (presentation != null)
            {
                presentation.SetActive(true);
            }
        }

        private void Build()
        {
            if (presentation == null)
            {
                presentation =
                    transform.Find("Title Presentation")?.gameObject;
            }
            if (presentation != null)
            {
                ApplyLogoLayout();
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

            GameObject backdropObject = new(
                "Lobby Backdrop", typeof(RectTransform));
            backdropObject.transform.SetParent(transform, false);
            RectTransform backdropRect =
                backdropObject.GetComponent<RectTransform>();
            SaveSlotSelectionController.Stretch(backdropRect);
            backdropObject.AddComponent<LobbyBackdropController>();

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
            presentation.AddComponent<UiPanelTransitionAnimator>();
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
            ApplyLogoLayout(logoRect);
            Image logo = logoObject.GetComponent<Image>();
            logo.sprite =
                Resources.Load<Sprite>("UiOverhaul/logo_transparent");
            logo.preserveAspect = true;
            logo.raycastTarget = false;
        }

        private void ApplyLogoLayout()
        {
            RectTransform logoRect = presentation?.transform.Find(
                    "Under the Horizon Logo")
                as RectTransform;
            ApplyLogoLayout(logoRect);
        }

        private static void ApplyLogoLayout(RectTransform logoRect)
        {
            if (logoRect == null)
                return;

            // Exactly twice the original 24% x 14% title-safe slot.
            // The right edge stops at screen center so it cannot cover the
            // character composition, and it remains well above the menu.
            logoRect.anchorMin = new Vector2(0.00f, 0.10f);
            logoRect.anchorMax = new Vector2(0.50f, 1.00f);
            logoRect.offsetMin = Vector2.zero;
            logoRect.offsetMax = Vector2.zero;
            logoRect.localScale = Vector3.one;
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
            GameObject footerObject = new(
                "Title Footer",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup));
            footerObject.transform.SetParent(root, false);
            RectTransform footer =
                footerObject.GetComponent<RectTransform>();
            RuntimeUiLayoutRegistry.CopyWorldLayout(
                footer,
                ScreenRegionIds.PrimaryBottomRight);
            VerticalLayoutGroup layout =
                footerObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing =
                UiVisualThemeService.Resolve(UiSpacingToken.ExtraSmall);
            layout.childAlignment = TextAnchor.LowerRight;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TMP_Text version = SaveSlotSelectionController.MakeText(
                footer,
                $"버전 {Application.version}",
                18f,
                Vector2.zero,
                Vector2.zero);
            version.name = "Version";
            version.alignment = TextAlignmentOptions.Right;
            UiVisualThemeService.ApplyText(
                version,
                UiTextStyle.Technical);

            TMP_Text copyright = SaveSlotSelectionController.MakeText(
                footer,
                "© MAD CAMP · UNDER THE HORIZON",
                18f,
                Vector2.zero,
                Vector2.zero);
            copyright.name = "Copyright";
            copyright.alignment = TextAlignmentOptions.Right;
            UiVisualThemeService.ApplyText(
                copyright,
                UiTextStyle.Caption);
        }

    }

    [DisallowMultipleComponent]
    public sealed class SaveSlotSelectionController : MonoBehaviour
    {
        private const float RiseDuration = 2.2f;
        private const float DiveDuration = 4f;
        private const float RevealDuration = RiseDuration + DiveDuration;
        private const float FadeInDelay = 1.5f;
        private const float PanelTravelExtra = 2.8f;
        private const float LobbyTravelExtra = 3.5f;
        // The title exit and the water dive must read as one motion, so they
        // start together and share the water's duration/easing exactly.
        private const float LobbyLeadIn = 0f;
        private const float LobbyExitDuration = DiveDuration;
        // The submerged position comes from wherever "water" is placed in
        // the scene, captured once the first time it's resolved - so tuning
        // its dive depth only means moving it there, no hardcoded copy to
        // keep in sync. Only the risen height is a pure animation choice
        // with no scene counterpart.
        private const float WaterRisenY = -7f;
        // Water audio cues (sloshing on dive, splash on surfacing) fire when
        // the water plane crosses this height, not at the animation's start
        // or end, so they land on the visual moment rather than the timer.
        private const float WaterAudioTriggerY = -40f;
        // BGM muffle is scaled so it already reads as "fully muffled" by the
        // time the water crosses this height, instead of only reaching that
        // feeling at the very end of the dive/rise.
        private const float WaterMuffleSaturationY = -30f;
        // On the rise, muffle stays fully saturated until the water climbs
        // above this height, then clears gradually the rest of the way home.
        private const float WaterMuffleClearStartY = -30f;

        private GameObject overlay;
        private GameObject confirmation;
        private RectTransform contentRect;
        private RectTransform lobbyContent;
        private LobbyBackdropController lobbyBackdrop;
        private RectTransform ingamePanel;
        private Transform water;
        private Vector3? waterHome;
        private LightShaftEffect lightShaft;
        private Coroutine revealRoutine;
        private int pendingSlot;
        private bool pendingContinue;
        private bool pendingDelete;
        private readonly TMP_Text[] slotLabels = new TMP_Text[3];
        private readonly Button[] deleteButtons = new Button[3];

        private Vector3 WaterHome => waterHome ?? Vector3.zero;

        private Vector3 WaterRisen => new(WaterHome.x, WaterRisenY, WaterHome.z);

        private Transform ResolveWater()
        {
            if (water == null)
            {
                water = GameObject.Find("water")?.transform;
                if (water != null)
                {
                    waterHome = water.position;
                }
            }
            return water;
        }

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

        private static float EaseOutQuint(float t) => 1f - Mathf.Pow(1f - t, 5f);

        private static float EaseInQuint(float t) => t * t * t * t * t;

        // Smoothed trapezoidal velocity profile: ease into a brief
        // peak-speed cruise, then ease back down to a full stop. Unlike a
        // plain linear-ramp trapezoid, velocity approaches and leaves vMax
        // with zero acceleration at each phase boundary, so there's no
        // jerk (instant acceleration change) at the seams - that jerk is
        // what reads as a mechanical "snap" rather than natural motion.
        private static float WaterTrapezoid(float t)
        {
            const float accel = 0.3f;
            const float decel = 0.25f;
            const float hold = 1f - accel - decel;
            float vMax = 1f / (0.5f * accel + hold + 0.5f * decel);

            if (t < accel)
            {
                float a = t / accel;
                return accel * vMax * (a * a * a - 0.5f * a * a * a * a);
            }
            if (t < accel + hold)
            {
                float pAccel = accel * vMax * 0.5f;
                return pAccel + vMax * (t - accel);
            }
            {
                float pAccel = accel * vMax * 0.5f;
                float pHold = pAccel + vMax * hold;
                float b = (t - accel - hold) / decel;
                return pHold + decel * vMax * (b - b * b * b + 0.5f * b * b * b * b);
            }
        }

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

        private IEnumerator MoveRect(
            RectTransform rect, Vector2 from, Vector2 to,
            Func<float, float> ease, float duration)
        {
            return RunSegment(duration, ease, t =>
            {
                if (rect != null)
                {
                    rect.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
                }
            });
        }

        private IEnumerator MoveWater(
            Vector3 from, Vector3 to, Func<float, float> ease,
            float duration, bool intensityRising,
            Action<float> onDepth = null,
            float? triggerY = null,
            Action onTriggerY = null)
        {
            bool triggered = false;
            bool ascending = to.y >= from.y;
            float lowY = Mathf.Min(from.y, to.y);
            float highY = Mathf.Max(from.y, to.y);
            float muffleSaturationDepth = Mathf.InverseLerp(
                lowY, highY, WaterMuffleSaturationY);
            float muffleClearStartDepth = Mathf.InverseLerp(
                lowY, highY, WaterMuffleClearStartY);
            return RunSegment(duration, ease, t =>
            {
                Vector3 current = Vector3.LerpUnclamped(from, to, t);
                if (water != null)
                {
                    water.position = current;
                }
                float depth = intensityRising ? t : 1f - t;
                lightShaft?.SetIntensity(depth);
                lobbyBackdrop?.SetDepth(depth);
                // Diving saturates quickly (muffleSaturationDepth) so the
                // muffle feels sudden right past the surface. Surfacing
                // stays fully muffled until the water climbs back above
                // muffleClearStartDepth, then clears the rest of the way -
                // same shape, later reference point, so it reads slower.
                float muffleReferenceDepth = intensityRising
                    ? muffleSaturationDepth
                    : muffleClearStartDepth;
                float muffleDepth = muffleReferenceDepth > 0f
                    ? Mathf.Clamp01(depth / muffleReferenceDepth)
                    : depth;
                onDepth?.Invoke(muffleDepth);
                if (!triggered && triggerY.HasValue)
                {
                    bool crossed = ascending
                        ? current.y >= triggerY.Value
                        : current.y <= triggerY.Value;
                    if (crossed)
                    {
                        triggered = true;
                        onTriggerY?.Invoke();
                    }
                }
            });
        }

        // Symmetric three-stage motion in both directions: opening plays
        // lobby-exit -> water dive -> slot-rise; closing plays the exact
        // mirror, slot-exit -> water-surface -> lobby-return, so the two
        // directions read as the same fall replayed backwards.
        private IEnumerator TransitionRoutine(bool showing)
        {
            float travel = ((RectTransform)transform).rect.height;
            Vector2 shown = Vector2.zero;
            Vector2 hidden = new Vector2(0f, -travel * PanelTravelExtra);
            Vector2 slotFrom = showing ? hidden : shown;
            Vector2 slotTo = showing ? shown : hidden;
            Vector2 lobbyShown = Vector2.zero;
            Vector2 lobbyExited = new Vector2(0f, travel * LobbyTravelExtra);
            Vector2 lobbyFrom = showing ? lobbyShown : lobbyExited;
            Vector2 lobbyTo = showing ? lobbyExited : lobbyShown;

            contentRect.anchoredPosition = slotFrom;
            ResolveWater();
            Vector3 waterFrom = showing ? WaterHome : WaterRisen;
            Vector3 waterTo = showing ? WaterRisen : WaterHome;
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
            lobbyBackdrop = lobbyBackdrop != null
                ? lobbyBackdrop
                : transform.Find("Lobby Backdrop")
                    ?.GetComponent<LobbyBackdropController>();
            if (water != null)
            {
                water.position = waterFrom;
            }
            lightShaft?.SetIntensity(showing ? 0f : 1f);
            if (lobbyContent != null)
            {
                lobbyContent.anchoredPosition = lobbyFrom;
            }

            if (showing)
            {
                Coroutine lobbyExit = StartCoroutine(MoveRect(
                    lobbyContent, lobbyFrom, lobbyTo, WaterTrapezoid, LobbyExitDuration));
                yield return new WaitForSecondsRealtime(LobbyLeadIn);
                yield return MoveWater(
                    waterFrom, waterTo, WaterTrapezoid, DiveDuration, true,
                    depth => AudioManager.Instance?.SetUnderwaterMuffle(depth),
                    WaterAudioTriggerY,
                    () => AudioManager.Instance?.PlayWaterSloshing());
                yield return lobbyExit;
                yield return MoveRect(
                    contentRect, slotFrom, slotTo, EaseOutQuint, RiseDuration);
            }
            else
            {
                yield return MoveRect(
                    contentRect, slotFrom, slotTo, EaseInQuint, RiseDuration);
                Coroutine waterSurface = StartCoroutine(
                    MoveWater(
                        waterFrom, waterTo, WaterTrapezoid, DiveDuration, false,
                        depth => AudioManager.Instance?.SetUnderwaterMuffle(depth),
                        WaterAudioTriggerY,
                        () => AudioManager.Instance?.PlayWaterSplashOut()));
                yield return new WaitForSecondsRealtime(LobbyLeadIn);
                Coroutine lobbyEnter = StartCoroutine(MoveRect(
                    lobbyContent, lobbyFrom, lobbyTo, WaterTrapezoid, LobbyExitDuration));
                yield return waterSurface;
                yield return lobbyEnter;
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
            // The reading-bottom slot overlaps the delete buttons on each
            // slot card; being a later sibling, this label's default
            // raycastTarget swallowed clicks meant for them.
            guide.raycastTarget = false;

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

        // Mirrors TransitionRoutine's closing half exactly, except the
        // panel that rises into view at the end is Ingame instead of the
        // lobby - same slot-exit -> water-surface -> tail-entrance shape.
        private IEnumerator EnterGameRoutine(int slot, bool continuing)
        {
            // This dive doesn't drive the underwater muffle (unlike
            // TransitionRoutine's), but if the player dove into the slot
            // list without surfacing back out first, the low-pass filter
            // is still left muffled from that dive - clear it so gameplay
            // BGM never starts muffled.
            AudioManager.Instance?.SetUnderwaterMuffle(0f);
            AudioManager.Instance?.StopMusicForGameEntry();
            StartCoroutine(DelayedFadeIn(
                FadeInDelay, RevealDuration - FadeInDelay, Color.white));

            ingamePanel = ingamePanel != null
                ? ingamePanel
                : GameObject.Find("Canvas")?.transform.Find("Ingame")
                    as RectTransform;
            RectTransform locationBackground = LocationLoader.Instance?.BackgroundRect;

            float travel = ((RectTransform)transform).rect.height;
            Vector2 slotShown = Vector2.zero;
            Vector2 slotHidden = new Vector2(0f, -travel * PanelTravelExtra);
            Vector2 ingameHidden = new Vector2(0f, travel);
            Vector2 ingameShown = Vector2.zero;

            ResolveWater();
            lightShaft = lightShaft != null
                ? lightShaft
                : water?.GetComponentInChildren<LightShaftEffect>(true);
            lobbyBackdrop = lobbyBackdrop != null
                ? lobbyBackdrop
                : transform.Find("Lobby Backdrop")
                    ?.GetComponent<LobbyBackdropController>();

            if (ingamePanel != null)
            {
                ingamePanel.gameObject.SetActive(true);
                ingamePanel.anchoredPosition = ingameHidden;
            }
            if (locationBackground != null)
            {
                locationBackground.anchoredPosition = ingameHidden;
            }

            yield return MoveRect(
                contentRect, slotShown, slotHidden, EaseInQuint, RiseDuration);
            Coroutine waterSurface = StartCoroutine(MoveWater(
                WaterRisen, WaterHome, WaterTrapezoid, DiveDuration, false,
                null,
                WaterAudioTriggerY,
                () => AudioManager.Instance?.PlayWaterSplashOut()));
            yield return new WaitForSecondsRealtime(LobbyLeadIn);
            Coroutine ingameEnter = ingamePanel != null
                ? StartCoroutine(MoveRect(
                    ingamePanel, ingameHidden, ingameShown, WaterTrapezoid, LobbyExitDuration))
                : null;
            Coroutine backgroundEnter = locationBackground != null
                ? StartCoroutine(MoveRect(
                    locationBackground, ingameHidden, ingameShown,
                    WaterTrapezoid, LobbyExitDuration))
                : null;
            yield return waterSurface;
            if (ingameEnter != null)
            {
                yield return ingameEnter;
            }
            if (backgroundEnter != null)
            {
                yield return backgroundEnter;
            }

            contentRect.anchoredPosition = slotHidden;
            if (water != null)
            {
                water.position = WaterHome;
            }
            if (lobbyContent != null)
            {
                lobbyContent.anchoredPosition = Vector2.zero;
            }
            lightShaft?.SetIntensity(0f);
            overlay.SetActive(false);

            revealRoutine = null;
            // ContinueGameInSlot/StartNewGameInSlot deactivate this
            // controller's own GameObject (via SetActivePanel), which kills
            // this coroutine immediately after - so FadeOut is fired here
            // rather than yielded on, and runs to completion on
            // ScreenFadeTransition's own (still-active) Canvas object.
            if (continuing)
            {
                UIManager.Instance?.ContinueGameInSlot(slot);
            }
            else
            {
                UIManager.Instance?.StartNewGameInSlot(slot);
            }

            ScreenFadeTransition.Ensure()?.FadeOut(0.4f);
        }

        private static IEnumerator DelayedFadeIn(
            float delay, float duration, Color color)
        {
            yield return new WaitForSecondsRealtime(delay);
            ScreenFadeTransition.Ensure()?.FadeIn(duration, color);
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
            button.onClick.AddListener(PlayClickSfx);
            return button;
        }

        private static void PlayClickSfx() =>
            AudioManager.Instance?.PlayButtonClick();

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
        private sealed class NoticeCard
        {
            public RectTransform Rect;
            public TMP_Text Text;
            public string Key;
            public float TopSince;
            public Coroutine Movement;
        }

        public const int MaximumVisibleNotices = 3;

        private const float CardWidth = 318f;
        private const float CardHeight = 68f;
        private const float CardGap = 9f;
        private const float RightMargin = 18f;
        private const float TopOffset = 168f;
        private const float SlideSeconds = 0.24f;
        private const float ReflowSeconds = 0.18f;
        private const float HoldSeconds = 2f;

        private readonly Queue<string> pending = new();
        private readonly HashSet<string> scheduled =
            new(StringComparer.Ordinal);
        private readonly List<NoticeCard> visible = new();
        private readonly List<NoticeCard> cards = new();

        private RectTransform canvas;
        private EvidenceInventory boundInventory;
        private Coroutine queueRoutine;

        public static EvidenceAcquisitionNoticeController Instance
        {
            get;
            private set;
        }

        public int PendingNoticeCount => pending.Count;
        public int VisibleNoticeCount => visible.Count;
        public IReadOnlyList<string> VisibleMessages =>
            visible.ConvertAll(card => card.Text.text);

        private void Awake()
        {
            Instance = this;
        }

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
            if (queueRoutine != null)
            {
                StopCoroutine(queueRoutine);
                queueRoutine = null;
            }
            if (boundInventory != null)
            {
                boundInventory.EvidenceAdded -= Show;
            }
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void EnsureBuilt()
        {
            if (canvas != null)
            {
                return;
            }
            canvas = GameObject.Find("Canvas")
                ?.transform as RectTransform;
        }

        private void Show(EvidenceDefinition evidence)
        {
            Enqueue(
                EvidencePlayerFacingText.AcquisitionMessage(evidence));
            AudioManager.Instance?.PlayEvidencePickup();
        }

        public void EnqueuePlayerMessage(string message)
        {
            if (!EvidencePlayerFacingText.TryExtractAcquisitionName(
                    message,
                    out string displayName))
            {
                return;
            }

            Enqueue($"새로운 단서를 발견했습니다\n{displayName}");
        }

        private void Enqueue(string message)
        {
            string sanitized =
                EvidencePlayerFacingText.SanitizeMessage(message);
            if (string.IsNullOrEmpty(sanitized) ||
                !scheduled.Add(sanitized))
            {
                return;
            }

            pending.Enqueue(sanitized);
            if (queueRoutine == null)
            {
                queueRoutine = StartCoroutine(ProcessQueue());
            }
        }

        private IEnumerator ProcessQueue()
        {
            EnsureBuilt();
            while (pending.Count > 0 || visible.Count > 0)
            {
                FillAvailableSlots();
                if (visible.Count == 0)
                {
                    yield return null;
                    continue;
                }

                NoticeCard top = visible[0];
                if (Time.unscaledTime - top.TopSince < HoldSeconds)
                {
                    yield return null;
                    continue;
                }

                Vector2 hidden = HiddenPosition(0);
                yield return MoveCard(
                    top,
                    top.Rect.anchoredPosition,
                    hidden,
                    SlideSeconds);
                visible.RemoveAt(0);
                scheduled.Remove(top.Key);
                top.Key = string.Empty;
                top.Rect.gameObject.SetActive(false);

                if (visible.Count > 0)
                {
                    yield return ReflowVisibleCards();
                    visible[0].TopSince = Time.unscaledTime;
                }
            }

            queueRoutine = null;
        }

        private void FillAvailableSlots()
        {
            while (pending.Count > 0 &&
                   visible.Count < MaximumVisibleNotices)
            {
                NoticeCard card = GetAvailableCard();
                string message = pending.Dequeue();
                int index = visible.Count;
                card.Key = message;
                card.Text.text = message;
                card.Rect.anchoredPosition = HiddenPosition(index);
                card.Rect.gameObject.SetActive(true);
                visible.Add(card);
                if (index == 0)
                {
                    card.TopSince =
                        Time.unscaledTime + SlideSeconds;
                }
                card.Movement = StartCoroutine(MoveCard(
                    card,
                    HiddenPosition(index),
                    ShownPosition(index),
                    SlideSeconds));
            }
        }

        private NoticeCard GetAvailableCard()
        {
            foreach (NoticeCard card in cards)
            {
                if (!card.Rect.gameObject.activeSelf)
                {
                    return card;
                }
            }

            EnsureBuilt();
            int number = cards.Count + 1;
            GameObject panel = SaveSlotSelectionController.Panel(
                canvas,
                $"Evidence Acquisition Notice {number}",
                Color.black);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.sizeDelta = new Vector2(CardWidth, CardHeight);
            Image background = panel.GetComponent<Image>();
            background.sprite = null;
            background.material = null;
            background.type = Image.Type.Simple;
            background.color = Color.black;

            TMP_Text text = SaveSlotSelectionController.MakeText(
                rect,
                string.Empty,
                18f,
                new Vector2(-8f, 0f),
                new Vector2(CardWidth - 34f, CardHeight - 12f));
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.color = new Color32(245, 239, 224, 255);
            text.textWrappingMode = TextWrappingModes.Normal;
            panel.SetActive(false);

            var created = new NoticeCard
            {
                Rect = rect,
                Text = text,
                Key = string.Empty
            };
            cards.Add(created);
            return created;
        }

        private IEnumerator ReflowVisibleCards()
        {
            Vector2[] starts = new Vector2[visible.Count];
            for (int index = 0; index < visible.Count; index++)
            {
                NoticeCard card = visible[index];
                if (card.Movement != null)
                {
                    StopCoroutine(card.Movement);
                    card.Movement = null;
                }
                starts[index] = card.Rect.anchoredPosition;
            }

            float elapsed = 0f;
            while (elapsed < ReflowSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(elapsed / ReflowSeconds));
                for (int index = 0; index < visible.Count; index++)
                {
                    visible[index].Rect.anchoredPosition = Vector2.Lerp(
                        starts[index],
                        ShownPosition(index),
                        t);
                }
                yield return null;
            }

            for (int index = 0; index < visible.Count; index++)
            {
                visible[index].Rect.anchoredPosition =
                    ShownPosition(index);
            }
        }

        private IEnumerator MoveCard(
            NoticeCard card,
            Vector2 from,
            Vector2 to,
            float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (card?.Rect == null)
                    yield break;
                elapsed += Time.unscaledDeltaTime;
                card.Rect.anchoredPosition = Vector2.Lerp(
                    from,
                    to,
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }
            if (card?.Rect == null)
                yield break;
            card.Rect.anchoredPosition = to;
            card.Movement = null;
        }

        private static Vector2 ShownPosition(int index) =>
            new(
                -RightMargin,
                -TopOffset - index * (CardHeight + CardGap));

        private static Vector2 HiddenPosition(int index) =>
            new(
                CardWidth + 24f,
                -TopOffset - index * (CardHeight + CardGap));
    }
}
