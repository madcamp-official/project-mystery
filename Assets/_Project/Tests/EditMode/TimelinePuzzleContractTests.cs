using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wake.Core;
using Wake.Puzzles;
using Wake.UI;

namespace Wake.Tests
{
    public class TimelinePuzzleContractTests
    {
        private const string SaveKey = "UNDER_THE_HORIZON_GAME_STATE_V2";
        private const string BackupKey = SaveKey + "_BACKUP";
        private const string PendingKey = SaveKey + "_PENDING";
        private GameObject host;
        private GameStateManager state;

        [SetUp]
        public void SetUp()
        {
            if (GameStateManager.Instance != null)
            {
                Object.DestroyImmediate(GameStateManager.Instance.gameObject);
            }

            ClearSaveSlots();
            host = new GameObject("TimelinePuzzleContractTests");
            state = host.AddComponent<GameStateManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (GameStateManager.Instance != null)
            {
                Object.DestroyImmediate(GameStateManager.Instance.gameObject);
            }

            ClearSaveSlots();
        }

        [Test]
        public void SourceCatalog_UsesScenarioSevenEventOrder()
        {
            Assert.That(
                TimelinePuzzleCatalog.SourceBackedCards.Count,
                Is.EqualTo(7));
            Assert.That(
                TimelinePuzzleCatalog.SourceMissingCount,
                Is.EqualTo(5));
            Assert.That(
                TimelinePuzzleCatalog.SourceBackedCards.Select(card => card.Label),
                Is.EqualTo(new[]
                {
                    "Daniel의 마지막 목격",
                    "Evelyn의 파티 복귀",
                    "실제 살해",
                    "시신 출발",
                    "감지기 오류",
                    "세면대 범람",
                    "발견"
                }));
            Assert.That(TimelinePuzzleCatalog.RequiredSequence, Is.EqualTo(new[]
            {
                TimelinePuzzleCatalog.LastSighting,
                TimelinePuzzleCatalog.EvelynPartyReturn,
                TimelinePuzzleCatalog.Murder,
                TimelinePuzzleCatalog.BodyDeparture,
                TimelinePuzzleCatalog.DetectorError,
                TimelinePuzzleCatalog.SinkOverflow,
                TimelinePuzzleCatalog.Discovery
            }));
        }

        [Test]
        public void SourceCatalog_PreservesOnlyDocumentedTimes()
        {
            Assert.That(
                TimelinePuzzleCatalog.SourceBackedCards
                    .Single(card => card.Id == TimelinePuzzleCatalog.Murder)
                    .ConfirmedTime,
                Is.EqualTo("21:45"));
            Assert.That(
                TimelinePuzzleCatalog.SourceBackedCards
                    .Single(card => card.Id == TimelinePuzzleCatalog.BodyDeparture)
                    .ConfirmedTime,
                Is.EqualTo("22:18"));
            Assert.That(
                TimelinePuzzleCatalog.SourceBackedCards
                    .Where(card =>
                        card.Id != TimelinePuzzleCatalog.Murder &&
                        card.Id != TimelinePuzzleCatalog.BodyDeparture)
                    .Select(card => card.ConfirmedTime),
                Has.All.Empty);
            Assert.That(
                TimelinePuzzleCatalog.SourceBackedCards.All(
                    card => card.HasAuthoritativeSource),
                Is.True);
            Assert.That(
                TimelinePuzzleCatalog.SourceBackedCards
                    .Where(card => string.IsNullOrEmpty(card.ConfirmedTime))
                    .All(card =>
                        !card.Label.Any(character =>
                            character >= '0' && character <= '9')),
                Is.True);
        }

        [Test]
        public void SourceCoverage_ReportsSevenOfTwelveWithoutInventingCards()
        {
            TimelineSourceCoverage coverage =
                TimelinePuzzleCatalog.SourceCoverage;

            Assert.That(coverage.RequiredCount, Is.EqualTo(12));
            Assert.That(coverage.DefinitionCount, Is.EqualTo(7));
            Assert.That(coverage.AuthoritativeCount, Is.EqualTo(7));
            Assert.That(coverage.MissingSourceCount, Is.EqualTo(5));
            Assert.That(coverage.UnverifiedDefinitionCount, Is.Zero);
            Assert.That(coverage.IsComplete, Is.False);
        }

        [Test]
        public void IncompleteSourceCatalog_BlocksCompletion()
        {
            var session = new TimelinePuzzleSession(
                state,
                TimelinePuzzleCatalog.SourceBackedCards);
            for (int index = 0;
                 index < TimelinePuzzleCatalog.SourceBackedCards.Count;
                 index++)
            {
                session.Place(
                    TimelinePuzzleCatalog.SourceBackedCards[index].Id,
                    index);
            }

            TimelineCompletionResult result = session.TryComplete();

            Assert.That(result.Completed, Is.False);
            Assert.That(result.MissingCardCount, Is.EqualTo(5));
            Assert.That(
                result.Diagnostics,
                Has.Some.Contains("정확히 12장"));
            Assert.That(
                result.Diagnostics.Count(message =>
                    message.StartsWith("source_missing:")),
                Is.EqualTo(5));
        }

        [Test]
        public void NewCardsPlacementHintAndSave_AreRestored()
        {
            var session = new TimelinePuzzleSession(
                state,
                TimelinePuzzleCatalog.SourceBackedCards);
            session.Place(TimelinePuzzleCatalog.EvelynPartyReturn, 1);
            session.Place(TimelinePuzzleCatalog.SinkOverflow, 5);
            session.Place(TimelinePuzzleCatalog.SinkOverflow, 6);
            session.UseHint();
            session.UseHint();

            state.ReloadSavedState();
            var restored = new TimelinePuzzleSession(
                state,
                TimelinePuzzleCatalog.SourceBackedCards);

            Assert.That(
                restored.Placements[1],
                Is.EqualTo(TimelinePuzzleCatalog.EvelynPartyReturn));
            Assert.That(restored.Placements.ContainsKey(5), Is.False);
            Assert.That(
                restored.Placements[6],
                Is.EqualTo(TimelinePuzzleCatalog.SinkOverflow));
            Assert.That(restored.HintLevel, Is.EqualTo(2));
            Assert.That(restored.GetHint(), Does.Contain("22:18"));
        }

        [Test]
        public void ExistingPlacementIds_RestoreWithoutMigration()
        {
            var session = new TimelinePuzzleSession(
                state,
                TimelinePuzzleCatalog.SourceBackedCards);
            session.Place("last_sighting", 0);
            session.Place("movement", 3);
            session.Place("body_discovery", 6);

            state.ReloadSavedState();
            var restored = new TimelinePuzzleSession(
                state,
                TimelinePuzzleCatalog.SourceBackedCards);

            Assert.That(restored.Placements[0], Is.EqualTo("last_sighting"));
            Assert.That(restored.Placements[3], Is.EqualTo("movement"));
            Assert.That(restored.Placements[6], Is.EqualTo("body_discovery"));
        }

        [Test]
        public void TwelveAuthoritativeCardsInRequiredOrder_CanComplete()
        {
            List<TimelineCardDefinition> cards =
                CreateCompleteContract(authoritative: true);
            var session = new TimelinePuzzleSession(state, cards);
            for (int index = 0; index < cards.Count; index++)
            {
                session.Place(cards[index].Id, index);
            }

            TimelineCompletionResult result = session.TryComplete();

            Assert.That(result.Completed, Is.True);
            Assert.That(
                state.HasCompletedScene(TimelinePuzzleCatalog.SceneId),
                Is.True);
            Assert.That(
                state.HasFlag("puzzle_timeline_12_cards_completed"),
                Is.True);
        }

        [Test]
        public void TwelveCardsWithoutFiveSources_RemainBlocked()
        {
            List<TimelineCardDefinition> cards =
                CreateCompleteContract(authoritative: false);
            var session = new TimelinePuzzleSession(state, cards);
            for (int index = 0; index < cards.Count; index++)
            {
                session.Place(cards[index].Id, index);
            }

            TimelineCompletionResult result = session.TryComplete();

            Assert.That(result.Completed, Is.False);
            Assert.That(
                result.Diagnostics.Count(message =>
                    message.StartsWith("source_missing:")),
                Is.EqualTo(5));
            Assert.That(
                state.HasCompletedScene(TimelinePuzzleCatalog.SceneId),
                Is.False);
        }

        [Test]
        public void WrongRequiredOrder_BlocksCompletion()
        {
            List<TimelineCardDefinition> cards =
                CreateCompleteContract(authoritative: true);
            var session = new TimelinePuzzleSession(state, cards);
            for (int index = 0; index < cards.Count; index++)
            {
                int slot = index == 1 ? 2 : index == 2 ? 1 : index;
                session.Place(cards[index].Id, slot);
            }

            Assert.That(
                session.TryComplete().Diagnostics,
                Has.Some.Contains("순서가 잘못"));
        }

        [Test]
        public void Presentation_ExposesTwelveSlotsAndTextualPlacementState()
        {
            var session = new TimelinePuzzleSession(
                state,
                TimelinePuzzleCatalog.SourceBackedCards);
            session.Place(TimelinePuzzleCatalog.Murder, 3);

            IReadOnlyList<TimelineSlotView> slots =
                TimelinePuzzlePresentation.CreateSlots(
                    session.Definitions,
                    session.Placements);

            Assert.That(slots, Has.Count.EqualTo(12));
            Assert.That(slots[0].IsEmpty, Is.True);
            Assert.That(slots[0].Label, Is.EqualTo("비어 있음"));
            Assert.That(slots[3].CardId, Is.EqualTo(TimelinePuzzleCatalog.Murder));
            Assert.That(slots[3].Label, Does.Contain("21:45"));
        }

        [Test]
        public void Presentation_LimitsDiagnosticsToReadableSummary()
        {
            var result = new TimelineCompletionResult(
                false,
                12,
                new[] { "첫 번째", "두 번째", "세 번째", "네 번째" });

            string message = TimelinePuzzlePresentation.Diagnostics(result);

            Assert.That(message, Does.Contain("첫 번째"));
            Assert.That(message, Does.Contain("세 번째"));
            Assert.That(message, Does.Not.Contain("네 번째"));
        }

        [Test]
        public void Presentation_ShowsSourceCoverageAndFiveMissingWarnings()
        {
            string status = TimelinePuzzlePresentation.SourceStatus(
                TimelinePuzzleCatalog.SourceBackedCards);
            var session = new TimelinePuzzleSession(
                state,
                TimelinePuzzleCatalog.SourceBackedCards);
            TimelineCompletionResult result = session.TryComplete();

            Assert.That(status, Does.Contain("근거 확인 7/12"));
            Assert.That(status, Does.Contain("source_missing 5장"));
            Assert.That(
                TimelinePuzzlePresentation.Diagnostics(result),
                Does.Contain("근거 자료 미확정 카드 5장"));
        }

        private static List<TimelineCardDefinition> CreateCompleteContract(
            bool authoritative)
        {
            var cards = TimelinePuzzleCatalog.SourceBackedCards.ToList();
            for (int index = cards.Count;
                 index < TimelinePuzzleCatalog.RequiredCardCount;
                 index++)
            {
                cards.Add(new TimelineCardDefinition(
                    $"test_card_{index}",
                    $"테스트 전용 카드 {index}",
                    sourceReference:
                        authoritative ? "테스트 전용 권위 자료" : string.Empty));
            }

            return cards;
        }

        private static void ClearSaveSlots()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.DeleteKey(BackupKey);
            PlayerPrefs.DeleteKey(PendingKey);
            PlayerPrefs.Save();
        }
    }
}
