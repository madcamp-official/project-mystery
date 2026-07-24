using UnityEngine;
using Wake.Narrative;

namespace Wake.Exploration
{
    public class CharacterHotspot : Hotspot
    {
        [SerializeField] private string characterId;
        [SerializeField] private DialogueSet dialogueSet;

        public string CharacterId => characterId;

        protected override void OnInteract()
        {
            if (dialogueSet == null)
            {
                Debug.LogWarning($"CharacterHotspot '{characterId}' has no DialogueSet assigned.");
                return;
            }

            DialogueController.Instance.StartDialogue(dialogueSet);
        }
    }
}
