using UnityEngine;
using Wake.Narrative;
using Wake.Evidence;
using Wake.UI;
using Wake.Core;

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
                bool added = EvidenceInventory.Instance.Add(evidenceDefinition);
                if (added)
                {
                    ToastController.Instance.Show($"단서 확보: {evidenceDefinition.DisplayName}");
                }
            }
        }
    }
}
