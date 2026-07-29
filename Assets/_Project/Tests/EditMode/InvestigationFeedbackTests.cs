using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wake.Core;
using Wake.Exploration;
using Wake.Puzzles;

namespace Wake.Tests
{
    public class InvestigationFeedbackTests
    {
        [Test]
        public void ObjectiveCatalog_UsesKoreanPlayerFacingTitles()
        {
            foreach (InvestigationObjectiveDefinition objective in
                     InvestigationObjectiveCatalog.All)
            {
                Assert.That(objective.Title, Is.Not.Empty);
                Assert.That(
                    objective.Title.Any(character =>
                        character >= '\uAC00' && character <= '\uD7A3'),
                    Is.True,
                    objective.Id);
                Assert.That(KoreanTextIntegrity.IsClean(objective.Title), Is.True);
            }
        }

        [TestCase(
            SceneAccessDenialReason.SceneNotRegistered,
            "아직 등록되지 않은 조사 장면")]
        [TestCase(
            SceneAccessDenialReason.PhysicalLocationUnresolved,
            "장소가 아직 확정되지 않았습니다")]
        [TestCase(
            SceneAccessDenialReason.LocationUnused,
            "사용하지 않는 장소")]
        [TestCase(
            SceneAccessDenialReason.PrerequisiteSceneIncomplete,
            "선행 조사를 완료")]
        [TestCase(
            SceneAccessDenialReason.RestrictedByPublicAnxiety,
            "승객 불안이 70 이상")]
        [TestCase(
            SceneAccessDenialReason.LocationLoadFailed,
            "다시 시도")]
        public void TravelDenial_MapsToKoreanPlayerFeedback(
            SceneAccessDenialReason reason,
            string expected)
        {
            SceneTravelResult result = SceneTravelResult.Denied(
                reason,
                "Developer diagnostic");

            InvestigationFeedback feedback =
                InvestigationFeedbackCatalog.ForTravel(result);

            Assert.That(feedback.Code, Is.Not.Empty);
            Assert.That(feedback.Title, Is.Not.Empty);
            Assert.That(feedback.Message, Does.Contain(expected));
            Assert.That(feedback.ActionLabel, Is.Not.Empty);
            Assert.That(feedback.DiagnosticDetail,
                Is.EqualTo("Developer diagnostic"));
            Assert.That(feedback.Message, Does.Not.Contain("Developer"));
            Assert.That(KoreanTextIntegrity.IsClean(feedback.Message), Is.True);
        }

        [Test]
        public void AnxietyDenial_DoesNotExposeInternalLocationCode()
        {
            SceneTravelResult result = SceneTravelResult.Denied(
                SceneAccessDenialReason.RestrictedByPublicAnxiety,
                "Restricted area 'SERVICE_RAIL' is closed at anxiety 70.");

            InvestigationFeedback feedback =
                InvestigationFeedbackCatalog.ForTravel(result);

            Assert.That(result.IsAllowed, Is.False);
            Assert.That(feedback.Message, Does.Not.Contain("SERVICE_RAIL"));
            Assert.That(feedback.Message, Does.Contain("제한구역"));
        }

        [Test]
        public void PuzzleFeedback_ListsCountsWithoutExposingIds()
        {
            Assert.That(
                ProductionPuzzleCatalog.TryGet(
                    ProductionPuzzleCatalog.CargoRailBranch,
                    out ProductionPuzzleDefinition definition),
                Is.True);
            var result = new PuzzleCompletionResult(
                false,
                new[] { "weight_86kg", "ballast_horizon_route" },
                new[] { "C-08" });

            InvestigationFeedback feedback =
                InvestigationFeedbackCatalog.ForPuzzle(definition, result);

            Assert.That(feedback.Message, Does.Contain("선택 2개"));
            Assert.That(feedback.Message, Does.Contain("필수 증거 1개"));
            Assert.That(feedback.Message, Does.Not.Contain("weight_86kg"));
            Assert.That(feedback.DiagnosticDetail, Does.Contain("weight_86kg"));
            Assert.That(feedback.ActionLabel, Is.EqualTo("증거 보기"));
        }

        [Test]
        public void CompletedPuzzle_UsesPositiveFeedback()
        {
            Assert.That(
                ProductionPuzzleCatalog.TryGet(
                    ProductionPuzzleCatalog.BloodPattern,
                    out ProductionPuzzleDefinition definition),
                Is.True);
            var result = new PuzzleCompletionResult(
                true,
                Array.Empty<string>(),
                Array.Empty<string>());

            InvestigationFeedback feedback =
                InvestigationFeedbackCatalog.ForPuzzle(definition, result);

            Assert.That(feedback.Code, Is.EqualTo("puzzle_completed"));
            Assert.That(feedback.Severity,
                Is.EqualTo(InvestigationFeedbackSeverity.Information));
            Assert.That(feedback.Message, Does.Contain("모두 확인"));
        }

        [Test]
        public void ObjectiveFeedback_ShowsRequirementProgress()
        {
            var host = new UnityEngine.GameObject("FeedbackObjectiveState");
            var state = host.AddComponent<GameStateManager>();
            state.StartNewGame();
            using (var tracker = new InvestigationObjectiveTracker(state))
            {
                InvestigationEventHub.Publish(
                    InvestigationEventKind.EvidenceCollected,
                    "C-03");
                ObjectiveProgress progress =
                    tracker.GetProgress("objective_d2_01_exit_inspection");

                InvestigationFeedback feedback =
                    InvestigationFeedbackCatalog.ForObjective(progress);

                Assert.That(
                    feedback.Message,
                    Does.Contain(progress.Definition.Title));
                Assert.That(feedback.Message, Does.Contain("(1/3)"));
            }

            UnityEngine.Object.DestroyImmediate(host);
        }

        [TestCase("정상적인 한국어 문장", true)]
        [TestCase("깨진 문자 \uFFFD 포함", false)]
        [TestCase("占쏙옙 변환 오류", false)]
        [TestCase("紐⑺몴", false)]
        public void KoreanIntegrity_DetectsKnownCorruption(
            string text,
            bool expectedClean)
        {
            Assert.That(KoreanTextIntegrity.IsClean(text), Is.EqualTo(expectedClean));
        }

        [Test]
        public void CanonicalPlayerText_HasNoKnownMojibake()
        {
            var values = new List<string>();
            values.AddRange(InvestigationObjectiveCatalog.All.Select(item => item.Title));
            foreach (SceneAccessDenialReason reason in
                     Enum.GetValues(typeof(SceneAccessDenialReason)))
            {
                if (reason == SceneAccessDenialReason.None)
                {
                    continue;
                }

                InvestigationFeedback feedback =
                    InvestigationFeedbackCatalog.ForTravel(
                        SceneTravelResult.Denied(reason, "diagnostic"));
                values.Add(feedback.Title);
                values.Add(feedback.Message);
                values.Add(feedback.ActionLabel);
            }

            Assert.That(
                values.Where(value => !KoreanTextIntegrity.IsClean(value)),
                Is.Empty);
        }
    }
}
