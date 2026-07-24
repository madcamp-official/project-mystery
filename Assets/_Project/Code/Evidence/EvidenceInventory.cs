using System;
using System.Collections.Generic;
using UnityEngine;

namespace Seat0A.Evidence
{
    public class EvidenceInventory : MonoBehaviour
    {
        public static EvidenceInventory Instance { get; private set; }

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
            EvidenceAdded?.Invoke(evidence);
            return true;
        }
    }
}
