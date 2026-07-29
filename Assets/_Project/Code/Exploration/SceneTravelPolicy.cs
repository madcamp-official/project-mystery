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
        BoardingSequenceIncomplete,
        RestrictedByPublicAnxiety,
        RouteRequired,
        LocationUndiscovered,
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
        public const string BoardingCompleteSceneId = "P-03";

        // Structure Map crew/service/technical spaces. Passenger spaces are intentionally absent.
        private static readonly HashSet<string> RestrictedLocationCodes =
            new(StringComparer.Ordinal)
            {
                "SECURITY",
                "BRIDGE",
                "INTERVIEW",
                "SERVICE7",
                "SERVICE_RAIL",
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

        public static SceneTravelResult EvaluateFreeTravel(
            LocationDefinition location,
            IEnumerable<string> completedSceneIds,
            int publicAnxiety)
        {
            SceneTravelResult locationResult =
                EvaluateLocation(location, publicAnxiety);
            if (!locationResult.IsAllowed)
            {
                return locationResult;
            }

            var completed = new HashSet<string>(
                (completedSceneIds ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim().ToUpperInvariant()),
                StringComparer.Ordinal);
            if (CanReachDuringBoarding(location.LocationCode, completed))
            {
                return locationResult;
            }

            return SceneTravelResult.Denied(
                SceneAccessDenialReason.BoardingSequenceIncomplete,
                "Complete P-01, P-02, and P-03 before using free travel.",
                location: location);
        }

        public static SceneTravelResult EvaluateMapTravel(
            LocationDefinition location,
            IEnumerable<string> completedSceneIds,
            IEnumerable<string> unlockedSceneIds,
            int publicAnxiety)
        {
            SceneTravelResult basic = EvaluateFreeTravel(
                location,
                completedSceneIds,
                publicAnxiety);
            if (!basic.IsAllowed)
                return basic;

            Wake.UI.MapLocationPlacement placement =
                Wake.UI.MapDeckCatalog.Find(location?.LocationCode);
            if (placement == null)
            {
                return SceneTravelResult.Denied(
                    SceneAccessDenialReason.LocationUndiscovered,
                    "The destination is not registered on the passenger map.",
                    location: location);
            }

            if (placement.TravelTier == Wake.UI.MapTravelTier.RouteOnly)
            {
                return SceneTravelResult.Denied(
                    SceneAccessDenialReason.RouteRequired,
                    "This area can only be entered through an adjacent route.",
                    location: location);
            }

            if (placement.TravelTier ==
                Wake.UI.MapTravelTier.ConditionalFastTravel)
            {
                var known = new HashSet<string>(
                    (completedSceneIds ?? Array.Empty<string>())
                        .Concat(unlockedSceneIds ?? Array.Empty<string>()),
                    StringComparer.Ordinal);
                bool discovered = ProductionSceneCatalog.All
                    .Where(scene =>
                        CanonicalLocationCatalog.FindSpec(
                            scene.NarrativeLocationCode)?.Code ==
                        location.LocationCode)
                    .Any(scene => known.Contains(scene.SceneId));
                if (!discovered)
                {
                    return SceneTravelResult.Denied(
                        SceneAccessDenialReason.LocationUndiscovered,
                        "The route to this destination has not been discovered.",
                        location: location);
                }
            }

            return basic;
        }

        private static bool CanReachDuringBoarding(
            string locationCode,
            HashSet<string> completed)
        {
            if (completed.Contains(BoardingCompleteSceneId))
            {
                return true;
            }

            return locationCode switch
            {
                "PORT" => true,
                "GANGWAY" => completed.Contains("P-01"),
                "RICHARD_SUITE" =>
                    completed.Contains("P-01") &&
                    completed.Contains("P-02"),
                _ => false
            };
        }
    }
}
