using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Wake.UI
{
    public sealed class MapAreaPolygonGraphic :
        MaskableGraphic,
        ICanvasRaycastFilter
    {
        private readonly List<Vector2> points = new();
        private MapAreaVisualState state;
        private bool hovered;

        public void Configure(
            IReadOnlyList<Vector2> normalizedPoints,
            MapAreaVisualState visualState)
        {
            points.Clear();
            if (normalizedPoints != null)
            {
                for (int index = 0; index < normalizedPoints.Count; index++)
                    points.Add(normalizedPoints[index]);
            }
            state = visualState;
            raycastTarget = true;
            SetVerticesDirty();
        }

        public void SetHovered(bool value)
        {
            if (hovered == value)
                return;
            hovered = value;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (points.Count < 3)
                return;

            Rect rect = rectTransform.rect;
            Color32 fill = state == MapAreaVisualState.TemporarilyClosed
                ? new Color32(150, 60, 54, hovered ? (byte)92 : (byte)70)
                : new Color32(92, 111, 128, hovered ? (byte)78 : (byte)54);
            Color32 line = state == MapAreaVisualState.TemporarilyClosed
                ? new Color32(238, 105, 87, 235)
                : new Color32(216, 177, 97, hovered ? (byte)255 : (byte)220);

            AddFill(vh, rect, fill);
            AddOutline(vh, rect, line, hovered ? 3.5f : 2.5f);
            AddHatching(vh, rect, new Color32(line.r, line.g, line.b, 82));
        }

        public bool IsRaycastLocationValid(
            Vector2 screenPoint,
            Camera eventCamera)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform,
                    screenPoint,
                    eventCamera,
                    out Vector2 local))
            {
                return false;
            }

            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f)
                return false;
            Vector2 normalized = new(
                Mathf.InverseLerp(rect.xMin, rect.xMax, local.x),
                Mathf.InverseLerp(rect.yMin, rect.yMax, local.y));
            return Contains(normalized);
        }

        private void AddFill(VertexHelper vh, Rect rect, Color32 color)
        {
            int start = vh.currentVertCount;
            for (int index = 0; index < points.Count; index++)
                vh.AddVert(ToLocal(points[index], rect), color, Vector2.zero);

            foreach (int[] triangle in Triangulate(points))
            {
                vh.AddTriangle(
                    start + triangle[0],
                    start + triangle[1],
                    start + triangle[2]);
            }
        }

        private void AddOutline(
            VertexHelper vh,
            Rect rect,
            Color32 color,
            float thickness)
        {
            for (int index = 0; index < points.Count; index++)
            {
                Vector2 a = ToLocal(points[index], rect);
                Vector2 b = ToLocal(points[(index + 1) % points.Count], rect);
                AddLine(vh, a, b, thickness, color);
            }
        }

        private void AddHatching(
            VertexHelper vh,
            Rect rect,
            Color32 color)
        {
            float spacing = 12f / Mathf.Max(1f, Mathf.Min(rect.width, rect.height));
            for (float diagonal = -1f; diagonal <= 1f; diagonal += spacing)
            {
                var intersections = new List<Vector2>();
                Vector2 origin = new(diagonal, 0f);
                Vector2 direction = new(1f, 1f);
                for (int index = 0; index < points.Count; index++)
                {
                    if (TryLineSegmentIntersection(
                            origin,
                            direction,
                            points[index],
                            points[(index + 1) % points.Count],
                            out Vector2 intersection))
                    {
                        intersections.Add(intersection);
                    }
                }

                if (intersections.Count < 2)
                    continue;
                float bestDistance = -1f;
                Vector2 bestA = default;
                Vector2 bestB = default;
                for (int a = 0; a < intersections.Count; a++)
                {
                    for (int b = a + 1; b < intersections.Count; b++)
                    {
                        float distance =
                            (intersections[a] - intersections[b]).sqrMagnitude;
                        if (distance <= bestDistance)
                            continue;
                        bestDistance = distance;
                        bestA = intersections[a];
                        bestB = intersections[b];
                    }
                }
                AddLine(
                    vh,
                    ToLocal(bestA, rect),
                    ToLocal(bestB, rect),
                    1f,
                    color);
            }
        }

        private static void AddLine(
            VertexHelper vh,
            Vector2 a,
            Vector2 b,
            float thickness,
            Color32 color)
        {
            Vector2 direction = (b - a).normalized;
            Vector2 normal = new(-direction.y, direction.x);
            Vector2 offset = normal * (thickness * .5f);
            int start = vh.currentVertCount;
            vh.AddVert(a - offset, color, Vector2.zero);
            vh.AddVert(a + offset, color, Vector2.zero);
            vh.AddVert(b + offset, color, Vector2.zero);
            vh.AddVert(b - offset, color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        private static Vector2 ToLocal(Vector2 point, Rect rect) =>
            new(
                Mathf.Lerp(rect.xMin, rect.xMax, point.x),
                Mathf.Lerp(rect.yMin, rect.yMax, point.y));

        private bool Contains(Vector2 point)
        {
            bool inside = false;
            for (int index = 0, previous = points.Count - 1;
                 index < points.Count;
                 previous = index++)
            {
                Vector2 a = points[index];
                Vector2 b = points[previous];
                if ((a.y > point.y) == (b.y > point.y))
                    continue;
                float edgeX =
                    (b.x - a.x) * (point.y - a.y) /
                    (b.y - a.y) + a.x;
                if (point.x < edgeX)
                    inside = !inside;
            }
            return inside;
        }

        private static IEnumerable<int[]> Triangulate(
            IReadOnlyList<Vector2> polygon)
        {
            var remaining = new List<int>();
            for (int index = 0; index < polygon.Count; index++)
                remaining.Add(index);

            bool counterClockwise = SignedArea(polygon) > 0f;
            int safety = polygon.Count * polygon.Count;
            while (remaining.Count > 3 && safety-- > 0)
            {
                bool clipped = false;
                for (int index = 0; index < remaining.Count; index++)
                {
                    int previous = remaining[
                        (index - 1 + remaining.Count) % remaining.Count];
                    int current = remaining[index];
                    int next = remaining[(index + 1) % remaining.Count];
                    float corner = Cross(
                        polygon[current] - polygon[previous],
                        polygon[next] - polygon[current]);
                    if (counterClockwise ? corner <= 0f : corner >= 0f)
                        continue;

                    bool containsVertex = false;
                    for (int test = 0; test < remaining.Count; test++)
                    {
                        int candidate = remaining[test];
                        if (candidate == previous ||
                            candidate == current ||
                            candidate == next)
                        {
                            continue;
                        }
                        if (PointInTriangle(
                                polygon[candidate],
                                polygon[previous],
                                polygon[current],
                                polygon[next]))
                        {
                            containsVertex = true;
                            break;
                        }
                    }
                    if (containsVertex)
                        continue;

                    yield return new[] { previous, current, next };
                    remaining.RemoveAt(index);
                    clipped = true;
                    break;
                }

                if (!clipped)
                    break;
            }

            if (remaining.Count == 3)
                yield return remaining.ToArray();
        }

        private static float SignedArea(IReadOnlyList<Vector2> polygon)
        {
            float area = 0f;
            for (int index = 0; index < polygon.Count; index++)
            {
                Vector2 current = polygon[index];
                Vector2 next = polygon[(index + 1) % polygon.Count];
                area += current.x * next.y - next.x * current.y;
            }
            return area * .5f;
        }

        private static bool PointInTriangle(
            Vector2 point,
            Vector2 a,
            Vector2 b,
            Vector2 c)
        {
            float first = Cross(b - a, point - a);
            float second = Cross(c - b, point - b);
            float third = Cross(a - c, point - c);
            bool hasNegative = first < 0f || second < 0f || third < 0f;
            bool hasPositive = first > 0f || second > 0f || third > 0f;
            return !(hasNegative && hasPositive);
        }

        private static bool TryLineSegmentIntersection(
            Vector2 lineOrigin,
            Vector2 lineDirection,
            Vector2 segmentA,
            Vector2 segmentB,
            out Vector2 intersection)
        {
            Vector2 segmentDirection = segmentB - segmentA;
            float denominator =
                Cross(lineDirection, segmentDirection);
            if (Mathf.Abs(denominator) < .000001f)
            {
                intersection = default;
                return false;
            }

            float lineT =
                Cross(segmentA - lineOrigin, segmentDirection) / denominator;
            float segmentT =
                Cross(segmentA - lineOrigin, lineDirection) / denominator;
            intersection = lineOrigin + lineDirection * lineT;
            return segmentT >= 0f && segmentT <= 1f;
        }

        private static float Cross(Vector2 a, Vector2 b) =>
            a.x * b.y - a.y * b.x;
    }

    public sealed class MapAreaPointerHandler :
        MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler
    {
        private MapAreaPolygonGraphic graphic;
        private System.Action click;

        public void Configure(
            MapAreaPolygonGraphic polygonGraphic,
            System.Action clickAction)
        {
            graphic = polygonGraphic;
            click = clickAction;
        }

        public void OnPointerEnter(PointerEventData eventData) =>
            graphic?.SetHovered(true);

        public void OnPointerExit(PointerEventData eventData) =>
            graphic?.SetHovered(false);

        public void OnPointerClick(PointerEventData eventData) =>
            click?.Invoke();
    }
}
