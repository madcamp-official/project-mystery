using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Wake.Narrative;

namespace Wake.Tests
{
    public class DialogueDatabaseContractTests
    {
        private const string ProductionPath =
            "Assets/_Project/Content/Dialogue/" +
            "Under_the_Horizon_Dialogue_KR.csv";

        private TextAsset production;

        [OneTimeSetUp]
        public void LoadProductionAsset()
        {
            production = AssetDatabase.LoadAssetAtPath<TextAsset>(
                ProductionPath);
            Assert.That(
                production,
                Is.Not.Null,
                $"Missing dialogue CSV at {ProductionPath}");
        }

        [Test]
        public void Database_LoadsCurrentContractWithoutLosingRecords()
        {
            var gameObject = new GameObject("Dialogue Database Test");
            DialogueDatabase database =
                gameObject.AddComponent<DialogueDatabase>();

            try
            {
                Assert.That(
                    database.LoadFromText(production.text),
                    Is.True,
                    string.Join("\n", database.LoadErrors));
                Assert.That(database.RecordCount, Is.EqualTo(1083));
                Assert.That(database.SceneCount, Is.EqualTo(41));
                Assert.That(database.LoadErrors, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Database_ResolvesOfficialAndCompatibilityLineIds()
        {
            var gameObject = new GameObject("Dialogue Alias Test");
            DialogueDatabase database =
                gameObject.AddComponent<DialogueDatabase>();

            try
            {
                Assert.That(database.LoadFromText(production.text), Is.True);
                Assert.That(
                    database.TryGetRecord(
                        "P-01_001",
                        out DialogueRecord official),
                    Is.True);
                Assert.That(
                    database.TryGetRecord(
                        "p_01_01",
                        out DialogueRecord compatibility),
                    Is.True);
                Assert.That(official, Is.SameAs(compatibility));
                Assert.That(official.LineId, Is.EqualTo("P-01_001"));
                Assert.That(official.StableLineId, Is.EqualTo("p_01_01"));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CurrentContract_PreservesAllMetadataColumns()
        {
            DialogueCsvParseResult parsed =
                DialogueCsvParser.Parse(production.text);
            DialogueRecord choice = parsed.Records.Single(record =>
                record.LineId == "P-01_020");

            Assert.That(choice.Beat, Is.EqualTo("choice"));
            Assert.That(choice.LineType, Is.EqualTo("choice"));
            Assert.That(choice.ChoiceId, Is.EqualTo("P-01_C1"));
            Assert.That(choice.BranchGroup, Is.EqualTo("P-01_WARN"));
            Assert.That(
                choice.ImplementationNote,
                Is.EqualTo(string.Empty));
        }

        [Test]
        public void LegacyProductionContract_RemainsReadable()
        {
            const string csv =
                "scene_id,order,speaker,text_ko,emotion,condition," +
                "choice_id,next_or_effect,stage_direction,voice_required\n" +
                "P-01,1,ADRIAN,legacy text,calm,,,flag:test,PORT,Y";

            DialogueCsvParseResult parsed = DialogueCsvParser.Parse(csv);

            Assert.That(
                parsed.Success,
                Is.True,
                string.Join("\n", parsed.Errors));
            DialogueRecord record = parsed.Records.Single();
            Assert.That(record.LineId, Is.Empty);
            Assert.That(record.Beat, Is.Empty);
            Assert.That(record.LineType, Is.Empty);
            Assert.That(record.BranchGroup, Is.Empty);
            Assert.That(record.CanonicalLineId, Is.EqualTo("p_01_01"));
            Assert.That(record.VoiceRequired, Is.True);
        }

        [Test]
        public void LegacySixColumnContract_PreservesSourceLineId()
        {
            const string csv =
                "line_id,scene_id,speaker_id,text,emotion,voice_required\n" +
                "legacy_intro,TEST,CREW,hello,neutral,N";

            DialogueRecord record =
                DialogueCsvParser.Parse(csv).Records.Single();

            Assert.That(record.LineId, Is.EqualTo("legacy_intro"));
            Assert.That(record.CanonicalLineId, Is.EqualTo("legacy_intro"));
            Assert.That(record.StableLineId, Is.EqualTo("test_01"));
            Assert.That(record.ChoiceId, Is.Empty);
        }

        [TestCase("P-01", 1, "p_01_01")]
        [TestCase("D8-03", 32, "d8_03_32")]
        [TestCase("  Scene / Test  ", 7, "scene_test_07")]
        public void CompatibilityLineId_IsDeterministic(
            string sceneId,
            int order,
            string expected)
        {
            Assert.That(
                DialogueRecord.CreateStableLineId(sceneId, order),
                Is.EqualTo(expected));
        }
    }
}
