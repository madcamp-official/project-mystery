using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.Core;
using Wake.Narrative;

namespace Wake.UI
{
    public enum SystemScreenState
    {
        None,
        Boot,
        Title,
        SaveSlots,
        Loading,
        ChapterTransition,
        Pause,
        Settings,
        Tutorial,
        Confirmation,
        Credits
    }

    [DisallowMultipleComponent]
    public sealed class SystemScreenFlowController : MonoBehaviour
    {
        private readonly Dictionary<SystemScreenState, GameObject> screens =
            new();

        private UIManager owner;
        private RectTransform canvas;
        private GameObject statusHud;
        private GameObject root;
        private GameObject activeScreen;
        private UIScreenTransitionCoordinator transitions;
        private Action confirmAction;
        private Action cancelAction;
        private SystemScreenState returnState;
        private TMP_Text loadingContext;
        private TMP_Text loadingTitle;
        private TMP_Text chapterContext;
        private TMP_Text chapterTitle;
        private TMP_Text chapterSummary;
        private TMP_Text chapterVesselName;
        private CanvasGroup chapterCanvasGroup;
        private Button chapterContinueButton;
        private RectTransform chapterRouteLine;
        private RectTransform chapterHorizon;
        private Image chapterSky;
        private Image chapterSea;
        private Coroutine chapterFadeRoutine;
        private ChapterTransitionRequest activeChapterRequest;
        private bool chapterContinuePending;
        private TMP_Text tutorialTitle;
        private TMP_Text tutorialBody;
        private TMP_Text confirmationTitle;
        private TMP_Text confirmationBody;

        public SystemScreenState ActiveState { get; private set; }
        public bool IsOverlayOpen =>
            root != null && root.activeSelf && activeScreen != null;

        public void Configure(
            UIManager uiManager,
            RectTransform canvasRoot,
            GameObject gameplayHud)
        {
            owner = uiManager;
            canvas = canvasRoot;
            statusHud = gameplayHud;
            transitions =
                owner != null
                    ? owner.GetComponent<UIScreenTransitionCoordinator>()
                    : null;
            EnsureBuilt();
        }

        public IEnumerator ShowBootOnce()
        {
            Show(SystemScreenState.Boot);
            while (transitions != null && transitions.IsTransitioning)
                yield return null;
            yield return null;
            Close();
            while (IsOverlayOpen)
                yield return null;
            ActiveState = SystemScreenState.Title;
        }

        public void SetPassiveState(SystemScreenState state)
        {
            if (IsOverlayOpen)
                return;

            ActiveState = state;
        }

        public void ShowLoading(
            string context,
            string title)
        {
            EnsureBuilt();
            loadingContext.text = string.IsNullOrWhiteSpace(context)
                ? "다음 목적지를 확인하고 있습니다."
                : context;
            loadingTitle.text = string.IsNullOrWhiteSpace(title)
                ? "장면을 준비하는 중"
                : title;
            Show(SystemScreenState.Loading);
        }

        public void ShowChapterTransition(
            ChapterTransitionRequest request,
            Action continueAction)
        {
            EnsureBuilt();
            activeChapterRequest = request;
            chapterContext.text = request.ChapterLabel;
            chapterTitle.text = request.Title;
            chapterSummary.text = request.Summary;
            confirmAction = continueAction;
            chapterContinuePending = false;
            ConfigureChapterVisuals(request);
            Show(SystemScreenState.ChapterTransition);
            StartChapterPresentation();
        }

        public void ShowChapterTransition(
            string context,
            string title,
            string summary,
            Action continueAction)
        {
            ShowChapterTransition(
                new ChapterTransitionRequest(
                    string.Empty,
                    string.Empty,
                    TransitionKind.DayChange,
                    context,
                    title,
                    summary,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    2.5f),
                continueAction);
        }

        public void OpenPause()
        {
            returnState = ActiveState;
            Show(SystemScreenState.Pause);
        }

        public void ShowTutorial(
            string title,
            string body,
            Action completed = null)
        {
            EnsureBuilt();
            returnState = ActiveState;
            tutorialTitle.text = title ?? "조작 안내";
            tutorialBody.text = body ?? string.Empty;
            confirmAction = completed;
            Show(SystemScreenState.Tutorial);
        }

        public void ShowCredits()
        {
            returnState = SystemScreenState.Title;
            Show(SystemScreenState.Credits);
        }

        public void RequestConfirmation(
            string title,
            string body,
            Action confirmed,
            Action cancelled = null)
        {
            EnsureBuilt();
            returnState = ActiveState;
            confirmationTitle.text = title ?? "확인";
            confirmationBody.text = body ?? string.Empty;
            confirmAction = confirmed;
            cancelAction = cancelled;
            Show(SystemScreenState.Confirmation);
        }

        public void Close()
        {
            Close(null);
        }

        private void Close(Action afterClosed)
        {
            if (!IsOverlayOpen)
            {
                afterClosed?.Invoke();
                return;
            }
            if (transitions != null && transitions.IsTransitioning)
                return;

            if (chapterFadeRoutine != null)
            {
                StopCoroutine(chapterFadeRoutine);
                chapterFadeRoutine = null;
            }

            GameObject outgoing = activeScreen;
            void CompleteClose()
            {
                if (activeScreen != null)
                    activeScreen.SetActive(false);
                activeScreen = null;
                if (root != null)
                    root.SetActive(false);
                owner?.SetSystemScreenOverlayActive(false);
                ActiveState = returnState == SystemScreenState.None
                    ? ResolvePassiveState()
                    : returnState;
                returnState = SystemScreenState.None;
                confirmAction = null;
                cancelAction = null;
            }

            if (transitions == null ||
                !transitions.Run(
                    outgoing,
                    null,
                    CompleteClose,
                    afterClosed))
            {
                CompleteClose();
                afterClosed?.Invoke();
            }
        }

        public void OnSettingsOpened()
        {
            returnState = ActiveState;
            ActiveState = SystemScreenState.Settings;
        }

        public void OnSettingsClosed()
        {
            ActiveState = returnState == SystemScreenState.None
                ? ResolvePassiveState()
                : returnState;
            returnState = SystemScreenState.None;
        }

        private SystemScreenState ResolvePassiveState() =>
            owner != null && owner.ActivePanel == UiPrimaryPanel.Start
                ? SystemScreenState.Title
                : SystemScreenState.None;

        private void EnsureBuilt()
        {
            if (root != null || canvas == null)
                return;

            root = CreatePanel(
                canvas,
                "System Screen Flow",
                UiSurfaceStyle.Canvas);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect);
            root.transform.SetAsLastSibling();
            // Ambient world markers (e.g. the dialogue speech bubble, the
            // interaction notice) use their own overridden-sorting Canvas
            // so they can render above other in-game content - sibling
            // order alone can't beat that, so every system screen
            // (pause, settings, etc.) needs a higher override too, or
            // those markers poke through it.
            Canvas overlayCanvas = root.AddComponent<Canvas>();
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 150;
            // A Graphic registers itself under its nearest ancestor
            // Canvas, so once this became one, its buttons stopped being
            // visible to the root Canvas's GraphicRaycaster - it needs
            // its own to be clickable again.
            root.AddComponent<GraphicRaycaster>();

            BuildBoot();
            BuildLoading();
            BuildChapterTransition();
            BuildPause();
            BuildTutorial();
            BuildConfirmation();
            BuildCredits();
            root.SetActive(false);
        }

        private void BuildBoot()
        {
            GameObject screen = CreateScreen(SystemScreenState.Boot);
            CreateText(
                screen.transform,
                "Boot Brand",
                "UNDER THE HORIZON",
                UiTextStyle.Technical,
                ScreenRegionIds.ObjectiveTop);
            TMP_Text center = CreateText(
                screen.transform,
                "Boot Logo",
                "UNDER\nTHE HORIZON",
                UiTextStyle.Display,
                ScreenRegionIds.ContentCenter);
            center.alignment = TextAlignmentOptions.Center;
            CreateText(
                screen.transform,
                "Autosave Guide",
                "이 게임은 장면 전환 시 진행 상황을 자동으로 저장합니다.",
                UiTextStyle.Body,
                ScreenRegionIds.ReadingBottom);
            CreateText(
                screen.transform,
                "Build Version",
                $"버전 {Application.version}",
                UiTextStyle.Technical,
                ScreenRegionIds.PrimaryBottomRight);
        }

        private void BuildLoading()
        {
            GameObject screen = CreateScreen(SystemScreenState.Loading);
            loadingContext = CreateText(
                screen.transform,
                "Loading Context",
                string.Empty,
                UiTextStyle.Technical,
                ScreenRegionIds.ContextTopLeft);
            loadingTitle = CreateText(
                screen.transform,
                "Loading Title",
                string.Empty,
                UiTextStyle.Heading,
                ScreenRegionIds.ObjectiveTop);
            TMP_Text indicator = CreateText(
                screen.transform,
                "Loading Indicator",
                "· · ·",
                UiTextStyle.Display,
                ScreenRegionIds.ContentCenter);
            indicator.alignment = TextAlignmentOptions.Center;
            CreateText(
                screen.transform,
                "Loading Guide",
                "장면을 준비하는 동안 잠시 기다려 주세요.",
                UiTextStyle.Body,
                ScreenRegionIds.ReadingBottom);
        }

        private void BuildChapterTransition()
        {
            GameObject screen =
                CreateScreen(SystemScreenState.ChapterTransition);
            chapterCanvasGroup = screen.AddComponent<CanvasGroup>();
            chapterCanvasGroup.alpha = 0f;
            chapterSky = CreateChapterLayer(
                screen.transform,
                "Chapter Background",
                new Color(.025f, .055f, .09f, 1f),
                new Vector2(0f, .42f),
                Vector2.one);
            chapterSea = CreateChapterLayer(
                screen.transform,
                "Sea",
                new Color(.02f, .11f, .16f, 1f),
                Vector2.zero,
                new Vector2(1f, .48f));
            Image horizon = CreateChapterLayer(
                screen.transform,
                "Horizon Glow",
                new Color(.64f, .47f, .28f, .32f),
                new Vector2(0f, .455f),
                new Vector2(1f, .465f));
            chapterHorizon = horizon.rectTransform;
            Image route = CreateChapterLayer(
                screen.transform,
                "Route Line",
                new Color(.82f, .71f, .46f, .75f),
                new Vector2(.12f, .455f),
                new Vector2(.88f, .459f));
            chapterRouteLine = route.rectTransform;
            chapterVesselName = CreateText(
                screen.transform,
                "MV Elysium Nameplate",
                string.Empty,
                UiTextStyle.Technical,
                ScreenRegionIds.ReadingBottom);
            chapterVesselName.alignment = TextAlignmentOptions.Center;
            chapterContext = CreateText(
                screen.transform,
                "Chapter Context",
                string.Empty,
                UiTextStyle.Technical,
                ScreenRegionIds.ContextTopLeft);
            chapterTitle = CreateText(
                screen.transform,
                "Chapter Title",
                string.Empty,
                UiTextStyle.Display,
                ScreenRegionIds.ObjectiveTop);
            chapterSummary = CreateText(
                screen.transform,
                "Chapter Summary",
                string.Empty,
                UiTextStyle.BodyLarge,
                ScreenRegionIds.ContentCenter);
            chapterContinueButton = CreateButton(
                screen.transform,
                "계속",
                "계속",
                UiButtonStyle.Primary,
                ScreenRegionIds.PrimaryBottomRight);
            chapterContinueButton.onClick.AddListener(BeginChapterContinue);
        }

        private void BuildPause()
        {
            GameObject screen = CreateScreen(SystemScreenState.Pause, true);
            CreateText(
                screen.transform,
                "Pause Title",
                "일시정지",
                UiTextStyle.Display,
                ScreenRegionIds.ObjectiveTop);
            RectTransform menu = CreateContainer(
                screen.transform,
                "Pause Menu",
                ScreenRegionIds.ContentCenter);
            Button resume = CreateLayoutButton(
                menu, "계속", UiButtonStyle.Primary);
            resume.onClick.AddListener(Close);
            Button save = CreateLayoutButton(
                menu, "저장", UiButtonStyle.Secondary);
            save.onClick.AddListener(() =>
                GameStateManager.Instance?.SaveCurrentState());
            Button settings = CreateLayoutButton(
                menu, "설정", UiButtonStyle.Secondary);
            settings.onClick.AddListener(() => owner?.OpenSettings());
            Button title = CreateLayoutButton(
                menu, "타이틀", UiButtonStyle.Danger);
            title.onClick.AddListener(() =>
                RequestConfirmation(
                    "타이틀로 이동",
                    "저장되지 않은 현재 조작 상태는 사라질 수 있습니다.",
                    () => owner?.ShowStartScene(),
                    OpenPause));
        }

        private void BuildTutorial()
        {
            GameObject screen =
                CreateScreen(SystemScreenState.Tutorial, true);
            tutorialTitle = CreateText(
                screen.transform,
                "Tutorial Title",
                string.Empty,
                UiTextStyle.Heading,
                ScreenRegionIds.ObjectiveTop);
            tutorialBody = CreateText(
                screen.transform,
                "Tutorial Body",
                string.Empty,
                UiTextStyle.BodyLarge,
                ScreenRegionIds.ContentCenter);
            Button skip = CreateButton(
                screen.transform,
                "건너뛰기",
                "건너뛰기",
                UiButtonStyle.Quiet,
                ScreenRegionIds.ToolsBottomLeft);
            skip.onClick.AddListener(Close);
            Button next = CreateButton(
                screen.transform,
                "확인",
                "확인",
                UiButtonStyle.Primary,
                ScreenRegionIds.PrimaryBottomRight);
            next.onClick.AddListener(ConfirmAndClose);
        }

        private void BuildConfirmation()
        {
            GameObject screen =
                CreateScreen(SystemScreenState.Confirmation, true);
            confirmationTitle = CreateText(
                screen.transform,
                "Confirmation Title",
                string.Empty,
                UiTextStyle.Heading,
                ScreenRegionIds.ObjectiveTop);
            confirmationBody = CreateText(
                screen.transform,
                "Confirmation Body",
                string.Empty,
                UiTextStyle.BodyLarge,
                ScreenRegionIds.ContentCenter);
            Button cancel = CreateButton(
                screen.transform,
                "취소",
                "취소",
                UiButtonStyle.Secondary,
                ScreenRegionIds.ToolsBottomLeft);
            cancel.onClick.AddListener(CancelAndClose);
            Button confirm = CreateButton(
                screen.transform,
                "확인",
                "확인",
                UiButtonStyle.Danger,
                ScreenRegionIds.PrimaryBottomRight);
            confirm.onClick.AddListener(ConfirmAndClose);
        }

        private void BuildCredits()
        {
            GameObject screen = CreateScreen(SystemScreenState.Credits);
            CreateText(
                screen.transform,
                "Credits Title",
                "크레딧",
                UiTextStyle.Display,
                ScreenRegionIds.ObjectiveTop);
            TMP_Text credits = CreateText(
                screen.transform,
                "Credits Content",
                "UNDER THE HORIZON\n\n" +
                "기획·개발  MAD CAMP PROJECT MYSTERY\n\n" +
                "사용 글꼴과 외부 리소스의 라이선스는\n" +
                "프로젝트 Licenses 폴더에서 확인할 수 있습니다.",
                UiTextStyle.BodyLarge,
                ScreenRegionIds.ContentCenter);
            credits.alignment = TextAlignmentOptions.Center;
            Button back = CreateButton(
                screen.transform,
                "타이틀로",
                "타이틀로",
                UiButtonStyle.Primary,
                ScreenRegionIds.ToolsBottomLeft);
            back.onClick.AddListener(Close);
        }

        private GameObject CreateScreen(
            SystemScreenState state,
            bool overlay = false)
        {
            GameObject screen = CreatePanel(
                root.transform,
                state.ToString(),
                overlay
                    ? UiSurfaceStyle.Overlay
                    : UiSurfaceStyle.Canvas);
            Stretch(screen.GetComponent<RectTransform>());
            screen.AddComponent<UiPanelTransitionAnimator>();
            screen.SetActive(false);
            screens[state] = screen;
            return screen;
        }

        private void Show(SystemScreenState state)
        {
            EnsureBuilt();
            if (!screens.TryGetValue(state, out GameObject screen))
                return;
            if (transitions != null && transitions.IsTransitioning)
                return;

            GameObject outgoing = activeScreen;
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            owner?.SetSystemScreenOverlayActive(true);
            if (statusHud != null)
                statusHud.SetActive(false);

            void SwapScreens()
            {
                foreach (GameObject candidate in screens.Values)
                    candidate.SetActive(candidate == screen);
                activeScreen = screen;
                ActiveState = state;
            }

            if (outgoing == screen ||
                transitions == null ||
                !transitions.Run(outgoing, screen, SwapScreens))
            {
                SwapScreens();
            }
        }

        private void ConfirmAndClose()
        {
            Action action = confirmAction;
            Close(action);
        }

        private void BeginChapterContinue()
        {
            if (chapterContinuePending ||
                chapterContinueButton == null ||
                !chapterContinueButton.interactable)
                return;

            chapterContinuePending = true;
            chapterContinueButton.interactable = false;
            Action action = confirmAction;
            StartChapterFade(
                chapterCanvasGroup != null ? chapterCanvasGroup.alpha : 1f,
                0f,
                () =>
                {
                    Close(action);
                });
        }

        private void StartChapterFade(
            float from,
            float to,
            Action completed)
        {
            if (chapterCanvasGroup == null)
            {
                completed?.Invoke();
                return;
            }

            if (chapterFadeRoutine != null)
                StopCoroutine(chapterFadeRoutine);
            chapterFadeRoutine = StartCoroutine(
                FadeChapter(from, to, completed));
        }

        private IEnumerator FadeChapter(
            float from,
            float to,
            Action completed)
        {
            float duration = ReducedMotionSettings.Enabled ? .25f : .5f;
            chapterCanvasGroup.alpha = from;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                chapterCanvasGroup.alpha = Mathf.Lerp(
                    from,
                    to,
                    Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            chapterCanvasGroup.alpha = to;
            chapterFadeRoutine = null;
            completed?.Invoke();
        }

        private void StartChapterPresentation()
        {
            if (chapterFadeRoutine != null)
                StopCoroutine(chapterFadeRoutine);
            chapterFadeRoutine = StartCoroutine(PresentChapter());
        }

        private IEnumerator PresentChapter()
        {
            bool reducedMotion = ReducedMotionSettings.Enabled;
            float fadeDuration = reducedMotion ? .25f : .55f;
            float minimumTime = Mathf.Max(
                fadeDuration,
                activeChapterRequest.MinimumDisplayTime);
            chapterCanvasGroup.alpha = 0f;
            chapterContinueButton.interactable = false;
            chapterContinueButton.gameObject.SetActive(false);
            if (chapterRouteLine != null)
            {
                chapterRouteLine.pivot = new Vector2(0f, .5f);
                chapterRouteLine.localScale =
                    reducedMotion ? Vector3.one : new Vector3(0f, 1f, 1f);
            }

            float elapsed = 0f;
            while (elapsed < minimumTime)
            {
                elapsed += Time.unscaledDeltaTime;
                chapterCanvasGroup.alpha = Mathf.Clamp01(
                    elapsed / fadeDuration);
                if (!reducedMotion)
                {
                    if (chapterRouteLine != null)
                    {
                        float routeProgress = Mathf.SmoothStep(
                            0f,
                            1f,
                            Mathf.Clamp01((elapsed - .2f) / 1.15f));
                        chapterRouteLine.localScale =
                            new Vector3(routeProgress, 1f, 1f);
                    }
                    if (chapterHorizon != null)
                    {
                        chapterHorizon.anchoredPosition =
                            new Vector2(
                                Mathf.Sin(elapsed * .8f) * 6f,
                                Mathf.Sin(elapsed * .45f) * 2f);
                    }
                }
                yield return null;
            }

            chapterCanvasGroup.alpha = 1f;
            chapterContinueButton.gameObject.SetActive(true);
            chapterContinueButton.interactable = true;
            chapterFadeRoutine = null;
        }

        private void ConfigureChapterVisuals(
            ChapterTransitionRequest request)
        {
            bool departure = request.TransitionKind ==
                TransitionKind.Departure;
            bool finale = request.TransitionKind ==
                TransitionKind.Finale;
            Color sky = departure
                ? new Color(.035f, .075f, .12f, 1f)
                : finale
                    ? new Color(.075f, .035f, .045f, 1f)
                    : new Color(.02f, .04f, .075f, 1f);
            Color sea = departure
                ? new Color(.025f, .16f, .2f, 1f)
                : finale
                    ? new Color(.08f, .045f, .055f, 1f)
                    : new Color(.018f, .085f, .13f, 1f);
            float keyTint = BackgroundTint(request.BackgroundKey);
            chapterSky.color = Color.Lerp(
                sky,
                new Color(.11f, .15f, .2f, 1f),
                keyTint * .2f);
            chapterSea.color = Color.Lerp(
                sea,
                new Color(.025f, .2f, .22f, 1f),
                keyTint * .16f);
            chapterVesselName.text = departure
                ? "MV ELYSIUM  ·  UNDER THE HORIZON"
                : request.BackgroundKey.Replace('_', ' ').ToUpperInvariant();
        }

        private static float BackgroundTint(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return 0f;
            int total = 0;
            foreach (char value in key)
                total = (total + value) % 97;
            return total / 96f;
        }

        private static Image CreateChapterLayer(
            Transform parent,
            string name,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            GameObject layer = new(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            layer.transform.SetParent(parent, false);
            RectTransform rect = layer.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = layer.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private void CancelAndClose()
        {
            Action action = cancelAction;
            Close(action);
        }

        private static GameObject CreatePanel(
            Transform parent,
            string name,
            UiSurfaceStyle style)
        {
            GameObject panel = new(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            panel.transform.SetParent(parent, false);
            Image image = panel.GetComponent<Image>();
            UiVisualThemeService.ApplySurface(image, style);
            return panel;
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            string value,
            UiTextStyle style,
            string slotId)
        {
            GameObject textObject = new(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            RuntimeUiLayoutRegistry.CopyWorldLayout(rect, slotId);
            TMP_Text text = textObject.GetComponent<TMP_Text>();
            text.text = value;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            UiVisualThemeService.ApplyText(text, style);
            return text;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            UiButtonStyle style,
            string slotId)
        {
            GameObject buttonObject = CreatePanel(
                parent,
                name,
                UiSurfaceStyle.RaisedPanel);
            RectTransform rect =
                buttonObject.GetComponent<RectTransform>();
            RuntimeUiLayoutRegistry.CopyWorldLayout(rect, slotId);
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            button.onClick.AddListener(PlayClickSfx);
            TMP_Text text = CreateStretchedLabel(
                buttonObject.transform,
                label);
            UiVisualThemeService.ApplyButton(button, style);
            UiVisualThemeService.ApplyText(text, UiTextStyle.Choice);
            return button;
        }

        private static RectTransform CreateContainer(
            Transform parent,
            string name,
            string slotId)
        {
            GameObject container = new(name, typeof(RectTransform));
            container.transform.SetParent(parent, false);
            RectTransform rect = container.GetComponent<RectTransform>();
            RuntimeUiLayoutRegistry.CopyWorldLayout(rect, slotId);
            VerticalLayoutGroup layout =
                container.AddComponent<VerticalLayoutGroup>();
            layout.spacing =
                UiVisualThemeService.Resolve(UiSpacingToken.Medium);
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return rect;
        }

        private static Button CreateLayoutButton(
            RectTransform parent,
            string label,
            UiButtonStyle style)
        {
            GameObject buttonObject = CreatePanel(
                parent,
                label,
                UiSurfaceStyle.RaisedPanel);
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            button.onClick.AddListener(PlayClickSfx);
            LayoutElement element =
                buttonObject.AddComponent<LayoutElement>();
            element.preferredHeight = 76f;
            element.minHeight = 64f;
            CreateStretchedLabel(buttonObject.transform, label);
            UiVisualThemeService.ApplyButton(button, style);
            return button;
        }

        private static void PlayClickSfx() =>
            AudioManager.Instance?.PlayButtonClick();

        private static TMP_Text CreateStretchedLabel(
            Transform parent,
            string value)
        {
            GameObject labelObject = new(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            RectTransform rect =
                labelObject.GetComponent<RectTransform>();
            Stretch(rect);
            TMP_Text text = labelObject.GetComponent<TMP_Text>();
            text.text = value;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
