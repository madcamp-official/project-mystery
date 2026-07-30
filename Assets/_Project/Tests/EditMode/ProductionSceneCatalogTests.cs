using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.Core;
using Wake.Narrative;

namespace Wake.Tests
{
    public class ProductionSceneCatalogTests
    {
        private const string CsvPath =
            "Assets/_Project/Content/Dialogue/Under_the_Horizon_Dialogue_KR.csv";
        private IReadOnlyList<DialogueRecord> records;

        [OneTimeSetUp]
        public void LoadProductionDialogue()
        {
            TextAsset csv = AssetDatabase.LoadAssetAtPath<TextAsset>(CsvPath);
            Assert.That(csv, Is.Not.Null, CsvPath);
            DialogueCsvParseResult result = DialogueCsvParser.Parse(csv.text);
            Assert.That(result.Success, Is.True, string.Join("\n", result.Errors));
            records = result.Records;
        }

        [Test]
        public void Catalog_ContainsExactlyTheFortyOneCsvScenes()
        {
            string[] scheduleIds = ProductionSceneCatalog.All
                .Select(scene => scene.SceneId)
                .ToArray();
            string[] csvIds = records
                .Select(record => record.SceneId)
                .Distinct()
                .ToArray();

            Assert.That(scheduleIds.Length, Is.EqualTo(41));
            Assert.That(scheduleIds.Distinct().Count(), Is.EqualTo(41));
            Assert.That(scheduleIds, Is.EquivalentTo(csvIds));
        }

        [TestCase("P-01", 0, "15:10", "PORT", ProductionSceneType.Prologue)]
        [TestCase("D1-06", 1, "22:45", "HORIZON", ProductionSceneType.Investigation)]
        [TestCase("D6-05", 6, "18:00", "EVIDENCE_BOARD", ProductionSceneType.Puzzle)]
        [TestCase("D8-01", 8, "08:00", "HORIZON", ProductionSceneType.Finale)]
        [TestCase("D8-03", 8, "11:30", "PORT", ProductionSceneType.Epilogue)]
        public void Catalog_PreservesSourceScheduleAnchors(
            string sceneId,
            int day,
            string time,
            string location,
            ProductionSceneType type)
        {
            Assert.That(ProductionSceneCatalog.TryGet(sceneId, out var definition), Is.True);
            Assert.That(definition.Day, Is.EqualTo(day));
            Assert.That(definition.TimeLabel, Is.EqualTo(time));
            Assert.That(definition.NarrativeLocationCode, Is.EqualTo(location));
            Assert.That(definition.SceneType, Is.EqualTo(type));
        }

        [Test]
        public void DayBoundaryCatalog_CoversEveryDayOneThroughEightTransition()
        {
            string[] expected =
            {
                "D1-07>D2-01",
                "D2-06>D3-01",
                "D3-05>D4-01",
                "D4-04>D5-01",
                "D5-04>D6-01",
                "D6-05>D7-01",
                "D7-04>D8-01"
            };

            Assert.That(
                ProductionDayBoundaryCatalog.All.Select(item =>
                    $"{item.CompletedSceneId}>{item.NextSceneId}"),
                Is.EqualTo(expected));
            foreach (ProductionDayBoundary boundary in
                     ProductionDayBoundaryCatalog.All)
            {
                Assert.That(
                    ProductionSceneCatalog.TryGet(
                        boundary.CompletedSceneId,
                        out ProductionSceneDefinition completed),
                    Is.True);
                Assert.That(
                    ProductionSceneCatalog.TryGet(
                        boundary.NextSceneId,
                        out ProductionSceneDefinition next),
                    Is.True);
                Assert.That(next.Day, Is.EqualTo(completed.Day + 1));
            }
        }

        [Test]
        public void ChapterTransitionCatalog_CoversDepartureAndEveryDayBoundary()
        {
            string[] expected =
            {
                "P-03>D1-01:Departure",
                "D1-07>D2-01:DayChange",
                "D2-06>D3-01:DayChange",
                "D3-05>D4-01:DayChange",
                "D4-04>D5-01:DayChange",
                "D5-04>D6-01:DayChange",
                "D6-05>D7-01:DayChange",
                "D7-04>D8-01:Finale"
            };

            Assert.That(
                ProductionChapterTransitionCatalog.All.Select(item =>
                    $"{item.CompletedSceneId}>{item.NextSceneId}:" +
                    item.TransitionKind),
                Is.EqualTo(expected));
            Assert.That(
                ProductionChapterTransitionCatalog.All,
                Has.All.Matches<ChapterTransitionRequest>(item =>
                    !string.IsNullOrWhiteSpace(item.ChapterLabel) &&
                    !string.IsNullOrWhiteSpace(item.Title) &&
                    !string.IsNullOrWhiteSpace(item.Summary) &&
                    !string.IsNullOrWhiteSpace(item.BackgroundKey) &&
                    !string.IsNullOrWhiteSpace(item.MusicKey) &&
                    item.MinimumDisplayTime >= 2.5f));
        }

        [Test]
        public void DepartureTransition_HasDedicatedPresentationAndAudio()
        {
            Assert.That(
                ProductionChapterTransitionCatalog.TryGet(
                    "p-03",
                    out ChapterTransitionRequest departure),
                Is.True);
            Assert.That(departure.IsDeparture, Is.True);
            Assert.That(departure.NextSceneId, Is.EqualTo("D1-01"));
            Assert.That(departure.ChapterLabel, Is.EqualTo("DAY 1"));
            Assert.That(departure.StingerKey, Does.EndWith("/horn"));
        }

        [Test]
        public void ChapterTransitions_HaveLoadableBackgroundMusic()
        {
            foreach (ChapterTransitionRequest transition in
                     ProductionChapterTransitionCatalog.All)
            {
                Assert.That(
                    transition.MusicKey,
                    Is.EqualTo(AudioCueCatalog.ChapterTransitionMusicKey),
                    $"{transition.CompletedSceneId} chapter music key");
                Assert.That(
                    UnityEngine.Resources.Load<UnityEngine.AudioClip>(
                        transition.MusicKey),
                    Is.Not.Null,
                    $"{transition.CompletedSceneId} chapter music");
            }
        }

        [Test]
        public void Validator_AcceptsTheProductionCsvWithoutDiagnostics()
        {
            Assert.That(
                records.Count(record => record.StageDirection == "UI choice"),
                Is.EqualTo(100));
            IReadOnlyList<SceneScheduleDiagnostic> diagnostics =
                ProductionSceneScheduleValidator.Validate(records);

            Assert.That(
                diagnostics,
                Is.Empty,
                string.Join("\n", diagnostics.Select(item => item.Message)));
        }

        [Test]
        public void Validator_ReportsMissingAndUnknownScenesWithSourceRows()
        {
            DialogueRecord unknown = CreateRecord(
                "X-01",
                "PORT",
                string.Empty,
                sourceRow: 900);
            ProductionSceneDefinition[] definitions =
                ProductionSceneCatalog.All.Skip(1).ToArray();

            IReadOnlyList<SceneScheduleDiagnostic> diagnostics =
                ProductionSceneScheduleValidator.Validate(
                    records.Concat(new[] { unknown }),
                    definitions);

            Assert.That(
                diagnostics,
                Has.Some.Matches<SceneScheduleDiagnostic>(item =>
                    item.SceneId == "P-01" &&
                    item.Message.Contains("missing from the production schedule")));
            Assert.That(
                diagnostics,
                Has.Some.Matches<SceneScheduleDiagnostic>(item =>
                    item.SceneId == "X-01" &&
                    item.SourceRow == 900));
        }

        [Test]
        public void Validator_ReportsInvalidDefinitionAndPrerequisite()
        {
            var invalid = new ProductionSceneDefinition(
                "X-01",
                9,
                24 * 60,
                string.Empty,
                ProductionSceneType.Investigation,
                "MISSING");
            DialogueRecord record = CreateRecord("X-01", "PORT", "MISSING", 42);

            IReadOnlyList<SceneScheduleDiagnostic> diagnostics =
                ProductionSceneScheduleValidator.Validate(
                    new[] { record },
                    new[] { invalid });

            Assert.That(diagnostics.Count, Is.GreaterThanOrEqualTo(4));
            Assert.That(
                diagnostics,
                Has.Some.Matches<SceneScheduleDiagnostic>(item =>
                    item.Message.Contains("between 0 and 8")));
            Assert.That(
                diagnostics,
                Has.Some.Matches<SceneScheduleDiagnostic>(item =>
                    item.Message.Contains("24-hour")));
            Assert.That(
                diagnostics,
                Has.Some.Matches<SceneScheduleDiagnostic>(item =>
                    item.Message.Contains("known scene")));
        }

        private static DialogueRecord CreateRecord(
            string sceneId,
            string location,
            string condition,
            int sourceRow)
        {
            return new DialogueRecord(
                sceneId,
                1,
                "NARRATION",
                "테스트",
                "observe",
                condition,
                string.Empty,
                string.Empty,
                location,
                "N",
                false,
                sourceRow);
        }
    }
}
