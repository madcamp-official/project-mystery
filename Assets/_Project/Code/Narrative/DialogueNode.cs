using System.Collections.Generic;
using UnityEngine;

namespace Seat0A.Narrative
{
    [System.Serializable]
    public class DialogueNode
    {
        [SerializeField] private string nodeId;
        [SerializeField] private string lineId;
        [SerializeField] private List<DialogueOption> options = new();

        public string NodeId => nodeId;
        public string LineId => lineId;
        public List<DialogueOption> Options => options;
    }
}
