using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Wake.Core;
using Wake.Puzzles;

namespace Wake.Tests.EditMode
{
    public sealed class CameraBlindSpotSessionTests
    {
        private const string SaveKey = "UNDER_THE_HORIZON_GAME_STATE_V2";
        private GameObject host;
        private GameStateManager state;
        private HashSet<string> evidence;

        [SetUp]
        public void SetUp()
        {
            DestroyState();
            PlayerPrefs.DeleteKey(SaveKey);
            host = new GameObject("CameraBlindSpotSessionTests");
            state = host.AddComponent<GameStateManager>();
            evidence = new HashSet<string>();
        }

        [TearDown]
        public void TearDown()
        {
            DestroyState();
            PlayerPrefs.DeleteKey(SaveKey);
        }

        [Test]
        public void CctvOnly_DoesNotUnlockCoreClue()
        {
            CameraBlindSpotSession session = CreateSession();

            session.ReviewCctv();
            CameraBlindSpotCompletion result = session.TryComplete();

            Assert.That(result.Completed, Is.False);
            Assert.That(result.MissingSteps, Does.Contain("설비 로그 중첩"));
            Assert.That(state.HasFlag(
                CameraBlindSpotSession.CeilingInvestigationFlag), Is.False);
            Assert.That(evidence, Does.Not.Contain(CameraBlindSpotSession.EvidenceId));
        }

        [Test]
        public void DetectorError_RequiresDoorAndLogOverlay()
        {
            CameraBlindSpotSession session = CreateSession();
            session.ReviewCctv();
            session.OpenFacilityLogs();
            session.SelectLog(FacilityLogKind.Detector);

            Assert.That(session.SelectDetectorError(), Is.False);

            session.SelectLog(FacilityLogKind.Door);
            session.SelectLog(FacilityLogKind.Detector);
            Assert.That(session.SelectDetectorError(), Is.True);
            Assert.That(
                session.CurrentSecond,
                Is.EqualTo(CameraBlindSpotSession.DetectorErrorSecond));
        }

        [Test]
        public void CompleteComparison_GrantsEvidenceFlagAndSceneCompletion()
        {
            CameraBlindSpotSession session = CreateSession();

            CompleteObservations(session);
            CameraBlindSpotCompletion result = session.TryComplete();

            Assert.That(result.Completed, Is.True);
            Assert.That(evidence, Contains.Item(CameraBlindSpotSession.EvidenceId));
            Assert.That(state.HasFlag(
                CameraBlindSpotSession.CeilingInvestigationFlag), Is.True);
            Assert.That(state.HasFlag(
                CameraBlindSpotSession.CompletionFlag), Is.True);
            Assert.That(state.HasCompletedScene(
                CameraBlindSpotSession.SceneId), Is.True);
        }

        [Test]
        public void PartialProgress_RestoresAcrossSession()
        {
            CameraBlindSpotSession session = CreateSession();
            session.ReviewCctv();
            session.OpenFacilityLogs();
            session.SelectLog(FacilityLogKind.Door);
            session.SetTime(845);

            CameraBlindSpotSession restored = CreateSession();

            Assert.That(restored.HasReviewedCctv, Is.True);
            Assert.That(restored.HasOverlaidLogs, Is.True);
            Assert.That(restored.HasConfirmedDoorLog, Is.True);
            Assert.That(restored.CurrentSecond, Is.EqualTo(845));
        }

        private CameraBlindSpotSession CreateSession()
        {
            return new CameraBlindSpotSession(
                state,
                evidence.Contains,
                evidence.Add);
        }

        private static void CompleteObservations(CameraBlindSpotSession session)
        {
            session.ReviewCctv();
            session.OpenFacilityLogs();
            session.SelectLog(FacilityLogKind.Door);
            session.SelectLog(FacilityLogKind.Detector);
            session.SelectDetectorError();
            session.ConfirmErrorLocation();
        }

        private static void DestroyState()
        {
            if (GameStateManager.Instance != null)
            {
                Object.DestroyImmediate(GameStateManager.Instance.gameObject);
            }
        }
    }
}
