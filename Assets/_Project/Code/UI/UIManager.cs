using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Wake.Core;
using Wake.Evidence;
using Wake.Narrative;

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
        private readonly List<IRuntimeModalController> runtimeModals = new();

        public bool IsInitialized { get; private set; }
        public UiPrimaryPanel ActivePanel { get; private set; }
        public bool IsSettingsOpen =>
            settingsPopup != null && settingsPopup.activeSelf;
        public int RuntimeModalControllerCount => runtimeModals.Count;
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
                BindButton(canvas, "Map/Back Btn", ShowIngame);
            if (!buttonsBound)
            {
                IsInitialized = false;
                return false;
            }

            FeatureTypography.ApplyMenuAction(
                canvas.Find("StartScene/Start Game Btn"));
            FeatureTypography.ApplyMenuAction(
                canvas.Find("StartScene/Continue Btn"));
            SetStartButtonLabel(canvas.Find("StartScene/Start Game Btn"));
            continueButton.SetActive(false);

            bool firstInitialization = !IsInitialized;
            IsInitialized = true;
            if (firstInitialization || ActivePanel == UiPrimaryPanel.None)
            {
                ShowStartScene();
            }

            return true;
        }

        private void EnsureRuntimeControllers()
        {
            runtimeModals.Clear();
            RegisterModal(EnsureComponent<ExitInspectionUIController>(
                ingamePanel));
            RegisterModal(EnsureComponent<ProductionPuzzleUIController>(
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
            EnsureComponent<NarrativeLocationHUDController>(ingamePanel);
            EnsureComponent<EvidenceNotebookTabsController>(evidencePanel);
            EnsureComponent<RuntimeUiOverhaulController>(gameObject);
            EnsureComponent<EvidenceAcquisitionNoticeController>(gameObject);
            saveSlotSelection =
                EnsureComponent<SaveSlotSelectionController>(startScenePanel);
            if (statusHud != null)
            {
                EnsureComponent<ObjectiveMapHUDController>(statusHud);
            }
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
            return true;
        }

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
            saveSlotSelection?.Open();
        }

        public void StartNewGameInSlot(int slot)
        {
            GameStateManager.Instance?.SelectSaveSlot(slot);
            GameFlow.Instance?.ResetSession();
            GameStateManager.Instance?.StartNewGame();
            EvidenceInventory.Instance?.Clear();
            ShowIngame();
            GameFlow.Instance?.BeginGame();
        }

        public void ContinueGameInSlot(int slot)
        {
            GameStateManager.Instance?.SelectSaveSlot(slot);
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

            DialogueController.Instance?.CancelActiveDialogue();
            GameFlow.Instance?.ResetSession();
            EvidenceInventory.Instance?.Clear();
            continueButton?.SetActive(false);
            SetActivePanel(startScenePanel, UiPrimaryPanel.Start);
        }

        public void ShowIngame()
        {
            SetActivePanel(ingamePanel, UiPrimaryPanel.Ingame);
        }

        public void ShowMap()
        {
            SetActivePanel(mapPanel, UiPrimaryPanel.Map);
            FindFirstObjectByType<MapController>()?.RefreshMap();
        }

        public void ShowEvidence()
        {
            SetActivePanel(evidencePanel, UiPrimaryPanel.Evidence);
            EvidencePanelController.Instance?.Refresh();
        }

        public void ShowEvidence(string evidenceId)
        {
            SetActivePanel(evidencePanel, UiPrimaryPanel.Evidence);
            EvidencePanelController.Instance?.Refresh(evidenceId);
        }

        public void OpenSettings()
        {
            if (!IsInitialized || settingsPopup == null || IsSettingsOpen)
            {
                return;
            }

            CloseRuntimeModals();
            SetPrimaryInteraction(false);
            settingsPopup.transform.SetAsLastSibling();
            settingsPopup.SetActive(true);
        }

        public void CloseSettings()
        {
            if (settingsPopup != null)
            {
                settingsPopup.SetActive(false);
            }
            SetPrimaryInteraction(true);
        }

        private void SetActivePanel(
            GameObject panel,
            UiPrimaryPanel panelKind)
        {
            if (!IsInitialized || panel == null)
            {
                return;
            }

            CloseRuntimeModals();
            CloseSettings();
            startScenePanel.SetActive(panel == startScenePanel);
            ingamePanel.SetActive(panel == ingamePanel);
            mapPanel.SetActive(panel == mapPanel);
            evidencePanel.SetActive(panel == evidencePanel);
            ActivePanel = panelKind;
            if (statusHud != null)
            {
                statusHud.SetActive(panel != startScenePanel);
            }
            SetPrimaryInteraction(true);
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
            SetInputState(statusHud, enabled);
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
