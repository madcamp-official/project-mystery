using System.Collections.Generic;
using UnityEngine;

namespace Wake.UI
{
    [CreateAssetMenu(
        fileName = "Deck_RoomMasks",
        menuName = "Wake/Map/Room Masks")]
    public sealed class RoomMasksAsset : MapGeometryAsset
    {
        [SerializeField] private List<MapRoomMask> masks = new();

        public IReadOnlyList<MapRoomMask> Masks => masks;

        public void ReplaceAll(IEnumerable<MapRoomMask> authoredMasks)
        {
            masks.Clear();
            if (authoredMasks != null)
                masks.AddRange(authoredMasks);
        }
    }
}
