using System.Linq;
using NUnit.Framework;
using UnityEditor;
using Wake.Narrative;
namespace Wake.Tests
{
    public class DialogueContentValidatorTests
    {
        private const string Path =
            "Assets/_Project/Content/Dialogue/Under_the_Horizon_Dialogue_KR.csv";
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
                csv.Replace(",P-01_C1,", ",BROKEN_CHOICE,"));
            Assert.That(report.Diagnostics.Any(item => item.Code == "CHOICE_ID"), Is.True);
        }

        [Test]
        public void Validator_ReportsMissingConditionScene()
        {
            DialogueValidationReport report = DialogueContentValidator.Validate(
                csv.Replace("choice(P-01_C1)", "choice(P-99_C1)"));
            Assert.That(
                report.Diagnostics.Any(item => item.Code == "CONDITION_SCENE_MISSING"),
                Is.True);
        }

        [Test]
        public void Validator_ReportsMissingBranchGroup()
        {
            DialogueValidationReport report = DialogueContentValidator.Validate(
                csv.Replace(",P-01_WARN,", ",,"));
            Assert.That(
                report.Diagnostics.Any(item =>
                    item.Code == "BRANCH_GROUP_REQUIRED"),
                Is.True);
        }

        [Test]
        public void Validator_ReportsUnknownLineType()
        {
            DialogueValidationReport report = DialogueContentValidator.Validate(
                csv.Replace(",narration,NARRATION,", ",unknown_type,NARRATION,"));
            Assert.That(
                report.Diagnostics.Any(item =>
                    item.Code == "LINE_TYPE_UNKNOWN"),
                Is.True);
        }

        [Test]
        public void Validator_ReportsInvalidOfficialEffectDsl()
        {
            DialogueValidationReport report = DialogueContentValidator.Validate(
                csv.Replace("evidence:C-01", "evidence:"));

            DialogueDiagnostic diagnostic = report.Diagnostics.Single(item =>
                item.Code == "EFFECT_INVALID");
            Assert.That(diagnostic.Field, Is.EqualTo("next_or_effect"));
            Assert.That(diagnostic.SourceRow, Is.GreaterThan(1));
            Assert.That(diagnostic.Message, Does.Contain("no value"));
        }
    }
}
