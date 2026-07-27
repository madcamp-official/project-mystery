using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Wake.Narrative;

namespace Wake.Evidence
{
    public sealed class EvidenceMasterRecord
    {
        public EvidenceMasterRecord(
            string evidenceId,
            string displayName,
            string interpretation,
            IEnumerable<string> sourceScenes,
            string argumentRole,
            string coverage)
        {
            EvidenceId = evidenceId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Interpretation = interpretation ?? string.Empty;
            SourceScenes = (sourceScenes ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim().ToUpperInvariant())
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            ArgumentRole = argumentRole ?? string.Empty;
            Coverage = coverage ?? string.Empty;
        }

        public string EvidenceId { get; }
        public string DisplayName { get; }
        public string Interpretation { get; }
        public IReadOnlyList<string> SourceScenes { get; }
        public string ArgumentRole { get; }
        public string Coverage { get; }
    }

    public sealed class EvidenceMasterParseResult
    {
        public EvidenceMasterParseResult(
            IReadOnlyList<EvidenceMasterRecord> records,
            IReadOnlyList<string> errors)
        {
            Records = records ?? Array.Empty<EvidenceMasterRecord>();
            Errors = errors ?? Array.Empty<string>();
        }

        public IReadOnlyList<EvidenceMasterRecord> Records { get; }
        public IReadOnlyList<string> Errors { get; }
        public bool Success => Errors.Count == 0;
    }

    public static class EvidenceMasterCsvParser
    {
        private static readonly string[] Headers =
        {
            "evidence_id",
            "display_name",
            "interpretation",
            "source_scene",
            "argument_role",
            "coverage"
        };

        public static EvidenceMasterParseResult Parse(string csv)
        {
            var errors = new List<string>();
            List<List<string>> rows =
                DialogueCsvParser.ReadRows(csv ?? string.Empty, errors);
            if (rows.Count == 0)
            {
                errors.Add("Evidence_Master CSV has no header row.");
                return new EvidenceMasterParseResult(
                    Array.Empty<EvidenceMasterRecord>(),
                    errors);
            }

            var columns = new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < rows[0].Count; index++)
            {
                string header =
                    rows[0][index].Trim().TrimStart('\uFEFF');
                if (!columns.TryAdd(header, index))
                {
                    errors.Add($"Duplicate Evidence_Master header: {header}");
                }
            }

            foreach (string header in Headers)
            {
                if (!columns.ContainsKey(header))
                {
                    errors.Add($"Missing Evidence_Master header: {header}");
                }
            }

            if (errors.Count > 0)
            {
                return new EvidenceMasterParseResult(
                    Array.Empty<EvidenceMasterRecord>(),
                    errors);
            }

            var records = new List<EvidenceMasterRecord>();
            for (int index = 1; index < rows.Count; index++)
            {
                List<string> row = rows[index];
                if (row.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                string Value(string header)
                {
                    int column = columns[header];
                    return column < row.Count ? row[column].Trim() : string.Empty;
                }

                string id = Value("evidence_id");
                string displayName = Value("display_name");
                string interpretation = Value("interpretation");
                string sourceScene = Value("source_scene");
                string argumentRole = Value("argument_role");
                if (new[]
                    {
                        id,
                        displayName,
                        interpretation,
                        sourceScene,
                        argumentRole
                    }.Any(string.IsNullOrWhiteSpace))
                {
                    errors.Add(
                        $"Evidence_Master row {index + 1} has an empty required field.");
                    continue;
                }

                records.Add(new EvidenceMasterRecord(
                    id,
                    displayName,
                    interpretation,
                    sourceScene.Split('/'),
                    argumentRole,
                    Value("coverage")));
            }

            foreach (IGrouping<string, EvidenceMasterRecord> duplicate in
                     records.GroupBy(
                         record => record.EvidenceId,
                         StringComparer.Ordinal)
                         .Where(group => group.Count() > 1))
            {
                errors.Add(
                    $"Duplicate Evidence_Master evidence_id: {duplicate.Key}");
            }

            return new EvidenceMasterParseResult(records, errors);
        }
    }

    public static class EvidenceMasterCatalog
    {
        public const string ResourcePath =
            "Data/Under_the_Horizon_Evidence_Master_KR";

        private static readonly IReadOnlyList<EvidenceMasterRecord> Records =
            Load();
        private static readonly IReadOnlyDictionary<string, EvidenceMasterRecord>
            ById = Records.ToDictionary(
                record => record.EvidenceId,
                StringComparer.Ordinal);

        public static IReadOnlyList<EvidenceMasterRecord> All => Records;

        public static bool TryGet(
            string evidenceId,
            out EvidenceMasterRecord record)
        {
            return ById.TryGetValue(
                NormalizeId(evidenceId),
                out record);
        }

        private static string NormalizeId(string evidenceId)
        {
            string value = string.IsNullOrWhiteSpace(evidenceId)
                ? string.Empty
                : evidenceId.Trim().ToUpperInvariant().Replace('_', '-');
            string digits = value.Length > 1 && value[0] == 'C'
                ? value.Substring(1).TrimStart('-')
                : string.Empty;
            return int.TryParse(digits, out int number) && number > 0
                ? $"C-{number:00}"
                : value;
        }

        private static IReadOnlyList<EvidenceMasterRecord> Load()
        {
            TextAsset source = Resources.Load<TextAsset>(ResourcePath);
            if (source == null)
            {
                throw new InvalidOperationException(
                    $"Evidence_Master resource is missing: {ResourcePath}");
            }

            EvidenceMasterParseResult parsed =
                EvidenceMasterCsvParser.Parse(source.text);
            if (!parsed.Success)
            {
                throw new InvalidOperationException(
                    string.Join("\n", parsed.Errors));
            }

            return parsed.Records;
        }
    }
}
