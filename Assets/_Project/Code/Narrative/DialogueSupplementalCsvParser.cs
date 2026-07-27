using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Wake.Narrative
{
    [Serializable]
    public sealed class ChoiceFlowRecord
    {
        public ChoiceFlowRecord(
            string choiceId,
            string sceneId,
            string textKo,
            string condition,
            string effect,
            string branchGroup,
            string implementationStatus,
            int sourceRow)
        {
            ChoiceId = choiceId;
            SceneId = sceneId;
            TextKo = textKo;
            Condition = condition;
            Effect = effect;
            BranchGroup = branchGroup;
            ImplementationStatus = implementationStatus;
            SourceRow = sourceRow;
        }

        public string ChoiceId { get; }
        public string SceneId { get; }
        public string TextKo { get; }
        public string Condition { get; }
        public string Effect { get; }
        public string BranchGroup { get; }
        public string ImplementationStatus { get; }
        public int SourceRow { get; }
    }

    [Serializable]
    public sealed class SceneIndexRecord
    {
        public SceneIndexRecord(
            IReadOnlyDictionary<string, string> fields,
            int dialogueLineCount,
            int voicedLineCount,
            int choiceCount,
            int hintCount,
            int sourceRow)
        {
            SceneId = fields["scene_id"];
            Chapter = fields["chapter"];
            Title = fields["title"];
            TimeLabel = fields["time_label"];
            Location = fields["location"];
            Objective = fields["objective"];
            EntryCondition = fields["entry_condition"];
            NextScene = fields["next_scene"];
            Characters = fields["characters"];
            Clues = fields["clues"];
            Choices = fields["choices"];
            Status = fields["status"];
            DialogueLineCount = dialogueLineCount;
            VoicedLineCount = voicedLineCount;
            ChoiceCount = choiceCount;
            HintCount = hintCount;
            SourceRow = sourceRow;
        }

        public string SceneId { get; }
        public string Chapter { get; }
        public string Title { get; }
        public string TimeLabel { get; }
        public string Location { get; }
        public string Objective { get; }
        public string EntryCondition { get; }
        public string NextScene { get; }
        public string Characters { get; }
        public string Clues { get; }
        public string Choices { get; }
        public int DialogueLineCount { get; }
        public int VoicedLineCount { get; }
        public int ChoiceCount { get; }
        public int HintCount { get; }
        public string Status { get; }
        public int SourceRow { get; }
    }

    public sealed class SupplementalCsvParseResult<T>
    {
        public SupplementalCsvParseResult(
            IReadOnlyList<T> records,
            IReadOnlyList<string> errors)
        {
            Records = records;
            Errors = errors;
        }

        public IReadOnlyList<T> Records { get; }
        public IReadOnlyList<string> Errors { get; }
        public bool Success => Errors.Count == 0;
    }

    public static class DialogueSupplementalCsvParser
    {
        public static readonly string[] ChoiceHeaders =
        {
            "choice_id", "scene_id", "text_ko", "condition", "effect",
            "branch_group", "implementation_status"
        };

        public static readonly string[] SceneHeaders =
        {
            "scene_id", "chapter", "title", "time_label", "location",
            "objective", "entry_condition", "next_scene", "characters",
            "clues", "choices", "dialogue_line_count", "voiced_line_count",
            "choice_count", "hint_count", "status"
        };

        public static SupplementalCsvParseResult<ChoiceFlowRecord> ParseChoices(
            string csv)
        {
            var errors = new List<string>();
            if (!TryReadTable(csv, ChoiceHeaders, errors, out var rows))
                return new SupplementalCsvParseResult<ChoiceFlowRecord>(
                    Array.Empty<ChoiceFlowRecord>(), errors);

            var records = new List<ChoiceFlowRecord>(rows.Count);
            for (int index = 0; index < rows.Count; index++)
            {
                IReadOnlyDictionary<string, string> fields = rows[index];
                int sourceRow = index + 2;
                if (!Require(fields, "choice_id", sourceRow, errors) |
                    !Require(fields, "scene_id", sourceRow, errors) |
                    !Require(fields, "text_ko", sourceRow, errors) |
                    !Require(fields, "branch_group", sourceRow, errors))
                    continue;
                records.Add(new ChoiceFlowRecord(
                    fields["choice_id"],
                    fields["scene_id"],
                    fields["text_ko"],
                    fields["condition"],
                    fields["effect"],
                    fields["branch_group"],
                    fields["implementation_status"],
                    sourceRow));
            }
            AddDuplicateErrors(
                records.Select(record => record.ChoiceId),
                "choice_id",
                errors);
            return new SupplementalCsvParseResult<ChoiceFlowRecord>(
                records, errors);
        }

        public static SupplementalCsvParseResult<SceneIndexRecord> ParseScenes(
            string csv)
        {
            var errors = new List<string>();
            if (!TryReadTable(csv, SceneHeaders, errors, out var rows))
                return new SupplementalCsvParseResult<SceneIndexRecord>(
                    Array.Empty<SceneIndexRecord>(), errors);

            var records = new List<SceneIndexRecord>(rows.Count);
            for (int index = 0; index < rows.Count; index++)
            {
                IReadOnlyDictionary<string, string> fields = rows[index];
                int sourceRow = index + 2;
                if (!Require(fields, "scene_id", sourceRow, errors) |
                    !Require(fields, "location", sourceRow, errors) |
                    !Require(fields, "status", sourceRow, errors))
                    continue;
                if (!TryCount(fields, "dialogue_line_count", sourceRow, errors,
                        out int dialogueCount) |
                    !TryCount(fields, "voiced_line_count", sourceRow, errors,
                        out int voicedCount) |
                    !TryCount(fields, "choice_count", sourceRow, errors,
                        out int choiceCount) |
                    !TryCount(fields, "hint_count", sourceRow, errors,
                        out int hintCount))
                    continue;
                records.Add(new SceneIndexRecord(
                    fields,
                    dialogueCount,
                    voicedCount,
                    choiceCount,
                    hintCount,
                    sourceRow));
            }
            AddDuplicateErrors(
                records.Select(record => record.SceneId),
                "scene_id",
                errors);
            return new SupplementalCsvParseResult<SceneIndexRecord>(
                records, errors);
        }

        private static bool TryReadTable(
            string csv,
            IReadOnlyCollection<string> requiredHeaders,
            ICollection<string> errors,
            out List<IReadOnlyDictionary<string, string>> records)
        {
            List<List<string>> rows =
                DialogueCsvParser.ReadRows(csv ?? string.Empty, errors);
            records = new List<IReadOnlyDictionary<string, string>>();
            if (rows.Count == 0)
            {
                errors.Add("CSV has no header row.");
                return false;
            }

            var indices = new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < rows[0].Count; index++)
            {
                string header = rows[0][index].Trim().TrimStart('\uFEFF');
                if (!indices.TryAdd(header, index))
                    errors.Add($"Header '{header}' is duplicated.");
            }
            foreach (string header in requiredHeaders)
            {
                if (!indices.ContainsKey(header))
                    errors.Add($"Required header '{header}' is missing.");
            }
            if (errors.Count > 0)
                return false;

            foreach (List<string> row in rows.Skip(1))
            {
                if (row.All(string.IsNullOrWhiteSpace))
                    continue;
                records.Add(indices.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value < row.Count
                        ? row[pair.Value].Trim()
                        : string.Empty,
                    StringComparer.OrdinalIgnoreCase));
            }
            return true;
        }

        private static bool Require(
            IReadOnlyDictionary<string, string> fields,
            string key,
            int sourceRow,
            ICollection<string> errors)
        {
            if (!string.IsNullOrWhiteSpace(fields[key]))
                return true;
            errors.Add($"Row {sourceRow}: {key} is empty.");
            return false;
        }

        private static bool TryCount(
            IReadOnlyDictionary<string, string> fields,
            string key,
            int sourceRow,
            ICollection<string> errors,
            out int value)
        {
            if (int.TryParse(
                    fields[key],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out value) &&
                value >= 0)
                return true;
            errors.Add($"Row {sourceRow}: {key} must be a non-negative integer.");
            return false;
        }

        private static void AddDuplicateErrors(
            IEnumerable<string> values,
            string key,
            ICollection<string> errors)
        {
            foreach (string duplicate in values
                         .GroupBy(value => value, StringComparer.Ordinal)
                         .Where(group => group.Count() > 1)
                         .Select(group => group.Key))
                errors.Add($"{key} '{duplicate}' is duplicated.");
        }
    }
}
