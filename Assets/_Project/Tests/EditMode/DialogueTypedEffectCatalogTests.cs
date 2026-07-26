using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.Core;
using Wake.Narrative;

namespace Wake.Tests
{
    public class DialogueTypedEffectCatalogTests
    {
        private const string SaveKey = "THE_WAKE_GAME_STATE_V1";
        private const string DialoguePath =
            "Assets/_Project/Content/Dialogue/The_Wake_Without_Footprints_Dialogue_KR.csv";

        private List<DialogueRecord> records;
        private GameObject host;
        private GameStateManager state;

        [OneTimeSetUp]
        public void LoadRecords()
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(DialoguePath);
            Assert.That(asset, Is.Not.Null);
            records = DialogueCsvParser.Parse(asset.text).Records.ToList();
        }

        [SetUp]
        public void SetUp()
        {
            DestroyState();
            PlayerPrefs.DeleteKey(SaveKey);
            host = new GameObject("DialogueTypedEffectCatalogTests");
            state = host.AddComponent<GameStateManager>();
        }

        [TearDown]
        public void TearDown()
        {
            DestroyState();
            PlayerPrefs.DeleteKey(SaveKey);
        }

        [Test]
        public void ProductionEffectInventory_MatchesAllFortyOneCsvValues()
        {
            string[] csvEffects = records
                .Select(record => record.NextOrEffect)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct()
                .OrderBy(value => value)
                .ToArray();

            Assert.That(csvEffects, Has.Length.EqualTo(41));
            Assert.That(
                DialogueEffectCatalog.ProductionEffectKeys.OrderBy(value => value),
                Is.EqualTo(csvEffects));
        }

        [TestCase("p_02_04", "secretary_access")]
        [TestCase("d1_01_04", "interrogation_keywords")]
        [TestCase("d1_05_04", "message_metadata")]
        [TestCase("d2_01_05", "sealed_room_proposition")]
        [TestCase("d2_05_04", "service_rail_foreshadowed")]
        [TestCase("d3_03_04", "vault_access_quest")]
        [TestCase("d4_04_04", "vault_accomplice_connection")]
        [TestCase("d5_02_05", "daniel_tablet_recovered")]
        [TestCase("d5_04_04", "service_rail_access")]
        [TestCase("d6_01_04", "body_movement_confirmed")]
        [TestCase("d6_03_04", "actual_scene_confirmed")]
        [TestCase("d6_05_04", "final_interrogation_condition_1")]
        [TestCase("d7_02_04", "final_physical_evidence")]
        [TestCase("d7_03_04", "past_culprit_confirmed")]
        public void ConfirmedResultLine_MapsToTypedFlag(
            string stableLineId,
            string expectedFlag)
        {
            DialogueRecord record =
                records.Single(item => item.StableLineId == stableLineId);

            Assert.That(
                DialogueEffectCatalog.TryResolve(record, out DialogueTypedEffect effect),
                Is.True);

            effect.Apply(state);
            Assert.That(state.HasFlag(expectedFlag), Is.True);
        }

        [TestCase("Daniel 신뢰도 ±1")]
        [TestCase("Claire 적대도 변화")]
        [TestCase("Helena 신뢰도")]
        [TestCase("Richard 신뢰도 분기")]
        [TestCase("승객 불안 수치")]
        [TestCase("현장 보존도")]
        public void AmbiguousNumericEffect_RemainsUnmapped(string source)
        {
            DialogueRecord[] matching = records
                .Where(record => record.NextOrEffect == source)
                .ToArray();

            Assert.That(matching, Is.Not.Empty);
            Assert.That(
                matching.All(record =>
                    !DialogueEffectCatalog.TryResolve(record, out _)),
                Is.True);
        }

        [Test]
        public void Diagnostics_IncludeStableIdSourceRowAndOriginalText()
        {
            DialogueRecord ambiguous = records.Single(record =>
                record.StableLineId == "p_01_06");

            DialogueEffectDiagnostic diagnostic =
                DialogueEffectCatalog.GetDiagnostics(new[] { ambiguous }).Single();

            Assert.That(diagnostic.StableLineId, Is.EqualTo("p_01_06"));
            Assert.That(diagnostic.SourceRow, Is.EqualTo(7));
            Assert.That(diagnostic.Source, Is.EqualTo("Daniel 신뢰도 ±1"));
            Assert.That(diagnostic.Message, Does.Contain("실행하지 않았습니다"));
        }

        [Test]
        public void EmptyEffect_DoesNotProduceDiagnostic()
        {
            DialogueRecord empty = records.First(record =>
                string.IsNullOrWhiteSpace(record.NextOrEffect));

            Assert.That(
                DialogueEffectCatalog.GetDiagnostics(new[] { empty }),
                Is.Empty);
        }

        [Test]
        public void EffectKeyMapping_RequiresExactConfirmedText()
        {
            Assert.That(
                DialogueEffectCatalog.TryResolve(
                    "비서실 권한 플래그",
                    out DialogueTypedEffect effect),
                Is.True);
            Assert.That(effect.AddFlags, Contains.Item("secretary_access"));
            Assert.That(
                DialogueEffectCatalog.TryResolve("비서실 권한", out _),
                Is.False);
        }

        [Test]
        public void StableLineMapping_DoesNotMapOtherRowsWithSameNaturalLanguage()
        {
            DialogueRecord result = records.Single(record =>
                record.StableLineId == "d7_04_04");
            DialogueRecord choice = records.Single(record =>
                record.StableLineId == "d7_04_05");

            Assert.That(DialogueEffectCatalog.TryResolve(result, out _), Is.True);
            Assert.That(DialogueEffectCatalog.TryResolve(choice, out _), Is.False);
        }

        [Test]
        public void TypedEffect_AppliesFlagsObjectivesAndRemovalsTogether()
        {
            state.AddFlag("temporary");
            var effect = new DialogueTypedEffect
            {
                AddFlags = new[] { "confirmed_result" },
                RemoveFlags = new[] { "temporary" },
                CompleteObjectives = new[] { "board_ship" }
            };

            effect.Apply(state);

            Assert.That(state.HasFlag("confirmed_result"), Is.True);
            Assert.That(state.HasFlag("temporary"), Is.False);
            Assert.That(state.HasCompletedObjective("board_ship"), Is.True);
        }

        [Test]
        public void ProductionDiagnostics_PreserveEveryUnconfirmedOccurrence()
        {
            DialogueEffectDiagnostic[] diagnostics =
                DialogueEffectCatalog.GetDiagnostics(records).ToArray();
            int unresolvedRows = records.Count(record =>
                !string.IsNullOrWhiteSpace(record.NextOrEffect) &&
                !DialogueEffectCatalog.TryResolve(record, out _));

            Assert.That(diagnostics, Has.Length.EqualTo(unresolvedRows));
            Assert.That(diagnostics, Is.Not.Empty);
            Assert.That(
                diagnostics.All(item =>
                    item.SourceRow > 1 &&
                    !string.IsNullOrWhiteSpace(item.StableLineId) &&
                    !string.IsNullOrWhiteSpace(item.Source)),
                Is.True);
        }

        [Test]
        public void EveryConfirmedMapping_UsesKnownProductionEffectText()
        {
            DialogueRecord[] confirmed = records
                .Where(record => DialogueEffectCatalog.TryResolve(record, out _))
                .ToArray();

            Assert.That(confirmed, Is.Not.Empty);
            Assert.That(
                confirmed.All(record =>
                    DialogueEffectCatalog.ProductionEffectKeys.Contains(
                        record.NextOrEffect)),
                Is.True);
        }

        private void DestroyState()
        {
            if (GameStateManager.Instance != null)
            {
                Object.DestroyImmediate(GameStateManager.Instance.gameObject);
            }

            host = null;
            state = null;
        }
    }
}
