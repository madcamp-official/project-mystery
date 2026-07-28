using NUnit.Framework;
using UnityEngine;
using Wake.Core;
using Wake.Narrative;

namespace Wake.Tests
{
    public class ProductionSceneDirectorTests
    {
        private const string SaveKey = "UNDER_THE_HORIZON_GAME_STATE_V2";
        private GameObject host;
        private GameStateManager state;

        [SetUp]
        public void SetUp()
        {
            DestroyState();
            PlayerPrefs.DeleteKey(SaveKey);
            host = new GameObject("ProductionSceneDirectorTests");
            state = host.AddComponent<GameStateManager>();
        }

        [TearDown]
        public void TearDown()
        {
            DestroyState();
            PlayerPrefs.DeleteKey(SaveKey);
        }

        [Test]
        public void StartNewGame_ClearsCheckpointAndEntersOpeningScene()
        {
            state.SaveDialogueCheckpoint("D2-02", 4, false, "blood_pattern");
            var player = new FakePlayer();
            var director = new ProductionSceneDirector(state, player);
            ProductionSceneEntry entered = default;
            director.SceneEntered += value => entered = value;

            Assert.That(director.StartNewGame(), Is.True);

            Assert.That(player.StartedSceneId, Is.EqualTo("P-01"));
            Assert.That(state.DialogueCheckpoint, Is.Null);
            Assert.That(entered.SceneId, Is.EqualTo("P-01"));
            Assert.That(entered.Restored, Is.False);
        }

        [Test]
        public void ResumeGame_RestoresSavedCheckpoint()
        {
            state.SaveDialogueCheckpoint("P-02", 3, true, string.Empty);
            var player = new FakePlayer { RestoreResult = true };
            var director = new ProductionSceneDirector(state, player);
            ProductionSceneEntry entered = default;
            director.SceneEntered += value => entered = value;

            Assert.That(director.ResumeGame(), Is.True);

            Assert.That(player.Restored.activeSceneId, Is.EqualTo("P-02"));
            Assert.That(player.Restored.lineIndex, Is.EqualTo(3));
            Assert.That(entered.Restored, Is.True);
        }

        [Test]
        public void ResumeGame_InvalidCheckpointFallsBackToFirstAvailableScene()
        {
            state.SaveDialogueCheckpoint("MISSING", 99, false, string.Empty);
            var player = new FakePlayer { RestoreResult = false };
            var director = new ProductionSceneDirector(state, player);

            Assert.That(director.ResumeGame(), Is.True);

            Assert.That(state.DialogueCheckpoint, Is.Null);
            Assert.That(player.StartedSceneId, Is.EqualTo("P-01"));
        }

        [Test]
        public void ResumeGame_AfterSceneCompletionWithoutTravelDoesNotAutoStartNextScene()
        {
            // Player finished the previous scene's dialogue and is free-roaming
            // the room without having manually walked into the next scene yet.
            state.SaveDialogueCheckpoint("P-01", 5, false, string.Empty);
            Assert.That(
                state.ClearDialogueCheckpoint("P-01", string.Empty),
                Is.True);
            Assert.That(state.IsAwaitingSceneTransition, Is.True);

            var player = new FakePlayer();
            var director = new ProductionSceneDirector(state, player);

            Assert.That(director.ResumeGame(), Is.True);

            Assert.That(player.StartedSceneId, Is.Empty);
            Assert.That(state.DialogueCheckpoint, Is.Null);
        }

        private void DestroyState()
        {
            if (GameStateManager.Instance != null)
            {
                Object.DestroyImmediate(GameStateManager.Instance.gameObject);
            }
            host = null;
            state = null;
        }

        private sealed class FakePlayer : IProductionScenePlayer
        {
            public string ActiveProductionSceneId { get; set; } = string.Empty;
            public string StartedSceneId { get; private set; } = string.Empty;
            public ProductionDialogueCheckpoint Restored { get; private set; }
            public bool RestoreResult { get; set; }

            public bool StartProductionScene(string sceneId)
            {
                StartedSceneId = sceneId;
                return true;
            }

            public bool RestoreProductionScene(ProductionDialogueCheckpoint checkpoint)
            {
                Restored = checkpoint;
                return RestoreResult;
            }
        }
    }
}
