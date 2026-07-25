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

        public IReadOnlyList<EvidenceDefinition> Collected => collected;

        private void Awake()
        {
            Instance = this;
        }

        public bool Add(EvidenceDefinition evidence)
        {
            if (evidence == null || collected.Contains(evidence))
            {
                return false;
            }

            collected.Add(evidence);
            GameStateManager.Instance?.RecordEvidenceCollected(evidence.EvidenceId);
            EvidenceAdded?.Invoke(evidence);
            return true;
        }

        /// Silently repopulates the inventory from saved evidence IDs (continue flow).
        /// Does not fire EvidenceAdded/audio/toast side effects.
        public void RestoreFromIds(IReadOnlyList<string> evidenceIds)
        {
            collected.Clear();
            if (evidenceIds == null || knownEvidence == null)
            {
                return;
            }

            foreach (string id in evidenceIds)
            {
                foreach (EvidenceDefinition definition in knownEvidence)
                {
                    if (definition != null && definition.EvidenceId == id)
                    {
                        collected.Add(definition);
                        break;
                    }
                }
            }
        }

        public void Clear()
        {
            collected.Clear();
        }
    }
}
