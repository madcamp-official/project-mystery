using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.Exploration;

namespace Wake.Tests
{
    public sealed class BackgroundInteractionShapeTests
    {
        private static readonly IReadOnlyDictionary<string, string>
            SourceAssets =
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["bg_location_port_evidence"] =
                        "Assets/_Project/Art/Backgrounds/Locations/" +
                        "bg_location_port_evidence.png",
                    ["bg_port_d8_epilogue"] =
                        "Assets/_Project/Resources/" +
                        "LocationBackgroundVariants/bg_port_d8_epilogue.png",
                    ["bg_horizon_d1_discovery"] =
                        "Assets/_Project/Resources/" +
                        "LocationBackgroundVariants/" +
                        "bg_horizon_d1_discovery.png",
                    ["bg_horizon_cleared_day"] =
                        "Assets/_Project/Resources/" +
                        "LocationBackgroundVariants/" +
                        "bg_horizon_cleared_day.png",
                    ["bg_horizon_d8_finale"] =
                        "Assets/_Project/Resources/" +
                        "LocationBackgroundVariants/" +
                        "bg_horizon_d8_finale.png",
                    ["bg_ballast_annex_d6_subtle"] =
                        "Assets/_Project/Resources/" +
                        "LocationBackgroundVariants/" +
                        "bg_ballast_annex_d6_subtle.png",
                    ["bg_location_d8_3_security_evidence"] =
                        "Assets/_Project/Art/Backgrounds/Locations/" +
                        "bg_location_d8_3_security_evidence.png",
                    ["bg_location_d7_3_engine_control_evidence"] =
                        "Assets/_Project/Art/Backgrounds/Locations/" +
                        "bg_location_d7_3_engine_control_evidence.png",
                    ["bg_service_rail_d6_subtle"] =
                        "Assets/_Project/Resources/" +
                        "LocationBackgroundVariants/" +
                        "bg_service_rail_d6_subtle.png",
                    ["bg_medbay_baseline"] =
                        "Assets/_Project/Resources/" +
                        "LocationBackgroundVariants/bg_medbay_baseline.png",
                    ["bg_medbay_forensic"] =
                        "Assets/_Project/Resources/" +
                        "LocationBackgroundVariants/bg_medbay_forensic.png",
                    ["bg_medbay_dna"] =
                        "Assets/_Project/Resources/" +
                        "LocationBackgroundVariants/bg_medbay_dna.png",
                    ["bg_interview_d5_day"] =
                        "Assets/_Project/Resources/" +
                        "LocationBackgroundVariants/bg_interview_d5_day.png",
                    ["bg_promenade_d3_night"] =
                        "Assets/_Project/Resources/" +
                        "LocationBackgroundVariants/" +
                        "bg_promenade_d3_night.png",
                    ["bg_location_d9_3_promenade_evidence"] =
                        "Assets/_Project/Art/Backgrounds/Locations/" +
                        "bg_location_d9_3_promenade_evidence.png",
                    ["bg_location_d6_2_archive"] =
                        "Assets/_Project/Art/Backgrounds/Locations/" +
                        "bg_location_d6_2_archive.png",
                    ["bg_atrium_default_champagne"] =
                        "Assets/_Project/Resources/" +
                        "LocationBackgroundVariants/" +
                        "bg_atrium_default_champagne.png",
                    ["bg_location_d10_2_vip_lounge"] =
                        "Assets/_Project/Art/Backgrounds/Locations/" +
                        "bg_location_d10_2_vip_lounge.png",
                    ["bg_news_lounge_d3"] =
                        "Assets/_Project/Resources/" +
                        "LocationBackgroundVariants/bg_news_lounge_d3.png",
                    ["bg_news_lounge_d6_evidence_board"] =
                        "Assets/_Project/Resources/" +
                        "LocationBackgroundVariants/" +
                        "bg_news_lounge_d6_evidence_board.png",
                    ["bg_gangway_default_luggage"] =
                        "Assets/_Project/Resources/" +
                        "LocationBackgroundVariants/" +
                        "bg_gangway_default_luggage.png",
                    ["bg_ballroom_default_mask"] =
                        "Assets/_Project/Resources/" +
                        "LocationBackgroundVariants/" +
                        "bg_ballroom_default_mask.png",
                    ["bg_location_d10_3_open_deck"] =
                        "Assets/_Project/Art/Backgrounds/Locations/" +
                        "bg_location_d10_3_open_deck.png",
                    ["bg_open_deck_d8_morning"] =
                        "Assets/_Project/Resources/" +
                        "LocationBackgroundVariants/" +
                        "bg_open_deck_d8_morning.png"
                };

        private static readonly Vector2[] ConcavePolygon =
        {
            new(0f, 0f),
            new(1f, 0f),
            new(1f, 1f),
            new(.5f, .5f),
            new(0f, 1f)
        };

        [Test]
        public void PolygonContains_UsesContourAndIncludesBoundary()
        {
            Assert.That(
                BackgroundInteractionPolygonUtility.Contains(
                    ConcavePolygon,
                    new Vector2(.25f, .75f)),
                Is.True);
            Assert.That(
                BackgroundInteractionPolygonUtility.Contains(
                    ConcavePolygon,
                    new Vector2(.75f, .75f)),
                Is.True);
            Assert.That(
                BackgroundInteractionPolygonUtility.Contains(
                    ConcavePolygon,
                    new Vector2(.5f, .75f)),
                Is.False);
            Assert.That(
                BackgroundInteractionPolygonUtility.Contains(
                    ConcavePolygon,
                    new Vector2(.5f, .5f)),
                Is.True);
        }

        [Test]
        public void PolygonSelfIntersects_DetectsBowTie()
        {
            Vector2[] bowTie =
            {
                new(0f, 0f),
                new(1f, 1f),
                new(0f, 1f),
                new(1f, 0f)
            };

            Assert.That(
                BackgroundInteractionPolygonUtility.SelfIntersects(bowTie),
                Is.True);
            Assert.That(
                BackgroundInteractionPolygonUtility.SelfIntersects(
                    ConcavePolygon),
                Is.False);
        }

        [Test]
        public void Shape_ConvertsBackgroundPolygonToBoundsLocalSpace()
        {
            var shape = new BackgroundInteractionShape(
                "OBJECT",
                "PORT",
                "serialized:bg_test.png",
                new string('a', 64),
                true,
                new Rect(.2f, .3f, .4f, .2f),
                new[]
                {
                    new Vector2(.2f, .3f),
                    new Vector2(.6f, .3f),
                    new Vector2(.4f, .5f)
                },
                new Vector2(.4f, .4f));

            Assert.That(shape.BackgroundVariantKey, Is.EqualTo("bg_test"));
            Assert.That(shape.LocalPolygon[0], Is.EqualTo(Vector2.zero));
            Assert.That(shape.LocalPolygon[1], Is.EqualTo(Vector2.right));
            Assert.That(shape.LocalPolygon[2].x, Is.EqualTo(.5f).Within(.0001f));
            Assert.That(shape.LocalPolygon[2].y, Is.EqualTo(1f).Within(.0001f));
            Assert.That(
                shape.LocalLabelAnchor.x,
                Is.EqualTo(.5f).Within(.0001f));
            Assert.That(
                shape.LocalLabelAnchor.y,
                Is.EqualTo(.5f).Within(.0001f));
            Assert.That(shape.Validate(out string diagnostic), Is.True);
            Assert.That(diagnostic, Is.Empty);
        }

        [Test]
        public void Catalog_ResolvesVariantAliasesAndPresence()
        {
            Assert.That(
                BackgroundInteractionShapeCatalog.TryGet(
                    "prop_brochure",
                    "port",
                    "serialized:bg_location_port_evidence",
                    out BackgroundInteractionShape brochure),
                Is.True);
            Assert.That(brochure.IsPresent, Is.True);
            Assert.That(brochure.LocalPolygon.Count, Is.GreaterThanOrEqualTo(3));

            Assert.That(
                BackgroundInteractionShapeCatalog.TryGet(
                    "PROP_BROCHURE",
                    "PORT",
                    "LocationBackgroundVariants/bg_port_d8_epilogue.png",
                    out BackgroundInteractionShape hiddenBrochure),
                Is.True);
            Assert.That(hiddenBrochure.IsPresent, Is.False);
        }

        [Test]
        public void Catalog_SeparatesPortEvidenceByBackgroundVariant()
        {
            Assert.That(
                BackgroundInteractionShapeCatalog.TryGet(
                    "C-01",
                    "PORT",
                    "bg_location_port_evidence",
                    out BackgroundInteractionShape baseEvidence),
                Is.True);
            Assert.That(baseEvidence.IsPresent, Is.True);

            Assert.That(
                BackgroundInteractionShapeCatalog.TryGet(
                    "C-01",
                    "PORT",
                    "bg_port_d8_epilogue",
                    out BackgroundInteractionShape hiddenEvidence),
                Is.True);
            Assert.That(hiddenEvidence.IsPresent, Is.False);

            Assert.That(
                BackgroundInteractionShapeCatalog.TryGet(
                    "C-18",
                    "PORT",
                    "bg_port_d8_epilogue",
                    out BackgroundInteractionShape epilogueEvidence),
                Is.True);
            Assert.That(epilogueEvidence.IsPresent, Is.True);
        }

        [TestCase(
            "C-09",
            "ENGINE_CONTROL",
            "serialized:bg_location_d7_3_engine_control_evidence")]
        [TestCase(
            "C-11",
            "MEDBAY",
            "LocationBackgroundVariants/bg_medbay_forensic")]
        public void StoryEvidence_WithVisibleBackgroundObjectHasPolygon(
            string evidenceId,
            string locationCode,
            string variantKey)
        {
            Assert.That(
                BackgroundInteractionShapeCatalog.TryGet(
                    evidenceId,
                    locationCode,
                    variantKey,
                    out BackgroundInteractionShape shape),
                Is.True);
            Assert.That(shape.IsPresent, Is.True);
            Assert.That(
                shape.NormalizedPolygon.Count,
                Is.GreaterThanOrEqualTo(3));
        }

        [TestCase(
            "C-03",
            "HORIZON",
            "LocationBackgroundVariants/bg_horizon_cleared_day")]
        [TestCase(
            "C-03",
            "HORIZON",
            "LocationBackgroundVariants/bg_horizon_d8_finale")]
        [TestCase(
            "C-15",
            "MEDBAY",
            "LocationBackgroundVariants/bg_medbay_forensic")]
        [TestCase(
            "C-15",
            "MEDBAY",
            "LocationBackgroundVariants/bg_medbay_dna")]
        public void StoryEvidence_WithoutMatchingBackgroundObjectIsExplicitlyHidden(
            string evidenceId,
            string locationCode,
            string variantKey)
        {
            Assert.That(
                BackgroundInteractionShapeCatalog.TryGet(
                    evidenceId,
                    locationCode,
                    variantKey,
                    out BackgroundInteractionShape shape),
                Is.True);
            Assert.That(shape.IsPresent, Is.False);
            Assert.That(shape.NormalizedPolygon, Is.Empty);
        }

        [Test]
        public void Catalog_SelectionValidatesCurrentSourceSprite()
        {
            const string path =
                "Assets/_Project/Art/Backgrounds/Locations/" +
                "bg_location_port_evidence.png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Assert.That(sprite, Is.Not.Null);

            var selection = new LocationBackgroundSelection(
                sprite,
                "serialized:bg_location_port_evidence",
                "bg_location_port_evidence",
                usesSerializedFallback: true);
            Assert.That(
                BackgroundInteractionShapeCatalog.TryGet(
                    "PROP_BROCHURE",
                    "PORT",
                    selection,
                    out BackgroundInteractionShape shape),
                Is.True);
            Assert.That(shape.SourceImageHash, Is.Not.Empty);
        }

        [Test]
        public void Catalog_AllShapesAreValidAndUnique()
        {
            Assert.That(
                BackgroundInteractionShapeCatalog.Validate(
                    out IReadOnlyList<string> diagnostics),
                Is.True,
                string.Join("\n", diagnostics));
        }

        [Test]
        public void Catalog_CoversEveryEvidenceAndInspectableObject()
        {
            string[] missingEvidence =
                EvidenceLocationHotspotCatalog.All
                    .Where(item =>
                        !BackgroundInteractionShapeCatalog
                            .HasAuthoredObject(
                                item.EvidenceId,
                                item.LocationCode))
                    .Select(item =>
                        $"{item.LocationCode}/{item.EvidenceId}")
                    .ToArray();
            string[] missingInspectables =
                AmbientInspectableCatalog.All
                    .Where(item =>
                        !BackgroundInteractionShapeCatalog
                            .HasAuthoredObject(
                                item.Id,
                                item.Location))
                    .Select(item => $"{item.Location}/{item.Id}")
                    .ToArray();

            Assert.That(
                missingEvidence,
                Is.Empty,
                string.Join(", ", missingEvidence));
            Assert.That(
                missingInspectables,
                Is.Empty,
                string.Join(", ", missingInspectables));
        }

        [Test]
        public void Catalog_SourceHashesMatchCurrentBackgroundFiles()
        {
            string repositoryRoot =
                Directory.GetParent(Application.dataPath)?.FullName ??
                string.Empty;
            var actualByVariant =
                new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (BackgroundInteractionShape shape in
                     BackgroundInteractionShapeCatalog.All)
            {
                Assert.That(
                    SourceAssets.TryGetValue(
                        shape.BackgroundVariantKey,
                        out string relativePath),
                    Is.True,
                    shape.BackgroundVariantKey);
                if (!actualByVariant.TryGetValue(
                        shape.BackgroundVariantKey,
                        out string actualHash))
                {
                    string fullPath =
                        Path.Combine(repositoryRoot, relativePath);
                    using SHA256 sha = SHA256.Create();
                    using FileStream stream = File.OpenRead(fullPath);
                    actualHash = BitConverter.ToString(
                            sha.ComputeHash(stream))
                        .Replace("-", string.Empty)
                        .ToLowerInvariant();
                    actualByVariant[shape.BackgroundVariantKey] =
                        actualHash;
                }

                Assert.That(
                    shape.SourceImageHash,
                    Is.EqualTo(actualHash),
                    $"{shape.ObjectId}/{shape.BackgroundVariantKey}");
            }
        }

        [Test]
        public void RaycastFilter_RejectsPointInsideBoundsButOutsidePolygon()
        {
            GameObject target = new(
                "Polygon Target",
                typeof(RectTransform));
            try
            {
                RectTransform rect = target.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(100f, 100f);
                PolygonHotspotRaycastFilter filter =
                    target.AddComponent<PolygonHotspotRaycastFilter>();
                filter.Configure(new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(0f, 1f)
                });

                Assert.That(
                    filter.ContainsNormalized(new Vector2(.2f, .2f)),
                    Is.True);
                Assert.That(
                    filter.ContainsNormalized(new Vector2(.8f, .8f)),
                    Is.False);
                Assert.That(
                    filter.IsRaycastLocationValid(
                        rect.TransformPoint(new Vector2(-30f, -30f)),
                        null),
                    Is.True);
                Assert.That(
                    filter.IsRaycastLocationValid(
                        rect.TransformPoint(new Vector2(30f, 30f)),
                        null),
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
