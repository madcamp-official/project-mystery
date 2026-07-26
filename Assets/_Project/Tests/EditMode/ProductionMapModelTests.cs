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
        public void ViewModel_ContainsAllTwentyFiveCanonicalLocations()
        {
            ProductionMapViewModel model =
                ProductionMapViewModel.Create(graph, null, 15);

            Assert.That(model.Entries, Has.Count.EqualTo(25));
            Assert.That(model.Entries.Select(entry => entry.Spec.Code), Is.Unique);
            Assert.That(
                model.Entries.Select(entry => entry.Location).All(item => item != null),
                Is.True);
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
            Assert.That(
                gangway.DenialReason,
                Is.EqualTo(SceneAccessDenialReason.PrerequisiteSceneIncomplete));
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
        public void UnresolvedNarrativeScenes_AreListedWithoutPhysicalMapping()
        {
            ProductionMapViewModel model =
                ProductionMapViewModel.Create(graph, null, 15);

            Assert.That(model.UnresolvedScenes.Count, Is.GreaterThan(0));
            Assert.That(
                model.UnresolvedScenes
                    .Select(scene => scene.NarrativeLocationCode)
                    .Distinct()
                    .OrderBy(value => value),
                Is.EqualTo(CanonicalLocationCatalog.UnresolvedCodes.OrderBy(value => value)));
            Assert.That(
                model.Entries.All(entry =>
                    !CanonicalLocationCatalog.UnresolvedCodes.Contains(
                        entry.Spec.Code)),
                Is.True);
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
                ProductionMapLayoutCalculator.Calculate(25, width, safeArea);

            Assert.That(layout.Columns, Is.EqualTo(expectedColumns));
            Assert.That(layout.CellSize.x, Is.GreaterThanOrEqualTo(280f));
            Assert.That(
                layout.CellSize.x * layout.Columns +
                12f * (layout.Columns - 1),
                Is.LessThanOrEqualTo(safeArea.width - 31.99f));
            Assert.That(layout.ContentHeight, Is.GreaterThan(900f));
        }

        [Test]
        public void Layout_UsesNarrowSafeAreaInsteadOfFullViewport()
        {
            Rect safeArea = new(120f, 0f, 680f, 900f);

            ProductionMapLayout layout =
                ProductionMapLayoutCalculator.Calculate(25, 1400f, safeArea);

            Assert.That(layout.Columns, Is.EqualTo(1));
            Assert.That(layout.CellSize.x, Is.EqualTo(648f).Within(0.01f));
        }
    }
}
