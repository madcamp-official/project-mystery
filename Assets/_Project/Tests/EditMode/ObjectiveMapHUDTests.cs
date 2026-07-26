using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wake.Core;
using Wake.Exploration;
using Wake.UI;

namespace Wake.Tests
{
    public class ObjectiveMapHUDTests
    {
        private const string SaveKey = "UNDER_THE_HORIZON_GAME_STATE_V2";
        private GameObject host;
        private GameStateManager state;
        private InvestigationObjectiveTracker tracker;

        [SetUp]
        public void SetUp()
        {
            if (GameStateManager.Instance != null)
            {
                Object.DestroyImmediate(GameStateManager.Instance.gameObject);
            }

            PlayerPrefs.DeleteKey(SaveKey);
            host = new GameObject("ObjectiveMapHUDTests");
            state = host.AddComponent<GameStateManager>();
            state.StartNewGame();
            tracker = new InvestigationObjectiveTracker(state);
        }

        [TearDown]
        public void TearDown()
        {
            tracker?.Dispose();
            if (host != null)
            {
                Object.DestroyImmediate(host);
            }

            PlayerPrefs.DeleteKey(SaveKey);
        }

        [Test]
        public void ViewModel_SelectsFirstIncompleteObjective()
        {
            ObjectiveHudViewModel view =
                ObjectiveHudViewModel.Create(tracker.Progress);

            Assert.That(view.TotalCount, Is.EqualTo(3));
            Assert.That(view.CompletedCount, Is.Zero);
            Assert.That(view.Current.HasValue, Is.True);
            Assert.That(
                view.Current.Value.Id,
                Is.EqualTo("objective_p_01_arrival"));
            Assert.That(view.Summary, Is.EqualTo("전체 목표 0/3"));
        }

        [Test]
        public void ViewModel_AdvancesAfterObjectiveCompletion()
        {
            InvestigationEventHub.Publish(
                InvestigationEventKind.SceneEntered,
                "P-01");

            ObjectiveHudViewModel view =
                ObjectiveHudViewModel.Create(tracker.Progress);

            Assert.That(view.CompletedCount, Is.EqualTo(1));
            Assert.That(
                view.Current.Value.Id,
                Is.EqualTo("objective_d2_01_exit_inspection"));
            Assert.That(view.Summary, Is.EqualTo("전체 목표 1/3"));
        }

        [Test]
        public void ViewModel_ShowsPartialEvidenceProgress()
        {
            InvestigationEventHub.Publish(
                InvestigationEventKind.SceneEntered,
                "P-01");
            InvestigationEventHub.Publish(
                InvestigationEventKind.EvidenceCollected,
                "C-03");
            InvestigationEventHub.Publish(
                InvestigationEventKind.EvidenceCollected,
                "C-05");

            ObjectiveHudViewModel view =
                ObjectiveHudViewModel.Create(tracker.Progress);
            ObjectiveHudItem horizon = view.Items.Single(item =>
                item.Id == "objective_d2_01_exit_inspection");

            Assert.That(horizon.ProgressLabel, Is.EqualTo("2/3"));
            Assert.That(horizon.StateIcon, Is.EqualTo("●"));
            Assert.That(horizon.AccessibilityLabel, Does.Contain("진행 중"));
            Assert.That(horizon.AccessibilityLabel, Does.Contain("2/3"));
        }

        [Test]
        public void CompletedItem_UsesTextAndIconBeyondColor()
        {
            InvestigationEventHub.Publish(
                InvestigationEventKind.SceneEntered,
                "P-01");

            ObjectiveHudItem completed =
                ObjectiveHudViewModel.Create(tracker.Progress)
                .Items.Single(item => item.Id == "objective_p_01_arrival");

            Assert.That(completed.IsCompleted, Is.True);
            Assert.That(completed.StateIcon, Is.EqualTo("✓"));
            Assert.That(completed.ProgressLabel, Is.EqualTo("완료"));
            Assert.That(completed.AccessibilityLabel, Does.StartWith("완료:"));
        }

        [Test]
        public void AnxietyRestriction_ProducesVisibleKoreanReason()
        {
            SceneTravelResult denied = SceneTravelResult.Denied(
                SceneAccessDenialReason.RestrictedByPublicAnxiety,
                "internal SERVICE_RAIL diagnostic");

            InvestigationFeedback feedback =
                InvestigationFeedbackCatalog.ForTravel(denied);

            Assert.That(feedback.Title, Is.EqualTo("제한구역 폐쇄"));
            Assert.That(feedback.Message, Does.Contain("70"));
            Assert.That(feedback.Message, Does.Contain("폐쇄"));
            Assert.That(feedback.Message, Does.Not.Contain("SERVICE_RAIL"));
        }

    }
}
