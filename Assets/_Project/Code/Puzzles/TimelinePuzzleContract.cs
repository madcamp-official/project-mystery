using System;
using System.Collections.Generic;
using System.Linq;
using Wake.Core;
using Wake.Narrative;

namespace Wake.Puzzles
{
    public sealed class TimelineCardDefinition
    {
        public TimelineCardDefinition(
            string id,
            string label,
            string confirmedTime = "",
            string sourceReference = "")
        {
            Id = Normalize(id);
            Label = label?.Trim() ?? string.Empty;
            ConfirmedTime = confirmedTime?.Trim() ?? string.Empty;
            SourceReference = sourceReference?.Trim() ?? string.Empty;
        }

        public string Id { get; }
        public string Label { get; }
        public string ConfirmedTime { get; }
        public string SourceReference { get; }
        public bool HasAuthoritativeSource =>
            !string.IsNullOrEmpty(SourceReference);

        public static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant().Replace(' ', '_');
    }

    public static class TimelinePuzzleCatalog
    {
        public const string PuzzleId = "timeline_12_cards";
        public const string SceneId = "D6-05";
        public const int RequiredCardCount = 12;

        public const string LastSighting = "last_sighting";
        public const string EvelynPartyReturn = "evelyn_party_return";
        public const string Murder = "murder";
        public const string Movement = "movement";
        public const string BodyDeparture = Movement;
        public const string DetectorError = "detector_error";
        public const string SinkOverflow = "sink_overflow";
        public const string BodyDiscovery = "body_discovery";
        public const string Discovery = BodyDiscovery;

        public const string ScenarioSource =
            "Game Scenario KR p.14 · 타임라인 카드";
        public const string DialogueTimeSource =
            "Dialogue CSV D6-05 · 살해 21:45 / 이동 22:18";

        private static readonly TimelineCardDefinition[] ConfirmedCards =
        {
            new(
                LastSighting,
                "Daniel의 마지막 목격",
                sourceReference: ScenarioSource),
            new(
                EvelynPartyReturn,
                "Evelyn의 파티 복귀",
                sourceReference: ScenarioSource),
            new(
                Murder,
                "실제 살해",
                "21:45",
                $"{ScenarioSource}; {DialogueTimeSource}"),
            new(
                BodyDeparture,
                "시신 출발",
                "22:18",
                $"{ScenarioSource}; {DialogueTimeSource}"),
            new(
                DetectorError,
                "감지기 오류",
                sourceReference: ScenarioSource),
            new(
                SinkOverflow,
                "세면대 범람",
                sourceReference: ScenarioSource),
            new(
                Discovery,
                "발견",
                sourceReference: ScenarioSource)
        };

        public static IReadOnlyList<TimelineCardDefinition> SourceBackedCards =>
            ConfirmedCards;
        public static TimelineSourceCoverage SourceCoverage =>
            TimelinePuzzleValidator.InspectSources(ConfirmedCards);
        public static int SourceMissingCount =>
            SourceCoverage.MissingSourceCount;

        public static IReadOnlyList<string> RequiredSequence { get; } = new[]
        {
            LastSighting,
            EvelynPartyReturn,
            Murder,
            BodyDeparture,
            DetectorError,
            SinkOverflow,
            Discovery
        };
    }

    public enum TimelinePlacementResult
    {
        Placed,
        UnknownCard,
        InvalidSlot,
        Completed
    }

    public readonly struct TimelineCompletionResult
    {
        public TimelineCompletionResult(
            bool completed,
            int missingCardCount,
            IReadOnlyList<string> diagnostics)
        {
            Completed = completed;
            MissingCardCount = missingCardCount;
            Diagnostics = diagnostics ?? Array.Empty<string>();
        }

        public bool Completed { get; }
        public int MissingCardCount { get; }
        public IReadOnlyList<string> Diagnostics { get; }
    }

    public readonly struct TimelineSourceCoverage
    {
        public TimelineSourceCoverage(
            int definitionCount,
            int authoritativeCount)
        {
            DefinitionCount = Math.Max(0, definitionCount);
            AuthoritativeCount = Math.Max(0, authoritativeCount);
        }

        public int RequiredCount => TimelinePuzzleCatalog.RequiredCardCount;
        public int DefinitionCount { get; }
        public int AuthoritativeCount { get; }
        public int MissingSourceCount =>
            Math.Max(0, RequiredCount - AuthoritativeCount);
        public int UnverifiedDefinitionCount =>
            Math.Max(0, DefinitionCount - AuthoritativeCount);
        public bool IsComplete =>
            DefinitionCount == RequiredCount &&
            MissingSourceCount == 0;
    }

    public static class TimelinePuzzleValidator
    {
        public static TimelineSourceCoverage InspectSources(
            IEnumerable<TimelineCardDefinition> definitions)
        {
            TimelineCardDefinition[] cards =
                (definitions ?? Array.Empty<TimelineCardDefinition>()).ToArray();
            return new TimelineSourceCoverage(
                cards.Length,
                cards.Count(card => card?.HasAuthoritativeSource == true));
        }

        public static IReadOnlyList<string> Validate(
            IEnumerable<TimelineCardDefinition> definitions)
        {
            TimelineCardDefinition[] cards =
                (definitions ?? Array.Empty<TimelineCardDefinition>()).ToArray();
            var diagnostics = new List<string>();
            if (cards.Length != TimelinePuzzleCatalog.RequiredCardCount)
            {
                diagnostics.Add(
                    $"타임라인 카드는 정확히 12장이어야 합니다: {cards.Length}장");
            }

            TimelineSourceCoverage coverage = InspectSources(cards);
            for (int index = 0;
                 index < coverage.MissingSourceCount;
                 index++)
            {
                diagnostics.Add(
                    $"source_missing: 권위 자료가 없는 타임라인 카드 " +
                    $"{index + 1}/{coverage.MissingSourceCount}");
            }

            foreach (TimelineCardDefinition card in cards)
            {
                if (card == null || string.IsNullOrEmpty(card.Id))
                {
                    diagnostics.Add("ID가 비어 있는 타임라인 카드가 있습니다.");
                }
                else if (string.IsNullOrEmpty(card.Label))
                {
                    diagnostics.Add($"표시 문구가 비어 있습니다: {card.Id}");
                }
            }

            foreach (string duplicate in cards
                         .Where(card => card != null && !string.IsNullOrEmpty(card.Id))
                         .GroupBy(card => card.Id, StringComparer.Ordinal)
                         .Where(group => group.Count() > 1)
                         .Select(group => group.Key))
            {
                diagnostics.Add($"중복 타임라인 카드 ID: {duplicate}");
            }

            foreach (string required in TimelinePuzzleCatalog.RequiredSequence)
            {
                if (!cards.Any(card => card != null && card.Id == required))
                {
                    diagnostics.Add($"필수 사건 카드가 없습니다: {required}");
                }
            }

            ValidateTime(cards, TimelinePuzzleCatalog.Murder, "21:45", diagnostics);
            ValidateTime(cards, TimelinePuzzleCatalog.Movement, "22:18", diagnostics);
            return diagnostics;
        }

        private static void ValidateTime(
            IEnumerable<TimelineCardDefinition> cards,
            string cardId,
            string expected,
            ICollection<string> diagnostics)
        {
            TimelineCardDefinition card =
                cards.FirstOrDefault(item => item != null && item.Id == cardId);
            if (card != null && card.ConfirmedTime != expected)
            {
                diagnostics.Add(
                    $"{cardId}의 확정 시각은 {expected}이어야 합니다.");
            }
        }
    }

    public sealed class TimelinePuzzleSession
    {
        private readonly GameStateManager state;
        private readonly IReadOnlyDictionary<string, TimelineCardDefinition> cards;
        private readonly Dictionary<int, string> placements = new();

        public TimelinePuzzleSession(
            GameStateManager state,
            IEnumerable<TimelineCardDefinition> definitions)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            cards = (definitions ?? Array.Empty<TimelineCardDefinition>())
                .Where(card => card != null && !string.IsNullOrEmpty(card.Id))
                .GroupBy(card => card.Id, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First());
            Definitions = cards.Values.ToArray();

            if (state.TryGetPuzzleSession(
                    TimelinePuzzleCatalog.PuzzleId,
                    out PuzzleSessionState saved))
            {
                Restore(saved);
            }
        }

        public IReadOnlyList<TimelineCardDefinition> Definitions { get; }
        public IReadOnlyDictionary<int, string> Placements => placements;
        public int HintLevel { get; private set; }
        public bool IsCompleted { get; private set; }

        public TimelinePlacementResult Place(string cardId, int slot)
        {
            if (IsCompleted)
            {
                return TimelinePlacementResult.Completed;
            }

            string normalized = TimelineCardDefinition.Normalize(cardId);
            if (!cards.ContainsKey(normalized))
            {
                return TimelinePlacementResult.UnknownCard;
            }

            if (slot < 0 || slot >= TimelinePuzzleCatalog.RequiredCardCount)
            {
                return TimelinePlacementResult.InvalidSlot;
            }

            int previousSlot = placements
                .Where(pair => pair.Value == normalized)
                .Select(pair => pair.Key)
                .DefaultIfEmpty(-1)
                .First();
            if (previousSlot >= 0)
            {
                placements.Remove(previousSlot);
            }

            placements[slot] = normalized;
            Save();
            return TimelinePlacementResult.Placed;
        }

        public bool UseHint()
        {
            if (IsCompleted || HintLevel >= 3)
            {
                return false;
            }

            HintLevel++;
            Save();
            return true;
        }

        public string GetHint() => HintLevel switch
        {
            1 => "살해 추정 시각은 21:45입니다.",
            2 => "시신 이동 기록은 22:18입니다.",
            3 => "마지막 목격 → 파티 복귀 → 실제 살해 → 시신 출발 → " +
                 "감지기 오류 → 세면대 범람 → 발견 순서를 확인하세요.",
            _ => "힌트를 사용하면 확정된 시각과 필수 순서를 확인할 수 있습니다."
        };

        public TimelineCompletionResult TryComplete()
        {
            var diagnostics = TimelinePuzzleValidator.Validate(Definitions).ToList();
            int missing = Math.Max(
                0,
                TimelinePuzzleCatalog.RequiredCardCount - placements.Count);
            if (missing > 0)
            {
                diagnostics.Add($"배치하지 않은 카드가 {missing}장 있습니다.");
            }

            int previous = -1;
            foreach (string required in TimelinePuzzleCatalog.RequiredSequence)
            {
                int slot = placements
                    .Where(pair => pair.Value == required)
                    .Select(pair => pair.Key)
                    .DefaultIfEmpty(-1)
                    .First();
                if (slot < 0)
                {
                    diagnostics.Add($"필수 사건이 배치되지 않았습니다: {required}");
                }
                else if (slot <= previous)
                {
                    diagnostics.Add($"필수 사건 순서가 잘못되었습니다: {required}");
                }

                previous = slot;
            }

            if (IsCompleted ||
                diagnostics.Count > 0 ||
                !ProductionSceneCompletionGate.CanStartInteraction(
                    state,
                    TimelinePuzzleCatalog.SceneId,
                    TimelinePuzzleCatalog.PuzzleId))
            {
                return new TimelineCompletionResult(false, missing, diagnostics);
            }

            using IDisposable batch = state.BeginStateBatch();
            IsCompleted = true;
            Save();
            state.AddFlag("puzzle_timeline_12_cards_completed");
            if (ProductionSceneCompletionGate.TryComplete(
                state,
                TimelinePuzzleCatalog.SceneId,
                TimelinePuzzleCatalog.PuzzleId,
                out bool newlyCompleted,
                out _) &&
                newlyCompleted)
            {
                InvestigationEventHub.Publish(
                    InvestigationEventKind.TheoryCompleted,
                    "incident_timeline",
                    TimelinePuzzleCatalog.SceneId);
            }
            return new TimelineCompletionResult(true, 0, Array.Empty<string>());
        }

        private void Restore(PuzzleSessionState saved)
        {
            foreach (string encoded in saved.selectedIds ?? new List<string>())
            {
                string[] parts = encoded.Split('=');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int slot) &&
                    slot >= 0 &&
                    slot < TimelinePuzzleCatalog.RequiredCardCount &&
                    cards.ContainsKey(parts[1]))
                {
                    placements[slot] = parts[1];
                }
            }

            HintLevel = saved.hintLevel;
            IsCompleted = saved.completed;
        }

        private void Save()
        {
            state.SavePuzzleSession(new PuzzleSessionState
            {
                puzzleId = TimelinePuzzleCatalog.PuzzleId,
                selectedIds = placements
                    .OrderBy(pair => pair.Key)
                    .Select(pair => $"{pair.Key}={pair.Value}")
                    .ToList(),
                step = placements.Count,
                hintLevel = HintLevel,
                completed = IsCompleted
            });
        }
    }
}
