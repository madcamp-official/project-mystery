using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
                if (entry != null)
                {
                    // Canonical rooms already use the authored room mask.
                    // Rendering the legacy area again would double the dimming
                    // and create a second rectangular label/lock.
                    continue;
                }

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

            CreateLock(areaObject.transform, area.EntranceAnchor, visualState);
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
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(MapPadlockGraphic));
            lockRoot.transform.SetParent(parent, false);
            RectTransform rect = lockRoot.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(38f, 46f);
            MapPadlockGraphic graphic =
                lockRoot.GetComponent<MapPadlockGraphic>();
            graphic.color = visualState ==
                            MapAreaVisualState.TemporarilyClosed
                ? new Color32(238, 105, 87, 255)
                : new Color32(230, 190, 105, 255);
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
