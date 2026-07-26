using System;
using System.Collections.Generic;
using System.Linq;
using Wake.Narrative;

namespace Wake.Evidence
{
    public sealed class CanonicalEvidenceContractDiagnostic
    {
        public CanonicalEvidenceContractDiagnostic(
            string evidenceId,
            string lineId,
            string message)
        {
            EvidenceId = evidenceId ?? string.Empty;
            LineId = lineId ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public string EvidenceId { get; }
        public string LineId { get; }
        public string Message { get; }
    }

    public static class CanonicalEvidenceContractValidator
    {
        public static IReadOnlyList<CanonicalEvidenceContractDiagnostic> Validate(
            IEnumerable<DialogueRecord> records,
            IEnumerable<CanonicalEvidenceEntry> entries = null)
        {
            DialogueRecord[] dialogue =
                (records ?? Array.Empty<DialogueRecord>()).ToArray();
            CanonicalEvidenceEntry[] catalog =
                (entries ?? CanonicalEvidenceCatalog.All).ToArray();
            var diagnostics =
                new List<CanonicalEvidenceContractDiagnostic>();
            Dictionary<string, DialogueRecord> byStableLine = dialogue
                .Where(record => !string.IsNullOrWhiteSpace(
                    record.StableLineId))
                .GroupBy(record => record.StableLineId, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.First(),
                    StringComparer.Ordinal);

            foreach (CanonicalEvidenceEntry entry in catalog)
            {
                if (entry == null)
                {
                    diagnostics.Add(new CanonicalEvidenceContractDiagnostic(
                        string.Empty,
                        string.Empty,
                        "증거 카탈로그에 null 항목이 있습니다."));
                    continue;
                }

                if (entry.GrantLineIds.Count == 0)
                {
                    diagnostics.Add(new CanonicalEvidenceContractDiagnostic(
                        entry.Id,
                        string.Empty,
                        "공식 획득 출처 줄이 등록되지 않았습니다."));
                    continue;
                }

                foreach (string lineId in entry.GrantLineIds)
                {
                    if (!byStableLine.TryGetValue(
                            lineId,
                            out DialogueRecord record))
                    {
                        diagnostics.Add(new CanonicalEvidenceContractDiagnostic(
                            entry.Id,
                            lineId,
                            "공식 대사집에 획득 출처 줄이 없습니다."));
                        continue;
                    }

                    if (entry.GrantMode == CanonicalEvidenceGrantMode.DialogueLine)
                    {
                        continue;
                    }

                    string[] granted = GetCanonicalEvidenceEffects(record)
                        .ToArray();
                    if (!granted.Contains(entry.Id, StringComparer.Ordinal))
                    {
                        diagnostics.Add(new CanonicalEvidenceContractDiagnostic(
                            entry.Id,
                            lineId,
                            $"대사 효과가 '{entry.Id}'를 지급하지 않습니다."));
                    }
                }
            }

            return diagnostics;
        }

        public static IReadOnlyList<string> GetCanonicalEvidenceEffects(
            DialogueRecord record)
        {
            if (record == null ||
                string.IsNullOrWhiteSpace(record.NextOrEffect))
            {
                return Array.Empty<string>();
            }

            ProductionEffectParseResult parsed =
                ProductionEffectParser.Parse(record.NextOrEffect);
            if (!parsed.Success)
            {
                return Array.Empty<string>();
            }

            return parsed.Instructions
                .Where(instruction =>
                    instruction.Kind == ProductionEffectKind.Evidence)
                .SelectMany(instruction => instruction.Values)
                .Select(CanonicalEvidenceCatalog.NormalizeId)
                .Where(id => CanonicalEvidenceCatalog.TryGet(id, out _))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }
    }
}
