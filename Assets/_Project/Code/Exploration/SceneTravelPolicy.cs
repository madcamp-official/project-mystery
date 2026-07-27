using System;
using System.Collections.Generic;
using System.Linq;
using Wake.Core;
using Wake.Narrative;

namespace Wake.Exploration
{
    public enum SceneAccessDenialReason
    {
        None,
        SceneNotRegistered,
        PhysicalLocationUnresolved,
        LocationVisualMissing,
        SceneNotUnlocked,
        PrerequisiteSceneIncomplete,
        RestrictedByPublicAnxiety,
        DialogueUnavailable,
        LocationLoadFailed
    }

    public readonly struct SceneTravelResult
    {
        private SceneTravelResult(
            bool isAllowed,
            SceneAccessDenialReason denialReason,
            string detail,
            ProductionSceneDefinition scene,
            LocationDefinition location)
        {
            IsAllowed = isAllowed;
            DenialReason = denialReason;
            Detail = detail;
            Scene = scene;
            Location = location;
        }

        public bool IsAllowed { get; }
        public SceneAccessDenialReason DenialReason { get; }
        public string Detail { get; }
        public ProductionSceneDefinition Scene { get; }
        public LocationDefinition Location { get; }

        public static SceneTravelResult Allowed(
            ProductionSceneDefinition scene,
            LocationDefinition location) =>
            new(true, SceneAccessDenialReason.None, string.Empty, scene, location);

        public static SceneTravelResult Denied(
            SceneAccessDenialReason reason,
            string detail,
            ProductionSceneDefinition scene = null,
            LocationDefinition location = null) =>
            new(false, reason, detail, scene, location);
    }

    public static class SceneTravelPolicy
    {
        // Structure Map crew/service/technical spaces. Passenger spaces are intentionally absent.
        private static readonly HashSet<string> RestrictedLocationCodes =
            new(StringComparer.Ordinal)
            {
                "SECURITY",
                "SERVICE_RAIL",
                "MEDBAY",
                "BALLAST_CONTROL_ANNEX",
                "ENGINE_CONTROL",
                "CREW_STAIRS",
                "VAULT",
                "ARCHIVE",
                "LAUNDRY",
                "SERVICE_HUB",
                "STABILIZERS",
                "BALLAST_TANKS",
                "GENERATOR",
                "WORKSHOP"
            };

        public static IReadOnlyCollection<string> RestrictedLocations =>
            RestrictedLocationCodes;

        public static SceneTravelResult EvaluateScene(
            string sceneId,
            LocationGraph graph,
            IEnumerable<string> completedSceneIds,
            int publicAnxiety)
        {
            if (!ProductionSceneCatalog.TryGet(sceneId, out ProductionSceneDefinition scene))
            {
                return SceneTravelResult.Denied(
                    SceneAccessDenialReason.SceneNotRegistered,
                    $"Scene '{sceneId?.Trim()}' is not registered.");
            }

            LocationDefinition destination = graph?.FindByCode(scene.NarrativeLocationCode);
            if (destination == null)
            {
                return SceneTravelResult.Denied(
                    SceneAccessDenialReason.PhysicalLocationUnresolved,
                    $"Scene '{scene.SceneId}' has no confirmed physical destination.",
                    scene);
            }

            HashSet<string> completed = new(
                (completedSceneIds ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim().ToUpperInvariant()),
                StringComparer.Ordinal);
            string missing = scene.Prerequisites.FirstOrDefault(value => !completed.Contains(value));
            if (!string.IsNullOrEmpty(missing))
            {
                return SceneTravelResult.Denied(
                    SceneAccessDenialReason.PrerequisiteSceneIncomplete,
                    $"Scene '{scene.SceneId}' requires completed scene '{missing}'.",
                    scene,
                    destination);
            }

            return EvaluateLocation(destination, publicAnxiety, scene);
        }

        public static SceneTravelResult EvaluateLocation(
            LocationDefinition location,
            int publicAnxiety,
            ProductionSceneDefinition scene = null)
        {
            if (location == null)
            {
                return SceneTravelResult.Denied(
                    SceneAccessDenialReason.PhysicalLocationUnresolved,
                    "Physical location is missing.",
                    scene);
            }

            if (location.ContentPrefab == null && location.BackgroundSprite == null)
            {
                return SceneTravelResult.Denied(
                    SceneAccessDenialReason.LocationVisualMissing,
                    $"Location '{location.LocationCode}' has no visual content.",
                    scene,
                    location);
            }

            if (publicAnxiety >= GameStateManager.RestrictedAreaAnxiety &&
                RestrictedLocationCodes.Contains(location.LocationCode))
            {
                return SceneTravelResult.Denied(
                    SceneAccessDenialReason.RestrictedByPublicAnxiety,
                    $"Restricted area '{location.LocationCode}' is closed at anxiety {publicAnxiety}.",
                    scene,
                    location);
            }

            return SceneTravelResult.Allowed(scene, location);
        }
    }
}
