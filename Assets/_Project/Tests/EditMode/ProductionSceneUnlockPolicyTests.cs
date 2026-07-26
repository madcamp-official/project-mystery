using NUnit.Framework;
using UnityEngine;
using Wake.Core;
using Wake.Narrative;

namespace Wake.Tests
{
    public sealed class ProductionSceneUnlockPolicyTests
    {
        private const string SaveKey = "UNDER_THE_HORIZON_GAME_STATE_V2";
        private GameObject host;
        private GameStateManager state;

        [SetUp]
        public void SetUp()
        {
            DestroyManager();
            PlayerPrefs.DeleteKey(SaveKey);
            host = new GameObject("ProductionSceneUnlockPolicyTests");
            state = host.AddComponent<GameStateManager>();
            state.StartNewGame();
        }

        [TearDown]
        public void TearDown()
        {
            DestroyManager();
            PlayerPrefs.DeleteKey(SaveKey);
        }

        [Test]
        public void OpeningScene_IsAvailableWithoutExplicitUnlock()
        {
            ProductionSceneUnlockResult result =
                ProductionSceneUnlockPolicy.Evaluate("P-01", state);

            Assert.That(result.IsAllowed, Is.True);
            Assert.That(result.Denial, Is.EqualTo(ProductionSceneUnlockDenial.None));
        }

        [Test]
        public void KnownScene_RequiresExplicitDialogueUnlock()
        {
            state.RecordCompletedScene("P-01");

            ProductionSceneUnlockResult locked =
                ProductionSceneUnlockPolicy.Evaluate("P-02", state);
            state.UnlockProductionScene("P-02");
            ProductionSceneUnlockResult unlocked =
                ProductionSceneUnlockPolicy.Evaluate("P-02", state);

            Assert.That(locked.IsAllowed, Is.False);
            Assert.That(locked.Denial,
                Is.EqualTo(ProductionSceneUnlockDenial.SceneLocked));
            Assert.That(unlocked.IsAllowed, Is.True);
        }

        [Test]
        public void UnlockDoesNotBypassIncompletePrerequisite()
        {
            state.UnlockProductionScene("D2-04");

            ProductionSceneUnlockResult result =
                ProductionSceneUnlockPolicy.Evaluate("D2-04", state);

            Assert.That(result.IsAllowed, Is.False);
            Assert.That(result.Denial,
                Is.EqualTo(ProductionSceneUnlockDenial.PrerequisiteIncomplete));
            Assert.That(result.Detail, Does.Contain("D2-01"));
        }

        [Test]
        public void BranchScenes_CanBeAvailableAtTheSameTime()
        {
            state.RecordCompletedScene("D1-03");
            state.UnlockProductionScene("D1-04");
            state.UnlockProductionScene("D1-05");

            Assert.That(
                ProductionSceneUnlockPolicy.Evaluate("D1-04", state).IsAllowed,
                Is.True);
            Assert.That(
                ProductionSceneUnlockPolicy.Evaluate("D1-05", state).IsAllowed,
                Is.True);
            Assert.That(
                ProductionSceneUnlockPolicy.GetAvailableSceneIds(state),
                Does.Contain("D1-04").And.Contain("D1-05"));
        }

        [Test]
        public void NextScene_UsesUnlockedScheduleOrder()
        {
            state.RecordCompletedScene("P-01");
            state.UnlockProductionScene("P-02");
            state.UnlockProductionScene("D1-04");

            Assert.That(
                ProductionSceneUnlockPolicy.FindNextAvailableScene(state),
                Is.EqualTo("P-02"));
        }

        [Test]
        public void CompletedScene_RemainsAddressableForRestore()
        {
            state.RecordCompletedScene("P-01");

            ProductionSceneUnlockResult result =
                ProductionSceneUnlockPolicy.Evaluate("P-01", state);

            Assert.That(result.IsAllowed, Is.True);
        }

        [Test]
        public void FinalConfession_RequiresValidEndingAfterUnlock()
        {
            state.RecordCompletedScene("D8-01");
            state.UnlockProductionScene("D8-02");
            ProductionSceneUnlockResult blocked =
                ProductionSceneUnlockPolicy.Evaluate("D8-02", state);

            state.TryRecordFinalEnding(FinalAccusationResolver.CompleteEndingId);
            ProductionSceneUnlockResult allowed =
                ProductionSceneUnlockPolicy.Evaluate("D8-02", state);

            Assert.That(blocked.IsAllowed, Is.False);
            Assert.That(blocked.Denial,
                Is.EqualTo(ProductionSceneUnlockDenial.FinalAccusationIncomplete));
            Assert.That(allowed.IsAllowed, Is.True);
        }

        [Test]
        public void Director_DoesNotLaunchLockedSceneFromResumeFallback()
        {
            state.RecordCompletedScene("P-01");
            var player = new RecordingPlayer();
            var director = new ProductionSceneDirector(state, player);

            Assert.That(director.FindNextAvailableScene(), Is.Empty);
            Assert.That(director.ResumeGame(), Is.False);
            Assert.That(player.StartedSceneId, Is.Empty);

            state.UnlockProductionScene("P-02");
            Assert.That(director.ResumeGame(), Is.True);
            Assert.That(player.StartedSceneId, Is.EqualTo("P-02"));
        }

        private void DestroyManager()
        {
            if (host != null)
            {
                Object.DestroyImmediate(host);
            }
            if (GameStateManager.Instance != null)
            {
                Object.DestroyImmediate(GameStateManager.Instance.gameObject);
            }
            host = null;
            state = null;
        }

        private sealed class RecordingPlayer : IProductionScenePlayer
        {
            public string ActiveProductionSceneId => string.Empty;
            public string StartedSceneId { get; private set; } = string.Empty;

            public bool StartProductionScene(string sceneId)
            {
                StartedSceneId = sceneId;
                return true;
            }

            public bool RestoreProductionScene(
                ProductionDialogueCheckpoint checkpoint) => false;
        }
    }
}
