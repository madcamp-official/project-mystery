using System;
using Wake.Core;
using Wake.Narrative;

namespace Wake.Exploration
{
    public sealed class ProductionSceneTravelCoordinator
    {
        private readonly LocationGraph graph;
        private readonly GameStateManager state;
        private readonly IProductionScenePlayer player;
        private readonly Func<LocationDefinition, bool> tryLoadLocation;
        private readonly Action<string> prepareNarrativeScene;
        private string deferredPhysicalSceneId = string.Empty;

        public ProductionSceneTravelCoordinator(
            LocationGraph graph,
            GameStateManager state,
            IProductionScenePlayer player,
            Func<LocationDefinition, bool> tryLoadLocation,
            Action<string> prepareNarrativeScene = null)
        {
            this.graph = graph;
            this.state = state;
            this.player = player;
            this.tryLoadLocation = tryLoadLocation;
            this.prepareNarrativeScene = prepareNarrativeScene;
        }

        public SceneTravelResult TryEnter(string sceneId)
        {
            ProductionSceneUnlockResult unlock =
                ProductionSceneUnlockPolicy.Evaluate(sceneId, state);
            if (!unlock.IsAllowed)
            {
                return SceneTravelResult.Denied(
                    SceneAccessDenialReason.SceneNotUnlocked,
                    unlock.Detail,
                    unlock.Scene);
            }

            SceneTravelResult evaluated = SceneTravelPolicy.EvaluateScene(
                sceneId,
                graph,
                state?.CompletedProductionSceneIds,
                state != null ? state.PublicAnxiety : 0);
            if (!evaluated.IsAllowed)
            {
                return evaluated;
            }

            return TryLaunch(evaluated, true);
        }

        public SceneTravelResult TryEnterDialogueOnly(string sceneId)
        {
            ProductionSceneUnlockResult unlock =
                ProductionSceneUnlockPolicy.Evaluate(sceneId, state);
            if (!unlock.IsAllowed)
            {
                return SceneTravelResult.Denied(
                    SceneAccessDenialReason.SceneNotUnlocked,
                    unlock.Detail,
                    unlock.Scene);
            }

            SceneTravelResult evaluated = DialogueOnlySceneAccess.Evaluate(
                sceneId,
                state?.CompletedProductionSceneIds,
                state?.FinalEndingId);
            return evaluated.IsAllowed
                ? TryLaunch(evaluated, false)
                : evaluated;
        }

        private SceneTravelResult TryLaunch(
            SceneTravelResult evaluated,
            bool loadPhysicalLocation)
        {
            if (IsAlreadyActive(evaluated.Scene.SceneId))
            {
                return evaluated;
            }

            if (!CanLaunch(evaluated.Scene.SceneId))
            {
                return SceneTravelResult.Denied(
                    SceneAccessDenialReason.DialogueUnavailable,
                    $"Dialogue runtime cannot start scene '{evaluated.Scene.SceneId}'.",
                    evaluated.Scene,
                    evaluated.Location);
            }

            if (loadPhysicalLocation)
                prepareNarrativeScene?.Invoke(evaluated.Scene.SceneId);

            if (loadPhysicalLocation &&
                (tryLoadLocation == null ||
                 !tryLoadLocation(evaluated.Location)))
            {
                return SceneTravelResult.Denied(
                    SceneAccessDenialReason.LocationLoadFailed,
                    $"Location '{evaluated.Location.LocationCode}' could not load visual content.",
                    evaluated.Scene,
                    evaluated.Location);
            }

            // Physical rooms are exploration spaces first. Their production
            // scene starts from the focus character hotspot, not map travel.
            if (!loadPhysicalLocation &&
                !player.StartProductionScene(evaluated.Scene.SceneId))
            {
                return SceneTravelResult.Denied(
                    SceneAccessDenialReason.DialogueUnavailable,
                    $"Dialogue runtime rejected scene '{evaluated.Scene.SceneId}' after travel.",
                    evaluated.Scene,
                    evaluated.Location);
            }

            if (state != null && evaluated.Scene.Day > 0)
            {
                state.SetTime(
                    evaluated.Scene.Day,
                    evaluated.Scene.TimeBlock);
            }

            InvestigationEventHub.Publish(
                InvestigationEventKind.SceneEntered,
                evaluated.Scene.SceneId,
                evaluated.Location != null
                    ? evaluated.Location.LocationCode
                    : evaluated.Scene.NarrativeLocationCode);
            if (loadPhysicalLocation)
                deferredPhysicalSceneId = evaluated.Scene.SceneId;
            return evaluated;
        }

        private bool CanLaunch(string sceneId)
        {
            if (player == null)
            {
                return false;
            }

            if (player is IProductionSceneLaunchAvailability availability)
            {
                return availability.CanStartProductionScene(sceneId);
            }

            return string.IsNullOrWhiteSpace(player.ActiveProductionSceneId) ||
                   string.Equals(
                       player.ActiveProductionSceneId,
                       sceneId,
                       StringComparison.OrdinalIgnoreCase);
        }

        private bool IsAlreadyActive(string sceneId)
        {
            return string.Equals(
                       deferredPhysicalSceneId,
                       sceneId,
                       StringComparison.OrdinalIgnoreCase) ||
                   (!string.IsNullOrWhiteSpace(
                        player?.ActiveProductionSceneId) &&
                    string.Equals(
                        player.ActiveProductionSceneId,
                        sceneId,
                        StringComparison.OrdinalIgnoreCase));
        }
    }
}
