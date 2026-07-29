using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wake.Exploration
{
    public enum BackgroundSemanticProfileState
    {
        Draft,
        NeedsReview,
        Approved,
        Rejected,
        Unused
    }

    public enum BackgroundSemanticConfidenceLevel
    {
        Unknown,
        Low,
        Medium,
        High,
        Verified
    }

    public enum BackgroundSemanticZoneKind
    {
        Forbidden,
        Protected,
        Uncertain
    }

    public enum BackgroundSemanticFacing
    {
        Automatic,
        Left,
        Right
    }

    public enum BackgroundSemanticSlotOrigin
    {
        Authored,
        Generated
    }

    [Flags]
    public enum BackgroundSemanticSlotRole
    {
        None = 0,
        Ambient = 1 << 0,
        Main = 1 << 1,
        Focus = 1 << 2,
        Any = Ambient | Main | Focus
    }

    [Serializable]
    public sealed class BackgroundSemanticConfidence
    {
        [SerializeField, Range(0f, 1f)] private float score;
        [SerializeField] private string source = string.Empty;
        [SerializeField] private bool manuallyVerified;

        public BackgroundSemanticConfidence()
        {
        }

        public BackgroundSemanticConfidence(
            float score,
            string source,
            bool manuallyVerified = false)
        {
            this.score = score;
            this.source = source ?? string.Empty;
            this.manuallyVerified = manuallyVerified;
        }

        public float Score => score;
        public string Source => source ?? string.Empty;
        public bool ManuallyVerified => manuallyVerified;
        public BackgroundSemanticConfidenceLevel Level
        {
            get
            {
                if (manuallyVerified)
                    return BackgroundSemanticConfidenceLevel.Verified;
                if (score <= 0f)
                    return BackgroundSemanticConfidenceLevel.Unknown;
                if (score < .35f)
                    return BackgroundSemanticConfidenceLevel.Low;
                if (score < .70f)
                    return BackgroundSemanticConfidenceLevel.Medium;
                if (score < .90f)
                    return BackgroundSemanticConfidenceLevel.High;
                return BackgroundSemanticConfidenceLevel.Verified;
            }
        }
    }

    [Serializable]
    public sealed class BackgroundSemanticStatus
    {
        [SerializeField] private BackgroundSemanticProfileState state =
            BackgroundSemanticProfileState.Draft;
        [SerializeField] private string note = string.Empty;
        [SerializeField] private string reviewer = string.Empty;
        [SerializeField, Min(0)] private int revision;

        public BackgroundSemanticStatus()
        {
        }

        public BackgroundSemanticStatus(
            BackgroundSemanticProfileState state,
            string note = "",
            string reviewer = "",
            int revision = 0)
        {
            this.state = state;
            this.note = note ?? string.Empty;
            this.reviewer = reviewer ?? string.Empty;
            this.revision = revision;
        }

        public BackgroundSemanticProfileState State => state;
        public string Note => note ?? string.Empty;
        public string Reviewer => reviewer ?? string.Empty;
        public int Revision => revision;
    }

    [Serializable]
    public sealed class BackgroundSemanticPolygon
    {
        [SerializeField] private List<Vector2> vertices = new();

        public BackgroundSemanticPolygon()
        {
        }

        public BackgroundSemanticPolygon(IEnumerable<Vector2> vertices)
        {
            this.vertices = vertices != null
                ? new List<Vector2>(vertices)
                : new List<Vector2>();
        }

        public IReadOnlyList<Vector2> Vertices =>
            vertices ??= new List<Vector2>();

        public Rect Bounds
        {
            get
            {
                IReadOnlyList<Vector2> points = Vertices;
                if (points.Count == 0)
                    return default;

                Vector2 minimum = points[0];
                Vector2 maximum = points[0];
                for (int index = 1; index < points.Count; index++)
                {
                    minimum = Vector2.Min(minimum, points[index]);
                    maximum = Vector2.Max(maximum, points[index]);
                }

                return Rect.MinMaxRect(
                    minimum.x,
                    minimum.y,
                    maximum.x,
                    maximum.y);
            }
        }

        public float SignedArea
        {
            get
            {
                IReadOnlyList<Vector2> points = Vertices;
                if (points.Count < 3)
                    return 0f;

                double twiceArea = 0d;
                for (int index = 0; index < points.Count; index++)
                {
                    Vector2 current = points[index];
                    Vector2 next = points[(index + 1) % points.Count];
                    twiceArea +=
                        (double)current.x * next.y -
                        (double)next.x * current.y;
                }

                return (float)(twiceArea * .5d);
            }
        }

        public bool Contains(
            Vector2 point,
            bool includeBoundary = true,
            float epsilon = .00001f)
        {
            IReadOnlyList<Vector2> points = Vertices;
            if (points.Count < 3)
                return false;

            float boundaryDistance = DistanceToBoundary(point);
            if (boundaryDistance <= Mathf.Max(0f, epsilon))
                return includeBoundary;

            bool inside = false;
            for (int index = 0, previous = points.Count - 1;
                 index < points.Count;
                 previous = index++)
            {
                Vector2 first = points[index];
                Vector2 second = points[previous];
                bool crossesY =
                    first.y > point.y != second.y > point.y;
                if (!crossesY)
                    continue;

                float intersectionX =
                    (second.x - first.x) *
                    (point.y - first.y) /
                    (second.y - first.y) +
                    first.x;
                if (point.x < intersectionX)
                    inside = !inside;
            }

            return inside;
        }

        public float DistanceToBoundary(Vector2 point)
        {
            IReadOnlyList<Vector2> points = Vertices;
            if (points.Count == 0)
                return 0f;
            if (points.Count == 1)
                return Vector2.Distance(point, points[0]);

            float minimum = float.PositiveInfinity;
            for (int index = 0; index < points.Count; index++)
            {
                Vector2 first = points[index];
                Vector2 second = points[(index + 1) % points.Count];
                minimum = Mathf.Min(
                    minimum,
                    DistanceToSegment(point, first, second));
            }

            return minimum;
        }

        private static float DistanceToSegment(
            Vector2 point,
            Vector2 first,
            Vector2 second)
        {
            Vector2 segment = second - first;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= Mathf.Epsilon)
                return Vector2.Distance(point, first);

            float t = Mathf.Clamp01(
                Vector2.Dot(point - first, segment) /
                lengthSquared);
            return Vector2.Distance(point, first + segment * t);
        }
    }

    [Serializable]
    public sealed class BackgroundSemanticZone
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private BackgroundSemanticZoneKind kind;
        [SerializeField] private Rect normalizedRect;
        [SerializeField, Min(0f)] private float clearance;
        [SerializeField] private bool enabled = true;
        [SerializeField] private BackgroundSemanticConfidence confidence =
            new();

        public BackgroundSemanticZone()
        {
        }

        public BackgroundSemanticZone(
            string id,
            BackgroundSemanticZoneKind kind,
            Rect normalizedRect,
            float clearance = 0f,
            bool enabled = true,
            BackgroundSemanticConfidence confidence = null)
        {
            this.id = id ?? string.Empty;
            this.kind = kind;
            this.normalizedRect = normalizedRect;
            this.clearance = clearance;
            this.enabled = enabled;
            this.confidence = confidence ??
                new BackgroundSemanticConfidence();
        }

        public string Id => id ?? string.Empty;
        public BackgroundSemanticZoneKind Kind => kind;
        public Rect NormalizedRect => normalizedRect;
        public float Clearance => clearance;
        public bool Enabled => enabled;
        public BackgroundSemanticConfidence Confidence =>
            confidence ??= new BackgroundSemanticConfidence();

        public Rect ExpandedRect
        {
            get
            {
                return Rect.MinMaxRect(
                    normalizedRect.xMin - clearance,
                    normalizedRect.yMin - clearance,
                    normalizedRect.xMax + clearance,
                    normalizedRect.yMax + clearance);
            }
        }
    }

    [Serializable]
    public sealed class BackgroundSemanticLight
    {
        [SerializeField] private Color tint = Color.white;
        [SerializeField] private Vector2 direction = new(.35f, .65f);
        [SerializeField] private float exposure = .80f;
        [SerializeField] private float saturation = .70f;
        [SerializeField] private float contrast = .86f;
        [SerializeField] private float softness = .30f;
        [SerializeField] private float shadowOpacity = .35f;
        [SerializeField] private BackgroundSemanticConfidence confidence =
            new();

        public BackgroundSemanticLight()
        {
        }

        public BackgroundSemanticLight(
            Color tint,
            Vector2 direction,
            float exposure,
            float saturation,
            float contrast,
            float softness,
            float shadowOpacity,
            BackgroundSemanticConfidence confidence = null)
        {
            this.tint = tint;
            this.direction = direction;
            this.exposure = exposure;
            this.saturation = saturation;
            this.contrast = contrast;
            this.softness = softness;
            this.shadowOpacity = shadowOpacity;
            this.confidence = confidence ??
                new BackgroundSemanticConfidence();
        }

        public Color Tint => tint;
        public Vector2 Direction => direction;
        public float Exposure => exposure;
        public float Saturation => saturation;
        public float Contrast => contrast;
        public float Softness => softness;
        public float ShadowOpacity => shadowOpacity;
        public BackgroundSemanticConfidence Confidence =>
            confidence ??= new BackgroundSemanticConfidence();
    }

    [Serializable]
    public sealed class BackgroundSemanticSlot
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private Vector2 anchor;
        [SerializeField, Range(0f, 1f)] private float depth01;
        [SerializeField, Range(0f, 1f)] private float normalizedHeight = .55f;
        [SerializeField] private Vector2 footprintSize = new(.10f, .36f);
        [SerializeField] private BackgroundSemanticFacing facing;
        [SerializeField] private BackgroundSemanticSlotRole allowedRoles =
            BackgroundSemanticSlotRole.Any;
        [SerializeField] private BackgroundSemanticSlotOrigin origin;
        [SerializeField] private string reservationKey = string.Empty;
        [SerializeField] private BackgroundSemanticConfidence confidence =
            new();

        public BackgroundSemanticSlot()
        {
        }

        public BackgroundSemanticSlot(
            string id,
            Vector2 anchor,
            float depth01,
            float normalizedHeight,
            Vector2 footprintSize,
            BackgroundSemanticFacing facing =
                BackgroundSemanticFacing.Automatic,
            BackgroundSemanticSlotRole allowedRoles =
                BackgroundSemanticSlotRole.Any,
            BackgroundSemanticSlotOrigin origin =
                BackgroundSemanticSlotOrigin.Authored,
            string reservationKey = "",
            BackgroundSemanticConfidence confidence = null)
        {
            this.id = id ?? string.Empty;
            this.anchor = anchor;
            this.depth01 = depth01;
            this.normalizedHeight = normalizedHeight;
            this.footprintSize = footprintSize;
            this.facing = facing;
            this.allowedRoles = allowedRoles;
            this.origin = origin;
            this.reservationKey = reservationKey ?? string.Empty;
            this.confidence = confidence ??
                new BackgroundSemanticConfidence();
        }

        public string Id => id ?? string.Empty;
        public Vector2 Anchor => anchor;
        public float Depth01 => depth01;
        public float NormalizedHeight => normalizedHeight;
        public Vector2 FootprintSize => footprintSize;
        public BackgroundSemanticFacing Facing => facing;
        public BackgroundSemanticSlotRole AllowedRoles => allowedRoles;
        public BackgroundSemanticSlotOrigin Origin => origin;
        public string ReservationKey => reservationKey ?? string.Empty;
        public BackgroundSemanticConfidence Confidence =>
            confidence ??= new BackgroundSemanticConfidence();

        public Rect FootprintRect => new(
            anchor.x - footprintSize.x * .5f,
            anchor.y,
            footprintSize.x,
            footprintSize.y);
    }

    [Serializable]
    public sealed class BackgroundSemanticProfile
    {
        [SerializeField] private string profileId = string.Empty;
        [SerializeField] private string locationCode = string.Empty;
        [SerializeField] private string variantId = string.Empty;
        [SerializeField] private string sourceImageHash = string.Empty;
        [SerializeField] private BackgroundSemanticStatus status = new();
        [SerializeField] private BackgroundSemanticConfidence confidence =
            new();
        [SerializeField] private List<BackgroundSemanticPolygon>
            walkablePolygons = new();
        [SerializeField] private List<BackgroundSemanticZone> zones = new();
        [SerializeField] private List<BackgroundSemanticSlot> slots = new();
        [SerializeField] private BackgroundSemanticLight light = new();
        [SerializeField] private AnimationCurve normalizedHeightByDepth =
            new(
                new Keyframe(0f, .42f),
                new Keyframe(1f, .62f));
        [SerializeField] private int generatorSeed = 1;
        [SerializeField, Min(0)] private int requestedSlotCount = 3;
        [SerializeField, Min(0f)] private float minimumSlotSpacing = .16f;
        [SerializeField, Min(0f)] private float polygonEdgeClearance = .015f;
        [SerializeField] private Vector2 generatedFootprintSize =
            new(.10f, .36f);

        public BackgroundSemanticProfile()
        {
        }

        public BackgroundSemanticProfile(
            string profileId,
            string locationCode,
            string variantId,
            string sourceImageHash,
            BackgroundSemanticStatus status,
            BackgroundSemanticConfidence confidence,
            BackgroundSemanticPolygon walkablePolygon,
            IEnumerable<BackgroundSemanticZone> zones,
            IEnumerable<BackgroundSemanticSlot> slots,
            BackgroundSemanticLight light,
            AnimationCurve normalizedHeightByDepth,
            int generatorSeed,
            int requestedSlotCount,
            float minimumSlotSpacing,
            float polygonEdgeClearance,
            Vector2 generatedFootprintSize)
            : this(
                profileId,
                locationCode,
                variantId,
                sourceImageHash,
                status,
                confidence,
                walkablePolygon != null
                    ? new[] { walkablePolygon }
                    : Array.Empty<BackgroundSemanticPolygon>(),
                zones,
                slots,
                light,
                normalizedHeightByDepth,
                generatorSeed,
                requestedSlotCount,
                minimumSlotSpacing,
                polygonEdgeClearance,
                generatedFootprintSize)
        {
        }

        public BackgroundSemanticProfile(
            string profileId,
            string locationCode,
            string variantId,
            string sourceImageHash,
            BackgroundSemanticStatus status,
            BackgroundSemanticConfidence confidence,
            IEnumerable<BackgroundSemanticPolygon> walkablePolygons,
            IEnumerable<BackgroundSemanticZone> zones,
            IEnumerable<BackgroundSemanticSlot> slots,
            BackgroundSemanticLight light,
            AnimationCurve normalizedHeightByDepth,
            int generatorSeed,
            int requestedSlotCount,
            float minimumSlotSpacing,
            float polygonEdgeClearance,
            Vector2 generatedFootprintSize)
        {
            this.profileId = profileId ?? string.Empty;
            this.locationCode = locationCode ?? string.Empty;
            this.variantId = variantId ?? string.Empty;
            this.sourceImageHash = sourceImageHash ?? string.Empty;
            this.status = status ?? new BackgroundSemanticStatus();
            this.confidence = confidence ??
                new BackgroundSemanticConfidence();
            this.walkablePolygons = walkablePolygons != null
                ? new List<BackgroundSemanticPolygon>(walkablePolygons)
                : new List<BackgroundSemanticPolygon>();
            this.zones = zones != null
                ? new List<BackgroundSemanticZone>(zones)
                : new List<BackgroundSemanticZone>();
            this.slots = slots != null
                ? new List<BackgroundSemanticSlot>(slots)
                : new List<BackgroundSemanticSlot>();
            this.light = light ?? new BackgroundSemanticLight();
            this.normalizedHeightByDepth = normalizedHeightByDepth ??
                new AnimationCurve(
                    new Keyframe(0f, .42f),
                    new Keyframe(1f, .62f));
            this.generatorSeed = generatorSeed;
            this.requestedSlotCount = requestedSlotCount;
            this.minimumSlotSpacing = minimumSlotSpacing;
            this.polygonEdgeClearance = polygonEdgeClearance;
            this.generatedFootprintSize = generatedFootprintSize;
        }

        public string ProfileId => profileId ?? string.Empty;
        public string LocationCode => locationCode ?? string.Empty;
        public string VariantId => variantId ?? string.Empty;
        public string SourceImageHash => sourceImageHash ?? string.Empty;
        public BackgroundSemanticStatus Status =>
            status ??= new BackgroundSemanticStatus();
        public BackgroundSemanticConfidence Confidence =>
            confidence ??= new BackgroundSemanticConfidence();
        public IReadOnlyList<BackgroundSemanticPolygon> WalkablePolygons =>
            walkablePolygons ??= new List<BackgroundSemanticPolygon>();
        public BackgroundSemanticPolygon WalkablePolygon
        {
            get
            {
                walkablePolygons ??=
                    new List<BackgroundSemanticPolygon>();
                if (walkablePolygons.Count == 0)
                    walkablePolygons.Add(new BackgroundSemanticPolygon());
                return walkablePolygons[0] ??=
                    new BackgroundSemanticPolygon();
            }
        }
        public IReadOnlyList<BackgroundSemanticZone> Zones =>
            zones ??= new List<BackgroundSemanticZone>();
        public IReadOnlyList<BackgroundSemanticSlot> Slots =>
            slots ??= new List<BackgroundSemanticSlot>();
        public BackgroundSemanticLight Light =>
            light ??= new BackgroundSemanticLight();
        public AnimationCurve NormalizedHeightByDepth =>
            normalizedHeightByDepth ??= new AnimationCurve(
                new Keyframe(0f, .42f),
                new Keyframe(1f, .62f));
        public int GeneratorSeed => generatorSeed;
        public int RequestedSlotCount => requestedSlotCount;
        public float MinimumSlotSpacing => minimumSlotSpacing;
        public float PolygonEdgeClearance => polygonEdgeClearance;
        public Vector2 GeneratedFootprintSize => generatedFootprintSize;
        public bool IsUnused =>
            Status.State == BackgroundSemanticProfileState.Unused;

        public float EvaluateNormalizedHeight(float depth01)
        {
            return NormalizedHeightByDepth.Evaluate(
                Mathf.Clamp01(depth01));
        }
    }
}
