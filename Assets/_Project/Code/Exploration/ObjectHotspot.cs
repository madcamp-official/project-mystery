using UnityEngine;
using Wake.Narrative;
using Wake.Evidence;

namespace Wake.Exploration
{
    public class ObjectHotspot : Hotspot
    {
        [SerializeField] private DialogueSet dialogueSet;
        [SerializeField] private bool isEvidence;
        [SerializeField] private EvidenceDefinition evidenceDefinition;

        protected override void OnInteract()
        {
            if (dialogueSet != null)
            {
                DialogueController.Instance.StartDialogue(dialogueSet);
            }

            if (isEvidence && evidenceDefinition != null)
            {
                EvidenceInventory.Instance.Add(evidenceDefinition);
            }
        }
    }
}
