using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wake.Core
{
    public enum StateThresholdKind
    {
        PublicAnxietyRestriction,
        PublicAnxietyBadEnd,
        EvidenceIntegrityBadEnd
    }

    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        [SerializeField] private int startingTrust = 2;
        [SerializeField] private int startingPublicAnxiety = 15;
        [SerializeField] private int startingEvidenceIntegrity = 100;

        public int PublicAnxiety { get; private set; }
        public int EvidenceIntegrity { get; private set; }

        private readonly Dictionary<string, int> characterTrust = new();

        public event Action<StateThresholdKind> StateThresholdReached;

        private bool anxietyRestrictionFired;
        private bool anxietyBadEndFired;
        private bool integrityBadEndFired;

        private void Awake()
        {
            Instance = this;
            PublicAnxiety = startingPublicAnxiety;
            EvidenceIntegrity = startingEvidenceIntegrity;
        }

        public int GetTrust(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
            {
                return 0;
            }

            return characterTrust.TryGetValue(characterId, out int value) ? value : startingTrust;
        }

        public void ApplyChoiceEffects(string targetCharacterId, int trustDelta, int anxietyDelta, int integrityDelta)
        {
            if (!string.IsNullOrEmpty(targetCharacterId) && trustDelta != 0)
            {
                int current = GetTrust(targetCharacterId);
                characterTrust[targetCharacterId] = Mathf.Clamp(current + trustDelta, 0, 5);
            }

            if (anxietyDelta != 0)
            {
                PublicAnxiety = Mathf.Clamp(PublicAnxiety + anxietyDelta, 0, 100);
                CheckAnxietyThresholds();
            }

            if (integrityDelta != 0)
            {
                EvidenceIntegrity = Mathf.Clamp(EvidenceIntegrity + integrityDelta, 0, 100);
                CheckIntegrityThreshold();
            }
        }

        private void CheckAnxietyThresholds()
        {
            if (!anxietyRestrictionFired && PublicAnxiety >= 70)
            {
                anxietyRestrictionFired = true;
                StateThresholdReached?.Invoke(StateThresholdKind.PublicAnxietyRestriction);
            }

            if (!anxietyBadEndFired && PublicAnxiety >= 100)
            {
                anxietyBadEndFired = true;
                StateThresholdReached?.Invoke(StateThresholdKind.PublicAnxietyBadEnd);
            }
        }

        private void CheckIntegrityThreshold()
        {
            if (!integrityBadEndFired && EvidenceIntegrity <= 0)
            {
                integrityBadEndFired = true;
                StateThresholdReached?.Invoke(StateThresholdKind.EvidenceIntegrityBadEnd);
            }
        }
    }
}
