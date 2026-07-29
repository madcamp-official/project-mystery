using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Wake.Exploration;
using Wake.Narrative;

namespace Wake.UI
{
    public enum MapTravelTier
    {
        PublicFastTravel,
        ConditionalFastTravel,
        RouteOnly
    }

    public enum MapLayerMode
    {
        Passenger,
        Investigation,
        Technical
    }

    public sealed class MapLocationPlacement
    {
        public MapLocationPlacement(
            string locationCode,
            int deck,
            Vector2 position,
            MapTravelTier travelTier,
            string description)
        {
            LocationCode = locationCode;
            Deck = deck;
            // Layer artwork coordinates are authored from the image's top-left,
            // while RectTransform anchors measure Y from the bottom.
            Position = new Vector2(position.x, 1f - position.y);
            TravelTier = travelTier;
            Description = description;
        }

        public string LocationCode { get; }
        public int Deck { get; }
        public Vector2 Position { get; }
        public MapTravelTier TravelTier { get; }
        public string Description { get; }
    }

    public static class MapDeckCatalog
    {
        public static readonly int[] DeckOrder = { 10, 9, 8, 7, 6, 0 };

        private static readonly MapLocationPlacement[] Placements =
        {
            P("PORT", 0, .28f, .52f, MapTravelTier.PublicFastTravel,
                "엘리시움호가 정박한 출발 항구."),
            P("GANGWAY", 0, .70f, .52f, MapTravelTier.PublicFastTravel,
                "항구와 선내를 잇는 승선 통로."),

            P("RICHARD_SUITE", 10, .28f, .50f,
                MapTravelTier.PublicFastTravel,
                "리처드 호손의 전용 스위트."),
            P("VIP_LOUNGE", 10, .48f, .68f,
                MapTravelTier.PublicFastTravel,
                "귀빈 승객을 위한 전용 라운지."),
            P("BRIDGE", 10, .86f, .48f,
                MapTravelTier.ConditionalFastTravel,
                "항해와 선박 제어를 담당하는 브리지."),
            P("VAULT", 10, .19f, .68f, MapTravelTier.RouteOnly,
                "회사 기록과 모듈이 보관된 보안 금고."),
            P("ARCHIVE", 10, .43f, .38f,
                MapTravelTier.ConditionalFastTravel,
                "오르페우스 기록을 보관하는 자료실."),
            P("INTERVIEW", 10, .72f, .36f,
                MapTravelTier.ConditionalFastTravel,
                "공식 면담과 심문에 사용되는 라운지."),
            P("OPEN_DECK", 10, .12f, .82f,
                MapTravelTier.PublicFastTravel,
                "선미와 바다를 조망하는 야외 갑판."),

            P("BALLROOM", 9, .22f, .50f, MapTravelTier.PublicFastTravel,
                "선상 파티가 열리는 그랜드 볼룸."),
            P("DINING", 9, .50f, .50f, MapTravelTier.PublicFastTravel,
                "승객용 다이닝 홀."),
            P("PROMENADE", 9, .54f, .82f,
                MapTravelTier.PublicFastTravel,
                "선체 외곽을 따라 이어지는 산책 갑판."),
            P("HORIZON", 9, .85f, .50f, MapTravelTier.PublicFastTravel,
                "사건이 발견된 전방 전망실."),

            P("ATRIUM", 8, .50f, .55f, MapTravelTier.PublicFastTravel,
                "층간 이동의 중심이 되는 선내 아트리움."),
            P("NEWS_LOUNGE", 8, .19f, .39f,
                MapTravelTier.PublicFastTravel,
                "승객이 기사와 소식을 확인하는 라운지."),
            P("SECURITY", 8, .33f, .39f,
                MapTravelTier.ConditionalFastTravel,
                "출입 기록과 CCTV를 관리하는 보안실."),
            P("MEDBAY", 8, .65f, .39f, MapTravelTier.PublicFastTravel,
                "진료와 법의학 검사가 이루어지는 의무실."),
            P("CABIN_CLAIRE", 8, .82f, .68f,
                MapTravelTier.ConditionalFastTravel,
                "클레어 베넷의 객실."),

            P("CABIN_DANIEL", 7, .18f, .50f,
                MapTravelTier.ConditionalFastTravel,
                "다니엘 조의 객실."),
            P("SERVICE7", 7, .40f, .22f, MapTravelTier.RouteOnly,
                "직원만 통행할 수 있는 7층 서비스 구역."),
            P("ENGINE_CONTROL", 7, .78f, .48f,
                MapTravelTier.RouteOnly,
                "기관과 안정화 로그를 확인하는 제어실."),
            P("BALLAST_CONTROL_ANNEX", 7, .90f, .48f,
                MapTravelTier.RouteOnly,
                "밸러스트 설비에 연결된 제한 부속실."),
            P("CREW_STAIRS", 7, .80f, .68f,
                MapTravelTier.RouteOnly,
                "승무원 전용 계단 B."),
            P("SERVICE_RAIL", 7, .88f, .27f,
                MapTravelTier.RouteOnly,
                "천장 화물 레일의 유지보수 접근 구역."),

            P("LAUNDRY", 6, .18f, .44f, MapTravelTier.RouteOnly,
                "승무원용 세탁·정비 구역."),
            P("SERVICE_HUB", 6, .36f, .44f, MapTravelTier.RouteOnly,
                "하부 서비스 통로의 연결 허브."),
            P("STABILIZERS", 6, .54f, .44f, MapTravelTier.RouteOnly,
                "선체 안정화 장치 구역."),
            P("BALLAST_TANKS", 6, .70f, .44f, MapTravelTier.RouteOnly,
                "밸러스트 탱크 설비 구역."),
            P("GENERATOR", 6, .38f, .66f, MapTravelTier.RouteOnly,
                "선내 전력을 공급하는 발전기실."),
            P("WORKSHOP", 6, .62f, .66f, MapTravelTier.RouteOnly,
                "하부 설비 정비 작업실.")
        };

        private static readonly IReadOnlyDictionary<string, MapLocationPlacement>
            ByLocation = Placements.ToDictionary(
                item => item.LocationCode,
                StringComparer.Ordinal);

        public static IReadOnlyList<MapLocationPlacement> All => Placements;

        public static IReadOnlyList<MapLocationPlacement> ForDeck(int deck) =>
            Placements.Where(item => item.Deck == deck).ToArray();

        public static MapLocationPlacement Find(string locationCode)
        {
            string code = CanonicalLocationCatalog.FindSpec(locationCode)?.Code ??
                          locationCode?.Trim().ToUpperInvariant() ??
                          string.Empty;
            return ByLocation.TryGetValue(code, out MapLocationPlacement placement)
                ? placement
                : null;
        }

        public static string DeckLabel(int deck) =>
            deck == 0 ? "항구" : $"DECK {deck}";

        public static string ResourceKey(
            int deck,
            MapLayerMode mode)
        {
            if (deck == 0)
            {
                return mode == MapLayerMode.Passenger
                    ? "Maps/DeckLayers/Port_Base"
                    : string.Empty;
            }
            if (deck < 0)
                return string.Empty;

            string suffix = mode switch
            {
                MapLayerMode.Investigation => "Restricted",
                MapLayerMode.Technical => "Technical",
                _ => "Base"
            };
            return $"Maps/DeckLayers/Deck{deck:00}_{suffix}";
        }

        public static bool IsLayerUnlocked(
            MapLayerMode mode,
            IEnumerable<string> completedSceneIds,
            IEnumerable<string> unlockedSceneIds)
        {
            if (mode == MapLayerMode.Passenger)
                return true;

            var completed = new HashSet<string>(
                completedSceneIds ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            return mode == MapLayerMode.Investigation
                ? completed.Contains("D1-04")
                : completed.Contains("D6-02");
        }

        public static bool ShouldReveal(
            MapLocationPlacement placement,
            ProductionMapEntry entry,
            string currentLocationCode,
            IEnumerable<string> completedSceneIds,
            IEnumerable<string> unlockedSceneIds)
        {
            if (placement == null || entry == null)
                return false;
            string current =
                CanonicalLocationCatalog.FindSpec(currentLocationCode)?.Code ??
                currentLocationCode;
            if (string.Equals(
                    placement.LocationCode,
                    current,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (placement.TravelTier == MapTravelTier.PublicFastTravel)
                return true;
            if (placement.Deck == 6 &&
                IsLayerUnlocked(
                    MapLayerMode.Technical,
                    completedSceneIds,
                    unlockedSceneIds))
            {
                return true;
            }
            if (entry.Status != ProductionMapEntryStatus.Locked)
                return true;

            var known = new HashSet<string>(
                (completedSceneIds ?? Array.Empty<string>())
                    .Concat(unlockedSceneIds ?? Array.Empty<string>()),
                StringComparer.Ordinal);
            return ProductionSceneCatalog.All
                .Where(scene =>
                    CanonicalLocationCatalog.FindSpec(
                        scene.NarrativeLocationCode)?.Code ==
                    placement.LocationCode)
                .Any(scene => known.Contains(scene.SceneId));
        }

        private static MapLocationPlacement P(
            string code,
            int deck,
            float x,
            float y,
            MapTravelTier tier,
            string description) =>
            new(code, deck, new Vector2(x, y), tier, description);
    }
}
