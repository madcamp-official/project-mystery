using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.Editor;
using Wake.Narrative;

namespace Wake.Tests
{
    public sealed class ProductionRuntimeSourceBindingTests
    {
        [Test]
        public void Preflight_UsesAllThreeOfficialExports()
        {
            Assert.That(
                ProductionContentPreflight.CsvPath,
                Does.EndWith("Under_the_Horizon_Dialogue_KR.csv"));
            Assert.That(
                ProductionContentPreflight.ChoicesPath,
                Does.EndWith("Under_the_Horizon_Choices_KR.csv"));
            Assert.That(
                ProductionContentPreflight.SceneIndexPath,
                Does.EndWith("Under_the_Horizon_Scene_Index_KR.csv"));

            AssertAssetExists(ProductionContentPreflight.CsvPath);
            AssertAssetExists(ProductionContentPreflight.ChoicesPath);
            AssertAssetExists(ProductionContentPreflight.SceneIndexPath);
        }

        [Test]
        public void UiBasicScene_BindsOfficialDialogueAssetGuid()
        {
            string scene = File.ReadAllText(
                ProductionContentPreflight.ScenePath);
            string officialGuid = AssetDatabase.AssetPathToGUID(
                ProductionContentPreflight.CsvPath);
            Assert.That(officialGuid, Has.Length.EqualTo(32));
            Assert.That(
                scene,
                Does.Contain(
                    $"csvFile: {{fileID: 4900000, guid: {officialGuid}, type: 3}}"));
        }

        [Test]
        public void DialogueFolder_HasOnlyOfficialProductionDialogueExport()
        {
            string[] productionDialogueFiles = AssetDatabase
                .FindAssets(
                    "t:TextAsset",
                    new[] { "Assets/_Project/Content/Dialogue" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Path.GetFileName(path)
                    .EndsWith("_Dialogue_KR.csv", StringComparison.Ordinal))
                .ToArray();

            Assert.That(
                productionDialogueFiles,
                Is.EqualTo(new[] { ProductionContentPreflight.CsvPath }));
        }

        [Test]
        public void OfficialRuntimeAsset_HasExpectedShape()
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(
                ProductionContentPreflight.CsvPath);
            DialogueCsvParseResult parsed =
                DialogueCsvParser.Parse(asset.text);

            Assert.That(parsed.Success, Is.True, string.Join("\n", parsed.Errors));
            Assert.That(
                parsed.Records,
                Has.Count.EqualTo(
                    OfficialDialogueContractValidator.ExpectedDialogueCount));
            Assert.That(
                parsed.Records.Select(record => record.SceneId)
                    .Distinct(StringComparer.Ordinal),
                Has.Count.EqualTo(
                    OfficialDialogueContractValidator.ExpectedSceneCount));
            Assert.That(
                parsed.Records.Count(record =>
                    !string.IsNullOrWhiteSpace(record.ChoiceId)),
                Is.EqualTo(
                    OfficialDialogueContractValidator.ExpectedChoiceCount));
        }

        [Test]
        public void OfficialRuntimeAsset_PreservesCanonicalLineIds()
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(
                ProductionContentPreflight.CsvPath);
            DialogueRecord[] records =
                DialogueCsvParser.Parse(asset.text).Records.ToArray();

            Assert.That(
                records.All(record =>
                    !string.IsNullOrWhiteSpace(record.LineId)),
                Is.True);
            Assert.That(
                records.Select(record => record.LineId)
                    .Distinct(StringComparer.Ordinal),
                Has.Count.EqualTo(records.Length));
            Assert.That(records[0].LineId, Is.EqualTo("P-01_001"));
            Assert.That(records[0].CanonicalLineId, Is.EqualTo("P-01_001"));
            Assert.That(records[0].StableLineId, Is.EqualTo("p_01_01"));
        }

        [Test]
        public void OfficialRuntimeAsset_PassesContentAndCrossSheetValidation()
        {
            string dialogue = LoadText(ProductionContentPreflight.CsvPath);
            string choices = LoadText(ProductionContentPreflight.ChoicesPath);
            string scenes = LoadText(ProductionContentPreflight.SceneIndexPath);

            DialogueValidationReport content =
                DialogueContentValidator.Validate(dialogue);
            OfficialDialogueContractReport contract =
                OfficialDialogueContractValidator.Validate(
                    dialogue, choices, scenes);

            Assert.That(
                content.IsValid,
                Is.True,
                string.Join("\n", content.Diagnostics));
            Assert.That(
                contract.IsValid,
                Is.True,
                string.Join("\n", contract.Errors));
        }

        [Test]
        public void OfficialOpeningLine_MatchesUpdatedWorkbook()
        {
            DialogueRecord opening = DialogueCsvParser.Parse(
                    LoadText(ProductionContentPreflight.CsvPath))
                .Records.Single(record => record.LineId == "P-01_001");

            Assert.That(opening.SceneId, Is.EqualTo("P-01"));
            Assert.That(opening.Order, Is.EqualTo(1));
            Assert.That(opening.Speaker, Is.EqualTo("NARRATION"));
            Assert.That(
                opening.TextKo,
                Is.EqualTo(
                    "MV Elysium은 항구의 유리 지붕 너머에서 " +
                    "지나치게 새것처럼 빛나고 있었다."));
        }

        private static void AssertAssetExists(string path)
        {
            Assert.That(
                AssetDatabase.LoadAssetAtPath<TextAsset>(path),
                Is.Not.Null,
                $"Missing official runtime export: {path}");
        }

        private static string LoadText(string path)
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            Assert.That(asset, Is.Not.Null, $"Missing TextAsset: {path}");
            return asset.text;
        }
    }
}
