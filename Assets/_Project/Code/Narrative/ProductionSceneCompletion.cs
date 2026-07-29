using System;
using System.Collections.Generic;
using System.Linq;
using Wake.Core;

namespace Wake.Narrative
{
    public enum ProductionScenePhase
    {
        NotStarted,
        DialogueActive,
        InteractionPending,
        Completed
    }

    public sealed class ProductionSceneCompletionRequirement
    {
        public ProductionSceneCompletionRequirement(
            string sceneId,
            string interactionId,
            string nextSceneId)
        {
            SceneId = NormalizeSceneId(sceneId);
            InteractionId = NormalizeInteractionId(interactionId);
            NextSceneId = NormalizeSceneId(nextSceneId);
        }

        public string SceneId { get; }
        public string InteractionId { get; }
        public string NextSceneId { get; }

        public bool Matches(string interactionId) =>
            string.Equals(
                InteractionId,
                NormalizeInteractionId(interactionId),
                StringComparison.Ordinal);

        internal static string NormalizeSceneId(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToUpperInvariant();

        internal static string NormalizeInteractionId(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant().Replace(' ', '_');
    }

    public readonly struct ProductionSceneCompletionDiagnostic
    {
        public ProductionSceneCompletionDiagnostic(
            string sceneId,
            string message)
        {
            SceneId = sceneId ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public string SceneId { get; }
        public string Message { get; }
    }

    public static class ProductionSceneCompletionCatalog
    {
        public const string ExitInspectionInteraction = "exit_inspection";
        public const string BloodPatternInteraction = "blood_pattern";
        public const string CameraBlindSpotInteraction = "camera_blind_spot";
        public const string MarcusInterrogationInteraction = "marcus_interrogation";
        public const string CargoRailInteraction = "cargo_rail_branch";
        public const string TimelineInteraction = "timeline_12_cards";
        public const string OrpheusInteraction = "orpheus_audio_restoration";
        public const string FinalAccusationInteraction = "final_accusation";

        private static readonly ProductionSceneCompletionRequirement[] Entries =
        {
            R("D2-01", ExitInspectionInteraction, "D2-02"),
            R("D2-02", BloodPatternInteraction, "D2-03"),
            R("D2-04", CameraBlindSpotInteraction, "D2-05"),
            R("D4-04", MarcusInterrogationInteraction, "D5-01"),
            R("D6-02", CargoRailInteraction, "D6-03"),
            R("D6-05", TimelineInteraction, "D7-01"),
            R("D7-03", OrpheusInteraction, "D7-04"),
            R("D8-01", FinalAccusationInteraction, "D8-02")
        };

        private static readonly IReadOnlyDictionary<string,
            ProductionSceneCompletionRequirement> ByScene =
            Entries.ToDictionary(item => item.SceneId, StringComparer.Ordinal);

        public static IReadOnlyList<ProductionSceneCompletionRequirement> All =>
            Entries;

        public static bool TryGet(
            string sceneId,
            out ProductionSceneCompletionRequirement requirement) =>
            ByScene.TryGetValue(
                ProductionSceneCompletionRequirement.NormalizeSceneId(sceneId),
                out requirement);

        public static IReadOnlyList<ProductionSceneCompletionDiagnostic> Validate(
            IEnumerable<ProductionSceneDefinition> scenes)
        {
            var diagnostics = new List<ProductionSceneCompletionDiagnostic>();
            ProductionSceneDefinition[] definitions =
                (scenes ?? Array.Empty<ProductionSceneDefinition>()).ToArray();
            var sceneIds = new HashSet<string>(
                definitions.Select(item => item.SceneId),
                StringComparer.Ordinal);

            foreach (ProductionSceneCompletionRequirement requirement in Entries)
            {
                if (!sceneIds.Contains(requirement.SceneId))
                {
                    diagnostics.Add(new ProductionSceneCompletionDiagnostic(
                        requirement.SceneId,
                        "상호작용 완료 계약의 장면이 프로덕션 씬 일정에 없습니다."));
                }
            }

            foreach (ProductionSceneDefinition scene in definitions.Where(item =>
                         item.SceneType == ProductionSceneType.Puzzle &&
                         !ByScene.ContainsKey(item.SceneId)))
            {
                diagnostics.Add(new ProductionSceneCompletionDiagnostic(
                    scene.SceneId,
                    "퍼즐 장면에 등록된 완료 핸들러가 없어 대사 완료로 진행됩니다."));
            }

            return diagnostics;
        }

        private static ProductionSceneCompletionRequirement R(
            string sceneId,
            string interactionId,
            string nextSceneId) =>
            new(sceneId, interactionId, nextSceneId);
    }

    public static class ProductionSceneCompletionGate
    {
        public static bool CanStartInteraction(
            GameStateManager state,
            string sceneId,
            string interactionId)
        {
            if (state == null ||
                !ProductionSceneCompletionCatalog.TryGet(
                    sceneId,
                    out ProductionSceneCompletionRequirement requirement) ||
                !requirement.Matches(interactionId))
            {
                return false;
            }

            bool sessionCompleted = state.TryGetPuzzleSession(
                                        requirement.InteractionId,
                                        out PuzzleSessionState session) &&
                                    session.completed;
            if (state.HasCompletedScene(requirement.SceneId) ||
                sessionCompleted)
            {
                TryComplete(
                    state,
                    requirement.SceneId,
                    requirement.InteractionId);
                return false;
            }

            return true;
        }

        public static bool TryComplete(
            GameStateManager state,
            string sceneId,
            string interactionId) =>
            TryComplete(
                state,
                sceneId,
                interactionId,
                out _,
                out _);

        public static bool TryComplete(
            GameStateManager state,
            string sceneId,
            string interactionId,
            out bool newlyCompleted,
            out bool checkpointCleared)
        {
            newlyCompleted = false;
            checkpointCleared = false;
            if (state == null ||
                !ProductionSceneCompletionCatalog.TryGet(
                    sceneId,
                    out ProductionSceneCompletionRequirement requirement) ||
                !requirement.Matches(interactionId))
            {
                return false;
            }

            if (!state.TrySynchronizeProductionSceneCompletion(
                    requirement.SceneId,
                    requirement.InteractionId,
                    out newlyCompleted,
                    out checkpointCleared))
            {
                return false;
            }

            if (newlyCompleted)
            {
                InvestigationEventHub.Publish(
                    InvestigationEventKind.SceneCompleted,
                    requirement.SceneId,
                    ResolveNextScene(state, requirement));
            }
            return true;
        }

        private static string ResolveNextScene(
            GameStateManager state,
            ProductionSceneCompletionRequirement requirement)
        {
            if (ProductionChapterTransitionCatalog.TryGet(
                    requirement.SceneId,
                    out ChapterTransitionRequest transition))
            {
                return transition.NextSceneId;
            }

            if (requirement.SceneId != "D8-01")
            {
                return requirement.NextSceneId;
            }

            return ProductionEndingCatalog.GetNextDialogueScene(
                state.FinalEndingId,
                state.HasCompletedScene(ProductionEndingCatalog.ConfessionSceneId),
                state.HasCompletedScene(ProductionEndingCatalog.EpilogueSceneId));
        }

    }
}
