using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wake.Core;

namespace Wake.Tests
{
    public class InvestigationObjectiveTests
    {
        private const string SaveKey = "UNDER_THE_HORIZON_GAME_STATE_V2";

        private GameObject host;
        private GameStateManager state;
        private InvestigationObjectiveTracker tracker;

        [SetUp]
        public void SetUp()
        {
            DestroyExistingManager();
            PlayerPrefs.DeleteKey(SaveKey);
            state = CreateManager();
            state.StartNewGame();
            tracker = new InvestigationObjectiveTracker(state);
        }

        [TearDown]
        public void TearDown()
        {
            tracker?.Dispose();
            if (host != null)
            {
                UnityEngine.Object.DestroyImmediate(host);
            }

            DestroyExistingManager();
            PlayerPrefs.DeleteKey(SaveKey);
        }

        [Test]
        public void Catalog_DefinesOnlySourceBackedInitialObjectives()
        {
            Assert.That(InvestigationObjectiveCatalog.All.Count, Is.EqualTo(3));
            Assert.That(
                InvestigationObjectiveCatalog.All.Select(item => item.SceneId),
                Is.EqualTo(new[] { "P-01", "D2-01", "D6-03" }));
            Assert.That(
                InvestigationObjectiveCatalog.All.Select(item => item.Id),
                Is.Unique);
        }

        [Test]
        public void PortObjective_CompletesOnSceneEntryAndPersistsOnce()
        {
            int completedCount = 0;
            tracker.ObjectiveCompleted += _ => completedCount++;

            InvestigationEventHub.Publish(
                InvestigationEventKind.SceneEntered,
                " p-01 ",
                "port");
            InvestigationEventHub.Publish(
                InvestigationEventKind.SceneEntered,
                "P-01",
                "PORT");

            ObjectiveProgress progress =
                tracker.GetProgress("objective_p_01_arrival");
            Assert.That(progress.IsCompleted, Is.True);
            Assert.That(progress.CompletedRequirementCount, Is.EqualTo(1));
            Assert.That(completedCount, Is.EqualTo(1));
            Assert.That(state.HasCompletedObjective(progress.Definition.Id), Is.True);
            Assert.That(state.CompletedObjectiveIds, Has.Count.EqualTo(1));
        }

        [Test]
        public void HorizonObjective_TracksThreeDistinctEvidenceEvents()
        {
            InvestigationEventHub.Publish(
                InvestigationEventKind.EvidenceCollected,
                "C-03");
            InvestigationEventHub.Publish(
                InvestigationEventKind.EvidenceCollected,
                "c_03");
            InvestigationEventHub.Publish(
                InvestigationEventKind.EvidenceCollected,
                "C-05");

            ObjectiveProgress progress =
                tracker.GetProgress("objective_d2_01_exit_inspection");
            Assert.That(progress.CompletedRequirementCount, Is.EqualTo(2));
            Assert.That(progress.IsCompleted, Is.False);

            InvestigationEventHub.Publish(
                InvestigationEventKind.EvidenceCollected,
                "C-04");

            Assert.That(progress.CompletedRequirementCount, Is.EqualTo(3));
            Assert.That(progress.IsCompleted, Is.True);
            Assert.That(state.HasCompletedObjective(progress.Definition.Id), Is.True);
        }

        [Test]
        public void BallastObjective_RequiresBothSceneEvidenceIds()
        {
            InvestigationEventHub.Publish(
                InvestigationEventKind.EvidenceCollected,
                "C-06");
            ObjectiveProgress progress =
                tracker.GetProgress("objective_d6_03_ballast_trace");

            Assert.That(progress.CompletedRequirementCount, Is.EqualTo(1));
            Assert.That(progress.IsCompleted, Is.False);

            InvestigationEventHub.Publish(
                InvestigationEventKind.EvidenceCollected,
                "C-12");

            Assert.That(progress.IsCompleted, Is.True);
            Assert.That(state.CompletedObjectiveIds,
                Contains.Item("objective_d6_03_ballast_trace"));
        }

        [Test]
        public void CompletedObjective_RestoresAsCompleteAfterManagerRecreation()
        {
            InvestigationEventHub.Publish(
                InvestigationEventKind.SceneEntered,
                "P-01");
            tracker.Dispose();
            UnityEngine.Object.DestroyImmediate(host);
            state = CreateManager();
            state.ReloadSavedState();
            tracker = new InvestigationObjectiveTracker(state);

            ObjectiveProgress restored =
                tracker.GetProgress("objective_p_01_arrival");
            Assert.That(restored.IsCompleted, Is.True);
            Assert.That(restored.CompletedRequirementCount,
                Is.EqualTo(restored.RequirementCount));
        }

        [Test]
        public void AnxietyThreshold_PublishesStateThresholdEvent()
        {
            InvestigationEvent received = default;
            void Capture(InvestigationEvent item)
            {
                if (item.Kind == InvestigationEventKind.StateThresholdReached)
                {
                    received = item;
                }
            }

            InvestigationEventHub.Published += Capture;
            try
            {
                state.ChangePublicAnxiety(55);
            }
            finally
            {
                InvestigationEventHub.Published -= Capture;
            }

            Assert.That(received.Kind,
                Is.EqualTo(InvestigationEventKind.StateThresholdReached));
            Assert.That(received.SubjectId,
                Is.EqualTo("PUBLICANXIETYRESTRICTION"));
        }

        private GameStateManager CreateManager()
        {
            host = new GameObject("InvestigationObjectiveState");
            return host.AddComponent<GameStateManager>();
        }

        private static void DestroyExistingManager()
        {
            if (GameStateManager.Instance != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    GameStateManager.Instance.gameObject);
            }
        }
    }
}
