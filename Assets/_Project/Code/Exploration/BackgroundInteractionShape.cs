using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
#endif

namespace Wake.Exploration
{
    public enum BackgroundInteractionShapeKind
    {
        Polygon
    }

    [Serializable]
    public sealed class BackgroundInteractionShape
    {
        private const float BoundsEpsilon = .00001f;

        [SerializeField] private string objectId = string.Empty;
        [SerializeField] private string locationCode = string.Empty;
        [SerializeField] private string backgroundVariantKey = string.Empty;
        [SerializeField] private string sourceImageHash = string.Empty;
        [SerializeField] private bool isPresent = true;
        [SerializeField] private BackgroundInteractionShapeKind shapeKind;
        [SerializeField] private Rect normalizedBounds;
        [SerializeField] private Vector2[] normalizedPolygon =
            Array.Empty<Vector2>();
        [SerializeField] private Vector2 labelAnchor = new(.5f, .5f);

        private Vector2[] localPolygon = Array.Empty<Vector2>();

        public BackgroundInteractionShape(
            string authoredObjectId,
            string authoredLocationCode,
            string authoredBackgroundVariantKey,
            string authoredSourceImageHash,
            bool authoredIsPresent,
            Rect authoredNormalizedBounds,
            IEnumerable<Vector2> authoredNormalizedPolygon,
            Vector2 authoredLabelAnchor)
        {
            objectId = NormalizeId(authoredObjectId);
            locationCode = NormalizeId(authoredLocationCode);
            backgroundVariantKey = NormalizeVariantKey(
                authoredBackgroundVariantKey);
            sourceImageHash =
                authoredSourceImageHash?.Trim().ToLowerInvariant() ??
                string.Empty;
            isPresent = authoredIsPresent;
            shapeKind = BackgroundInteractionShapeKind.Polygon;
            normalizedBounds = authoredNormalizedBounds;
            normalizedPolygon = authoredNormalizedPolygon?.ToArray() ??
                                Array.Empty<Vector2>();
            labelAnchor = authoredLabelAnchor;
            RebuildLocalPolygon();
        }

        public string ObjectId => objectId;
        public string LocationCode => locationCode;
        public string BackgroundVariantKey => backgroundVariantKey;
        public string SourceImageHash => sourceImageHash;
        public bool IsPresent => isPresent;
        public BackgroundInteractionShapeKind ShapeKind => shapeKind;
        public Rect NormalizedBounds => normalizedBounds;
        public IReadOnlyList<Vector2> NormalizedPolygon =>
            normalizedPolygon;
        public IReadOnlyList<Vector2> LocalPolygon
        {
            get
            {
                EnsureLocalPolygon();
                return localPolygon;
            }
        }

        public Vector2 LabelAnchor => labelAnchor;
        public Vector2 LocalLabelAnchor =>
            ToBoundsLocal(labelAnchor, normalizedBounds);

        public bool ContainsBackgroundPoint(Vector2 point) =>
            isPresent &&
            BackgroundInteractionPolygonUtility.Contains(
                normalizedPolygon,
                point);

        public bool Validate(out string diagnostic)
        {
            if (string.IsNullOrWhiteSpace(objectId))
                return Invalid("object id is empty.", out diagnostic);
            if (string.IsNullOrWhiteSpace(locationCode))
                return Invalid("location code is empty.", out diagnostic);
            if (string.IsNullOrWhiteSpace(backgroundVariantKey))
                return Invalid("background variant key is empty.", out diagnostic);
            if (!IsSha256(sourceImageHash))
                return Invalid("source image hash is not SHA-256.", out diagnostic);

            if (!isPresent)
            {
                diagnostic = string.Empty;
                return true;
            }

            if (!IsNormalizedBounds(normalizedBounds))
                return Invalid("normalized bounds are invalid.", out diagnostic);
            if (normalizedPolygon == null || normalizedPolygon.Length < 3)
                return Invalid("polygon has fewer than three points.", out diagnostic);
            if (!BackgroundInteractionPolygonUtility.IsNormalized(
                    labelAnchor))
            {
                return Invalid(
                    "label anchor is outside normalized background space.",
                    out diagnostic);
            }

            for (int index = 0; index < normalizedPolygon.Length; index++)
            {
                Vector2 point = normalizedPolygon[index];
                if (!BackgroundInteractionPolygonUtility.IsNormalized(point))
                {
                    return Invalid(
                        $"polygon point {index} is not normalized.",
                        out diagnostic);
                }
                if (!ContainsWithEpsilon(normalizedBounds, point))
                {
                    return Invalid(
                        $"polygon point {index} is outside its bounds.",
                        out diagnostic);
                }
            }

            if (Mathf.Abs(
                    BackgroundInteractionPolygonUtility.SignedArea(
                        normalizedPolygon)) < .000001f)
            {
                return Invalid("polygon area is too small.", out diagnostic);
            }
            if (BackgroundInteractionPolygonUtility.SelfIntersects(
                    normalizedPolygon))
            {
                return Invalid("polygon self-intersects.", out diagnostic);
            }

            diagnostic = string.Empty;
            return true;
        }

        internal static string NormalizeVariantKey(string value)
        {
            string normalized = value?.Trim().Replace('\\', '/') ??
                                string.Empty;
            if (normalized.StartsWith(
                    LocationBackgroundVariantCatalog.SerializedVariantPrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(
                    LocationBackgroundVariantCatalog
                        .SerializedVariantPrefix.Length);
            }

            int slash = normalized.LastIndexOf('/');
            if (slash >= 0 && slash < normalized.Length - 1)
                normalized = normalized.Substring(slash + 1);
            if (normalized.EndsWith(
                    ".png",
                    StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(
                    0,
                    normalized.Length - 4);
            }
            return normalized.ToLowerInvariant();
        }

        private static string NormalizeId(string value) =>
            value?.Trim().ToUpperInvariant() ?? string.Empty;

        private void EnsureLocalPolygon()
        {
            if (localPolygon == null ||
                localPolygon.Length != normalizedPolygon.Length)
            {
                RebuildLocalPolygon();
            }
        }

        private void RebuildLocalPolygon()
        {
            if (!isPresent ||
                normalizedPolygon == null ||
                normalizedBounds.width <= 0f ||
                normalizedBounds.height <= 0f)
            {
                localPolygon = Array.Empty<Vector2>();
                return;
            }

            localPolygon = normalizedPolygon
                .Select(point => ToBoundsLocal(point, normalizedBounds))
                .ToArray();
        }

        private static Vector2 ToBoundsLocal(Vector2 point, Rect bounds) =>
            bounds.width > 0f && bounds.height > 0f
                ? new Vector2(
                    (point.x - bounds.xMin) / bounds.width,
                    (point.y - bounds.yMin) / bounds.height)
                : new Vector2(.5f, .5f);

        private static bool IsNormalizedBounds(Rect bounds) =>
            float.IsFinite(bounds.x) &&
            float.IsFinite(bounds.y) &&
            float.IsFinite(bounds.width) &&
            float.IsFinite(bounds.height) &&
            bounds.width > 0f &&
            bounds.height > 0f &&
            bounds.xMin >= 0f &&
            bounds.yMin >= 0f &&
            bounds.xMax <= 1f &&
            bounds.yMax <= 1f;

        private static bool ContainsWithEpsilon(Rect bounds, Vector2 point) =>
            point.x >= bounds.xMin - BoundsEpsilon &&
            point.x <= bounds.xMax + BoundsEpsilon &&
            point.y >= bounds.yMin - BoundsEpsilon &&
            point.y <= bounds.yMax + BoundsEpsilon;

        private static bool IsSha256(string value) =>
            value?.Length == 64 &&
            value.All(character =>
                character >= '0' && character <= '9' ||
                character >= 'a' && character <= 'f');

        private static bool Invalid(
            string message,
            out string diagnostic)
        {
            diagnostic = message;
            return false;
        }
    }
}
