using UnityEngine;

namespace Seat0A.Evidence
{
    [CreateAssetMenu(fileName = "EvidenceDefinition", menuName = "Seat0A/Evidence Definition")]
    public class EvidenceDefinition : ScriptableObject
    {
        [SerializeField] private string evidenceId;
        [SerializeField] private string displayName;
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private Sprite[] views;

        public string EvidenceId => evidenceId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite[] Views => views;
    }
}
