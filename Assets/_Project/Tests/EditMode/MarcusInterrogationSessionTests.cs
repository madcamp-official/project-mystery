using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Wake.Core;
using Wake.Evidence;
using Wake.Puzzles;

namespace Wake.Tests
{
    public class MarcusInterrogationSessionTests
    {
        private const string SaveKey = "THE_WAKE_GAME_STATE_V1";
        private GameObject host;
        private GameStateManager state;
        private EvidenceInventory inventory;

        [SetUp]
        public void SetUp()
        {
            DestroyManager();
            PlayerPrefs.DeleteKey(SaveKey);
            host = new GameObject("MarcusInterrogationSessionTests");
            state = host.AddComponent<GameStateManager>();
            inventory = host.AddComponent<EvidenceInventory>();
            inventory.BindState(state);
        }

        [TearDown]
        public void TearDown()
        {
            DestroyManager();
            PlayerPrefs.DeleteKey(SaveKey);
        }

        [Test]
        public void AskingQuestion_PersistsAnswerAcrossManagerRecreation()
        {
            var session = new MarcusInterrogationSession(state);
            Assert.That(
                session.Ask(
                    MarcusInterrogationCatalog.AuthenticationQuestion,
                    MarcusAnswer.Yes),
                Is.EqualTo(MarcusQuestionResult.Recorded));
            Object.DestroyImmediate(host);
            host = new GameObject("RestoredMarcusInterrogation");
            state = host.AddComponent<GameStateManager>();
            inventory = host.AddComponent<EvidenceInventory>();
            inventory.BindState(state);
            state.ReloadSavedState();
            var restored = new MarcusInterrogationSession(state);

            Assert.That(restored.Answers, Has.Count.EqualTo(1));
            Assert.That(
                restored.ResolveAuthentication(),
                Is.EqualTo(
                    MarcusAuthenticationResult.EvelynAuthenticationConfirmed));
            Assert.That(restored.RemainingQuestions, Is.EqualTo(4));
        }

        [Test]
        public void Session_EnforcesFiveQuestionLimit()
        {
            var questions = new List<MarcusQuestionDefinition>();
            for (int index = 0; index < 6; index++)
            {
                questions.Add(new MarcusQuestionDefinition(
                    $"question_{index}",
                    $"검증 질문 {index}",
                    index == 0));
            }

            var session = new MarcusInterrogationSession(state, questions);
            for (int index = 0; index < 5; index++)
            {
                Assert.That(
                    session.Ask($"question_{index}", MarcusAnswer.No),
                    Is.EqualTo(MarcusQuestionResult.Recorded));
            }

            Assert.That(
                session.Ask("question_5", MarcusAnswer.No),
                Is.EqualTo(MarcusQuestionResult.LimitReached));
            Assert.That(session.RemainingQuestions, Is.Zero);
        }

        [Test]
        public void Completion_RequiresAuthenticationAnswer()
        {
            var session = new MarcusInterrogationSession(state);
            MarcusInterrogationCompletion result = session.Complete();

            Assert.That(result.Completed, Is.False);
            Assert.That(
                result.Authentication,
                Is.EqualTo(MarcusAuthenticationResult.Unresolved));
            Assert.That(result.Message, Does.Contain("먼저 확인"));
            Assert.That(
                state.HasCompletedScene(MarcusInterrogationCatalog.SceneId),
                Is.False);
        }

        [Test]
        public void ConfirmedAuthentication_GrantsC15AndTypedFlag()
        {
            var session = new MarcusInterrogationSession(
                state,
                tryGrantEvidence: id => inventory.TryAddById(id));
            session.Ask(
                MarcusInterrogationCatalog.AuthenticationQuestion,
                MarcusAnswer.Yes);

            MarcusInterrogationCompletion result = session.Complete();

            Assert.That(result.Completed, Is.True);
            Assert.That(
                result.Authentication,
                Is.EqualTo(
                    MarcusAuthenticationResult.EvelynAuthenticationConfirmed));
            Assert.That(
                state.CollectedEvidenceIds,
                Does.Contain(MarcusInterrogationCatalog.AuthenticationEvidence));
            Assert.That(
                state.HasFlag(MarcusInterrogationCatalog.AuthenticationFlag),
                Is.True);
            Assert.That(
                state.HasCompletedScene(MarcusInterrogationCatalog.SceneId),
                Is.True);
        }

        [Test]
        public void ConfirmedAuthentication_AcceptsPreviouslyGrantedC15()
        {
            Assert.That(
                inventory.TryAddById(
                    MarcusInterrogationCatalog.AuthenticationEvidence),
                Is.True);
            int grantAttempts = 0;
            var session = new MarcusInterrogationSession(
                state,
                tryGrantEvidence: _ =>
                {
                    grantAttempts++;
                    return false;
                });
            session.Ask(
                MarcusInterrogationCatalog.AuthenticationQuestion,
                MarcusAnswer.Yes);

            MarcusInterrogationCompletion result = session.Complete();

            Assert.That(result.Completed, Is.True);
            Assert.That(grantAttempts, Is.Zero);
            Assert.That(
                state.HasCompletedScene(MarcusInterrogationCatalog.SceneId),
                Is.True);
        }

        [Test]
        public void ConfirmedAuthentication_WaitsWhenEvidenceInventoryRejectsC15()
        {
            var session = new MarcusInterrogationSession(
                state,
                tryGrantEvidence: _ => false);
            session.Ask(
                MarcusInterrogationCatalog.AuthenticationQuestion,
                MarcusAnswer.Yes);

            MarcusInterrogationCompletion result = session.Complete();

            Assert.That(result.Completed, Is.False);
            Assert.That(result.Message, Does.Contain("C-15"));
            Assert.That(session.IsCompleted, Is.False);
            Assert.That(
                state.HasCompletedScene(MarcusInterrogationCatalog.SceneId),
                Is.False);
        }

        [Test]
        public void Validator_ReportsEmptyDuplicateAndUndefinedContracts()
        {
            var invalid = new MarcusQuestionDefinition[]
            {
                null,
                new("duplicate", string.Empty, true),
                new("duplicate", "중복 질문", true),
                new("extra_1", "질문"),
                new("extra_2", "질문"),
                new("extra_3", "질문")
            };

            IReadOnlyList<string> warnings =
                MarcusInterrogationValidator.Validate(invalid);

            Assert.That(warnings, Has.Some.Contains("ID가 비어"));
            Assert.That(warnings, Has.Some.Contains("문구가 비어"));
            Assert.That(warnings, Has.Some.Contains("중복"));
            Assert.That(warnings, Has.Some.Contains("정확히 1개"));
            Assert.That(warnings, Has.Some.Contains("최대 5개"));
        }

        private void DestroyManager()
        {
            if (GameStateManager.Instance != null)
            {
                Object.DestroyImmediate(GameStateManager.Instance.gameObject);
            }
            else if (host != null)
            {
                Object.DestroyImmediate(host);
            }

            host = null;
            inventory = null;
        }
    }
}
