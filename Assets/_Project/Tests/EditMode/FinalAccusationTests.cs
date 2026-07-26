using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.Core;
using Wake.Narrative;
using Wake.Puzzles;

namespace Wake.Tests
{
    public class FinalAccusationTests
    {
        private const string SaveKey = "THE_WAKE_GAME_STATE_V1";
        private const string DialoguePath =
            "Assets/_Project/Content/Dialogue/The_Wake_Without_Footprints_Dialogue_KR.csv";

        private GameObject host;
        private GameStateManager state;
        private FinalAccusationResolver resolver;

        [SetUp]
        public void SetUp()
        {
            DestroyExistingManager();
            PlayerPrefs.DeleteKey(SaveKey);
            host = new GameObject("FinalAccusationTests");
            state = host.AddComponent<GameStateManager>();
            state.StartNewGame();
            resolver = new FinalAccusationResolver(state);
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
        public void CompleteEnding_RequiresAllAnswersCrimeCaseAndCoverup()
        {
            UnlockCrimeCase(includePastEvent: true);

            FinalAccusationResult result = resolver.Resolve(
                CreateCorrectAccusation(discloseCoverup: true));

            Assert.That(result.Ending, Is.EqualTo(FinalEnding.Complete));
            Assert.That(result.EndingId, Is.EqualTo("ending_a_complete"));
            Assert.That(result.WasRecorded, Is.True);
            Assert.That(state.FinalEndingId, Is.EqualTo(result.EndingId));
        }

        [Test]
        public void ConvenientEnding_SolvesMurderWithoutDisclosingCoverup()
        {
            UnlockCrimeCase(includePastEvent: true);

            FinalAccusationResult result = resolver.Resolve(
                CreateCorrectAccusation(discloseCoverup: false));

            Assert.That(result.Ending, Is.EqualTo(FinalEnding.ConvenientCulprit));
            Assert.That(result.EndingId, Is.EqualTo("ending_b_convenient_culprit"));
        }

        [Test]
        public void WrongPersonEnding_WhenAnyTypedAnswerIsWrong()
        {
            UnlockCrimeCase(includePastEvent: true);
            FinalAccusation accusation = CreateCorrectAccusation(true);
            accusation.Accused = AccusedPerson.Richard;

            FinalAccusationResult result = resolver.Resolve(accusation);

            Assert.That(result.Ending, Is.EqualTo(FinalEnding.WrongPerson));
            Assert.That(result.EndingId, Is.EqualTo("ending_c_wrong_person"));
        }

        [Test]
        public void WrongPersonEnding_WhenCoreDeductionIsMissing()
        {
            foreach (string deductionId in new[]
                     {
                         CanonicalDeductionCatalog.SceneDenial,
                         CanonicalDeductionCatalog.BodyInsertion,
                         CanonicalDeductionCatalog.TransportRoute,
                         CanonicalDeductionCatalog.ActualMurder
                     })
            {
                state.UnlockDeduction(deductionId);
            }

            FinalAccusationResult result =
                resolver.Resolve(CreateCorrectAccusation(false));

            Assert.That(result.Ending, Is.EqualTo(FinalEnding.WrongPerson));
            Assert.That(state.HasUnlockedDeduction("culprit_link"), Is.False);
        }

        [Test]
        public void PanicBadEnd_PreemptsOtherwiseCompleteAccusation()
        {
            UnlockCrimeCase(includePastEvent: true);
            state.ChangePublicAnxiety(85);

            FinalAccusationResult result =
                resolver.Resolve(CreateCorrectAccusation(true));

            Assert.That(result.Ending, Is.EqualTo(FinalEnding.BadPanic));
            Assert.That(result.EndingId, Is.EqualTo("ending_bad_panic"));
        }

        [Test]
        public void IntegrityBadEnd_PreemptsOtherwiseCompleteAccusation()
        {
            UnlockCrimeCase(includePastEvent: true);
            state.ChangeEvidenceIntegrity(-100);

            FinalAccusationResult result =
                resolver.Resolve(CreateCorrectAccusation(true));

            Assert.That(result.Ending, Is.EqualTo(FinalEnding.BadIntegrity));
            Assert.That(result.EndingId, Is.EqualTo("ending_bad_integrity"));
        }

        [Test]
        public void Ending_IsPersistedAndCannotBeOverwritten()
        {
            UnlockCrimeCase(includePastEvent: true);
            FinalAccusationResult first =
                resolver.Resolve(CreateCorrectAccusation(true));

            Object.DestroyImmediate(host);
            host = new GameObject("RestoredFinalEnding");
            state = host.AddComponent<GameStateManager>();
            state.ReloadSavedState();
            resolver = new FinalAccusationResolver(state);
            FinalAccusationResult repeated =
                resolver.Resolve(new FinalAccusation { Accused = AccusedPerson.Richard });

            Assert.That(state.FinalEndingId, Is.EqualTo(first.EndingId));
            Assert.That(repeated.Ending, Is.EqualTo(FinalEnding.Complete));
            Assert.That(repeated.EndingId, Is.EqualTo(first.EndingId));
            Assert.That(repeated.WasRecorded, Is.False);
        }

        [TestCase("ending_a_complete", true)]
        [TestCase("ending_b_convenient_culprit", true)]
        [TestCase("ending_c_wrong_person", false)]
        [TestCase("ending_bad_panic", false)]
        [TestCase("ending_bad_integrity", false)]
        public void D8ConfessionGate_OnlyAcceptsAAndB(string endingId, bool expected)
        {
            Assert.That(
                FinalAccusationResolver.OpensD8Confession(endingId),
                Is.EqualTo(expected));
        }

        [Test]
        public void ProductionFlow_OpensD802AfterCorrectEnding()
        {
            UnlockCrimeCase(includePastEvent: false);
            resolver.Resolve(CreateCorrectAccusation(false));
            ProductionDialogueFlow flow = CreateProductionFlow();

            Assert.That(flow.GetMissingPrerequisites("D8-02"), Is.Empty);
            Assert.That(flow.CanStartScene("D8-02"), Is.True);
            Assert.That(flow.StartScene("D8-02"), Is.True);
        }

        [Test]
        public void ProductionFlow_KeepsD802ClosedAfterWrongEnding()
        {
            resolver.Resolve(new FinalAccusation { Accused = AccusedPerson.Richard });
            ProductionDialogueFlow flow = CreateProductionFlow();

            Assert.That(
                flow.GetMissingPrerequisites("D8-02"),
                Is.EqualTo(new[] { "D8-01 정답" }));
            Assert.That(flow.CanStartScene("D8-02"), Is.False);
        }

        [TestCase("ending_a_complete", "D8-02")]
        [TestCase("ending_b_convenient_culprit", "D8-02")]
        [TestCase("ending_c_wrong_person", "")]
        [TestCase("ending_bad_panic", "")]
        [TestCase("ending_bad_integrity", "")]
        public void EndingCatalog_RoutesOnlySolvedMurderToConfession(
            string endingId,
            string expected)
        {
            Assert.That(
                ProductionEndingCatalog.GetNextDialogueScene(
                    endingId,
                    false,
                    false),
                Is.EqualTo(expected));
        }

        [Test]
        public void EndingCatalog_RoutesConfessionToEpilogueThenStops()
        {
            Assert.That(
                ProductionEndingCatalog.GetNextDialogueScene(
                    FinalAccusationResolver.CompleteEndingId,
                    true,
                    false),
                Is.EqualTo("D8-03"));
            Assert.That(
                ProductionEndingCatalog.GetNextDialogueScene(
                    FinalAccusationResolver.CompleteEndingId,
                    true,
                    true),
                Is.Empty);
        }

        [Test]
        public void EndingCatalog_ProvidesKoreanSummaryForEveryStoredEnding()
        {
            foreach (ProductionEndingDefinition ending in
                     ProductionEndingCatalog.All)
            {
                Assert.That(ending.EndingId, Is.Not.Empty);
                Assert.That(ending.RouteLabel, Does.Contain("엔딩"));
                Assert.That(ending.Title, Is.Not.Empty);
                Assert.That(ending.Epilogue, Is.Not.Empty);
            }
        }

        private ProductionDialogueFlow CreateProductionFlow()
        {
            TextAsset csv = AssetDatabase.LoadAssetAtPath<TextAsset>(DialoguePath);
            var completed = new HashSet<string> { "D7-04", "D8-01" };
            return new ProductionDialogueFlow(
                DialogueCsvParser.Parse(csv.text).Records,
                completed,
                state);
        }

        private void UnlockCrimeCase(bool includePastEvent)
        {
            foreach (CanonicalDeductionDefinition definition in
                     CanonicalDeductionCatalog.All)
            {
                if (includePastEvent ||
                    definition.Id != CanonicalDeductionCatalog.PastEvent)
                {
                    state.UnlockDeduction(definition.Id);
                }
            }
        }

        private static FinalAccusation CreateCorrectAccusation(bool discloseCoverup)
        {
            return new FinalAccusation
            {
                Accused = AccusedPerson.Evelyn,
                Location = MurderLocation.BallastControlAnnex,
                Method = MurderMethod.NitrogenSuffocation,
                Transport = BodyTransport.CeilingServiceRail,
                DanielBelievedTarget = DanielTargetBelief.Richard,
                OrpheusDesign = OrpheusEventDesign.InsuranceFraud,
                DiscloseRichardCoverup = discloseCoverup
            };
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
