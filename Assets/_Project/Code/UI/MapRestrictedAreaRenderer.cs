using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wake.Core;

namespace Wake.UI
{
    public sealed class MapRestrictedAreaRenderer
    {
        private readonly List<GameObject> rendered = new();
        private RectTransform root;

        public bool IsBuilt => root != null;

        public void Build(Transform parent)
        {
            if (root != null || parent == null)
                return;

            root = new GameObject(
                "Restricted Area Polygons",
                typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(parent, false);
            Stretch(root);
        }

        public void Refresh(
            int deck,
            bool visible,
            ProductionMapViewModel viewModel,
            GameStateManager state,
            Action<MapAreaShape> onSelect)
        {
            Clear();
            if (root == null || !visible)
                return;

            Dictionary<string, ProductionMapEntry> entries =
                viewModel?.Entries.ToDictionary(
                    entry => entry.Spec.Code,
                    StringComparer.Ordinal) ??
                new Dictionary<string, ProductionMapEntry>(
                    StringComparer.Ordinal);
            foreach (MapAreaShape area in MapAreaCatalog.ForDeck(deck))
            {
                entries.TryGetValue(area.AreaId, out ProductionMapEntry entry);
                bool reveal = MapAreaCatalog.ConditionMet(
                    area.RevealCondition,
                    state?.CompletedProductionSceneIds,
                    state?.UnlockedProductionSceneIds,
                    state == null ? null : state.HasFlag);
                bool access = MapAreaCatalog.ConditionMet(
                    area.AccessCondition,
                    state?.CompletedProductionSceneIds,
                    state?.UnlockedProductionSceneIds,
                    state == null ? null : state.HasFlag);
                bool entryAccessible =
                    entry != null &&
                    entry.Status != ProductionMapEntryStatus.Locked;
                MapAreaVisualState visualState =
                    MapAreaCatalog.ResolveState(
                        area,
                        reveal,
                        access,
                        entryAccessible,
                        state?.HasFlag("restricted_areas_closed") == true);
                if (visualState is MapAreaVisualState.Hidden or
                    MapAreaVisualState.Accessible)
                {
                    continue;
                }

                CreateArea(area, visualState, onSelect);
            }
        }

        private void CreateArea(
            MapAreaShape area,
            MapAreaVisualState visualState,
            Action<MapAreaShape> onSelect)
        {
            GameObject areaObject = new(
                $"Restricted Area {area.AreaId}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(MapAreaPolygonGraphic),
                typeof(MapAreaPointerHandler));
            areaObject.transform.SetParent(root, false);
            rendered.Add(areaObject);
            RectTransform rect = areaObject.GetComponent<RectTransform>();
            Stretch(rect);

            MapAreaPolygonGraphic graphic =
                areaObject.GetComponent<MapAreaPolygonGraphic>();
            graphic.Configure(area.Polygon, visualState);
            areaObject.GetComponent<MapAreaPointerHandler>().Configure(
                graphic,
                () => onSelect?.Invoke(area));

            CreateLabel(areaObject.transform, area);
            CreateLock(areaObject.transform, area.EntranceAnchor, visualState);
        }

        private static void CreateLabel(
            Transform parent,
            MapAreaShape area)
        {
            GameObject labelObject = new(
                "Area Label",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = area.LabelAnchor;
            rect.anchorMax = area.LabelAnchor;
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(180f, 34f);
            TMP_Text label = labelObject.GetComponent<TMP_Text>();
            label.text = area.DisplayName;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 19f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 11f;
            label.fontSizeMax = 19f;
            label.color = new Color32(242, 220, 170, 235);
            label.raycastTarget = false;
            MapTypography.ApplyLocation(label);
        }

        private static void CreateLock(
            Transform parent,
            Vector2 anchor,
            MapAreaVisualState visualState)
        {
            GameObject lockRoot = new(
                visualState == MapAreaVisualState.TemporarilyClosed
                    ? "Closure Marker"
                    : "Entrance Lock",
                typeof(RectTransform));
            lockRoot.transform.SetParent(parent, false);
            RectTransform rect = lockRoot.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(26f, 30f);
            Color color = visualState == MapAreaVisualState.TemporarilyClosed
                ? new Color32(238, 105, 87, 255)
                : new Color32(230, 190, 105, 255);
            AddBlock(rect, "Body", new Vector2(20f, 15f), new Vector2(0f, -5f), color);
            AddBlock(rect, "Shackle Left", new Vector2(3f, 10f), new Vector2(-6f, 7f), color);
            AddBlock(rect, "Shackle Right", new Vector2(3f, 10f), new Vector2(6f, 7f), color);
            AddBlock(rect, "Shackle Top", new Vector2(15f, 3f), new Vector2(0f, 12f), color);
        }

        private static void AddBlock(
            RectTransform parent,
            string name,
            Vector2 size,
            Vector2 position,
            Color color)
        {
            GameObject block = new(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            block.transform.SetParent(parent, false);
            RectTransform rect = block.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = block.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private void Clear()
        {
            foreach (GameObject item in rendered)
                UnityEngine.Object.Destroy(item);
            rendered.Clear();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
