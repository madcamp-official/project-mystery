using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wake.UI
{
    public static class MapPolygonUtility
    {
        private const float Epsilon = .000001f;

        public static bool IsNormalized(Vector2 point) =>
            float.IsFinite(point.x) &&
            float.IsFinite(point.y) &&
            point.x >= 0f && point.x <= 1f &&
            point.y >= 0f && point.y <= 1f;

        public static float SignedArea(IReadOnlyList<Vector2> polygon)
        {
            if (polygon == null || polygon.Count < 3)
                return 0f;

            float area = 0f;
            for (int index = 0; index < polygon.Count; index++)
            {
                Vector2 current = polygon[index];
                Vector2 next = polygon[(index + 1) % polygon.Count];
                area += current.x * next.y - next.x * current.y;
            }
            return area * .5f;
        }

        public static bool Contains(
            IReadOnlyList<Vector2> polygon,
            Vector2 point,
            bool includeBoundary = true)
        {
            if (polygon == null || polygon.Count < 3)
                return false;

            bool inside = false;
            for (int index = 0, previous = polygon.Count - 1;
                 index < polygon.Count;
                 previous = index++)
            {
                Vector2 a = polygon[previous];
                Vector2 b = polygon[index];
                if (PointOnSegment(point, a, b))
                    return includeBoundary;
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

        public static bool SelfIntersects(IReadOnlyList<Vector2> polygon)
        {
            if (polygon == null || polygon.Count < 4)
                return false;

            for (int first = 0; first < polygon.Count; first++)
            {
                int firstNext = (first + 1) % polygon.Count;
                for (int second = first + 1;
                     second < polygon.Count;
                     second++)
                {
                    int secondNext = (second + 1) % polygon.Count;
                    if (first == second ||
                        firstNext == second ||
                        secondNext == first)
                    {
                        continue;
                    }
                    if (SegmentsIntersect(
                            polygon[first],
                            polygon[firstNext],
                            polygon[second],
                            polygon[secondNext]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool OverlapsInterior(
            IReadOnlyList<Vector2> first,
            IReadOnlyList<Vector2> second)
        {
            if (first == null || second == null ||
                first.Count < 3 || second.Count < 3)
            {
                return false;
            }

            for (int index = 0; index < first.Count; index++)
            {
                if (Contains(second, first[index], false))
                    return true;
            }
            for (int index = 0; index < second.Count; index++)
            {
                if (Contains(first, second[index], false))
                    return true;
            }
            for (int firstIndex = 0;
                 firstIndex < first.Count;
                 firstIndex++)
            {
                Vector2 firstA = first[firstIndex];
                Vector2 firstB = first[(firstIndex + 1) % first.Count];
                for (int secondIndex = 0;
                     secondIndex < second.Count;
                     secondIndex++)
                {
                    Vector2 secondA = second[secondIndex];
                    Vector2 secondB =
                        second[(secondIndex + 1) % second.Count];
                    if (SegmentsIntersect(
                            firstA,
                            firstB,
                            secondA,
                            secondB))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool SegmentsIntersect(
            Vector2 a,
            Vector2 b,
            Vector2 c,
            Vector2 d)
        {
            float abC = Cross(b - a, c - a);
            float abD = Cross(b - a, d - a);
            float cdA = Cross(d - c, a - c);
            float cdB = Cross(d - c, b - c);
            return abC * abD < 0f && cdA * cdB < 0f;
        }

        private static bool PointOnSegment(
            Vector2 point,
            Vector2 a,
            Vector2 b)
        {
            if (Mathf.Abs(Cross(b - a, point - a)) > Epsilon)
                return false;
            return point.x >= Mathf.Min(a.x, b.x) - Epsilon &&
                   point.x <= Mathf.Max(a.x, b.x) + Epsilon &&
                   point.y >= Mathf.Min(a.y, b.y) - Epsilon &&
                   point.y <= Mathf.Max(a.y, b.y) + Epsilon;
        }

        private static float Cross(Vector2 a, Vector2 b) =>
            a.x * b.y - a.y * b.x;
    }
}
