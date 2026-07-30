using System;
using System.Collections.Generic;
using Wake.Narrative;

namespace Wake.Core
{
    public readonly struct SaveSlotSummary
    {
        public SaveSlotSummary(
            bool isOccupied,
            string chapterLabel,
            bool isCompleted)
        {
            IsOccupied = isOccupied;
            ChapterLabel = chapterLabel ?? string.Empty;
            IsCompleted = isCompleted;
        }

        public bool IsOccupied { get; }
        public string ChapterLabel { get; }
        public bool IsCompleted { get; }
    }

    internal static class SaveSlotSummaryResolver
    {
        private const string PrologueLabel = "\uD504\uB864\uB85C\uADF8";
        private const string EpilogueLabel = "\uC5D0\uD544\uB85C\uADF8";

        public static SaveSlotSummary Resolve(
            GameStateSaveData data,
            bool isLegacy)
        {
            if (data == null)
            {
                return new SaveSlotSummary(
                    false,
                    string.Empty,
                    false);
            }

            var completedSceneIds = new HashSet<string>(
                data.completedProductionSceneIds ?? new List<string>(),
                StringComparer.OrdinalIgnoreCase);
            bool isCompleted = HasCompletedEpilogue(completedSceneIds);

            string checkpointSceneId = data.dialogueCheckpoint?.activeSceneId;
            if (ProductionSceneCatalog.TryGet(
                    checkpointSceneId,
                    out ProductionSceneDefinition checkpointScene))
            {
                return Occupied(
                    ChapterLabelFor(checkpointScene),
                    isCompleted);
            }

            if (data.awaitingSceneTransition &&
                TryGetPendingTransitionLabel(
                    completedSceneIds,
                    out string transitionLabel))
            {
                return Occupied(transitionLabel, isCompleted);
            }

            if (TryGetLastCompletedScene(
                    completedSceneIds,
                    out ProductionSceneDefinition completedScene))
            {
                return Occupied(
                    ChapterLabelFor(completedScene),
                    isCompleted);
            }

            if (HasFallbackProgress(data, isLegacy))
            {
                return Occupied(DayLabel(data.day), isCompleted);
            }

            return Occupied(PrologueLabel, isCompleted);
        }

        private static bool HasCompletedEpilogue(
            HashSet<string> completedSceneIds)
        {
            foreach (ProductionSceneDefinition scene in
                     ProductionSceneCatalog.All)
            {
                if (scene.SceneType == ProductionSceneType.Epilogue &&
                    completedSceneIds.Contains(scene.SceneId))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetPendingTransitionLabel(
            HashSet<string> completedSceneIds,
            out string chapterLabel)
        {
            chapterLabel = string.Empty;
            bool found = false;
            foreach (ChapterTransitionRequest transition in
                     ProductionChapterTransitionCatalog.All)
            {
                if (!completedSceneIds.Contains(transition.CompletedSceneId))
                {
                    continue;
                }

                chapterLabel = transition.ChapterLabel;
                found = true;
            }

            return found;
        }

        private static bool TryGetLastCompletedScene(
            HashSet<string> completedSceneIds,
            out ProductionSceneDefinition completedScene)
        {
            completedScene = null;
            foreach (ProductionSceneDefinition scene in
                     ProductionSceneCatalog.All)
            {
                if (completedSceneIds.Contains(scene.SceneId))
                {
                    completedScene = scene;
                }
            }

            return completedScene != null;
        }

        private static string ChapterLabelFor(ProductionSceneDefinition scene)
        {
            return scene.SceneType switch
            {
                ProductionSceneType.Prologue => PrologueLabel,
                ProductionSceneType.Epilogue => EpilogueLabel,
                _ => DayLabel(scene.Day)
            };
        }

        private static string DayLabel(int savedDay)
        {
            int maximumDay = 1;
            foreach (ProductionSceneDefinition scene in
                     ProductionSceneCatalog.All)
            {
                maximumDay = Math.Max(maximumDay, scene.Day);
            }

            int day = Math.Min(Math.Max(savedDay, 1), maximumDay);
            return $"DAY {day}";
        }

        private static bool HasFallbackProgress(
            GameStateSaveData data,
            bool isLegacy)
        {
            return isLegacy ||
                   data.day > 1 ||
                   !string.IsNullOrWhiteSpace(data.currentLocationCode) ||
                   !string.IsNullOrWhiteSpace(data.finalEndingId) ||
                   HasAny(data.trust) ||
                   HasAny(data.flags) ||
                   HasAny(data.collectedEvidenceIds) ||
                   HasAny(data.completedObjectiveIds) ||
                   HasAny(data.puzzleSessions) ||
                   HasAny(data.unlockedDeductionIds) ||
                   HasAny(data.runtimeCounters) ||
                   HasAny(data.appliedDialogueEffectIds) ||
                   HasAny(data.completedNpcInteractionIds);
        }

        private static bool HasAny<T>(ICollection<T> values) =>
            values != null && values.Count > 0;

        private static SaveSlotSummary Occupied(
            string chapterLabel,
            bool isCompleted)
        {
            return new SaveSlotSummary(
                true,
                chapterLabel,
                isCompleted);
        }
    }
}
