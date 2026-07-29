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
                    !string.IsNullOrWhiteSpace(item.ObjectiveId) &&
                    !string.IsNullOrWhiteSpace(item.DisplayText) &&
                    !string.IsNullOrWhiteSpace(item.DetailText) &&
                    item.Priority == ObjectivePriority.Main &&
                    item.Steps.Count > 0),
                Is.True);
            Assert.That(
                ProductionObjectiveCatalog.All.Single(item => item.SceneId == "D8-01").Title,
                Is.EqualTo("최종 논증 완성하기"));
        }

        [Test]
        public void Catalog_ExposesOnlyNaturalLanguageInPlayerFacingFields()
        {
            foreach (ProductionObjectiveDefinition objective in
                     ProductionObjectiveCatalog.All)
            {
                string visible =
                    $"{objective.DisplayText} {objective.DetailText} " +
                    string.Join(" ", objective.Steps);
                Assert.That(visible, Does.Not.Contain(objective.SceneId));
                Assert.That(visible, Does.Not.Match(@"\bC-\d+\b"));
                Assert.That(visible, Does.Not.Contain("trust_"));
                Assert.That(
                    visible,
                    Does.Not.Match("[A-Za-z]"),
                    $"{objective.SceneId} 목표에 영문 표기가 남아 있습니다.");
                Assert.That(objective.DisplayText, Does.EndWith("기"));
            }
        }

        [TestCase("P-01", "DANIEL", true)]
        [TestCase("P-01", "RICHARD", false)]
        [TestCase("D1-01", "OWEN", true)]
        [TestCase("D3-03", "THOMAS", true)]
        public void NpcTargets_ContainOnlyAuthoredObjectiveCharacters(
            string sceneId,
            string characterId,
            bool expected)
        {
            Assert.That(
                ProductionObjectiveNpcTargets.Contains(
                    sceneId,
                    characterId),
                Is.EqualTo(expected));
        }

        [Test]
        public void Presentation_UsesTravelGoalUntilDestinationIsReached()
        {
            state.RecordLocation("PORT");
            state.RecordCompletedScene("P-01");

            ProductionObjectivePresentation travel =
                ProductionObjectiveViewModel.Resolve(state).Presentation.Value;
            Assert.That(travel.IsTravel, Is.True);
            Assert.That(travel.ActionType, Is.EqualTo(ObjectiveActionType.Move));
            Assert.That(travel.DisplayText, Is.EqualTo("승선 통로로 향하기"));
            Assert.That(travel.MarkerMode, Is.EqualTo(ObjectiveMarkerMode.Map));

            state.RecordLocation("GANGWAY");
            ProductionObjectivePresentation arrived =
                ProductionObjectiveViewModel.Resolve(state).Presentation.Value;
            Assert.That(arrived.IsTravel, Is.False);
            Assert.That(arrived.DisplayText, Is.EqualTo("승선 명단의 오류 확인하기"));
            Assert.That(arrived.MarkerMode, Is.EqualTo(ObjectiveMarkerMode.Hover));
        }

        [Test]
        public void Presentation_UsesCorrectKoreanDirectionalParticle()
        {
            CompleteThrough("P-02");
            state.RecordLocation("PORT");

            ProductionObjectivePresentation presentation =
                ProductionObjectiveViewModel.Resolve(state).Presentation.Value;

            Assert.That(
                presentation.DisplayText,
                Is.EqualTo("리처드 스위트룸으로 향하기"));
        }

        [Test]
        public void CompletedStory_RemovesActiveObjective()
        {
            CompleteThrough("D8-03");

            Assert.That(
                ProductionObjectiveViewModel.Resolve(state).Presentation,
                Is.Null);
        }

        [Test]
        public void EpilogueObjective_FollowsSupportedEndingRoute()
        {
            CompleteThrough("D8-01");
            state.TryRecordFinalEnding(FinalAccusationResolver.CompleteEndingId);
            state.RecordCompletedScene("D8-02");
            state.RecordLocation("OPEN_DECK");

            ProductionObjectivePresentation presentation =
                ProductionObjectiveViewModel.Resolve(state).Presentation.Value;

            Assert.That(presentation.Definition.SceneId, Is.EqualTo("D8-03"));
            Assert.That(presentation.DisplayText, Is.EqualTo("항구로 향하기"));
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
