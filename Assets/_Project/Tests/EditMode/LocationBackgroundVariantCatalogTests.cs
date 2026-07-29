using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.Exploration;
using Wake.Narrative;

namespace Wake.Tests
{
    public sealed class LocationBackgroundVariantCatalogTests
    {
        private const string ResourceFolder =
            "Assets/_Project/Resources/LocationBackgroundVariants";

        [Test]
        public void ApprovedSet_RegistersTwentySixUniqueSpriteResources()
        {
            string[] resourceKeys =
                LocationBackgroundVariantCatalog.All
                    .Select(binding => binding.ResourceKey)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray();

            Assert.That(resourceKeys, Has.Length.EqualTo(26));
            foreach (string resourceKey in resourceKeys)
            {
                string fileName =
                    resourceKey[(resourceKey.LastIndexOf('/') + 1)..];
                string assetPath =
                    $"{ResourceFolder}/{fileName}.png";
                Sprite sprite =
                    AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

                Assert.That(sprite, Is.Not.Null, assetPath);
                Assert.That(Resources.Load<Sprite>(resourceKey), Is.SameAs(sprite));
                Assert.That(
                    sprite.rect.width / sprite.rect.height,
                    Is.EqualTo(16f / 9f).Within(0.002f),
                    assetPath);
            }

            string[] diskFiles = Directory
                .GetFiles(ResourceFolder, "*.png")
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            Assert.That(
                diskFiles,
                Is.EquivalentTo(
                    resourceKeys.Select(key =>
                        key[(key.LastIndexOf('/') + 1)..])));
        }

        [Test]
        public void SceneBindings_ReferToTheirCanonicalPlayableLocations()
        {
            foreach (LocationBackgroundVariantBinding binding in
                     LocationBackgroundVariantCatalog.All)
            {
                Assert.That(
                    CanonicalLocationCatalog.IsPlayable(
                        binding.LogicalLocationCode),
                    Is.True,
                    binding.LogicalLocationCode);

                foreach (string sceneId in binding.SceneIds)
                {
                    Assert.That(
                        ProductionSceneCatalog.TryGet(
                            sceneId,
                            out ProductionSceneDefinition scene),
                        Is.True,
                        sceneId);
                    Assert.That(
                        CanonicalLocationCatalog.FindSpec(
                            scene.NarrativeLocationCode)?.Code,
                        Is.EqualTo(binding.LogicalLocationCode),
                        sceneId);
                }
            }
        }

        [TestCase(
            "CABIN_CLAIRE",
            "D5-01",
            "bg_cabin_claire_d5_smoke")]
        [TestCase(
            "CABIN_CLAIRE",
            "D5-02",
            "bg_cabin_claire_d5_dismantled")]
        [TestCase(
            "STAIR_B",
            "D4-02",
            "bg_crew_stairs_d4_wet")]
        [TestCase(
            "CREW_STAIRS",
            "D4-03",
            "bg_crew_stairs_d4_reconstruction")]
        [TestCase(
            "HORIZON",
            "D1-06",
            "bg_horizon_d1_discovery")]
        [TestCase(
            "HORIZON",
            "D2-05",
            "bg_horizon_cleared_day")]
        [TestCase(
            "HORIZON",
            "D8-01",
            "bg_horizon_d8_finale")]
        [TestCase(
            "FORENSIC",
            "D6-04",
            "bg_medbay_forensic")]
        [TestCase(
            "MEDBAY",
            "D7-02",
            "bg_medbay_dna")]
        [TestCase(
            "EVIDENCE_BOARD",
            "D6-05",
            "bg_news_lounge_d6_evidence_board")]
        [TestCase(
            "VAULT",
            "D7-01",
            "bg_vault_d7_damaged")]
        [TestCase(
            "PORT",
            "D8-03",
            "bg_port_d8_epilogue")]
        [TestCase(
            "PROMENADE",
            "D3-05",
            "bg_promenade_d3_night")]
        [TestCase(
            "STERN",
            "D8-02",
            "bg_open_deck_d8_morning")]
        [TestCase(
            "BRIDGE",
            "D3-03",
            "bg_bridge_d3_day")]
        [TestCase(
            "CABIN_DANIEL",
            "D2-06",
            "bg_cabin_daniel_d2_late_afternoon")]
        [TestCase(
            "INTERVIEW",
            "D5-03",
            "bg_interview_d5_day")]
        [TestCase(
            "SERVICE7",
            "D1-04",
            "bg_crew_stairs_default")]
        [TestCase(
            "SERVICE_RAIL",
            "D6-02",
            "bg_service_rail_d6_subtle")]
        [TestCase(
            "BALLAST",
            "D6-03",
            "bg_ballast_annex_d6_subtle")]
        [TestCase(
            "ATRIUM",
            "D1-01",
            "bg_atrium_default_champagne")]
        [TestCase(
            "BALLROOM",
            "D1-03",
            "bg_ballroom_default_mask")]
        [TestCase(
            "GANGWAY",
            "P-02",
            "bg_gangway_default_luggage")]
        public void ResolveResourceKey_UsesApprovedSceneOrLocationVariant(
            string locationCode,
            string sceneId,
            string expectedResourceName)
        {
            Assert.That(
                LocationBackgroundVariantCatalog.ResolveResourceKey(
                    locationCode,
                    sceneId),
                Is.EqualTo(
                    $"{LocationBackgroundVariantCatalog.ResourceRoot}/" +
                    expectedResourceName));
        }

        [Test]
        public void LocationsWithoutApprovedVariants_KeepSerializedBackground()
        {
            Assert.That(
                LocationBackgroundVariantCatalog.ResolveResourceKey(
                    "DINING",
                    "D1-02"),
                Is.Empty);
            Assert.That(
                LocationBackgroundVariantCatalog.ResolveResourceKey(
                    "LAUNDRY",
                    string.Empty),
                Is.Empty);
        }

        [Test]
        public void ResolveSelection_DescribesExactApprovedSemanticVariant()
        {
            LocationDefinition horizon =
                AssetDatabase.LoadAssetAtPath<LocationDefinition>(
                    "Assets/_Project/Content/Locations/" +
                    "LocationDefinition_Horizon.asset");

            LocationBackgroundSelection selection =
                LocationBackgroundVariantCatalog.ResolveSelection(
                    "HORIZON",
                    "D1-06",
                    horizon.BackgroundSprite);

            Assert.That(
                selection.Sprite?.name,
                Is.EqualTo("bg_horizon_d1_discovery"));
            Assert.That(
                selection.VariantKey,
                Is.EqualTo(
                    "LocationBackgroundVariants/" +
                    "bg_horizon_d1_discovery"));
            Assert.That(
                selection.SemanticProfileId,
                Is.EqualTo("bg_horizon_d1_discovery"));
            Assert.That(selection.UsesSerializedFallback, Is.False);
            Assert.That(
                LocationBackgroundVariantCatalog.Resolve(
                    "HORIZON",
                    "D1-06",
                    horizon.BackgroundSprite),
                Is.SameAs(selection.Sprite));
        }

        [Test]
        public void ResolveSelection_IdentifiesSerializedFallbackProfile()
        {
            LocationDefinition dining =
                AssetDatabase.LoadAssetAtPath<LocationDefinition>(
                    "Assets/_Project/Content/Locations/" +
                    "LocationDefinition_DINING.asset");

            LocationBackgroundSelection selection =
                LocationBackgroundVariantCatalog.ResolveSelection(
                    "DINING",
                    "D1-02",
                    dining.BackgroundSprite);

            Assert.That(
                selection.Sprite,
                Is.SameAs(dining.BackgroundSprite));
            Assert.That(
                selection.VariantKey,
                Is.EqualTo(
                    LocationBackgroundVariantCatalog
                        .SerializedVariantPrefix +
                    dining.BackgroundSprite.name));
            Assert.That(
                selection.SemanticProfileId,
                Is.EqualTo(dining.BackgroundSprite.name));
            Assert.That(selection.UsesSerializedFallback, Is.True);
        }

        [TestCase(
            "HORIZON",
            "D1-06",
            "D1-06",
            "bg_horizon_cleared_day")]
        [TestCase(
            "CREW_STAIRS",
            "",
            "D4-02",
            "bg_crew_stairs_d4_wet")]
        [TestCase(
            "CREW_STAIRS",
            "",
            "D4-02,D4-03",
            "bg_crew_stairs_d4_reconstruction")]
        [TestCase(
            "CABIN_CLAIRE",
            "",
            "D5-01,D5-02",
            "bg_cabin_claire_d5_dismantled")]
        [TestCase(
            "MEDBAY",
            "",
            "D4-04,D7-02",
            "bg_medbay_dna")]
        [TestCase(
            "NEWS_LOUNGE",
            "",
            "D6-05",
            "bg_news_lounge_d6_evidence_board")]
        [TestCase(
            "VAULT",
            "",
            "D7-01",
            "bg_vault_d7_damaged")]
        public void CompletedScenes_SelectLatestPersistentRoomState(
            string locationCode,
            string sceneId,
            string completedCsv,
            string expectedResourceName)
        {
            string[] completed = completedCsv.Split(',');

            Assert.That(
                LocationBackgroundVariantCatalog.ResolveResourceKey(
                    locationCode,
                    sceneId,
                    completed),
                Is.EqualTo(
                    $"{LocationBackgroundVariantCatalog.ResourceRoot}/" +
                    expectedResourceName));
        }
    }
}
