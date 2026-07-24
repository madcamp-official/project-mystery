using UnityEngine;

namespace Wake.Narrative
{
    [System.Serializable]
    public class DialogueOption
    {
        [SerializeField] private string optionLineId;
        [SerializeField] private string nextNodeId;
        [SerializeField] private string targetCharacterId;
        [SerializeField] private int trustDelta;
        [SerializeField] private int anxietyDelta;
        [SerializeField] private int integrityDelta;

        public string OptionLineId => optionLineId;
        public string NextNodeId => nextNodeId;
        public string TargetCharacterId => targetCharacterId;
        public int TrustDelta => trustDelta;
        public int AnxietyDelta => anxietyDelta;
        public int IntegrityDelta => integrityDelta;
    }
}
