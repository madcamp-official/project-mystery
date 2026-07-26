using NUnit.Framework;
using UnityEngine;
using Wake.Core;
using Wake.Puzzles;
using Wake.UI;

namespace Wake.Tests
{
    public class FinalAccusationSessionTests
    {
        private const string SaveKey = "THE_WAKE_GAME_STATE_V1";
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
            Assert.That(result.Messages, Has.Some.EqualTo("범인을 선택하세요."));
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
                DanielTargetBelief.Richard,
                OrpheusEventDesign.InsuranceFraud,
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
                DanielTargetBelief.Richard,
                OrpheusEventDesign.InsuranceFraud,
                true);

            FinalAccusationSubmission result = session.Submit();

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
                DanielTargetBelief.Richard,
                OrpheusEventDesign.InsuranceFraud,
                false);

            FinalAccusationSubmission result = session.Submit();

            Assert.That(result.Messages, Has.Some.Contains("승객 불안 100"));
            Assert.That(result.Result.Ending, Is.EqualTo(FinalEnding.BadPanic));
        }
    }
}
