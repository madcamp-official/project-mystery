using System;
using System.Linq;
using Wake.Core;

namespace Wake.Narrative
{
    public interface IProductionScenePlayer
    {
        string ActiveProductionSceneId { get; }
        bool StartProductionScene(string sceneId);
        bool RestoreProductionScene(ProductionDialogueCheckpoint checkpoint);
    }

    public interface IProductionSceneLaunchAvailability
    {
        bool CanStartProductionScene(string sceneId);
    }

    public readonly struct ProductionSceneEntry
    {
        public ProductionSceneEntry(
            string sceneId,
            bool restored,
            string objective)
        {
            SceneId = sceneId ?? string.Empty;
            Restored = restored;
            Objective = objective ?? string.Empty;
        }

        public string SceneId { get; }
        public bool Restored { get; }
        public string Objective { get; }
    }

    public sealed class ProductionSceneDirector
    {
        public const string OpeningSceneId = "P-01";

        private readonly GameStateManager state;
        private readonly IProductionScenePlayer player;

        public ProductionSceneDirector(
            GameStateManager state,
            IProductionScenePlayer player)
        {
            this.state = state;
            this.player = player;
        }

        public event Action<ProductionSceneEntry> SceneEntered;

        public bool StartNewGame()
        {
            state?.ClearDialogueCheckpoint();
            return TryEnter(OpeningSceneId, false);
        }

        public bool ResumeGame()
        {
            if (state == null || player == null)
            {
                return false;
            }

            ProductionDialogueCheckpoint checkpoint = state.DialogueCheckpoint;
            if (checkpoint != null)
            {
                if (IsAlreadyActive(checkpoint.activeSceneId))
                {
                    return true;
                }

                if (player.RestoreProductionScene(checkpoint))
                {
                    PublishEntry(checkpoint.activeSceneId, true);
                    return true;
                }

                state.ClearDialogueCheckpoint();
            }

            string sceneId = FindNextAvailableScene();
            return !string.IsNullOrEmpty(sceneId) && TryEnter(sceneId, false);
        }

        public string FindNextAvailableScene()
        {
            return ProductionSceneUnlockPolicy.FindNextAvailableScene(state);
        }

        public static string GetObjective(string sceneId)
        {
            if (!ProductionSceneCatalog.TryGet(
                    sceneId,
                    out ProductionSceneDefinition definition))
            {
                return string.Empty;
            }

            return definition.SceneType switch
            {
                ProductionSceneType.Prologue =>
                    $"{definition.NarrativeLocationCode}에서 승선 단서를 확인하세요.",
                ProductionSceneType.Investigation =>
                    $"{definition.NarrativeLocationCode} 현장을 조사하세요.",
                ProductionSceneType.Interrogation =>
                    $"{definition.NarrativeLocationCode}에서 증언을 확보하세요.",
                ProductionSceneType.Puzzle =>
                    $"{definition.NarrativeLocationCode}의 단서를 재구성하세요.",
                ProductionSceneType.Finale =>
                    "수집한 증거로 최종 논증을 완성하세요.",
                ProductionSceneType.Epilogue =>
                    "수사의 결말을 확인하세요.",
                _ => string.Empty
            };
        }

        private bool TryEnter(string sceneId, bool restored)
        {
            if (state == null || player == null || string.IsNullOrWhiteSpace(sceneId))
            {
                return false;
            }

            if (IsAlreadyActive(sceneId))
            {
                return true;
            }

            if (!ProductionSceneUnlockPolicy.Evaluate(sceneId, state).IsAllowed)
            {
                return false;
            }

            if (!player.StartProductionScene(sceneId))
            {
                return false;
            }

            PublishEntry(sceneId, restored);
            return true;
        }

        private void PublishEntry(string sceneId, bool restored)
        {
            string normalized = sceneId.Trim().ToUpperInvariant();
            if (ProductionSceneCatalog.TryGet(
                    normalized,
                    out ProductionSceneDefinition definition) &&
                definition.Day > 0)
            {
                state.SetTime(definition.Day, definition.TimeBlock);
            }

            InvestigationEventHub.Publish(
                InvestigationEventKind.SceneEntered,
                normalized);
            SceneEntered?.Invoke(new ProductionSceneEntry(
                normalized,
                restored,
                GetObjective(normalized)));
        }

        private bool IsAlreadyActive(string sceneId)
        {
            return !string.IsNullOrWhiteSpace(player?.ActiveProductionSceneId) &&
                   string.Equals(
                       player.ActiveProductionSceneId,
                       sceneId?.Trim(),
                       StringComparison.OrdinalIgnoreCase);
        }

    }
}
