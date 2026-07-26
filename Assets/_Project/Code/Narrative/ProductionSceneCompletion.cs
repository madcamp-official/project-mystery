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
            string interactionId)
        {
            SceneId = NormalizeSceneId(sceneId);
            InteractionId = NormalizeInteractionId(interactionId);
        }

        public string SceneId { get; }
        public string InteractionId { get; }

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
        public const string BloodPatternInteraction = "blood_pattern";
        public const string MarcusInterrogationInteraction = "marcus_interrogation";
        public const string CargoRailInteraction = "cargo_rail_branch";
        public const string TimelineInteraction = "timeline_12_cards";
        public const string OrpheusInteraction = "orpheus_audio_restoration";
        public const string FinalAccusationInteraction = "final_accusation";

        private static readonly ProductionSceneCompletionRequirement[] Entries =
        {
            R("D2-02", BloodPatternInteraction),
            R("D4-04", MarcusInterrogationInteraction),
            R("D6-02", CargoRailInteraction),
            R("D6-05", TimelineInteraction),
            R("D7-03", OrpheusInteraction),
            R("D8-01", FinalAccusationInteraction)
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
            string interactionId) =>
            new(sceneId, interactionId);
    }

    public static class ProductionSceneCompletionGate
    {
        public static bool TryComplete(
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

            if (state.HasCompletedScene(requirement.SceneId))
            {
                return true;
            }

            return state.RecordCompletedScene(requirement.SceneId);
        }
    }
}
