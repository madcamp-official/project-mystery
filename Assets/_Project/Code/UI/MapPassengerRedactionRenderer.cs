using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wake.UI
{
    public sealed class MapPassengerRedactionGraphic : MaskableGraphic
    {
        private readonly List<Vector2> points = new();

        public void Configure(IReadOnlyList<Vector2> normalizedPoints)
        {
            points.Clear();
            if (normalizedPoints != null)
            {
                for (int index = 0; index < normalizedPoints.Count; index++)
                    points.Add(normalizedPoints[index]);
            }
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (points.Count < 3)
                return;

            Rect rect = rectTransform.rect;
            Color32 fill = new(8, 18, 30, 255);
            int start = vh.currentVertCount;
            for (int index = 0; index < points.Count; index++)
            {
                vh.AddVert(
                    ToLocal(points[index], rect),
                    fill,
                    Vector2.zero);
            }

            IReadOnlyList<int> triangles =
                MapPolygonUtility.Triangulate(points);
            for (int index = 0; index + 2 < triangles.Count; index += 3)
            {
                vh.AddTriangle(
                    start + triangles[index],
                    start + triangles[index + 1],
                    start + triangles[index + 2]);
            }
        }

        private static Vector2 ToLocal(Vector2 point, Rect rect) =>
            new(
                Mathf.Lerp(rect.xMin, rect.xMax, point.x),
                Mathf.Lerp(rect.yMin, rect.yMax, point.y));
    }

    public sealed class MapPassengerRedactionRenderer
    {
        private readonly List<GameObject> rendered = new();
        private RectTransform layer;

        public int Count => rendered.Count;

        public void Build(RectTransform parent)
        {
            if (layer != null || parent == null)
                return;

            layer = new GameObject(
                "Passenger Spoiler Redactions",
                typeof(RectTransform)).GetComponent<RectTransform>();
            layer.SetParent(parent, false);
            Stretch(layer);
        }

        public void Refresh(
            int deck,
            MapLayerMode selectedLayer,
            IEnumerable<string> completedSceneIds)
        {
            Clear();
            if (layer == null)
                return;

            foreach (MapPassengerRedaction redaction in
                     MapPassengerRedactionCatalog.ForDeck(deck))
            {
                if (!MapPassengerRedactionCatalog.ShouldRender(
                        redaction,
                        selectedLayer,
                        completedSceneIds))
                {
                    continue;
                }

                GameObject item = new(
                    $"Passenger Redaction {redaction.Id}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(MapPassengerRedactionGraphic));
                item.transform.SetParent(layer, false);
                rendered.Add(item);
                RectTransform rect = item.GetComponent<RectTransform>();
                Stretch(rect);
                item.GetComponent<MapPassengerRedactionGraphic>()
                    .Configure(redaction.Polygon);
            }
        }

        public void Clear()
        {
            foreach (GameObject item in rendered)
            {
                if (item != null)
                    Object.Destroy(item);
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
