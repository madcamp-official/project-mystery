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
            L("PORT", "항구", 0, "", "bg_location_port_evidence.png"),
            L("GANGWAY", "승선 통로", 0, "", "bg_location_gangway.png"),
            L("RICHARD_SUITE", "리처드 스위트룸", 10, "D10-1", "bg_location_d10_1_richard_suite.png", "DECK10_SUITE"),
            L("VIP_LOUNGE", "귀빈 라운지", 10, "D10-2", "bg_location_d10_2_vip_lounge.png"),
            L("BRIDGE", "브리지", 10, "D10-3", "bg_location_d7_3_engine_control_evidence.png"),
            L("VAULT", "보안 금고", 10, "D10-4", "bg_location_d6_1_vault.png"),
            L("ARCHIVE", "기록 보관실", 10, "D10-5", "bg_location_d6_2_archive.png"),
            L("INTERVIEW", "인터뷰 라운지", 10, "D10-6", "bg_location_d8_3_security_evidence.png"),
            L("OPEN_DECK", "야외 갑판", 10, "D10-7", "bg_location_d10_3_open_deck.png", "STERN"),
            L("BALLROOM", "볼룸", 9, "D9-1", "bg_location_d9_1_ballroom.png", "DECK9_BALLROOM"),
            L("DINING", "식당", 9, "D9-2", "bg_location_d9_2_dining.png", "DECK9_DINING"),
            L("PROMENADE", "산책 갑판", 9, "D9-3", "bg_location_d9_3_promenade_evidence.png"),
            L("HORIZON", "호라이즌 룸", 9, "D9-4", "bg_location_d9_4_horizon_room_evidence.png"),
            L("ATRIUM", "아트리움", 8, "D8-1", "bg_location_d8_1_atrium.png", "DECK8_ATRIUM"),
            L("NEWS_LOUNGE", "뉴스 라운지", 8, "D8-2", "bg_location_d8_2_news_lounge_evidence.png", "EVIDENCE_BOARD"),
            L("SECURITY", "보안실", 8, "D8-3", "bg_location_d8_3_security_evidence.png"),
            L("MEDBAY", "의무실", 8, "D8-4", "bg_location_d7_1_medbay_evidence.png", "FORENSIC"),
            L("CABIN_CLAIRE", "클레어 객실", 8, "D8-5", "bg_location_d10_2_vip_lounge.png"),
            L("CABIN_DANIEL", "다니엘 객실", 7, "D7-1", "bg_location_d8_2_news_lounge_evidence.png"),
            L("SERVICE7", "7층 서비스 구역", 7, "D7-2", "bg_location_d7_4_crew_stairs.png"),
            L("ENGINE_CONTROL", "기관 제어실", 7, "D7-3", "bg_location_d7_3_engine_control_evidence.png", "ENGINE_CTRL"),
            L("BALLAST_CONTROL_ANNEX", "밸러스트 제어 별실", 7, "D7-4", "bg_location_d7_2_ballast_control_annex_evidence.png", "BALLAST"),
            L("CREW_STAIRS", "승무원 계단 B", 7, "D7-5", "bg_location_d7_4_crew_stairs.png", "STAIR_B"),
            L("SERVICE_RAIL", "서비스 레일", 7, "D7-6", "bg_location_d8_4_service_rail.png"),
            L("LAUNDRY", "세탁실", 6, "D6-1", "bg_location_d6_3_laundry.png"),
            L("SERVICE_HUB", "서비스 허브", 6, "D6-2", "bg_location_d6_4_service_hub.png"),
            L("STABILIZERS", "안정기실", 6, "D6-3", "bg_location_d5_1_stabilizers.png"),
            L("BALLAST_TANKS", "밸러스트 탱크", 6, "D6-4", "bg_location_d5_2_ballast_tanks.png"),
            L("GENERATOR", "발전기실", 6, "D6-5", "bg_location_d5_3_generator.png"),
            L("WORKSHOP", "작업실", 6, "D6-6", "bg_location_d5_4_workshop.png")
        };

        public static IReadOnlyList<CanonicalLocationSpec> All => Definitions;
        public static IReadOnlyList<CanonicalLocationSpec> StoryRelevant =>
            Definitions
                .Where(spec => ProductionSceneCatalog.All.Any(scene =>
                    FindSpec(scene.NarrativeLocationCode)?.Code == spec.Code))
                .ToArray();
        public static IReadOnlyCollection<string> UnresolvedCodes =>
            Array.Empty<string>();

        public static bool IsStoryRelevant(string code)
        {
            CanonicalLocationSpec spec = FindSpec(code);
            return spec != null && ProductionSceneCatalog.All.Any(scene =>
                FindSpec(scene.NarrativeLocationCode)?.Code == spec.Code);
        }

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
