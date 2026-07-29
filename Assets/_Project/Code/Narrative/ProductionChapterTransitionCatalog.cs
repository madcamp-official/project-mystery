using System;
using System.Collections.Generic;
using System.Linq;

namespace Wake.Narrative
{
    public enum TransitionKind
    {
        Departure,
        DayChange,
        Finale
    }

    public readonly struct ChapterTransitionRequest
    {
        public ChapterTransitionRequest(
            string completedSceneId,
            string nextSceneId,
            TransitionKind transitionKind,
            string chapterLabel,
            string title,
            string summary,
            string backgroundKey,
            string musicKey,
            string stingerKey,
            float minimumDisplayTime)
        {
            CompletedSceneId = Normalize(completedSceneId);
            NextSceneId = Normalize(nextSceneId);
            TransitionKind = transitionKind;
            ChapterLabel = chapterLabel ?? string.Empty;
            Title = title ?? string.Empty;
            Summary = summary ?? string.Empty;
            BackgroundKey = backgroundKey ?? string.Empty;
            MusicKey = musicKey ?? string.Empty;
            StingerKey = stingerKey ?? string.Empty;
            MinimumDisplayTime = Math.Max(0f, minimumDisplayTime);
        }

        public string CompletedSceneId { get; }
        public string NextSceneId { get; }
        public TransitionKind TransitionKind { get; }
        public string ChapterLabel { get; }
        public string Title { get; }
        public string Summary { get; }
        public string BackgroundKey { get; }
        public string MusicKey { get; }
        public string StingerKey { get; }
        public float MinimumDisplayTime { get; }
        public bool IsDeparture => TransitionKind == TransitionKind.Departure;

        private static string Normalize(string value) =>
            value?.Trim().ToUpperInvariant() ?? string.Empty;
    }

    public static class ProductionChapterTransitionCatalog
    {
        private const float DefaultMinimumDisplayTime = 2.5f;

        private static readonly ChapterTransitionRequest[] Entries =
        {
            T(
                "P-03", "D1-01", TransitionKind.Departure,
                "DAY 1",
                "출항과 첫 조사 파티",
                "MV Elysium은 항구를 떠난다",
                "departure",
                "BGM/Passage_to_Port",
                "SoundEffect/horn"),
            T(
                "D1-07", "D2-01", TransitionKind.DayChange,
                "DAY 2", "2일 차",
                "밤사이 남겨진 흔적과 기록을 다시 추적합니다.",
                "open_deck", "BGM/Midnight_Latitude(Theme_Adrian_Vale)", ""),
            T(
                "D2-06", "D3-01", TransitionKind.DayChange,
                "DAY 3", "3일 차",
                "승객들의 증언과 사라진 기록을 대조합니다.",
                "promenade", "BGM/Midnight_Latitude(Theme_Adrian_Vale)", ""),
            T(
                "D3-05", "D4-01", TransitionKind.DayChange,
                "DAY 4", "4일 차",
                "감시망의 빈틈과 이동 경로를 좁혀 갑니다.",
                "security", "BGM/Midnight_Latitude(Theme_Adrian_Vale)", ""),
            T(
                "D4-04", "D5-01", TransitionKind.DayChange,
                "DAY 5", "5일 차",
                "얽힌 관계와 조작된 증언을 확인합니다.",
                "horizon", "BGM/The_Horizon_Room", ""),
            T(
                "D5-04", "D6-01", TransitionKind.DayChange,
                "DAY 6", "6일 차",
                "기관 구역에서 사건의 실제 흔적을 찾습니다.",
                "engine", "BGM/The_Horizon_Room", ""),
            T(
                "D6-05", "D7-01", TransitionKind.DayChange,
                "DAY 7", "7일 차",
                "남은 기록을 복원해 마지막 모순을 정리합니다.",
                "archive", "BGM/The_Horizon_Room", ""),
            T(
                "D7-04", "D8-01", TransitionKind.Finale,
                "DAY 8", "8일 차",
                "확보한 정보와 증거로 진실을 밝힐 시간입니다.",
                "bridge", "BGM/The_Horizon_Room", "")
        };

        private static readonly IReadOnlyDictionary<string, ChapterTransitionRequest>
            ByCompletedScene = Entries.ToDictionary(
                item => item.CompletedSceneId,
                StringComparer.Ordinal);

        public static IReadOnlyList<ChapterTransitionRequest> All => Entries;

        public static bool TryGet(
            string completedSceneId,
            out ChapterTransitionRequest transition)
        {
            return ByCompletedScene.TryGetValue(
                completedSceneId?.Trim().ToUpperInvariant() ?? string.Empty,
                out transition);
        }

        private static ChapterTransitionRequest T(
            string completedSceneId,
            string nextSceneId,
            TransitionKind kind,
            string chapterLabel,
            string title,
            string summary,
            string backgroundKey,
            string musicKey,
            string stingerKey)
        {
            return new ChapterTransitionRequest(
                completedSceneId,
                nextSceneId,
                kind,
                chapterLabel,
                title,
                summary,
                backgroundKey,
                musicKey,
                stingerKey,
                DefaultMinimumDisplayTime);
        }
    }
}
