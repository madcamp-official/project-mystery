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
            GetBounds(
                out float left,
                out float bottom,
                out float right,
                out float top);
            float width = right - left;
            float height = top - bottom;
            if (width <= 0f || height <= 0f)
                return;

            Color32 ink = new(8, 18, 30, 238);
            const int StrokeCount = 5;
            for (int index = 0; index < StrokeCount; index++)
            {
                float t = (index + .5f) / StrokeCount;
                float centerY = Mathf.Lerp(bottom, top, t);
                float halfThickness = height *
                                      (index % 2 == 0 ? .055f : .045f);
                float leftInset = width *
                                  (index % 3 == 0 ? .015f : .035f);
                float rightInset = width *
                                   (index % 2 == 0 ? .025f : .01f);
                float slope = height *
                              (index % 2 == 0 ? .045f : -.035f);

                AddStroke(
                    vh,
                    rect,
                    new Vector2(left + leftInset, centerY - halfThickness),
                    new Vector2(
                        right - rightInset,
                        centerY - halfThickness + slope),
                    new Vector2(
                        right - rightInset,
                        centerY + halfThickness + slope),
                    new Vector2(
                        left + leftInset,
                        centerY + halfThickness),
                    ink);
            }
        }

        private void GetBounds(
            out float left,
            out float bottom,
            out float right,
            out float top)
        {
            left = right = points[0].x;
            bottom = top = points[0].y;
            for (int index = 1; index < points.Count; index++)
            {
                Vector2 point = points[index];
                left = Mathf.Min(left, point.x);
                bottom = Mathf.Min(bottom, point.y);
                right = Mathf.Max(right, point.x);
                top = Mathf.Max(top, point.y);
            }
        }

        private static void AddStroke(
            VertexHelper vh,
            Rect rect,
            Vector2 bottomLeft,
            Vector2 bottomRight,
            Vector2 topRight,
            Vector2 topLeft,
            Color32 color)
        {
            int start = vh.currentVertCount;
            vh.AddVert(ToLocal(bottomLeft, rect), color, Vector2.zero);
            vh.AddVert(ToLocal(bottomRight, rect), color, Vector2.zero);
            vh.AddVert(ToLocal(topRight, rect), color, Vector2.zero);
            vh.AddVert(ToLocal(topLeft, rect), color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
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
