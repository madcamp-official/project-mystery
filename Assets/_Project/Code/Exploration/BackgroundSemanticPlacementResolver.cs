using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Wake.Exploration
{
    public readonly struct BackgroundSemanticCharacterRequest
    {
        public BackgroundSemanticCharacterRequest(
            string characterId,
            BackgroundSemanticCharacterRole role)
        {
            CharacterId =
                BackgroundSemanticCharacterSlotBinding.NormalizeCode(
                    characterId);
            Role = role;
            if (!AmbientWorldCharacterCatalog.TryGetAsset(
                    CharacterId,
                    out AmbientWorldCharacterAsset asset))
            {
                asset = new AmbientWorldCharacterAsset(
                    string.Empty,
                    new Rect(0f, 0f, 1f, 1f),
                    .7f,
                    .02f,
                    .02f);
            }

            CharacterAsset = asset;
        }

        public BackgroundSemanticCharacterRequest(
            string characterId,
            BackgroundSemanticCharacterRole role,
            AmbientWorldCharacterAsset characterAsset)
        {
            CharacterId =
                BackgroundSemanticCharacterSlotBinding.NormalizeCode(
                    characterId);
            Role = role;
            CharacterAsset = characterAsset;
        }

        public string CharacterId { get; }
        public BackgroundSemanticCharacterRole Role { get; }
        public AmbientWorldCharacterAsset CharacterAsset { get; }
    }

    public sealed class BackgroundSemanticPlacementAssignment
    {
        public BackgroundSemanticPlacementAssignment(
            BackgroundSemanticCharacterRequest character,
            BackgroundSemanticSlot slot,
            Rect silhouetteRect,
            bool fixedBySceneLayout)
        {
            Character = character;
            Slot = slot;
            SilhouetteRect = silhouetteRect;
            FixedBySceneLayout = fixedBySceneLayout;
        }

        public BackgroundSemanticCharacterRequest Character { get; }
        public BackgroundSemanticSlot Slot { get; }
        public Rect SilhouetteRect { get; }
        public bool FixedBySceneLayout { get; }
    }

    public sealed class BackgroundSemanticPlacementResult
    {
        public BackgroundSemanticPlacementResult(
            IEnumerable<BackgroundSemanticPlacementAssignment>
                assignments,
            IEnumerable<string> offCameraCharacterIds,
            IEnumerable<string> diagnostics,
            bool usedFixedSceneLayout,
            bool isValid)
        {
            Assignments = Array.AsReadOnly(
                (assignments ??
                 Array.Empty<BackgroundSemanticPlacementAssignment>())
                .ToArray());
            OffCameraCharacterIds = Array.AsReadOnly(
                (offCameraCharacterIds ?? Array.Empty<string>())
                .ToArray());
            Diagnostics = Array.AsReadOnly(
                (diagnostics ?? Array.Empty<string>()).ToArray());
            UsedFixedSceneLayout = usedFixedSceneLayout;
            IsValid = isValid;
        }

        public IReadOnlyList<BackgroundSemanticPlacementAssignment>
            Assignments { get; }
        public IReadOnlyList<string> OffCameraCharacterIds { get; }
        public IReadOnlyList<string> Diagnostics { get; }
        public bool UsedFixedSceneLayout { get; }
        public bool IsValid { get; }
    }

    /// <summary>
    /// Converts approved semantic slots into a deterministic, collision-free
    /// cast layout. It does not instantiate or mutate scene objects.
    /// </summary>
    public static class BackgroundSemanticPlacementResolver
    {
        private enum PlacementFailure
        {
            None,
            NullSlot,
            OccupiedSlot,
            Role,
            Reservation,
            Geometry,
            Viewport,
            ProtectedZone,
            CharacterOverlap
        }

        private sealed class FixedPlacementSearchEntry
        {
            public FixedPlacementSearchEntry(
                BackgroundSemanticCharacterRequest character,
                BackgroundSemanticCharacterRole role,
                BackgroundSemanticSlot originalSlot,
                bool hardProtectionOverlap,
                IReadOnlyList<ShiftedSlotCandidate> candidates)
            {
                Character = character;
                Role = role;
                OriginalSlot = originalSlot;
                HardProtectionOverlap = hardProtectionOverlap;
                Candidates = candidates;
            }

            public BackgroundSemanticCharacterRequest Character { get; }
            public BackgroundSemanticCharacterRole Role { get; }
            public BackgroundSemanticSlot OriginalSlot { get; }
            public bool HardProtectionOverlap { get; }
            public IReadOnlyList<ShiftedSlotCandidate> Candidates { get; }
        }

        private readonly struct ShiftedSlotCandidate
        {
            public ShiftedSlotCandidate(
                BackgroundSemanticSlot slot,
                float horizontalOffset)
            {
                Slot = slot;
                HorizontalOffset = horizontalOffset;
            }

            public BackgroundSemanticSlot Slot { get; }
            public float HorizontalOffset { get; }
            public float Cost => Mathf.Abs(HorizontalOffset);
        }

        private const float ApprovedAnchorAdjustmentStep = .005f;
        private const float PlacementEpsilon = .00001f;

        private static readonly Rect FullImage =
            new(0f, 0f, 1f, 1f);

        public static BackgroundSemanticPlacementResult Resolve(
            BackgroundSemanticRuntimeResolution resolution,
            IEnumerable<BackgroundSemanticCharacterRequest> characters)
        {
            return Resolve(
                resolution,
                characters,
                FullImage,
                GetBackgroundAspectRatio(resolution));
        }

        public static BackgroundSemanticPlacementResult Resolve(
            BackgroundSemanticRuntimeResolution resolution,
            IEnumerable<BackgroundSemanticCharacterRequest> characters,
            Rect visibleNormalizedRect)
        {
            return Resolve(
                resolution,
                characters,
                visibleNormalizedRect,
                GetBackgroundAspectRatio(resolution));
        }

        public static BackgroundSemanticPlacementResult Resolve(
            BackgroundSemanticRuntimeResolution resolution,
            IEnumerable<BackgroundSemanticCharacterRequest> characters,
            Rect visibleNormalizedRect,
            float backgroundAspectRatio)
        {
            var diagnostics = new List<string>();
            List<BackgroundSemanticCharacterRequest> ordered =
                NormalizeRequests(characters, diagnostics);
            var assignments =
                new List<BackgroundSemanticPlacementAssignment>();
            var offCamera = new List<string>();

            BackgroundSemanticProfile profile = resolution?.Profile;
            if (profile == null ||
                resolution.Binding == null ||
                !resolution.Binding.IsApproved ||
                (resolution.Catalog != null &&
                 !resolution.Catalog.IsUsable))
            {
                offCamera.AddRange(
                    ordered.Select(character => character.CharacterId));
                diagnostics.Add(
                    "No approved background semantic profile was resolved.");
                return BuildResult(
                    assignments,
                    offCamera,
                    diagnostics,
                    usedFixedSceneLayout: false,
                    FullImage);
            }

            Rect visible = IntersectWithFullImage(
                visibleNormalizedRect);
            if (visible.width <= 0f || visible.height <= 0f)
            {
                offCamera.AddRange(
                    ordered.Select(character => character.CharacterId));
                diagnostics.Add(
                    "The visible normalized background rectangle is empty.");
                return BuildResult(
                    assignments,
                    offCamera,
                    diagnostics,
                    resolution.HasFixedSceneLayout,
                    visible);
            }

            float safeAspect = IsFinite(backgroundAspectRatio) &&
                               backgroundAspectRatio > .01f
                ? backgroundAspectRatio
                : 16f / 9f;
            var slots = profile.Slots
                .Where(slot => slot != null)
                .ToArray();
            var slotsById = slots
                .GroupBy(
                    slot => slot.Id,
                    StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() == 1)
                .ToDictionary(
                    group => group.Key,
                    group => group.Single(),
                    StringComparer.OrdinalIgnoreCase);
            var usedSlots = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            var placedCharacters = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            var approvedFixedFallbackCharacters = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            var fixedFallbackProtectedOverlap =
                new Dictionary<string, bool>(
                    StringComparer.OrdinalIgnoreCase);
            ApprovedBackgroundSemanticSceneLayout layout =
                resolution.SceneLayout;
            if (layout != null && !layout.IsValidFor(profile))
            {
                diagnostics.Add(
                    "The fixed scene layout failed approved profile " +
                    "integrity validation and was ignored.");
                layout = null;
            }
            var explicitOffCamera = new HashSet<string>(
                layout?.OffCameraCharacterIds ??
                Array.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            foreach (BackgroundSemanticCharacterRequest character in ordered)
            {
                if (!explicitOffCamera.Contains(character.CharacterId))
                    continue;

                offCamera.Add(character.CharacterId);
                placedCharacters.Add(character.CharacterId);
            }

            bool placedJointFixedLayout = false;
            if (layout != null &&
                TryBuildAdjustedFixedLayout(
                    profile,
                    layout,
                    ordered,
                    explicitOffCamera,
                    slotsById,
                    visible,
                    safeAspect,
                    out IReadOnlyList<
                        BackgroundSemanticPlacementAssignment>
                        fixedAssignments))
            {
                foreach (BackgroundSemanticPlacementAssignment assignment in
                         fixedAssignments)
                {
                    assignments.Add(assignment);
                    usedSlots.Add(assignment.Slot.Id);
                    placedCharacters.Add(
                        assignment.Character.CharacterId);

                    BackgroundSemanticCharacterSlotBinding binding =
                        layout.Assignments.First(candidate =>
                            candidate != null &&
                            string.Equals(
                                candidate.CharacterId,
                                assignment.Character.CharacterId,
                                StringComparison.OrdinalIgnoreCase));
                    BackgroundSemanticSlot original =
                        slotsById[binding.SlotId];
                    float adjustment =
                        assignment.Slot.Anchor.x - original.Anchor.x;
                    if (Mathf.Abs(adjustment) > PlacementEpsilon)
                    {
                        diagnostics.Add(
                            $"Approved fixed slot '{assignment.Slot.Id}' " +
                            $"for '{assignment.Character.CharacterId}' " +
                            $"was shifted horizontally by " +
                            $"{adjustment:+0.000;-0.000} to preserve the " +
                            "collision-free approved cast.");
                    }
                }

                placedJointFixedLayout = true;
            }

            if (layout != null && !placedJointFixedLayout)
            {
                foreach (BackgroundSemanticCharacterRequest character in
                         ordered)
                {
                    if (placedCharacters.Contains(character.CharacterId))
                        continue;

                    BackgroundSemanticCharacterSlotBinding fixedBinding =
                        layout.Assignments.FirstOrDefault(candidate =>
                            candidate != null &&
                            string.Equals(
                                candidate.CharacterId,
                                character.CharacterId,
                                StringComparison.OrdinalIgnoreCase));
                    if (fixedBinding == null)
                        continue;
                    if (!slotsById.TryGetValue(
                            fixedBinding.SlotId,
                            out BackgroundSemanticSlot slot))
                    {
                        diagnostics.Add(
                            $"Fixed slot '{fixedBinding.SlotId}' for " +
                            $"'{character.CharacterId}' does not exist.");
                        continue;
                    }

                    if (!TryPlace(
                            profile,
                            character,
                            fixedBinding.Role,
                            slot,
                            visible,
                            safeAspect,
                            assignments,
                            usedSlots,
                            fixedBySceneLayout: true,
                            allowProtectedZoneOverlap: true,
                            out PlacementFailure failure,
                            out string reason))
                    {
                        diagnostics.Add(
                            $"Fixed placement for " +
                            $"'{character.CharacterId}' was rejected: " +
                            reason);
                        if (failure == PlacementFailure.Viewport ||
                            failure ==
                            PlacementFailure.CharacterOverlap)
                        {
                            approvedFixedFallbackCharacters.Add(
                                character.CharacterId);
                            fixedFallbackProtectedOverlap[
                                character.CharacterId] =
                                fixedBinding.HardProtectionOverlap;
                        }
                        continue;
                    }

                    placedCharacters.Add(character.CharacterId);
                }
            }

            foreach (BackgroundSemanticCharacterRequest character in ordered)
            {
                if (placedCharacters.Contains(character.CharacterId))
                    continue;

                bool isApprovedFixedFallback =
                    approvedFixedFallbackCharacters.Contains(
                        character.CharacterId);
                bool allowApprovedProtectedFallback =
                    isApprovedFixedFallback &&
                    fixedFallbackProtectedOverlap.TryGetValue(
                        character.CharacterId,
                        out bool approvedProtectionOverlap) &&
                    approvedProtectionOverlap;
                BackgroundSemanticSlot selected = OrderSlots(
                        slots,
                        character)
                    .FirstOrDefault(slot =>
                        CanPlace(
                            profile,
                            character,
                            character.Role,
                            slot,
                            visible,
                            safeAspect,
                            assignments,
                            usedSlots,
                            allowProtectedZoneOverlap:
                                allowApprovedProtectedFallback,
                            out _,
                            out _));
                string reason = string.Empty;
                if (selected == null ||
                    !TryPlace(
                        profile,
                        character,
                        character.Role,
                        selected,
                        visible,
                        safeAspect,
                        assignments,
                        usedSlots,
                        fixedBySceneLayout: false,
                        allowProtectedZoneOverlap:
                            allowApprovedProtectedFallback,
                        out _,
                        out reason))
                {
                    offCamera.Add(character.CharacterId);
                    diagnostics.Add(
                        $"No valid semantic slot remained for " +
                        $"'{character.CharacterId}'" +
                        (string.IsNullOrEmpty(reason)
                            ? "."
                            : $": {reason}"));
                    continue;
                }

                if (isApprovedFixedFallback)
                {
                    diagnostics.Add(
                        $"'{character.CharacterId}' used an approved " +
                        "alternate slot after its fixed slot was cropped " +
                        "or overlapped another character.");
                }
                placedCharacters.Add(character.CharacterId);
            }

            return BuildResult(
                assignments,
                offCamera,
                diagnostics,
                layout != null,
                visible);
        }

        public static Rect CalculateSilhouetteRect(
            BackgroundSemanticSlot slot,
            AmbientWorldCharacterAsset characterAsset,
            float backgroundAspectRatio)
        {
            if (slot == null)
                return default;

            float safeAspect =
                IsFinite(backgroundAspectRatio) &&
                backgroundAspectRatio > .01f
                    ? backgroundAspectRatio
                    : 16f / 9f;
            float visibleSpan = Mathf.Max(
                .5f,
                characterAsset.VisibleVerticalSpan);
            float cellHeight = slot.NormalizedHeight / visibleSpan;
            float visibleBottom = slot.Anchor.y;
            float cellWidth =
                cellHeight *
                Mathf.Max(.01f, characterAsset.CellAspectRatio) /
                safeAspect;
            float cellLeft = slot.Anchor.x - cellWidth * .5f;
            bool mirror = slot.Facing switch
            {
                BackgroundSemanticFacing.Left => true,
                BackgroundSemanticFacing.Right => false,
                _ => slot.Anchor.x >= .5f
            };
            float visibleLeftMargin = mirror
                ? characterAsset.VisibleRightMargin
                : characterAsset.VisibleLeftMargin;
            float alphaLeft =
                cellLeft +
                cellWidth * visibleLeftMargin;
            float alphaWidth =
                cellWidth * characterAsset.VisibleHorizontalSpan;
            float approvedWidth = Mathf.Max(
                .01f,
                slot.FootprintSize.x);
            float reviewSilhouetteWidth =
                slot.NormalizedHeight * .28f / safeAspect;
            float visibleWidth = Mathf.Min(
                alphaWidth,
                approvedWidth,
                reviewSilhouetteWidth);
            float visibleLeft =
                alphaLeft +
                (alphaWidth - visibleWidth) * .5f;
            return new Rect(
                visibleLeft,
                visibleBottom,
                visibleWidth,
                slot.NormalizedHeight);
        }

        public static bool Validate(
            IEnumerable<BackgroundSemanticPlacementAssignment>
                assignments,
            Rect visibleNormalizedRect,
            out string diagnostic)
        {
            BackgroundSemanticPlacementAssignment[] values =
                (assignments ??
                 Array.Empty<BackgroundSemanticPlacementAssignment>())
                .Where(value => value != null)
                .ToArray();
            Rect visible = IntersectWithFullImage(
                visibleNormalizedRect);
            var characters = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            var slots = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            for (int index = 0; index < values.Length; index++)
            {
                BackgroundSemanticPlacementAssignment value =
                    values[index];
                if (string.IsNullOrEmpty(value.Character.CharacterId) ||
                    value.Slot == null ||
                    !characters.Add(value.Character.CharacterId) ||
                    !slots.Add(value.Slot.Id))
                {
                    diagnostic =
                        "Character and slot IDs must be non-empty and unique.";
                    return false;
                }

                if (!ContainsRect(
                        visible,
                        value.SilhouetteRect))
                {
                    diagnostic =
                        $"'{value.Character.CharacterId}' falls outside " +
                        "the visible normalized background rectangle.";
                    return false;
                }

                for (int previous = 0; previous < index; previous++)
                {
                    if (value.SilhouetteRect.Overlaps(
                            values[previous].SilhouetteRect,
                            true))
                    {
                        diagnostic =
                            $"'{value.Character.CharacterId}' overlaps " +
                            $"'{values[previous].Character.CharacterId}'.";
                        return false;
                    }
                }
            }

            diagnostic = string.Empty;
            return true;
        }

        public static bool IsApprovedFixedSlotAllowed(
            BackgroundSemanticProfile profile,
            BackgroundSemanticSlot slot)
        {
            return IsSlotAnchorAllowed(
                profile,
                slot,
                allowProtectedZone: true);
        }

        public static bool IsGenericSlotAllowed(
            BackgroundSemanticProfile profile,
            BackgroundSemanticSlot slot)
        {
            return IsSlotAnchorAllowed(
                profile,
                slot,
                allowProtectedZone: false);
        }

        private static List<BackgroundSemanticCharacterRequest>
            NormalizeRequests(
                IEnumerable<BackgroundSemanticCharacterRequest>
                    characters,
                ICollection<string> diagnostics)
        {
            var unique = new Dictionary<
                string,
                BackgroundSemanticCharacterRequest>(
                StringComparer.OrdinalIgnoreCase);
            foreach (BackgroundSemanticCharacterRequest character in
                     characters ??
                     Array.Empty<BackgroundSemanticCharacterRequest>())
            {
                if (string.IsNullOrEmpty(character.CharacterId))
                {
                    diagnostics.Add(
                        "A placement request with an empty character ID " +
                        "was ignored.");
                    continue;
                }

                if (!unique.TryAdd(
                        character.CharacterId,
                        character))
                {
                    diagnostics.Add(
                        $"Duplicate placement request for " +
                        $"'{character.CharacterId}' was ignored.");
                }
            }

            return unique.Values
                .OrderByDescending(character =>
                    Priority(character.Role))
                .ThenBy(
                    character => character.CharacterId,
                    StringComparer.Ordinal)
                .ToList();
        }

        private static IEnumerable<BackgroundSemanticSlot> OrderSlots(
            IEnumerable<BackgroundSemanticSlot> slots,
            BackgroundSemanticCharacterRequest character)
        {
            return slots
                .Where(slot => slot != null)
                .OrderByDescending(slot =>
                    string.Equals(
                        slot.ReservationKey,
                        character.CharacterId,
                        StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(slot =>
                    IsExactRoleSlot(slot, character.Role))
                .ThenByDescending(slot => slot.Confidence.Score)
                .ThenBy(slot =>
                    character.Role ==
                    BackgroundSemanticCharacterRole.Context
                        ? -Mathf.Abs(slot.Anchor.x - .5f)
                        : Mathf.Abs(slot.Anchor.x - .5f))
                .ThenBy(slot => slot.Id, StringComparer.Ordinal);
        }

        private static bool TryBuildAdjustedFixedLayout(
            BackgroundSemanticProfile profile,
            ApprovedBackgroundSemanticSceneLayout layout,
            IEnumerable<BackgroundSemanticCharacterRequest> ordered,
            ISet<string> explicitOffCamera,
            IReadOnlyDictionary<string, BackgroundSemanticSlot> slotsById,
            Rect visible,
            float backgroundAspectRatio,
            out IReadOnlyList<BackgroundSemanticPlacementAssignment>
                result)
        {
            var entries = new List<FixedPlacementSearchEntry>();
            foreach (BackgroundSemanticCharacterRequest character in ordered)
            {
                if (explicitOffCamera.Contains(character.CharacterId))
                    continue;

                BackgroundSemanticCharacterSlotBinding binding =
                    layout.Assignments.FirstOrDefault(candidate =>
                        candidate != null &&
                        string.Equals(
                            candidate.CharacterId,
                            character.CharacterId,
                            StringComparison.OrdinalIgnoreCase));
                if (binding == null)
                    continue;
                if (!slotsById.TryGetValue(
                        binding.SlotId,
                        out BackgroundSemanticSlot originalSlot))
                {
                    result =
                        Array.Empty<
                            BackgroundSemanticPlacementAssignment>();
                    return false;
                }

                entries.Add(
                    new FixedPlacementSearchEntry(
                        character,
                        binding.Role,
                        originalSlot,
                        binding.HardProtectionOverlap,
                        BuildShiftedSlotCandidates(originalSlot)));
            }

            if (entries.Count == 0)
            {
                result =
                    Array.Empty<
                        BackgroundSemanticPlacementAssignment>();
                return true;
            }

            var working =
                new List<BackgroundSemanticPlacementAssignment>(
                    entries.Count);
            var usedSlots = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            List<BackgroundSemanticPlacementAssignment> best = null;
            float bestCost = float.PositiveInfinity;
            SearchAdjustedFixedLayout(
                profile,
                entries,
                visible,
                backgroundAspectRatio,
                useApprovedProtectionExceptions: false,
                entryIndex: 0,
                currentCost: 0f,
                working,
                usedSlots,
                ref bestCost,
                ref best);

            if (best == null)
            {
                bestCost = float.PositiveInfinity;
                SearchAdjustedFixedLayout(
                    profile,
                    entries,
                    visible,
                    backgroundAspectRatio,
                    useApprovedProtectionExceptions: true,
                    entryIndex: 0,
                    currentCost: 0f,
                    working,
                    usedSlots,
                    ref bestCost,
                    ref best);
            }

            result = best ??
                     (IReadOnlyList<
                         BackgroundSemanticPlacementAssignment>)
                     Array.Empty<
                         BackgroundSemanticPlacementAssignment>();
            return best != null;
        }

        private static void SearchAdjustedFixedLayout(
            BackgroundSemanticProfile profile,
            IReadOnlyList<FixedPlacementSearchEntry> entries,
            Rect visible,
            float backgroundAspectRatio,
            bool useApprovedProtectionExceptions,
            int entryIndex,
            float currentCost,
            IList<BackgroundSemanticPlacementAssignment> working,
            ISet<string> usedSlots,
            ref float bestCost,
            ref List<BackgroundSemanticPlacementAssignment> best)
        {
            if (currentCost >= bestCost - PlacementEpsilon)
                return;

            if (entryIndex >= entries.Count)
            {
                bestCost = currentCost;
                best = working.ToList();
                return;
            }

            FixedPlacementSearchEntry entry = entries[entryIndex];
            foreach (ShiftedSlotCandidate candidate in entry.Candidates)
            {
                float nextCost = currentCost + candidate.Cost;
                if (nextCost >= bestCost - PlacementEpsilon)
                    continue;

                if (!CanPlace(
                        profile,
                        entry.Character,
                        entry.Role,
                        candidate.Slot,
                        visible,
                        backgroundAspectRatio,
                        working,
                        usedSlots,
                        useApprovedProtectionExceptions,
                        out _,
                        out _))
                {
                    continue;
                }

                Rect silhouette = CalculateSilhouetteRect(
                    candidate.Slot,
                    entry.Character.CharacterAsset,
                    backgroundAspectRatio);
                working.Add(
                    new BackgroundSemanticPlacementAssignment(
                        entry.Character,
                        candidate.Slot,
                        silhouette,
                        fixedBySceneLayout: true));
                usedSlots.Add(candidate.Slot.Id);

                SearchAdjustedFixedLayout(
                    profile,
                    entries,
                    visible,
                    backgroundAspectRatio,
                    useApprovedProtectionExceptions,
                    entryIndex + 1,
                    nextCost,
                    working,
                    usedSlots,
                    ref bestCost,
                    ref best);

                usedSlots.Remove(candidate.Slot.Id);
                working.RemoveAt(working.Count - 1);
            }
        }

        private static IReadOnlyList<ShiftedSlotCandidate>
            BuildShiftedSlotCandidates(BackgroundSemanticSlot original)
        {
            float maxOffset = Mathf.Max(
                0f,
                original.FootprintSize.x * .5f);
            var offsets = new List<float> { 0f };
            for (float distance = ApprovedAnchorAdjustmentStep;
                 distance < maxOffset - PlacementEpsilon;
                 distance += ApprovedAnchorAdjustmentStep)
            {
                offsets.Add(-distance);
                offsets.Add(distance);
            }

            if (maxOffset > PlacementEpsilon)
            {
                AddOffsetIfMissing(offsets, -maxOffset);
                AddOffsetIfMissing(offsets, maxOffset);
            }

            return offsets
                .Select(offset => new ShiftedSlotCandidate(
                    CloneSlotWithHorizontalOffset(
                        original,
                        offset),
                    offset))
                .ToArray();
        }

        private static void AddOffsetIfMissing(
            ICollection<float> offsets,
            float value)
        {
            if (offsets.Any(existing =>
                    Mathf.Abs(existing - value) <=
                    PlacementEpsilon))
            {
                return;
            }

            offsets.Add(value);
        }

        private static BackgroundSemanticSlot
            CloneSlotWithHorizontalOffset(
                BackgroundSemanticSlot source,
                float horizontalOffset)
        {
            return new BackgroundSemanticSlot(
                source.Id,
                new Vector2(
                    source.Anchor.x + horizontalOffset,
                    source.Anchor.y),
                source.Depth01,
                source.NormalizedHeight,
                source.FootprintSize,
                source.Facing,
                source.AllowedRoles,
                source.Origin,
                source.ReservationKey,
                source.Confidence);
        }

        private static bool TryPlace(
            BackgroundSemanticProfile profile,
            BackgroundSemanticCharacterRequest character,
            BackgroundSemanticCharacterRole role,
            BackgroundSemanticSlot slot,
            Rect visible,
            float backgroundAspectRatio,
            ICollection<BackgroundSemanticPlacementAssignment>
                assignments,
            ISet<string> usedSlots,
            bool fixedBySceneLayout,
            bool allowProtectedZoneOverlap,
            out PlacementFailure failure,
            out string reason)
        {
            if (!CanPlace(
                    profile,
                    character,
                    role,
                    slot,
                    visible,
                    backgroundAspectRatio,
                    assignments,
                    usedSlots,
                    allowProtectedZoneOverlap,
                    out failure,
                    out reason))
            {
                return false;
            }

            Rect silhouette = CalculateSilhouetteRect(
                slot,
                character.CharacterAsset,
                backgroundAspectRatio);
            assignments.Add(
                new BackgroundSemanticPlacementAssignment(
                    character,
                    slot,
                    silhouette,
                    fixedBySceneLayout));
            usedSlots.Add(slot.Id);
            return true;
        }

        private static bool CanPlace(
            BackgroundSemanticProfile profile,
            BackgroundSemanticCharacterRequest character,
            BackgroundSemanticCharacterRole role,
            BackgroundSemanticSlot slot,
            Rect visible,
            float backgroundAspectRatio,
            IEnumerable<BackgroundSemanticPlacementAssignment>
                assignments,
            ISet<string> usedSlots,
            bool allowProtectedZoneOverlap,
            out PlacementFailure failure,
            out string reason)
        {
            failure = PlacementFailure.None;
            reason = string.Empty;
            if (slot == null)
            {
                failure = PlacementFailure.NullSlot;
                reason = "slot is null.";
                return false;
            }

            if (usedSlots.Contains(slot.Id))
            {
                failure = PlacementFailure.OccupiedSlot;
                reason = "slot is already occupied.";
                return false;
            }

            BackgroundSemanticSlotRole required = RequiredSlotRole(role);
            if ((slot.AllowedRoles & required) == 0)
            {
                failure = PlacementFailure.Role;
                reason = "slot does not allow the character role.";
                return false;
            }

            if (!string.IsNullOrEmpty(slot.ReservationKey) &&
                !string.Equals(
                    slot.ReservationKey.Trim(),
                    character.CharacterId,
                    StringComparison.OrdinalIgnoreCase))
            {
                failure = PlacementFailure.Reservation;
                reason = "slot is reserved for another character.";
                return false;
            }

            bool geometryAllowed =
                allowProtectedZoneOverlap
                    ? IsApprovedFixedSlotAllowed(profile, slot)
                    : IsGenericSlotAllowed(profile, slot);
            if (!geometryAllowed)
            {
                failure = PlacementFailure.Geometry;
                reason =
                    "slot is outside approved walkable geometry or " +
                    "intersects a restricted zone.";
                return false;
            }

            Rect silhouette = CalculateSilhouetteRect(
                slot,
                character.CharacterAsset,
                backgroundAspectRatio);
            if (!ContainsRect(visible, silhouette))
            {
                failure = PlacementFailure.Viewport;
                reason =
                    "character silhouette falls outside the visible image.";
                return false;
            }

            if (!allowProtectedZoneOverlap &&
                profile.Zones.Any(zone =>
                    zone != null &&
                    zone.Enabled &&
                    zone.Kind ==
                    BackgroundSemanticZoneKind.Protected &&
                    silhouette.Overlaps(
                        zone.ExpandedRect,
                        true)))
            {
                failure = PlacementFailure.ProtectedZone;
                reason =
                    "character silhouette overlaps a protected clue zone.";
                return false;
            }

            if (assignments.Any(existing =>
                    existing != null &&
                    silhouette.Overlaps(
                        existing.SilhouetteRect,
                        true)))
            {
                failure = PlacementFailure.CharacterOverlap;
                reason = "character silhouette overlaps another character.";
                return false;
            }

            return true;
        }

        private static bool IsSlotAnchorAllowed(
            BackgroundSemanticProfile profile,
            BackgroundSemanticSlot slot,
            bool allowProtectedZone)
        {
            if (profile == null ||
                slot == null ||
                !IsFinite(slot.Anchor.x) ||
                !IsFinite(slot.Anchor.y) ||
                slot.Anchor.x < 0f ||
                slot.Anchor.x > 1f ||
                slot.Anchor.y < 0f ||
                slot.Anchor.y > 1f ||
                !profile.WalkablePolygons.Any(polygon =>
                    polygon != null &&
                    polygon.Vertices.Count >= 3 &&
                    polygon.Contains(slot.Anchor)))
            {
                return false;
            }

            return !profile.Zones.Any(zone =>
                zone != null &&
                zone.Enabled &&
                zone.Kind != BackgroundSemanticZoneKind.Uncertain &&
                (!allowProtectedZone ||
                 zone.Kind == BackgroundSemanticZoneKind.Forbidden) &&
                ContainsPoint(zone.ExpandedRect, slot.Anchor));
        }

        private static bool ContainsPoint(Rect rect, Vector2 point)
        {
            const float Epsilon = .00001f;
            return point.x + Epsilon >= rect.xMin &&
                   point.x <= rect.xMax + Epsilon &&
                   point.y + Epsilon >= rect.yMin &&
                   point.y <= rect.yMax + Epsilon;
        }

        private static BackgroundSemanticPlacementResult BuildResult(
            IEnumerable<BackgroundSemanticPlacementAssignment>
                assignments,
            IEnumerable<string> offCamera,
            ICollection<string> diagnostics,
            bool usedFixedSceneLayout,
            Rect visible)
        {
            BackgroundSemanticPlacementAssignment[] values =
                assignments.ToArray();
            bool valid = Validate(
                values,
                visible,
                out string validationDiagnostic);
            if (!valid &&
                !string.IsNullOrEmpty(validationDiagnostic))
            {
                diagnostics.Add(validationDiagnostic);
            }

            return new BackgroundSemanticPlacementResult(
                values,
                offCamera,
                diagnostics,
                usedFixedSceneLayout,
                valid);
        }

        private static bool IsExactRoleSlot(
            BackgroundSemanticSlot slot,
            BackgroundSemanticCharacterRole role)
        {
            BackgroundSemanticSlotRole required = RequiredSlotRole(role);
            return slot.AllowedRoles == required;
        }

        private static BackgroundSemanticSlotRole RequiredSlotRole(
            BackgroundSemanticCharacterRole role)
        {
            return role switch
            {
                BackgroundSemanticCharacterRole.Focus =>
                    BackgroundSemanticSlotRole.Focus,
                BackgroundSemanticCharacterRole.Main =>
                    BackgroundSemanticSlotRole.Main,
                _ => BackgroundSemanticSlotRole.Ambient
            };
        }

        private static int Priority(
            BackgroundSemanticCharacterRole role)
        {
            return role switch
            {
                BackgroundSemanticCharacterRole.Focus => 2,
                BackgroundSemanticCharacterRole.Main => 1,
                _ => 0
            };
        }

        private static bool ContainsRect(Rect outer, Rect inner)
        {
            const float Epsilon = .00001f;
            return IsFinite(inner.xMin) &&
                   IsFinite(inner.yMin) &&
                   IsFinite(inner.xMax) &&
                   IsFinite(inner.yMax) &&
                   inner.width > 0f &&
                   inner.height > 0f &&
                   inner.xMin + Epsilon >= outer.xMin &&
                   inner.yMin + Epsilon >= outer.yMin &&
                   inner.xMax <= outer.xMax + Epsilon &&
                   inner.yMax <= outer.yMax + Epsilon;
        }

        private static Rect IntersectWithFullImage(Rect value)
        {
            if (!IsFinite(value.xMin) ||
                !IsFinite(value.yMin) ||
                !IsFinite(value.xMax) ||
                !IsFinite(value.yMax))
            {
                return default;
            }

            return Rect.MinMaxRect(
                Mathf.Clamp01(Mathf.Min(value.xMin, value.xMax)),
                Mathf.Clamp01(Mathf.Min(value.yMin, value.yMax)),
                Mathf.Clamp01(Mathf.Max(value.xMin, value.xMax)),
                Mathf.Clamp01(Mathf.Max(value.yMin, value.yMax)));
        }

        private static float GetBackgroundAspectRatio(
            BackgroundSemanticRuntimeResolution resolution)
        {
            Sprite sprite = resolution?.Binding?.SourceSprite;
            return sprite != null &&
                   sprite.rect.height > Mathf.Epsilon
                ? sprite.rect.width / sprite.rect.height
                : 16f / 9f;
        }

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
