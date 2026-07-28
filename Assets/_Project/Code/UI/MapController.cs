using System;
using System.Collections.Generic;
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
        private Image cruiseMapImage;
        private TMP_Text screenTitle;
        private readonly Dictionary<string, MapNodeView> mapNodes =
            new(StringComparer.Ordinal);
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

            foreach (MapNodeView node in mapNodes.Values)
            {
                node.Root.SetActive(false);
            }

            GameStateManager state = GameStateManager.Instance;
            CurrentViewModel = ProductionMapViewModel.Create(
                locationGraph,
                state?.CompletedProductionSceneIds,
                state != null ? state.PublicAnxiety : 0,
                state?.FinalEndingId,
                state?.UnlockedProductionSceneIds);
            ProductionObjectivePresentation? objective =
                state != null
                    ? ProductionObjectiveViewModel.Resolve(state).Presentation
                    : null;

            foreach (ProductionMapEntry entry in CurrentViewModel.Entries)
            {
                if (!mapNodes.TryGetValue(
                        entry.Spec.Code,
                        out MapNodeView node))
                {
                    Debug.LogError(
                        $"MapController is missing the authored node " +
                        $"'Map Node {entry.Spec.Code}'.");
                    continue;
                }

                bool isObjectiveDestination =
                    objective.HasValue &&
                    objective.Value.MarkerMode == ObjectiveMarkerMode.Map &&
                    string.Equals(
                        objective.Value.TargetLocation,
                        entry.Spec.Code,
                        StringComparison.Ordinal);
                ApplyEntry(node, entry, isObjectiveDestination);
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

            for (int index = 0; index < roomsContainer.childCount; index++)
            {
                Transform child = roomsContainer.GetChild(index);
                if (child.name.StartsWith(
                        "Room ",
                        StringComparison.Ordinal))
                {
                    child.gameObject.SetActive(false);
                }
            }

            Transform mapPanel = roomsContainer.parent;
            screenTitle = mapPanel
                .Find("Map Screen Title")
                ?.GetComponent<TMP_Text>();
            if (screenTitle != null)
            {
                MapTypography.ApplyLocation(screenTitle);
                screenTitle.text = "엘리시움호 · 장소 선택";
            }

            Transform viewport = roomsContainer.Find(
                "Dynamic Location Viewport");
            dynamicContent = viewport
                ?.Find("Dynamic Location Content") as RectTransform;
            deckScroll = viewport?.GetComponent<ScrollRect>();
            cruiseMapImage = dynamicContent
                ?.Find("MV Elysium Cutaway")
                ?.GetComponent<Image>();
            if (dynamicContent == null ||
                deckScroll == null ||
                cruiseMapImage == null)
            {
                Debug.LogError(
                    "MapController requires the authored Map viewport, " +
                    "content and cutaway image in UI Basic Scene.");
                return false;
            }

            deckScroll.viewport = viewport as RectTransform;
            deckScroll.content = dynamicContent;
            Canvas.ForceUpdateCanvases();
            deckScroll.verticalNormalizedPosition = 1f;
            cruiseMapImage.sprite = cruiseMapSprite;
            cruiseMapImage.color = cruiseMapSprite != null
                ? Color.white
                : UiVisualThemeService.Resolve(UiColorToken.Canvas);
            if (cruiseMapSprite == null)
            {
                Debug.LogError(
                    "MapController has no cruise map sprite assigned. " +
                    "The location selector will use its dark fallback background.");
            }

            BindAuthoredNodes();
            initialized = true;
            return true;
        }

        private void BindAuthoredNodes()
        {
            mapNodes.Clear();
            for (int index = 0; index < dynamicContent.childCount; index++)
            {
                Transform child = dynamicContent.GetChild(index);
                if (!child.name.StartsWith(
                        "Map Node ",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                string code = child.name["Map Node ".Length..];
                Image image = child.GetComponent<Image>();
                Button button = child.GetComponent<Button>();
                Outline outline = child.GetComponent<Outline>();
                TMP_Text label = child.Find("Label")?.GetComponent<TMP_Text>();
                if (image == null ||
                    button == null ||
                    outline == null ||
                    label == null)
                {
                    Debug.LogError(
                        $"Authored map node '{child.name}' is incomplete.");
                    continue;
                }

                MapTypography.ApplyLocation(label);
                GameObject destinationMarker =
                    EnsureDestinationMarker(child);
                mapNodes[code] = new MapNodeView(
                    child.gameObject,
                    image,
                    button,
                    outline,
                    label,
                    destinationMarker);
            }
        }

        private static GameObject EnsureDestinationMarker(Transform node)
        {
            Transform existing = node.Find("Objective Destination Arrow");
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject marker = new(
                "Objective Destination Arrow",
                typeof(RectTransform));
            marker.transform.SetParent(node, false);
            RectTransform markerRect =
                marker.GetComponent<RectTransform>();
            markerRect.anchorMin = new Vector2(1f, 0.5f);
            markerRect.anchorMax = new Vector2(1f, 0.5f);
            markerRect.pivot = new Vector2(1f, 0.5f);
            markerRect.anchoredPosition = new Vector2(-10f, 0f);
            markerRect.sizeDelta = new Vector2(48f, 34f);

            Color color = new Color32(255, 205, 84, 255);
            CreateArrowPart(
                markerRect,
                "Shaft",
                new Vector2(24f, 8f),
                new Vector2(3f, 0f),
                0f,
                color);
            CreateArrowPart(
                markerRect,
                "Head Upper",
                new Vector2(20f, 8f),
                new Vector2(24f, 7f),
                42f,
                color);
            CreateArrowPart(
                markerRect,
                "Head Lower",
                new Vector2(20f, 8f),
                new Vector2(24f, -7f),
                -42f,
                color);
            marker.transform.SetAsLastSibling();
            marker.SetActive(false);
            return marker;
        }

        private static void CreateArrowPart(
            RectTransform parent,
            string name,
            Vector2 size,
            Vector2 position,
            float rotation,
            Color color)
        {
            GameObject part = new(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            part.transform.SetParent(parent, false);
            RectTransform rect = part.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            Image image = part.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private void ApplyEntry(
            MapNodeView node,
            ProductionMapEntry entry,
            bool isObjectiveDestination)
        {
            bool locked =
                entry.Status == ProductionMapEntryStatus.Locked;
            bool current = string.Equals(
                entry.Spec.Code,
                GameStateManager.Instance?.CurrentLocationCode,
                StringComparison.OrdinalIgnoreCase);
            node.Root.SetActive(true);
            node.Image.sprite = mapNodeSprite;
            node.Image.type = mapNodeSprite != null
                ? Image.Type.Sliced
                : Image.Type.Simple;
            node.Image.color = current
                ? UiVisualThemeService.Resolve(UiColorToken.Cream)
                : isObjectiveDestination && !locked
                    ? new Color32(255, 205, 84, 255)
                : locked
                ? UiVisualThemeService.Resolve(UiColorToken.Disabled)
                : entry.Status == ProductionMapEntryStatus.Completed
                    ? UiVisualThemeService.Resolve(UiColorToken.Success)
                    : UiVisualThemeService.Resolve(UiColorToken.Brass);
            node.Outline.effectColor = current
                ? UiVisualThemeService.Resolve(UiColorToken.Brass)
                : isObjectiveDestination && !locked
                    ? UiVisualThemeService.Resolve(UiColorToken.Focus)
                : locked
                ? UiVisualThemeService.Resolve(UiColorToken.SurfaceOverlay)
                : UiVisualThemeService.Resolve(UiColorToken.Focus);
            node.Outline.effectDistance =
                current || isObjectiveDestination
                    ? new Vector2(5f, -5f)
                    : new Vector2(2f, -2f);
            node.Button.interactable = !locked;
            node.DestinationMarker.SetActive(
                isObjectiveDestination && !current && !locked);
            if (isObjectiveDestination)
            {
                node.Root.transform.SetAsLastSibling();
            }
            node.Button.onClick.RemoveAllListeners();
            if (current)
                node.Button.onClick.AddListener(
                    () => UIManager.Instance?.ShowIngame());
            else
                node.Button.onClick.AddListener(() => SelectEntry(entry));
            node.Label.text =
                isObjectiveDestination && !current && !locked
                    ? $"목표 · {entry.Spec.DisplayName}"
                    : entry.Spec.DisplayName;
            node.Label.color = locked
                ? UiVisualThemeService.Resolve(UiColorToken.TextSecondary)
                : current
                    ? UiVisualThemeService.Resolve(UiColorToken.Canvas)
                    : UiVisualThemeService.Resolve(UiColorToken.TextPrimary);
        }

        private void SelectEntry(ProductionMapEntry entry)
        {
            if (entry.StartsProductionScene)
            {
                TryTravelToScene(entry.SceneId);
            }
            else
            {
                LocationLoader.Instance?.PrepareNarrativeScene(
                    entry.UsesSceneTravel
                        ? entry.SceneId
                        : string.Empty);
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
                    LocationLoader.Instance.TryLoadLocation(location, out _),
                sceneId =>
                    LocationLoader.Instance?.PrepareNarrativeScene(sceneId));
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

        private readonly struct MapNodeView
        {
            public MapNodeView(
                GameObject root,
                Image image,
                Button button,
                Outline outline,
                TMP_Text label,
                GameObject destinationMarker)
            {
                Root = root;
                Image = image;
                Button = button;
                Outline = outline;
                Label = label;
                DestinationMarker = destinationMarker;
            }

            public GameObject Root { get; }
            public Image Image { get; }
            public Button Button { get; }
            public Outline Outline { get; }
            public TMP_Text Label { get; }
            public GameObject DestinationMarker { get; }
        }
    }
}
