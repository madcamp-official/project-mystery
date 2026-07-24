using UnityEngine;
using Seat0A.Narrative;
using Seat0A.Evidence;
using Seat0A.UI;

namespace Seat0A.Exploration
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
                    ToastController.Instance.Show($"증거품 획득: {evidenceDefinition.DisplayName}");
                }
            }
        }
    }
}
