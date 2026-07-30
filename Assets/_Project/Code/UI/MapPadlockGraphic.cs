using UnityEngine;
using UnityEngine.UI;

namespace Wake.UI
{
    public sealed class MapPadlockGraphic : MaskableGraphic
    {
        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            color = new Color32(226, 185, 96, 255);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            Color32 gold = color;
            Color32 shade = new(
                (byte)Mathf.RoundToInt(gold.r * .58f),
                (byte)Mathf.RoundToInt(gold.g * .58f),
                (byte)Mathf.RoundToInt(gold.b * .58f),
                gold.a);
            Color32 keyhole = new(9, 17, 27, 255);

            AddLine(vh, rect, new Vector2(.29f, .58f),
                new Vector2(.29f, .74f), .11f, shade);
            AddLine(vh, rect, new Vector2(.29f, .74f),
                new Vector2(.36f, .87f), .11f, shade);
            AddLine(vh, rect, new Vector2(.36f, .87f),
                new Vector2(.50f, .92f), .11f, shade);
            AddLine(vh, rect, new Vector2(.50f, .92f),
                new Vector2(.64f, .87f), .11f, shade);
            AddLine(vh, rect, new Vector2(.64f, .87f),
                new Vector2(.71f, .74f), .11f, shade);
            AddLine(vh, rect, new Vector2(.71f, .74f),
                new Vector2(.71f, .58f), .11f, shade);

            AddQuad(vh, rect, .18f, .14f, .82f, .60f, shade);
            AddQuad(vh, rect, .22f, .19f, .78f, .56f, gold);
            AddDiamond(vh, rect, new Vector2(.50f, .40f), .08f, keyhole);
            AddQuad(vh, rect, .465f, .20f, .535f, .40f, keyhole);
        }

        private static void AddLine(
            VertexHelper vh,
            Rect rect,
            Vector2 from,
            Vector2 to,
            float normalizedThickness,
            Color32 color)
        {
            Vector2 a = ToLocal(from, rect);
            Vector2 b = ToLocal(to, rect);
            Vector2 direction = (b - a).normalized;
            Vector2 normal = new(-direction.y, direction.x);
            float thickness =
                Mathf.Min(rect.width, rect.height) * normalizedThickness;
            Vector2 offset = normal * (thickness * .5f);
            int start = vh.currentVertCount;
            vh.AddVert(a - offset, color, Vector2.zero);
            vh.AddVert(a + offset, color, Vector2.zero);
            vh.AddVert(b + offset, color, Vector2.zero);
            vh.AddVert(b - offset, color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        private static void AddQuad(
            VertexHelper vh,
            Rect rect,
            float left,
            float bottom,
            float right,
            float top,
            Color32 color)
        {
            int start = vh.currentVertCount;
            vh.AddVert(ToLocal(new Vector2(left, bottom), rect),
                color, Vector2.zero);
            vh.AddVert(ToLocal(new Vector2(left, top), rect),
                color, Vector2.zero);
            vh.AddVert(ToLocal(new Vector2(right, top), rect),
                color, Vector2.zero);
            vh.AddVert(ToLocal(new Vector2(right, bottom), rect),
                color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        private static void AddDiamond(
            VertexHelper vh,
            Rect rect,
            Vector2 center,
            float radius,
            Color32 color)
        {
            int start = vh.currentVertCount;
            vh.AddVert(ToLocal(
                    center + new Vector2(0f, radius),
                    rect),
                color, Vector2.zero);
            vh.AddVert(ToLocal(
                    center + new Vector2(radius, 0f),
                    rect),
                color, Vector2.zero);
            vh.AddVert(ToLocal(
                    center + new Vector2(0f, -radius),
                    rect),
                color, Vector2.zero);
            vh.AddVert(ToLocal(
                    center + new Vector2(-radius, 0f),
                    rect),
                color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        private static Vector2 ToLocal(Vector2 point, Rect rect) =>
            new(
                Mathf.Lerp(rect.xMin, rect.xMax, point.x),
                Mathf.Lerp(rect.yMin, rect.yMax, point.y));
    }
}
