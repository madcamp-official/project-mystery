using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wake.Exploration
{
    [Serializable]
    public sealed class BackgroundSemanticSlotGenerationSettings
    {
        [SerializeField, Min(0)] private int requestedCount;
        [SerializeField, Min(16)] private int sampleCount = 512;
        [SerializeField, Min(0f)] private float minimumSpacing;
        [SerializeField, Min(0f)] private float edgeClearance;
        [SerializeField] private Vector2 footprintSize;
        [SerializeField] private int seed;

        public BackgroundSemanticSlotGenerationSettings()
        {
        }

        public BackgroundSemanticSlotGenerationSettings(
            int requestedCount,
            int sampleCount,
            float minimumSpacing,
            float edgeClearance,
            Vector2 footprintSize,
            int seed)
        {
            this.requestedCount = requestedCount;
            this.sampleCount = sampleCount;
            this.minimumSpacing = minimumSpacing;
            this.edgeClearance = edgeClearance;
            this.footprintSize = footprintSize;
            this.seed = seed;
        }

        public int RequestedCount => requestedCount;
        public int SampleCount => sampleCount;
        public float MinimumSpacing => minimumSpacing;
        public float EdgeClearance => edgeClearance;
        public Vector2 FootprintSize => footprintSize;
        public int Seed => seed;

        public static BackgroundSemanticSlotGenerationSettings FromProfile(
            BackgroundSemanticProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            return new BackgroundSemanticSlotGenerationSettings(
                profile.RequestedSlotCount,
                Mathf.Max(128, profile.RequestedSlotCount * 96),
                profile.MinimumSlotSpacing,
                profile.PolygonEdgeClearance,
                profile.GeneratedFootprintSize,
                profile.GeneratorSeed);
        }
    }

    public sealed class BackgroundSemanticSlotGenerationResult
    {
        public BackgroundSemanticSlotGenerationResult(
            IReadOnlyList<BackgroundSemanticSlot> slots,
            int sampledCandidateCount,
            int validCandidateCount)
        {
            Slots = slots ?? Array.Empty<BackgroundSemanticSlot>();
            SampledCandidateCount = sampledCandidateCount;
            ValidCandidateCount = validCandidateCount;
        }

        public IReadOnlyList<BackgroundSemanticSlot> Slots { get; }
        public int SampledCandidateCount { get; }
        public int ValidCandidateCount { get; }
        public bool IsComplete(int requestedCount) =>
            Slots.Count >= Mathf.Max(0, requestedCount);
    }

    /// <summary>
    /// Generates stable, renderer-independent candidate slots. The generator
    /// only reads semantic data and has no connection to location runtime code.
    /// </summary>
    public static class BackgroundSemanticSlotGenerator
    {
        private readonly struct Candidate
        {
            public Candidate(
                Vector2 anchor,
                float boundaryClearance,
                uint tieBreaker)
            {
                Anchor = anchor;
                BoundaryClearance = boundaryClearance;
                TieBreaker = tieBreaker;
            }

            public Vector2 Anchor { get; }
            public float BoundaryClearance { get; }
            public uint TieBreaker { get; }
        }

        public static BackgroundSemanticSlotGenerationResult Generate(
            BackgroundSemanticProfile profile)
        {
            return Generate(
                profile,
                BackgroundSemanticSlotGenerationSettings.FromProfile(
                    profile));
        }

        public static BackgroundSemanticSlotGenerationResult Generate(
            BackgroundSemanticProfile profile,
            BackgroundSemanticSlotGenerationSettings settings)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            int requestedCount = Mathf.Max(0, settings.RequestedCount);
            int sampleCount = Mathf.Max(
                16,
                Mathf.Max(settings.SampleCount, requestedCount * 32));
            if (requestedCount == 0 ||
                profile.IsUnused ||
                !TryGetWalkableBounds(profile, out Rect bounds))
            {
                return new BackgroundSemanticSlotGenerationResult(
                    Array.Empty<BackgroundSemanticSlot>(),
                    sampleCount,
                    0);
            }

            Vector2 footprint = new(
                Mathf.Max(0f, settings.FootprintSize.x),
                Mathf.Max(0f, settings.FootprintSize.y));
            float edgeClearance = Mathf.Max(0f, settings.EdgeClearance);
            var candidates = new List<Candidate>(
                sampleCount + profile.WalkablePolygons.Count);
            int offset = StablePositiveOffset(settings.Seed);

            int polygonIndex = 0;
            foreach (BackgroundSemanticPolygon polygon in
                     profile.WalkablePolygons)
            {
                if (polygon == null || polygon.Vertices.Count < 3)
                    continue;
                AddCandidate(
                    profile,
                    settings,
                    footprint,
                    polygon.Bounds.center,
                    StableHash(settings.Seed, polygonIndex++),
                    candidates);
            }

            for (int index = 0; index < sampleCount; index++)
            {
                int sequenceIndex = index + 1 + offset;
                Vector2 unit = new(
                    Halton(sequenceIndex, 2),
                    Halton(sequenceIndex, 3));
                Vector2 point = new(
                    Mathf.Lerp(bounds.xMin, bounds.xMax, unit.x),
                    Mathf.Lerp(bounds.yMin, bounds.yMax, unit.y));
                AddCandidate(
                    profile,
                    settings,
                    footprint,
                    point,
                    StableHash(settings.Seed, sequenceIndex),
                    candidates);
            }

            var selected = new List<Candidate>(requestedCount);
            float minimumSpacing = Mathf.Max(0f, settings.MinimumSpacing);
            while (selected.Count < requestedCount)
            {
                int bestIndex = FindBestCandidate(
                    candidates,
                    selected,
                    profile.Slots,
                    minimumSpacing,
                    edgeClearance);
                if (bestIndex < 0)
                    break;

                selected.Add(candidates[bestIndex]);
                candidates.RemoveAt(bestIndex);
            }

            var slots = new BackgroundSemanticSlot[selected.Count];
            float minimumY = bounds.yMin;
            float maximumY = bounds.yMax;
            for (int index = 0; index < selected.Count; index++)
            {
                Vector2 anchor = selected[index].Anchor;
                float depth = maximumY - minimumY > Mathf.Epsilon
                    ? Mathf.InverseLerp(maximumY, minimumY, anchor.y)
                    : .5f;
                slots[index] = new BackgroundSemanticSlot(
                    $"{profile.ProfileId}.auto.{index + 1:00}",
                    anchor,
                    depth,
                    profile.EvaluateNormalizedHeight(depth),
                    footprint,
                    anchor.x < .5f
                        ? BackgroundSemanticFacing.Right
                        : BackgroundSemanticFacing.Left,
                    BackgroundSemanticSlotRole.Any,
                    BackgroundSemanticSlotOrigin.Generated,
                    confidence: new BackgroundSemanticConfidence(
                        .72f,
                        "deterministic-semantic-generator"));
            }

            return new BackgroundSemanticSlotGenerationResult(
                Array.AsReadOnly(slots),
                sampleCount + polygonIndex,
                candidates.Count + selected.Count);
        }

        public static bool IsSlotAllowed(
            BackgroundSemanticProfile profile,
            Vector2 anchor,
            Vector2 footprintSize,
            float edgeClearance = 0f)
        {
            if (profile == null)
                return false;

            float halfFootWidth = Mathf.Max(0f, footprintSize.x) * .5f;
            Rect footprint = new(
                anchor.x - halfFootWidth,
                anchor.y,
                Mathf.Max(0f, footprintSize.x),
                Mathf.Max(0f, footprintSize.y));
            if (footprint.xMin < 0f ||
                footprint.yMin < 0f ||
                footprint.xMax > 1f ||
                footprint.yMax > 1f)
            {
                return false;
            }

            if (!TryGetContainingWalkablePolygon(
                    profile,
                    anchor,
                    footprintSize,
                    edgeClearance,
                    out _))
            {
                return false;
            }

            foreach (BackgroundSemanticZone zone in profile.Zones)
            {
                if (zone == null ||
                    !zone.Enabled ||
                    zone.Kind == BackgroundSemanticZoneKind.Uncertain)
                {
                    continue;
                }
                if (footprint.Overlaps(zone.ExpandedRect, true))
                    return false;
            }

            return true;
        }

        private static void AddCandidate(
            BackgroundSemanticProfile profile,
            BackgroundSemanticSlotGenerationSettings settings,
            Vector2 footprint,
            Vector2 point,
            uint tieBreaker,
            ICollection<Candidate> candidates)
        {
            if (!IsSlotAllowed(
                    profile,
                    point,
                    footprint,
                    settings.EdgeClearance))
            {
                return;
            }

            candidates.Add(new Candidate(
                point,
                GetContainingWalkableBoundaryDistance(
                    profile,
                    point,
                    footprint),
                tieBreaker));
        }

        internal static bool TryGetContainingWalkablePolygon(
            BackgroundSemanticProfile profile,
            Vector2 anchor,
            Vector2 footprintSize,
            float edgeClearance,
            out BackgroundSemanticPolygon containing)
        {
            containing = null;
            if (profile == null)
                return false;

            float halfFootWidth =
                Mathf.Max(0f, footprintSize.x) * .5f;
            Vector2 leftFoot =
                new(anchor.x - halfFootWidth, anchor.y);
            Vector2 rightFoot =
                new(anchor.x + halfFootWidth, anchor.y);
            float requiredClearance = Mathf.Max(0f, edgeClearance);
            foreach (BackgroundSemanticPolygon polygon in
                     profile.WalkablePolygons)
            {
                if (polygon == null ||
                    polygon.Vertices.Count < 3 ||
                    !polygon.Contains(anchor) ||
                    !polygon.Contains(leftFoot) ||
                    !polygon.Contains(rightFoot) ||
                    polygon.DistanceToBoundary(anchor) + .000001f <
                    requiredClearance)
                {
                    continue;
                }

                containing = polygon;
                return true;
            }

            return false;
        }

        private static float GetContainingWalkableBoundaryDistance(
            BackgroundSemanticProfile profile,
            Vector2 anchor,
            Vector2 footprintSize)
        {
            return TryGetContainingWalkablePolygon(
                profile,
                anchor,
                footprintSize,
                0f,
                out BackgroundSemanticPolygon polygon)
                ? polygon.DistanceToBoundary(anchor)
                : 0f;
        }

        private static bool TryGetWalkableBounds(
            BackgroundSemanticProfile profile,
            out Rect bounds)
        {
            bounds = default;
            bool found = false;
            foreach (BackgroundSemanticPolygon polygon in
                     profile.WalkablePolygons)
            {
                if (polygon == null || polygon.Vertices.Count < 3)
                    continue;

                Rect polygonBounds = polygon.Bounds;
                if (!found)
                {
                    bounds = polygonBounds;
                    found = true;
                    continue;
                }

                bounds = Rect.MinMaxRect(
                    Mathf.Min(bounds.xMin, polygonBounds.xMin),
                    Mathf.Min(bounds.yMin, polygonBounds.yMin),
                    Mathf.Max(bounds.xMax, polygonBounds.xMax),
                    Mathf.Max(bounds.yMax, polygonBounds.yMax));
            }

            return found;
        }

        private static int FindBestCandidate(
            IReadOnlyList<Candidate> candidates,
            IReadOnlyList<Candidate> selected,
            IReadOnlyList<BackgroundSemanticSlot> authored,
            float minimumSpacing,
            float edgeClearance)
        {
            int bestIndex = -1;
            float bestSeparation = float.NegativeInfinity;
            float bestBoundary = float.NegativeInfinity;
            uint bestTieBreaker = uint.MaxValue;

            for (int index = 0; index < candidates.Count; index++)
            {
                Candidate candidate = candidates[index];
                float minimumDistance = float.PositiveInfinity;

                for (int selectedIndex = 0;
                     selectedIndex < selected.Count;
                     selectedIndex++)
                {
                    minimumDistance = Mathf.Min(
                        minimumDistance,
                        Vector2.Distance(
                            candidate.Anchor,
                            selected[selectedIndex].Anchor));
                }

                for (int authoredIndex = 0;
                     authoredIndex < authored.Count;
                     authoredIndex++)
                {
                    BackgroundSemanticSlot slot = authored[authoredIndex];
                    if (slot == null)
                        continue;
                    minimumDistance = Mathf.Min(
                        minimumDistance,
                        Vector2.Distance(candidate.Anchor, slot.Anchor));
                }

                if (float.IsPositiveInfinity(minimumDistance))
                    minimumDistance = 1f;
                if (minimumDistance + .000001f < minimumSpacing)
                    continue;
                if (candidate.BoundaryClearance + .000001f <
                    edgeClearance)
                {
                    continue;
                }

                bool better =
                    minimumDistance > bestSeparation + .000001f ||
                    Mathf.Abs(minimumDistance - bestSeparation) <=
                    .000001f &&
                    (candidate.BoundaryClearance >
                     bestBoundary + .000001f ||
                     Mathf.Abs(
                         candidate.BoundaryClearance -
                         bestBoundary) <= .000001f &&
                     candidate.TieBreaker < bestTieBreaker);
                if (!better)
                    continue;

                bestIndex = index;
                bestSeparation = minimumDistance;
                bestBoundary = candidate.BoundaryClearance;
                bestTieBreaker = candidate.TieBreaker;
            }

            return bestIndex;
        }

        private static int StablePositiveOffset(int seed)
        {
            uint value = unchecked((uint)seed);
            value ^= value >> 16;
            value *= 0x7feb352du;
            value ^= value >> 15;
            return (int)(value % 997u);
        }

        private static uint StableHash(int seed, int index)
        {
            uint value = unchecked((uint)seed);
            value ^= unchecked((uint)index) + 0x9e3779b9u +
                     (value << 6) + (value >> 2);
            value ^= value >> 16;
            value *= 0x7feb352du;
            value ^= value >> 15;
            value *= 0x846ca68bu;
            value ^= value >> 16;
            return value;
        }

        private static float Halton(int index, int radix)
        {
            float result = 0f;
            float fraction = 1f / radix;
            int value = Mathf.Max(1, index);
            while (value > 0)
            {
                result += fraction * (value % radix);
                value /= radix;
                fraction /= radix;
            }

            return result;
        }
    }
}
