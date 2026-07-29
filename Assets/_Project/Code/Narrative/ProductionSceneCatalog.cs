using System;
using System.Collections.Generic;
using System.Linq;
using Wake.Core;

namespace Wake.Narrative
{
    public enum ProductionSceneType
    {
        Prologue,
        Investigation,
        Interrogation,
        Puzzle,
        Finale,
        Epilogue
    }

    public sealed class ProductionSceneDefinition
    {
        public string SceneId { get; }
        public int Day { get; }
        public int MinuteOfDay { get; }
        public TimeBlock TimeBlock { get; }
        public string NarrativeLocationCode { get; }
        public IReadOnlyList<string> Prerequisites { get; }
        public ProductionSceneType SceneType { get; }
        public string TimeLabel => $"{MinuteOfDay / 60:00}:{MinuteOfDay % 60:00}";

        public ProductionSceneDefinition(
            string sceneId,
            int day,
            int minuteOfDay,
            string narrativeLocationCode,
            ProductionSceneType sceneType,
            params string[] prerequisites)
        {
            SceneId = NormalizeSceneId(sceneId);
            Day = day;
            MinuteOfDay = minuteOfDay;
            TimeBlock = ToTimeBlock(minuteOfDay);
            NarrativeLocationCode = narrativeLocationCode?.Trim().ToUpperInvariant() ??
                                    string.Empty;
            SceneType = sceneType;
            Prerequisites = prerequisites?
                .Select(value => value?.Trim() ?? string.Empty)
                .Where(value => value.Length > 0)
                .ToArray() ?? Array.Empty<string>();
        }

        private static string NormalizeSceneId(string value)
        {
            return value?.Trim().ToUpperInvariant() ?? string.Empty;
        }

        private static TimeBlock ToTimeBlock(int minuteOfDay)
        {
            if (minuteOfDay < 12 * 60)
            {
                return TimeBlock.AM;
            }

            return minuteOfDay < 18 * 60 ? TimeBlock.PM : TimeBlock.NIGHT;
        }
    }

    public static class ProductionSceneCatalog
    {
        public const string FinalAccusationPrerequisite = "D8-01 correct";
        public const string EpiloguePrerequisite =
            "D8-02 or ending:C or bad_end";

        private static readonly ProductionSceneDefinition[] Entries =
        {
            S("P-01", 0, 15, 10, "PORT", ProductionSceneType.Prologue),
            S("P-02", 0, 15, 35, "GANGWAY", ProductionSceneType.Prologue, "P-01"),
            S("P-03", 0, 16, 20, "DECK10_SUITE", ProductionSceneType.Prologue, "P-02"),
            S("D1-01", 1, 17, 0, "DECK8_ATRIUM", ProductionSceneType.Investigation, "P-03"),
            S("D1-02", 1, 19, 30, "DECK9_DINING", ProductionSceneType.Investigation, "D1-01"),
            S("D1-03", 1, 21, 0, "DECK9_BALLROOM", ProductionSceneType.Investigation, "D1-02"),
            S("D1-04", 1, 21, 22, "SERVICE7", ProductionSceneType.Investigation, "D1-03"),
            S("D1-05", 1, 22, 35, "DECK9_BALLROOM", ProductionSceneType.Investigation, "D1-03"),
            S("D1-06", 1, 22, 45, "HORIZON", ProductionSceneType.Investigation, "D1-05"),
            S("D1-07", 1, 23, 10, "MEDBAY", ProductionSceneType.Interrogation, "D1-06"),
            S("D2-01", 2, 7, 30, "HORIZON", ProductionSceneType.Puzzle, "D1-07"),
            S("D2-02", 2, 8, 40, "HORIZON", ProductionSceneType.Puzzle, "D2-01"),
            S("D2-03", 2, 9, 30, "MEDBAY", ProductionSceneType.Interrogation, "D2-02"),
            S("D2-04", 2, 11, 0, "SECURITY", ProductionSceneType.Investigation, "D2-01"),
            S("D2-05", 2, 14, 0, "HORIZON", ProductionSceneType.Investigation, "D2-04"),
            S("D2-06", 2, 17, 20, "CABIN_DANIEL", ProductionSceneType.Investigation, "D2-03"),
            S("D3-01", 3, 8, 0, "NEWS_LOUNGE", ProductionSceneType.Investigation, "D2-06"),
            S("D3-02", 3, 10, 30, "DECK10_SUITE", ProductionSceneType.Interrogation, "D3-01"),
            S("D3-03", 3, 13, 0, "BRIDGE", ProductionSceneType.Interrogation, "D3-02"),
            S("D3-04", 3, 16, 0, "VAULT", ProductionSceneType.Investigation, "D3-03"),
            S("D3-05", 3, 20, 0, "PROMENADE", ProductionSceneType.Investigation, "D3-04"),
            S("D4-01", 4, 8, 30, "SECURITY", ProductionSceneType.Interrogation, "D3-04"),
            S("D4-02", 4, 11, 45, "STAIR_B", ProductionSceneType.Investigation, "D4-01"),
            S("D4-03", 4, 14, 30, "STAIR_B", ProductionSceneType.Puzzle, "D4-02"),
            S("D4-04", 4, 18, 0, "MEDBAY", ProductionSceneType.Puzzle, "D4-03"),
            S("D5-01", 5, 9, 0, "CABIN_CLAIRE", ProductionSceneType.Investigation, "D4-04"),
            S("D5-02", 5, 11, 0, "CABIN_CLAIRE", ProductionSceneType.Investigation, "D5-01"),
            S("D5-03", 5, 13, 30, "INTERVIEW", ProductionSceneType.Interrogation, "D5-02"),
            S("D5-04", 5, 16, 0, "HORIZON", ProductionSceneType.Investigation, "D5-03"),
            S("D6-01", 6, 7, 0, "ENGINE_CTRL", ProductionSceneType.Investigation, "D5-04"),
            S("D6-02", 6, 9, 30, "SERVICE_RAIL", ProductionSceneType.Puzzle, "D6-01"),
            S("D6-03", 6, 11, 0, "BALLAST", ProductionSceneType.Investigation, "D6-02"),
            S("D6-04", 6, 14, 0, "FORENSIC", ProductionSceneType.Investigation, "D6-03"),
            S("D6-05", 6, 18, 0, "EVIDENCE_BOARD", ProductionSceneType.Puzzle, "D6-04"),
            S("D7-01", 7, 6, 30, "VAULT", ProductionSceneType.Investigation, "D6-05"),
            S("D7-02", 7, 9, 0, "FORENSIC", ProductionSceneType.Investigation, "D7-01"),
            S("D7-03", 7, 13, 0, "ARCHIVE", ProductionSceneType.Puzzle, "D7-02"),
            S("D7-04", 7, 18, 0, "PROMENADE", ProductionSceneType.Interrogation, "D7-03"),
            S("D8-01", 8, 8, 0, "HORIZON", ProductionSceneType.Finale, "D7-04"),
            S(
                "D8-02",
                8,
                9,
                0,
                "STERN",
                ProductionSceneType.Finale,
                FinalAccusationPrerequisite),
            S(
                "D8-03",
                8,
                11,
                30,
                "PORT",
                ProductionSceneType.Epilogue,
                EpiloguePrerequisite)
        };

        private static readonly IReadOnlyDictionary<string, ProductionSceneDefinition> ById =
            Entries.ToDictionary(entry => entry.SceneId, StringComparer.Ordinal);

        public static IReadOnlyList<ProductionSceneDefinition> All => Entries;

        public static bool TryGet(string sceneId, out ProductionSceneDefinition definition)
        {
            return ById.TryGetValue(
                sceneId?.Trim().ToUpperInvariant() ?? string.Empty,
                out definition);
        }

        private static ProductionSceneDefinition S(
            string id,
            int day,
            int hour,
            int minute,
            string location,
            ProductionSceneType type,
            params string[] prerequisites)
        {
            return new ProductionSceneDefinition(
                id,
                day,
                hour * 60 + minute,
                location,
                type,
                prerequisites);
        }
    }

    public readonly struct ProductionDayBoundary
    {
        public ProductionDayBoundary(
            string completedSceneId,
            string nextSceneId)
        {
            CompletedSceneId =
                completedSceneId?.Trim().ToUpperInvariant() ?? string.Empty;
            NextSceneId =
                nextSceneId?.Trim().ToUpperInvariant() ?? string.Empty;
        }

        public string CompletedSceneId { get; }
        public string NextSceneId { get; }
    }

    public static class ProductionDayBoundaryCatalog
    {
        private static readonly ProductionDayBoundary[] Entries =
            ProductionChapterTransitionCatalog.All
                .Where(item => item.TransitionKind !=
                    TransitionKind.Departure)
                .Select(item => B(
                    item.CompletedSceneId,
                    item.NextSceneId))
                .ToArray();

        private static readonly IReadOnlyDictionary<string, ProductionDayBoundary>
            ByCompletedScene = Entries.ToDictionary(
                item => item.CompletedSceneId,
                StringComparer.Ordinal);

        public static IReadOnlyList<ProductionDayBoundary> All => Entries;

        public static bool TryGet(
            string completedSceneId,
            out ProductionDayBoundary boundary)
        {
            return ByCompletedScene.TryGetValue(
                completedSceneId?.Trim().ToUpperInvariant() ?? string.Empty,
                out boundary);
        }

        private static ProductionDayBoundary B(
            string completedSceneId,
            string nextSceneId)
        {
            return new ProductionDayBoundary(completedSceneId, nextSceneId);
        }
    }

    public sealed class SceneScheduleDiagnostic
    {
        public string SceneId { get; }
        public int SourceRow { get; }
        public string Message { get; }

        public SceneScheduleDiagnostic(
            string sceneId,
            int sourceRow,
            string message)
        {
            SceneId = sceneId;
            SourceRow = sourceRow;
            Message = message;
        }
    }

    public static class ProductionSceneScheduleValidator
    {
        public static IReadOnlyList<SceneScheduleDiagnostic> Validate(
            IEnumerable<DialogueRecord> records,
            IEnumerable<ProductionSceneDefinition> definitions = null)
        {
            var diagnostics = new List<SceneScheduleDiagnostic>();
            DialogueRecord[] source = records?.ToArray() ?? Array.Empty<DialogueRecord>();
            ProductionSceneDefinition[] schedule =
                (definitions ?? ProductionSceneCatalog.All).ToArray();
            var scheduleById = new Dictionary<string, ProductionSceneDefinition>(
                StringComparer.Ordinal);

            foreach (ProductionSceneDefinition definition in schedule)
            {
                ValidateDefinition(definition, scheduleById, diagnostics);
                if (definition != null && !string.IsNullOrEmpty(definition.SceneId))
                {
                    scheduleById.TryAdd(definition.SceneId, definition);
                }
            }

            var recordsByScene = source
                .Where(record => !string.IsNullOrWhiteSpace(record.SceneId))
                .GroupBy(record => record.SceneId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

            foreach (string sceneId in scheduleById.Keys.Except(recordsByScene.Keys))
            {
                AddError(diagnostics, sceneId, 0, "Schedule scene is missing from dialogue CSV.");
            }

            foreach (string sceneId in recordsByScene.Keys.Except(scheduleById.Keys))
            {
                AddError(
                    diagnostics,
                    sceneId,
                    recordsByScene[sceneId].Min(record => record.SourceRow),
                    "Dialogue CSV scene is missing from the production schedule.");
            }

            foreach (var pair in recordsByScene)
            {
                if (scheduleById.TryGetValue(pair.Key, out ProductionSceneDefinition definition))
                {
                    ValidateRecords(definition, pair.Value, scheduleById, diagnostics);
                }
            }

            return diagnostics;
        }

        private static void ValidateDefinition(
            ProductionSceneDefinition definition,
            IDictionary<string, ProductionSceneDefinition> seen,
            ICollection<SceneScheduleDiagnostic> diagnostics)
        {
            if (definition == null)
            {
                AddError(diagnostics, string.Empty, 0, "Schedule contains a null definition.");
                return;
            }

            if (string.IsNullOrEmpty(definition.SceneId))
            {
                AddError(diagnostics, string.Empty, 0, "Scene ID is empty.");
            }
            else if (seen.ContainsKey(definition.SceneId))
            {
                AddError(diagnostics, definition.SceneId, 0, "Scene ID is duplicated.");
            }

            if (definition.Day < 0 || definition.Day > 8)
            {
                AddError(diagnostics, definition.SceneId, 0, "Day must be between 0 and 8.");
            }

            if (definition.MinuteOfDay < 0 || definition.MinuteOfDay >= 24 * 60)
            {
                AddError(diagnostics, definition.SceneId, 0, "Time is outside a 24-hour day.");
            }

            if (string.IsNullOrEmpty(definition.NarrativeLocationCode))
            {
                AddError(diagnostics, definition.SceneId, 0, "Narrative location is empty.");
            }
        }

        private static void ValidateRecords(
            ProductionSceneDefinition definition,
            IReadOnlyList<DialogueRecord> records,
            IReadOnlyDictionary<string, ProductionSceneDefinition> schedule,
            ICollection<SceneScheduleDiagnostic> diagnostics)
        {
            string[] locations = records
                .Where(record => record.Speaker != "PLAYER_CHOICE")
                .Select(record => record.StageDirection)
                .Where(IsNarrativeLocation)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            int sourceRow = records.Min(record => record.SourceRow);
            if (locations.Length != 1 ||
                !string.Equals(
                    locations[0],
                    definition.NarrativeLocationCode,
                    StringComparison.Ordinal))
            {
                AddError(
                    diagnostics,
                    definition.SceneId,
                    sourceRow,
                    $"CSV location does not match '{definition.NarrativeLocationCode}'.");
            }

            string[] csvConditions = records
                .Select(record => record.Condition)
                .Where(IsSchedulePrerequisite)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (!csvConditions.SequenceEqual(definition.Prerequisites))
            {
                AddError(
                    diagnostics,
                    definition.SceneId,
                    sourceRow,
                    "CSV prerequisites do not match the production schedule.");
            }

            foreach (string prerequisite in definition.Prerequisites)
            {
                if (prerequisite !=
                        ProductionSceneCatalog.FinalAccusationPrerequisite &&
                    prerequisite != ProductionSceneCatalog.EpiloguePrerequisite &&
                    !schedule.ContainsKey(prerequisite))
                {
                    AddError(
                        diagnostics,
                        definition.SceneId,
                        sourceRow,
                        $"Prerequisite '{prerequisite}' does not reference a known scene.");
                }
            }
        }

        private static bool IsNarrativeLocation(string value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                string.Equals(value, "UI", StringComparison.Ordinal))
            {
                return false;
            }

            return System.Text.RegularExpressions.Regex.IsMatch(
                value.Trim(),
                @"^[A-Z][A-Z0-9_]*$");
        }

        private static bool IsSchedulePrerequisite(string value)
        {
            if (string.Equals(
                    value,
                    ProductionSceneCatalog.FinalAccusationPrerequisite,
                    StringComparison.Ordinal))
            {
                return true;
            }
            if (string.Equals(
                    value,
                    ProductionSceneCatalog.EpiloguePrerequisite,
                    StringComparison.Ordinal))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(value) &&
                   System.Text.RegularExpressions.Regex.IsMatch(
                       value.Trim(),
                       @"^(P|D\d+)-\d+$");
        }

        private static void AddError(
            ICollection<SceneScheduleDiagnostic> diagnostics,
            string sceneId,
            int sourceRow,
            string message)
        {
            diagnostics.Add(new SceneScheduleDiagnostic(
                sceneId,
                sourceRow,
                message));
        }
    }
}
