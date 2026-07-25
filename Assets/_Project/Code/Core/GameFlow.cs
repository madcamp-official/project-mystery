using UnityEngine;
using Wake.Exploration;

namespace Wake.Core
{
    public class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        [SerializeField] private LocationGraph locationGraph;

        private bool started;

        private void Awake()
        {
            Instance = this;
        }

        public void BeginGame()
        {
            if (started || locationGraph == null || locationGraph.StartingLocation == null)
            {
                return;
            }

            started = true;
            LocationLoader.Instance.LoadLocation(locationGraph.StartingLocation);
        }
    }
}
