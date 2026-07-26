using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using Wake.Core;
using Wake.Exploration;
using Wake.Narrative;

namespace Wake.UI
{
    public class MapController : MonoBehaviour
    {
        [SerializeField] private LocationGraph locationGraph;

        public SceneTravelResult LastTravelResult { get; private set; }
        public ProductionMapViewModel CurrentViewModel { get; private set; }

        private Transform roomsContainer;
        private Button buttonTemplate;
        private RectTransform dynamicContent;
        private TMP_Text unresolvedLabel;

        private void Start()
        {
            if (locationGraph == null)
            {
                Debug.LogWarning("MapController has no LocationGraph assigned.");
                return;
            }

            Transform canvas = GameObject.Find("Canvas").transform;
            roomsContainer = canvas.Find("Map/Rooms");
            Button[] buttons = roomsContainer.GetComponentsInChildren<Button>(true);
            buttonTemplate = buttons.FirstOrDefault();
            if (buttonTemplate == null)
            {
                Debug.LogError("MapController requires one scene button as a style template.");
                return;
            }

            foreach (Button button in buttons)
            {
                button.gameObject.SetActive(false);
            }

            CreateScrollContent();
            CreateUnresolvedLabel();
            RefreshMap();
        }

        public void RefreshMap()
        {
            if (dynamicContent == null || locationGraph == null)
            {
                return;
            }

            for (int index = dynamicContent.childCount - 1; index >= 0; index--)
            {
                Destroy(dynamicContent.GetChild(index).gameObject);
            }

            GameStateManager state = GameStateManager.Instance;
            CurrentViewModel = ProductionMapViewModel.Create(
                locationGraph,
                state?.CompletedProductionSceneIds,
                state != null ? state.PublicAnxiety : 0,
                state?.FinalEndingId);
            ProductionMapLayout layout = ProductionMapLayoutCalculator.Calculate(
                CurrentViewModel.Entries.Count +
                CurrentViewModel.DialogueOnlyEntries.Count,
                ((RectTransform)roomsContainer).rect.width,
                Screen.safeArea);
            GridLayoutGroup grid = dynamicContent.GetComponent<GridLayoutGroup>();
            grid.constraintCount = layout.Columns;
            grid.cellSize = layout.CellSize;
            dynamicContent.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                layout.ContentHeight);

            foreach (ProductionMapEntry entry in CurrentViewModel.Entries)
            {
                Button button = Instantiate(buttonTemplate, dynamicContent);
                button.gameObject.SetActive(true);
                button.interactable = entry.Status != ProductionMapEntryStatus.Locked;
                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = $"{entry.Header}\n{entry.StatusLabel}";
                    label.font = StatusHUDController.RuntimeKoreanFont;
                    label.enableAutoSizing = true;
                    label.fontSizeMin = 13f;
                    label.fontSizeMax = 20f;
                }

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SelectEntry(entry));
            }

            foreach (DialogueOnlyMapEntry entry in
                     CurrentViewModel.DialogueOnlyEntries)
            {
                Button button = Instantiate(buttonTemplate, dynamicContent);
                button.gameObject.SetActive(true);
                button.interactable =
                    entry.Status == ProductionMapEntryStatus.Available;
                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = $"{entry.Header}\n{entry.StatusLabel}";
                    label.font = StatusHUDController.RuntimeKoreanFont;
                    label.enableAutoSizing = true;
                    label.fontSizeMin = 13f;
                    label.fontSizeMax = 20f;
                }

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(
                    () => TryEnterDialogueOnlyScene(entry.SceneId));
            }

            unresolvedLabel.text = CurrentViewModel.UnresolvedScenes.Count == 0
                ? string.Empty
                : "배경 미확정 장면: " + string.Join(
                    ", ",
                    CurrentViewModel.UnresolvedScenes
                        .Select(scene =>
                            $"{scene.SceneId}({scene.NarrativeLocationCode})"));
        }

        private void CreateScrollContent()
        {
            GameObject viewportObject = new(
                "Dynamic Location Viewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(RectMask2D));
            viewportObject.transform.SetParent(roomsContainer, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(0f, 52f);
            viewport.offsetMax = Vector2.zero;
            Image maskImage = viewportObject.GetComponent<Image>();
            maskImage.color = new Color(0f, 0f, 0f, 0.01f);
            maskImage.raycastTarget = true;

            GameObject contentObject = new(
                "Dynamic Location Content",
                typeof(RectTransform),
                typeof(GridLayoutGroup));
            contentObject.transform.SetParent(viewport, false);
            dynamicContent = contentObject.GetComponent<RectTransform>();
            dynamicContent.anchorMin = new Vector2(0f, 1f);
            dynamicContent.anchorMax = new Vector2(1f, 1f);
            dynamicContent.pivot = new Vector2(0.5f, 1f);
            dynamicContent.anchoredPosition = Vector2.zero;
            GridLayoutGroup grid = contentObject.GetComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(16, 16, 16, 16);
            grid.spacing = new Vector2(12f, 12f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;

            ScrollRect scroll = roomsContainer.gameObject.GetComponent<ScrollRect>() ??
                                roomsContainer.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = dynamicContent;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
        }

        private void CreateUnresolvedLabel()
        {
            GameObject labelObject = new(
                "Unresolved Scene Notice",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(roomsContainer, false);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(12f, 4f);
            rect.offsetMax = new Vector2(-12f, 48f);
            unresolvedLabel = labelObject.GetComponent<TMP_Text>();
            unresolvedLabel.font = StatusHUDController.RuntimeKoreanFont;
            unresolvedLabel.fontSize = 13f;
            unresolvedLabel.color = new Color32(255, 205, 120, 255);
            unresolvedLabel.alignment = TextAlignmentOptions.Center;
            unresolvedLabel.textWrappingMode = TextWrappingModes.Normal;
        }

        private void SelectEntry(ProductionMapEntry entry)
        {
            if (entry.StartsProductionScene)
            {
                TryTravelToScene(entry.SceneId);
            }
            else
            {
                SelectLocation(entry.Location);
            }
        }

        private void SelectLocation(LocationDefinition location)
        {
            GameStateManager state = GameStateManager.Instance;
            LastTravelResult = SceneTravelPolicy.EvaluateLocation(
                location,
                state != null ? state.PublicAnxiety : 0);
            if (TryLoadAllowedDestination(LastTravelResult))
            {
                UIManager.Instance?.ShowIngame();
            }
            else
            {
                ShowTravelFeedback();
            }
        }

        public SceneTravelResult TryTravelToScene(string sceneId)
        {
            LastTravelResult = CreateTravelCoordinator().TryEnter(sceneId);
            if (!LastTravelResult.IsAllowed)
            {
                ShowTravelFeedback();
                return LastTravelResult;
            }

            UIManager.Instance?.ShowIngame();
            return LastTravelResult;
        }

        public SceneTravelResult TryEnterDialogueOnlyScene(string sceneId)
        {
            LastTravelResult =
                CreateTravelCoordinator().TryEnterDialogueOnly(sceneId);
            if (!LastTravelResult.IsAllowed)
            {
                ShowTravelFeedback();
                return LastTravelResult;
            }

            UIManager.Instance?.ShowIngame();
            return LastTravelResult;
        }

        private ProductionSceneTravelCoordinator CreateTravelCoordinator()
        {
            return new ProductionSceneTravelCoordinator(
                locationGraph,
                GameStateManager.Instance,
                DialogueController.Instance,
                location =>
                    LocationLoader.Instance != null &&
                    LocationLoader.Instance.TryLoadLocation(location, out _));
        }

        private void ShowTravelFeedback()
        {
            InvestigationFeedback feedback =
                InvestigationFeedbackCatalog.ForTravel(LastTravelResult);
            ToastController.Instance?.Show(
                $"{feedback.Title}\n{feedback.Message}");
        }

        private bool TryLoadAllowedDestination(SceneTravelResult result)
        {
            if (!result.IsAllowed || LocationLoader.Instance == null ||
                LocationLoader.Instance.TryLoadLocation(result.Location, out _))
            {
                return result.IsAllowed && LocationLoader.Instance != null;
            }

            LastTravelResult = SceneTravelResult.Denied(
                SceneAccessDenialReason.LocationLoadFailed,
                $"Location '{result.Location.LocationCode}' could not load visual content.",
                result.Scene,
                result.Location);
            return false;
        }
    }
}
