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
        public void SourceCatalog_UsesDialogueMasterTwelveEventOrder()
        {
            Assert.That(
                TimelinePuzzleCatalog.SourceBackedCards.Count,
                Is.EqualTo(12));
            Assert.That(
                TimelinePuzzleCatalog.SourceMissingCount,
                Is.Zero);
            Assert.That(
                TimelinePuzzleCatalog.SourceBackedCards.Select(card => card.Label),
                Is.EqualTo(new[]
                {
                    "토큰 전달",
                    "금고 덮어쓰기",
                    "파티 사진",
                    "Daniel 서비스 구역 진입",
                    "Ballast 구역 도착",
                    "질소 주입 시작",
                    "Daniel 사망",
                    "녹음 생성",
                    "서비스 레일 출발",
                    "감지기 오류",
                    "세면대 작동",
                    "시신 발견"
                }));
            Assert.That(TimelinePuzzleCatalog.RequiredSequence, Is.EqualTo(new[]
            {
                TimelinePuzzleCatalog.TokenHandoff,
                TimelinePuzzleCatalog.VaultOverwrite,
                TimelinePuzzleCatalog.PartyPhoto,
                TimelinePuzzleCatalog.DanielServiceEntry,
                TimelinePuzzleCatalog.BallastArrival,
                TimelinePuzzleCatalog.NitrogenStart,
                TimelinePuzzleCatalog.Murder,
                TimelinePuzzleCatalog.RecordingCreated,
                TimelinePuzzleCatalog.BodyDeparture,
                TimelinePuzzleCatalog.DetectorError,
                TimelinePuzzleCatalog.SinkOverflow,
                TimelinePuzzleCatalog.Discovery
            }));
        }

        [Test]
        public void SourceCatalog_PreservesAllDialogueMasterTimes()
        {
            Assert.That(
                TimelinePuzzleCatalog.SourceBackedCards
                    .Select(card => card.ConfirmedTime),
                Is.EqualTo(new[]
                {
                    "20:56", "21:05", "21:20", "21:22",
                    "21:35", "21:43", "21:46", "21:47",
                    "22:17", "22:18", "22:27", "22:45"
                }));
            Assert.That(
                TimelinePuzzleCatalog.SourceBackedCards.All(
                    card => card.HasAuthoritativeSource),
                Is.True);
            Assert.That(
                TimelinePuzzleCatalog.SourceBackedCards
                    .Select(card => card.SourceReference),
                Has.All.EqualTo(TimelinePuzzleCatalog.DialogueTimeSource));
        }

        [Test]
        public void SourceCoverage_ReportsCompleteDialogueMasterContract()
        {
            TimelineSourceCoverage coverage =
                TimelinePuzzleCatalog.SourceCoverage;

            Assert.That(coverage.RequiredCount, Is.EqualTo(12));
            Assert.That(coverage.DefinitionCount, Is.EqualTo(12));
            Assert.That(coverage.AuthoritativeCount, Is.EqualTo(12));
            Assert.That(coverage.MissingSourceCount, Is.Zero);
            Assert.That(coverage.UnverifiedDefinitionCount, Is.Zero);
            Assert.That(coverage.IsComplete, Is.True);
        }

        [Test]
        public void SourceCatalog_UsesUniqueStableIdsAndReadableLabels()
        {
            IReadOnlyList<TimelineCardDefinition> cards =
                TimelinePuzzleCatalog.SourceBackedCards;

            Assert.That(
                cards.Select(card => card.Id).Distinct().Count(),
                Is.EqualTo(TimelinePuzzleCatalog.RequiredCardCount));
            Assert.That(
                cards.All(card => !string.IsNullOrWhiteSpace(card.Id)),
                Is.True);
            Assert.That(
                cards.All(card => !string.IsNullOrWhiteSpace(card.Label)),
                Is.True);
            Assert.That(
                TimelinePuzzleValidator.Validate(cards).Count,
                Is.Zero);
        }

        [Test]
        public void CurrentSourceCatalog_CanCompleteInWorkbookOrder()
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

            Assert.That(result.Completed, Is.True);
            Assert.That(result.MissingCardCount, Is.Zero);
            Assert.That(result.Diagnostics, Is.Empty);
        }

        [Test]
        public void NewCardsPlacementHintAndSave_AreRestored()
        {
            var session = new TimelinePuzzleSession(
                state,
                TimelinePuzzleCatalog.SourceBackedCards);
            session.Place(TimelinePuzzleCatalog.VaultOverwrite, 1);
            session.Place(TimelinePuzzleCatalog.SinkOverflow, 10);
            session.Place(TimelinePuzzleCatalog.SinkOverflow, 11);
            session.UseHint();
            session.UseHint();

            state.ReloadSavedState();
            var restored = new TimelinePuzzleSession(
                state,
                TimelinePuzzleCatalog.SourceBackedCards);

            Assert.That(
                restored.Placements[1],
                Is.EqualTo(TimelinePuzzleCatalog.VaultOverwrite));
            Assert.That(restored.Placements.ContainsKey(10), Is.False);
            Assert.That(
                restored.Placements[11],
                Is.EqualTo(TimelinePuzzleCatalog.SinkOverflow));
            Assert.That(restored.HintLevel, Is.EqualTo(2));
            Assert.That(restored.GetHint(), Does.Contain("21:43"));
        }

        [Test]
        public void ObsoleteSevenCardIds_AreRejected()
        {
            var session = new TimelinePuzzleSession(
                state,
                TimelinePuzzleCatalog.SourceBackedCards);
            TimelinePlacementResult first = session.Place("last_sighting", 0);
            TimelinePlacementResult second = session.Place("movement", 3);
            TimelinePlacementResult current =
                session.Place(TimelinePuzzleCatalog.Discovery, 11);

            state.ReloadSavedState();
            var restored = new TimelinePuzzleSession(
                state,
                TimelinePuzzleCatalog.SourceBackedCards);

            Assert.That(first, Is.EqualTo(TimelinePlacementResult.UnknownCard));
            Assert.That(second, Is.EqualTo(TimelinePlacementResult.UnknownCard));
            Assert.That(current, Is.EqualTo(TimelinePlacementResult.Placed));
            Assert.That(restored.Placements, Has.Count.EqualTo(1));
            Assert.That(
                restored.Placements[11],
                Is.EqualTo(TimelinePuzzleCatalog.Discovery));
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
            Assert.That(slots[3].Label, Does.Contain("21:46"));
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
        public void Presentation_ShowsCompleteSourceCoverage()
        {
            string status = TimelinePuzzlePresentation.SourceStatus(
                TimelinePuzzleCatalog.SourceBackedCards);
            var session = new TimelinePuzzleSession(
                state,
                TimelinePuzzleCatalog.SourceBackedCards);
            TimelineCompletionResult result = session.TryComplete();

            Assert.That(status, Does.Contain("근거 확인 12/12"));
            Assert.That(status, Does.Not.Contain("source_missing"));
            Assert.That(
                TimelinePuzzlePresentation.Diagnostics(result),
                Does.Not.Contain("근거 자료 미확정"));
        }

        private static List<TimelineCardDefinition> CreateCompleteContract(
            bool authoritative)
        {
            List<TimelineCardDefinition> cards =
                TimelinePuzzleCatalog.SourceBackedCards
                    .Select((card, index) => new TimelineCardDefinition(
                        card.Id,
                        card.Label,
                        card.ConfirmedTime,
                        authoritative || index < 7
                            ? card.SourceReference
                            : string.Empty))
                    .ToList();
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
