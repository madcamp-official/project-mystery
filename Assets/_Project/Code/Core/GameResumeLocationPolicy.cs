using Wake.Exploration;
using Wake.Narrative;

namespace Wake.Core
{
    /// <summary>
    /// Chooses the physical location that must be visible when Continue restores
    /// an active production scene.
    /// </summary>
    public static class GameResumeLocationPolicy
    {
        /// <summary>
        /// The saved dialogue checkpoint is authoritative while a story scene is
        /// in progress. Without a checkpoint, Continue prepares the next scene
        /// selected by <see cref="ProductionSceneDirector"/> - unless the player
        /// had already finished the previous scene and was free-roaming (never
        /// manually entered the next one), in which case the saved location must
        /// win instead of skipping straight into the next scene's dialogue.
        /// </summary>
        public static string ResolveStorySceneId(
            GameStateManager state,
            string nextAvailableSceneId)
        {
            string checkpointSceneId =
                NormalizeSceneId(state?.DialogueCheckpoint?.activeSceneId);
            if (ProductionSceneCatalog.TryGet(checkpointSceneId, out _))
            {
                return checkpointSceneId;
            }

            if (state != null && state.IsAwaitingSceneTransition)
            {
                return string.Empty;
            }

            string nextSceneId = NormalizeSceneId(nextAvailableSceneId);
            return ProductionSceneCatalog.TryGet(nextSceneId, out _)
                ? nextSceneId
                : string.Empty;
        }

        /// <summary>
        /// Story context takes precedence over the last freely visited location.
        /// The saved location and the graph starting location remain fallbacks
        /// for ending-only saves and damaged legacy data.
        /// </summary>
        public static LocationDefinition ResolveLocation(
            LocationGraph graph,
            GameStateManager state,
            string storySceneId)
        {
            if (graph == null)
            {
                return null;
            }

            string normalizedSceneId = NormalizeSceneId(storySceneId);
            if (ProductionSceneCatalog.TryGet(
                    normalizedSceneId,
                    out ProductionSceneDefinition scene))
            {
                LocationDefinition sceneLocation =
                    graph.FindByCode(scene.NarrativeLocationCode);
                if (sceneLocation != null)
                {
                    return sceneLocation;
                }
            }

            LocationDefinition savedLocation =
                graph.FindByCode(state?.CurrentLocationCode);
            return savedLocation != null
                ? savedLocation
                : graph.StartingLocation;
        }

        private static string NormalizeSceneId(string sceneId) =>
            string.IsNullOrWhiteSpace(sceneId)
                ? string.Empty
                : sceneId.Trim().ToUpperInvariant();
    }
}
