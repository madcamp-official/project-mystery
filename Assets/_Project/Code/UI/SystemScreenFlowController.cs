using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.Core;

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
        private Action confirmAction;
        private Action cancelAction;
        private SystemScreenState returnState;
        private TMP_Text loadingContext;
        private TMP_Text loadingTitle;
        private TMP_Text chapterContext;
        private TMP_Text chapterTitle;
        private TMP_Text chapterSummary;
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
            EnsureBuilt();
        }

        public IEnumerator ShowBootOnce()
        {
            Show(SystemScreenState.Boot);
            yield return null;
            Close();
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
            string context,
            string title,
            string summary,
            Action continueAction)
        {
            EnsureBuilt();
            chapterContext.text = context ?? string.Empty;
            chapterTitle.text = title ?? string.Empty;
            chapterSummary.text = summary ?? string.Empty;
            confirmAction = continueAction;
            Show(SystemScreenState.ChapterTransition);
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
            if (!IsOverlayOpen)
                return;

            if (activeScreen != null)
            {
                activeScreen.SetActive(false);
            }
            activeScreen = null;
            if (root != null)
            {
                root.SetActive(false);
            }
            owner?.SetSystemScreenOverlayActive(false);
            ActiveState = returnState == SystemScreenState.None
                ? ResolvePassiveState()
                : returnState;
            returnState = SystemScreenState.None;
            confirmAction = null;
            cancelAction = null;
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
            Button next = CreateButton(
                screen.transform,
                "계속",
                "계속",
                UiButtonStyle.Primary,
                ScreenRegionIds.PrimaryBottomRight);
            next.onClick.AddListener(ConfirmAndClose);
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
            screen.SetActive(false);
            screens[state] = screen;
            return screen;
        }

        private void Show(SystemScreenState state)
        {
            EnsureBuilt();
            if (!screens.TryGetValue(state, out GameObject screen))
                return;

            foreach (GameObject candidate in screens.Values)
            {
                candidate.SetActive(candidate == screen);
            }
            activeScreen = screen;
            ActiveState = state;
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            owner?.SetSystemScreenOverlayActive(true);
            if (statusHud != null)
            {
                statusHud.SetActive(false);
            }
        }

        private void ConfirmAndClose()
        {
            Action action = confirmAction;
            Close();
            action?.Invoke();
        }

        private void CancelAndClose()
        {
            Action action = cancelAction;
            Close();
            action?.Invoke();
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
            LayoutElement element =
                buttonObject.AddComponent<LayoutElement>();
            element.preferredHeight = 76f;
            element.minHeight = 64f;
            CreateStretchedLabel(buttonObject.transform, label);
            UiVisualThemeService.ApplyButton(button, style);
            return button;
        }

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
