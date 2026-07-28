using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.Core;
using Wake.Exploration;

namespace Wake.Tests
{
    public sealed class GameResumeLocationPolicyTests
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
            DestroyExistingState();
            ClearSaves();

            stateHost = new GameObject("GameResumeLocationPolicyTests");
            state = stateHost.AddComponent<GameStateManager>();
            state.StartNewGame();
            graph = AssetDatabase.LoadAssetAtPath<LocationGraph>(GraphPath);

            Assert.That(graph, Is.Not.Null);
            Assert.That(graph.StartingLocation, Is.Not.Null);
        }

        [TearDown]
        public void TearDown()
        {
            if (stateHost != null)
            {
                Object.DestroyImmediate(stateHost);
            }

            DestroyExistingState();
            ClearSaves();
        }

        [Test]
        public void ActiveCheckpointScene_TakesPriorityOverSavedFreeTravelLocation()
        {
            state.RecordLocation("LAUNDRY");
            Assert.That(
                state.SaveDialogueCheckpoint("D2-01", 4, false, string.Empty),
                Is.True);

            string storySceneId =
                GameResumeLocationPolicy.ResolveStorySceneId(
                    state,
                    "D2-02");
            LocationDefinition location =
                GameResumeLocationPolicy.ResolveLocation(
                    graph,
                    state,
                    storySceneId);

            Assert.That(storySceneId, Is.EqualTo("D2-01"));
            Assert.That(location, Is.Not.Null);
            Assert.That(location.LocationCode, Is.EqualTo("HORIZON"));
            Assert.That(state.CurrentLocationCode, Is.EqualTo("LAUNDRY"));
        }

        [Test]
        public void CheckpointNarrativeAlias_ResolvesCanonicalPhysicalLocation()
        {
            state.RecordLocation("PORT");
            Assert.That(
                state.SaveDialogueCheckpoint("d1-04", 8, true, string.Empty),
                Is.True);

            string storySceneId =
                GameResumeLocationPolicy.ResolveStorySceneId(
                    state,
                    "D1-05");
            LocationDefinition location =
                GameResumeLocationPolicy.ResolveLocation(
                    graph,
                    state,
                    storySceneId);

            Assert.That(storySceneId, Is.EqualTo("D1-04"));
            Assert.That(location.LocationCode, Is.EqualTo("CREW_STAIRS"));
            Assert.That(location.MatchesCode("SERVICE7"), Is.True);
        }

        [Test]
        public void MissingCheckpoint_UsesNextAvailableStorySceneLocation()
        {
            state.RecordLocation("PORT");

            string storySceneId =
                GameResumeLocationPolicy.ResolveStorySceneId(
                    state,
                    "P-02");
            LocationDefinition location =
                GameResumeLocationPolicy.ResolveLocation(
                    graph,
                    state,
                    storySceneId);

            Assert.That(storySceneId, Is.EqualTo("P-02"));
            Assert.That(location.LocationCode, Is.EqualTo("GANGWAY"));
        }

        [Test]
        public void InvalidCheckpoint_UsesRegisteredNextAvailableScene()
        {
            state.RecordLocation("PORT");
            Assert.That(
                state.SaveDialogueCheckpoint("UNKNOWN", 1, false, string.Empty),
                Is.True);

            string storySceneId =
                GameResumeLocationPolicy.ResolveStorySceneId(
                    state,
                    "P-03");
            LocationDefinition location =
                GameResumeLocationPolicy.ResolveLocation(
                    graph,
                    state,
                    storySceneId);

            Assert.That(storySceneId, Is.EqualTo("P-03"));
            Assert.That(location.LocationCode, Is.EqualTo("RICHARD_SUITE"));
        }

        [Test]
        public void AwaitingSceneTransition_KeepsSavedFreeRoamLocationInsteadOfNextScene()
        {
            // Player finished P-01's dialogue and is free-roaming PORT without
            // having manually walked into P-02 yet.
            Assert.That(
                state.SaveDialogueCheckpoint("P-01", 5, false, string.Empty),
                Is.True);
            Assert.That(
                state.ClearDialogueCheckpoint("P-01", string.Empty),
                Is.True);
            state.RecordLocation("PORT");

            string storySceneId =
                GameResumeLocationPolicy.ResolveStorySceneId(
                    state,
                    "P-02");
            LocationDefinition location =
                GameResumeLocationPolicy.ResolveLocation(
                    graph,
                    state,
                    storySceneId);

            Assert.That(storySceneId, Is.Empty);
            Assert.That(location.LocationCode, Is.EqualTo("PORT"));
        }

        [Test]
        public void EndingOnlySave_KeepsLastValidSavedLocation()
        {
            state.RecordLocation("PROMENADE");

            string storySceneId =
                GameResumeLocationPolicy.ResolveStorySceneId(
                    state,
                    string.Empty);
            LocationDefinition location =
                GameResumeLocationPolicy.ResolveLocation(
                    graph,
                    state,
                    storySceneId);

            Assert.That(storySceneId, Is.Empty);
            Assert.That(location.LocationCode, Is.EqualTo("PROMENADE"));
        }

        [Test]
        public void UnknownSavedLocation_FallsBackToGraphStartingLocation()
        {
            state.RecordLocation("UNKNOWN");

            LocationDefinition location =
                GameResumeLocationPolicy.ResolveLocation(
                    graph,
                    state,
                    string.Empty);

            Assert.That(location, Is.SameAs(graph.StartingLocation));
            Assert.That(location.LocationCode, Is.EqualTo("PORT"));
        }

        [Test]
        public void NullGraph_ReturnsNoLocationWithoutThrowing()
        {
            Assert.That(
                GameResumeLocationPolicy.ResolveLocation(
                    null,
                    state,
                    "D2-01"),
                Is.Null);
        }

        [Test]
        public void SceneIds_AreTrimmedAndNormalized()
        {
            Assert.That(
                GameResumeLocationPolicy.ResolveStorySceneId(
                    state,
                    "  d7-03  "),
                Is.EqualTo("D7-03"));
        }

        private static void DestroyExistingState()
        {
            foreach (GameStateManager manager in
                     Object.FindObjectsByType<GameStateManager>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(manager.gameObject);
            }
        }

        private static void ClearSaves()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.DeleteKey(SaveKey + "_BACKUP");
            PlayerPrefs.DeleteKey(SaveKey + "_PENDING");
            PlayerPrefs.Save();
        }
    }
}
