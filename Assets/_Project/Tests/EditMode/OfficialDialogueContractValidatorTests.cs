using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.Narrative;

namespace Wake.Tests
{
    public class OfficialDialogueContractValidatorTests
    {
        private const string Root =
            "Assets/_Project/Content/Dialogue/Under_the_Horizon_";
        private string dialogue;
        private string choices;
        private string scenes;

        [OneTimeSetUp]
        public void LoadOfficialExports()
        {
            dialogue = Load("Dialogue_KR.csv");
            choices = Load("Choices_KR.csv");
            scenes = Load("Scene_Index_KR.csv");
        }

        [Test]
        public void OfficialExports_PassCrossSheetContract()
        {
            OfficialDialogueContractReport report =
                OfficialDialogueContractValidator.Validate(
                    dialogue, choices, scenes);

            Assert.That(
                report.IsValid,
                Is.True,
                string.Join("\n", report.Errors));
            Assert.That(report.Errors, Is.Empty);
        }

        [Test]
        public void Validator_DetectsChoiceTextDrift()
        {
            string changed = choices.Replace(
                "그의 경고를 진지하게 듣기",
                "변경된 선택지");

            OfficialDialogueContractReport report =
                OfficialDialogueContractValidator.Validate(
                    dialogue, changed, scenes);

            Assert.That(
                report.Errors.Any(error =>
                    error.Contains("P-01_C1") &&
                    error.Contains("text_ko")),
                Is.True);
        }

        [Test]
        public void Validator_DetectsSceneCountDrift()
        {
            string changed = scenes.Replace(
                "P-01,프롤로그,항구의 기자,15:10,PORT,관찰 튜토리얼 후 Daniel과 대화,없음,P-02,Daniel,구겨진 초대장; 암호화 메신저 알림,경고를 진지하게 듣기 / 농담으로 넘기기,26,17,2,0,COMPLETE",
                "P-01,프롤로그,항구의 기자,15:10,PORT,관찰 튜토리얼 후 Daniel과 대화,없음,P-02,Daniel,구겨진 초대장; 암호화 메신저 알림,경고를 진지하게 듣기 / 농담으로 넘기기,25,17,2,0,COMPLETE");

            OfficialDialogueContractReport report =
                OfficialDialogueContractValidator.Validate(
                    dialogue, choices, changed);

            Assert.That(
                report.Errors.Any(error =>
                    error.Contains("P-01") &&
                    error.Contains("dialogue_line_count")),
                Is.True);
        }

        [Test]
        public void Validator_DetectsUnknownNextScene()
        {
            string changed = scenes.Replace(
                ",P-02,Daniel,",
                ",D9-99,Daniel,");

            OfficialDialogueContractReport report =
                OfficialDialogueContractValidator.Validate(
                    dialogue, choices, changed);

            Assert.That(
                report.Errors.Any(error =>
                    error.Contains("D9-99") &&
                    error.Contains("unknown scene")),
                Is.True);
        }

        [Test]
        public void Validator_DetectsMissingEndingMarker()
        {
            string changed = dialogue.Replace(
                "ending:C_complete",
                "ending:B_complete");

            OfficialDialogueContractReport report =
                OfficialDialogueContractValidator.Validate(
                    changed, choices, scenes);

            Assert.That(
                report.Errors.Any(error =>
                    error.Contains("ending markers") ||
                    error.Contains("Ending markers")),
                Is.True);
        }

        [Test]
        public void Validator_DetectsNonReadyChoice()
        {
            string changed = choices.Replace(
                "P-01_C1,P-01,그의 경고를 진지하게 듣기,,trust_daniel:+1; flag:daniel_warning_taken,P-01_WARN,READY",
                "P-01_C1,P-01,그의 경고를 진지하게 듣기,,trust_daniel:+1; flag:daniel_warning_taken,P-01_WARN,TODO");

            OfficialDialogueContractReport report =
                OfficialDialogueContractValidator.Validate(
                    dialogue, changed, scenes);

            Assert.That(
                report.Errors.Any(error =>
                    error.Contains("P-01_C1") &&
                    error.Contains("implementation_status")),
                Is.True);
        }

        [Test]
        public void Validator_DetectsUnknownSceneUnlockEffect()
        {
            string changed = dialogue.Replace(
                "scene_unlock:D7-04",
                "scene_unlock:D9-99");

            OfficialDialogueContractReport report =
                OfficialDialogueContractValidator.Validate(
                    changed, choices, scenes);

            Assert.That(
                report.Errors.Any(error =>
                    error.Contains("D9-99") &&
                    error.Contains("unlocks unknown scene")),
                Is.True);
        }

        [Test]
        public void Validator_DetectsDeclaredTransitionWithoutUnlock()
        {
            string changed = dialogue.Replace(
                "scene_unlock:D3-02",
                "flag:d3_02_route_removed");

            OfficialDialogueContractReport report =
                OfficialDialogueContractValidator.Validate(
                    changed, choices, scenes);

            Assert.That(
                report.Errors.Any(error =>
                    error.Contains("D3-01") &&
                    error.Contains("D3-02") &&
                    error.Contains("without a matching scene_unlock")),
                Is.True);
        }

        private static string Load(string suffix)
        {
            string path = Root + suffix;
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            Assert.That(asset, Is.Not.Null, $"Missing CSV at {path}");
            return asset.text;
        }
    }
}
