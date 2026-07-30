using System;
using System.Collections.Generic;
using UnityEngine;
using Wake.Core;

namespace Wake.Evidence
{
    public class EvidenceInventory : MonoBehaviour
    {
        public static EvidenceInventory Instance { get; private set; }

        [SerializeField] private EvidenceDefinition[] knownEvidence;

        public event Action<EvidenceDefinition> EvidenceAdded;

        private readonly List<EvidenceDefinition> collected = new();
        private readonly HashSet<string> collectedIds =
            new(StringComparer.Ordinal);
        private readonly Dictionary<string, EvidenceDefinition> knownById =
            new(StringComparer.Ordinal);
        private readonly List<string> warnings = new();
        private GameStateManager boundState;

        public IReadOnlyList<EvidenceDefinition> Collected => collected;
        public IReadOnlyList<string> Warnings => warnings;

        public void BindState(GameStateManager state)
        {
            boundState = state;
        }

        private void Awake()
        {
            Instance = this;
            IndexKnownEvidence();
        }

        public bool Add(EvidenceDefinition evidence)
        {
            string evidenceId = CanonicalEvidenceCatalog.NormalizeId(evidence?.EvidenceId);
            if (evidence == null ||
                string.IsNullOrEmpty(evidenceId) ||
                !collectedIds.Add(evidenceId))
            {
                return false;
            }

            collected.Add(evidence);
            (boundState != null ? boundState : GameStateManager.Instance)
                ?.RecordEvidenceCollected(evidenceId);
            InvestigationEventHub.Publish(
                InvestigationEventKind.EvidenceCollected,
                evidenceId);
            EvidenceAdded?.Invoke(evidence);
            return true;
        }

        public bool TryAddById(string evidenceId)
        {
            EvidenceDefinition definition = ResolveDefinition(evidenceId);
            return definition != null && Add(definition);
        }

        public bool Contains(string evidenceId)
        {
            return collectedIds.Contains(CanonicalEvidenceCatalog.NormalizeId(evidenceId));
        }

        public EvidenceDefinition FindDefinition(string evidenceId)
        {
            return ResolveDefinition(evidenceId);
        }

        /// Silently repopulates the inventory from saved evidence IDs (continue flow).
        /// Does not fire EvidenceAdded/audio/toast side effects.
        public void RestoreFromIds(IReadOnlyList<string> evidenceIds)
        {
            collected.Clear();
            collectedIds.Clear();
            warnings.Clear();
            IndexKnownEvidence();
            if (evidenceIds == null)
            {
                return;
            }

            foreach (string sourceId in evidenceIds)
            {
                string evidenceId = CanonicalEvidenceCatalog.NormalizeId(sourceId);
                if (string.IsNullOrEmpty(evidenceId) || !collectedIds.Add(evidenceId))
                {
                    continue;
                }

                EvidenceDefinition definition = ResolveDefinition(evidenceId);
                if (definition == null)
                {
                    collectedIds.Remove(evidenceId);
                    warnings.Add($"Unknown saved evidence ID '{sourceId}' was ignored.");
                    continue;
                }

                collected.Add(definition);
            }
        }

        public void Clear()
        {
            collected.Clear();
            collectedIds.Clear();
            warnings.Clear();
        }

        private void IndexKnownEvidence()
        {
            knownById.Clear();
            if (knownEvidence == null)
            {
                return;
            }

            foreach (EvidenceDefinition definition in knownEvidence)
            {
                string evidenceId =
                    CanonicalEvidenceCatalog.NormalizeId(definition?.EvidenceId);
                if (definition == null || string.IsNullOrEmpty(evidenceId))
                {
                    continue;
                }

                if (!knownById.TryAdd(evidenceId, definition))
                {
                    warnings.Add($"Duplicate evidence ID '{evidenceId}' is configured.");
                }
            }
        }

        private EvidenceDefinition ResolveDefinition(string evidenceId)
        {
            string normalized = CanonicalEvidenceCatalog.NormalizeId(evidenceId);
            if (string.IsNullOrEmpty(normalized))
            {
                return null;
            }

            if (knownById.TryGetValue(normalized, out EvidenceDefinition definition))
            {
                return definition;
            }

            definition = CanonicalEvidenceCatalog.CreateRuntimeDefinition(normalized);
            if (definition != null)
            {
                knownById.Add(normalized, definition);
            }

            return definition;
        }
    }
}
