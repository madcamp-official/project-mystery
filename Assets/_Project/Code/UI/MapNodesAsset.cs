using System.Collections.Generic;
using UnityEngine;

namespace Wake.UI
{
    [CreateAssetMenu(
        fileName = "Deck_MapNodes",
        menuName = "Wake/Map/Location Nodes")]
    public sealed class MapNodesAsset : MapGeometryAsset
    {
        [SerializeField] private List<MapLocationNode> nodes = new();

        public IReadOnlyList<MapLocationNode> Nodes => nodes;

        public void ReplaceAll(IEnumerable<MapLocationNode> authoredNodes)
        {
            nodes.Clear();
            if (authoredNodes != null)
                nodes.AddRange(authoredNodes);
        }
    }
}
