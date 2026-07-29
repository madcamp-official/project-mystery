using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Wake.Core;
using Wake.Evidence;
using Wake.Exploration;
using Wake.Narrative;
using Wake.Puzzles;

namespace Wake.UI
{
    public enum UiPrimaryPanel
    {
        None,
        Start,
        Ingame,
        Map,
        Evidence
    }

    public interface IRuntimeModalController
    {
        bool IsOpen { get; }
        void Close();
    }

    [DisallowMultipleComponent]
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        private GameObject startScenePanel;
        private GameObject ingamePanel;
        private GameObject mapPanel;
        private GameObject evidencePanel;
        private GameObject settingsPopup;
        private GameObject statusHud;
        private GameObject continueButton;
        private SaveSlotSelectionController saveSlotSelection;
        private SystemScreenFlowController systemScreens;
        private UIScreenTransitionCoordinator screenTransitions;
        private ExplorationNavigationController explorationNavigation;
        private bool hasShownBoot;
        private UiPrimaryPanel mapReturnPanel = UiPrimaryPanel.Ingame;
        private readonly List<IRuntimeModalController> runtimeModals = new();

        public bool IsInitialized { get; private set; }
        public UiPrimaryPanel ActivePanel { get; private set; }
        public bool IsSettingsOpen =>
            settingsPopup != null && settingsPopup.activeSelf;
        public SystemScreenState ActiveSystemScreen =>
            systemScreens != null
                ? systemScreens.ActiveState
                : SystemScreenState.None;
        public int RuntimeModalControllerCount => runtimeModals.Count;
        public bool IsTransitioning =>
            screenTransitions != null && screenTransitions.IsTransitioning;
        public int OpenRuntimeModalCount
        {
            get
            {
                int count = 0;
                foreach (IRuntimeModalController modal in runtimeModals)
                {
                    if (modal != null && modal.IsOpen)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                enabled = false;
                return;
            }

            Instance = this;
            EnsureInitialized();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (!IsInitialized ||
                Keyboard.current == null ||
                ActivePanel == UiPrimaryPanel.Start ||
                IsSettingsOpen ||
                ActiveSystemScreen != SystemScreenState.None)
            {
                return;
            }

            if (Keyboard.current.mKey.wasPressedThisFrame)
            {
                if (ActivePanel == UiPrimaryPanel.Map)
                    CloseMap();
                else
                    ShowMap();
            }
            else if (
                ActivePanel == UiPrimaryPanel.Map &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseMap();
            }
        }

        public bool EnsureInitialized()
        {
            GameObject canvasObject = GameObject.Find("Canvas");
            if (canvasObject == null)
            {
                IsInitialized = false;
                Debug.LogError("UIManager requires an active Canvas root.");
                return false;
            }

            Transform canvas = canvasObject.transform;
            var missing = new List<string>();
            startScenePanel = FindRequired(canvas, "StartScene", missing);
            ingamePanel = FindRequired(canvas, "Ingame", missing);
            mapPanel = FindRequired(canvas, "Map", missing);
            evidencePanel = FindRequired(canvas, "Evidence", missing);
            settingsPopup = FindRequired(canvas, "Settings Popup", missing);
            continueButton =
                FindRequired(canvas, "StartScene/Continue Btn", missing);
            Transform statusHudTransform = canvas.Find("Status HUD");
            statusHud =
                statusHudTransform != null ? statusHudTransform.gameObject : null;
            if (missing.Count > 0)
            {
                IsInitialized = false;
                Debug.LogError(
                    "UIManager could not bind required objects: " +
                    string.Join(", ", missing));
                return false;
            }

            EnsureRuntimeControllers();
            bool buttonsBound =
                BindButton(
                    canvas,
                    "StartScene/Start Game Btn",
                    OpenSaveSlots) &
                BindButton(
                    canvas,
                    "StartScene/Settings Btn",
                    OpenSettings) &
                BindButton(
                    canvas,
                    "StartScene/Continue Btn",
                    OnContinueClicked) &
                BindButton(canvas, "Ingame/Map Btn", ShowMap) &
                BindButton(canvas, "Ingame/Evidence Btn", ShowEvidence) &
                BindButton(canvas, "Ingame/Settings Btn", OpenSettings) &
                BindButton(canvas, "Map/Back Btn", CloseMap);
            if (!buttonsBound)
            {
                IsInitialized = false;
                return false;
            }

            FeatureTypography.ApplyMenuAction(
                canvas.Find("StartScene/Start Game Btn"));
            FeatureTypography.ApplyMenuAction(
                canvas.Find("StartScene/Continue Btn"));
            StyleMapBackButton(canvas);
            SetStartButtonLabel(canvas.Find("StartScene/Start Game Btn"));
            continueButton.SetActive(false);
            SetLegacyExplorationNavigationVisible(canvas, false);

            bool firstInitialization = !IsInitialized;
            IsInitialized = true;
            if (firstInitialization || ActivePanel == UiPrimaryPanel.None)
            {
                ShowStartScene();
            }

            return true;
        }

        private static void StyleMapBackButton(Transform canvas)
        {
            Transform target = canvas?.Find("Map/Back Btn");
            if (target == null)
                return;

            Button button = target.GetComponent<Button>();
            Image image = target.GetComponent<Image>();
            RectTransform rect = target as RectTransform;
            TMP_Text label =
                target.GetComponentInChildren<TMP_Text>(true);
            if (image != null)
            {
                image.sprite = null;
                image.type = Image.Type.Simple;
                image.material = null;
            }
            if (button != null)
            {
                button.transition = Selectable.Transition.ColorTint;
                button.spriteState = default;
                button.targetGraphic = image;
            }
            UiVisualThemeService.ApplyButton(
                button,
                UiButtonStyle.Secondary);
            if (rect != null)
            {
                rect.anchorMin = new Vector2(.855f, .905f);
                rect.anchorMax = new Vector2(.985f, .975f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
            }
            if (label != null)
            {
                label.text = "← 돌아가기";
                UiVisualThemeService.ApplyText(
                    label,
                    UiTextStyle.Technical);
                label.alignment = TextAlignmentOptions.Center;
            }
            target.SetAsLastSibling();
        }

        private void EnsureRuntimeControllers()
        {
            runtimeModals.Clear();
            RegisterModal(EnsureComponent<ExitInspectionUIController>(
                ingamePanel));
            RegisterModal(EnsureComponent<ProductionPuzzleUIController>(
                ingamePanel));
            RegisterModal(EnsureComponent<BloodDirectionPuzzleUIController>(
                ingamePanel));
            RegisterModal(EnsureComponent<CameraBlindSpotUIController>(
                ingamePanel));
            RegisterModal(EnsureComponent<FinalAccusationUIController>(
                ingamePanel));
            RegisterModal(EnsureComponent<MarcusInterrogationUIController>(
                ingamePanel));
            RegisterModal(EnsureComponent<TimelinePuzzleUIController>(
                ingamePanel));
            RegisterModal(EnsureComponent<OrpheusAudioRestorationUIController>(
                ingamePanel));
            RegisterModal(EnsureComponent<ProductionEndingUIController>(
                ingamePanel));
            RegisterModal(EnsureComponent<EvidenceTheoryBoardController>(
                evidencePanel));
            EnsureComponent<ObjectiveMapHUDController>(ingamePanel);
            EnsureComponent<NarrativeLocationHUDController>(ingamePanel);
            EnsureComponent<EvidenceNotebookTabsController>(evidencePanel);
            EnsureComponent<RuntimeUiOverhaulController>(gameObject);
            EnsureComponent<EvidenceAcquisitionNoticeController>(gameObject);
            EnsureComponent<ChapterTransitionPresenter>(gameObject);
            screenTransitions =
                EnsureComponent<UIScreenTransitionCoordinator>(gameObject);
            screenTransitions.Configure(
                GameObject.Find("Canvas").transform as RectTransform);
            EnsureComponent<TitleScreenPresentationController>(startScenePanel);
            saveSlotSelection =
                EnsureComponent<SaveSlotSelectionController>(startScenePanel);
            systemScreens =
                EnsureComponent<SystemScreenFlowController>(gameObject);
            systemScreens.Configure(
                this,
                GameObject.Find("Canvas").transform as RectTransform,
                statusHud);
            explorationNavigation =
                EnsureComponent<ExplorationNavigationController>(gameObject);
            explorationNavigation.Configure(this);
            statusHud?.SetActive(false);
        }

        private void RegisterModal(IRuntimeModalController modal)
        {
            if (modal != null && !runtimeModals.Contains(modal))
            {
                runtimeModals.Add(modal);
            }
        }

        private static T EnsureComponent<T>(GameObject host)
            where T : Component
        {
            if (host == null)
            {
                return null;
            }

            T existing = host.GetComponent<T>();
            return existing != null ? existing : host.AddComponent<T>();
        }

        private static GameObject FindRequired(
            Transform root,
            string path,
            ICollection<string> missing)
        {
            Transform target = root.Find(path);
            if (target == null)
            {
                missing.Add(path);
                return null;
            }
            return target.gameObject;
        }

        private static bool BindButton(
            Transform root,
            string path,
            UnityAction action)
        {
            Button button = root.Find(path)?.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError($"UIManager requires Button at Canvas/{path}.");
                return false;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
            button.onClick.RemoveListener(PlayClickSfx);
            button.onClick.AddListener(PlayClickSfx);
            return true;
        }

        private static void PlayClickSfx() =>
            AudioManager.Instance?.PlayButtonClick();

        private static void SetStartButtonLabel(Transform button)
        {
            TMP_Text label = button?.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = "시작하기";
            }
        }

        private void OpenSaveSlots()
        {
            systemScreens?.SetPassiveState(SystemScreenState.SaveSlots);
            saveSlotSelection?.Open();
        }

        private static void SetLegacyExplorationNavigationVisible(
            Transform canvas,
            bool visible)
        {
            string[] paths =
            {
                "Ingame/Map Btn",
                "Ingame/Evidence Btn",
                "Ingame/Settings Btn"
            };
            foreach (string path in paths)
            {
                Transform target = canvas?.Find(path);
                if (target != null)
                    target.gameObject.SetActive(visible);
            }
        }

        public void StartNewGameInSlot(int slot)
        {
            GameStateManager.Instance?.SelectSaveSlot(slot);
            GameFlow.Instance?.ResetSession();
            GameStateManager.Instance?.StartNewGame();
            EvidenceInventory.Instance?.Clear();
            systemScreens?.SetPassiveState(SystemScreenState.None);
            ShowIngame();
            GameFlow.Instance?.BeginGame();
        }

        public void ContinueGameInSlot(int slot)
        {
            GameStateManager.Instance?.SelectSaveSlot(slot);
            systemScreens?.SetPassiveState(SystemScreenState.None);
            ShowIngame();
            GameFlow.Instance?.ResumeGame();
        }

        private void OnNewGameClicked() => StartNewGameInSlot(1);
        private void OnContinueClicked() => ContinueGameInSlot(1);

        public void ShowStartScene()
        {
            if (!IsInitialized)
            {
                return;
            }

            AudioManager.Instance?.PlayTitleTheme();
            DialogueController.Instance?.CancelActiveDialogue();
            GameFlow.Instance?.ResetSession();
            EvidenceInventory.Instance?.Clear();
            continueButton?.SetActive(false);
            SetActivePanel(startScenePanel, UiPrimaryPanel.Start);
            systemScreens?.SetPassiveState(SystemScreenState.Title);
            if (!hasShownBoot && systemScreens != null)
            {
                hasShownBoot = true;
                StartCoroutine(systemScreens.ShowBootOnce());
            }
        }

        public void ShowIngame()
        {
            SetActivePanel(ingamePanel, UiPrimaryPanel.Ingame);
        }

        public bool ResumePendingInteraction(
            ProductionDialogueCheckpoint checkpoint)
        {
            if (checkpoint == null ||
                string.IsNullOrWhiteSpace(checkpoint.pendingInteractionId))
            {
                return false;
            }

            string interactionId = checkpoint.pendingInteractionId.Trim();
            if (interactionId == ExitInspectionCatalog.SessionId)
                return FindFirstObjectByType<ExitInspectionUIController>()?.Open() == true;
            if (ProductionPuzzleCatalog.TryGetByScene(
                    checkpoint.activeSceneId,
                    out ProductionPuzzleDefinition puzzle) &&
                interactionId == puzzle.Id)
            {
                return FindFirstObjectByType<ProductionPuzzleUIController>()
                    ?.Open(puzzle.Id) == true;
            }
            if (interactionId == CameraBlindSpotSession.SessionId)
                return FindFirstObjectByType<CameraBlindSpotUIController>()?.Open() == true;
            if (interactionId == MarcusInterrogationCatalog.SessionId)
                return FindFirstObjectByType<MarcusInterrogationUIController>()?.Open() == true;
            if (interactionId == TimelinePuzzleCatalog.PuzzleId)
                return FindFirstObjectByType<TimelinePuzzleUIController>()?.Open() == true;
            if (interactionId == OrpheusRecordCatalog.PuzzleId)
                return FindFirstObjectByType<OrpheusAudioRestorationUIController>()?.Open() == true;
            if (interactionId == FinalAccusationSession.SessionId)
            {
                FindFirstObjectByType<FinalAccusationUIController>()?.Open();
                return true;
            }

            return false;
        }

        public void ShowMap()
        {
            if (IsTransitioning)
                return;

            if (ActivePanel != UiPrimaryPanel.Map)
            {
                mapReturnPanel =
                    ActivePanel == UiPrimaryPanel.Evidence
                        ? UiPrimaryPanel.Evidence
                        : UiPrimaryPanel.Ingame;
            }
            SetActivePanel(
                mapPanel,
                UiPrimaryPanel.Map,
                () =>
                {
                    FindFirstObjectByType<MapController>()?.RefreshMap();
                    StyleMapBackButton(mapPanel?.transform.parent);
                });
        }

        public void CloseMap()
        {
            if (mapReturnPanel == UiPrimaryPanel.Evidence)
                ShowEvidence();
            else
                ShowIngame();
        }

        public void ShowEvidence()
        {
            SetActivePanel(
                evidencePanel,
                UiPrimaryPanel.Evidence,
                () => EvidencePanelController.Instance?.Refresh());
        }

        public void ShowEvidence(string evidenceId)
        {
            SetActivePanel(
                evidencePanel,
                UiPrimaryPanel.Evidence,
                () => EvidencePanelController.Instance?.Refresh(evidenceId));
        }

        public void OpenSettings()
        {
            if (!IsInitialized || settingsPopup == null || IsSettingsOpen)
            {
                return;
            }

            CloseRuntimeModals();
            systemScreens?.Close();
            systemScreens?.OnSettingsOpened();
            SetPrimaryInteraction(false);
            if (statusHud != null)
            {
                statusHud.SetActive(false);
            }
            settingsPopup.transform.SetAsLastSibling();
            settingsPopup.SetActive(true);
            FindFirstObjectByType<SettingsController>()
                ?.RefreshFromAudioManager();
            Transform credit =
                settingsPopup.transform.Find("Settings/Credit");
            if (credit != null)
                credit.gameObject.SetActive(false);
            Transform exit =
                settingsPopup.transform.Find("Exit Btn");
            if (exit != null)
                exit.gameObject.SetActive(false);
            Canvas.ForceUpdateCanvases();
            SettingsController.FitPopupInsideCanvas(
                settingsPopup.transform as RectTransform,
                settingsPopup.transform.parent as RectTransform);
        }

        public void CloseSettings()
        {
            bool wasOpen =
                settingsPopup != null && settingsPopup.activeSelf;
            if (settingsPopup != null)
            {
                settingsPopup.SetActive(false);
            }
            if (wasOpen)
            {
                systemScreens?.OnSettingsClosed();
            }
            statusHud?.SetActive(false);
            explorationNavigation?.SetInteractionEnabled(true);
            SetPrimaryInteraction(true);
        }

        public void OpenPause()
        {
            if (!IsInitialized || ActivePanel == UiPrimaryPanel.Start)
                return;

            CloseRuntimeModals();
            systemScreens?.OpenPause();
        }

        public void ShowCredits()
        {
            if (!IsInitialized)
                return;

            CloseSettings();
            systemScreens?.ShowCredits();
        }

        public void ShowTutorial(
            string title,
            string body,
            UnityAction completed = null)
        {
            if (!IsInitialized)
                return;

            systemScreens?.ShowTutorial(
                title,
                body,
                completed == null ? null : completed.Invoke);
        }

        public void ShowLoading(string context, string title)
        {
            if (!IsInitialized)
                return;

            systemScreens?.ShowLoading(context, title);
        }

        public void ShowChapterTransition(
            string context,
            string title,
            string summary,
            UnityAction completed = null)
        {
            if (!IsInitialized)
                return;

            systemScreens?.ShowChapterTransition(
                context,
                title,
                summary,
                completed == null ? null : completed.Invoke);
        }

        public void RequestQuit()
        {
            systemScreens?.RequestConfirmation(
                "게임 종료",
                "게임을 종료하시겠습니까?",
                TitleScreenPresentationController.QuitGame);
        }

        public void RequestReturnToTitle()
        {
            systemScreens?.RequestConfirmation(
                "타이틀로 이동",
                "현재 화면을 닫고 타이틀로 이동하시겠습니까?",
                ShowStartScene);
        }

        public void RequestConfirmation(
            string title,
            string body,
            UnityAction confirmed,
            UnityAction cancelled = null)
        {
            if (!IsInitialized)
                return;

            systemScreens?.RequestConfirmation(
                title,
                body,
                confirmed == null ? null : confirmed.Invoke,
                cancelled == null ? null : cancelled.Invoke);
        }

        public void SetSystemScreenState(SystemScreenState state)
        {
            systemScreens?.SetPassiveState(state);
        }

        private void SetActivePanel(
            GameObject panel,
            UiPrimaryPanel panelKind,
            Action activated = null)
        {
            if (!IsInitialized || panel == null)
            {
                return;
            }
            if (screenTransitions != null &&
                screenTransitions.IsTransitioning)
            {
                return;
            }

            systemScreens?.Close();
            CloseRuntimeModals();
            CloseSettings();
            GameObject outgoing = ResolvePanel(ActivePanel);
            bool hasDedicatedTransition =
                ActivePanel != UiPrimaryPanel.None &&
                outgoing != panel &&
                (panel.activeSelf || IsTravelFadeRunning());

            void SwapPanels()
            {
                startScenePanel.SetActive(panel == startScenePanel);
                ingamePanel.SetActive(panel == ingamePanel);
                mapPanel.SetActive(panel == mapPanel);
                evidencePanel.SetActive(panel == evidencePanel);
                ActivePanel = panelKind;
                LocationLoader.Instance?.SetPresentationVisible(
                    panelKind == UiPrimaryPanel.Ingame);
                statusHud?.SetActive(false);
                explorationNavigation?.Refresh();
                SetPrimaryInteraction(true);
                activated?.Invoke();
            }

            if (outgoing == panel || hasDedicatedTransition)
            {
                SwapPanels();
                return;
            }

            SetPrimaryInteraction(false);
            if (screenTransitions == null ||
                !screenTransitions.Run(
                    outgoing,
                    panel,
                    SwapPanels,
                    () => SetPrimaryInteraction(true)))
            {
                SwapPanels();
            }
        }

        private GameObject ResolvePanel(UiPrimaryPanel panel) =>
            panel switch
            {
                UiPrimaryPanel.Start => startScenePanel,
                UiPrimaryPanel.Ingame => ingamePanel,
                UiPrimaryPanel.Map => mapPanel,
                UiPrimaryPanel.Evidence => evidencePanel,
                _ => null
            };

        private bool IsTravelFadeRunning() =>
            startScenePanel?.transform.parent
                ?.GetComponent<ScreenFadeTransition>()?.IsRunning == true;

        internal void SetSystemScreenOverlayActive(bool active)
        {
            SetPrimaryInteraction(!active);
            statusHud?.SetActive(false);
            explorationNavigation?.SetInteractionEnabled(!active);
        }

        internal void SetExplorationNavigationSuppressed(bool suppressed)
        {
            explorationNavigation?.SetPresentationSuppressed(suppressed);
        }

        private void CloseRuntimeModals()
        {
            foreach (IRuntimeModalController modal in runtimeModals)
            {
                if (modal != null && modal.IsOpen)
                {
                    modal.Close();
                }
            }
        }

        private void SetPrimaryInteraction(bool enabled)
        {
            GameObject primary = ActivePanel switch
            {
                UiPrimaryPanel.Start => startScenePanel,
                UiPrimaryPanel.Ingame => ingamePanel,
                UiPrimaryPanel.Map => mapPanel,
                UiPrimaryPanel.Evidence => evidencePanel,
                _ => null
            };
            SetInputState(primary, enabled);
            SetInputState(statusHud, false);
            explorationNavigation?.SetInteractionEnabled(enabled);
        }

        private static void SetInputState(
            GameObject target,
            bool enabled)
        {
            if (target == null)
            {
                return;
            }

            CanvasGroup group = EnsureComponent<CanvasGroup>(target);
            group.interactable = enabled;
            group.blocksRaycasts = enabled;
        }
    }
}
