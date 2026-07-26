using System;
using System.Collections.Generic;
using System.Linq;
using Wake.Core;
using Wake.Narrative;

namespace Wake.Exploration
{
    public enum NarrativeLocationKind
    {
        Undocumented,
        Physical,
        DialogueOnly
    }
    public readonly struct NarrativeLocationContext
    {
        public NarrativeLocationContext(
            string sceneId,
            string narrativeCode,
            string physicalLocationCode,
            string displayName,
            NarrativeLocationKind kind)
        {
            SceneId = sceneId ?? string.Empty;
            NarrativeCode = narrativeCode ?? string.Empty;
            PhysicalLocationCode = physicalLocationCode ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Kind = kind;
        }

        public string SceneId { get; }
        public string NarrativeCode { get; }
        public string PhysicalLocationCode { get; }
        public string DisplayName { get; }
        public NarrativeLocationKind Kind { get; }
        public bool IsPhysicalLocationResolved =>
            Kind == NarrativeLocationKind.Physical;
        public bool IsDialogueOnly =>
            Kind == NarrativeLocationKind.DialogueOnly;
        public string WarningMessage => IsDialogueOnly
            ? "배경 미확정 · 현재 배경 유지"
            : string.Empty;
    }

    public static class NarrativeLocationContextResolver
    {
        public static NarrativeLocationContext Resolve(string sceneId)
        {
            if (!ProductionSceneCatalog.TryGet(
                    sceneId,
                    out ProductionSceneDefinition scene))
            {
                return default;
            }

            CanonicalLocationSpec spec =
                CanonicalLocationCatalog.FindSpec(scene.NarrativeLocationCode);
            NarrativeLocationKind kind = spec != null
                ? NarrativeLocationKind.Physical
                : CanonicalLocationCatalog.UnresolvedCodes.Contains(
                    scene.NarrativeLocationCode)
                    ? NarrativeLocationKind.DialogueOnly
                    : NarrativeLocationKind.Undocumented;
            return new NarrativeLocationContext(
                scene.SceneId,
                scene.NarrativeLocationCode,
                spec?.Code,
                spec?.DisplayName ??
                scene.NarrativeLocationCode.Replace('_', ' '),
                kind);
        }
    }

    public static class DialogueOnlySceneAccess
    {
        public static SceneTravelResult Evaluate(
            string sceneId,
            IEnumerable<string> completedSceneIds,
            string finalEndingId = "")
        {
            if (!ProductionSceneCatalog.TryGet(
                    sceneId,
                    out ProductionSceneDefinition scene))
            {
                return SceneTravelResult.Denied(
                    SceneAccessDenialReason.SceneNotRegistered,
                    $"Scene '{sceneId?.Trim()}' is not registered.");
            }

            if (CanonicalLocationCatalog.FindSpec(
                    scene.NarrativeLocationCode) != null ||
                !CanonicalLocationCatalog.UnresolvedCodes.Contains(
                    scene.NarrativeLocationCode))
            {
                return SceneTravelResult.Denied(
                    SceneAccessDenialReason.PhysicalLocationUnresolved,
                    $"Scene '{scene.SceneId}' is not an approved dialogue-only context.",
                    scene);
            }

            HashSet<string> completed = new(
                (completedSceneIds ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim().ToUpperInvariant()),
                StringComparer.Ordinal);
            string missing = scene.Prerequisites
                .FirstOrDefault(value =>
                    !completed.Contains(value) &&
                    !(value == "D8-01 정답" &&
                      FinalAccusationResolver.OpensD8Confession(
                          finalEndingId)));
            return string.IsNullOrEmpty(missing)
                ? SceneTravelResult.Allowed(scene, null)
                : SceneTravelResult.Denied(
                    SceneAccessDenialReason.PrerequisiteSceneIncomplete,
                    $"Scene '{scene.SceneId}' requires completed condition '{missing}'.",
                    scene);
        }
    }
}
