using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.Narrative;

namespace Wake.Tests
{
    public sealed class ProductionEffectParserTests
    {
        private const string DialoguePath =
            "Assets/_Project/Content/Dialogue/" +
            "Under_the_Horizon_Dialogue_KR.csv";

        [Test]
        public void OfficialEffects_AllParseWithoutErrors()
        {
            DialogueRecord[] records = LoadRecords();
            DialogueRecord[] withEffects = records
                .Where(record =>
                    !string.IsNullOrWhiteSpace(record.NextOrEffect))
                .ToArray();
            ProductionEffectParseResult[] parsed = withEffects
                .Select(record =>
                    ProductionEffectParser.Parse(record.NextOrEffect))
                .ToArray();

            Assert.That(withEffects, Has.Length.EqualTo(286));
            Assert.That(
                parsed.SelectMany(result => result.Errors),
                Is.Empty);
            Assert.That(
                parsed.Sum(result => result.Instructions.Count),
                Is.GreaterThan(300));
        }

        [Test]
        public void CompoundChoiceEffect_PreservesOrderAndTypes()
        {
            ProductionEffectParseResult result = ProductionEffectParser.Parse(
                "trust_daniel:+1; flag:daniel_warning_taken");

            Assert.That(result.Success, Is.True);
            Assert.That(result.Instructions, Has.Count.EqualTo(2));
            Assert.That(
                result.Instructions[0].Kind,
                Is.EqualTo(ProductionEffectKind.Trust));
            Assert.That(result.Instructions[0].Key, Is.EqualTo("trust_daniel"));
            Assert.That(result.Instructions[0].NumericValue, Is.EqualTo(1));
            Assert.That(
                result.Instructions[1].Kind,
                Is.EqualTo(ProductionEffectKind.Flag));
            Assert.That(
                result.Instructions[1].Value,
                Is.EqualTo("daniel_warning_taken"));
        }

        [Test]
        public void CoreNumericEffects_AcceptSignedAndZeroValues()
        {
            ProductionEffectParseResult result = ProductionEffectParser.Parse(
                "publicAnxiety:-15; evidenceIntegrity:+0; " +
                "timeBlock:-1; wrong_strike:+1");

            Assert.That(result.Success, Is.True);
            Assert.That(
                result.Instructions.Select(item => item.NumericValue),
                Is.EqualTo(new int?[] { -15, 0, -1, 1 }));
            Assert.That(
                result.Instructions.Select(item => item.Kind),
                Is.EqualTo(new[]
                {
                    ProductionEffectKind.PublicAnxiety,
                    ProductionEffectKind.EvidenceIntegrity,
                    ProductionEffectKind.TimeBlock,
                    ProductionEffectKind.WrongStrike
                }));
        }

        [Test]
        public void SceneUnlock_PreservesMultipleSceneIds()
        {
            ProductionEffectInstruction instruction =
                ProductionEffectParser.Parse(
                    "scene_unlock:D1-04,D1-05")
                .Instructions.Single();

            Assert.That(
                instruction.Kind,
                Is.EqualTo(ProductionEffectKind.SceneUnlock));
            Assert.That(
                instruction.Values,
                Is.EqualTo(new[] { "D1-04", "D1-05" }));
        }

        [Test]
        public void MarkerAndMetadata_AreNotDiscarded()
        {
            ProductionEffectParseResult result = ProductionEffectParser.Parse(
                "question_used; answer:no; accusation1:correct");

            Assert.That(result.Success, Is.True);
            Assert.That(
                result.Instructions.Select(item => item.Kind),
                Is.EqualTo(new[]
                {
                    ProductionEffectKind.Marker,
                    ProductionEffectKind.Metadata,
                    ProductionEffectKind.Metadata
                }));
            Assert.That(result.Instructions[0].Key, Is.EqualTo("question_used"));
            Assert.That(result.Instructions[1].Value, Is.EqualTo("no"));
        }

        [Test]
        public void TrustAndHostility_RequireSignedInteger()
        {
            ProductionEffectParseResult result = ProductionEffectParser.Parse(
                "trust_richard:many; hostility_claire:high");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Instructions, Is.Empty);
            Assert.That(result.Errors, Has.Count.EqualTo(2));
            Assert.That(result.Errors, Has.All.Contains("signed integer"));
        }

        [Test]
        public void InvalidTokens_ReportPositionAndCause()
        {
            ProductionEffectParseResult result = ProductionEffectParser.Parse(
                "flag:; ; bad-key:value");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(3));
            Assert.That(result.Errors[0], Does.Contain("token 1"));
            Assert.That(result.Errors[0], Does.Contain("no value"));
            Assert.That(result.Errors[1], Does.Contain("token 2"));
            Assert.That(result.Errors[1], Does.Contain("empty"));
            Assert.That(result.Errors[2], Does.Contain("invalid key"));
        }

        [Test]
        public void EmptySource_IsValidNoOp()
        {
            ProductionEffectParseResult result =
                ProductionEffectParser.Parse("  ");

            Assert.That(result.Success, Is.True);
            Assert.That(result.Instructions, Is.Empty);
            Assert.That(result.Errors, Is.Empty);
        }

        private static DialogueRecord[] LoadRecords()
        {
            TextAsset asset =
                AssetDatabase.LoadAssetAtPath<TextAsset>(DialoguePath);
            Assert.That(asset, Is.Not.Null);
            DialogueCsvParseResult parsed =
                DialogueCsvParser.Parse(asset.text);
            Assert.That(parsed.Success, Is.True, string.Join("\n", parsed.Errors));
            return parsed.Records.ToArray();
        }
    }
}
