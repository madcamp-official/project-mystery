using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.Narrative;

namespace Wake.Tests
{
    public class DialogueSupplementalCsvParserTests
    {
        private const string Root =
            "Assets/_Project/Content/Dialogue/Under_the_Horizon_";

        private SupplementalCsvParseResult<ChoiceFlowRecord> choices;
        private SupplementalCsvParseResult<SceneIndexRecord> scenes;

        [OneTimeSetUp]
        public void ParseProductionAssets()
        {
            choices = DialogueSupplementalCsvParser.ParseChoices(
                Load("Choices_KR.csv").text);
            scenes = DialogueSupplementalCsvParser.ParseScenes(
                Load("Scene_Index_KR.csv").text);
        }

        [Test]
        public void ChoiceFlow_PreservesOfficialContract()
        {
            Assert.That(choices.Success, Is.True, string.Join("\n", choices.Errors));
            Assert.That(choices.Records, Has.Count.EqualTo(100));
            Assert.That(
                choices.Records.Select(record => record.ChoiceId).Distinct().Count(),
                Is.EqualTo(100));
            Assert.That(
                choices.Records.Select(record => record.SceneId).Distinct().Count(),
                Is.GreaterThan(20));
            Assert.That(
                choices.Records.All(record =>
                    record.ImplementationStatus == "READY"),
                Is.True);
        }

        [Test]
        public void SceneIndex_PreservesOfficialContract()
        {
            Assert.That(scenes.Success, Is.True, string.Join("\n", scenes.Errors));
            Assert.That(scenes.Records, Has.Count.EqualTo(41));
            Assert.That(
                scenes.Records.Select(record => record.SceneId).Distinct().Count(),
                Is.EqualTo(41));
            Assert.That(
                scenes.Records.Sum(record => record.DialogueLineCount),
                Is.EqualTo(1083));
            Assert.That(
                scenes.Records.Sum(record => record.ChoiceCount),
                Is.EqualTo(100));
        }

        [Test]
        public void FinalAccusation_PreservesSixStageCounts()
        {
            SceneIndexRecord finalAccusation = scenes.Records.Single(record =>
                record.SceneId == "D8-01");

            Assert.That(finalAccusation.DialogueLineCount, Is.EqualTo(106));
            Assert.That(finalAccusation.VoicedLineCount, Is.EqualTo(70));
            Assert.That(finalAccusation.ChoiceCount, Is.EqualTo(26));
            Assert.That(finalAccusation.Objective, Does.Contain("범인"));
        }

        [Test]
        public void DanielTrackingIndex_RecordsAttendantAndBallroomReturn()
        {
            SceneIndexRecord tracking = scenes.Records.Single(record =>
                record.SceneId == "D1-04");

            Assert.That(tracking.Characters, Is.EqualTo("객실 승무원"));
            Assert.That(
                tracking.Objective,
                Does.Contain("볼룸으로 복귀")
                    .And.Contain("행사 운영 계정"));
            Assert.That(tracking.NextScene, Is.EqualTo("D1-05"));
            Assert.That(tracking.DialogueLineCount, Is.EqualTo(22));
            Assert.That(tracking.VoicedLineCount, Is.EqualTo(13));
        }

        [Test]
        public void SupplementalRecords_ReferenceRegisteredScenes()
        {
            var sceneIds = scenes.Records
                .Select(record => record.SceneId)
                .ToHashSet();
            Assert.That(
                choices.Records.All(record => sceneIds.Contains(record.SceneId)),
                Is.True);
        }

        [Test]
        public void SupplementalParser_RejectsMissingHeaderAndDuplicateId()
        {
            string missing =
                "choice_id,scene_id,text_ko,condition,effect,branch_group\n" +
                "C1,P-01,선택,,,GROUP";
            Assert.That(
                DialogueSupplementalCsvParser.ParseChoices(missing).Success,
                Is.False);

            string duplicate =
                string.Join(",", DialogueSupplementalCsvParser.ChoiceHeaders) +
                "\nC1,P-01,선택,,,GROUP,READY" +
                "\nC1,P-01,다른 선택,,,GROUP,READY";
            SupplementalCsvParseResult<ChoiceFlowRecord> parsed =
                DialogueSupplementalCsvParser.ParseChoices(duplicate);
            Assert.That(parsed.Success, Is.False);
            Assert.That(parsed.Errors.Single(error =>
                error.Contains("duplicated")), Does.Contain("C1"));
        }

        private static TextAsset Load(string suffix)
        {
            string path = Root + suffix;
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            Assert.That(asset, Is.Not.Null, $"Missing CSV at {path}");
            return asset;
        }
    }
}
