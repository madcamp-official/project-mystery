using UnityEngine;

namespace Seat0A.Narrative
{
    [System.Serializable]
    public class DialogueOption
    {
        [SerializeField] private string optionLineId;
        [SerializeField] private string nextNodeId;

        public string OptionLineId => optionLineId;
        public string NextNodeId => nextNodeId;
    }
}
