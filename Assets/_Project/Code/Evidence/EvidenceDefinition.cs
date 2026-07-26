using UnityEngine;

namespace Wake.Evidence
{
    [CreateAssetMenu(fileName = "EvidenceDefinition", menuName = "Wake/Evidence Definition")]
    public class EvidenceDefinition : ScriptableObject
    {
        [SerializeField] private string evidenceId;
        [SerializeField] private string displayName;
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private string category;
        [SerializeField] private bool isDirect = true;
        [SerializeField] private Sprite[] views;

        public string EvidenceId => evidenceId;
        public string DisplayName => displayName;
        public string Description => description;
        public string Category => category;
        public bool IsDirect => isDirect;
        public Sprite[] Views => views;

        internal void Initialize(CanonicalEvidenceEntry entry)
        {
            evidenceId = entry.Id;
            displayName = entry.DisplayName;
            description = entry.Description;
            category = entry.Category;
            isDirect = entry.IsDirect;
            views = System.Array.Empty<Sprite>();
        }
    }
}
