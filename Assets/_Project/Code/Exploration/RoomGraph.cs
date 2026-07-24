using System.Collections.Generic;
using UnityEngine;

namespace Seat0A.Exploration
{
    [CreateAssetMenu(fileName = "RoomGraph", menuName = "Seat0A/Room Graph")]
    public class RoomGraph : ScriptableObject
    {
        [SerializeField] private List<RoomDefinition> rooms = new();
        [SerializeField] private RoomDefinition startingRoom;

        public IReadOnlyList<RoomDefinition> Rooms => rooms;
        public RoomDefinition StartingRoom => startingRoom;

        public RoomDefinition FindById(string roomId)
        {
            foreach (RoomDefinition room in rooms)
            {
                if (room != null && room.RoomId == roomId)
                {
                    return room;
                }
            }

            return null;
        }
    }
}
