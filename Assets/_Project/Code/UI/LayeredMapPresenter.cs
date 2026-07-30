using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.Core;
using Wake.Exploration;
using Wake.Narrative;

namespace Wake.UI
{
    public sealed class LayeredMapPresenter
    {
        private readonly Dictionary<int, Button> deckButtons = new();
        private readonly Dictionary<MapLayerMode, Button> layerButtons = new();
        private readonly List<GameObject> nodeObjects = new();
        private readonly List<GameObject> structuralAnnotationObjects = new();
        private readonly MapPassengerRedactionRenderer redactionRenderer =
            new();
        private readonly MapRestrictedAreaRenderer restrictedAreaRenderer =
            new();
        private readonly MapRoomHitAreaRenderer roomHitAreaRenderer = new();

        private RectTransform root;
        private RectTransform mapFrame;
        private RectTransform structuralAnnotationLayer;
        private RectTransform nodeLayer;
        private Image baseMap;
        private Image restrictedOverlay;
        private Image technicalOverlay;
        private TMP_Text screenTitle;
        private TMP_Text placeName;
        private TMP_Text placeMeta;
        private TMP_Text placeDescription;
        private TMP_Text knownPeople;
        private TMP_Text accessDescription;
        private Button travelButton;
        private TMP_Text travelLabel;
        private ProductionMapViewModel viewModel;
        private GameStateManager state;
        private ProductionMapEntry selectedEntry;
        private Action<ProductionMapEntry> travelAction;
        private string objectiveSceneId = string.Empty;
        private string objectiveLocationCode = string.Empty;
        private int selectedDeck = 8;
        private MapLayerMode selectedLayer = MapLayerMode.Passenger;

        public bool IsBuilt => root != null;
        public int SelectedDeck => selectedDeck;
        public MapLayerMode SelectedLayer => selectedLayer;
        public string SelectedLocationCode =>
            selectedEntry?.Spec.Code ?? string.Empty;

        public void Build(
            Transform parent,
            TMP_Text title,
            Action<ProductionMapEntry> onTravel)
        {
            if (root != null || parent == null)
                return;

            travelAction = onTravel;
            screenTitle = title;
            root = Panel(parent, "Layered Map Surface");
            Stretch(root);

            RectTransform deckRail = Panel(root, "Deck Selector");
            SetAnchors(deckRail, .01f, .10f, .105f, .94f);
            VerticalLayoutGroup deckLayout =
                deckRail.gameObject.AddComponent<VerticalLayoutGroup>();
            deckLayout.padding = new RectOffset(10, 10, 14, 14);
            deckLayout.spacing = 10f;
            deckLayout.childControlHeight = true;
            deckLayout.childControlWidth = true;
            deckLayout.childForceExpandHeight = false;
            deckLayout.childForceExpandWidth = true;

            foreach (int deck in MapDeckCatalog.DeckOrder)
            {
                int captured = deck;
                Button button = LayoutButton(
                    deckRail,
                    MapDeckCatalog.DeckLabel(deck),
                    () => SelectDeck(captured));
                button.gameObject.name = $"Deck {deck} Tab";
                MapTypography.ApplyCode(
                    button.GetComponentInChildren<TMP_Text>(true));
                deckButtons[deck] = button;
            }

            RectTransform layerRail = Panel(root, "Layer Selector");
            SetAnchors(layerRail, .12f, .89f, .75f, .98f);
            HorizontalLayoutGroup layerLayout =
                layerRail.gameObject.AddComponent<HorizontalLayoutGroup>();
            layerLayout.padding = new RectOffset(12, 12, 8, 8);
            layerLayout.spacing = 12f;
            layerLayout.childControlWidth = true;
            layerLayout.childControlHeight = true;
            layerLayout.childForceExpandWidth = true;
            layerLayout.childForceExpandHeight = true;
            AddLayerButton(
                layerRail,
                MapLayerMode.Passenger,
                "선내도");
            AddLayerButton(
                layerRail,
                MapLayerMode.Investigation,
                "수사 주석");
            AddLayerButton(
                layerRail,
                MapLayerMode.Technical,
                "설비도");

            mapFrame = Panel(root, "Deck Map");
            SetAnchors(mapFrame, .12f, .12f, .75f, .88f);
            AspectRatioFitter mapAspect =
                mapFrame.gameObject.AddComponent<AspectRatioFitter>();
            mapAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            mapAspect.aspectRatio = 1448f / 1086f;
            baseMap = LayerImage(mapFrame, "Base Map");
            restrictedOverlay =
                LayerImage(mapFrame, "Investigation Overlay Fallback");
            technicalOverlay =
                LayerImage(mapFrame, "Technical Overlay");
            redactionRenderer.Build(mapFrame);
            structuralAnnotationLayer = new GameObject(
                "Structural Map Annotations",
                typeof(RectTransform)).GetComponent<RectTransform>();
            structuralAnnotationLayer.SetParent(mapFrame, false);
            Stretch(structuralAnnotationLayer);
            restrictedAreaRenderer.Build(mapFrame);
            roomHitAreaRenderer.Build(mapFrame);
            nodeLayer = new GameObject(
                "Map Location Nodes",
                typeof(RectTransform)).GetComponent<RectTransform>();
            nodeLayer.SetParent(mapFrame, false);
            Stretch(nodeLayer);

            RectTransform info = Panel(root, "Location Detail");
            SetAnchors(info, .76f, .12f, .99f, .88f);
            VerticalLayoutGroup infoLayout =
                info.gameObject.AddComponent<VerticalLayoutGroup>();
            infoLayout.padding = new RectOffset(24, 24, 24, 24);
            infoLayout.spacing = 14f;
            infoLayout.childControlHeight = true;
            infoLayout.childControlWidth = true;
            infoLayout.childForceExpandWidth = true;
            infoLayout.childForceExpandHeight = false;

            Text(
                info,
                "Detail Caption",
                "선택한 장소",
                UiTextStyle.Technical,
                28f);
            placeName = Text(
                info,
                "Location Name",
                string.Empty,
                UiTextStyle.Heading,
                70f);
            placeMeta = Text(
                info,
                "Location Meta",
                string.Empty,
                UiTextStyle.Caption,
                48f);
            placeDescription = Text(
                info,
                "Location Description",
                "지도에서 장소를 선택하세요.",
                UiTextStyle.Body,
                150f);
            knownPeople = Text(
                info,
                "Known People",
                "알려진 인물 · 없음",
                UiTextStyle.Body,
                90f);
            accessDescription = Text(
                info,
                "Access Description",
                string.Empty,
                UiTextStyle.Body,
                120f);
            travelButton = LayoutButton(
                info,
                "이동하기",
                ConfirmTravel,
                72f);
            travelLabel =
                travelButton.GetComponentInChildren<TMP_Text>(true);

            RectTransform legend = Panel(root, "Map Legend");
            SetAnchors(legend, .01f, .01f, .99f, .095f);
            BuildLegend(legend);
        }

        public void Refresh(
            ProductionMapViewModel model,
            GameStateManager gameState,
            string objectiveLocationCode,
            string targetSceneId)
        {
            if (root == null)
                return;

            viewModel = model;
            state = gameState;
            objectiveSceneId = targetSceneId ?? string.Empty;
            this.objectiveLocationCode =
                objectiveLocationCode ?? string.Empty;
            if (!MapDeckCatalog.IsLayerUnlocked(
                    selectedLayer,
                    state?.CompletedProductionSceneIds,
                    state?.UnlockedProductionSceneIds))
            {
                selectedLayer = MapLayerMode.Passenger;
            }
            string current = state?.CurrentLocationCode ?? string.Empty;
            MapLocationPlacement currentPlacement =
                MapDeckCatalog.Find(current);
            if (currentPlacement != null)
                selectedDeck = currentPlacement.Deck;

            RefreshLayerButtons();
            RefreshDeck(this.objectiveLocationCode);
            ClearSelection();
        }

        private void SelectDeck(int deck)
        {
            selectedDeck = deck;
            RefreshDeck(objectiveLocationCode);
            ClearSelection();
        }

        private void SelectLayer(MapLayerMode mode)
        {
            if (!MapDeckCatalog.IsLayerUnlocked(
                    mode,
                    state?.CompletedProductionSceneIds,
                    state?.UnlockedProductionSceneIds))
            {
                accessDescription.gameObject.SetActive(true);
                accessDescription.text = mode == MapLayerMode.Technical
                    ? "설비 자료를 확보한 뒤 확인할 수 있습니다."
                    : "서비스 구역을 조사한 뒤 확인할 수 있습니다.";
                return;
            }

            selectedLayer = mode;
            RefreshLayerButtons();
            ApplyLayerSprites();
            RefreshPassengerRedactions();
            RefreshRestrictedAreas();
        }

        private void RefreshLayerButtons()
        {
            foreach (var pair in layerButtons)
            {
                bool unlocked = MapDeckCatalog.IsLayerUnlocked(
                    pair.Key,
                    state?.CompletedProductionSceneIds,
                    state?.UnlockedProductionSceneIds);
                pair.Value.interactable =
                    unlocked && pair.Key != selectedLayer;
                TMP_Text label =
                    pair.Value.GetComponentInChildren<TMP_Text>(true);
                string baseLabel = pair.Key switch
                {
                    MapLayerMode.Investigation => "수사 주석",
                    MapLayerMode.Technical => "설비도",
                    _ => "선내도"
                };
                label.text = unlocked ? baseLabel : $"{baseLabel} [잠김]";
            }
        }

        private void RefreshDeck(string objectiveLocationCode)
        {
            foreach (var pair in deckButtons)
                pair.Value.interactable = pair.Key != selectedDeck;

            if (screenTitle != null)
            {
                screenTitle.text =
                    MapDeckCatalog.DeckDisplayTitle(selectedDeck);
            }
            ApplyLayerSprites();
            RefreshPassengerRedactions();
            RefreshStructuralAnnotations();
            RefreshRestrictedAreas();

            foreach (GameObject node in nodeObjects)
                UnityEngine.Object.Destroy(node);
            nodeObjects.Clear();
            roomHitAreaRenderer.Clear();

            if (viewModel == null)
                return;

            Dictionary<string, ProductionMapEntry> entries =
                viewModel.Entries.ToDictionary(
                    item => item.Spec.Code,
                    StringComparer.Ordinal);
            string objectiveCode =
                CanonicalLocationCatalog.FindSpec(objectiveLocationCode)?.Code ??
                objectiveLocationCode ?? string.Empty;
            string currentCode =
                CanonicalLocationCatalog.FindSpec(
                    state?.CurrentLocationCode)?.Code ??
                state?.CurrentLocationCode ?? string.Empty;
            foreach (MapLocationPlacement placement in
                     MapDeckCatalog.ForDeck(selectedDeck))
            {
                if (!entries.TryGetValue(
                        placement.LocationCode,
                        out ProductionMapEntry entry) ||
                    !MapDeckCatalog.ShouldReveal(
                        placement,
                        entry,
                        state?.CurrentLocationCode,
                        state?.CompletedProductionSceneIds,
                        state?.UnlockedProductionSceneIds))
                {
                    continue;
                }

                bool objective = string.Equals(
                    placement.LocationCode,
                    objectiveCode,
                    StringComparison.Ordinal);
                bool current = string.Equals(
                    placement.LocationCode,
                    currentCode,
                    StringComparison.OrdinalIgnoreCase);
                bool polygonBacked = roomHitAreaRenderer.Add(
                    placement.LocationCode,
                    entry.Status == ProductionMapEntryStatus.Locked,
                    objective,
                    current,
                    () => SelectEntry(entry, placement, current));
                CreateNode(
                    placement,
                    entry,
                    objective,
                    polygonBacked);
            }
        }

        private void ApplyLayerSprites()
        {
            baseMap.sprite = LoadLayer(
                selectedDeck,
                MapLayerMode.Passenger);
            restrictedOverlay.sprite = LoadLayer(
                selectedDeck,
                MapLayerMode.Investigation);
            technicalOverlay.sprite = LoadLayer(
                selectedDeck,
                MapLayerMode.Technical);
            baseMap.enabled = baseMap.sprite != null;
            restrictedOverlay.enabled =
                selectedLayer >= MapLayerMode.Investigation &&
                restrictedOverlay.sprite != null &&
                MapAreaCatalog.ForDeck(selectedDeck).Count == 0;
            technicalOverlay.enabled =
                selectedLayer >= MapLayerMode.Technical &&
                technicalOverlay.sprite != null;
        }

        private static Sprite LoadLayer(int deck, MapLayerMode mode)
        {
            string key = MapDeckCatalog.ResourceKey(deck, mode);
            return string.IsNullOrEmpty(key)
                ? null
                : Resources.Load<Sprite>(key);
        }

        private void RefreshRestrictedAreas()
        {
            restrictedAreaRenderer.Refresh(
                selectedDeck,
                selectedLayer >= MapLayerMode.Investigation,
                viewModel,
                state,
                SelectArea);
        }

        private void RefreshPassengerRedactions()
        {
            redactionRenderer.Refresh(
                selectedDeck,
                selectedLayer,
                state?.CompletedProductionSceneIds);
        }

        private void RefreshStructuralAnnotations()
        {
            foreach (GameObject annotation in structuralAnnotationObjects)
            {
                if (annotation != null)
                    UnityEngine.Object.Destroy(annotation);
            }
            structuralAnnotationObjects.Clear();

            Vector2? atriumPosition = selectedDeck switch
            {
                10 => new Vector2(.51f, .565f),
                9 => new Vector2(.485f, .405f),
                _ => null
            };
            if (!atriumPosition.HasValue ||
                structuralAnnotationLayer == null)
            {
                return;
            }

            TMP_Text label = Text(
                structuralAnnotationLayer,
                "Atrium Connection Label",
                "아트리움",
                UiTextStyle.Technical,
                TextAlignmentOptions.Center);
            structuralAnnotationObjects.Add(label.gameObject);
            RectTransform rect = label.rectTransform;
            rect.anchorMin = atriumPosition.Value;
            rect.anchorMax = atriumPosition.Value;
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(132f, 42f);
            label.enableAutoSizing = true;
            label.fontSizeMin = 16f;
            label.fontSizeMax = 24f;
            label.fontStyle = FontStyles.Bold;
            label.color =
                UiVisualThemeService.Resolve(UiColorToken.Cream);
            label.outlineColor = new Color32(3, 10, 18, 255);
            label.outlineWidth = .18f;
            MapTypography.ApplyLocation(label);
        }

        private void SelectArea(MapAreaShape area)
        {
            if (area == null || viewModel == null)
                return;

            ProductionMapEntry entry = viewModel.Entries.FirstOrDefault(item =>
                string.Equals(
                    item.Spec.Code,
                    area.AreaId,
                    StringComparison.Ordinal));
            MapLocationPlacement placement =
                MapDeckCatalog.Find(area.AreaId);
            if (entry != null && placement != null)
            {
                string current =
                    CanonicalLocationCatalog.FindSpec(
                        state?.CurrentLocationCode)?.Code ??
                    state?.CurrentLocationCode;
                SelectEntry(
                    entry,
                    placement,
                    string.Equals(
                        area.AreaId,
                        current,
                        StringComparison.OrdinalIgnoreCase));
                return;
            }

            selectedEntry = null;
            placeName.text = "잠긴 장소";
            placeMeta.text = MapDeckCatalog.DeckLabel(area.Deck);
            SetDetailFieldsVisible(false);
            placeDescription.text = string.Empty;
            knownPeople.text = string.Empty;
            accessDescription.text = string.Empty;
            travelButton.interactable = false;
            travelLabel.text = "이동 불가";
        }

        private void CreateNode(
            MapLocationPlacement placement,
            ProductionMapEntry entry,
            bool objective,
            bool polygonBacked)
        {
            GameObject rootObject = new(
                $"Layered Map Node {placement.LocationCode}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(Outline));
            rootObject.transform.SetParent(nodeLayer, false);
            nodeObjects.Add(rootObject);
            RectTransform rect =
                rootObject.GetComponent<RectTransform>();
            Vector2 nodePosition =
                MapInteractionGeometryCatalog.TryGetNode(
                    placement.LocationCode,
                    out MapLocationNode authoredNode)
                    ? authoredNode.NormalizedPosition
                    : placement.Position;
            rect.anchorMin = nodePosition;
            rect.anchorMax = nodePosition;
            rect.pivot = new Vector2(.5f, .5f);

            string currentLocation =
                CanonicalLocationCatalog.FindSpec(
                    state?.CurrentLocationCode)?.Code ??
                state?.CurrentLocationCode;
            bool current = string.Equals(
                placement.LocationCode,
                currentLocation,
                StringComparison.OrdinalIgnoreCase);
            bool locked =
                entry.Status == ProductionMapEntryStatus.Locked;
            rect.sizeDelta = locked
                ? new Vector2(52f, 62f)
                : new Vector2(150f, 54f);
            Image image = rootObject.GetComponent<Image>();
            image.color = locked
                ? Color.clear
                : current
                    ? UiVisualThemeService.Resolve(UiColorToken.Cream)
                    : objective
                        ? new Color32(255, 205, 84, 245)
                        : UiVisualThemeService.Resolve(UiColorToken.Brass);
            image.raycastTarget = !polygonBacked;
            Outline outline = rootObject.GetComponent<Outline>();
            outline.enabled = !locked;
            outline.effectColor = objective
                ? UiVisualThemeService.Resolve(UiColorToken.Focus)
                : UiVisualThemeService.Resolve(UiColorToken.SurfaceOverlay);
            outline.effectDistance =
                objective ? new Vector2(4f, -4f) : new Vector2(2f, -2f);

            Button button = rootObject.GetComponent<Button>();
            button.onClick.AddListener(() =>
                SelectEntry(entry, placement, current));
            if (locked)
            {
                GameObject lockObject = new(
                    "Lock Icon",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(MapPadlockGraphic));
                lockObject.transform.SetParent(rect, false);
                RectTransform lockRect =
                    lockObject.GetComponent<RectTransform>();
                Stretch(lockRect, 4f);
                return;
            }

            TMP_Text label = Text(
                rect,
                "Label",
                entry.Spec.DisplayName,
                UiTextStyle.Technical,
                TextAlignmentOptions.Center);
            Stretch(label.rectTransform, 6f);
            label.raycastTarget = false;
            label.color = current
                ? UiVisualThemeService.Resolve(UiColorToken.Canvas)
                : UiVisualThemeService.Resolve(UiColorToken.TextPrimary);
        }

        private void SelectEntry(
            ProductionMapEntry entry,
            MapLocationPlacement placement,
            bool current)
        {
            if (entry.Status == ProductionMapEntryStatus.Locked)
            {
                selectedEntry = null;
                placeName.text = "잠긴 장소";
                placeMeta.text = MapDeckCatalog.DeckLabel(placement.Deck);
                SetDetailFieldsVisible(false);
                placeDescription.text = string.Empty;
                knownPeople.text = string.Empty;
                accessDescription.text = string.Empty;
                travelButton.interactable = false;
                travelLabel.text = "이동 불가";
                return;
            }

            selectedEntry = entry;
            SetDetailFieldsVisible(true);
            placeName.text = entry.Spec.DisplayName;
            placeMeta.text =
                $"{MapDeckCatalog.DeckLabel(placement.Deck)} · " +
                $"{TravelTierLabel(placement.TravelTier)}";
            placeDescription.text = placement.Description;
            string[] people = KnownPeopleAt(placement.LocationCode);
            knownPeople.text = people.Length == 0
                ? "알려진 인물 · 없음"
                : "마지막 목격 · " + string.Join(", ", people);
            accessDescription.text = current
                ? "현재 위치입니다."
                : placement.TravelTier == MapTravelTier.RouteOnly &&
                  entry.Status == ProductionMapEntryStatus.Available
                    ? "현재 목표의 지정 경로로 진입할 수 있습니다."
                    : $"접근 상태 · {entry.StatusLabel}";
            travelButton.interactable = true;
            travelLabel.text = current
                ? "지도로 돌아가기"
                : placement.TravelTier == MapTravelTier.RouteOnly
                    ? "목표 경로로 이동"
                    : "이동하기";
        }

        private void ClearSelection()
        {
            selectedEntry = null;
            SetDetailFieldsVisible(true);
            placeName.text = "장소를 선택하세요";
            placeMeta.text = MapDeckCatalog.DeckLabel(selectedDeck);
            placeDescription.text =
                "지도 위 장소를 선택하면 상세 정보와 접근 상태를 확인할 수 있습니다.";
            knownPeople.text = "알려진 인물 · 없음";
            accessDescription.text = string.Empty;
            travelButton.interactable = false;
            travelLabel.text = "이동하기";
        }

        private void SetDetailFieldsVisible(bool visible)
        {
            placeDescription.gameObject.SetActive(visible);
            knownPeople.gameObject.SetActive(visible);
            accessDescription.gameObject.SetActive(visible);
        }

        private void ConfirmTravel()
        {
            if (selectedEntry == null)
                return;

            string currentLocation =
                CanonicalLocationCatalog.FindSpec(
                    state?.CurrentLocationCode)?.Code ??
                state?.CurrentLocationCode;
            if (string.Equals(
                    selectedEntry.Spec.Code,
                    currentLocation,
                    StringComparison.OrdinalIgnoreCase))
            {
                UIManager.Instance?.ShowIngame();
                return;
            }
            travelAction?.Invoke(selectedEntry);
        }

        private void AddLayerButton(
            RectTransform parent,
            MapLayerMode mode,
            string label)
        {
            Button button = LayoutButton(
                parent,
                label,
                () => SelectLayer(mode));
            button.gameObject.name = $"{mode} Layer Tab";
            layerButtons[mode] = button;
        }

        private static string TravelTierLabel(MapTravelTier tier) =>
            tier switch
            {
                MapTravelTier.ConditionalFastTravel => "조건부 이동",
                MapTravelTier.RouteOnly => "현장 경유 전용",
                _ => "공개 빠른 이동"
            };

        private string[] KnownPeopleAt(string locationCode)
        {
            if (!ScenePresenceCatalog.TryGet(
                    objectiveSceneId,
                    out ScenePresenceRecord presence))
            {
                return Array.Empty<string>();
            }

            return ProductionObjectiveNpcTargets.ForScene(objectiveSceneId)
                .Where(character =>
                {
                    CanonicalLocationSpec known =
                        CanonicalLocationCatalog.FindSpec(
                            presence.GetLocation(character));
                    return string.Equals(
                        known?.Code,
                        locationCode,
                        StringComparison.Ordinal);
                })
                .Select(DialoguePortraitCatalog.GetDisplayName)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();
        }

        private static void BuildLegend(RectTransform legend)
        {
            Image background = legend.GetComponent<Image>();
            if (background != null)
            {
                background.raycastTarget = false;
            }

            HorizontalLayoutGroup layout =
                legend.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 10, 10);
            layout.spacing = 28f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            AddLegendItem(
                legend,
                "Current",
                "현재 위치",
                UiColorToken.Cream,
                170f);
            AddLegendItem(
                legend,
                "Objective",
                "주요 목표",
                UiColorToken.Focus,
                170f);
            AddLegendItem(
                legend,
                "Known Person",
                "알려진 인물",
                UiColorToken.Success,
                190f);
            AddLegendItem(
                legend,
                "Locked",
                "접근 불가",
                UiColorToken.Disabled,
                170f);
            AddLegendItem(
                legend,
                "Close",
                "ESC 닫기",
                UiColorToken.Brass,
                150f);
        }

        private static void AddLegendItem(
            Transform parent,
            string name,
            string value,
            UiColorToken markerColor,
            float width)
        {
            GameObject item = new(
                $"Legend {name}",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            item.transform.SetParent(parent, false);
            LayoutElement itemLayout = item.GetComponent<LayoutElement>();
            itemLayout.preferredWidth = width;
            itemLayout.minWidth = width;

            HorizontalLayoutGroup row =
                item.GetComponent<HorizontalLayoutGroup>();
            row.spacing = 10f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = false;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            GameObject marker = new(
                "Marker",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            marker.transform.SetParent(item.transform, false);
            RectTransform markerRect =
                marker.GetComponent<RectTransform>();
            markerRect.sizeDelta = new Vector2(14f, 14f);
            Image markerImage = marker.GetComponent<Image>();
            markerImage.color =
                UiVisualThemeService.Resolve(markerColor);
            markerImage.raycastTarget = false;

            TMP_Text label = Text(
                item.transform,
                "Label",
                value,
                UiTextStyle.Caption,
                TextAlignmentOptions.MidlineLeft);
            RectTransform labelRect = label.rectTransform;
            labelRect.sizeDelta = new Vector2(width - 24f, 32f);
            MapTypography.ApplyLegend(label);
        }

        private static Image LayerImage(Transform parent, string name)
        {
            GameObject target = new(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            target.transform.SetParent(parent, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            Stretch(rect);
            Image image = target.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static RectTransform Panel(Transform parent, string name)
        {
            GameObject target = new(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            target.transform.SetParent(parent, false);
            Image image = target.GetComponent<Image>();
            UiVisualThemeService.ApplySurface(
                image,
                UiSurfaceStyle.Overlay);
            return target.GetComponent<RectTransform>();
        }

        private static Button LayoutButton(
            Transform parent,
            string label,
            UnityEngine.Events.UnityAction action,
            float height = 56f)
        {
            GameObject target = new(
                label,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));
            target.transform.SetParent(parent, false);
            LayoutElement element = target.GetComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = 48f;
            Button button = target.GetComponent<Button>();
            UiVisualThemeService.ApplyButton(
                button,
                UiButtonStyle.Secondary);
            button.onClick.AddListener(action);
            TMP_Text text = Text(
                target.transform,
                "Label",
                label,
                UiTextStyle.Choice,
                TextAlignmentOptions.Center);
            MapTypography.ApplyControl(text);
            Stretch(text.rectTransform, 6f);
            return button;
        }

        private static TMP_Text Text(
            Transform parent,
            string name,
            UiTextStyle style,
            TextAlignmentOptions alignment)
        {
            GameObject target = new(
                name,
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            target.transform.SetParent(parent, false);
            TMP_Text text = target.GetComponent<TMP_Text>();
            UiVisualThemeService.ApplyText(text, style);
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private static TMP_Text Text(
            Transform parent,
            string name,
            string value,
            UiTextStyle style,
            float height)
        {
            TMP_Text text = Text(
                parent,
                name,
                style,
                TextAlignmentOptions.MidlineLeft);
            text.text = value;
            LayoutElement layout =
                text.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            return text;
        }

        private static TMP_Text Text(
            Transform parent,
            string name,
            string value,
            UiTextStyle style,
            TextAlignmentOptions alignment)
        {
            TMP_Text text = Text(parent, name, style, alignment);
            text.text = value;
            return text;
        }

        private static void SetAnchors(
            RectTransform rect,
            float minX,
            float minY,
            float maxX,
            float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
