using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.Core;
using Wake.Exploration;
using Wake.Narrative;
using Wake.UI;

namespace Wake.Tests
{
    public class ProductionMapModelTests
    {
        private const string GraphPath =
            "Assets/_Project/Content/Locations/LocationGraph.asset";
        private LocationGraph graph;

        [OneTimeSetUp]
        public void LoadGraph()
        {
            graph = AssetDatabase.LoadAssetAtPath<LocationGraph>(GraphPath);
            Assert.That(graph, Is.Not.Null);
        }

        [Test]
        public void ViewModel_ContainsOnlyTwentyFourPlayableLocations()
        {
            ProductionMapViewModel model =
                ProductionMapViewModel.Create(graph, null, 15);

            Assert.That(model.Entries, Has.Count.EqualTo(24));
            Assert.That(model.Entries.Select(entry => entry.Spec.Code), Is.Unique);
            Assert.That(
                model.Entries.Select(entry => entry.Location).All(item => item != null),
                Is.True);
            Assert.That(
                model.Entries.Select(entry => entry.Spec.Code),
                Is.EquivalentTo(
                    CanonicalLocationCatalog.Playable.Select(item => item.Code)));
            Assert.That(
                model.Entries.Any(entry =>
                    CanonicalLocationCatalog.IsUnused(entry.Spec.Code)),
                Is.False);
        }

        [Test]
        public void OpeningScene_IsAvailableAndDependentSceneIsLocked()
        {
            ProductionMapViewModel model =
                ProductionMapViewModel.Create(graph, null, 15);
            ProductionMapEntry port =
                model.Entries.Single(entry => entry.Spec.Code == "PORT");
            ProductionMapEntry gangway =
                model.Entries.Single(entry => entry.Spec.Code == "GANGWAY");

            Assert.That(port.SceneId, Is.EqualTo("P-01"));
            Assert.That(port.Status, Is.EqualTo(ProductionMapEntryStatus.Available));
            Assert.That(gangway.SceneId, Is.EqualTo("P-02"));
            Assert.That(gangway.Status, Is.EqualTo(ProductionMapEntryStatus.Locked));
            Assert.That(gangway.IsVisible, Is.False);
            Assert.That(
                gangway.DenialReason,
                Is.EqualTo(SceneAccessDenialReason.PrerequisiteSceneIncomplete));
        }

        [Test]
        public void RuntimeUnlockSet_KeepsFutureLocationLocked()
        {
            ProductionMapViewModel model = ProductionMapViewModel.Create(
                graph,
                new[] { "P-01" },
                15,
                "",
                System.Array.Empty<string>());
            ProductionMapEntry gangway =
                model.Entries.Single(entry => entry.Spec.Code == "GANGWAY");

            Assert.That(gangway.SceneId, Is.EqualTo("P-02"));
            Assert.That(gangway.Status, Is.EqualTo(ProductionMapEntryStatus.Locked));
            Assert.That(gangway.IsVisible, Is.True);
            Assert.That(
                gangway.DenialReason,
                Is.EqualTo(SceneAccessDenialReason.SceneNotUnlocked));
            Assert.That(
                gangway.StatusLabel,
                Is.EqualTo("선행 장면 필요"));
        }

        [Test]
        public void Prologue_UnlocksGangwayThenSuiteBeforeFreeTravel()
        {
            ProductionMapViewModel afterPort = ProductionMapViewModel.Create(
                graph,
                new[] { "P-01" },
                15,
                "",
                new[] { "P-02" });
            Assert.That(
                afterPort.Entries.Single(
                    entry => entry.Spec.Code == "GANGWAY").Status,
                Is.EqualTo(ProductionMapEntryStatus.Available));
            Assert.That(
                afterPort.Entries.Single(
                    entry => entry.Spec.Code == "GANGWAY").IsVisible,
                Is.True);
            Assert.That(
                afterPort.Entries.Any(
                    entry => CanonicalLocationCatalog.IsUnused(
                        entry.Spec.Code)),
                Is.False);

            ProductionMapViewModel afterGangway =
                ProductionMapViewModel.Create(
                    graph,
                    new[] { "P-01", "P-02" },
                    15,
                    "",
                    new[] { "P-03" });
            Assert.That(
                afterGangway.Entries.Single(
                    entry => entry.Spec.Code == "RICHARD_SUITE").Status,
                Is.EqualTo(ProductionMapEntryStatus.Available));
            Assert.That(
                afterGangway.Entries.Single(
                    entry => entry.Spec.Code == "GANGWAY").IsVisible,
                Is.False);
            Assert.That(
                afterGangway.Entries.Any(
                    entry => CanonicalLocationCatalog.IsUnused(
                        entry.Spec.Code)),
                Is.False);

            ProductionMapViewModel afterBoarding =
                ProductionMapViewModel.Create(
                    graph,
                    new[] { "P-01", "P-02", "P-03" },
                    15);
            Assert.That(
                afterBoarding.Entries.Single(
                    entry => entry.Spec.Code == "VIP_LOUNGE").Status,
                Is.EqualTo(ProductionMapEntryStatus.LocationOnly));
            Assert.That(
                afterBoarding.Entries.Single(
                    entry => entry.Spec.Code == "GANGWAY").IsVisible,
                Is.False);
            foreach (string futureStoryLocation in new[]
                     {
                         "HORIZON",
                         "OPEN_DECK",
                         "BRIDGE",
                         "CABIN_CLAIRE"
                     })
            {
                Assert.That(
                    afterBoarding.Entries.Single(
                        entry =>
                            entry.Spec.Code == futureStoryLocation).Status,
                    Is.EqualTo(ProductionMapEntryStatus.Locked),
                    futureStoryLocation);
            }
        }

        [Test]
        public void RestrictedTechnicalLocation_ShowsAnxietyClosure()
        {
            string[] completed = ProductionSceneCatalog.All
                .Where(scene => scene.SceneId != "D2-04")
                .Select(scene => scene.SceneId)
                .ToArray();
            ProductionMapViewModel model = ProductionMapViewModel.Create(
                graph,
                completed,
                GameStateManager.RestrictedAreaAnxiety);
            ProductionMapEntry security =
                model.Entries.Single(entry => entry.Spec.Code == "SECURITY");

            Assert.That(security.SceneId, Is.EqualTo("D2-04"));
            Assert.That(security.Status, Is.EqualTo(ProductionMapEntryStatus.Locked));
            Assert.That(
                security.DenialReason,
                Is.EqualTo(SceneAccessDenialReason.RestrictedByPublicAnxiety));
            Assert.That(security.StatusLabel, Does.Contain("폐쇄"));
        }

        [Test]
        public void EveryOfficialNarrativeScene_IsBackedByPhysicalMapping()
        {
            ProductionMapViewModel model =
                ProductionMapViewModel.Create(graph, null, 15);

            Assert.That(model.UnresolvedScenes, Is.Empty);
            Assert.That(model.DialogueOnlyEntries, Is.Empty);
            Assert.That(
                ProductionSceneCatalog.All.All(scene =>
                    CanonicalLocationCatalog.FindSpec(
                        scene.NarrativeLocationCode) != null),
                Is.True);
        }

        [TestCase("CABIN_CLAIRE", "CABIN_CLAIRE")]
        [TestCase("STERN", "OPEN_DECK")]
        [TestCase("CABIN_DANIEL", "CABIN_DANIEL")]
        [TestCase("EVIDENCE_BOARD", "NEWS_LOUNGE")]
        [TestCase("INTERVIEW", "INTERVIEW")]
        [TestCase("FORENSIC", "MEDBAY")]
        [TestCase("BRIDGE", "BRIDGE")]
        [TestCase("SERVICE7", "SERVICE7")]
        public void NarrativeAlias_AppearsUnderMappedPhysicalLocation(
            string narrativeCode,
            string expectedPhysicalCode)
        {
            ProductionSceneDefinition scene = ProductionSceneCatalog.All
                .First(item => item.NarrativeLocationCode == narrativeCode);
            ProductionMapViewModel model =
                ProductionMapViewModel.Create(graph, null, 15);
            CanonicalLocationSpec spec =
                CanonicalLocationCatalog.FindSpec(narrativeCode);
            ProductionMapEntry entry = model.Entries
                .Single(item => item.Spec.Code == expectedPhysicalCode);

            Assert.That(spec, Is.Not.Null);
            Assert.That(spec.Code, Is.EqualTo(expectedPhysicalCode));
            Assert.That(entry.Location, Is.Not.Null);
            Assert.That(entry.Location.BackgroundSprite, Is.Not.Null);
            Assert.That(
                ProductionSceneCatalog.All
                    .Where(item =>
                        CanonicalLocationCatalog.FindSpec(
                            item.NarrativeLocationCode)?.Code ==
                        expectedPhysicalCode)
                    .Select(item => item.SceneId),
                Does.Contain(scene.SceneId));
        }

        [TestCase(500f, 1)]
        [TestCase(900f, 2)]
        [TestCase(1400f, 3)]
        public void Layout_AdaptsColumnsToSafeWidth(
            float width,
            int expectedColumns)
        {
            Rect safeArea = new(0f, 0f, width, 900f);

            ProductionMapLayout layout =
                ProductionMapLayoutCalculator.Calculate(19, width, safeArea);

            Assert.That(layout.Columns, Is.EqualTo(expectedColumns));
            Assert.That(layout.CellSize.x, Is.GreaterThanOrEqualTo(280f));
            Assert.That(
                layout.CellSize.x * layout.Columns +
                12f * (layout.Columns - 1),
                Is.LessThanOrEqualTo(safeArea.width - 31.99f));
            int rows = Mathf.CeilToInt(19f / expectedColumns);
            float expectedHeight =
                32f + rows * 112f + Mathf.Max(0, rows - 1) * 12f;
            Assert.That(layout.ContentHeight, Is.EqualTo(expectedHeight));
        }

        [Test]
        public void Layout_UsesNarrowSafeAreaInsteadOfFullViewport()
        {
            Rect safeArea = new(120f, 0f, 680f, 900f);

            ProductionMapLayout layout =
                ProductionMapLayoutCalculator.Calculate(19, 1400f, safeArea);

            Assert.That(layout.Columns, Is.EqualTo(1));
            Assert.That(layout.CellSize.x, Is.EqualTo(648f).Within(0.01f));
        }
    }
}
