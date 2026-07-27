using System;
using System.Collections.Generic;
using System.Linq;
using Wake.Narrative;

namespace Wake.Exploration
{
    public sealed class CanonicalLocationSpec
    {
        public CanonicalLocationSpec(
            string code,
            string displayName,
            int deck,
            string roomCode,
            string spriteFileName,
            params string[] narrativeAliases)
        {
            Code = code;
            DisplayName = displayName;
            Deck = deck;
            RoomCode = roomCode;
            SpriteFileName = spriteFileName;
            NarrativeAliases = narrativeAliases ?? Array.Empty<string>();
        }

        public string Code { get; }
        public string DisplayName { get; }
        public int Deck { get; }
        public string RoomCode { get; }
        public string SpriteFileName { get; }
        public IReadOnlyList<string> NarrativeAliases { get; }
    }

    public enum LocationCatalogDiagnosticSeverity
    {
        Warning,
        Error
    }

    public readonly struct LocationCatalogDiagnostic
    {
        public LocationCatalogDiagnostic(
            LocationCatalogDiagnosticSeverity severity,
            string code,
            string message)
        {
            Severity = severity;
            Code = code;
            Message = message;
        }

        public LocationCatalogDiagnosticSeverity Severity { get; }
        public string Code { get; }
        public string Message { get; }
    }

    public static class CanonicalLocationCatalog
    {
        public const string StartingLocationCode = "PORT";

        private static readonly CanonicalLocationSpec[] Definitions =
        {
            L("PORT", "Port", 0, "", "bg_location_port_evidence.png"),
            L("GANGWAY", "Gangway", 0, "", "bg_location_gangway.png"),
            L("RICHARD_SUITE", "Richard Suite", 10, "D10-1", "bg_location_d10_1_richard_suite.png", "DECK10_SUITE"),
            L("VIP_LOUNGE", "VIP Lounge", 10, "D10-2", "bg_location_d10_2_vip_lounge.png", "CABIN_CLAIRE"),
            L("OPEN_DECK", "Open Deck", 10, "D10-3", "bg_location_d10_3_open_deck.png", "STERN"),
            L("BALLROOM", "Ballroom", 9, "D9-1", "bg_location_d9_1_ballroom.png", "DECK9_BALLROOM"),
            L("DINING", "Dining", 9, "D9-2", "bg_location_d9_2_dining.png", "DECK9_DINING"),
            L("PROMENADE", "Promenade", 9, "D9-3", "bg_location_d9_3_promenade_evidence.png"),
            L("HORIZON", "Horizon Room", 9, "D9-4", "bg_location_d9_4_horizon_room_evidence.png"),
            L("ATRIUM", "Atrium", 8, "D8-1", "bg_location_d8_1_atrium.png", "DECK8_ATRIUM"),
            L("NEWS_LOUNGE", "News Lounge", 8, "D8-2", "bg_location_d8_2_news_lounge_evidence.png", "CABIN_DANIEL", "EVIDENCE_BOARD"),
            L("SECURITY", "Security", 8, "D8-3", "bg_location_d8_3_security_evidence.png", "INTERVIEW"),
            L("SERVICE_RAIL", "Service Rail", 8, "D8-4", "bg_location_d8_4_service_rail.png"),
            L("MEDBAY", "Medbay", 7, "D7-1", "bg_location_d7_1_medbay_evidence.png", "FORENSIC"),
            L("BALLAST_CONTROL_ANNEX", "Ballast Control Annex", 7, "D7-2", "bg_location_d7_2_ballast_control_annex_evidence.png", "BALLAST"),
            L("ENGINE_CONTROL", "Engine Control", 7, "D7-3", "bg_location_d7_3_engine_control_evidence.png", "ENGINE_CTRL", "BRIDGE"),
            L("CREW_STAIRS", "Crew Stairs", 7, "D7-4", "bg_location_d7_4_crew_stairs.png", "STAIR_B", "SERVICE7"),
            L("VAULT", "Vault", 6, "D6-1", "bg_location_d6_1_vault.png"),
            L("ARCHIVE", "Archive", 6, "D6-2", "bg_location_d6_2_archive.png"),
            L("LAUNDRY", "Laundry", 6, "D6-3", "bg_location_d6_3_laundry.png"),
            L("SERVICE_HUB", "Service Hub", 6, "D6-4", "bg_location_d6_4_service_hub.png"),
            L("STABILIZERS", "Stabilizers", 5, "D5-1", "bg_location_d5_1_stabilizers.png"),
            L("BALLAST_TANKS", "Ballast Tanks", 5, "D5-2", "bg_location_d5_2_ballast_tanks.png"),
            L("GENERATOR", "Generator", 5, "D5-3", "bg_location_d5_3_generator.png"),
            L("WORKSHOP", "Workshop", 5, "D5-4", "bg_location_d5_4_workshop.png")
        };

        public static IReadOnlyList<CanonicalLocationSpec> All => Definitions;
        public static IReadOnlyCollection<string> UnresolvedCodes =>
            Array.Empty<string>();

        public static CanonicalLocationSpec FindSpec(string code)
        {
            string normalized = code?.Trim();
            return Definitions.FirstOrDefault(definition =>
                string.Equals(definition.Code, normalized, StringComparison.Ordinal) ||
                definition.NarrativeAliases.Contains(normalized, StringComparer.Ordinal));
        }

        public static IReadOnlyList<LocationCatalogDiagnostic> Validate(
            IReadOnlyList<LocationDefinition> locations,
            IEnumerable<ProductionSceneDefinition> scenes)
        {
            List<LocationCatalogDiagnostic> diagnostics = new();
            Dictionary<string, LocationDefinition> byCode = new(StringComparer.Ordinal);

            foreach (LocationDefinition location in locations ?? Array.Empty<LocationDefinition>())
            {
                if (location == null)
                {
                    diagnostics.Add(Error("", "Location catalog contains a null asset."));
                    continue;
                }

                if (!byCode.TryAdd(location.LocationCode, location))
                {
                    diagnostics.Add(Error(location.LocationCode, "Duplicate physical location code."));
                }
            }

            foreach (CanonicalLocationSpec spec in Definitions)
            {
                if (!byCode.TryGetValue(spec.Code, out LocationDefinition asset))
                {
                    diagnostics.Add(Error(spec.Code, "Canonical location asset is missing."));
                    continue;
                }

                ValidateAsset(spec, asset, diagnostics);
            }

            foreach (ProductionSceneDefinition scene in scenes ?? Array.Empty<ProductionSceneDefinition>())
            {
                string narrativeCode = scene.NarrativeLocationCode;
                if (FindSpec(narrativeCode) != null)
                {
                    continue;
                }

                diagnostics.Add(new LocationCatalogDiagnostic(
                    LocationCatalogDiagnosticSeverity.Error,
                    narrativeCode,
                    $"Narrative location '{narrativeCode}' is not documented."));
            }

            return diagnostics
                .GroupBy(item => (item.Severity, item.Code, item.Message))
                .Select(group => group.First())
                .ToArray();
        }

        private static void ValidateAsset(
            CanonicalLocationSpec spec,
            LocationDefinition asset,
            ICollection<LocationCatalogDiagnostic> diagnostics)
        {
            if (!string.Equals(asset.DisplayName, spec.DisplayName, StringComparison.Ordinal) ||
                asset.Deck != spec.Deck ||
                !string.Equals(asset.RoomCode, spec.RoomCode, StringComparison.Ordinal))
            {
                diagnostics.Add(Error(spec.Code, "Location metadata does not match the canonical structure map."));
            }

            if (asset.BackgroundSprite == null)
            {
                diagnostics.Add(Error(spec.Code, "Location background sprite is missing."));
            }

            foreach (string alias in spec.NarrativeAliases)
            {
                if (!asset.MatchesCode(alias))
                {
                    diagnostics.Add(Error(spec.Code, $"Narrative alias '{alias}' is missing."));
                }
            }
        }

        private static CanonicalLocationSpec L(
            string code,
            string name,
            int deck,
            string room,
            string sprite,
            params string[] aliases) =>
            new(code, name, deck, room, sprite, aliases);

        private static LocationCatalogDiagnostic Error(string code, string message) =>
            new(LocationCatalogDiagnosticSeverity.Error, code, message);
    }
}
