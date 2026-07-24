using System.Collections.Generic;
using UnityEngine;

namespace Seat0A.Narrative
{
    [CreateAssetMenu(fileName = "DialogueSet", menuName = "Seat0A/Dialogue Set")]
    public class DialogueSet : ScriptableObject
    {
        [SerializeField] private string startNodeId;
        [SerializeField] private List<DialogueNode> nodes = new();

        public string StartNodeId => startNodeId;

        public DialogueNode FindNode(string nodeId)
        {
            foreach (DialogueNode node in nodes)
            {
                if (node.NodeId == nodeId)
                {
                    return node;
                }
            }

            return null;
        }
    }
}
