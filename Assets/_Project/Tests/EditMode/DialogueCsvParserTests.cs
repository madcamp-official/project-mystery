using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using Wake.Narrative;

namespace Wake.Tests
{
    public class DialogueCsvParserTests
    {
        private const string ProductionCsvPath =
            "Assets/_Project/Content/Dialogue/Under_the_Horizon_Dialogue_KR.csv";

        private DialogueCsvParseResult production;

        [OneTimeSetUp]
        public void ParseProductionAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(ProductionCsvPath);
            Assert.That(asset, Is.Not.Null, $"Missing dialogue CSV at {ProductionCsvPath}");
            production = DialogueCsvParser.Parse(asset.text);
        }

        [Test]
        public void ProductionCsv_PreservesContractTotals()
        {
            Assert.That(production.Success, Is.True, string.Join("\n", production.Errors));
            Assert.That(production.Records.Count, Is.EqualTo(1083));
            Assert.That(
                production.Records.Select(record => record.SceneId).Distinct().Count(),
                Is.EqualTo(41));
            Assert.That(production.Records.Count(record => record.VoiceRequired), Is.EqualTo(677));
        }

        [Test]
        public void ProductionCsv_PreservesAllRequiredHeaders()
        {
            Assert.That(
                production.Headers,
                Is.EquivalentTo(DialogueCsvParser.ProductionHeaders));
        }

        [Test]
        public void StableLineIds_AreUniqueAndNormalized()
        {
            List<string> ids = production.Records.Select(record => record.StableLineId).ToList();
            Assert.That(ids.Count, Is.EqualTo(1083));
            Assert.That(ids.Distinct().Count(), Is.EqualTo(1083));
            Assert.That(ids, Does.Contain("p_01_01"));
            Assert.That(ids, Does.Contain("d1_06_08"));
            Assert.That(
                production.Records.Select(record => record.LineId),
                Does.Contain("P-01_001"));
            Assert.That(
                production.Records.Select(record => record.CanonicalLineId),
                Does.Contain("D8-03_032"));
        }

        [Test]
        public void ProductionCsv_PreservesChoiceRowsAndGroups()
        {
            List<DialogueRecord> choices = production.Records
                .Where(record => record.Speaker == "PLAYER_CHOICE")
                .ToList();

            Assert.That(choices.Count, Is.EqualTo(100));
            Assert.That(
                choices.Select(record => record.BranchGroup)
                    .Where(group => !string.IsNullOrEmpty(group))
                    .Distinct()
                    .Count(),
                Is.EqualTo(38));
            Assert.That(choices, Has.All.Matches<DialogueRecord>(record =>
                !string.IsNullOrWhiteSpace(record.ChoiceId)));
        }

        [Test]
        public void Parser_HandlesQuotedCommasQuotesAndNewlines()
        {
            string csv =
                "scene_id,order,speaker,text_ko,emotion,condition,choice_id,next_or_effect,stage_direction,voice_required\n" +
                "P-99,1,ADRIAN,\"첫 줄, \"\"인용\"\"\n둘째 줄\",calm,,,,PORT,Y";

            DialogueCsvParseResult parsed = DialogueCsvParser.Parse(csv);

            Assert.That(parsed.Success, Is.True, string.Join("\n", parsed.Errors));
            Assert.That(parsed.Records.Count, Is.EqualTo(1));
            Assert.That(parsed.Records[0].TextKo, Is.EqualTo("첫 줄, \"인용\"\n둘째 줄"));
            Assert.That(parsed.Records[0].StableLineId, Is.EqualTo("p_99_01"));
        }

        [Test]
        public void Parser_PreservesEmptyChoiceAndConditionFields()
        {
            DialogueRecord record = production.Records.First(item =>
                string.IsNullOrEmpty(item.Condition) && string.IsNullOrEmpty(item.ChoiceId));

            Assert.That(record.Condition, Is.Empty);
            Assert.That(record.ChoiceId, Is.Empty);
            Assert.That(record.SourceRow, Is.GreaterThan(1));
        }

        [Test]
        public void Parser_PreservesCurrentProductionMetadata()
        {
            DialogueRecord record = production.Records.Single(item =>
                item.LineId == "P-01_001");

            Assert.That(record.Beat, Is.EqualTo("opening"));
            Assert.That(record.LineType, Is.EqualTo("narration"));
            Assert.That(record.Speaker, Is.EqualTo("NARRATION"));
            Assert.That(record.CanonicalLineId, Is.EqualTo("P-01_001"));
        }

        [Test]
        public void Parser_RejectsMissingLineIdInCurrentContract()
        {
            string csv =
                string.Join(",", DialogueCsvParser.ProductionHeaders) + "\n" +
                ",P-01,1,opening,narration,NARRATION,text,observe,,,," +
                "PORT,N,,";

            DialogueCsvParseResult parsed = DialogueCsvParser.Parse(csv);

            Assert.That(parsed.Success, Is.False);
            Assert.That(parsed.Errors.Single(), Does.Contain("line_id"));
            Assert.That(parsed.Records, Is.Empty);
        }

        [Test]
        public void Parser_RejectsNonIntegerOrderWithoutDroppingOtherRows()
        {
            string csv =
                "scene_id,order,speaker,text_ko,emotion,condition,choice_id,next_or_effect,stage_direction,voice_required\n" +
                "P-01,nope,ADRIAN,invalid,calm,,,,PORT,N\n" +
                "P-01,2,ADRIAN,valid,calm,,,,PORT,N";

            DialogueCsvParseResult parsed = DialogueCsvParser.Parse(csv);

            Assert.That(parsed.Success, Is.False);
            Assert.That(parsed.Errors.Single(), Does.Contain("Row 2"));
            Assert.That(parsed.Records.Single().Order, Is.EqualTo(2));
        }

        [Test]
        public void LegacySixColumnCsv_RemainsReadable()
        {
            string csv =
                "line_id,scene_id,speaker_id,text,emotion,voice_required\n" +
                "intro,TEST,CREW,\"comma, safe\",neutral,N";

            DialogueCsvParseResult parsed = DialogueCsvParser.Parse(csv);

            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Records.Single().LineId, Is.EqualTo("intro"));
            Assert.That(parsed.Records.Single().ChoiceId, Is.Empty);
            Assert.That(parsed.Records.Single().TextKo, Is.EqualTo("comma, safe"));
        }
    }
}
