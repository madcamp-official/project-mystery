using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.Exploration;
using Wake.Narrative;
using Wake.UI;

namespace Wake.Tests
{
    public class CanonicalLocationCatalogTests
    {
        private const string LocationFolder = "Assets/_Project/Content/Locations";
        private const string BackgroundFolder =
            "Assets/_Project/Art/Backgrounds/Locations";

        [Test]
        public void Catalog_DefinesAllThirtyStructureMapLocations()
        {
            IReadOnlyList<CanonicalLocationSpec> definitions = CanonicalLocationCatalog.All;

            Assert.That(definitions.Count, Is.EqualTo(30));
            Assert.That(definitions.Select(item => item.Code), Is.Unique);
            Assert.That(definitions.Count(item => item.Deck == 10), Is.EqualTo(7));
            Assert.That(definitions.Count(item => item.Deck == 9), Is.EqualTo(4));
            Assert.That(definitions.Count(item => item.Deck == 8), Is.EqualTo(5));
            Assert.That(definitions.Count(item => item.Deck == 7), Is.EqualTo(6));
            Assert.That(definitions.Count(item => item.Deck == 6), Is.EqualTo(6));
            Assert.That(definitions.Count(item => item.Deck == 5), Is.Zero);
            Assert.That(definitions.Count(item => item.Deck == 0), Is.EqualTo(2));
            Assert.That(
                CanonicalLocationCatalog.Playable.Count,
                Is.EqualTo(24));
            Assert.That(
                CanonicalLocationCatalog.Unused.Select(item => item.Code),
                Is.EquivalentTo(new[]
                {
                    "LAUNDRY",
                    "SERVICE_HUB",
                    "STABILIZERS",
                    "BALLAST_TANKS",
                    "GENERATOR",
                    "WORKSHOP"
                }));
            Assert.That(
                CanonicalLocationCatalog.Unused.All(item =>
                    item.Usage == CanonicalLocationUsage.Unused),
                Is.True);
        }

        [Test]
        public void StructureContract_KeepsEngineeringOnDeckSevenAndLowerMachineryUnused()
        {
            CanonicalLocationSpec[] deckSeven = CanonicalLocationCatalog.Playable
                .Where(item => item.Deck == 7)
                .ToArray();

            Assert.That(
                deckSeven.Select(item => item.Code),
                Is.EquivalentTo(new[]
                {
                    "CABIN_DANIEL",
                    "SERVICE7",
                    "ENGINE_CONTROL",
                    "BALLAST_CONTROL_ANNEX",
                    "CREW_STAIRS",
                    "SERVICE_RAIL"
                }));
            Assert.That(
                deckSeven.Select(item => item.RoomCode),
                Is.EquivalentTo(new[]
                {
                    "D7-1",
                    "D7-2",
                    "D7-3",
                    "D7-4",
                    "D7-5",
                    "D7-6"
                }));
            Assert.That(
                CanonicalLocationCatalog.Unused.All(item => item.Deck == 6),
                Is.True);
            Assert.That(
                CanonicalLocationCatalog.Playable.Any(item => item.Deck == 6),
                Is.False);
            Assert.That(MapDeckCatalog.DeckOrder.Contains(6), Is.False);
            Assert.That(
                ProductionSceneCatalog.All.All(scene =>
                    CanonicalLocationCatalog.FindSpec(
                        scene.NarrativeLocationCode)?.IsPlayable == true),
                Is.True);
            Assert.That(
                CanonicalLocationCatalog.FindSpec(
                    ProductionSceneCatalog.All.Single(scene =>
                        scene.SceneId == "D6-01").NarrativeLocationCode)?.Code,
                Is.EqualTo("ENGINE_CONTROL"));
            Assert.That(
                CanonicalLocationCatalog.FindSpec(
                    ProductionSceneCatalog.All.Single(scene =>
                        scene.SceneId == "D6-02").NarrativeLocationCode)?.Code,
                Is.EqualTo("SERVICE_RAIL"));
            Assert.That(
                CanonicalLocationCatalog.FindSpec(
                    ProductionSceneCatalog.All.Single(scene =>
                        scene.SceneId == "D6-03").NarrativeLocationCode)?.Code,
                Is.EqualTo("BALLAST_CONTROL_ANNEX"));
        }

        [Test]
        public void EveryProductionSceneLocation_HasExactlyOneCanonicalAndGraphOwner()
        {
            LocationGraph graph = AssetDatabase.LoadAssetAtPath<LocationGraph>(
                $"{LocationFolder}/LocationGraph.asset");
            string[] ownedCodes = CanonicalLocationCatalog.All
                .SelectMany(spec =>
                    new[] { spec.Code }.Concat(spec.NarrativeAliases))
                .Select(code => code?.Trim())
                .Where(code => !string.IsNullOrEmpty(code))
                .ToArray();

            Assert.That(graph, Is.Not.Null);
            Assert.That(
                ownedCodes.Select(code => code.ToUpperInvariant()),
                Is.Unique,
                "A canonical code or narrative alias belongs to more than one location.");
            Assert.That(
                graph.Locations.Select(location => location.LocationCode),
                Is.EquivalentTo(
                    CanonicalLocationCatalog.All.Select(spec => spec.Code)));

            foreach (ProductionSceneDefinition scene in ProductionSceneCatalog.All)
            {
                CanonicalLocationSpec[] canonicalOwners =
                    CanonicalLocationCatalog.All
                        .Where(spec =>
                            string.Equals(
                                spec.Code,
                                scene.NarrativeLocationCode?.Trim(),
                                StringComparison.Ordinal) ||
                            spec.NarrativeAliases.Contains(
                                scene.NarrativeLocationCode?.Trim(),
                                StringComparer.Ordinal))
                        .ToArray();
                LocationDefinition[] graphOwners = graph.Locations
                    .Where(location =>
                        location.MatchesCode(scene.NarrativeLocationCode))
                    .ToArray();

                Assert.That(
                    canonicalOwners,
                    Has.Length.EqualTo(1),
                    scene.SceneId);
                Assert.That(
                    graphOwners,
                    Has.Length.EqualTo(1),
                    scene.SceneId);
                Assert.That(canonicalOwners[0].IsPlayable, Is.True, scene.SceneId);
                Assert.That(
                    graphOwners[0].LocationCode,
                    Is.EqualTo(canonicalOwners[0].Code),
                    scene.SceneId);
                Assert.That(
                    graphOwners[0].Deck,
                    Is.EqualTo(canonicalOwners[0].Deck),
                    scene.SceneId);
            }
        }

        [Test]
        public void LocationAssets_AreCompleteAndReferenceBackgroundSprites()
        {
            LocationDefinition[] locations = LoadLocations();
            IReadOnlyList<LocationCatalogDiagnostic> diagnostics =
                CanonicalLocationCatalog.Validate(locations, ProductionSceneCatalog.All);

            Assert.That(locations, Has.Length.EqualTo(30));
            Assert.That(
                diagnostics.Where(item => item.Severity == LocationCatalogDiagnosticSeverity.Error),
                Is.Empty,
                string.Join("\n", diagnostics.Select(item => $"{item.Code}: {item.Message}")));
            Assert.That(locations.All(item => item.BackgroundSprite != null), Is.True);
            Assert.That(
                locations.Select(item => item.BackgroundSprite).Distinct().Count(),
                Is.EqualTo(25),
                "Five newly separated physical locations temporarily reuse their former shared scene art.");
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
        [TestCase("SERVICE7", "SERVICE7")]
        [TestCase("CABIN_DANIEL", "CABIN_DANIEL")]
        [TestCase("BRIDGE", "BRIDGE")]
        [TestCase("CABIN_CLAIRE", "CABIN_CLAIRE")]
        [TestCase("INTERVIEW", "INTERVIEW")]
        [TestCase("FORENSIC", "MEDBAY")]
        [TestCase("EVIDENCE_BOARD", "NEWS_LOUNGE")]
        [TestCase("STERN", "OPEN_DECK")]
        [TestCase(" engine_ctrl ", "ENGINE_CONTROL")]
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
            Assert.That(graph.Locations, Has.Count.EqualTo(30));
            Assert.That(graph.StartingLocation.LocationCode, Is.EqualTo("PORT"));
            Assert.That(graph.FindByCode(" PORT "), Is.SameAs(graph.StartingLocation));
            Assert.That(graph.FindByCode("DECK10_SUITE").LocationCode, Is.EqualTo("RICHARD_SUITE"));
            Assert.That(graph.FindByCode("SERVICE7").LocationCode, Is.EqualTo("SERVICE7"));
            Assert.That(graph.FindByCode("CABIN_DANIEL").LocationCode, Is.EqualTo("CABIN_DANIEL"));
            Assert.That(graph.FindByCode("BRIDGE").LocationCode, Is.EqualTo("BRIDGE"));
            Assert.That(graph.FindByCode("CABIN_CLAIRE").LocationCode, Is.EqualTo("CABIN_CLAIRE"));
            Assert.That(graph.FindByCode("INTERVIEW").LocationCode, Is.EqualTo("INTERVIEW"));
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
        public void EveryCanonicalLocation_RegistersAnAuthoredBackground()
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

            Assert.That(registeredPaths, Has.Length.EqualTo(30));
            Assert.That(
                registeredPaths.Distinct().Count(),
                Is.EqualTo(25));
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
