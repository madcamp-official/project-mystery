using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Wake.Exploration
{
    public enum BackgroundSemanticDiagnosticSeverity
    {
        Info,
        Warning,
        Error
    }

    public enum BackgroundSemanticDiagnosticCode
    {
        MissingProfileId,
        MissingLocationCode,
        MissingSourceHash,
        InvalidSourceHash,
        SourceHashMismatch,
        MissingStatus,
        InvalidRevision,
        InvalidConfidence,
        MissingWalkablePolygon,
        InvalidPolygon,
        SelfIntersectingPolygon,
        CoordinateOutOfRange,
        DuplicateZoneId,
        DuplicateSlotId,
        DuplicateSlotAnchor,
        InvalidZone,
        InvalidLight,
        InvalidDepthCurve,
        InvalidGenerationSettings,
        SlotOutsideWalkablePolygon,
        SlotIntersectsRestrictedZone,
        InvalidSlot,
        UnusedProfileContainsSemanticData
    }

    public readonly struct BackgroundSemanticDiagnostic
    {
        public BackgroundSemanticDiagnostic(
            BackgroundSemanticDiagnosticSeverity severity,
            BackgroundSemanticDiagnosticCode code,
            string subject,
            string message)
        {
            Severity = severity;
            Code = code;
            Subject = subject ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public BackgroundSemanticDiagnosticSeverity Severity { get; }
        public BackgroundSemanticDiagnosticCode Code { get; }
        public string Subject { get; }
        public string Message { get; }
    }

    public static class BackgroundSemanticValidator
    {
        private const float DuplicateAnchorTolerance = .0001f;

        public static IReadOnlyList<BackgroundSemanticDiagnostic> Validate(
            BackgroundSemanticProfile profile,
            IEnumerable<BackgroundSemanticSlot> generatedSlots = null,
            string expectedSourceImageHash = null)
        {
            var diagnostics = new List<BackgroundSemanticDiagnostic>();
            if (profile == null)
            {
                diagnostics.Add(Error(
                    BackgroundSemanticDiagnosticCode.MissingProfileId,
                    string.Empty,
                    "Background semantic profile is null."));
                return diagnostics;
            }

            ValidateIdentity(
                profile,
                expectedSourceImageHash,
                diagnostics);
            ValidateStatusAndConfidence(profile, diagnostics);
            ValidatePolygon(profile, diagnostics);
            ValidateZones(profile, diagnostics);
            ValidateLight(profile, diagnostics);
            ValidateDepthCurve(profile, diagnostics);
            ValidateGenerationSettings(profile, diagnostics);

            BackgroundSemanticSlot[] allSlots =
                profile.Slots
                    .Concat(
                        generatedSlots ??
                        Array.Empty<BackgroundSemanticSlot>())
                    .ToArray();
            ValidateSlots(profile, allSlots, diagnostics);
            ValidateUnused(profile, allSlots, diagnostics);
            return diagnostics;
        }

        public static bool HasErrors(
            IEnumerable<BackgroundSemanticDiagnostic> diagnostics)
        {
            return diagnostics != null &&
                   diagnostics.Any(item =>
                       item.Severity ==
                       BackgroundSemanticDiagnosticSeverity.Error);
        }

        private static void ValidateIdentity(
            BackgroundSemanticProfile profile,
            string expectedSourceImageHash,
            ICollection<BackgroundSemanticDiagnostic> diagnostics)
        {
            if (string.IsNullOrWhiteSpace(profile.ProfileId))
            {
                diagnostics.Add(Error(
                    BackgroundSemanticDiagnosticCode.MissingProfileId,
                    "profile",
                    "Profile ID is required."));
            }

            if (string.IsNullOrWhiteSpace(profile.LocationCode))
            {
                diagnostics.Add(Error(
                    BackgroundSemanticDiagnosticCode.MissingLocationCode,
                    profile.ProfileId,
                    "Location code is required."));
            }

            if (string.IsNullOrWhiteSpace(profile.SourceImageHash))
            {
                diagnostics.Add(Error(
                    BackgroundSemanticDiagnosticCode.MissingSourceHash,
                    profile.ProfileId,
                    "A SHA-256 source image hash is required."));
            }
            else if (!IsSha256(profile.SourceImageHash))
            {
                diagnostics.Add(Error(
                    BackgroundSemanticDiagnosticCode.InvalidSourceHash,
                    profile.ProfileId,
                    "Source image hash must contain 64 hexadecimal characters."));
            }

            if (!string.IsNullOrWhiteSpace(expectedSourceImageHash) &&
                !string.Equals(
                    profile.SourceImageHash,
                    expectedSourceImageHash.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                diagnostics.Add(Error(
                    BackgroundSemanticDiagnosticCode.SourceHashMismatch,
                    profile.ProfileId,
                    "The semantic profile was authored against a different image hash."));
            }
        }

        private static void ValidateStatusAndConfidence(
            BackgroundSemanticProfile profile,
            ICollection<BackgroundSemanticDiagnostic> diagnostics)
        {
            if (profile.Status == null)
            {
                diagnostics.Add(Error(
                    BackgroundSemanticDiagnosticCode.MissingStatus,
                    profile.ProfileId,
                    "Review status is required."));
            }
            else if (profile.Status.Revision < 0)
            {
                diagnostics.Add(Error(
                    BackgroundSemanticDiagnosticCode.InvalidRevision,
                    profile.ProfileId,
                    "Review revision cannot be negative."));
            }

            ValidateConfidence(
                profile.Confidence,
                profile.ProfileId,
                diagnostics);
        }

        private static void ValidatePolygon(
            BackgroundSemanticProfile profile,
            ICollection<BackgroundSemanticDiagnostic> diagnostics)
        {
            bool hasValidVertexCount = profile.WalkablePolygons.Any(
                polygon =>
                    polygon != null &&
                    polygon.Vertices.Count >= 3);
            if (!hasValidVertexCount)
            {
                if (!profile.IsUnused)
                {
                    diagnostics.Add(Error(
                        BackgroundSemanticDiagnosticCode
                            .MissingWalkablePolygon,
                        profile.ProfileId,
                        "Playable profiles require at least three walkable vertices."));
                }
                return;
            }

            for (int polygonIndex = 0;
                 polygonIndex < profile.WalkablePolygons.Count;
                 polygonIndex++)
            {
                BackgroundSemanticPolygon polygon =
                    profile.WalkablePolygons[polygonIndex];
                string subject =
                    $"{profile.ProfileId}.walkable[{polygonIndex}]";
                if (polygon == null ||
                    polygon.Vertices.Count < 3)
                {
                    diagnostics.Add(Error(
                        BackgroundSemanticDiagnosticCode
                            .InvalidPolygon,
                        subject,
                        "Every walkable island requires at least three vertices."));
                    continue;
                }

                IReadOnlyList<Vector2> vertices = polygon.Vertices;
                for (int vertexIndex = 0;
                     vertexIndex < vertices.Count;
                     vertexIndex++)
                {
                    Vector2 vertex = vertices[vertexIndex];
                    if (!IsFinite(vertex) ||
                        !IsNormalized(vertex))
                    {
                        diagnostics.Add(Error(
                            BackgroundSemanticDiagnosticCode
                                .CoordinateOutOfRange,
                            $"{subject}.vertex[{vertexIndex}]",
                            "Walkable vertices must be finite normalized coordinates."));
                    }
                }

                if (Mathf.Abs(polygon.SignedArea) < .000001f)
                {
                    diagnostics.Add(Error(
                        BackgroundSemanticDiagnosticCode.InvalidPolygon,
                        subject,
                        "Walkable polygon area must be non-zero."));
                }

                if (HasSelfIntersection(vertices))
                {
                    diagnostics.Add(Error(
                        BackgroundSemanticDiagnosticCode
                            .SelfIntersectingPolygon,
                        subject,
                        "Walkable polygon edges cannot self-intersect."));
                }
            }
        }

        private static void ValidateZones(
            BackgroundSemanticProfile profile,
            ICollection<BackgroundSemanticDiagnostic> diagnostics)
        {
            var ids = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < profile.Zones.Count; index++)
            {
                BackgroundSemanticZone zone = profile.Zones[index];
                string subject = zone?.Id ??
                    $"{profile.ProfileId}.zone[{index}]";
                if (zone == null)
                {
                    diagnostics.Add(Error(
                        BackgroundSemanticDiagnosticCode.InvalidZone,
                        subject,
                        "Zone cannot be null."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(zone.Id) ||
                    !ids.Add(zone.Id.Trim()))
                {
                    diagnostics.Add(Error(
                        BackgroundSemanticDiagnosticCode.DuplicateZoneId,
                        subject,
                        "Zone IDs must be non-empty and unique."));
                }

                Rect rect = zone.NormalizedRect;
                if (!IsFinite(rect) ||
                    rect.width <= 0f ||
                    rect.height <= 0f ||
                    rect.xMin < 0f ||
                    rect.yMin < 0f ||
                    rect.xMax > 1f ||
                    rect.yMax > 1f ||
                    !IsFinite(zone.Clearance) ||
                    zone.Clearance < 0f)
                {
                    diagnostics.Add(Error(
                        BackgroundSemanticDiagnosticCode.InvalidZone,
                        subject,
                        "Zone rectangles and clearances must be finite, positive, and normalized."));
                }

                ValidateConfidence(
                    zone.Confidence,
                    subject,
                    diagnostics);
            }
        }

        private static void ValidateLight(
            BackgroundSemanticProfile profile,
            ICollection<BackgroundSemanticDiagnostic> diagnostics)
        {
            BackgroundSemanticLight light = profile.Light;
            bool validTint =
                IsFinite(light.Tint.r) &&
                IsFinite(light.Tint.g) &&
                IsFinite(light.Tint.b) &&
                IsFinite(light.Tint.a) &&
                light.Tint.r >= 0f && light.Tint.r <= 1f &&
                light.Tint.g >= 0f && light.Tint.g <= 1f &&
                light.Tint.b >= 0f && light.Tint.b <= 1f &&
                light.Tint.a >= 0f && light.Tint.a <= 1f;
            bool valid =
                validTint &&
                IsFinite(light.Direction) &&
                light.Direction.sqrMagnitude <= 1.0001f &&
                IsInRange(light.Exposure, .45f, 1.2f) &&
                IsInRange(light.Saturation, .35f, 1.1f) &&
                IsInRange(light.Contrast, .55f, 1.2f) &&
                IsInRange(light.Softness, 0f, 1f) &&
                IsInRange(light.ShadowOpacity, 0f, 1f);
            if (!valid)
            {
                diagnostics.Add(Error(
                    BackgroundSemanticDiagnosticCode.InvalidLight,
                    profile.ProfileId,
                    "Light grade values are outside the supported normalized ranges."));
            }

            ValidateConfidence(
                light.Confidence,
                $"{profile.ProfileId}.light",
                diagnostics);
        }

        private static void ValidateDepthCurve(
            BackgroundSemanticProfile profile,
            ICollection<BackgroundSemanticDiagnostic> diagnostics)
        {
            Keyframe[] keys = profile.NormalizedHeightByDepth.keys;
            bool valid = keys.Length >= 2;
            float previousTime = float.NegativeInfinity;
            float previousValue = float.NegativeInfinity;
            foreach (Keyframe key in keys)
            {
                valid &= IsInRange(key.time, 0f, 1f);
                valid &= IsInRange(key.value, .2f, .9f);
                valid &= key.time >= previousTime;
                valid &= key.value + .000001f >= previousValue;
                previousTime = key.time;
                previousValue = key.value;
            }

            if (!valid)
            {
                diagnostics.Add(Error(
                    BackgroundSemanticDiagnosticCode.InvalidDepthCurve,
                    profile.ProfileId,
                    "Depth-height curve must be monotonic with normalized keys and heights from .2 to .9."));
            }
        }

        private static void ValidateGenerationSettings(
            BackgroundSemanticProfile profile,
            ICollection<BackgroundSemanticDiagnostic> diagnostics)
        {
            Vector2 footprint = profile.GeneratedFootprintSize;
            bool valid =
                profile.RequestedSlotCount >= 0 &&
                IsInRange(profile.MinimumSlotSpacing, 0f, 1f) &&
                IsInRange(profile.PolygonEdgeClearance, 0f, .5f) &&
                IsFinite(footprint) &&
                footprint.x > 0f &&
                footprint.y > 0f &&
                footprint.x <= 1f &&
                footprint.y <= 1f;
            if (!valid)
            {
                diagnostics.Add(Error(
                    BackgroundSemanticDiagnosticCode
                        .InvalidGenerationSettings,
                    profile.ProfileId,
                    "Slot generation count, spacing, clearance, and footprint must be normalized."));
            }
        }

        private static void ValidateSlots(
            BackgroundSemanticProfile profile,
            IReadOnlyList<BackgroundSemanticSlot> slots,
            ICollection<BackgroundSemanticDiagnostic> diagnostics)
        {
            var ids = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < slots.Count; index++)
            {
                BackgroundSemanticSlot slot = slots[index];
                string subject = slot?.Id ??
                    $"{profile.ProfileId}.slot[{index}]";
                if (slot == null)
                {
                    diagnostics.Add(Error(
                        BackgroundSemanticDiagnosticCode.InvalidSlot,
                        subject,
                        "Slot cannot be null."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(slot.Id) ||
                    !ids.Add(slot.Id.Trim()))
                {
                    diagnostics.Add(Error(
                        BackgroundSemanticDiagnosticCode.DuplicateSlotId,
                        subject,
                        "Slot IDs must be non-empty and unique."));
                }

                bool valid =
                    IsFinite(slot.Anchor) &&
                    IsNormalized(slot.Anchor) &&
                    IsInRange(slot.Depth01, 0f, 1f) &&
                    IsInRange(slot.NormalizedHeight, .2f, .9f) &&
                    IsFinite(slot.FootprintSize) &&
                    slot.FootprintSize.x > 0f &&
                    slot.FootprintSize.y > 0f &&
                    slot.FootprintSize.x <= 1f &&
                    slot.FootprintSize.y <= 1f &&
                    slot.AllowedRoles !=
                    BackgroundSemanticSlotRole.None;
                Rect footprint = slot.FootprintRect;
                valid &=
                    footprint.xMin >= 0f &&
                    footprint.yMin >= 0f &&
                    footprint.xMax <= 1f &&
                    footprint.yMax <= 1f;
                if (!valid)
                {
                    diagnostics.Add(Error(
                        BackgroundSemanticDiagnosticCode.InvalidSlot,
                        subject,
                        "Slot coordinates, depth, height, footprint, and roles must be valid."));
                }

                ValidateConfidence(
                    slot.Confidence,
                    subject,
                    diagnostics);

                if (!profile.IsUnused &&
                    !BackgroundSemanticSlotGenerator.IsSlotAllowed(
                        profile,
                        slot.Anchor,
                        slot.FootprintSize,
                        profile.PolygonEdgeClearance))
                {
                    bool contactInside =
                        BackgroundSemanticSlotGenerator
                            .TryGetContainingWalkablePolygon(
                                profile,
                                slot.Anchor,
                                slot.FootprintSize,
                                profile.PolygonEdgeClearance,
                                out _);
                    if (!contactInside)
                    {
                        diagnostics.Add(Error(
                            BackgroundSemanticDiagnosticCode
                                .SlotOutsideWalkablePolygon,
                            subject,
                            "Slot anchor is outside the walkable polygon."));
                    }

                    if (IntersectsRestrictedZone(profile, slot))
                    {
                        diagnostics.Add(Error(
                            BackgroundSemanticDiagnosticCode
                                .SlotIntersectsRestrictedZone,
                            subject,
                            "Slot footprint overlaps a forbidden or protected zone."));
                    }
                }

                for (int previous = 0; previous < index; previous++)
                {
                    BackgroundSemanticSlot other = slots[previous];
                    if (other == null)
                        continue;
                    if (Vector2.Distance(
                            slot.Anchor,
                            other.Anchor) <= DuplicateAnchorTolerance)
                    {
                        diagnostics.Add(Error(
                            BackgroundSemanticDiagnosticCode
                                .DuplicateSlotAnchor,
                            subject,
                            $"Slot anchor duplicates '{other.Id}'."));
                        break;
                    }
                }
            }
        }

        private static void ValidateUnused(
            BackgroundSemanticProfile profile,
            IReadOnlyCollection<BackgroundSemanticSlot> slots,
            ICollection<BackgroundSemanticDiagnostic> diagnostics)
        {
            if (!profile.IsUnused)
                return;

            bool containsData =
                profile.WalkablePolygons.Any(
                    polygon =>
                        polygon != null &&
                        polygon.Vertices.Count > 0) ||
                profile.Zones.Count > 0 ||
                slots.Count > 0 ||
                profile.RequestedSlotCount > 0;
            if (containsData)
            {
                diagnostics.Add(new BackgroundSemanticDiagnostic(
                    BackgroundSemanticDiagnosticSeverity.Warning,
                    BackgroundSemanticDiagnosticCode
                        .UnusedProfileContainsSemanticData,
                    profile.ProfileId,
                    "Unused locations should not retain placement semantics."));
            }
        }

        private static bool IntersectsRestrictedZone(
            BackgroundSemanticProfile profile,
            BackgroundSemanticSlot slot)
        {
            Rect footprint = slot.FootprintRect;
            return profile.Zones.Any(zone =>
                zone != null &&
                zone.Enabled &&
                zone.Kind != BackgroundSemanticZoneKind.Uncertain &&
                footprint.Overlaps(zone.ExpandedRect, true));
        }

        private static void ValidateConfidence(
            BackgroundSemanticConfidence confidence,
            string subject,
            ICollection<BackgroundSemanticDiagnostic> diagnostics)
        {
            if (confidence == null ||
                !IsInRange(confidence.Score, 0f, 1f))
            {
                diagnostics.Add(Error(
                    BackgroundSemanticDiagnosticCode.InvalidConfidence,
                    subject,
                    "Confidence score must be normalized."));
            }
        }

        private static bool HasSelfIntersection(
            IReadOnlyList<Vector2> vertices)
        {
            int count = vertices.Count;
            if (count < 4)
                return false;

            for (int first = 0; first < count; first++)
            {
                int firstNext = (first + 1) % count;
                for (int second = first + 1; second < count; second++)
                {
                    int secondNext = (second + 1) % count;
                    bool adjacent =
                        first == second ||
                        firstNext == second ||
                        secondNext == first;
                    if (adjacent)
                        continue;

                    if (SegmentsIntersect(
                            vertices[first],
                            vertices[firstNext],
                            vertices[second],
                            vertices[secondNext]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool SegmentsIntersect(
            Vector2 firstA,
            Vector2 firstB,
            Vector2 secondA,
            Vector2 secondB)
        {
            float firstDirection =
                Cross(firstB - firstA, secondA - firstA);
            float secondDirection =
                Cross(firstB - firstA, secondB - firstA);
            float thirdDirection =
                Cross(secondB - secondA, firstA - secondA);
            float fourthDirection =
                Cross(secondB - secondA, firstB - secondA);

            return firstDirection * secondDirection < 0f &&
                   thirdDirection * fourthDirection < 0f;
        }

        private static float Cross(Vector2 first, Vector2 second)
        {
            return first.x * second.y - first.y * second.x;
        }

        private static bool IsSha256(string value)
        {
            string trimmed = value?.Trim() ?? string.Empty;
            if (trimmed.Length != 64)
                return false;

            for (int index = 0; index < trimmed.Length; index++)
            {
                char character = trimmed[index];
                bool hexadecimal =
                    character >= '0' && character <= '9' ||
                    character >= 'a' && character <= 'f' ||
                    character >= 'A' && character <= 'F';
                if (!hexadecimal)
                    return false;
            }

            return true;
        }

        private static bool IsNormalized(Vector2 value)
        {
            return value.x >= 0f && value.x <= 1f &&
                   value.y >= 0f && value.y <= 1f;
        }

        private static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        private static bool IsFinite(Rect value)
        {
            return IsFinite(value.x) &&
                   IsFinite(value.y) &&
                   IsFinite(value.width) &&
                   IsFinite(value.height);
        }

        private static bool IsInRange(
            float value,
            float minimum,
            float maximum)
        {
            return IsFinite(value) &&
                   value >= minimum &&
                   value <= maximum;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) &&
                   !float.IsInfinity(value);
        }

        private static BackgroundSemanticDiagnostic Error(
            BackgroundSemanticDiagnosticCode code,
            string subject,
            string message)
        {
            return new BackgroundSemanticDiagnostic(
                BackgroundSemanticDiagnosticSeverity.Error,
                code,
                subject,
                message);
        }
    }
}
