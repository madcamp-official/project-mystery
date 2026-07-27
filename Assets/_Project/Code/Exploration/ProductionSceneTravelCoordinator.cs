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

        public ProductionSceneTravelCoordinator(
            LocationGraph graph,
            GameStateManager state,
            IProductionScenePlayer player,
            Func<LocationDefinition, bool> tryLoadLocation)
        {
            this.graph = graph;
            this.state = state;
            this.player = player;
            this.tryLoadLocation = tryLoadLocation;
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

            if (!player.StartProductionScene(evaluated.Scene.SceneId))
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
            return !string.IsNullOrWhiteSpace(player?.ActiveProductionSceneId) &&
                   string.Equals(
                       player.ActiveProductionSceneId,
                       sceneId,
                       StringComparison.OrdinalIgnoreCase);
        }
    }
}
