using System.Collections.Generic;
using UnityEngine;

namespace Seat0A.Exploration
{
    [CreateAssetMenu(fileName = "RoomDefinition", menuName = "Seat0A/Room Definition")]
    public class RoomDefinition : ScriptableObject
    {
        [SerializeField] private string roomId;
        [SerializeField] private string displayName;
        [SerializeField] private string sceneName;
        [SerializeField] private List<RoomDefinition> connectedRooms = new();

        public string RoomId => roomId;
        public string DisplayName => displayName;
        public string SceneName => sceneName;
        public IReadOnlyList<RoomDefinition> ConnectedRooms => connectedRooms;
    }
}
