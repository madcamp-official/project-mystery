using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Wake.Core;
using Wake.Narrative;

namespace Wake.Tests
{
    public sealed class ProductionConditionEvaluatorTests
    {
        private const string SaveKey = "UNDER_THE_HORIZON_GAME_STATE_V2";
        private GameObject host;
        private GameStateManager state;
        private ProductionConditionEvaluator evaluator;

        [SetUp]
        public void SetUp()
        {
            DestroyManager();
            PlayerPrefs.DeleteKey(SaveKey);
            host = new GameObject("ProductionConditionEvaluatorTests");
            state = host.AddComponent<GameStateManager>();
            state.StartNewGame();
            evaluator = new ProductionConditionEvaluator(state);
        }

        [TearDown]
        public void TearDown()
        {
            DestroyManager();
            PlayerPrefs.DeleteKey(SaveKey);
        }

        [Test]
        public void ChoiceAndFlagConditions_UsePersistedFlags()
        {
            state.AddFlag(ProductionConditionEvaluator.ChoiceFlag("D1-02_C1"));
            state.AddFlag("camera_only_complete");

            Assert.That(evaluator.Evaluate("choice(D1-02_C1)").IsMet, Is.True);
            Assert.That(evaluator.Evaluate("choice(D1-02_C2)").IsMet, Is.False);
            Assert.That(evaluator.Evaluate("flag(camera_only_complete)").IsMet, Is.True);
            Assert.That(evaluator.Evaluate("not flag(missing)").IsMet, Is.True);
        }

        [Test]
        public void AllFlags_RequiresEveryNamedFlag()
        {
            state.AddFlag("met_claire");
            state.AddFlag("met_marcus");
            state.AddFlag("met_helena");

            const string condition =
                "all_flags(met_claire,met_marcus,met_helena,met_owen)";
            Assert.That(evaluator.Evaluate(condition).IsMet, Is.False);

            state.AddFlag("met_owen");
            Assert.That(evaluator.Evaluate(condition).IsMet, Is.True);
        }

        [Test]
        public void NumericComparisons_ReadTrustMetersAndCounters()
        {
            state.ChangeTrust("RICHARD", 1);
            state.ChangePublicAnxiety(85);
            state.ChangeEvidenceIntegrity(-100);
            state.ChangeRuntimeCounter("hostility_claire", 2);
            state.ChangeRuntimeCounter("wrong_strike", 3);

            Assert.That(evaluator.Evaluate("trust_richard>=3").IsMet, Is.True);
            Assert.That(evaluator.Evaluate("trust_richard<3").IsMet, Is.False);
            Assert.That(evaluator.Evaluate("publicAnxiety>=100").IsMet, Is.True);
            Assert.That(evaluator.Evaluate("evidenceIntegrity<=0").IsMet, Is.True);
            Assert.That(evaluator.Evaluate("hostility_claire>1").IsMet, Is.True);
            Assert.That(evaluator.Evaluate("wrong_strike>=3").IsMet, Is.True);
        }

        [Test]
        public void MetadataAndEndingConditions_ReadEffectResults()
        {
            state.AddFlag("accusation1_correct");
            state.AddFlag("audio_step_3");
            state.TryRecordFinalEnding("B");

            Assert.That(evaluator.Evaluate("accusation1:correct").IsMet, Is.True);
            Assert.That(evaluator.Evaluate("audio_step=3").IsMet, Is.True);
            Assert.That(evaluator.Evaluate("ending:B").IsMet, Is.True);
            Assert.That(evaluator.Evaluate("ending:A").IsMet, Is.False);
        }

        [Test]
        public void BooleanOperators_RespectParenthesesAndAndPrecedence()
        {
            state.AddFlag("camera_only_complete");
            state.AddFlag("accusation1_wrong_richard");
            state.ChangeRuntimeCounter("wrong_strike", 2);

            Assert.That(
                evaluator.Evaluate(
                    "choice(D2-04_C2) or flag(camera_only_complete)").IsMet,
                Is.True);
            Assert.That(
                evaluator.Evaluate(
                    "wrong_strike>=3 or ending_candidate:C and " +
                    "accusation1:wrong_richard").IsMet,
                Is.False);

            state.AddFlag("ending_candidate_C");
            Assert.That(
                evaluator.Evaluate(
                    "wrong_strike>=3 or ending_candidate:C and " +
                    "accusation1:wrong_richard").IsMet,
                Is.True);
        }

        [Test]
        public void AllAccusationsCorrect_RequiresSixStageMarkers()
        {
            for (int index = 1; index <= 5; index++)
            {
                state.AddFlag($"accusation{index}_correct");
            }
            Assert.That(evaluator.Evaluate("all_accusations_correct").IsMet, Is.False);

            state.AddFlag("accusation6_correct");
            Assert.That(evaluator.Evaluate("all_accusations_correct").IsMet, Is.True);
        }

        [Test]
        public void CompletedSceneCondition_ReadsProductionProgress()
        {
            Assert.That(evaluator.Evaluate("D3-04").IsMet, Is.False);
            state.RecordCompletedScene("D3-04");
            Assert.That(evaluator.Evaluate("D3-04").IsMet, Is.True);
        }

        [Test]
        public void DialogueFlow_PresentsOnlySelectedChoiceResponse()
        {
            var records = new[]
            {
                Record(1, "NARRATION", "intro"),
                Record(2, "PLAYER_CHOICE", "first", choiceId: "TEST_C1"),
                Record(3, "PLAYER_CHOICE", "second", choiceId: "TEST_C2"),
                Record(4, "CLAIRE", "first result", "choice(TEST_C1)"),
                Record(5, "CLAIRE", "second result", "choice(TEST_C2)"),
                Record(6, "SYSTEM", "close")
            };
            var flow = new ProductionDialogueFlow(records, new HashSet<string>(), state);

            Assert.That(flow.StartScene("TEST"), Is.True);
            flow.Advance();
            Assert.That(flow.IsAwaitingChoice, Is.True);
            Assert.That(flow.SelectChoice(1), Is.True);

            Assert.That(flow.Current.TextKo, Is.EqualTo("second result"));
            Assert.That(state.HasFlag("choice_test_c2"), Is.True);
            Assert.That(state.HasFlag("choice_test_c1"), Is.False);
        }

        private static DialogueRecord Record(
            int order,
            string speaker,
            string text,
            string condition = "",
            string choiceId = "")
        {
            return new DialogueRecord(
                "TEST",
                order,
                speaker,
                text,
                "neutral",
                condition,
                choiceId,
                "",
                "",
                "N",
                false,
                order + 1);
        }

        private void DestroyManager()
        {
            if (host != null)
            {
                Object.DestroyImmediate(host);
            }
            if (GameStateManager.Instance != null)
            {
                Object.DestroyImmediate(GameStateManager.Instance.gameObject);
            }
            host = null;
            state = null;
        }
    }
}
