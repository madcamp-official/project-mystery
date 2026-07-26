using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wake.Core;
using Wake.Puzzles;

namespace Wake.Tests
{
    public class ExitInspectionSessionTests
    {
        private const string SaveKey = "UNDER_THE_HORIZON_GAME_STATE_V2";
        private GameObject host;
        private GameStateManager state;
        private HashSet<string> evidence;
        private List<string> grants;
        [SetUp]
        public void SetUp()
        {
            DestroyExistingManager();
            PlayerPrefs.DeleteKey(SaveKey);
            state = CreateManager();
            state.StartNewGame();
            evidence = new HashSet<string>();
            grants = new List<string>();
        }
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
            DestroyExistingManager();
            PlayerPrefs.DeleteKey(SaveKey);
        }
        [Test]
        public void Catalog_MapsThreeRoutesToCanonicalEvidence()
        {
            Assert.That(
                ExitInspectionCatalog.All.Select(item =>
                    $"{item.Id}:{item.EvidenceId}"),
                Is.EqualTo(new[]
                {
                    "exterior_ledge:C-03",
                    "air_duct:C-04",
                    "service_hatch:C-05"
                }));
        }
        [Test]
        public void Inspect_AllowsFreeOrderAndRejectsDuplicateRewards()
        {
            ExitInspectionSession session = CreateSession();
            Assert.That(session.Inspect(ExitInspectionCatalog.ServiceHatch),
                Is.EqualTo(ExitInspectionResult.Recorded));
            Assert.That(session.Inspect(ExitInspectionCatalog.ExteriorLedge),
                Is.EqualTo(ExitInspectionResult.Recorded));
            Assert.That(session.Inspect(ExitInspectionCatalog.ServiceHatch),
                Is.EqualTo(ExitInspectionResult.AlreadyInspected));
            Assert.That(session.Inspect(ExitInspectionCatalog.AirDuct),
                Is.EqualTo(ExitInspectionResult.Recorded));

            Assert.That(session.InspectionOrder,
                Is.EqualTo(new[]
                {
                    ExitInspectionCatalog.ServiceHatch,
                    ExitInspectionCatalog.ExteriorLedge,
                    ExitInspectionCatalog.AirDuct
                }));
            Assert.That(grants, Is.EqualTo(new[] { "C-05", "C-03", "C-04" }));
            Assert.That(session.Step, Is.EqualTo(3));
        }
        [Test]
        public void Progress_RestoresOrderAndHintAfterMidSessionExit()
        {
            ExitInspectionSession session = CreateSession();
            session.Inspect(ExitInspectionCatalog.AirDuct);
            session.Inspect(ExitInspectionCatalog.ExteriorLedge);
            Assert.That(session.UseHint(), Is.True);
            Assert.That(session.UseHint(), Is.True);
            Object.DestroyImmediate(host);
            state = CreateManager();
            state.ReloadSavedState();
            evidence.Clear();
            ExitInspectionSession restored = CreateSession();
            Assert.That(restored.InspectionOrder,
                Is.EqualTo(new[]
                {
                    ExitInspectionCatalog.AirDuct,
                    ExitInspectionCatalog.ExteriorLedge
                }));
            Assert.That(restored.Step, Is.EqualTo(2));
            Assert.That(restored.HintLevel, Is.EqualTo(2));
            Assert.That(restored.IsCompleted, Is.False);
            Assert.That(restored.Inspect(ExitInspectionCatalog.AirDuct),
                Is.EqualTo(ExitInspectionResult.AlreadyInspected));
            Assert.That(evidence, Contains.Item("C-04"));
        }

        [Test]
        public void Completion_RequiresAllRoutesThenUnlocksAndCompletesScene()
        {
            ExitInspectionSession session = CreateSession();
            session.Inspect(ExitInspectionCatalog.ExteriorLedge);

            ExitInspectionCompletion incomplete = session.TryComplete();
            session.Inspect(ExitInspectionCatalog.AirDuct);
            session.Inspect(ExitInspectionCatalog.ServiceHatch);
            ExitInspectionCompletion completed = session.TryComplete();

            Assert.That(incomplete.Completed, Is.False);
            Assert.That(incomplete.MissingInspectionIds.Count, Is.EqualTo(2));
            Assert.That(completed.Completed, Is.True);
            Assert.That(session.IsCompleted, Is.True);
            Assert.That(state.HasUnlockedDeduction(
                    CanonicalDeductionCatalog.SceneDenial),
                Is.True);
            Assert.That(state.HasFlag(ExitInspectionCatalog.CompletionFlag), Is.True);
            Assert.That(state.HasCompletedScene(ExitInspectionCatalog.SceneId), Is.True);
        }

        [Test]
        public void Completion_IsIdempotentAndRestoresCompletedState()
        {
            int sceneEvents = 0;
            void Count(InvestigationEvent item)
            {
                if (item.Kind == InvestigationEventKind.SceneCompleted &&
                    item.SubjectId == ExitInspectionCatalog.SceneId)
                {
                    sceneEvents++;
                }
            }

            InvestigationEventHub.Published += Count;
            try
            {
                ExitInspectionSession session = CreateSession();
                foreach (ExitInspectionDefinition definition in ExitInspectionCatalog.All)
                {
                    session.Inspect(definition.Id);
                }

                Assert.That(session.TryComplete().Completed, Is.True);
                Assert.That(session.TryComplete().Completed, Is.False);
                ExitInspectionSession restored = CreateSession();
                Assert.That(restored.IsCompleted, Is.True);
            }
            finally
            {
                InvestigationEventHub.Published -= Count;
            }

            Assert.That(sceneEvents, Is.EqualTo(1));
        }

        private ExitInspectionSession CreateSession()
        {
            return new ExitInspectionSession(
                state,
                evidence.Contains,
                evidenceId =>
                {
                    grants.Add(evidenceId);
                    return evidence.Add(evidenceId);
                });
        }

        private GameStateManager CreateManager()
        {
            host = new GameObject("ExitInspectionSessionTests");
            return host.AddComponent<GameStateManager>();
        }

        private static void DestroyExistingManager()
        {
            if (GameStateManager.Instance != null)
            {
                Object.DestroyImmediate(GameStateManager.Instance.gameObject);
            }
        }
    }
}
