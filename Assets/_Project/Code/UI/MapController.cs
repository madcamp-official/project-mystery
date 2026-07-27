using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.Core;
using Wake.Exploration;
using Wake.Narrative;

namespace Wake.UI
{
    public class MapController : MonoBehaviour
    {
        [SerializeField] private LocationGraph locationGraph;
        [SerializeField] private Sprite cruiseMapSprite;
        [SerializeField] private Sprite mapNodeSprite;

        public SceneTravelResult LastTravelResult { get; private set; }
        public ProductionMapViewModel CurrentViewModel { get; private set; }

        private Transform roomsContainer;
        private RectTransform dynamicContent;
        private ScrollRect deckScroll;
        private bool initialized;

        private void Start()
        {
            EnsureInitialized();
            RefreshMap();
        }

        public void RefreshMap()
        {
            if (!EnsureInitialized() || locationGraph == null)
            {
                return;
            }

            for (int index = dynamicContent.childCount - 1; index >= 1; index--)
            {
                Destroy(dynamicContent.GetChild(index).gameObject);
            }

            GameStateManager state = GameStateManager.Instance;
            CurrentViewModel = ProductionMapViewModel.Create(
                locationGraph,
                state?.CompletedProductionSceneIds,
                state != null ? state.PublicAnxiety : 0,
                state?.FinalEndingId,
                state?.UnlockedProductionSceneIds);

            foreach (ProductionMapEntry entry in CurrentViewModel.Entries)
            {
                CreateLocationNode(entry);
            }
        }

        private bool EnsureInitialized()
        {
            if (initialized)
            {
                return dynamicContent != null;
            }
            if (locationGraph == null)
            {
                Debug.LogWarning("MapController has no LocationGraph assigned.");
                return false;
            }

            GameObject canvas = GameObject.Find("Canvas");
            roomsContainer = canvas?.transform.Find("Map/Rooms");
            if (roomsContainer == null)
            {
                Debug.LogError("MapController could not find Map/Rooms.");
                return false;
            }

            ConfigureFullscreenPanel();
            Button[] buttons = roomsContainer.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                button.gameObject.SetActive(false);
            }

            CreateMapSurface();
            initialized = true;
            return true;
        }

        private void ConfigureFullscreenPanel()
        {
            RectTransform mapPanel = roomsContainer.parent as RectTransform;
            mapPanel.anchorMin = Vector2.zero;
            mapPanel.anchorMax = Vector2.one;
            mapPanel.offsetMin = Vector2.zero;
            mapPanel.offsetMax = Vector2.zero;
            mapPanel.localScale = Vector3.one;

            RectTransform rooms = (RectTransform)roomsContainer;
            rooms.anchorMin = Vector2.zero;
            rooms.anchorMax = Vector2.one;
            rooms.offsetMin = new Vector2(24f, 24f);
            rooms.offsetMax = new Vector2(-24f, -88f);
            rooms.localScale = Vector3.one;

            Transform backTransform = mapPanel.Find("Back Btn");
            if (backTransform is RectTransform back)
            {
                back.anchorMin = new Vector2(0f, 1f);
                back.anchorMax = new Vector2(0f, 1f);
                back.pivot = new Vector2(0f, 1f);
                back.anchoredPosition = new Vector2(24f, -150f);
                back.sizeDelta = new Vector2(164f, 54f);
                back.localScale = Vector3.one;
            }

            Transform legacyDecoration = mapPanel.Find("Image");
            if (legacyDecoration != null)
            {
                legacyDecoration.gameObject.SetActive(false);
            }

            GameObject titleObject = new(
                "Map Screen Title",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            titleObject.transform.SetParent(mapPanel, false);
            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(210f, -205f);
            titleRect.offsetMax = new Vector2(-210f, -140f);
            TMP_Text title = titleObject.GetComponent<TMP_Text>();
            MapTypography.ApplyLocation(title);
            title.text = "MV ELYSIUM  ·  장소 선택";
            title.fontSize = 34f;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color32(244, 214, 150, 255);
            title.raycastTarget = false;
        }

        private void CreateMapSurface()
        {
            GameObject viewportObject = new(
                "Dynamic Location Viewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(RectMask2D),
                typeof(ScrollRect));
            viewportObject.transform.SetParent(roomsContainer, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(0.015f, 0.025f, 0.045f, 1f);
            deckScroll = viewportObject.GetComponent<ScrollRect>();

            GameObject contentObject = new(
                "Dynamic Location Content",
                typeof(RectTransform));
            contentObject.transform.SetParent(viewport, false);
            dynamicContent = contentObject.GetComponent<RectTransform>();
            dynamicContent.anchorMin = new Vector2(0f, 1f);
            dynamicContent.anchorMax = new Vector2(1f, 1f);
            dynamicContent.pivot = new Vector2(0.5f, 1f);
            dynamicContent.anchoredPosition = Vector2.zero;
            dynamicContent.sizeDelta = new Vector2(0f, 1480f);
            deckScroll.viewport = viewport;
            deckScroll.content = dynamicContent;
            deckScroll.horizontal = false;
            deckScroll.vertical = true;
            deckScroll.movementType = ScrollRect.MovementType.Elastic;
            deckScroll.inertia = true;
            deckScroll.scrollSensitivity = 42f;
            deckScroll.verticalNormalizedPosition = 1f;

            GameObject backgroundObject = new(
                "MV Elysium Cutaway",
                typeof(RectTransform),
                typeof(Image));
            backgroundObject.transform.SetParent(dynamicContent, false);
            RectTransform background = backgroundObject.GetComponent<RectTransform>();
            background.anchorMin = Vector2.zero;
            background.anchorMax = Vector2.one;
            background.offsetMin = Vector2.zero;
            background.offsetMax = Vector2.zero;
            Image image = backgroundObject.GetComponent<Image>();
            image.sprite = cruiseMapSprite;
            image.preserveAspect = false;
            image.raycastTarget = false;
            image.color = cruiseMapSprite != null
                ? Color.white
                : new Color(0.015f, 0.025f, 0.045f, 1f);
            if (cruiseMapSprite == null)
            {
                Debug.LogError(
                    "MapController has no cruise map sprite assigned. " +
                    "The location selector will use its dark fallback background.");
            }
        }

        private void CreateLocationNode(ProductionMapEntry entry)
        {
            GameObject nodeObject = new(
                $"Map Node {entry.Spec.Code}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(Outline));
            nodeObject.transform.SetParent(dynamicContent, false);
            RectTransform rect = nodeObject.GetComponent<RectTransform>();
            Vector2 position = CruiseMapLayoutCatalog.PositionFor(entry.Spec.Code);
            rect.anchorMin = position;
            rect.anchorMax = position;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(154f, 58f);

            bool locked = entry.Status == ProductionMapEntryStatus.Locked;
            Image image = nodeObject.GetComponent<Image>();
            image.sprite = mapNodeSprite;
            image.type = mapNodeSprite != null
                ? Image.Type.Sliced
                : Image.Type.Simple;
            image.color = locked
                ? new Color32(48, 53, 62, 235)
                : entry.Status == ProductionMapEntryStatus.Completed
                    ? new Color32(55, 105, 105, 245)
                    : new Color32(183, 133, 54, 250);
            Outline outline = nodeObject.GetComponent<Outline>();
            outline.effectColor = locked
                ? new Color32(15, 18, 24, 230)
                : new Color32(246, 211, 135, 240);
            outline.effectDistance = new Vector2(0.45f, -0.45f);

            Button button = nodeObject.GetComponent<Button>();
            button.interactable = !locked;
            button.onClick.AddListener(() => SelectEntry(entry));

            GameObject labelObject = new(
                "Label",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(nodeObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 4f);
            labelRect.offsetMax = new Vector2(-8f, -4f);
            TMP_Text label = labelObject.GetComponent<TMP_Text>();
            MapTypography.ApplyLocation(label);
            label.text = $"{entry.Spec.DisplayName}\n{entry.StatusLabel}";
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = 21f;
            label.color = locked
                ? new Color32(175, 180, 188, 255)
                : Color.white;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
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
            LastTravelResult = SceneTravelPolicy.EvaluateFreeTravel(
                location,
                state?.CompletedProductionSceneIds,
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
            LastTravelResult = CreateTravelCoordinator().TryEnterDialogueOnly(sceneId);
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
