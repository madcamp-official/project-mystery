using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.Core;
using Wake.Evidence;

namespace Wake.UI
{
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

        private void Awake()
        {
            Instance = this;
            BindPanels();
        }

        private void BindPanels()
        {
            Transform canvas = GameObject.Find("Canvas").transform;
            startScenePanel = canvas.Find("StartScene").gameObject;
            ingamePanel = canvas.Find("Ingame").gameObject;
            if (ingamePanel.GetComponent<ProductionPuzzleUIController>() == null)
            {
                ingamePanel.AddComponent<ProductionPuzzleUIController>();
            }
            if (ingamePanel.GetComponent<FinalAccusationUIController>() == null)
            {
                ingamePanel.AddComponent<FinalAccusationUIController>();
            }
            if (ingamePanel.GetComponent<MarcusInterrogationUIController>() == null)
            {
                ingamePanel.AddComponent<MarcusInterrogationUIController>();
            }
            mapPanel = canvas.Find("Map").gameObject;
            evidencePanel = canvas.Find("Evidence").gameObject;
            settingsPopup = canvas.Find("Settings Popup").gameObject;
            Transform statusHudTransform = canvas.Find("Status HUD");
            statusHud = statusHudTransform != null ? statusHudTransform.gameObject : null;
            if (statusHud != null &&
                statusHud.GetComponent<ObjectiveMapHUDController>() == null)
            {
                statusHud.AddComponent<ObjectiveMapHUDController>();
            }

            Transform newGameTransform = canvas.Find("StartScene/Start Game Btn");
            newGameTransform.GetComponent<Button>().onClick.AddListener(OnNewGameClicked);
            canvas.Find("StartScene/Settings Btn").GetComponent<Button>().onClick.AddListener(OpenSettings);

            Transform continueTransform = canvas.Find("StartScene/Continue Btn");
            if (continueTransform != null)
            {
                continueButton = continueTransform.gameObject;
                continueButton.GetComponent<Button>().onClick.AddListener(OnContinueClicked);
            }

            // These two labels carry Korean text; the dynamic runtime font asset can't be
            // saved into the scene, so it has to be (re)applied here every session.
            TMP_FontAsset koreanFont = StatusHUDController.RuntimeKoreanFont;
            if (koreanFont != null)
            {
                ApplyFont(newGameTransform, koreanFont);
                if (continueTransform != null)
                {
                    ApplyFont(continueTransform, koreanFont);
                }
            }

            canvas.Find("Ingame/Map Btn").GetComponent<Button>().onClick.AddListener(ShowMap);
            canvas.Find("Ingame/Evidence Btn").GetComponent<Button>().onClick.AddListener(ShowEvidence);
            canvas.Find("Ingame/Settings Btn").GetComponent<Button>().onClick.AddListener(OpenSettings);

            canvas.Find("Map/Back Btn").GetComponent<Button>().onClick.AddListener(ShowIngame);

            ShowStartScene();
        }

        private static void ApplyFont(Transform target, TMP_FontAsset font)
        {
            TMP_Text label = target.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.font = font;
                label.SetAllDirty();
            }
        }

        private void OnNewGameClicked()
        {
            GameStateManager.Instance?.StartNewGame();
            EvidenceInventory.Instance?.Clear();
            ShowIngame();
            GameFlow.Instance.BeginGame();
        }

        private void OnContinueClicked()
        {
            ShowIngame();
            GameFlow.Instance.ResumeGame();
        }

        public void ShowStartScene()
        {
            if (continueButton != null)
            {
                continueButton.SetActive(GameStateManager.HasSaveData);
            }
            SetActivePanel(startScenePanel);
        }

        public void ShowIngame()
        {
            SetActivePanel(ingamePanel);
        }

        public void ShowMap()
        {
            SetActivePanel(mapPanel);
            FindFirstObjectByType<MapController>()?.RefreshMap();
        }

        public void ShowEvidence()
        {
            SetActivePanel(evidencePanel);
            EvidencePanelController.Instance?.Refresh();
        }

        public void OpenSettings()
        {
            settingsPopup.SetActive(true);
        }

        public void CloseSettings()
        {
            settingsPopup.SetActive(false);
        }

        private void SetActivePanel(GameObject panel)
        {
            startScenePanel.SetActive(panel == startScenePanel);
            ingamePanel.SetActive(panel == ingamePanel);
            mapPanel.SetActive(panel == mapPanel);
            evidencePanel.SetActive(panel == evidencePanel);
            if (statusHud != null)
            {
                statusHud.SetActive(panel != startScenePanel);
            }
        }
    }
}
