using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wake.Core;
using Wake.Narrative;

namespace Wake.Tests
{
    public class ProductionObjectiveCatalogTests
    {
        private const string SaveKey = "UNDER_THE_HORIZON_GAME_STATE_V2";
        private GameObject host;
        private GameStateManager state;

        [SetUp]
        public void SetUp()
        {
            DestroyManager();
            PlayerPrefs.DeleteKey(SaveKey);
            host = new GameObject(nameof(ProductionObjectiveCatalogTests));
            state = host.AddComponent<GameStateManager>();
            state.StartNewGame();
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null)
            {
                Object.DestroyImmediate(host);
            }

            DestroyManager();
            PlayerPrefs.DeleteKey(SaveKey);
        }

        [Test]
        public void Catalog_HasSourceBackedTextForEveryScheduledScene()
        {
            Assert.That(ProductionObjectiveCatalog.All.Count, Is.EqualTo(41));
            Assert.That(
                ProductionObjectiveCatalog.All.Select(item => item.SceneId),
                Is.EqualTo(ProductionSceneCatalog.All.Select(item => item.SceneId)));
            Assert.That(
                ProductionObjectiveCatalog.All.All(item =>
                    !string.IsNullOrWhiteSpace(item.Title) &&
                    !string.IsNullOrWhiteSpace(item.Description)),
                Is.True);
            Assert.That(
                ProductionObjectiveCatalog.All.Single(item => item.SceneId == "D8-01").Title,
                Is.EqualTo("흔적 없는 밀실의 해답"));
        }

        [Test]
        public void FreshState_SelectsOpeningAsCurrent()
        {
            ProductionObjectiveViewModel view =
                ProductionObjectiveViewModel.Resolve(state);

            Assert.That(view.Current?.Definition.SceneId, Is.EqualTo("P-01"));
            Assert.That(view.Next, Is.Null);
            Assert.That(view.CompletedCount, Is.Zero);
            Assert.That(view.Items.Count(item =>
                item.Status == ProductionObjectiveStatus.Locked), Is.EqualTo(40));
        }

        [Test]
        public void BranchFrontier_UsesCheckpointAsCurrentAndOtherBranchAsNext()
        {
            CompleteThrough("D1-03");
            ProductionObjectiveViewModel frontier =
                ProductionObjectiveViewModel.Resolve(state);
            Assert.That(frontier.Current?.Definition.SceneId, Is.EqualTo("D1-04"));
            Assert.That(frontier.Next?.Definition.SceneId, Is.EqualTo("D1-05"));

            state.SaveDialogueCheckpoint("D1-05", 0, false, string.Empty);
            ProductionObjectiveViewModel checkpoint =
                ProductionObjectiveViewModel.Resolve(state);
            Assert.That(checkpoint.Current?.Definition.SceneId, Is.EqualTo("D1-05"));
            Assert.That(checkpoint.Next?.Definition.SceneId, Is.EqualTo("D1-04"));
        }

        [Test]
        public void PendingInteraction_SurvivesLocationChangeAndSaveReload()
        {
            CompleteThrough("D1-07");
            state.SaveDialogueCheckpoint("D2-01", 5, false, "exit_inspection");
            state.RecordLocation("PORT");
            state.ReloadSavedState();

            ProductionObjectiveViewModel view =
                ProductionObjectiveViewModel.Resolve(state);
            Assert.That(
                view.Current?.Status,
                Is.EqualTo(ProductionObjectiveStatus.InteractionPending));
            Assert.That(view.Current?.Definition.SceneId, Is.EqualTo("D2-01"));
            Assert.That(view.Next, Is.Null);

            state.SaveDialogueCheckpoint("D2-01", 5, false, "unknown");
            Assert.That(
                ProductionObjectiveViewModel.Resolve(state).Current?.Status,
                Is.EqualTo(ProductionObjectiveStatus.Current));
        }

        [Test]
        public void TypedFinalPrerequisite_RemainsLockedUntilSupportedEnding()
        {
            CompleteThrough("D8-01");
            ProductionObjectiveViewModel locked =
                ProductionObjectiveViewModel.Resolve(state);
            Assert.That(locked.Current, Is.Null);
            Assert.That(
                locked.Items.Single(item => item.Definition.SceneId == "D8-02").Status,
                Is.EqualTo(ProductionObjectiveStatus.Locked));

            state.TryRecordFinalEnding(FinalAccusationResolver.CompleteEndingId);
            ProductionObjectiveViewModel unlocked =
                ProductionObjectiveViewModel.Resolve(state);
            Assert.That(unlocked.Current?.Definition.SceneId, Is.EqualTo("D8-02"));
        }

        private void CompleteThrough(string sceneId)
        {
            foreach (ProductionSceneDefinition scene in ProductionSceneCatalog.All)
            {
                state.RecordCompletedScene(scene.SceneId);
                if (scene.SceneId == sceneId)
                {
                    return;
                }
            }
        }

        private static void DestroyManager()
        {
            if (GameStateManager.Instance != null)
            {
                Object.DestroyImmediate(GameStateManager.Instance.gameObject);
            }
        }
    }
}
