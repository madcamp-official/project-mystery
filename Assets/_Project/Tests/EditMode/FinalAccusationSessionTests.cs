using NUnit.Framework;
using UnityEngine;
using Wake.Core;
using Wake.Puzzles;
using Wake.UI;

namespace Wake.Tests
{
    public class FinalAccusationSessionTests
    {
        private const string SaveKey = "UNDER_THE_HORIZON_GAME_STATE_V2";
        private GameObject host;
        private GameStateManager state;

        [SetUp]
        public void SetUp()
        {
            if (GameStateManager.Instance != null)
                Object.DestroyImmediate(GameStateManager.Instance.gameObject);
            PlayerPrefs.DeleteKey(SaveKey);
            host = new GameObject("FinalAccusationSessionTests");
            state = host.AddComponent<GameStateManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (GameStateManager.Instance != null)
                Object.DestroyImmediate(GameStateManager.Instance.gameObject);
            PlayerPrefs.DeleteKey(SaveKey);
        }

        [Test]
        public void MissingSelectionsAndDeductions_AreShownBeforeSubmit()
        {
            var session = new FinalAccusationSession(state);
            FinalAccusationSubmission result = session.Submit();

            Assert.That(result.Submitted, Is.False);
            Assert.That(
                result.Messages,
                Has.Some.Contains("Daniel Mercer를 살해한 범인은 누구인가?"));
            Assert.That(result.Messages, Has.Some.StartsWith("핵심 논증이 부족"));
            Assert.That(state.FinalEndingId, Is.Empty);
        }

        [Test]
        public void PartialSelections_AreRestored()
        {
            var session = new FinalAccusationSession(state);
            session.Update(
                AccusedPerson.Evelyn,
                MurderLocation.BallastControlAnnex,
                MurderMethod.NitrogenSuffocation,
                BodyTransport.CeilingServiceRail,
                DanielTargetBelief.Misconception,
                OrpheusEventDesign.Evelyn,
                true);
            state.ReloadSavedState();

            var restored = new FinalAccusationSession(state);
            Assert.That(restored.Accusation.Accused, Is.EqualTo(AccusedPerson.Evelyn));
            Assert.That(restored.Accusation.DiscloseRichardCoverup, Is.True);
        }

        [Test]
        public void CompleteSubmission_DelegatesToResolverAndPersistsEnding()
        {
            foreach (CanonicalDeductionDefinition definition in
                     CanonicalDeductionCatalog.All)
            {
                state.UnlockDeduction(definition.Id);
            }

            var session = new FinalAccusationSession(state);
            session.Update(
                AccusedPerson.Evelyn,
                MurderLocation.BallastControlAnnex,
                MurderMethod.NitrogenSuffocation,
                BodyTransport.CeilingServiceRail,
                DanielTargetBelief.Misconception,
                OrpheusEventDesign.Evelyn,
                true);

            FinalAccusationSubmission result = SubmitAllStages(session);

            Assert.That(result.Submitted, Is.True);
            Assert.That(result.Result.Ending, Is.EqualTo(FinalEnding.Complete));
            Assert.That(state.FinalEndingId, Is.EqualTo(result.Result.EndingId));
        }

        [Test]
        public void ThresholdWarnings_AreKoreanAndStillReachResolver()
        {
            foreach (CanonicalDeductionDefinition definition in
                     CanonicalDeductionCatalog.All)
                state.UnlockDeduction(definition.Id);
            state.ChangePublicAnxiety(85);

            var session = new FinalAccusationSession(state);
            session.Update(
                AccusedPerson.Evelyn,
                MurderLocation.BallastControlAnnex,
                MurderMethod.NitrogenSuffocation,
                BodyTransport.CeilingServiceRail,
                DanielTargetBelief.Misconception,
                OrpheusEventDesign.Evelyn,
                false);

            FinalAccusationSubmission result = SubmitAllStages(session);

            Assert.That(result.Messages, Has.Some.Contains("승객 불안 100"));
            Assert.That(result.Result.Ending, Is.EqualTo(FinalEnding.Bad));
        }

        [Test]
        public void CorrectAnswer_AdvancesOneStageAndPersistsProgress()
        {
            UnlockAllDeductions();
            var session = CreateCorrectSession();

            FinalAccusationSubmission result = session.Submit();

            Assert.That(result.Submitted, Is.False);
            Assert.That(session.CompletedStageCount, Is.EqualTo(1));
            Assert.That(
                session.CurrentStage,
                Is.EqualTo(FinalAccusationStage.MurderLocation));

            state.ReloadSavedState();
            var restored = new FinalAccusationSession(state);
            Assert.That(restored.CompletedStageCount, Is.EqualTo(1));
            Assert.That(
                restored.CurrentStage,
                Is.EqualTo(FinalAccusationStage.MurderLocation));
        }

        [Test]
        public void WrongAnswer_IncrementsStrikeAndKeepsCurrentStage()
        {
            UnlockAllDeductions();
            var session = CreateCorrectSession();
            session.Update(
                AccusedPerson.Claire,
                MurderLocation.BallastControlAnnex,
                MurderMethod.NitrogenSuffocation,
                BodyTransport.CeilingServiceRail,
                DanielTargetBelief.Misconception,
                OrpheusEventDesign.Evelyn,
                true);

            FinalAccusationSubmission result = session.Submit();

            Assert.That(result.Submitted, Is.False);
            Assert.That(session.WrongStrikeCount, Is.EqualTo(1));
            Assert.That(session.CompletedStageCount, Is.Zero);
            Assert.That(session.Accusation.Accused, Is.EqualTo(AccusedPerson.Unknown));
            Assert.That(result.Messages, Has.Some.Contains("오류 1/3"));
        }

        [Test]
        public void ThirdWrongAnswer_CompletesWithWrongPersonEnding()
        {
            UnlockAllDeductions();
            var session = CreateCorrectSession();

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                session.Update(
                    AccusedPerson.Marcus,
                    MurderLocation.BallastControlAnnex,
                    MurderMethod.NitrogenSuffocation,
                    BodyTransport.CeilingServiceRail,
                    DanielTargetBelief.Misconception,
                    OrpheusEventDesign.Evelyn,
                    true);
                FinalAccusationSubmission result = session.Submit();

                if (attempt < 3)
                {
                    Assert.That(result.Submitted, Is.False);
                    continue;
                }

                Assert.That(result.Submitted, Is.True);
                Assert.That(result.Result.Ending, Is.EqualTo(FinalEnding.WrongPerson));
            }

            Assert.That(session.WrongStrikeCount, Is.EqualTo(3));
            Assert.That(session.IsCompleted, Is.True);
        }

        [Test]
        public void SixCorrectStages_WaitForSeparateDisclosureDecision()
        {
            UnlockAllDeductions();
            var session = CreateCorrectSession();
            FinalAccusationSubmission result = default;

            for (int stage = 0;
                 stage < FinalAccusationStageCatalog.All.Count;
                 stage++)
            {
                result = session.Submit();
            }

            Assert.That(result.Submitted, Is.False);
            Assert.That(result.Result, Is.Null);
            Assert.That(session.CompletedStageCount, Is.EqualTo(6));
            Assert.That(session.CurrentStage, Is.Null);
            Assert.That(
                result.Messages,
                Has.Some.Contains("Richard의 은폐 공개 여부"));

            result = session.Submit();
            Assert.That(result.Submitted, Is.True);
            Assert.That(result.Result.Ending, Is.EqualTo(FinalEnding.Complete));
        }

        private FinalAccusationSession CreateCorrectSession()
        {
            var session = new FinalAccusationSession(state);
            session.Update(
                AccusedPerson.Evelyn,
                MurderLocation.BallastControlAnnex,
                MurderMethod.NitrogenSuffocation,
                BodyTransport.CeilingServiceRail,
                DanielTargetBelief.Misconception,
                OrpheusEventDesign.Evelyn,
                true);
            return session;
        }

        private void UnlockAllDeductions()
        {
            foreach (CanonicalDeductionDefinition definition in
                     CanonicalDeductionCatalog.All)
            {
                state.UnlockDeduction(definition.Id);
            }
        }

        private static FinalAccusationSubmission SubmitAllStages(
            FinalAccusationSession session)
        {
            FinalAccusationSubmission result = default;
            for (int stage = 0;
                 stage <= FinalAccusationStageCatalog.All.Count &&
                 !result.Submitted;
                 stage++)
            {
                result = session.Submit();
            }
            return result;
        }
    }
}
