using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wake.Core;
using Wake.Puzzles;
using Wake.UI;

namespace Wake.Tests
{
    public class OrpheusAudioRestorationTests
    {
        private const string SaveKey = "UNDER_THE_HORIZON_GAME_STATE_V2";
        private GameObject host;
        private GameStateManager state;

        [SetUp]
        public void SetUp()
        {
            if (GameStateManager.Instance != null)
            {
                Object.DestroyImmediate(GameStateManager.Instance.gameObject);
            }

            PlayerPrefs.DeleteKey(SaveKey);
            host = new GameObject("OrpheusAudioRestorationTests");
            state = host.AddComponent<GameStateManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (GameStateManager.Instance != null)
            {
                Object.DestroyImmediate(GameStateManager.Instance.gameObject);
            }

            PlayerPrefs.DeleteKey(SaveKey);
        }

        [Test]
        public void Catalog_PreservesFourCsvLinesAndStableIds()
        {
            Assert.That(
                OrpheusRecordCatalog.All.Select(item => item.LineId),
                Is.EqualTo(new[]
                {
                    "d7_03_01", "d7_03_02", "d7_03_03", "d7_03_04"
                }));
            Assert.That(
                OrpheusRecordCatalog.All[2].Transcript,
                Does.Contain("사고가 아니라 살인"));
            Assert.That(
                OrpheusRecordCatalog.All.All(item => item.VoiceRequired),
                Is.True);
        }

        [Test]
        public void MissingAudio_UsesKoreanTranscriptFallback()
        {
            var session = new OrpheusAudioRestorationSession(state);
            OrpheusPlaybackRequest request =
                session.RequestPlayback("d7_03_01");

            Assert.That(request.Found, Is.True);
            Assert.That(request.Clip, Is.Null);
            Assert.That(request.UsesTranscriptFallback, Is.True);
            Assert.That(request.Transcript, Does.Contain("아버지"));
            Assert.That(request.Warning, Does.Contain("AudioClip 없음"));
        }

        [Test]
        public void SegmentOrderAndHint_AreSaved()
        {
            var session = new OrpheusAudioRestorationSession(state);
            session.Move("d7_03_02", 0);
            session.Move("d7_03_01", 0);
            session.UseHint();
            state.ReloadSavedState();

            var restored = new OrpheusAudioRestorationSession(state);
            Assert.That(
                restored.OrderedLineIds,
                Is.EqualTo(new[] { "d7_03_01", "d7_03_02" }));
            Assert.That(restored.HintLevel, Is.EqualTo(1));
        }

        [Test]
        public void WrongOrder_BlocksCompletion()
        {
            var session = new OrpheusAudioRestorationSession(state);
            foreach (string lineId in OrpheusRecordCatalog.All
                         .Select(item => item.LineId)
                         .Reverse())
            {
                session.Move(lineId, session.OrderedLineIds.Count);
            }

            Assert.That(session.TryComplete().Completed, Is.False);
            Assert.That(
                state.CollectedEvidenceIds,
                Does.Not.Contain(OrpheusRecordCatalog.EvidenceId));
        }

        [Test]
        public void CorrectOrder_GrantsC17AndPastCulpritFlow()
        {
            var session = new OrpheusAudioRestorationSession(state);
            foreach (OrpheusRecordSegment segment in OrpheusRecordCatalog.All)
            {
                session.Move(segment.LineId, session.OrderedLineIds.Count);
            }

            OrpheusCompletionResult result = session.TryComplete();

            Assert.That(result.Completed, Is.True);
            Assert.That(
                state.CollectedEvidenceIds,
                Does.Contain(OrpheusRecordCatalog.EvidenceId));
            Assert.That(state.HasFlag("past_culprit_confirmed"), Is.True);
            Assert.That(
                state.HasCompletedScene(OrpheusRecordCatalog.SceneId),
                Is.True);
        }

        [Test]
        public void Validator_WarnsForEveryMissingRequiredClip()
        {
            var diagnostics =
                OrpheusRecordValidator.Validate(OrpheusRecordCatalog.All);

            Assert.That(
                diagnostics.Count(message => message.Contains("AudioClip 없음")),
                Is.EqualTo(4));
            Assert.That(
                diagnostics.All(message => message.Contains("자막으로 대체")),
                Is.True);
        }

        [Test]
        public void Presentation_LabelsRecordedSpeakersAndSavedPositions()
        {
            var session = new OrpheusAudioRestorationSession(state);
            session.Move("d7_03_02", 0);

            var views = OrpheusAudioPresentation.CreateSegments(
                session.OrderedLineIds,
                "d7_03_01");

            Assert.That(views[0].Speaker, Is.EqualTo("Julian 기록 음성"));
            Assert.That(views[0].Selected, Is.True);
            Assert.That(views[1].Position, Is.Zero);
            Assert.That(views[1].IsPlaced, Is.True);
        }

        [Test]
        public void Presentation_MarksTranscriptOnlyPlaybackExplicitly()
        {
            OrpheusPlaybackRequest request =
                new OrpheusAudioRestorationSession(state)
                    .RequestPlayback("d7_03_01");

            string text = OrpheusAudioPresentation.PlaybackText(request);

            Assert.That(text, Does.Contain("음성 없음"));
            Assert.That(text, Does.Contain("한국어 자막"));
            Assert.That(text, Does.Contain("아버지"));
        }

        [Test]
        public void SelectClipIndex_PairsBothJulianSegmentsToDistinctClips()
        {
            OrpheusRecordSegment julian1 = OrpheusRecordCatalog.All[0];
            OrpheusRecordSegment julian2 = OrpheusRecordCatalog.All[2];
            string[] candidateNames =
            {
                "D7-03_JULIAN_01", "D7-03_JULIAN_02",
                "D7-03_JULIAN_03", "D7-03_JULIAN_04", "D7-03_THOMAS_01"
            };

            int index1 = ResourcesOrpheusAudioProvider.SelectClipIndex(
                julian1, OrpheusRecordCatalog.All, candidateNames);
            int index2 = ResourcesOrpheusAudioProvider.SelectClipIndex(
                julian2, OrpheusRecordCatalog.All, candidateNames);

            Assert.That(index1, Is.EqualTo(0));
            Assert.That(index2, Is.EqualTo(1));
        }

        [Test]
        public void SelectClipIndex_ReturnsMinusOneWhenNoFileMatchesSpeaker()
        {
            OrpheusRecordSegment evelynSegment = OrpheusRecordCatalog.All[1];
            string[] candidateNames =
            {
                "D7-03_JULIAN_01", "D7-03_THOMAS_01"
            };

            int index = ResourcesOrpheusAudioProvider.SelectClipIndex(
                evelynSegment, OrpheusRecordCatalog.All, candidateNames);

            Assert.That(index, Is.EqualTo(-1));
        }
    }
}
