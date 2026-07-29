using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using Wake.Exploration;
using Wake.Narrative;
using Wake.UI;

namespace Wake.Tests
{
    public class MapStoryIntegrationTests
    {
        private const string GraphPath =
            "Assets/_Project/Content/Locations/LocationGraph.asset";
        private const string LayerRoot =
            "Assets/_Project/Resources/Maps/DeckLayers";

        [Test]
        public void AllFortyOneScenes_HaveMatchingObjectiveAndMapDestination()
        {
            Assert.That(ProductionSceneCatalog.All.Count, Is.EqualTo(41));
            Assert.That(ProductionObjectiveCatalog.All.Count, Is.EqualTo(41));
            Assert.That(
                ProductionObjectiveCatalog.All.Select(item => item.SceneId),
                Is.EquivalentTo(
                    ProductionSceneCatalog.All.Select(item => item.SceneId)));

            foreach (ProductionSceneDefinition scene in
                     ProductionSceneCatalog.All)
            {
                ProductionObjectiveDefinition objective =
                    ProductionObjectiveCatalog.All.Single(item =>
                        item.SceneId == scene.SceneId);
                CanonicalLocationSpec location =
                    CanonicalLocationCatalog.FindSpec(
                        scene.NarrativeLocationCode);
                MapLocationPlacement placement =
                    MapDeckCatalog.Find(scene.NarrativeLocationCode);

                Assert.That(location, Is.Not.Null, scene.SceneId);
                Assert.That(placement, Is.Not.Null, scene.SceneId);
                Assert.That(
                    objective.TargetLocation,
                    Is.EqualTo(scene.NarrativeLocationCode),
                    scene.SceneId);
                Assert.That(
                    placement.LocationCode,
                    Is.EqualTo(location.Code),
                    scene.SceneId);
                Assert.That(
                    placement.Deck,
                    Is.EqualTo(location.Deck),
                    scene.SceneId);
            }
        }

        [Test]
        public void SeparatedNarrativePlaces_NoLongerResolveToSharedRooms()
        {
            AssertPhysical("SERVICE7", "SERVICE7", 7);
            AssertPhysical("CABIN_DANIEL", "CABIN_DANIEL", 7);
            AssertPhysical("BRIDGE", "BRIDGE", 10);
            AssertPhysical("CABIN_CLAIRE", "CABIN_CLAIRE", 8);
            AssertPhysical("INTERVIEW", "INTERVIEW", 10);
        }

        [Test]
        public void StoryRelevantLocations_AllHaveMapPlacementsOnTheirCanonicalDeck()
        {
            Assert.That(
                CanonicalLocationCatalog.StoryRelevant.Count,
                Is.EqualTo(23));
            foreach (CanonicalLocationSpec location in
                     CanonicalLocationCatalog.StoryRelevant)
            {
                MapLocationPlacement placement =
                    MapDeckCatalog.Find(location.Code);
                Assert.That(placement, Is.Not.Null, location.Code);
                Assert.That(placement.Deck, Is.EqualTo(location.Deck), location.Code);
            }
        }

        [Test]
        public void EveryPlayableLocation_HasExactlyOneMapPlacement()
        {
            Assert.That(MapDeckCatalog.All.Count, Is.EqualTo(24));
            Assert.That(
                MapDeckCatalog.All.Select(item => item.LocationCode),
                Is.Unique);
            Assert.That(
                MapDeckCatalog.All.Select(item => item.LocationCode),
                Is.EquivalentTo(
                    CanonicalLocationCatalog.Playable.Select(item => item.Code)));
            foreach (CanonicalLocationSpec unused in
                     CanonicalLocationCatalog.Unused)
            {
                Assert.That(
                    MapDeckCatalog.Find(unused.Code),
                    Is.Null,
                    unused.Code);
            }
            Assert.That(
                MapDeckCatalog.Unused.Select(item => item.LocationCode),
                Is.EquivalentTo(
                    CanonicalLocationCatalog.Unused.Select(item => item.Code)));
        }

        [Test]
        public void VipLounge_IsTheOnlySceneLessFastTravelException()
        {
            string[] storyLocationCodes = ProductionSceneCatalog.All
                .Select(scene =>
                    CanonicalLocationCatalog.FindSpec(
                        scene.NarrativeLocationCode)?.Code)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct()
                .ToArray();
            string[] sceneLessFastTravel = MapDeckCatalog.All
                .Where(placement =>
                    placement.TravelTier != MapTravelTier.RouteOnly &&
                    !storyLocationCodes.Contains(placement.LocationCode))
                .Select(placement => placement.LocationCode)
                .ToArray();

            Assert.That(
                sceneLessFastTravel,
                Is.EquivalentTo(new[] { "VIP_LOUNGE" }));
        }

        [Test]
        public void DeckLayerAssets_AreCompleteAndImportedAsSprites()
        {
            foreach (int deck in new[] { 6, 7, 8, 9, 10 })
            {
                foreach (string suffix in
                         new[] { "Base", "Restricted", "Technical" })
                {
                    string path =
                        $"{LayerRoot}/Deck{deck:00}_{suffix}.png";
                    Assert.That(
                        AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(path),
                        Is.Not.Null,
                        path);
                }
            }
        }

        [Test]
        public void PortPassengerLayer_IsImportedAndConnected()
        {
            const string path =
                LayerRoot + "/Port_Base.png";
            Assert.That(
                MapDeckCatalog.ResourceKey(
                    0,
                    MapLayerMode.Passenger),
                Is.EqualTo("Maps/DeckLayers/Port_Base"));
            Assert.That(
                AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(path),
                Is.Not.Null,
                path);
            Assert.That(
                MapDeckCatalog.ResourceKey(
                    0,
                    MapLayerMode.Investigation),
                Is.Empty);
        }

        [Test]
        public void InvestigationAndTechnicalLayers_UnlockWithoutSpoilingEarlierDays()
        {
            Assert.That(
                MapDeckCatalog.IsLayerUnlocked(
                    MapLayerMode.Passenger,
                    Array.Empty<string>(),
                    Array.Empty<string>()),
                Is.True);
            Assert.That(
                MapDeckCatalog.IsLayerUnlocked(
                    MapLayerMode.Investigation,
                    Array.Empty<string>(),
                    new[] { "D1-04" }),
                Is.False);
            Assert.That(
                MapDeckCatalog.IsLayerUnlocked(
                    MapLayerMode.Investigation,
                    new[] { "D1-04" },
                    Array.Empty<string>()),
                Is.True);
            Assert.That(
                MapDeckCatalog.IsLayerUnlocked(
                    MapLayerMode.Technical,
                    new[] { "D6-01" },
                    new[] { "D6-02" }),
                Is.False);
            Assert.That(
                MapDeckCatalog.IsLayerUnlocked(
                    MapLayerMode.Technical,
                    new[] { "D6-02" },
                    Array.Empty<string>()),
                Is.True);
        }

        [Test]
        public void UnusedLowerMachineryLocations_NeverAppearOnActiveMap()
        {
            foreach (CanonicalLocationSpec unused in
                     CanonicalLocationCatalog.Unused)
            {
                Assert.That(MapDeckCatalog.Find(unused.Code), Is.Null);
                Assert.That(
                    SceneTravelPolicy.IsLocationVisibleOnMap(
                        unused.Code,
                        ProductionSceneCatalog.All.Select(
                            scene => scene.SceneId)),
                    Is.False,
                    unused.Code);
            }
        }

        [Test]
        public void RouteOnlyMapRestriction_DoesNotBlockScheduledStoryEntry()
        {
            LocationGraph graph =
                AssetDatabase.LoadAssetAtPath<LocationGraph>(GraphPath);
            LocationDefinition engine =
                graph.FindByCode("ENGINE_CONTROL");

            SceneTravelResult mapTravel =
                SceneTravelPolicy.EvaluateMapTravel(
                    engine,
                    new[] { "P-03", "D5-04" },
                    new[] { "D6-01" },
                    15);
            SceneTravelResult storyTravel =
                SceneTravelPolicy.EvaluateScene(
                    "D6-01",
                    graph,
                    new[] { "D5-04" },
                    15);

            Assert.That(mapTravel.IsAllowed, Is.False);
            Assert.That(
                mapTravel.DenialReason,
                Is.EqualTo(SceneAccessDenialReason.RouteRequired));
            Assert.That(storyTravel.IsAllowed, Is.True);
            Assert.That(
                storyTravel.Location.LocationCode,
                Is.EqualTo("ENGINE_CONTROL"));
        }

        private static void AssertPhysical(
            string narrativeCode,
            string physicalCode,
            int deck)
        {
            CanonicalLocationSpec location =
                CanonicalLocationCatalog.FindSpec(narrativeCode);
            MapLocationPlacement placement =
                MapDeckCatalog.Find(narrativeCode);

            Assert.That(location, Is.Not.Null, narrativeCode);
            Assert.That(location.Code, Is.EqualTo(physicalCode), narrativeCode);
            Assert.That(location.Deck, Is.EqualTo(deck), narrativeCode);
            Assert.That(placement, Is.Not.Null, narrativeCode);
            Assert.That(placement.LocationCode, Is.EqualTo(physicalCode));
            Assert.That(placement.Deck, Is.EqualTo(deck));
        }
    }
}
