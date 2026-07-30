using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wake.Core;
using Wake.Narrative;
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
            if (host != null)
            {
                Object.DestroyImmediate(host);
            }

            DestroyExistingManager();
            PlayerPrefs.DeleteKey(SaveKey);
        }

        [Test]
        public void Catalog_MapsThreeRoutesToTwoObservationsAndCanonicalEvidence()
        {
            Assert.That(
                ExitInspectionCatalog.All.Select(item =>
                    $"{item.Id}:{item.EvidenceId}:{item.ObservationPointIds.Count}"),
                Is.EqualTo(new[]
                {
                    "exterior_ledge:C-03:2",
                    "air_duct:C-04:2",
                    "service_hatch:C-05:2"
                }));
            Assert.That(
                ExitInspectionCatalog.All.SelectMany(item =>
                    item.ObservationPointIds),
                Is.EqualTo(new[]
                {
                    "SALT_FILM", "PRESSURE_SENSOR",
                    "DUCT_DUST", "INNER_WALL",
                    "SCREWS", "DUST_SEAL"
                }));
        }

        [Test]
        public void Observe_AllowsFreeOrderAndRewardsOnlyCompletedRouteOnce()
        {
            ExitInspectionSession session = CreateSession();

            ExitInspectionAction ductFirst = session.Observe(
                ExitInspectionCatalog.AirDuct,
                "INNER_WALL");
            ExitInspectionAction hatchFirst = session.Observe(
                ExitInspectionCatalog.ServiceHatch,
                "SCREWS");
            ExitInspectionAction ductSecond = session.Observe(
                ExitInspectionCatalog.AirDuct,
                "DUCT_DUST");
            ExitInspectionAction duplicate = session.Observe(
                ExitInspectionCatalog.AirDuct,
                "DUCT_DUST");
            ExitInspectionAction hatchSecond = session.Observe(
                ExitInspectionCatalog.ServiceHatch,
                "DUST_SEAL");

            Assert.That(ductFirst.Code, Is.EqualTo(ExitInspectionActionCode.Recorded));
            Assert.That(hatchFirst.Code, Is.EqualTo(ExitInspectionActionCode.Recorded));
            Assert.That(
                ductSecond.Code,
                Is.EqualTo(ExitInspectionActionCode.RouteObservationCompleted));
            Assert.That(
                duplicate.Code,
                Is.EqualTo(ExitInspectionActionCode.AlreadyRecorded));
            Assert.That(
                hatchSecond.Code,
                Is.EqualTo(ExitInspectionActionCode.RouteObservationCompleted));
            Assert.That(
                session.InspectionOrder,
                Is.EqualTo(new[]
                {
                    ExitInspectionCatalog.AirDuct,
                    ExitInspectionCatalog.ServiceHatch
                }));
            Assert.That(session.Step, Is.EqualTo(2));
            Assert.That(session.ObservationCount, Is.EqualTo(4));
            Assert.That(grants, Is.EqualTo(new[] { "C-04", "C-05" }));
        }

        [Test]
        public void SetRouteVerdict_RequiresBothObservations()
        {
            ExitInspectionSession session = CreateSession();
            session.Observe(ExitInspectionCatalog.ExteriorLedge, "SALT_FILM");

            ExitInspectionAction early = session.SetRouteVerdict(
                ExitInspectionCatalog.ExteriorLedge,
                ExitRouteVerdict.Unused);
            session.Observe(
                ExitInspectionCatalog.ExteriorLedge,
                "PRESSURE_SENSOR");
            ExitInspectionAction accepted = session.SetRouteVerdict(
                ExitInspectionCatalog.ExteriorLedge,
                ExitRouteVerdict.Unused);

            Assert.That(early.Accepted, Is.False);
            Assert.That(
                early.Code,
                Is.EqualTo(ExitInspectionActionCode.ObservationsIncomplete));
            Assert.That(early.Message, Does.Contain("두 가지"));
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(
                session.GetVerdict(ExitInspectionCatalog.ExteriorLedge),
                Is.EqualTo(ExitRouteVerdict.Unused));
        }

        [Test]
        public void Completion_ClickingEveryRouteAloneCannotComplete()
        {
            ExitInspectionSession session = CreateSession();
            InspectAll(session);

            ExitInspectionCompletion result = session.TryComplete();

            Assert.That(result.Completed, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(ExitInspectionCompletionFailure.MissingVerdicts));
            Assert.That(result.Message, Does.Contain("사용 여부"));
            Assert.That(session.IsCompleted, Is.False);
            Assert.That(
                state.HasUnlockedDeduction(CanonicalDeductionCatalog.SceneDenial),
                Is.False);
            Assert.That(
                state.HasCompletedScene(ExitInspectionCatalog.SceneId),
                Is.False);
        }

        [TestCase(ExitRouteVerdict.Used, "모순")]
        [TestCase(ExitRouteVerdict.Inconclusive, "판단 불가")]
        public void Completion_IncorrectRouteVerdictReturnsTargetedFeedback(
            ExitRouteVerdict wrongVerdict,
            string expectedMessage)
        {
            ExitInspectionSession session = CreateSession();
            InspectAll(session);
            SetAllVerdicts(session, ExitRouteVerdict.Unused);
            session.SetRouteVerdict(
                ExitInspectionCatalog.AirDuct,
                wrongVerdict);
            session.SelectTheory(ExitInspectionTheory.NoLiveThirdParty);

            ExitInspectionCompletion result = session.TryComplete();

            Assert.That(result.Completed, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(ExitInspectionCompletionFailure.IncorrectVerdicts));
            Assert.That(
                result.IncorrectVerdictIds,
                Is.EqualTo(new[] { ExitInspectionCatalog.AirDuct }));
            Assert.That(result.Message, Does.Contain(expectedMessage));
        }

        [TestCase(
            ExitInspectionTheory.PerfectCleanup,
            ExitInspectionCompletionFailure.PerfectCleanupContradicted,
            "센서 기록")]
        [TestCase(
            ExitInspectionTheory.DoorExit,
            ExitInspectionCompletionFailure.DoorExitContradicted,
            "젖은 발자국")]
        public void Completion_WrongTheoryReturnsSpecificContradiction(
            ExitInspectionTheory theory,
            ExitInspectionCompletionFailure expectedFailure,
            string expectedMessage)
        {
            ExitInspectionSession session = CreateSession();
            InspectAll(session);
            SetAllVerdicts(session, ExitRouteVerdict.Unused);
            Assert.That(session.SelectTheory(theory).Accepted, Is.True);

            ExitInspectionCompletion result = session.TryComplete();

            Assert.That(result.Completed, Is.False);
            Assert.That(result.Failure, Is.EqualTo(expectedFailure));
            Assert.That(result.Message, Does.Contain(expectedMessage));
            Assert.That(session.IsCompleted, Is.False);
        }

        [Test]
        public void Completion_RequiresUnusedVerdictsAndNoLiveThirdPartyTheory()
        {
            ExitInspectionSession session = CreateSession();
            InspectAll(session);
            SetAllVerdicts(session, ExitRouteVerdict.Unused);
            session.SelectTheory(ExitInspectionTheory.NoLiveThirdParty);

            ExitInspectionCompletion completed = session.TryComplete();

            Assert.That(completed.Completed, Is.True);
            Assert.That(completed.Failure, Is.EqualTo(
                ExitInspectionCompletionFailure.None));
            Assert.That(completed.Message, Does.Contain("살아 있는 제3자"));
            Assert.That(session.IsCompleted, Is.True);
            Assert.That(state.HasUnlockedDeduction(
                    CanonicalDeductionCatalog.SceneDenial),
                Is.True);
            Assert.That(state.HasFlag(ExitInspectionCatalog.CompletionFlag), Is.True);
            Assert.That(state.HasCompletedScene(ExitInspectionCatalog.SceneId), Is.True);
            Assert.That(
                state.IsProductionSceneUnlocked(
                    ExitInspectionCatalog.ParallelSceneId),
                Is.True);
            Assert.That(
                ProductionSceneCompletionCatalog.TryGet(
                    ExitInspectionCatalog.SceneId,
                    out ProductionSceneCompletionRequirement requirement),
                Is.True);
            Assert.That(
                state.IsProductionSceneUnlocked(requirement.NextSceneId),
                Is.True);
        }

        [Test]
        public void Completion_SceneGateFailureKeepsSessionAndBranchLocked()
        {
            ExitInspectionSession session = CreateSession((_, _, _) => false);
            InspectAll(session);
            SetAllVerdicts(session, ExitRouteVerdict.Unused);
            session.SelectTheory(ExitInspectionTheory.NoLiveThirdParty);

            ExitInspectionCompletion failed = session.TryComplete();

            Assert.That(failed.Completed, Is.False);
            Assert.That(
                failed.Failure,
                Is.EqualTo(
                    ExitInspectionCompletionFailure.SceneCompletionFailed));
            Assert.That(session.IsCompleted, Is.False);
            Assert.That(
                state.TryGetPuzzleSession(
                    ExitInspectionCatalog.SessionId,
                    out PuzzleSessionState saved),
                Is.True);
            Assert.That(saved.completed, Is.False);
            Assert.That(
                state.HasUnlockedDeduction(
                    CanonicalDeductionCatalog.SceneDenial),
                Is.False);
            Assert.That(
                state.HasFlag(ExitInspectionCatalog.CompletionFlag),
                Is.False);
            Assert.That(
                state.HasCompletedScene(ExitInspectionCatalog.SceneId),
                Is.False);
            Assert.That(
                state.IsProductionSceneUnlocked(
                    ExitInspectionCatalog.ParallelSceneId),
                Is.False);
            Assert.That(
                ProductionSceneCompletionCatalog.TryGet(
                    ExitInspectionCatalog.SceneId,
                    out ProductionSceneCompletionRequirement requirement),
                Is.True);
            Assert.That(
                state.IsProductionSceneUnlocked(requirement.NextSceneId),
                Is.False);
        }

        [Test]
        public void CurrentProgress_RestoresObservationsVerdictsTheoryAndHint()
        {
            ExitInspectionSession session = CreateSession();
            session.Observe(ExitInspectionCatalog.AirDuct, "INNER_WALL");
            session.Observe(ExitInspectionCatalog.AirDuct, "DUCT_DUST");
            session.SetRouteVerdict(
                ExitInspectionCatalog.AirDuct,
                ExitRouteVerdict.Unused);
            session.Observe(ExitInspectionCatalog.ExteriorLedge, "SALT_FILM");
            session.UseHint();
            session.UseHint();

            CompleteRoute(session, ExitInspectionCatalog.ExteriorLedge);
            session.SetRouteVerdict(
                ExitInspectionCatalog.ExteriorLedge,
                ExitRouteVerdict.Inconclusive);
            CompleteRoute(session, ExitInspectionCatalog.ServiceHatch);
            session.SetRouteVerdict(
                ExitInspectionCatalog.ServiceHatch,
                ExitRouteVerdict.Unused);
            session.SelectTheory(ExitInspectionTheory.DoorExit);
            RecreateManagerFromSave();

            ExitInspectionSession restored = CreateSession();

            Assert.That(restored.Step, Is.EqualTo(3));
            Assert.That(restored.ObservationCount, Is.EqualTo(6));
            Assert.That(restored.HintLevel, Is.EqualTo(2));
            Assert.That(
                restored.GetVerdict(ExitInspectionCatalog.AirDuct),
                Is.EqualTo(ExitRouteVerdict.Unused));
            Assert.That(
                restored.GetVerdict(ExitInspectionCatalog.ExteriorLedge),
                Is.EqualTo(ExitRouteVerdict.Inconclusive));
            Assert.That(
                restored.SelectedTheory,
                Is.EqualTo(ExitInspectionTheory.DoorExit));
            Assert.That(restored.IsCompleted, Is.False);
        }

        [Test]
        public void LegacyIncompleteSave_MigratesRoutesToObservationsWithoutAnswer()
        {
            state.SavePuzzleSession(new PuzzleSessionState
            {
                puzzleId = ExitInspectionCatalog.SessionId,
                selectedIds = new List<string>
                {
                    ExitInspectionCatalog.AirDuct,
                    ExitInspectionCatalog.ExteriorLedge
                },
                step = 2,
                hintLevel = 1,
                completed = false
            });

            ExitInspectionSession restored = CreateSession();

            Assert.That(restored.Step, Is.EqualTo(2));
            Assert.That(restored.ObservationCount, Is.EqualTo(4));
            Assert.That(restored.HasObserved(
                ExitInspectionCatalog.AirDuct, "DUCT_DUST"), Is.True);
            Assert.That(restored.HasObserved(
                ExitInspectionCatalog.AirDuct, "INNER_WALL"), Is.True);
            Assert.That(
                restored.GetVerdict(ExitInspectionCatalog.AirDuct),
                Is.EqualTo(ExitRouteVerdict.None));
            Assert.That(
                restored.SelectedTheory,
                Is.EqualTo(ExitInspectionTheory.None));
            Assert.That(restored.IsCompleted, Is.False);

            Assert.That(
                restored.Inspect(ExitInspectionCatalog.AirDuct),
                Is.EqualTo(ExitInspectionResult.AlreadyInspected));
            Assert.That(grants, Is.EqualTo(new[] { "C-04" }));
        }

        [Test]
        public void LegacyCompletedSave_RemainsCompletedAndRestoresCorrectAnswer()
        {
            state.SavePuzzleSession(new PuzzleSessionState
            {
                puzzleId = ExitInspectionCatalog.SessionId,
                selectedIds = new List<string>(),
                step = 3,
                hintLevel = 3,
                completed = true
            });

            ExitInspectionSession restored = CreateSession();

            Assert.That(restored.IsCompleted, Is.True);
            Assert.That(restored.Step, Is.EqualTo(3));
            Assert.That(restored.ObservationCount, Is.EqualTo(6));
            Assert.That(
                restored.SelectedTheory,
                Is.EqualTo(ExitInspectionTheory.NoLiveThirdParty));
            Assert.That(ExitInspectionCatalog.All.All(item =>
                restored.GetVerdict(item.Id) == ExitRouteVerdict.Unused), Is.True);
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
                InspectAll(session);
                SetAllVerdicts(session, ExitRouteVerdict.Unused);
                session.SelectTheory(ExitInspectionTheory.NoLiveThirdParty);

                Assert.That(session.TryComplete().Completed, Is.True);
                Assert.That(session.TryComplete().Failure, Is.EqualTo(
                    ExitInspectionCompletionFailure.AlreadyCompleted));
                ExitInspectionSession restored = CreateSession();
                Assert.That(restored.IsCompleted, Is.True);
            }
            finally
            {
                InvestigationEventHub.Published -= Count;
            }

            Assert.That(sceneEvents, Is.EqualTo(1));
        }

        private void InspectAll(ExitInspectionSession session)
        {
            foreach (ExitInspectionDefinition definition
                     in ExitInspectionCatalog.All)
            {
                Assert.That(
                    session.Inspect(definition.Id),
                    Is.EqualTo(ExitInspectionResult.Recorded));
            }
        }

        private static void CompleteRoute(
            ExitInspectionSession session,
            string routeId)
        {
            ExitInspectionCatalog.TryGet(routeId, out var definition);
            foreach (string pointId in definition.ObservationPointIds)
            {
                if (!session.HasObserved(routeId, pointId))
                {
                    session.Observe(routeId, pointId);
                }
            }
        }

        private static void SetAllVerdicts(
            ExitInspectionSession session,
            ExitRouteVerdict verdict)
        {
            foreach (ExitInspectionDefinition definition
                     in ExitInspectionCatalog.All)
            {
                Assert.That(
                    session.SetRouteVerdict(definition.Id, verdict).Accepted,
                    Is.True);
            }
        }

        private ExitInspectionSession CreateSession()
        {
            return CreateSession(
                ProductionSceneCompletionGate.TryComplete);
        }

        private ExitInspectionSession CreateSession(
            System.Func<GameStateManager, string, string, bool>
                tryCompleteScene)
        {
            return new ExitInspectionSession(
                state,
                evidence.Contains,
                evidenceId =>
                {
                    grants.Add(evidenceId);
                    return evidence.Add(evidenceId);
                },
                tryCompleteScene);
        }

        private void RecreateManagerFromSave()
        {
            Object.DestroyImmediate(host);
            state = CreateManager();
            state.ReloadSavedState();
            evidence.Clear();
            grants.Clear();
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
