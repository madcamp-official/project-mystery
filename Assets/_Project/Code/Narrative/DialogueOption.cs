using System.Collections.Generic;
using UnityEngine;
using Wake.Core;

namespace Wake.Narrative
{
    [System.Serializable]
    public class DialogueOption
    {
        [SerializeField] private string optionLineId;
        [SerializeField] private string nextNodeId;

        [Header("State Effects")]
        [SerializeField] private string targetCharacterId;
        [SerializeField] private int trustDelta;
        [SerializeField] private int anxietyDelta;
        [SerializeField] private int integrityDelta;
        [SerializeField] private List<string> addFlags = new();
        [SerializeField] private List<string> removeFlags = new();

        public string OptionLineId => optionLineId;
        public string NextNodeId => nextNodeId;
        public string TargetCharacterId => targetCharacterId;
        public int TrustDelta => trustDelta;
        public int AnxietyDelta => anxietyDelta;
        public int IntegrityDelta => integrityDelta;

        public void ApplyStateEffects()
        {
            GameStateManager state = GameStateManager.Instance;
            if (state == null)
            {
                return;
            }

            state.ApplyChoiceEffects(
                targetCharacterId,
                trustDelta,
                anxietyDelta,
                integrityDelta);

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
