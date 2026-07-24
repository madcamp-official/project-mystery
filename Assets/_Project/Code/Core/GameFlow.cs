using UnityEngine;
using Seat0A.Exploration;

namespace Seat0A.Core
{
    public class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        [SerializeField] private RoomGraph roomGraph;

        private bool started;

        private void Awake()
        {
            Instance = this;
        }

        public void BeginGame()
        {
            if (started || roomGraph == null || roomGraph.StartingRoom == null)
            {
                return;
            }

            started = true;
            RoomLoader.Instance.LoadRoom(roomGraph.StartingRoom);
        }
    }
}
