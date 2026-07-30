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
        private const float MapTravelFadeSeconds = .45f;

        private static readonly string[] AtriumInvestigationMonologueLines =
        {
            "다른 사람의 이야기도 들어보자.",
            "아직은 더 탐문을 할 때야."
        };

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
        private readonly LayeredMapPresenter layeredPresenter = new();
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

            // The authored cutaway nodes are a compatibility view. A scene
            // that uses only the layered deck map intentionally has none, so
            // do not report every canonical location as a missing object.
            if (mapNodes.Count > 0)
            {
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

            layeredPresenter.Refresh(
                CurrentViewModel,
                state,
                objective?.TargetLocation ?? string.Empty,
                objective?.Definition.SceneId ?? string.Empty);
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
            MapScreenBackdropPresenter.Ensure(mapPanel);
            screenTitle = mapPanel
                .Find("Map Screen Title")
                ?.GetComponent<TMP_Text>();
            if (screenTitle != null)
            {
                MapTypography.ApplyScreenTitle(screenTitle);
                screenTitle.text = string.Empty;
                screenTitle.enableAutoSizing = true;
                screenTitle.fontSizeMin = 26f;
                screenTitle.fontSizeMax = 38f;
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
            layeredPresenter.Build(
                roomsContainer,
                screenTitle,
                BeginConfirmedTravel);
            viewport.gameObject.SetActive(false);
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

            foreach (CanonicalLocationSpec spec in
                     CanonicalLocationCatalog.StoryRelevant)
            {
                if (!mapNodes.ContainsKey(spec.Code))
                {
                    MapNodeView compatibilityNode =
                        CreateCompatibilityNode(spec);
                    mapNodes[spec.Code] = compatibilityNode;
                }
            }
        }

        private MapNodeView CreateCompatibilityNode(
            CanonicalLocationSpec spec)
        {
            GameObject nodeObject = new(
                $"Map Node {spec.Code}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(Outline));
            nodeObject.transform.SetParent(dynamicContent, false);
            RectTransform rect =
                nodeObject.GetComponent<RectTransform>();
            Vector2 position =
                CruiseMapLayoutCatalog.PositionFor(spec.Code);
            rect.anchorMin = position;
            rect.anchorMax = position;
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(154f, 58f);

            GameObject labelObject = new(
                "Label",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(nodeObject.transform, false);
            RectTransform labelRect =
                labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 4f);
            labelRect.offsetMax = new Vector2(-8f, -4f);
            TMP_Text label =
                labelObject.GetComponent<TMP_Text>();
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = 21f;
            label.raycastTarget = false;
            MapTypography.ApplyLocation(label);

            return new MapNodeView(
                nodeObject,
                nodeObject.GetComponent<Image>(),
                nodeObject.GetComponent<Button>(),
                nodeObject.GetComponent<Outline>(),
                label,
                EnsureDestinationMarker(nodeObject.transform));
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
            node.Root.SetActive(entry.IsVisible);
            if (!entry.IsVisible)
            {
                node.Button.interactable = false;
                node.Button.onClick.RemoveAllListeners();
                node.DestinationMarker.SetActive(false);
                return;
            }

            bool locked =
                entry.Status == ProductionMapEntryStatus.Locked;
            bool current = string.Equals(
                entry.Spec.Code,
                GameStateManager.Instance?.CurrentLocationCode,
                StringComparison.OrdinalIgnoreCase);
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

        private void BeginConfirmedTravel(ProductionMapEntry entry)
        {
            ScreenFadeTransition transition =
                ScreenFadeTransition.Ensure();
            if (transition == null)
            {
                SelectEntry(entry);
                return;
            }
            AudioManager audio = AudioManager.Instance;
            // Hold the screen fully black until the travel footstep sound
            // finishes, rather than starting the reveal partway through it.
            float holdSeconds = Mathf.Max(
                0f,
                AudioCueCatalog.MapTravelFootstepSeconds - MapTravelFadeSeconds);
            transition.Run(
                () =>
                {
                    audio?.CompleteMapTravelFadeOut();
                    SelectEntry(entry);
                    audio?.ResumeCurrentLocationIfTravelPending();
                },
                MapTravelFadeSeconds,
                MapTravelFadeSeconds,
                () => audio?.BeginMapTravelAudio(
                    entry.Spec.Code,
                    MapTravelFadeSeconds,
                    MapTravelFadeSeconds),
                () => audio?.EndMapTravelAudio(),
                holdSeconds);
        }

        private void SelectLocation(LocationDefinition location)
        {
            GameStateManager state = GameStateManager.Instance;
            string currentLocationCode =
                LocationLoader.Instance?.CurrentLocation?.LocationCode ??
                state?.CurrentLocationCode ??
                string.Empty;
            if (SceneTravelPolicy.IsTravelBlockedByIncompleteInvestigation(
                    currentLocationCode,
                    location?.LocationCode,
                    state))
            {
                UIManager.Instance?.ShowIngame();
                DialogueController.Instance?.StartAmbientLine(
                    "ADRIAN",
                    AtriumInvestigationMonologueLines[
                        UnityEngine.Random.Range(
                            0,
                            AtriumInvestigationMonologueLines.Length)],
                    "internal");
                return;
            }

            LastTravelResult = SceneTravelPolicy.EvaluateMapTravel(
                location,
                state?.CompletedProductionSceneIds,
                state?.UnlockedProductionSceneIds,
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
            if (LastTravelResult.DenialReason ==
                SceneAccessDenialReason.RestrictedByPublicAnxiety)
            {
                AudioManager.Instance?.PlayIronDoorKnock();
            }
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
