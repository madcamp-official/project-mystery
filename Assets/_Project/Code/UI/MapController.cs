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

                ApplyEntry(node, entry);
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
                mapNodes[code] = new MapNodeView(
                    child.gameObject,
                    image,
                    button,
                    outline,
                    label);
            }
        }

        private void ApplyEntry(
            MapNodeView node,
            ProductionMapEntry entry)
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
                : locked
                ? UiVisualThemeService.Resolve(UiColorToken.Disabled)
                : entry.Status == ProductionMapEntryStatus.Completed
                    ? UiVisualThemeService.Resolve(UiColorToken.Success)
                    : UiVisualThemeService.Resolve(UiColorToken.Brass);
            node.Outline.effectColor = current
                ? UiVisualThemeService.Resolve(UiColorToken.Brass)
                : locked
                ? UiVisualThemeService.Resolve(UiColorToken.SurfaceOverlay)
                : UiVisualThemeService.Resolve(UiColorToken.Focus);
            node.Outline.effectDistance =
                current ? new Vector2(5f, -5f) : new Vector2(2f, -2f);
            node.Button.interactable = !locked;
            node.Button.onClick.RemoveAllListeners();
            if (current)
                node.Button.onClick.AddListener(
                    () => UIManager.Instance?.ShowIngame());
            else
                node.Button.onClick.AddListener(() => SelectEntry(entry));
            node.Label.text = entry.Spec.DisplayName;
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
                TMP_Text label)
            {
                Root = root;
                Image = image;
                Button = button;
                Outline = outline;
                Label = label;
            }

            public GameObject Root { get; }
            public Image Image { get; }
            public Button Button { get; }
            public Outline Outline { get; }
            public TMP_Text Label { get; }
        }
    }
}
