using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Wake.Core;
using Wake.Evidence;

namespace Wake.UI
{
    [DisallowMultipleComponent]
    public class LobbyUIManager : MonoBehaviour
    {
        private const string IngameSceneName = "Ingame Scene";

        public static LobbyUIManager Instance { get; private set; }

        private GameObject startScenePanel;
        private GameObject settingsPopup;
        private GameObject continueButton;
        private SaveSlotSelectionController saveSlotSelection;
        private LobbyRevealSequence revealSequence;

        public bool IsInitialized { get; private set; }

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
                Debug.LogError("LobbyUIManager requires an active Canvas root.");
                return false;
            }
            Transform canvas = canvasObject.transform;
            var missing = new List<string>();
            startScenePanel = FindRequired(canvas, "StartScene", missing);
            settingsPopup = FindRequired(canvas, "Settings Popup", missing);
            continueButton =
                FindRequired(canvas, "StartScene/Continue Btn", missing);
            if (missing.Count > 0)
            {
                Debug.LogError(
                    "LobbyUIManager could not bind required objects: " +
                    string.Join(", ", missing));
                return false;
            }

            saveSlotSelection =
                EnsureComponent<SaveSlotSelectionController>(startScenePanel);
            EnsureComponent<TitleScreenPresentationController>(startScenePanel);
            revealSequence = EnsureComponent<LobbyRevealSequence>(gameObject);
            RectTransform revealGroup =
                saveSlotSelection.GetComponent<RectTransform>();
            revealSequence.Configure(
                startScenePanel.transform.Find("Title Presentation")
                    as RectTransform,
                revealGroup,
                GameObject.Find("Water")?.transform,
                canvas as RectTransform);

            bool buttonsBound =
                BindButton(canvas, "StartScene/Start Game Btn", OpenSaveSlots) &
                BindButton(canvas, "StartScene/Settings Btn", OpenSettings) &
                BindButton(
                    canvas, "StartScene/Continue Btn", OnContinueClicked);
            if (!buttonsBound)
            {
                return false;
            }

            continueButton.SetActive(false);
            // A successful EnsureInitialized() defines "the" live Lobby UI, so it
            // must also hold the singleton reference. Awake() is not the only
            // caller that can reach this point (EnsureInitialized is public and
            // is invoked directly elsewhere, e.g. as a recovery/re-init path),
            // so leaving Instance assignment solely to Awake() lets IsInitialized
            // become true while Instance stays null or stale.
            Instance = this;
            IsInitialized = true;
            return true;
        }

        private static T EnsureComponent<T>(GameObject host)
            where T : Component
        {
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
                Debug.LogError($"LobbyUIManager requires Button at Canvas/{path}.");
                return false;
            }
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
            return true;
        }

        private void OpenSaveSlots()
        {
            revealSequence.Play();
            saveSlotSelection?.Open();
        }

        public void OpenSettings()
        {
            if (settingsPopup == null)
            {
                return;
            }
            settingsPopup.transform.SetAsLastSibling();
            settingsPopup.SetActive(true);
        }

        private void OnContinueClicked() => ContinueGameInSlot(1);

        public void StartNewGameInSlot(int slot)
        {
            GameStateManager.Instance?.SelectSaveSlot(slot);
            GameFlow.Instance?.ResetSession();
            GameStateManager.Instance?.StartNewGame();
            EvidenceInventory.Instance?.Clear();
            SceneManager.LoadScene(IngameSceneName, LoadSceneMode.Single);
            GameFlow.Instance?.BeginGame();
        }

        public void ContinueGameInSlot(int slot)
        {
            GameStateManager.Instance?.SelectSaveSlot(slot);
            SceneManager.LoadScene(IngameSceneName, LoadSceneMode.Single);
            GameFlow.Instance?.ResumeGame();
        }
    }
}
