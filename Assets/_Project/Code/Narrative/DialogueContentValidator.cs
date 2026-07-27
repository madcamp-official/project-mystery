using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Wake.Narrative
{
    public enum DialogueDiagnosticSeverity
    {
        Warning,
        Error
    }

    public sealed class DialogueDiagnostic
    {
        public DialogueDiagnosticSeverity Severity { get; }
        public string Code { get; }
        public int SourceRow { get; }
        public string Field { get; }
        public string Message { get; }

        public DialogueDiagnostic(
            DialogueDiagnosticSeverity severity,
            string code,
            int sourceRow,
            string field,
            string message)
        {
            Severity = severity;
            Code = code;
            SourceRow = sourceRow;
            Field = field;
            Message = message;
        }

        public override string ToString()
        {
            string row = SourceRow > 0 ? $"row {SourceRow}" : "header";
            return $"{Severity} {Code} ({row}, {Field}): {Message}";
        }
    }

    public sealed class DialogueValidationReport
    {
        public IReadOnlyList<DialogueDiagnostic> Diagnostics { get; }
        public int ErrorCount => Diagnostics.Count(item =>
            item.Severity == DialogueDiagnosticSeverity.Error);
        public int WarningCount => Diagnostics.Count(item =>
            item.Severity == DialogueDiagnosticSeverity.Warning);
        public bool IsValid => ErrorCount == 0;

        public DialogueValidationReport(IReadOnlyList<DialogueDiagnostic> diagnostics)
        {
            Diagnostics = diagnostics;
        }
    }

    public static class DialogueContentValidator
    {
        private const int MaximumChoicesPerGroup = 8;
        private static readonly HashSet<string> KnownSpeakers = new(StringComparer.Ordinal)
        {
            "ADRIAN", "ANON_CHAT", "CLAIRE", "CREW_ATTENDANT", "DANIEL",
            "DANIEL_CHAT", "EVELYN", "EVELYN_MESSAGE", "EVELYN_RECORD",
            "HELENA", "JULIAN_RECORD", "MARCUS", "NARRATION", "NEWS_REPORT",
            "OWEN", "PASSENGER_A", "PASSENGER_B", "PLAYER_CHOICE", "RICHARD",
            "SYSTEM", "THOMAS", "THOMAS_RECORD", "UI_HINT"
        };
        private static readonly HashSet<string> KnownLineTypes = new(StringComparer.Ordinal)
        {
            "branch_response", "choice", "dialogue", "ending_title",
            "ending_trigger", "fallback", "hint", "inspection", "monologue",
            "narration", "puzzle_feedback", "recording", "system"
        };
        private static readonly Regex ChoicePattern =
            new(@"^[A-Z0-9]+-\d{2}_[A-Z0-9_]+$", RegexOptions.Compiled);
        private static readonly Regex SceneReferencePattern =
            new(@"\b(?:P|D\d|I|F)-\d{2}\b", RegexOptions.Compiled);

        public static DialogueValidationReport Validate(string csv)
        {
            DialogueCsvParseResult parsed = DialogueCsvParser.Parse(csv);
            var diagnostics = new List<DialogueDiagnostic>();

            foreach (string error in parsed.Errors)
            {
                Add(diagnostics, DialogueDiagnosticSeverity.Error, "CSV_PARSE", 0, "csv", error);
            }
            ValidateHeaders(parsed.Headers, diagnostics);
            ValidateRecords(parsed.Records, diagnostics);
            return new DialogueValidationReport(diagnostics);
        }

        private static void ValidateHeaders(
            IReadOnlyList<string> headers,
            ICollection<DialogueDiagnostic> diagnostics)
        {
            foreach (string required in DialogueCsvParser.ProductionHeaders)
            {
                if (!headers.Contains(required, StringComparer.OrdinalIgnoreCase))
                {
                    Add(
                        diagnostics,
                        DialogueDiagnosticSeverity.Error,
                        "HEADER_MISSING",
                        0,
                        required,
                        $"Required header '{required}' is missing.");
                }
            }
        }

        private static void ValidateRecords(
            IReadOnlyList<DialogueRecord> records,
            ICollection<DialogueDiagnostic> diagnostics)
        {
            var sceneIds = new HashSet<string>(
                records.Select(record => record.SceneId),
                StringComparer.Ordinal);
            var stableIds = new HashSet<string>(StringComparer.Ordinal);
            var sceneOrders = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
            var choiceGroups = new Dictionary<string, List<DialogueRecord>>(StringComparer.Ordinal);

            foreach (DialogueRecord record in records)
            {
                Require(record.SceneId, record, "scene_id", diagnostics);
                Require(record.Speaker, record, "speaker", diagnostics);
                Require(record.TextKo, record, "text_ko", diagnostics);
                Require(record.Emotion, record, "emotion", diagnostics);
                Require(record.LineId, record, "line_id", diagnostics);
                Require(record.LineType, record, "line_type", diagnostics);
                ValidateOrder(record, sceneOrders, diagnostics);
                ValidateStableId(record, stableIds, diagnostics);
                ValidateToken(record, diagnostics);
                ValidateKorean(record, diagnostics);
                ValidateConditionReferences(record, sceneIds, diagnostics);
                CollectChoice(record, choiceGroups, diagnostics);
                if (!string.IsNullOrWhiteSpace(record.NextOrEffect) &&
                    !DialogueEffectCatalog.TryResolve(record, out _))
                {
                    Add(
                        diagnostics,
                        DialogueDiagnosticSeverity.Warning,
                        "EFFECT_UNCONFIRMED",
                        record.SourceRow,
                        "next_or_effect",
                        "Natural-language or ambiguous effect will not execute.");
                }
            }

            ValidateOrderContinuity(records, sceneOrders, diagnostics);
            ValidateChoiceGroups(choiceGroups, diagnostics);
        }

        private static void ValidateOrder(
            DialogueRecord record,
            IDictionary<string, HashSet<int>> sceneOrders,
            ICollection<DialogueDiagnostic> diagnostics)
        {
            if (record.Order < 1)
            {
                Error(diagnostics, "ORDER_RANGE", record, "order", "Order must start at 1.");
            }
            if (!sceneOrders.TryGetValue(record.SceneId, out HashSet<int> orders))
            {
                orders = new HashSet<int>();
                sceneOrders[record.SceneId] = orders;
            }
            if (!orders.Add(record.Order))
            {
                Error(
                    diagnostics,
                    "ORDER_DUPLICATE",
                    record,
                    "order",
                    $"Duplicate ({record.SceneId}, {record.Order}).");
            }
        }

        private static void ValidateStableId(
            DialogueRecord record,
            ISet<string> stableIds,
            ICollection<DialogueDiagnostic> diagnostics)
        {
            if (!stableIds.Add(record.StableLineId))
            {
                Error(
                    diagnostics,
                    "LINE_ID_DUPLICATE",
                    record,
                    "stable_line_id",
                    $"Stable line ID '{record.StableLineId}' is duplicated.");
            }
        }

        private static void ValidateToken(
            DialogueRecord record,
            ICollection<DialogueDiagnostic> diagnostics)
        {
            if (record.VoiceRequiredToken != "Y" && record.VoiceRequiredToken != "N")
            {
                Error(diagnostics, "VOICE_VALUE", record, "voice_required", "Use only Y or N.");
            }
            if (!KnownSpeakers.Contains(record.Speaker))
            {
                Error(diagnostics, "SPEAKER_UNKNOWN", record, "speaker", record.Speaker);
            }
            if (!KnownLineTypes.Contains(record.LineType))
            {
                Error(
                    diagnostics,
                    "LINE_TYPE_UNKNOWN",
                    record,
                    "line_type",
                    record.LineType);
            }
        }

        private static void ValidateKorean(
            DialogueRecord record,
            ICollection<DialogueDiagnostic> diagnostics)
        {
            foreach ((string field, string value) in new[]
            {
                ("text_ko", record.TextKo),
                ("condition", record.Condition),
                ("next_or_effect", record.NextOrEffect)
            })
            {
                if (value.Contains('\uFFFD') || value.Contains("???") || value.Contains("\u5360\uC3D9\uC639"))
                {
                    Error(diagnostics, "TEXT_ENCODING", record, field, "Broken Korean text detected.");
                }
            }
        }

        private static void ValidateConditionReferences(
            DialogueRecord record,
            ISet<string> sceneIds,
            ICollection<DialogueDiagnostic> diagnostics)
        {
            foreach (Match match in SceneReferencePattern.Matches(record.Condition))
            {
                if (!sceneIds.Contains(match.Value))
                {
                    Error(
                        diagnostics,
                        "CONDITION_SCENE_MISSING",
                        record,
                        "condition",
                        $"Scene '{match.Value}' does not exist.");
                }
            }
        }

        private static void CollectChoice(
            DialogueRecord record,
            IDictionary<string, List<DialogueRecord>> groups,
            ICollection<DialogueDiagnostic> diagnostics)
        {
            if (record.Speaker != "PLAYER_CHOICE")
            {
                return;
            }
            if (!ChoicePattern.IsMatch(record.ChoiceId))
            {
                Error(diagnostics, "CHOICE_ID", record, "choice_id", record.ChoiceId);
                return;
            }
            if (string.IsNullOrWhiteSpace(record.BranchGroup))
            {
                Error(
                    diagnostics,
                    "BRANCH_GROUP_REQUIRED",
                    record,
                    "branch_group",
                    "A choice must declare its branch group.");
                return;
            }
            string group = record.BranchGroup;
            if (!groups.TryGetValue(group, out List<DialogueRecord> choices))
            {
                choices = new List<DialogueRecord>();
                groups[group] = choices;
            }
            choices.Add(record);
        }

        private static void ValidateOrderContinuity(
            IEnumerable<DialogueRecord> records,
            IReadOnlyDictionary<string, HashSet<int>> sceneOrders,
            ICollection<DialogueDiagnostic> diagnostics)
        {
            foreach ((string sceneId, HashSet<int> orders) in sceneOrders)
            {
                for (int expected = 1; expected <= orders.Count; expected++)
                {
                    if (!orders.Contains(expected))
                    {
                        int row = records.First(record => record.SceneId == sceneId).SourceRow;
                        Add(
                            diagnostics,
                            DialogueDiagnosticSeverity.Error,
                            "ORDER_GAP",
                            row,
                            "order",
                            $"Scene '{sceneId}' is missing order {expected}.");
                    }
                }
            }
        }

        private static void ValidateChoiceGroups(
            IReadOnlyDictionary<string, List<DialogueRecord>> groups,
            ICollection<DialogueDiagnostic> diagnostics)
        {
            foreach ((string group, List<DialogueRecord> choices) in groups)
            {
                if (choices.Count > MaximumChoicesPerGroup)
                {
                    Error(
                        diagnostics,
                        "CHOICE_GROUP_SIZE",
                        choices[0],
                        "choice_id",
                        $"Group '{group}' has {choices.Count} choices; " +
                        $"the UI supports at most {MaximumChoicesPerGroup}.");
                }
            }
        }

        private static void Require(
            string value,
            DialogueRecord record,
            string field,
            ICollection<DialogueDiagnostic> diagnostics)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Error(diagnostics, "VALUE_REQUIRED", record, field, "Value is required.");
            }
        }

        private static void Error(
            ICollection<DialogueDiagnostic> diagnostics,
            string code,
            DialogueRecord record,
            string field,
            string message)
        {
            Add(
                diagnostics,
                DialogueDiagnosticSeverity.Error,
                code,
                record.SourceRow,
                field,
                message);
        }

        private static void Add(
            ICollection<DialogueDiagnostic> diagnostics,
            DialogueDiagnosticSeverity severity,
            string code,
            int sourceRow,
            string field,
            string message)
        {
            diagnostics.Add(new DialogueDiagnostic(severity, code, sourceRow, field, message));
        }
    }
}

#if UNITY_EDITOR
namespace Wake.Narrative.Editor
{
    using UnityEditor;
    using UnityEngine;

    public static class DialogueValidationMenu
    {
        private const string ProductionPath =
            "Assets/_Project/Content/Dialogue/Under_the_Horizon_Dialogue_KR.csv";

        [MenuItem("Wake/Dialogue/Validate Production CSV")]
        public static void ValidateProductionCsv()
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(ProductionPath);
            if (asset == null)
            {
                Debug.LogError($"Dialogue CSV was not found at {ProductionPath}.");
                return;
            }

            DialogueValidationReport report = DialogueContentValidator.Validate(asset.text);
            string details = string.Join("\n", report.Diagnostics);
            if (report.ErrorCount > 0)
            {
                Debug.LogError(details);
            }
            else if (report.WarningCount > 0)
            {
                Debug.LogWarning(details);
            }
            Debug.Log($"Dialogue validation: {report.ErrorCount} errors, {report.WarningCount} warnings.");
        }
    }
}
#endif
