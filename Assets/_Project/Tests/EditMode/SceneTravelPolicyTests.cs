using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.Core;
using Wake.Exploration;
using Wake.UI;

namespace Wake.Tests
{
    public class SceneTravelPolicyTests
    {
        private const string SaveKey = "THE_WAKE_GAME_STATE_V1";
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
        public void UnresolvedNarrativeLocation_ReturnsTypedReason()
        {
            SceneTravelResult result = SceneTravelPolicy.EvaluateScene(
                "D1-04",
                graph,
                new[] { "D1-03" },
                15);

            Assert.That(result.IsAllowed, Is.False);
            Assert.That(result.DenialReason,
                Is.EqualTo(SceneAccessDenialReason.PhysicalLocationUnresolved));
            Assert.That(result.Scene.NarrativeLocationCode, Is.EqualTo("SERVICE7"));
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
            Assert.That(loader.TryLoadLocation(graph.StartingLocation, out _), Is.True);
            Assert.That(loader.CurrentLocation.LocationCode, Is.EqualTo("PORT"));
            Assert.That(state.CurrentLocationCode, Is.EqualTo("PORT"));

            Object.DestroyImmediate(empty);
            Object.DestroyImmediate(loaderHost);
        }

        [Test]
        public void MapTravel_UsesScheduleTimeOnlyAfterSuccessfulLoad()
        {
            state.RecordCompletedScene("D1-01");
            GameObject loaderHost = new("LocationLoaderForMap");
            LocationLoader loader = loaderHost.AddComponent<LocationLoader>();
            EnsureAwake(loader, LocationLoader.Instance);
            GameObject mapHost = new("MapControllerTest");
            MapController map = mapHost.AddComponent<MapController>();
            SerializedObject mapData = new(map);
            mapData.FindProperty("locationGraph").objectReferenceValue = graph;
            mapData.ApplyModifiedPropertiesWithoutUndo();

            SceneTravelResult result = map.TryTravelToScene("D1-02");

            Assert.That(result.IsAllowed, Is.True);
            Assert.That(state.Day, Is.EqualTo(1));
            Assert.That(state.CurrentTimeBlock, Is.EqualTo(TimeBlock.NIGHT));
            Assert.That(state.CurrentLocationCode, Is.EqualTo("DINING"));

            Object.DestroyImmediate(mapHost);
            Object.DestroyImmediate(loaderHost);
        }

        [Test]
        public void RestrictedCatalog_ContainsOnlyPhysicalLocations()
        {
            Assert.That(SceneTravelPolicy.RestrictedLocations, Has.Count.EqualTo(14));
            Assert.That(SceneTravelPolicy.RestrictedLocations.All(code =>
                CanonicalLocationCatalog.FindSpec(code) != null), Is.True);
            Assert.That(SceneTravelPolicy.RestrictedLocations, Does.Not.Contain("DINING"));
            Assert.That(SceneTravelPolicy.RestrictedLocations, Does.Not.Contain("HORIZON"));
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
    }
}
