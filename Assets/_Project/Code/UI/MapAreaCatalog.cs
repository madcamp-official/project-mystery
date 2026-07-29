using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Wake.UI
{
    public enum MapAreaVisualState
    {
        Hidden,
        Restricted,
        TemporarilyClosed,
        Accessible
    }

    [Serializable]
    public sealed class MapAreaShape
    {
        [SerializeField] private string areaId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private int deck;
        [SerializeField] private Vector2[] polygon = Array.Empty<Vector2>();
        [SerializeField] private Vector2 labelAnchor = new(.5f, .5f);
        [SerializeField] private Vector2 entranceAnchor = new(.5f, .5f);
        [SerializeField] private string revealCondition = "D1-04";
        [SerializeField] private string accessCondition = string.Empty;
        [SerializeField] private MapAreaVisualState initialState =
            MapAreaVisualState.Restricted;

        public string AreaId => areaId;
        public string DisplayName => displayName;
        public int Deck => deck;
        public IReadOnlyList<Vector2> Polygon => polygon;
        public Vector2 LabelAnchor => labelAnchor;
        public Vector2 EntranceAnchor => entranceAnchor;
        public string RevealCondition => revealCondition;
        public string AccessCondition => accessCondition;
        public MapAreaVisualState InitialState => initialState;

        public void SetAuthoringData(
            string id,
            string name,
            int authoredDeck,
            IEnumerable<Vector2> points,
            Vector2 label,
            Vector2 entrance,
            string reveal,
            string access,
            MapAreaVisualState state)
        {
            areaId = id?.Trim().ToUpperInvariant() ?? string.Empty;
            displayName = name?.Trim() ?? string.Empty;
            deck = authoredDeck;
            polygon = points?.ToArray() ?? Array.Empty<Vector2>();
            labelAnchor = label;
            entranceAnchor = entrance;
            revealCondition = reveal?.Trim().ToUpperInvariant() ?? string.Empty;
            accessCondition = access?.Trim().ToUpperInvariant() ?? string.Empty;
            initialState = state;
        }
    }

    [CreateAssetMenu(
        fileName = "MapAreaCatalog",
        menuName = "Wake/Map Area Catalog")]
    public sealed class MapAreaCatalogAsset : ScriptableObject
    {
        [SerializeField] private List<MapAreaShape> areas = new();

        public IReadOnlyList<MapAreaShape> Areas => areas;

        public void Replace(MapAreaShape shape)
        {
            if (shape == null)
                return;

            int index = areas.FindIndex(item =>
                string.Equals(
                    item.AreaId,
                    shape.AreaId,
                    StringComparison.Ordinal));
            if (index >= 0)
                areas[index] = shape;
            else
                areas.Add(shape);
        }
    }

    public static class MapAreaCatalog
    {
        private const string ResourceKey = "Maps/MapAreaCatalog";

        private static readonly MapAreaShape[] Defaults =
        {
            Area("SERVICE7", "서비스 7", 7, .39f, .23f, .48f, .31f,
                V(.10f, .18f), V(.73f, .18f), V(.78f, .22f),
                V(.74f, .31f), V(.11f, .31f), V(.08f, .26f)),
            Area("ENGINE_CONTROL", "기관 제어실", 7, .78f, .48f, .73f, .52f,
                V(.70f, .36f), V(.85f, .36f), V(.86f, .55f),
                V(.70f, .55f)),
            Area(
                "BALLAST_CONTROL_ANNEX",
                "밸러스트 부속실",
                7,
                .90f,
                .48f,
                .86f,
                .51f,
                V(.86f, .36f), V(.95f, .40f), V(.95f, .54f),
                V(.86f, .56f)),
            Area("CREW_STAIRS", "승무원 계단 B", 7, .80f, .68f, .76f, .63f,
                V(.73f, .57f), V(.84f, .57f), V(.84f, .75f),
                V(.73f, .75f)),
            Area("SERVICE_RAIL", "서비스 레일", 7, .88f, .27f, .84f, .31f,
                V(.80f, .18f), V(.93f, .21f), V(.94f, .34f),
                V(.81f, .34f)),

            Area("SECURITY", "보안실", 8, .33f, .39f, .38f, .43f,
                V(.28f, .32f), V(.39f, .32f), V(.40f, .45f),
                V(.28f, .45f)),
            Area(
                "DECK08_CREW_ACCESS",
                "승무원 접근부",
                8,
                .72f,
                .40f,
                .78f,
                .42f,
                V(.68f, .28f), V(.79f, .28f), V(.79f, .35f),
                V(.82f, .35f), V(.82f, .66f), V(.78f, .66f),
                V(.78f, .49f), V(.68f, .49f)),

            Area("DECK09_BALLROOM_SERVICE", "볼룸 지원 구역", 9,
                .21f, .27f, .29f, .31f,
                V(.11f, .22f), V(.31f, .22f), V(.35f, .27f),
                V(.32f, .32f), V(.11f, .31f), V(.08f, .27f)),
            Area("DECK09_KITCHEN", "주방·팬트리", 9,
                .55f, .25f, .59f, .31f,
                V(.35f, .22f), V(.70f, .22f), V(.72f, .26f),
                V(.68f, .31f), V(.36f, .31f)),
            Area("DECK09_BAR_SUPPORT", "바 지원실", 9,
                .66f, .53f, .69f, .57f,
                V(.62f, .47f), V(.72f, .47f), V(.72f, .58f),
                V(.62f, .58f)),
            Area("DECK09_STAFF_LOUNGE", "직원 휴게 구역", 9,
                .28f, .69f, .36f, .67f,
                V(.12f, .64f), V(.39f, .64f), V(.40f, .73f),
                V(.13f, .73f)),
            Area("DECK09_CREW_STAIR_B", "승무원 계단 B", 9,
                .70f, .70f, .66f, .66f,
                V(.64f, .61f), V(.75f, .61f), V(.76f, .76f),
                V(.64f, .76f)),

            Area("VAULT", "보안 금고", 10, .19f, .68f, .24f, .64f,
                V(.11f, .59f), V(.28f, .59f), V(.29f, .75f),
                V(.12f, .76f), V(.08f, .70f)),
            Area("ARCHIVE", "기록실", 10, .43f, .38f, .46f, .45f,
                V(.35f, .31f), V(.50f, .31f), V(.50f, .49f),
                V(.35f, .49f)),
            Area("INTERVIEW", "인터뷰 라운지", 10, .72f, .36f, .68f, .44f,
                V(.63f, .29f), V(.76f, .29f), V(.77f, .48f),
                V(.63f, .48f)),
            Area("BRIDGE", "브리지", 10, .86f, .48f, .79f, .50f,
                V(.78f, .30f), V(.91f, .34f), V(.95f, .46f),
                V(.94f, .62f), V(.79f, .62f)),
            Area("DECK10_CREW_STAIRS", "승무원 계단", 10,
                .54f, .69f, .58f, .63f,
                V(.49f, .59f), V(.62f, .59f), V(.62f, .76f),
                V(.49f, .76f))
        };

        private static IReadOnlyList<MapAreaShape> cached;

        public static IReadOnlyList<MapAreaShape> All
        {
            get
            {
                if (cached != null)
                    return cached;

                MapAreaCatalogAsset authored =
                    Resources.Load<MapAreaCatalogAsset>(ResourceKey);
                cached = authored != null && authored.Areas.Count > 0
                    ? authored.Areas.ToArray()
                    : Defaults;
                return cached;
            }
        }

        public static IReadOnlyList<MapAreaShape> ForDeck(int deck) =>
            All.Where(area => area.Deck == deck).ToArray();

        public static MapAreaShape Find(string areaId) =>
            All.FirstOrDefault(area =>
                string.Equals(
                    area.AreaId,
                    areaId,
                    StringComparison.OrdinalIgnoreCase));

        public static bool ConditionMet(
            string condition,
            IEnumerable<string> completedSceneIds,
            IEnumerable<string> unlockedSceneIds,
            Func<string, bool> hasFlag = null)
        {
            if (string.IsNullOrWhiteSpace(condition))
                return true;

            string normalized = condition.Trim().ToUpperInvariant();
            if (normalized.StartsWith("FLAG:", StringComparison.Ordinal))
            {
                string flag = normalized["FLAG:".Length..]
                    .ToLowerInvariant();
                return hasFlag?.Invoke(flag) == true;
            }

            return (completedSceneIds ?? Array.Empty<string>())
                       .Contains(normalized, StringComparer.OrdinalIgnoreCase) ||
                   (unlockedSceneIds ?? Array.Empty<string>())
                       .Contains(normalized, StringComparer.OrdinalIgnoreCase);
        }

        public static MapAreaVisualState ResolveState(
            MapAreaShape shape,
            bool revealConditionMet,
            bool accessConditionMet,
            bool entryAccessible,
            bool temporarilyClosed)
        {
            if (shape == null || !revealConditionMet)
                return MapAreaVisualState.Hidden;
            if (temporarilyClosed)
                return MapAreaVisualState.TemporarilyClosed;
            bool accessible = string.IsNullOrEmpty(shape.AccessCondition)
                ? entryAccessible
                : accessConditionMet;
            if (accessible)
            {
                return MapAreaVisualState.Accessible;
            }
            return shape.InitialState == MapAreaVisualState.Accessible
                ? MapAreaVisualState.Restricted
                : shape.InitialState;
        }

        public static bool IsValid(MapAreaShape shape, out string error)
        {
            if (shape == null)
            {
                error = "영역 데이터가 없습니다.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(shape.AreaId))
            {
                error = "영역 ID가 없습니다.";
                return false;
            }
            if (shape.Deck < 0)
            {
                error = "Deck 값은 0 이상이어야 합니다.";
                return false;
            }
            if (shape.Polygon == null || shape.Polygon.Count < 3)
            {
                error = "폴리곤에는 꼭짓점이 3개 이상 필요합니다.";
                return false;
            }
            if (shape.Polygon.Any(point => !InUnitRange(point)) ||
                !InUnitRange(shape.LabelAnchor) ||
                !InUnitRange(shape.EntranceAnchor))
            {
                error = "모든 좌표는 0~1 범위여야 합니다.";
                return false;
            }
            if (SelfIntersects(shape.Polygon))
            {
                error = "폴리곤이 자기 교차합니다.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public static bool SelfIntersects(IReadOnlyList<Vector2> polygon)
        {
            if (polygon == null || polygon.Count < 4)
                return false;

            for (int first = 0; first < polygon.Count; first++)
            {
                int firstNext = (first + 1) % polygon.Count;
                for (int second = first + 1; second < polygon.Count; second++)
                {
                    int secondNext = (second + 1) % polygon.Count;
                    if (first == second ||
                        firstNext == second ||
                        secondNext == first)
                    {
                        continue;
                    }
                    if (SegmentsIntersect(
                            polygon[first],
                            polygon[firstNext],
                            polygon[second],
                            polygon[secondNext]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool SegmentsIntersect(
            Vector2 a,
            Vector2 b,
            Vector2 c,
            Vector2 d)
        {
            float abC = Cross(b - a, c - a);
            float abD = Cross(b - a, d - a);
            float cdA = Cross(d - c, a - c);
            float cdB = Cross(d - c, b - c);
            return abC * abD < 0f && cdA * cdB < 0f;
        }

        private static float Cross(Vector2 a, Vector2 b) =>
            a.x * b.y - a.y * b.x;

        private static bool InUnitRange(Vector2 point) =>
            point.x >= 0f && point.x <= 1f &&
            point.y >= 0f && point.y <= 1f;

        private static MapAreaShape Area(
            string id,
            string name,
            int deck,
            float labelX,
            float labelY,
            float entranceX,
            float entranceY,
            params Vector2[] topLeftPoints)
        {
            var shape = new MapAreaShape();
            shape.SetAuthoringData(
                id,
                name,
                deck,
                topLeftPoints.Select(FlipY),
                FlipY(new Vector2(labelX, labelY)),
                FlipY(new Vector2(entranceX, entranceY)),
                "D1-04",
                string.Empty,
                MapAreaVisualState.Restricted);
            return shape;
        }

        private static Vector2 V(float x, float y) => new(x, y);
        private static Vector2 FlipY(Vector2 point) =>
            new(point.x, 1f - point.y);
    }
}
