using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Wake.Exploration
{
    public sealed class LocationBackgroundVariantBinding
    {
        public LocationBackgroundVariantBinding(
            string logicalLocationCode,
            string resourceName,
            params string[] sceneIds)
        {
            LogicalLocationCode =
                logicalLocationCode?.Trim().ToUpperInvariant() ??
                string.Empty;
            ResourceName = resourceName?.Trim() ?? string.Empty;
            SceneIds = sceneIds?
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim().ToUpperInvariant())
                .ToArray() ?? Array.Empty<string>();
        }

        public string LogicalLocationCode { get; }
        public string ResourceName { get; }
        public string ResourceKey =>
            $"{LocationBackgroundVariantCatalog.ResourceRoot}/{ResourceName}";
        public IReadOnlyList<string> SceneIds { get; }
        public bool IsDefault => SceneIds.Count == 0;
    }

    /// <summary>
    /// Selects approved location art without replacing the original serialized
    /// location sprite. Exact scene bindings win; a location default is used
    /// for free travel and as the fallback for that room.
    /// </summary>
    public static class LocationBackgroundVariantCatalog
    {
        public const string ResourceRoot = "LocationBackgroundVariants";

        private static readonly LocationBackgroundVariantBinding[] Entries =
        {
            Default("GANGWAY", "bg_gangway_default_luggage"),
            Default("ATRIUM", "bg_atrium_default_champagne"),
            Default("BALLROOM", "bg_ballroom_default_mask"),
            Default("BRIDGE", "bg_bridge_d3_day"),
            Default(
                "CABIN_DANIEL",
                "bg_cabin_daniel_d2_late_afternoon"),
            Default("INTERVIEW", "bg_interview_d5_day"),

            Default(
                "CABIN_CLAIRE",
                "bg_cabin_claire_default_morning"),
            Scene(
                "CABIN_CLAIRE",
                "bg_cabin_claire_d5_smoke",
                "D5-01"),
            Scene(
                "CABIN_CLAIRE",
                "bg_cabin_claire_d5_dismantled",
                "D5-02"),

            Default("SERVICE7", "bg_crew_stairs_default"),
            Default("CREW_STAIRS", "bg_crew_stairs_default"),
            Scene(
                "CREW_STAIRS",
                "bg_crew_stairs_d4_wet",
                "D4-02"),
            Scene(
                "CREW_STAIRS",
                "bg_crew_stairs_d4_reconstruction",
                "D4-03"),

            Default("HORIZON", "bg_horizon_cleared_day"),
            Scene(
                "HORIZON",
                "bg_horizon_d1_discovery",
                "D1-06"),
            Scene(
                "HORIZON",
                "bg_horizon_d8_finale",
                "D8-01"),

            Default("MEDBAY", "bg_medbay_baseline"),
            Scene(
                "MEDBAY",
                "bg_medbay_forensic",
                "D4-04",
                "D6-04"),
            Scene(
                "MEDBAY",
                "bg_medbay_dna",
                "D7-02"),

            Default("NEWS_LOUNGE", "bg_news_lounge_d3"),
            Scene(
                "NEWS_LOUNGE",
                "bg_news_lounge_d6_evidence_board",
                "D6-05"),
            Scene("VAULT", "bg_vault_d7_damaged", "D7-01"),
            Scene("PORT", "bg_port_d8_epilogue", "D8-03"),
            Scene(
                "PROMENADE",
                "bg_promenade_d3_night",
                "D3-05"),
            Scene(
                "OPEN_DECK",
                "bg_open_deck_d8_morning",
                "D8-02"),
            Default(
                "SERVICE_RAIL",
                "bg_service_rail_d6_subtle"),
            Default(
                "BALLAST_CONTROL_ANNEX",
                "bg_ballast_annex_d6_subtle")
        };

        private static readonly PersistentVariant[] PersistentEntries =
        {
            Persist(
                "CABIN_CLAIRE",
                "D5-02",
                "bg_cabin_claire_d5_dismantled"),
            Persist(
                "CABIN_CLAIRE",
                "D5-01",
                "bg_cabin_claire_d5_smoke"),
            Persist(
                "CREW_STAIRS",
                "D4-03",
                "bg_crew_stairs_d4_reconstruction"),
            Persist(
                "CREW_STAIRS",
                "D4-02",
                "bg_crew_stairs_d4_wet"),
            Persist(
                "HORIZON",
                "D8-01",
                "bg_horizon_d8_finale"),
            Persist(
                "HORIZON",
                "D1-06",
                "bg_horizon_cleared_day"),
            Persist(
                "MEDBAY",
                "D7-02",
                "bg_medbay_dna"),
            Persist(
                "MEDBAY",
                "D4-04",
                "bg_medbay_forensic"),
            Persist(
                "NEWS_LOUNGE",
                "D6-05",
                "bg_news_lounge_d6_evidence_board"),
            Persist(
                "VAULT",
                "D7-01",
                "bg_vault_d7_damaged"),
            Persist(
                "PORT",
                "D8-03",
                "bg_port_d8_epilogue"),
            Persist(
                "OPEN_DECK",
                "D8-02",
                "bg_open_deck_d8_morning")
        };

        private static readonly Dictionary<string, Sprite> SpriteCache =
            new(StringComparer.Ordinal);

        public static IReadOnlyList<LocationBackgroundVariantBinding> All =>
            Entries;

        public static string ResolveResourceKey(
            string locationCode,
            string sceneId) =>
            ResolveResourceKey(
                locationCode,
                sceneId,
                Array.Empty<string>());

        public static string ResolveResourceKey(
            string locationCode,
            string sceneId,
            IEnumerable<string> completedSceneIds)
        {
            string canonicalLocation =
                CanonicalLocationCatalog.FindSpec(locationCode)?.Code ??
                locationCode?.Trim().ToUpperInvariant() ??
                string.Empty;
            string normalizedScene =
                sceneId?.Trim().ToUpperInvariant() ?? string.Empty;
            HashSet<string> completed = new(
                (completedSceneIds ?? Array.Empty<string>())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value.Trim().ToUpperInvariant()),
                StringComparer.Ordinal);

            LocationBackgroundVariantBinding exact = Entries.FirstOrDefault(
                binding =>
                    binding.LogicalLocationCode == canonicalLocation &&
                    binding.SceneIds.Contains(
                        normalizedScene,
                        StringComparer.Ordinal) &&
                    !completed.Contains(normalizedScene));
            if (exact != null)
            {
                return exact.ResourceKey;
            }

            PersistentVariant persistent =
                PersistentEntries.FirstOrDefault(binding =>
                    binding.LogicalLocationCode == canonicalLocation &&
                    completed.Contains(binding.ActivationSceneId));
            if (persistent != null)
            {
                return persistent.ResourceKey;
            }

            return Entries.FirstOrDefault(binding =>
                    binding.LogicalLocationCode == canonicalLocation &&
                    binding.IsDefault)
                ?.ResourceKey ?? string.Empty;
        }

        public static Sprite Resolve(
            string locationCode,
            string sceneId,
            Sprite serializedFallback,
            IEnumerable<string> completedSceneIds = null)
        {
            string resourceKey =
                ResolveResourceKey(
                    locationCode,
                    sceneId,
                    completedSceneIds);
            if (string.IsNullOrEmpty(resourceKey))
            {
                return serializedFallback;
            }

            if (!SpriteCache.TryGetValue(resourceKey, out Sprite sprite))
            {
                sprite = Resources.Load<Sprite>(resourceKey);
                SpriteCache[resourceKey] = sprite;
            }

            return sprite != null ? sprite : serializedFallback;
        }

        private static LocationBackgroundVariantBinding Default(
            string locationCode,
            string resourceName) =>
            new(locationCode, resourceName);

        private static LocationBackgroundVariantBinding Scene(
            string locationCode,
            string resourceName,
            params string[] sceneIds) =>
            new(locationCode, resourceName, sceneIds);

        private static PersistentVariant Persist(
            string locationCode,
            string activationSceneId,
            string resourceName) =>
            new(locationCode, activationSceneId, resourceName);

        private sealed class PersistentVariant
        {
            public PersistentVariant(
                string logicalLocationCode,
                string activationSceneId,
                string resourceName)
            {
                LogicalLocationCode = logicalLocationCode;
                ActivationSceneId = activationSceneId;
                ResourceKey = $"{ResourceRoot}/{resourceName}";
            }

            public string LogicalLocationCode { get; }
            public string ActivationSceneId { get; }
            public string ResourceKey { get; }
        }
    }
}
