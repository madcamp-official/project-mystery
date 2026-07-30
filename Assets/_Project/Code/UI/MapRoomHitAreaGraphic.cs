using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Wake.UI
{
    public sealed class MapRoomHitAreaGraphic :
        MaskableGraphic,
        ICanvasRaycastFilter
    {
        private readonly List<Vector2> points = new();
        private bool hovered;
        private bool locked;
        private bool objective;
        private bool current;

        public IReadOnlyList<Vector2> Points => points;
        public bool IsLocked => locked;

        public void Configure(
            IReadOnlyList<Vector2> normalizedPoints,
            bool isLocked,
            bool isObjective,
            bool isCurrent)
        {
            points.Clear();
            if (normalizedPoints != null)
            {
                for (int index = 0; index < normalizedPoints.Count; index++)
                    points.Add(normalizedPoints[index]);
            }
            locked = isLocked;
            objective = isObjective;
            current = isCurrent;
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

        public bool ContainsNormalized(Vector2 point) =>
            MapPolygonUtility.Contains(points, point);

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
            return ContainsNormalized(normalized);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (points.Count < 3)
                return;

            Color32 accent = current
                ? new Color32(246, 236, 207, 255)
                : objective
                    ? new Color32(255, 205, 84, 255)
                    : locked
                        ? new Color32(126, 136, 146, 220)
                        : new Color32(216, 177, 97, 225);
            Color32 fill = locked
                ? new Color32(
                    3,
                    10,
                    17,
                    hovered ? (byte)225 : (byte)215)
                : new Color32(
                    accent.r,
                    accent.g,
                    accent.b,
                    hovered ? (byte)42 : (byte)0);
            Color32 line = new(
                accent.r,
                accent.g,
                accent.b,
                hovered ? (byte)220 : (byte)0);
            Rect rect = rectTransform.rect;
            AddFill(vh, rect, fill);
            if (hovered)
                AddOutline(vh, rect, line, 2.5f);
        }

        private void AddFill(VertexHelper vh, Rect rect, Color32 color)
        {
            int start = vh.currentVertCount;
            for (int index = 0; index < points.Count; index++)
                vh.AddVert(ToLocal(points[index], rect), color, Vector2.zero);

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

        private void AddOutline(
            VertexHelper vh,
            Rect rect,
            Color32 color,
            float thickness)
        {
            for (int index = 0; index < points.Count; index++)
            {
                AddLine(
                    vh,
                    ToLocal(points[index], rect),
                    ToLocal(points[(index + 1) % points.Count], rect),
                    thickness,
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
    }

    public sealed class MapRoomHitAreaPointerHandler :
        MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler
    {
        private MapRoomHitAreaGraphic graphic;
        private Action click;

        public void Configure(
            MapRoomHitAreaGraphic hitAreaGraphic,
            Action clickAction)
        {
            graphic = hitAreaGraphic;
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
