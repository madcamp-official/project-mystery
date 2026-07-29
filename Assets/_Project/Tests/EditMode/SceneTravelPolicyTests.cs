using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.Core;
using Wake.Exploration;
using Wake.Narrative;
using Wake.UI;

namespace Wake.Tests
{
    public class SceneTravelPolicyTests
    {
        private const string SaveKey = "UNDER_THE_HORIZON_GAME_STATE_V2";
        private const string GraphPath =
            "Assets/_Project/Content/Locations/LocationGraph.asset";

        private GameObject stateHost;
        private GameStateManager state;
        private LocationGraph graph;

        [SetUp]
        public void SetUp()
        {
            DestroyExisting<GameStateManager>();
            DestroyExisting<LocationLoader>();
            PlayerPrefs.DeleteKey(SaveKey);
            stateHost = new GameObject("SceneTravelPolicyState");
            state = stateHost.AddComponent<GameStateManager>();
            EnsureAwake(state, GameStateManager.Instance);
            state.StartNewGame();
            graph = AssetDatabase.LoadAssetAtPath<LocationGraph>(GraphPath);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(stateHost);
            DestroyExisting<GameStateManager>();
            DestroyExisting<LocationLoader>();
            PlayerPrefs.DeleteKey(SaveKey);
        }

        [Test]
        public void SceneAccess_RequiresEveryRegisteredPrerequisite()
        {
            SceneTravelResult denied = SceneTravelPolicy.EvaluateScene(
                "D2-04",
                graph,
                System.Array.Empty<string>(),
                15);
            SceneTravelResult allowed = SceneTravelPolicy.EvaluateScene(
                "D2-04",
                graph,
                new[] { "D2-01" },
                15);

            Assert.That(denied.IsAllowed, Is.False);
            Assert.That(denied.DenialReason,
                Is.EqualTo(SceneAccessDenialReason.PrerequisiteSceneIncomplete));
            Assert.That(denied.Detail, Does.Contain("D2-01"));
            Assert.That(allowed.IsAllowed, Is.True);
            Assert.That(allowed.Location.LocationCode, Is.EqualTo("SECURITY"));
        }

        [Test]
        public void RestrictedArea_ClosesAtAnxietySeventy()
        {
            SceneTravelResult belowThreshold = SceneTravelPolicy.EvaluateScene(
                "D2-04",
                graph,
                new[] { "D2-01" },
                69);
            SceneTravelResult atThreshold = SceneTravelPolicy.EvaluateScene(
                "D2-04",
                graph,
                new[] { "D2-01" },
                70);

            Assert.That(belowThreshold.IsAllowed, Is.True);
            Assert.That(atThreshold.IsAllowed, Is.False);
            Assert.That(atThreshold.DenialReason,
                Is.EqualTo(SceneAccessDenialReason.RestrictedByPublicAnxiety));
        }

        [Test]
        public void PassengerArea_RemainsAvailableAtHighAnxiety()
        {
            SceneTravelResult result = SceneTravelPolicy.EvaluateScene(
                "D1-02",
                graph,
                new[] { "D1-01" },
                100);

            Assert.That(result.IsAllowed, Is.True);
            Assert.That(result.Location.LocationCode, Is.EqualTo("DINING"));
        }

        [Test]
        public void Gangway_IsVisibleOnlyWhileP02IsPending()
        {
            Assert.That(
                SceneTravelPolicy.IsLocationVisibleOnMap(
                    "GANGWAY",
                    System.Array.Empty<string>()),
                Is.False);
            Assert.That(
                SceneTravelPolicy.IsLocationVisibleOnMap(
                    "GANGWAY",
                    new[] { "P-01" }),
                Is.True);
            Assert.That(
                SceneTravelPolicy.IsLocationVisibleOnMap(
                    "GANGWAY",
                    new[] { "P-01", "P-02" }),
                Is.False);
            Assert.That(
                SceneTravelPolicy.IsLocationVisibleOnMap(
                    "PORT",
                    new[] { "P-01", "P-02" }),
                Is.True);
        }

        [Test]
        public void Gangway_FreeTravelClosesAfterP02()
        {
            LocationDefinition gangway = graph.FindByCode("GANGWAY");

            SceneTravelResult duringBoarding =
                SceneTravelPolicy.EvaluateFreeTravel(
                    gangway,
                    new[] { "P-01" },
                    15);
            SceneTravelResult afterBoarding =
                SceneTravelPolicy.EvaluateFreeTravel(
                    gangway,
                    new[] { "P-01", "P-02", "P-03" },
                    15);

            Assert.That(duringBoarding.IsAllowed, Is.True);
            Assert.That(afterBoarding.IsAllowed, Is.False);
            Assert.That(
                afterBoarding.DenialReason,
                Is.EqualTo(SceneAccessDenialReason.NarrativeWindowClosed));
        }

        [Test]
        public void NarrativeAlias_ResolvesToRegisteredPhysicalLocation()
        {
            SceneTravelResult result = SceneTravelPolicy.EvaluateScene(
                "D1-04",
                graph,
                new[] { "D1-03" },
                15);

            Assert.That(result.IsAllowed, Is.True);
            Assert.That(result.DenialReason,
                Is.EqualTo(SceneAccessDenialReason.None));
            Assert.That(result.Scene.NarrativeLocationCode, Is.EqualTo("SERVICE7"));
            Assert.That(result.Location, Is.Not.Null);
            Assert.That(result.Location.LocationCode, Is.EqualTo("SERVICE7"));
            Assert.That(result.Location.BackgroundSprite, Is.Not.Null);
        }

        [Test]
        public void LocationLoader_UsesRegisteredSpriteAndFailsSafely()
        {
            GameObject loaderHost = new("LocationLoaderTest");
            LocationLoader loader = loaderHost.AddComponent<LocationLoader>();
            EnsureAwake(loader, LocationLoader.Instance);
            LocationDefinition empty = ScriptableObject.CreateInstance<LocationDefinition>();

            Assert.That(loader.TryLoadLocation(null, out LocationLoader.LoadFailure nullFailure),
                Is.False);
            Assert.That(nullFailure, Is.EqualTo(LocationLoader.LoadFailure.MissingLocation));
            Assert.That(loader.TryLoadLocation(empty, out LocationLoader.LoadFailure emptyFailure),
                Is.False);
            Assert.That(emptyFailure, Is.EqualTo(LocationLoader.LoadFailure.MissingVisualContent));
            LocationDefinition unused = graph.FindByCode("LAUNDRY");
            Assert.That(
                loader.TryLoadLocation(
                    unused,
                    out LocationLoader.LoadFailure unusedFailure),
                Is.False);
            Assert.That(
                unusedFailure,
                Is.EqualTo(LocationLoader.LoadFailure.UnusedLocation));
            Assert.That(loader.TryLoadLocation(graph.StartingLocation, out _), Is.True);
            Assert.That(loader.CurrentLocation.LocationCode, Is.EqualTo("PORT"));
            Assert.That(state.CurrentLocationCode, Is.EqualTo("PORT"));
            Assert.That(
                loader.ActiveBackgroundVariantKey,
                Is.EqualTo("serialized:bg_location_port_evidence"));
            Assert.That(
                loader.ActiveSemanticProfileId,
                Is.EqualTo("bg_location_port_evidence"));

            Object.DestroyImmediate(empty);
            Object.DestroyImmediate(loaderHost);
        }

        [Test]
        public void LocationLoader_RefreshesApprovedBackgroundForSceneState()
        {
            GameObject loaderHost =
                new("LocationLoaderBackgroundVariantTest");
            LocationLoader loader =
                loaderHost.AddComponent<LocationLoader>();
            EnsureAwake(loader, LocationLoader.Instance);
            LocationDefinition horizon = graph.FindByCode("HORIZON");

            loader.PrepareNarrativeScene("D1-06");
            Assert.That(
                loader.TryLoadLocation(horizon, out _),
                Is.True);
            Assert.That(
                loader.ActiveBackgroundSprite?.name,
                Is.EqualTo("bg_horizon_d1_discovery"));
            Assert.That(
                loader.ActiveBackgroundVariantKey,
                Is.EqualTo(
                    "LocationBackgroundVariants/" +
                    "bg_horizon_d1_discovery"));
            Assert.That(
                loader.ActiveSemanticProfileId,
                Is.EqualTo("bg_horizon_d1_discovery"));

            state.RecordCompletedScene("D1-06");
            loader.PrepareNarrativeScene("D2-01");
            Assert.That(
                loader.ActiveBackgroundSprite?.name,
                Is.EqualTo("bg_horizon_cleared_day"));
            Assert.That(
                loader.ActiveBackgroundVariantKey,
                Is.EqualTo(
                    "LocationBackgroundVariants/" +
                    "bg_horizon_cleared_day"));
            Assert.That(
                loader.ActiveSemanticProfileId,
                Is.EqualTo("bg_horizon_cleared_day"));

            Object.DestroyImmediate(loaderHost);
        }

        [Test]
        public void MapTravel_LoadsLocationDefersDialogueAndUsesScheduleTime()
        {
            state.RecordCompletedScene("D1-01");
            state.UnlockProductionScene("D1-02");
            GameObject loaderHost = new("LocationLoaderForMap");
            LocationLoader loader = loaderHost.AddComponent<LocationLoader>();
            EnsureAwake(loader, LocationLoader.Instance);
            RecordingScenePlayer player = new();
            int loadCount = 0;
            int sceneEnteredCount = 0;
            ProductionSceneTravelCoordinator coordinator = new(
                graph,
                state,
                player,
                location =>
                {
                    loadCount++;
                    return loader.TryLoadLocation(location, out _);
                });
            System.Action<InvestigationEvent> capture = investigationEvent =>
            {
                if (investigationEvent.Kind == InvestigationEventKind.SceneEntered &&
                    investigationEvent.SubjectId == "D1-02")
                {
                    sceneEnteredCount++;
                }
            };

            SceneTravelResult result;
            SceneTravelResult repeated;
            InvestigationEventHub.Published += capture;
            try
            {
                result = coordinator.TryEnter("D1-02");
                repeated = coordinator.TryEnter("D1-02");
            }
            finally
            {
                InvestigationEventHub.Published -= capture;
            }

            Assert.That(result.IsAllowed, Is.True);
            Assert.That(repeated.IsAllowed, Is.True);
            Assert.That(player.StartedSceneId, Is.Empty);
            Assert.That(player.StartCount, Is.Zero);
            Assert.That(loadCount, Is.EqualTo(1));
            Assert.That(sceneEnteredCount, Is.EqualTo(1));
            Assert.That(state.Day, Is.EqualTo(1));
            Assert.That(state.CurrentTimeBlock, Is.EqualTo(TimeBlock.NIGHT));
            Assert.That(state.CurrentLocationCode, Is.EqualTo("DINING"));

            Object.DestroyImmediate(loaderHost);
        }

        [Test]
        public void RestrictedCatalog_ContainsOnlyPhysicalLocations()
        {
            Assert.That(SceneTravelPolicy.RestrictedLocations, Has.Count.EqualTo(10));
            Assert.That(SceneTravelPolicy.RestrictedLocations.All(code =>
                CanonicalLocationCatalog.IsPlayable(code)), Is.True);
            Assert.That(SceneTravelPolicy.RestrictedLocations, Does.Not.Contain("DINING"));
            Assert.That(SceneTravelPolicy.RestrictedLocations, Does.Not.Contain("HORIZON"));
        }

        [Test]
        public void UnusedLocations_AreDeniedBeforeOtherTravelRules()
        {
            foreach (CanonicalLocationSpec unused in
                     CanonicalLocationCatalog.Unused)
            {
                LocationDefinition location =
                    graph.FindByCode(unused.Code);
                SceneTravelResult result =
                    SceneTravelPolicy.EvaluateMapTravel(
                        location,
                        ProductionSceneCatalog.All.Select(
                            scene => scene.SceneId),
                        ProductionSceneCatalog.All.Select(
                            scene => scene.SceneId),
                        0);

                Assert.That(result.IsAllowed, Is.False, unused.Code);
                Assert.That(
                    result.DenialReason,
                    Is.EqualTo(SceneAccessDenialReason.LocationUnused),
                    unused.Code);
            }
        }

        private static void DestroyExisting<T>() where T : Component
        {
            foreach (T component in Object.FindObjectsByType<T>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(component.gameObject);
            }
        }

        private static void EnsureAwake<T>(T component, T instance) where T : Component
        {
            if (instance == component)
            {
                return;
            }

            component.GetType()
                .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(component, null);
        }

        private sealed class RecordingScenePlayer :
            IProductionScenePlayer,
            IProductionSceneLaunchAvailability
        {
            public string ActiveProductionSceneId { get; private set; } =
                string.Empty;
            public string StartedSceneId { get; private set; } = string.Empty;
            public int StartCount { get; private set; }

            public bool CanStartProductionScene(string sceneId) =>
                string.IsNullOrEmpty(ActiveProductionSceneId);

            public bool StartProductionScene(string sceneId)
            {
                StartCount++;
                StartedSceneId = sceneId;
                ActiveProductionSceneId = sceneId;
                return true;
            }

            public bool RestoreProductionScene(
                ProductionDialogueCheckpoint checkpoint) => false;
        }
    }
}
