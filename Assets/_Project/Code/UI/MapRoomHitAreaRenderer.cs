using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wake.UI
{
    public sealed class MapRoomHitAreaRenderer
    {
        private readonly List<GameObject> rendered = new();
        private RectTransform layer;

        public int Count => rendered.Count;

        public void Build(RectTransform parent)
        {
            if (layer != null || parent == null)
                return;

            layer = new GameObject(
                "Map Room Hit Areas",
                typeof(RectTransform)).GetComponent<RectTransform>();
            layer.SetParent(parent, false);
            Stretch(layer);
        }

        public bool Add(
            string locationCode,
            bool locked,
            bool objective,
            bool current,
            Action onClick)
        {
            if (layer == null ||
                !MapInteractionGeometryCatalog.TryGetMask(
                    locationCode,
                    out MapRoomMask mask))
            {
                return false;
            }

            GameObject areaObject = new(
                $"Room Hit Area {locationCode}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(MapRoomHitAreaGraphic),
                typeof(MapRoomHitAreaPointerHandler));
            areaObject.transform.SetParent(layer, false);
            rendered.Add(areaObject);
            RectTransform rect = areaObject.GetComponent<RectTransform>();
            Stretch(rect);

            MapRoomHitAreaGraphic graphic =
                areaObject.GetComponent<MapRoomHitAreaGraphic>();
            graphic.Configure(mask.Polygon, locked, objective, current);
            areaObject.GetComponent<MapRoomHitAreaPointerHandler>()
                .Configure(graphic, onClick);
            return true;
        }

        public void Clear()
        {
            foreach (GameObject item in rendered)
            {
                if (item != null)
                    UnityEngine.Object.Destroy(item);
            }
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
