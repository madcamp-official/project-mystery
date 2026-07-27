using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Wake.Core;
using Wake.Evidence;
using Wake.Exploration;
using Wake.Narrative;

namespace Wake.UI
{
    public enum IngamePrimaryPanel
    {
        None,
        Ingame,
        Map,
        Evidence
    }

    [DisallowMultipleComponent]
    public class IngameUIManager : MonoBehaviour, IIngameUiHost
    {
        private const string LobbySceneName = "Lobby Scene";

        public static IngameUIManager Instance { get; private set; }

        private GameObject ingamePanel;
        private GameObject mapPanel;
        private GameObject evidencePanel;
        private GameObject settingsPopup;
        private GameObject statusHud;
        private readonly List<IRuntimeModalController> runtimeModals = new();

        public bool IsInitialized { get; private set; }
        public IngamePrimaryPanel ActivePanel { get; private set; }
        public bool IsShowingIngamePanel => ActivePanel == IngamePrimaryPanel.Ingame;
        public bool IsSettingsOpen =>
            settingsPopup != null && settingsPopup.activeSelf;
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
            IngameUi.Register(this);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                IngameUi.Register(null);
            }
        }

        public bool EnsureInitialized()
        {
            GameObject canvasObject = GameObject.Find("Canvas");
            if (canvasObject == null)
            {
                Debug.LogError("IngameUIManager requires an active Canvas root.");
                return false;
            }
            Transform canvas = canvasObject.transform;
            var missing = new List<string>();
            ingamePanel = FindRequired(canvas, "Ingame", missing);
            mapPanel = FindRequired(canvas, "Map", missing);
            evidencePanel = FindRequired(canvas, "Evidence", missing);
            settingsPopup = FindRequired(canvas, "Settings Popup", missing);
            Transform statusHudTransform = canvas.Find("Status HUD");
            statusHud =
                statusHudTransform != null ? statusHudTransform.gameObject : null;
            if (missing.Count > 0)
            {
                Debug.LogError(
                    "IngameUIManager could not bind required objects: " +
                    string.Join(", ", missing));
                return false;
            }

            EnsureRuntimeControllers();
            bool buttonsBound =
                BindButton(canvas, "Ingame/Map Btn", ShowMap) &
                BindButton(canvas, "Ingame/Evidence Btn", ShowEvidence) &
                BindButton(canvas, "Ingame/Settings Btn", OpenSettings) &
                BindButton(canvas, "Map/Back Btn", ShowIngame);
            if (!buttonsBound)
            {
                return false;
            }

            IsInitialized = true;
            ShowIngame();
            return true;
        }

        private void EnsureRuntimeControllers()
        {
            runtimeModals.Clear();
            RegisterModal(EnsureComponent<ExitInspectionUIController>(ingamePanel));
            RegisterModal(EnsureComponent<ProductionPuzzleUIController>(ingamePanel));
            RegisterModal(EnsureComponent<FinalAccusationUIController>(ingamePanel));
            RegisterModal(EnsureComponent<MarcusInterrogationUIController>(ingamePanel));
            RegisterModal(EnsureComponent<TimelinePuzzleUIController>(ingamePanel));
            RegisterModal(EnsureComponent<OrpheusAudioRestorationUIController>(ingamePanel));
            RegisterModal(EnsureComponent<ProductionEndingUIController>(ingamePanel));
            RegisterModal(EnsureComponent<EvidenceTheoryBoardController>(evidencePanel));
            EnsureComponent<NarrativeLocationHUDController>(ingamePanel);
            EnsureComponent<EvidenceNotebookTabsController>(evidencePanel);
            EnsureComponent<RuntimeUiOverhaulController>(gameObject);
            EnsureComponent<EvidenceAcquisitionNoticeController>(gameObject);
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
            Transform root, string path, ICollection<string> missing)
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
            Transform root, string path, UnityAction action)
        {
            Button button = root.Find(path)?.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError(
                    $"IngameUIManager requires Button at Canvas/{path}.");
                return false;
            }
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
            return true;
        }

        public void ShowIngame() =>
            SetActivePanel(ingamePanel, IngamePrimaryPanel.Ingame);

        public void ShowMap()
        {
            SetActivePanel(mapPanel, IngamePrimaryPanel.Map);
            FindFirstObjectByType<MapController>()?.RefreshMap();
        }

        public void ShowEvidence()
        {
            SetActivePanel(evidencePanel, IngamePrimaryPanel.Evidence);
            EvidencePanelController.Instance?.Refresh();
        }

        public void ShowEvidence(string evidenceId)
        {
            SetActivePanel(evidencePanel, IngamePrimaryPanel.Evidence);
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

        public void ReturnToLobby()
        {
            DialogueController.Instance?.CancelActiveDialogue();
            GameFlow.Instance?.ResetSession();
            EvidenceInventory.Instance?.Clear();
            SceneManager.LoadScene(LobbySceneName, LoadSceneMode.Single);
        }

        private void SetActivePanel(GameObject panel, IngamePrimaryPanel kind)
        {
            if (!IsInitialized || panel == null)
            {
                return;
            }
            CloseRuntimeModals();
            CloseSettings();
            ingamePanel.SetActive(panel == ingamePanel);
            mapPanel.SetActive(panel == mapPanel);
            evidencePanel.SetActive(panel == evidencePanel);
            ActivePanel = kind;
            LocationLoader.Instance?.SetPresentationVisible(true);
            if (statusHud != null)
            {
                statusHud.SetActive(true);
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
                IngamePrimaryPanel.Ingame => ingamePanel,
                IngamePrimaryPanel.Map => mapPanel,
                IngamePrimaryPanel.Evidence => evidencePanel,
                _ => null
            };
            SetInputState(primary, enabled);
            SetInputState(statusHud, enabled);
        }

        private static void SetInputState(GameObject target, bool enabled)
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
