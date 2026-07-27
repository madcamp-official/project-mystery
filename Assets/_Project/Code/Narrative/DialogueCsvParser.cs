using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Wake.Narrative
{
    public sealed class DialogueCsvParseResult
    {
        public IReadOnlyList<string> Headers { get; }
        public IReadOnlyList<DialogueRecord> Records { get; }
        public IReadOnlyList<string> Errors { get; }
        public bool Success => Errors.Count == 0;

        public DialogueCsvParseResult(
            IReadOnlyList<string> headers,
            IReadOnlyList<DialogueRecord> records,
            IReadOnlyList<string> errors)
        {
            Headers = headers;
            Records = records;
            Errors = errors;
        }
    }

    public static class DialogueCsvParser
    {
        public static readonly string[] ProductionHeaders =
        {
            "line_id", "scene_id", "order", "beat", "line_type", "speaker",
            "text_ko", "emotion", "condition", "choice_id", "next_or_effect",
            "stage_direction", "voice_required", "branch_group",
            "implementation_note"
        };

        public static readonly string[] LegacyProductionHeaders =
        {
            "scene_id", "order", "speaker", "text_ko", "emotion", "condition",
            "choice_id", "next_or_effect", "stage_direction", "voice_required"
        };

        public static DialogueCsvParseResult Parse(string csv)
        {
            var errors = new List<string>();
            List<List<string>> rows = ReadRows(csv ?? string.Empty, errors);
            if (rows.Count == 0)
            {
                errors.Add("CSV has no header row.");
                return new DialogueCsvParseResult(Array.Empty<string>(), Array.Empty<DialogueRecord>(), errors);
            }

            var headers = new List<string>(rows[0].Count);
            var indices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int column = 0; column < rows[0].Count; column++)
            {
                string header = rows[0][column].Trim().TrimStart('\uFEFF');
                headers.Add(header);
                if (!indices.TryAdd(header, column))
                {
                    errors.Add($"Header '{header}' is duplicated.");
                }
            }

            bool isCurrentProduction =
                HasAllHeaders(indices, ProductionHeaders);
            bool isLegacyProduction =
                HasAllHeaders(indices, LegacyProductionHeaders);
            bool isLegacy = HasAllHeaders(
                indices,
                new[] { "line_id", "scene_id", "speaker_id", "text", "emotion", "voice_required" });
            if (!isCurrentProduction && !isLegacyProduction && !isLegacy)
            {
                errors.Add(
                    "CSV does not match the current 15-column, legacy " +
                    "production 10-column, or legacy 6-column contract.");
                return new DialogueCsvParseResult(headers, Array.Empty<DialogueRecord>(), errors);
            }

            var records = new List<DialogueRecord>(Math.Max(0, rows.Count - 1));
            for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                List<string> row = rows[rowIndex];
                if (IsBlank(row))
                {
                    continue;
                }

                int sourceRow = rowIndex + 1;
                if (isCurrentProduction || isLegacyProduction)
                {
                    ParseProductionRow(
                        row,
                        indices,
                        sourceRow,
                        isCurrentProduction,
                        records,
                        errors);
                }
                else
                {
                    ParseLegacyRow(row, indices, sourceRow, records, errors);
                }
            }

            return new DialogueCsvParseResult(headers, records, errors);
        }

        private static void ParseProductionRow(
            IReadOnlyList<string> row,
            IReadOnlyDictionary<string, int> indices,
            int sourceRow,
            bool requiresLineId,
            ICollection<DialogueRecord> records,
            ICollection<string> errors)
        {
            string orderText = Get(row, indices, "order").Trim();
            if (!int.TryParse(orderText, NumberStyles.None, CultureInfo.InvariantCulture, out int order))
            {
                errors.Add($"Row {sourceRow}: order '{orderText}' is not an integer.");
                return;
            }

            string lineId = Get(row, indices, "line_id").Trim();
            if (requiresLineId && string.IsNullOrEmpty(lineId))
            {
                errors.Add($"Row {sourceRow}: line_id is empty.");
                return;
            }

            string voice = Get(row, indices, "voice_required").Trim();
            bool voiceRequired = voice.Equals("Y", StringComparison.OrdinalIgnoreCase);
            records.Add(new DialogueRecord(
                lineId,
                Get(row, indices, "scene_id").Trim(),
                order,
                Get(row, indices, "beat").Trim(),
                Get(row, indices, "line_type").Trim(),
                Get(row, indices, "speaker").Trim(),
                Get(row, indices, "text_ko"),
                Get(row, indices, "emotion").Trim(),
                Get(row, indices, "condition").Trim(),
                Get(row, indices, "choice_id").Trim(),
                Get(row, indices, "next_or_effect").Trim(),
                Get(row, indices, "stage_direction").Trim(),
                voice,
                Get(row, indices, "branch_group").Trim(),
                Get(row, indices, "implementation_note").Trim(),
                voiceRequired,
                sourceRow));
        }

        private static void ParseLegacyRow(
            IReadOnlyList<string> row,
            IReadOnlyDictionary<string, int> indices,
            int sourceRow,
            ICollection<DialogueRecord> records,
            ICollection<string> errors)
        {
            string lineId = Get(row, indices, "line_id").Trim();
            if (string.IsNullOrEmpty(lineId))
            {
                errors.Add($"Row {sourceRow}: legacy line_id is empty.");
                return;
            }

            string voice = Get(row, indices, "voice_required").Trim();
            records.Add(new DialogueRecord(
                lineId,
                Get(row, indices, "scene_id").Trim(),
                sourceRow - 1,
                string.Empty,
                string.Empty,
                Get(row, indices, "speaker_id").Trim(),
                Get(row, indices, "text"),
                Get(row, indices, "emotion").Trim(),
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                voice,
                string.Empty,
                string.Empty,
                voice.Equals("Y", StringComparison.OrdinalIgnoreCase) ||
                voice.Equals("TRUE", StringComparison.OrdinalIgnoreCase),
                sourceRow));
        }

        internal static List<List<string>> ReadRows(
            string csv,
            ICollection<string> errors)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            bool quoted = false;

            for (int index = 0; index < csv.Length; index++)
            {
                char character = csv[index];
                if (quoted)
                {
                    if (character == '"' && index + 1 < csv.Length && csv[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else if (character == '"')
                    {
                        quoted = false;
                    }
                    else
                    {
                        field.Append(character);
                    }
                }
                else if (character == '"')
                {
                    quoted = true;
                }
                else if (character == ',')
                {
                    row.Add(field.ToString());
                    field.Clear();
                }
                else if (character == '\r' || character == '\n')
                {
                    if (character == '\r' && index + 1 < csv.Length && csv[index + 1] == '\n')
                    {
                        index++;
                    }
                    row.Add(field.ToString());
                    field.Clear();
                    rows.Add(row);
                    row = new List<string>();
                }
                else
                {
                    field.Append(character);
                }
            }

            if (quoted)
            {
                errors.Add("CSV ended inside a quoted field.");
            }
            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }
            return rows;
        }

        private static bool HasAllHeaders(
            IReadOnlyDictionary<string, int> indices,
            IEnumerable<string> required)
        {
            foreach (string header in required)
            {
                if (!indices.ContainsKey(header))
                {
                    return false;
                }
            }
            return true;
        }

        private static string Get(
            IReadOnlyList<string> row,
            IReadOnlyDictionary<string, int> indices,
            string header)
        {
            return indices.TryGetValue(header, out int index) && index < row.Count
                ? row[index]
                : string.Empty;
        }

        private static bool IsBlank(IEnumerable<string> row)
        {
            foreach (string field in row)
            {
                if (!string.IsNullOrWhiteSpace(field))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
