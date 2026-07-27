using UnityEngine;
using Wake.Evidence;
using Wake.Exploration;
using Wake.Narrative;

namespace Wake.Core
{
    public class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        [SerializeField] private LocationGraph locationGraph;

        private bool started;
        public bool HasActiveSession => started;

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
            if (LocationLoader.Instance != null &&
                LocationLoader.Instance.TryLoadLocation(
                    locationGraph.StartingLocation,
                    out _))
            {
                CreateSceneDirector()?.StartNewGame();
            }
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

            ProductionSceneDirector director = CreateSceneDirector();
            string storySceneId = GameResumeLocationPolicy.ResolveStorySceneId(
                state,
                director?.FindNextAvailableScene());
            LocationDefinition target = GameResumeLocationPolicy.ResolveLocation(
                locationGraph,
                state,
                storySceneId);

            if (target == null || LocationLoader.Instance == null)
            {
                return;
            }

            TryLoadLocationWithStartingFallback(target);

            bool resumed = director?.ResumeGame() ?? false;
            if (resumed)
            {
                AlignLocationWithActiveStoryScene();
            }
            if (!resumed && !string.IsNullOrEmpty(state?.FinalEndingId))
            {
                FindFirstObjectByType<Wake.UI.ProductionEndingUIController>()
                    ?.ShowStoredEnding();
            }
        }

        public void ResetSession()
        {
            started = false;
        }

        private static ProductionSceneDirector CreateSceneDirector()
        {
            return GameStateManager.Instance != null &&
                   DialogueController.Instance != null
                ? new ProductionSceneDirector(
                    GameStateManager.Instance,
                    DialogueController.Instance)
                : null;
        }

        private void AlignLocationWithActiveStoryScene()
        {
            string activeSceneId =
                DialogueController.Instance?.ActiveProductionSceneId;
            LocationDefinition activeSceneLocation =
                GameResumeLocationPolicy.ResolveLocation(
                    locationGraph,
                    GameStateManager.Instance,
                    activeSceneId);
            if (activeSceneLocation != null)
            {
                TryLoadLocationWithStartingFallback(activeSceneLocation);
            }
        }

        private void TryLoadLocationWithStartingFallback(
            LocationDefinition target)
        {
            if (LocationLoader.Instance.TryLoadLocation(target, out _) ||
                target == locationGraph.StartingLocation ||
                locationGraph.StartingLocation == null)
            {
                return;
            }

            LocationLoader.Instance.TryLoadLocation(
                locationGraph.StartingLocation,
                out _);
        }
    }
}
