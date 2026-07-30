using System.Collections.Generic;
using UnityEngine;

namespace Wake.Exploration
{
    [DisallowMultipleComponent]
    public sealed class PolygonHotspotRaycastFilter :
        MonoBehaviour,
        ICanvasRaycastFilter
    {
        private readonly List<Vector2> points = new();

        public IReadOnlyList<Vector2> Points => points;
        public bool HasValidPolygon => points.Count >= 3;

        public void Configure(IReadOnlyList<Vector2> normalizedPoints)
        {
            points.Clear();
            if (normalizedPoints == null)
                return;

            for (int index = 0; index < normalizedPoints.Count; index++)
                points.Add(normalizedPoints[index]);
        }

        public bool ContainsNormalized(Vector2 point) =>
            HasValidPolygon &&
            BackgroundInteractionPolygonUtility.Contains(points, point);

        public bool IsRaycastLocationValid(
            Vector2 screenPoint,
            Camera eventCamera)
        {
            RectTransform rectTransform = transform as RectTransform;
            if (rectTransform == null ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform,
                    screenPoint,
                    eventCamera,
                    out Vector2 localPoint))
            {
                return false;
            }

            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f)
                return false;

            Vector2 normalized = new(
                (localPoint.x - rect.xMin) / rect.width,
                (localPoint.y - rect.yMin) / rect.height);
            if (!BackgroundInteractionPolygonUtility.IsNormalized(normalized))
                return false;
            return ContainsNormalized(normalized);
        }
    }
}
