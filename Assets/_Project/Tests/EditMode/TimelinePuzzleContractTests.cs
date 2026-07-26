using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wake.Core;
using Wake.Puzzles;

namespace Wake.Tests
{
    public class TimelinePuzzleContractTests
    {
        private const string SaveKey = "THE_WAKE_GAME_STATE_V1";
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

            PlayerPrefs.DeleteKey(SaveKey);
        }

        [Test]
        public void SourceCatalog_PreservesKnownTimesAndSequence()
        {
            Assert.That(
                TimelinePuzzleCatalog.SourceBackedCards
                    .Single(card => card.Id == TimelinePuzzleCatalog.Murder)
                    .ConfirmedTime,
                Is.EqualTo("21:45"));
            Assert.That(
                TimelinePuzzleCatalog.SourceBackedCards
                    .Single(card => card.Id == TimelinePuzzleCatalog.Movement)
                    .ConfirmedTime,
                Is.EqualTo("22:18"));
            Assert.That(TimelinePuzzleCatalog.RequiredSequence, Is.EqualTo(new[]
            {
                "last_sighting", "murder", "movement",
                "detector_error", "body_discovery"
            }));
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
            Assert.That(result.MissingCardCount, Is.EqualTo(7));
            Assert.That(
                result.Diagnostics,
                Has.Some.Contains("정확히 12장"));
        }

        [Test]
        public void PlacementMoveHintAndSave_AreRestored()
        {
            var session = new TimelinePuzzleSession(
                state,
                TimelinePuzzleCatalog.SourceBackedCards);
            session.Place(TimelinePuzzleCatalog.Murder, 2);
            session.Place(TimelinePuzzleCatalog.Murder, 4);
            session.UseHint();
            session.UseHint();

            state.ReloadSavedState();
            var restored = new TimelinePuzzleSession(
                state,
                TimelinePuzzleCatalog.SourceBackedCards);

            Assert.That(restored.Placements.ContainsKey(2), Is.False);
            Assert.That(
                restored.Placements[4],
                Is.EqualTo(TimelinePuzzleCatalog.Murder));
            Assert.That(restored.HintLevel, Is.EqualTo(2));
            Assert.That(restored.GetHint(), Does.Contain("22:18"));
        }

        [Test]
        public void TwelveCardsInRequiredOrder_CanComplete()
        {
            List<TimelineCardDefinition> cards = CreateCompleteContract();
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
        public void WrongRequiredOrder_BlocksCompletion()
        {
            List<TimelineCardDefinition> cards = CreateCompleteContract();
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

        private static List<TimelineCardDefinition> CreateCompleteContract()
        {
            var cards = TimelinePuzzleCatalog.SourceBackedCards.ToList();
            for (int index = cards.Count;
                 index < TimelinePuzzleCatalog.RequiredCardCount;
                 index++)
            {
                cards.Add(new TimelineCardDefinition(
                    $"test_card_{index}",
                    $"테스트 전용 카드 {index}"));
            }

            return cards;
        }
    }
}
