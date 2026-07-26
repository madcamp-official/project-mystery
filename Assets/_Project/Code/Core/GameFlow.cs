using UnityEngine;
using Wake.Evidence;
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
            LocationLoader.Instance?.TryLoadLocation(locationGraph.StartingLocation, out _);
        }

        /// Continue flow: restores collected evidence and jumps back to the saved location
        /// instead of replaying the intro location.
        public void ResumeGame()
        {
            if (started || locationGraph == null)
            {
                return;
            }

            started = true;

            GameStateManager state = GameStateManager.Instance;
            if (state != null)
            {
                EvidenceInventory.Instance?.RestoreFromIds(state.CollectedEvidenceIds);
            }

            LocationDefinition savedLocation = state != null
                ? locationGraph.FindByCode(state.CurrentLocationCode)
                : null;
            LocationDefinition target = savedLocation != null ? savedLocation : locationGraph.StartingLocation;

            if (target == null || LocationLoader.Instance == null)
            {
                return;
            }

            if (!LocationLoader.Instance.TryLoadLocation(target, out _) &&
                target != locationGraph.StartingLocation &&
                locationGraph.StartingLocation != null)
            {
                LocationLoader.Instance.TryLoadLocation(locationGraph.StartingLocation, out _);
            }
        }
    }
}
