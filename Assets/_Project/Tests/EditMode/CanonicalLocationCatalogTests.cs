using System;
using System.Collections.Generic;
using System.IO;
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
        private const string BackgroundFolder =
            "Assets/_Project/Art/Backgrounds/Locations";

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
        public void NarrativeSchedule_ResolvesEveryBackgroundCode()
        {
            IReadOnlyList<LocationCatalogDiagnostic> diagnostics =
                CanonicalLocationCatalog.Validate(LoadLocations(), ProductionSceneCatalog.All);

            Assert.That(diagnostics, Is.Empty);
            Assert.That(CanonicalLocationCatalog.UnresolvedCodes, Is.Empty);
        }

        [TestCase("DECK10_SUITE", "RICHARD_SUITE")]
        [TestCase("DECK8_ATRIUM", "ATRIUM")]
        [TestCase("ENGINE_CTRL", "ENGINE_CONTROL")]
        [TestCase("STAIR_B", "CREW_STAIRS")]
        [TestCase("BALLAST", "BALLAST_CONTROL_ANNEX")]
        [TestCase("SERVICE7", "CREW_STAIRS")]
        [TestCase("CABIN_DANIEL", "NEWS_LOUNGE")]
        [TestCase("BRIDGE", "ENGINE_CONTROL")]
        [TestCase("CABIN_CLAIRE", "VIP_LOUNGE")]
        [TestCase("INTERVIEW", "SECURITY")]
        [TestCase("FORENSIC", "MEDBAY")]
        [TestCase("EVIDENCE_BOARD", "NEWS_LOUNGE")]
        [TestCase("STERN", "OPEN_DECK")]
        public void NarrativeAliases_ResolveToDocumentedPhysicalLocations(
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
            Assert.That(graph.FindByCode("SERVICE7").LocationCode, Is.EqualTo("CREW_STAIRS"));
            Assert.That(graph.FindByCode("CABIN_DANIEL").LocationCode, Is.EqualTo("NEWS_LOUNGE"));
            Assert.That(graph.FindByCode("BRIDGE").LocationCode, Is.EqualTo("ENGINE_CONTROL"));
            Assert.That(graph.FindByCode("CABIN_CLAIRE").LocationCode, Is.EqualTo("VIP_LOUNGE"));
            Assert.That(graph.FindByCode("INTERVIEW").LocationCode, Is.EqualTo("SECURITY"));
            Assert.That(graph.FindByCode("FORENSIC").LocationCode, Is.EqualTo("MEDBAY"));
            Assert.That(graph.FindByCode("EVIDENCE_BOARD").LocationCode, Is.EqualTo("NEWS_LOUNGE"));
            Assert.That(graph.FindByCode("STERN").LocationCode, Is.EqualTo("OPEN_DECK"));
        }

        [Test]
        public void EveryLocation_ReferencesItsCanonicalBackgroundFile()
        {
            foreach (LocationDefinition location in LoadLocations())
            {
                CanonicalLocationSpec spec =
                    CanonicalLocationCatalog.FindSpec(location.LocationCode);
                string spritePath =
                    AssetDatabase.GetAssetPath(location.BackgroundSprite);

                Assert.That(spec, Is.Not.Null, location.LocationCode);
                Assert.That(
                    Path.GetFileName(spritePath),
                    Is.EqualTo(spec.SpriteFileName),
                    location.LocationCode);
                Assert.That(
                    spritePath,
                    Does.StartWith(BackgroundFolder),
                    location.LocationCode);
            }
        }

        [Test]
        public void AllTwentyFiveCanonicalBackgroundSprites_AreRegisteredOnce()
        {
            LocationDefinition[] locations = LoadLocations();
            string[] registeredPaths = locations
                .Select(location =>
                    AssetDatabase.GetAssetPath(location.BackgroundSprite))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            string[] backgroundPaths = AssetDatabase
                .FindAssets("t:Sprite", new[] { BackgroundFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.That(registeredPaths, Has.Length.EqualTo(25));
            Assert.That(registeredPaths, Is.Unique);
            string[] unregisteredArtVariants = backgroundPaths
                .Except(registeredPaths, StringComparer.Ordinal)
                .ToArray();

            Assert.That(
                registeredPaths.All(backgroundPaths.Contains),
                Is.True);
            Assert.That(
                unregisteredArtVariants,
                Has.Length.EqualTo(8));
        }

        [Test]
        public void EveryScheduledScene_ResolvesToASpriteBackedLocation()
        {
            LocationDefinition[] locations = LoadLocations();
            foreach (ProductionSceneDefinition scene in
                     ProductionSceneCatalog.All)
            {
                CanonicalLocationSpec spec =
                    CanonicalLocationCatalog.FindSpec(
                        scene.NarrativeLocationCode);
                LocationDefinition location = locations.SingleOrDefault(item =>
                    item.LocationCode == spec?.Code);

                Assert.That(spec, Is.Not.Null, scene.SceneId);
                Assert.That(location, Is.Not.Null, scene.SceneId);
                Assert.That(
                    location.BackgroundSprite,
                    Is.Not.Null,
                    scene.SceneId);
            }
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
