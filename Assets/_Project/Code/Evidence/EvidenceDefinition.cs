using System.Collections.Generic;
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
        public string DisplayName =>
            TryGetMaster(out EvidenceMasterRecord master)
                ? master.DisplayName
                : displayName;
        public string Description =>
            TryGetMaster(out EvidenceMasterRecord master)
                ? master.Interpretation
                : description;
        public IReadOnlyList<string> SourceScenes =>
            TryGetMaster(out EvidenceMasterRecord master)
                ? master.SourceScenes
                : System.Array.Empty<string>();
        public string ArgumentRole =>
            TryGetMaster(out EvidenceMasterRecord master)
                ? master.ArgumentRole
                : string.Empty;
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

        private bool TryGetMaster(out EvidenceMasterRecord master) =>
            EvidenceMasterCatalog.TryGet(evidenceId, out master);
    }
}
