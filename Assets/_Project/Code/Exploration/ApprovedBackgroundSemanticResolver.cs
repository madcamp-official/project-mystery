using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Wake.Exploration
{
    public sealed class BackgroundSemanticRuntimeResolution
    {
        public BackgroundSemanticRuntimeResolution(
            ApprovedBackgroundSemanticBinding binding,
            ApprovedBackgroundSemanticSceneLayout sceneLayout = null,
            ApprovedBackgroundSemanticCatalog catalog = null)
        {
            Binding = binding;
            SceneLayout = sceneLayout;
            Catalog = catalog;
        }

        public ApprovedBackgroundSemanticBinding Binding { get; }
        public BackgroundSemanticProfile Profile => Binding?.Profile;
        public ApprovedBackgroundSemanticSceneLayout SceneLayout { get; }
        public ApprovedBackgroundSemanticCatalog Catalog { get; }
        public bool HasFixedSceneLayout => SceneLayout != null;
        public string CastFingerprint =>
            SceneLayout?.CastFingerprint ?? string.Empty;
        public bool AllowsApprovedWarningExceptions =>
            Catalog != null &&
            Catalog.ApprovedWarnings &&
            Catalog.ApprovedWarningCount > 0;
    }

    /// <summary>
    /// Resolves only reviewed semantic data. Returning false is intentional:
    /// callers can keep their existing placement path as a legacy fallback.
    /// </summary>
    public static class ApprovedBackgroundSemanticResolver
    {
        public const string ResourcePath =
            "BackgroundSemantics/ApprovedBackgroundSemanticCatalog";

        private static ApprovedBackgroundSemanticCatalog cachedCatalog;
        private static bool hasLoadedCatalog;

        public static string BuildSerializedVariantKey(Sprite sprite) =>
            sprite != null
                ? $"serialized:{sprite.name}"
                : string.Empty;

        public static bool TryResolve(
            string locationCode,
            string variantKey,
            Sprite sourceSprite,
            out BackgroundSemanticRuntimeResolution resolution)
        {
            return TryResolve(
                locationCode,
                variantKey,
                sourceSprite,
                sceneId: string.Empty,
                expectedSourceImageHash: string.Empty,
                expectedCastFingerprint: string.Empty,
                out resolution);
        }

        public static bool TryResolve(
            string locationCode,
            string variantKey,
            Sprite sourceSprite,
            string sceneId,
            out BackgroundSemanticRuntimeResolution resolution)
        {
            return TryResolve(
                locationCode,
                variantKey,
                sourceSprite,
                sceneId,
                expectedSourceImageHash: string.Empty,
                expectedCastFingerprint: string.Empty,
                out resolution);
        }

        public static bool TryResolve(
            string locationCode,
            string variantKey,
            Sprite sourceSprite,
            string sceneId,
            string expectedSourceImageHash,
            out BackgroundSemanticRuntimeResolution resolution)
        {
            return TryResolve(
                locationCode,
                variantKey,
                sourceSprite,
                sceneId,
                expectedSourceImageHash,
                expectedCastFingerprint: string.Empty,
                out resolution);
        }

        public static bool TryResolve(
            string locationCode,
            string variantKey,
            Sprite sourceSprite,
            string sceneId,
            string expectedSourceImageHash,
            string expectedCastFingerprint,
            out BackgroundSemanticRuntimeResolution resolution)
        {
            resolution = null;
            ApprovedBackgroundSemanticCatalog catalog = GetCatalog();
            if (catalog == null ||
                !catalog.IsUsable ||
                sourceSprite == null)
            {
                return false;
            }

            string normalizedLocation =
                BackgroundSemanticCharacterSlotBinding.NormalizeCode(
                    locationCode);
            string normalizedVariant = NormalizeVariant(
                variantKey,
                sourceSprite);
            if (string.IsNullOrEmpty(normalizedLocation) ||
                string.IsNullOrEmpty(normalizedVariant))
            {
                return false;
            }

            ApprovedBackgroundSemanticBinding[] matches =
                catalog.Bindings
                    .Where(binding =>
                        BindingMatches(
                            binding,
                            normalizedLocation,
                            normalizedVariant,
                            sourceSprite))
                    .ToArray();
            if (matches.Length != 1)
                return false;

            ApprovedBackgroundSemanticBinding binding = matches[0];
            if (!IsValidBinding(
                    binding,
                    normalizedLocation,
                    normalizedVariant,
                    expectedSourceImageHash))
            {
                return false;
            }

            ApprovedBackgroundSemanticSceneLayout sceneLayout = null;
            string normalizedScene =
                BackgroundSemanticCharacterSlotBinding.NormalizeCode(
                    sceneId);
            string normalizedCastFingerprint =
                expectedCastFingerprint?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(normalizedCastFingerprint) &&
                string.IsNullOrEmpty(normalizedScene))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(normalizedScene))
            {
                ApprovedBackgroundSemanticSceneLayout[] layouts =
                    catalog.SceneLayouts
                        .Where(layout =>
                            layout != null &&
                            layout.Matches(
                                normalizedScene,
                                normalizedLocation,
                                normalizedVariant,
                                binding.SourceImageHash))
                        .ToArray();
                if (layouts.Length > 1)
                    return false;
                if (layouts.Length == 0 &&
                    !string.IsNullOrEmpty(
                        normalizedCastFingerprint))
                {
                    return false;
                }
                if (layouts.Length == 1)
                {
                    if (!layouts[0].IsValidFor(
                            binding.Profile,
                            normalizedCastFingerprint))
                        return false;
                    sceneLayout = layouts[0];
                }
            }

            resolution = new BackgroundSemanticRuntimeResolution(
                binding,
                sceneLayout,
                catalog);
            return true;
        }

        public static void SetCatalogForTests(
            ApprovedBackgroundSemanticCatalog catalog)
        {
            cachedCatalog = catalog;
            hasLoadedCatalog = true;
        }

        public static void ResetCacheForTests()
        {
            cachedCatalog = null;
            hasLoadedCatalog = false;
        }

        private static ApprovedBackgroundSemanticCatalog GetCatalog()
        {
            if (!hasLoadedCatalog)
            {
                cachedCatalog =
                    Resources.Load<ApprovedBackgroundSemanticCatalog>(
                        ResourcePath);
                hasLoadedCatalog = true;
            }

            return cachedCatalog;
        }

        private static string NormalizeVariant(
            string variantKey,
            Sprite sourceSprite)
        {
            string normalized = variantKey?.Trim() ?? string.Empty;
            return !string.IsNullOrEmpty(normalized)
                ? normalized
                : BuildSerializedVariantKey(sourceSprite);
        }

        private static bool BindingMatches(
            ApprovedBackgroundSemanticBinding binding,
            string locationCode,
            string variantKey,
            Sprite sourceSprite)
        {
            return binding != null &&
                   string.Equals(
                       binding.LocationCode,
                       locationCode,
                       StringComparison.Ordinal) &&
                   string.Equals(
                       binding.VariantKey,
                       variantKey,
                       StringComparison.Ordinal) &&
                   binding.SourceSprite == sourceSprite;
        }

        private static bool IsValidBinding(
            ApprovedBackgroundSemanticBinding binding,
            string locationCode,
            string variantKey,
            string expectedSourceImageHash)
        {
            BackgroundSemanticProfile profile = binding?.Profile;
            if (binding == null ||
                !binding.IsApproved ||
                profile == null ||
                !string.Equals(
                    binding.LocationCode,
                    locationCode,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    binding.VariantKey,
                    variantKey,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    profile.LocationCode?.Trim().ToUpperInvariant(),
                    locationCode,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    profile.VariantId?.Trim() ?? string.Empty,
                    variantKey,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    binding.SourceImageHash,
                    profile.SourceImageHash?.Trim() ?? string.Empty,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string expected =
                expectedSourceImageHash?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(expected) &&
                !string.Equals(
                    binding.SourceImageHash,
                    expected,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            IReadOnlyList<BackgroundSemanticDiagnostic> diagnostics =
                BackgroundSemanticValidator.Validate(
                    profile,
                    expectedSourceImageHash:
                        string.IsNullOrEmpty(expected)
                            ? binding.SourceImageHash
                            : expected);
            // Stage-one footprint rectangles approximate silhouette size.
            // Runtime placement validates the foot anchor against semantic
            // polygons/zones and validates the silhouette separately.
            return !diagnostics.Any(diagnostic =>
                diagnostic.Severity ==
                BackgroundSemanticDiagnosticSeverity.Error &&
                diagnostic.Code !=
                BackgroundSemanticDiagnosticCode
                    .SlotOutsideWalkablePolygon &&
                diagnostic.Code !=
                BackgroundSemanticDiagnosticCode
                    .SlotIntersectsRestrictedZone);
        }
    }
}
