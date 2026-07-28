using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Wake.Evidence
{
    public enum CanonicalEvidenceGrantMode
    {
        DialogueLine,
        DialogueEffect,
        Interaction
    }

    public sealed class CanonicalEvidenceEntry
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public IReadOnlyList<string> SourceScenes { get; }
        public string ArgumentRole { get; }
        public string Category { get; }
        public bool IsDirect { get; }
        public CanonicalEvidenceGrantMode GrantMode { get; }
        public IReadOnlyList<string> GrantLineIds { get; }

        public CanonicalEvidenceEntry(
            string id,
            string displayName,
            string description,
            string category,
            bool isDirect,
            params string[] grantLineIds)
            : this(
                id,
                displayName,
                description,
                category,
                isDirect,
                CanonicalEvidenceGrantMode.DialogueEffect,
                grantLineIds)
        {
        }

        public CanonicalEvidenceEntry(
            string id,
            string displayName,
            string description,
            string category,
            bool isDirect,
            CanonicalEvidenceGrantMode grantMode,
            params string[] grantLineIds)
            : this(
                id,
                displayName,
                description,
                Array.Empty<string>(),
                string.Empty,
                category,
                isDirect,
                grantMode,
                grantLineIds)
        {
        }

        internal CanonicalEvidenceEntry(
            string id,
            string displayName,
            string description,
            IReadOnlyList<string> sourceScenes,
            string argumentRole,
            string category,
            bool isDirect,
            CanonicalEvidenceGrantMode grantMode,
            params string[] grantLineIds)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            SourceScenes = sourceScenes ?? Array.Empty<string>();
            ArgumentRole = argumentRole ?? string.Empty;
            Category = category;
            IsDirect = isDirect;
            GrantMode = grantMode;
            GrantLineIds = grantLineIds ?? Array.Empty<string>();
        }
    }

    public static class CanonicalEvidenceCatalog
    {
        private static readonly CanonicalEvidenceEntry[] Entries =
        {
            Create("C-01", "invitation", false, "p_01_05", "p_02_14"),
            Create("C-02", "exit", true, "d1_06_10"),
            CreateInteraction("C-03", "exit", true, "d2_01_09"),
            CreateInteraction("C-04", "exit", true, "d2_01_13"),
            CreateInteraction("C-05", "exit", true, "d2_01_17"),
            Create("C-06", "forensic", true, "d6_03_05"),
            Create("C-07", "forensic", true, "d2_02_14"),
            CreateInteraction("C-08", "timeline", false, "d2_04_14"),
            Create("C-09", "transport", false, "d6_01_10"),
            Create("C-10", "transport", true, "d6_04_10"),
            Create("C-11", "medical", true, "d2_03_07"),
            Create("C-12", "medical", false, "d6_03_16"),
            Create("C-13", "communication", false, "d5_03_08"),
            Create("C-14", "communication", false, "d3_05_10"),
            CreateInteraction(
                "C-15",
                "authentication",
                false,
                "d4_04_15"),
            Create("C-16", "identity", true, "d7_02_06"),
            Create("C-17", "history", false, "d7_03_15"),
            Create("C-18", "resolution", false, "d8_03_12")
        };

        private static readonly IReadOnlyDictionary<string, CanonicalEvidenceEntry> ById =
            Entries.ToDictionary(entry => entry.Id, StringComparer.Ordinal);

        private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> ByGrantLine =
            Entries
                .Where(entry =>
                    entry.GrantMode == CanonicalEvidenceGrantMode.DialogueLine)
                .SelectMany(entry => entry.GrantLineIds.Select(lineId => (lineId, entry.Id)))
                .GroupBy(pair => pair.lineId, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<string>)group.Select(pair => pair.Id).ToArray(),
                    StringComparer.Ordinal);

        public static IReadOnlyList<CanonicalEvidenceEntry> All => Entries;

        private static CanonicalEvidenceEntry Create(
            string id,
            string category,
            bool isDirect,
            params string[] grantLineIds)
        {
            return Create(
                id,
                category,
                isDirect,
                CanonicalEvidenceGrantMode.DialogueEffect,
                grantLineIds);
        }

        private static CanonicalEvidenceEntry CreateInteraction(
            string id,
            string category,
            bool isDirect,
            params string[] grantLineIds)
        {
            return Create(
                id,
                category,
                isDirect,
                CanonicalEvidenceGrantMode.Interaction,
                grantLineIds);
        }

        private static CanonicalEvidenceEntry Create(
            string id,
            string category,
            bool isDirect,
            CanonicalEvidenceGrantMode grantMode,
            params string[] grantLineIds)
        {
            if (!EvidenceMasterCatalog.TryGet(
                    id,
                    out EvidenceMasterRecord metadata))
            {
                throw new InvalidOperationException(
                    $"Evidence_Master metadata is missing: {id}");
            }

            return new CanonicalEvidenceEntry(
                metadata.EvidenceId,
                metadata.DisplayName,
                metadata.Interpretation,
                metadata.SourceScenes,
                metadata.ArgumentRole,
                category,
                isDirect,
                grantMode,
                grantLineIds);
        }

        public static bool TryGet(string evidenceId, out CanonicalEvidenceEntry entry)
        {
            return ById.TryGetValue(NormalizeId(evidenceId), out entry);
        }

        public static IReadOnlyList<string> GetGrantedEvidenceIds(string stableLineId)
        {
            string normalized = string.IsNullOrWhiteSpace(stableLineId)
                ? string.Empty
                : stableLineId.Trim().ToLowerInvariant();
            return ByGrantLine.TryGetValue(normalized, out IReadOnlyList<string> ids)
                ? ids
                : Array.Empty<string>();
        }

        public static EvidenceDefinition CreateRuntimeDefinition(string evidenceId)
        {
            if (!TryGet(evidenceId, out CanonicalEvidenceEntry entry))
            {
                return null;
            }

            EvidenceDefinition definition =
                ScriptableObject.CreateInstance<EvidenceDefinition>();
            definition.name = $"EvidenceDefinition_{entry.Id.Replace("-", string.Empty)}";
            definition.Initialize(entry);
            return definition;
        }

        public static string NormalizeId(string evidenceId)
        {
            string value = string.IsNullOrWhiteSpace(evidenceId)
                ? string.Empty
                : evidenceId.Trim().ToUpperInvariant().Replace('_', '-');
            if (value.Length >= 2 && value[0] == 'C')
            {
                string digits = value.Substring(1).TrimStart('-');
                if (int.TryParse(digits, out int number) && number > 0)
                {
                    return $"C-{number:00}";
                }
            }

            return value;
        }
    }
}
