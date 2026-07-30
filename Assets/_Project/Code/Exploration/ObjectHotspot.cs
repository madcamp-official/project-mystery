using UnityEngine;
using Wake.Narrative;
using Wake.Evidence;
using Wake.UI;

namespace Wake.Exploration
{
    public class ObjectHotspot : Hotspot
    {
        [SerializeField] private DialogueSet dialogueSet;
        [SerializeField] private bool isEvidence;
        [SerializeField] private EvidenceDefinition evidenceDefinition;

        protected override void OnInteract()
        {
            if (isEvidence && evidenceDefinition != null)
            {
                if (InvestigationScreenController.Instance?.Begin(
                        evidenceDefinition.EvidenceId,
                        StartConfiguredDialogue) == true)
                {
                    return;
                }

                if (InvestigationTargetCatalog.RequiresInspection(
                        evidenceDefinition.EvidenceId))
                {
                    ToastController.Instance?.Show(
                        "조사 화면을 준비하지 못했습니다.");
                    return;
                }
            }

            StartConfiguredDialogue();

            if (isEvidence && evidenceDefinition != null)
            {
                EvidenceInventory.Instance.Add(evidenceDefinition);
            }
        }

        private void StartConfiguredDialogue()
        {
            if (dialogueSet != null)
            {
                DialogueController.Instance.StartDialogue(dialogueSet);
            }
        }
    }
}
