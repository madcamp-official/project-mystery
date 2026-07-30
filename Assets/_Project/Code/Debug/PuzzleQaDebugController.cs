#if UNITY_EDITOR
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Wake.Core;
using Wake.Evidence;
using Wake.Narrative;
using Wake.Puzzles;
using Wake.UI;

namespace Wake.QA
{
    /// Editor Play Mode only QA tool: additively loads the real game scene,
    /// then lets QA open any of the 8 production puzzles directly. Resets
    /// only the one puzzle being opened on whatever save slot is already
    /// active - never touches StartNewGame/SelectSaveSlot (see
    /// GameStateManager.DebugResetPuzzle).
    public sealed class PuzzleQaDebugController : MonoBehaviour
    {
        private const string GameSceneName = "UI Basic Scene";

        private GameObject pickerRoot;
        private TMP_Text statusText;
        private IRuntimeModalController openController;

        private void Start()
        {
            StartCoroutine(Bootstrap());
        }

        private IEnumerator Bootstrap()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(
                GameSceneName, LoadSceneMode.Additive);
            while (load != null && !load.isDone)
            {
                yield return null;
            }

            while (GameStateManager.Instance == null ||
                   UIManager.Instance == null ||
                   !UIManager.Instance.IsInitialized)
            {
                yield return null;
            }

            // UIManager's own boot flow (ShowStartScene, run from inside
            // EnsureInitialized) kicks off an async panel transition that
            // can still be running the moment IsInitialized flips true.
            // SetActivePanel silently no-ops while a transition is in
            // flight, so a single ShowIngame() call here can race it and
            // get swallowed - keep retrying until it actually sticks.
            while (UIManager.Instance.ActivePanel != UiPrimaryPanel.Ingame)
            {
                if (!UIManager.Instance.IsTransitioning)
                {
                    UIManager.Instance.ShowIngame();
                }
                yield return null;
            }

            BuildPicker();
        }

        private void BuildPicker()
        {
            var canvasObject = new GameObject("Puzzle QA Picker");
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 1000;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            pickerRoot = new GameObject("Root");
            pickerRoot.transform.SetParent(canvasObject.transform, false);
            var rootRect = pickerRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 0.5f);
            rootRect.anchorMax = new Vector2(0f, 0.5f);
            rootRect.pivot = new Vector2(0f, 0.5f);
            rootRect.anchoredPosition = new Vector2(16f, 0f);
            rootRect.sizeDelta = new Vector2(320f, 460f);
            pickerRoot.AddComponent<Image>().color =
                new Color(0f, 0f, 0f, 0.75f);
            var layout = pickerRoot.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 6f;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = false;
            layout.childControlWidth = true;

            AddLabel(
                pickerRoot.transform,
                "퍼즐 QA 선택 — 현재 활성 세이브 슬롯의 완료 상태를 " +
                "직접 초기화합니다.");

            foreach (ProductionSceneCompletionRequirement requirement in
                     ProductionSceneCompletionCatalog.All)
            {
                AddPuzzleButton(requirement);
            }

            statusText = AddLabel(pickerRoot.transform, string.Empty);
        }

        private TMP_Text AddLabel(Transform parent, string text)
        {
            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(parent, false);
            var label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = 14f;
            label.color = Color.white;
            label.enableWordWrapping = true;
            var rect = labelObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(296f, 40f);
            return label;
        }

        private void AddPuzzleButton(
            ProductionSceneCompletionRequirement requirement)
        {
            var buttonObject = new GameObject($"Btn_{requirement.InteractionId}");
            buttonObject.transform.SetParent(pickerRoot.transform, false);
            buttonObject.AddComponent<RectTransform>().sizeDelta =
                new Vector2(296f, 32f);
            buttonObject.AddComponent<Image>().color =
                new Color(1f, 1f, 1f, 0.12f);
            Button button = buttonObject.AddComponent<Button>();

            TMP_Text label = AddLabel(
                buttonObject.transform,
                $"{requirement.SceneId} · {requirement.InteractionId}");
            label.alignment = TextAlignmentOptions.Center;
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.sizeDelta = Vector2.zero;

            button.onClick.AddListener(() => OpenPuzzle(requirement));
        }

        private void OpenPuzzle(ProductionSceneCompletionRequirement requirement)
        {
            GameStateManager state = GameStateManager.Instance;
            if (state == null)
            {
                return;
            }

            state.DebugResetPuzzle(
                requirement.SceneId, requirement.InteractionId);
            InjectRequiredEvidence(requirement.InteractionId);

            IRuntimeModalController controller =
                OpenController(requirement.InteractionId);
            if (controller == null)
            {
                statusText.text =
                    $"열기 실패: {requirement.InteractionId} " +
                    "(컨트롤러를 찾을 수 없거나 Open()이 실패했습니다.)";
                return;
            }

            openController = controller;
            pickerRoot.SetActive(false);
            StartCoroutine(WaitForClose());
        }

        private IEnumerator WaitForClose()
        {
            while (openController != null && openController.IsOpen)
            {
                yield return null;
            }

            openController = null;
            pickerRoot.SetActive(true);
        }

        private static void InjectRequiredEvidence(string interactionId)
        {
            if (EvidenceInventory.Instance == null ||
                !ProductionPuzzleCatalog.TryGet(
                    interactionId,
                    out ProductionPuzzleDefinition definition))
            {
                return;
            }

            foreach (string evidenceId in definition.RequiredEvidenceIds)
            {
                EvidenceInventory.Instance.TryAddById(evidenceId);
            }
        }

        private static IRuntimeModalController OpenController(
            string interactionId)
        {
            if (interactionId ==
                ProductionSceneCompletionCatalog.ExitInspectionInteraction)
            {
                var controller = FindFirstObjectByType<ExitInspectionUIController>();
                return controller != null && controller.Open()
                    ? controller : null;
            }

            if (interactionId ==
                ProductionSceneCompletionCatalog.BloodPatternInteraction)
            {
                var controller =
                    FindFirstObjectByType<BloodDirectionPuzzleUIController>();
                return controller != null && controller.Open()
                    ? controller : null;
            }

            if (interactionId ==
                ProductionSceneCompletionCatalog.CameraBlindSpotInteraction)
            {
                var controller = FindFirstObjectByType<CameraBlindSpotUIController>();
                return controller != null && controller.Open()
                    ? controller : null;
            }

            if (interactionId ==
                ProductionSceneCompletionCatalog.MarcusInterrogationInteraction)
            {
                var controller =
                    FindFirstObjectByType<MarcusInterrogationUIController>();
                return controller != null && controller.Open()
                    ? controller : null;
            }

            if (interactionId ==
                ProductionSceneCompletionCatalog.CargoRailInteraction)
            {
                var controller = FindFirstObjectByType<ProductionPuzzleUIController>();
                return controller != null &&
                       controller.Open(
                           ProductionSceneCompletionCatalog.CargoRailInteraction)
                    ? controller : null;
            }

            if (interactionId ==
                ProductionSceneCompletionCatalog.TimelineInteraction)
            {
                var controller = FindFirstObjectByType<TimelinePuzzleUIController>();
                return controller != null && controller.Open()
                    ? controller : null;
            }

            if (interactionId ==
                ProductionSceneCompletionCatalog.OrpheusInteraction)
            {
                var controller =
                    FindFirstObjectByType<OrpheusAudioRestorationUIController>();
                return controller != null && controller.Open()
                    ? controller : null;
            }

            if (interactionId ==
                ProductionSceneCompletionCatalog.FinalAccusationInteraction)
            {
                var controller = FindFirstObjectByType<FinalAccusationUIController>();
                if (controller == null)
                {
                    return null;
                }

                controller.Open();
                return controller;
            }

            return null;
        }
    }
}
#endif
