using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.Narrative;
using Wake.Puzzles;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class MarcusQuestionContractTests
    {
        private const string DialoguePath =
            "Assets/_Project/Content/Dialogue/Under_the_Horizon_Dialogue_KR.csv";

        private MarcusQuestionDefinition[] questions;

        [OneTimeSetUp]
        public void LoadQuestions()
        {
            TextAsset asset =
                AssetDatabase.LoadAssetAtPath<TextAsset>(DialoguePath);
            Assert.That(asset, Is.Not.Null, DialoguePath);
            DialogueCsvParseResult parsed =
                DialogueCsvParser.Parse(asset.text);
            Assert.That(parsed.Success, Is.True, string.Join("\n", parsed.Errors));
            questions = MarcusInterrogationCatalog
                .Create(parsed.Records)
                .ToArray();
        }

        [Test]
        public void OfficialD404Source_ProvidesEightOrderedCandidates()
        {
            Assert.That(
                questions,
                Has.Length.EqualTo(
                    MarcusInterrogationCatalog.OfficialQuestionCount));
            Assert.That(
                questions.Select(item => item.Prompt),
                Is.EqualTo(new[]
                {
                    "이블린이 다니엘을 죽였습니까?",
                    "당신은 이블린에게 인증 토큰을 제공했습니까?",
                    "이블린이 당신에게 돈을 약속했습니까?",
                    "당신이 금고의 모듈을 덮어썼습니까?",
                    "이블린이 금고에 들어가는 것을 직접 봤습니까?",
                    "토큰을 준 시각은 21시 이전입니까?",
                    "추락 전에 이블린이 당신을 위협했습니까?",
                    "리처드가 토큰 제공을 지시했습니까?"
                }));
        }

        [Test]
        public void EvidenceEffect_IdentifiesSingleAuthenticationQuestion()
        {
            MarcusQuestionDefinition authentication =
                questions.Single(item =>
                    item.ConfirmsEvelynAuthentication);

            Assert.That(
                authentication.Id,
                Is.EqualTo(
                    MarcusInterrogationCatalog.AuthenticationQuestion));
            Assert.That(authentication.Prompt, Does.Contain("인증 토큰"));
            Assert.That(
                questions.Count(item =>
                    item.ConfirmsEvelynAuthentication),
                Is.EqualTo(1));
        }

        [Test]
        public void OfficialQuestions_PassCandidateContractValidation()
        {
            Assert.That(
                MarcusInterrogationValidator.Validate(questions),
                Is.Empty);
            Assert.That(
                MarcusInterrogationSession.MaximumQuestions,
                Is.EqualTo(5));
        }

        [Test]
        public void Catalog_IgnoresChoicesFromOtherScenesAndGroups()
        {
            var unrelated = new DialogueRecord(
                "D4-04_999",
                "D4-04",
                999,
                "fixture",
                "choice",
                "PLAYER_CHOICE",
                "포함되면 안 되는 질문",
                string.Empty,
                string.Empty,
                "OTHER_Q1",
                "question_used",
                string.Empty,
                "N",
                "OTHER_GROUP",
                string.Empty,
                false,
                999);

            MarcusQuestionDefinition[] filtered =
                MarcusInterrogationCatalog.Create(
                        Array.Empty<DialogueRecord>()
                            .Append(unrelated))
                    .ToArray();

            Assert.That(filtered, Is.Empty);
        }

        [Test]
        public void EightCandidates_UseTwoColumnsAndFourRows()
        {
            MarcusQuestionGridCell[] cells =
                Enumerable.Range(0, 8)
                    .Select(index =>
                        MarcusQuestionGridLayout.Calculate(index, 8))
                    .ToArray();

            Assert.That(
                cells.Select(cell => cell.AnchorMin.x).Distinct().Count(),
                Is.EqualTo(2));
            Assert.That(
                cells.Select(cell => cell.AnchorMin.y).Distinct().Count(),
                Is.EqualTo(4));
            Assert.That(cells.All(cell =>
                cell.AnchorMin.x >= MarcusQuestionGridLayout.Left &&
                cell.AnchorMax.x <= MarcusQuestionGridLayout.Right &&
                cell.AnchorMin.y >= MarcusQuestionGridLayout.Bottom &&
                cell.AnchorMax.y <= MarcusQuestionGridLayout.Top), Is.True);
        }

        [Test]
        public void GridCells_DoNotOverlapAndKeepReadableHeight()
        {
            MarcusQuestionGridCell[] cells =
                Enumerable.Range(0, 8)
                    .Select(index =>
                        MarcusQuestionGridLayout.Calculate(index, 8))
                    .ToArray();

            Assert.That(cells.All(cell => cell.Height > 0.07f), Is.True);
            Assert.That(cells.All(cell => cell.Width > 0.4f), Is.True);
            for (int left = 0; left < cells.Length; left++)
            {
                for (int right = left + 1;
                     right < cells.Length;
                     right++)
                {
                    Assert.That(
                        Overlaps(cells[left], cells[right]),
                        Is.False,
                        $"Grid cells overlap: {left}, {right}");
                }
            }
        }

        [Test]
        public void Grid_RejectsOutOfRangeIndices()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                MarcusQuestionGridLayout.Calculate(-1, 8));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                MarcusQuestionGridLayout.Calculate(8, 8));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                MarcusQuestionGridLayout.Calculate(0, 0));
        }

        private static bool Overlaps(
            MarcusQuestionGridCell left,
            MarcusQuestionGridCell right)
        {
            return left.AnchorMin.x < right.AnchorMax.x &&
                   left.AnchorMax.x > right.AnchorMin.x &&
                   left.AnchorMin.y < right.AnchorMax.y &&
                   left.AnchorMax.y > right.AnchorMin.y;
        }
    }
}
