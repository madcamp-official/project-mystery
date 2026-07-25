using System.Linq;
using NUnit.Framework;
using UnityEditor;
using Wake.Narrative;
namespace Wake.Tests
{
    public class DialogueContentValidatorTests
    {
        private const string Path =
            "Assets/_Project/Content/Dialogue/The_Wake_Without_Footprints_Dialogue_KR.csv";
        private string csv;
        [OneTimeSetUp]
        public void LoadCsv()
        {
            csv = AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(Path).text;
        }

        [Test]
        public void ProductionCsv_PassesContentValidation()
        {
            DialogueValidationReport report = DialogueContentValidator.Validate(csv);
            Assert.That(
                report.IsValid,
                Is.True,
                string.Join("\n", report.Diagnostics.Select(item => item.ToString())));
            Assert.That(report.ErrorCount, Is.Zero);
        }

        [Test]
        public void Validator_ReportsMissingHeader()
        {
            DialogueValidationReport report = DialogueContentValidator.Validate(
                csv.Replace("voice_required", "voice"));
            Assert.That(
                report.Diagnostics.Any(item =>
                    item.Code == "HEADER_MISSING" && item.Field == "voice_required"),
                Is.True);
        }

        [Test]
        public void Validator_ReportsVoiceValueWithSourceRow()
        {
            DialogueValidationReport report = DialogueContentValidator.Validate(
                csv.Replace(",N\r\n", ",MAYBE\r\n"));
            DialogueDiagnostic diagnostic = report.Diagnostics.First(item =>
                item.Code == "VOICE_VALUE");
            Assert.That(diagnostic.SourceRow, Is.GreaterThan(1));
            Assert.That(diagnostic.Field, Is.EqualTo("voice_required"));
            DialogueValidationReport vocabulary = DialogueContentValidator.Validate(
                csv.Replace(",ADRIAN,", ",UNKNOWN_SPEAKER,"));
            Assert.That(
                vocabulary.Diagnostics.Any(item => item.Code == "SPEAKER_UNKNOWN"),
                Is.True);
        }

        [Test]
        public void Validator_ReportsBrokenKorean()
        {
            DialogueValidationReport report = DialogueContentValidator.Validate(
                csv.Replace(",calm,", "\uFFFD,calm,"));
            Assert.That(report.Diagnostics.Any(item => item.Code == "TEXT_ENCODING"), Is.True);
        }

        [Test]
        public void Validator_ReportsChoiceGroupContract()
        {
            DialogueValidationReport report = DialogueContentValidator.Validate(
                csv.Replace(",P-01_C1,Daniel", ",BROKEN_CHOICE,Daniel"));
            Assert.That(report.Diagnostics.Any(item => item.Code == "CHOICE_ID"), Is.True);
            Assert.That(report.Diagnostics.Any(item => item.Code == "CHOICE_GROUP_SIZE"), Is.True);
        }

        [Test]
        public void Validator_ReportsMissingConditionScene()
        {
            DialogueValidationReport report = DialogueContentValidator.Validate(
                csv.Replace(",observe,P-01,,,GANGWAY,", ",observe,P-99,,,GANGWAY,"));
            Assert.That(
                report.Diagnostics.Any(item => item.Code == "CONDITION_SCENE_MISSING"),
                Is.True);
        }
    }
}
