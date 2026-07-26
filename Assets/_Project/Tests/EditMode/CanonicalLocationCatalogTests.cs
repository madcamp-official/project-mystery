using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.Exploration;
using Wake.Narrative;

namespace Wake.Tests
{
    public class CanonicalLocationCatalogTests
    {
        private const string LocationFolder = "Assets/_Project/Content/Locations";

        [Test]
        public void Catalog_DefinesAllTwentyFiveStructureMapLocations()
        {
            IReadOnlyList<CanonicalLocationSpec> definitions = CanonicalLocationCatalog.All;

            Assert.That(definitions.Count, Is.EqualTo(25));
            Assert.That(definitions.Select(item => item.Code), Is.Unique);
            Assert.That(definitions.Count(item => item.Deck == 10), Is.EqualTo(3));
            Assert.That(definitions.Count(item => item.Deck == 9), Is.EqualTo(4));
            Assert.That(definitions.Count(item => item.Deck == 8), Is.EqualTo(4));
            Assert.That(definitions.Count(item => item.Deck == 7), Is.EqualTo(4));
            Assert.That(definitions.Count(item => item.Deck == 6), Is.EqualTo(4));
            Assert.That(definitions.Count(item => item.Deck == 5), Is.EqualTo(4));
        }

        [Test]
        public void LocationAssets_AreCompleteAndReferenceBackgroundSprites()
        {
            LocationDefinition[] locations = LoadLocations();
            IReadOnlyList<LocationCatalogDiagnostic> diagnostics =
                CanonicalLocationCatalog.Validate(locations, ProductionSceneCatalog.All);

            Assert.That(locations, Has.Length.EqualTo(25));
            Assert.That(
                diagnostics.Where(item => item.Severity == LocationCatalogDiagnosticSeverity.Error),
                Is.Empty,
                string.Join("\n", diagnostics.Select(item => $"{item.Code}: {item.Message}")));
            Assert.That(locations.All(item => item.BackgroundSprite != null), Is.True);
            Assert.That(locations.Select(item => item.BackgroundSprite).Distinct().Count(), Is.EqualTo(25));
        }

        [Test]
        public void NarrativeSchedule_ReportsExactlyEightUnresolvedBackgroundCodes()
        {
            IReadOnlyList<LocationCatalogDiagnostic> diagnostics =
                CanonicalLocationCatalog.Validate(LoadLocations(), ProductionSceneCatalog.All);
            string[] warningCodes = diagnostics
                .Where(item => item.Severity == LocationCatalogDiagnosticSeverity.Warning)
                .Select(item => item.Code)
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToArray();

            Assert.That(warningCodes, Is.EqualTo(new[]
            {
                "BRIDGE",
                "CABIN_CLAIRE",
                "CABIN_DANIEL",
                "EVIDENCE_BOARD",
                "FORENSIC",
                "INTERVIEW",
                "SERVICE7",
                "STERN"
            }));
        }

        [TestCase("DECK10_SUITE", "RICHARD_SUITE")]
        [TestCase("DECK8_ATRIUM", "ATRIUM")]
        [TestCase("ENGINE_CTRL", "ENGINE_CONTROL")]
        [TestCase("STAIR_B", "CREW_STAIRS")]
        [TestCase("BALLAST", "BALLAST_CONTROL_ANNEX")]
        public void NarrativeAliases_ResolveOnlyDocumentedPhysicalLocations(
            string narrativeCode,
            string expectedPhysicalCode)
        {
            CanonicalLocationSpec spec = CanonicalLocationCatalog.FindSpec(narrativeCode);

            Assert.That(spec, Is.Not.Null);
            Assert.That(spec.Code, Is.EqualTo(expectedPhysicalCode));
        }

        [Test]
        public void LocationGraph_StartsAtPortAndResolvesAliases()
        {
            LocationGraph graph = AssetDatabase.LoadAssetAtPath<LocationGraph>(
                $"{LocationFolder}/LocationGraph.asset");

            Assert.That(graph, Is.Not.Null);
            Assert.That(graph.Locations, Has.Count.EqualTo(25));
            Assert.That(graph.StartingLocation.LocationCode, Is.EqualTo("PORT"));
            Assert.That(graph.FindByCode(" PORT "), Is.SameAs(graph.StartingLocation));
            Assert.That(graph.FindByCode("DECK10_SUITE").LocationCode, Is.EqualTo("RICHARD_SUITE"));
            Assert.That(graph.FindByCode("SERVICE7"), Is.Null);
        }

        [Test]
        public void ExistingHorizonAssetGuid_IsPreserved()
        {
            string guid = AssetDatabase.AssetPathToGUID(
                $"{LocationFolder}/LocationDefinition_Horizon.asset");

            Assert.That(guid, Is.EqualTo("39be30fe0a3b429d8b6b00202b7f02f8"));
        }

        [Test]
        public void Validator_ReportsMissingAssetAndUnknownNarrativeCode()
        {
            LocationDefinition[] incomplete = LoadLocations()
                .Where(item => item.LocationCode != "PORT")
                .ToArray();
            ProductionSceneDefinition unknownScene = new(
                "TEST-01",
                1,
                600,
                "UNPLANNED_ROOM",
                ProductionSceneType.Investigation,
                Array.Empty<string>());

            IReadOnlyList<LocationCatalogDiagnostic> diagnostics =
                CanonicalLocationCatalog.Validate(incomplete, new[] { unknownScene });

            Assert.That(diagnostics.Any(item =>
                item.Severity == LocationCatalogDiagnosticSeverity.Error &&
                item.Code == "PORT" &&
                item.Message.Contains("missing")), Is.True);
            Assert.That(diagnostics.Any(item =>
                item.Severity == LocationCatalogDiagnosticSeverity.Error &&
                item.Code == "UNPLANNED_ROOM" &&
                item.Message.Contains("not documented")), Is.True);
        }

        private static LocationDefinition[] LoadLocations()
        {
            return AssetDatabase.FindAssets("t:LocationDefinition", new[] { LocationFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<LocationDefinition>)
                .OrderBy(item => item.LocationCode, StringComparer.Ordinal)
                .ToArray();
        }
    }
}
