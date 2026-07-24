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
            mapPanel = canvas.Find("Map").gameObject;
            evidencePanel = canvas.Find("Evidence").gameObject;
            settingsPopup = canvas.Find("Settings Popup").gameObject;

            canvas.Find("StartScene/Start Game Btn").GetComponent<Button>().onClick.AddListener(OnStartGameClicked);
            canvas.Find("StartScene/Settings Btn").GetComponent<Button>().onClick.AddListener(OpenSettings);

            canvas.Find("Ingame/Map Btn").GetComponent<Button>().onClick.AddListener(ShowMap);
            canvas.Find("Ingame/Evidence Btn").GetComponent<Button>().onClick.AddListener(ShowEvidence);
            canvas.Find("Ingame/Settings Btn").GetComponent<Button>().onClick.AddListener(OpenSettings);

            canvas.Find("Map/Back Btn").GetComponent<Button>().onClick.AddListener(ShowIngame);

            ShowStartScene();
        }

        private void OnStartGameClicked()
        {
            ShowIngame();
            GameFlow.Instance.BeginGame();
        }

        public void ShowStartScene()
        {
            SetActivePanel(startScenePanel);
        }

        public void ShowIngame()
        {
            SetActivePanel(ingamePanel);
        }

        public void ShowMap()
        {
            SetActivePanel(mapPanel);
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
        }
    }
}
