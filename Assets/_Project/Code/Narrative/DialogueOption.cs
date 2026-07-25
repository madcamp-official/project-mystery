using System.Collections.Generic;
using UnityEngine;
using Seat0A.Core;

namespace Seat0A.Narrative
{
    [System.Serializable]
    public class DialogueOption
    {
        [SerializeField] private string optionLineId;
        [SerializeField] private string nextNodeId;
        [Header("State Effects")]
        [SerializeField] private string trustTarget;
        [SerializeField] private int trustDelta;
        [SerializeField] private int anxietyDelta;
        [SerializeField] private int integrityDelta;
        [SerializeField] private List<string> addFlags = new();
        [SerializeField] private List<string> removeFlags = new();

        public string OptionLineId => optionLineId;
        public string NextNodeId => nextNodeId;

        public void ApplyStateEffects()
        {
            GameStateService state = GameStateService.Instance;
            if (state == null)
            {
                return;
            }

            state.ChangeTrust(trustTarget, trustDelta);
            state.ChangePublicAnxiety(anxietyDelta);
            state.ChangeEvidenceIntegrity(integrityDelta);

            foreach (string flag in addFlags)
            {
                state.AddFlag(flag);
            }

            foreach (string flag in removeFlags)
            {
                state.RemoveFlag(flag);
            }
        }
    }
}
