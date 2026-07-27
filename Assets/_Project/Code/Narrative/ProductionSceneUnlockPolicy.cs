using System;
using System.Collections.Generic;
using System.Linq;
using Wake.Core;

namespace Wake.Narrative
{
    public enum ProductionSceneUnlockDenial
    {
        None,
        StateUnavailable,
        SceneUnknown,
        SceneLocked,
        PrerequisiteIncomplete,
        FinalAccusationIncomplete
    }

    public readonly struct ProductionSceneUnlockResult
    {
        private ProductionSceneUnlockResult(
            bool isAllowed,
            ProductionSceneUnlockDenial denial,
            string detail,
            ProductionSceneDefinition scene)
        {
            IsAllowed = isAllowed;
            Denial = denial;
            Detail = detail ?? string.Empty;
            Scene = scene;
        }

        public bool IsAllowed { get; }
        public ProductionSceneUnlockDenial Denial { get; }
        public string Detail { get; }
        public ProductionSceneDefinition Scene { get; }

        public static ProductionSceneUnlockResult Allowed(
            ProductionSceneDefinition scene) =>
            new(true, ProductionSceneUnlockDenial.None, string.Empty, scene);

        public static ProductionSceneUnlockResult Denied(
            ProductionSceneUnlockDenial denial,
            string detail,
            ProductionSceneDefinition scene = null) =>
            new(false, denial, detail, scene);
    }

    public static class ProductionSceneUnlockPolicy
    {
        public static ProductionSceneUnlockResult Evaluate(
            string sceneId,
            GameStateManager state)
        {
            if (state == null)
            {
                return ProductionSceneUnlockResult.Denied(
                    ProductionSceneUnlockDenial.StateUnavailable,
                    "Game state is unavailable.");
            }
            if (!ProductionSceneCatalog.TryGet(
                    sceneId,
                    out ProductionSceneDefinition scene))
            {
                return ProductionSceneUnlockResult.Denied(
                    ProductionSceneUnlockDenial.SceneUnknown,
                    $"Scene '{sceneId?.Trim()}' is not registered.");
            }

            bool isOpening = scene.SceneId == ProductionSceneDirector.OpeningSceneId;
            if (!isOpening &&
                !state.HasCompletedScene(scene.SceneId) &&
                !state.IsProductionSceneUnlocked(scene.SceneId))
            {
                return ProductionSceneUnlockResult.Denied(
                    ProductionSceneUnlockDenial.SceneLocked,
                    $"Scene '{scene.SceneId}' has not been unlocked.",
                    scene);
            }

            foreach (string prerequisite in scene.Prerequisites)
            {
                if (scene.SceneId == ProductionEndingCatalog.EpilogueSceneId &&
                    prerequisite ==
                    ProductionEndingCatalog.ConfessionSceneId &&
                    ProductionEndingCatalog.TryGet(
                        state.FinalEndingId,
                        out ProductionEndingDefinition ending) &&
                    !ending.OpensConfession)
                {
                    continue;
                }

                if (!ProductionSceneCatalog.TryGet(prerequisite, out _))
                {
                    if (!FinalAccusationResolver.OpensD8Confession(state.FinalEndingId))
                    {
                        return ProductionSceneUnlockResult.Denied(
                            ProductionSceneUnlockDenial.FinalAccusationIncomplete,
                            "The final accusation has not opened the confession scene.",
                            scene);
                    }
                    continue;
                }

                if (!state.HasCompletedScene(prerequisite))
                {
                    return ProductionSceneUnlockResult.Denied(
                        ProductionSceneUnlockDenial.PrerequisiteIncomplete,
                        $"Scene '{scene.SceneId}' requires '{prerequisite}'.",
                        scene);
                }
            }
            return ProductionSceneUnlockResult.Allowed(scene);
        }

        public static string FindNextAvailableScene(GameStateManager state)
        {
            if (state == null)
            {
                return ProductionSceneDirector.OpeningSceneId;
            }

            ProductionSceneDefinition next = ProductionSceneCatalog.All
                .Where(scene => !state.HasCompletedScene(scene.SceneId))
                .FirstOrDefault(scene => Evaluate(scene.SceneId, state).IsAllowed);
            return next?.SceneId ?? string.Empty;
        }

        public static IReadOnlyList<string> GetAvailableSceneIds(
            GameStateManager state)
        {
            return ProductionSceneCatalog.All
                .Where(scene => Evaluate(scene.SceneId, state).IsAllowed)
                .Select(scene => scene.SceneId)
                .ToArray();
        }
    }
}
