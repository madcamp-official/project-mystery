using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Wake.Exploration;

namespace Wake.Tests
{
    public class NarrativeInvestigationCatalogTests
    {
        [Test]
        public void PortMessenger_HasThreeImageObservations()
        {
            Assert.That(
                NarrativeInvestigationCatalog.TryGet(
                    NarrativeInvestigationCatalog.PortMessengerTargetId,
                    out NarrativeInvestigationDefinition target),
                Is.True);
            Assert.That(target.SceneId, Is.EqualTo("P-01"));
            Assert.That(target.LocationCode, Is.EqualTo("PORT"));
            Assert.That(target.ResourcePath, Is.Not.Empty);
            Assert.That(target.ImageText, Does.Contain("21시"));
            Assert.That(target.Points.Count, Is.EqualTo(3));
            Assert.That(
                target.IsComplete(pointId =>
                    pointId is "ANONYMOUS_SENDER" or
                        "MEETING_TIME" or
                        "MEETING_CONDITION"),
                Is.True);
        }

        [TestCase(
            NarrativeInvestigationCatalog.GangwayManifestTargetId,
            "p02_boarding_manifest_inspected",
            3)]
        [TestCase(
            NarrativeInvestigationCatalog.GangwaySignatureTargetId,
            "p02_electronic_signature_inspected",
            2)]
        public void GangwayTargets_AreDirectImageInvestigations(
            string targetId,
            string completionFlag,
            int pointCount)
        {
            Assert.That(
                NarrativeInvestigationCatalog.TryGet(
                    targetId,
                    out NarrativeInvestigationDefinition target),
                Is.True);
            Assert.That(target.SceneId, Is.EqualTo("P-02"));
            Assert.That(target.LocationCode, Is.EqualTo("GANGWAY"));
            Assert.That(
                target.ResourcePath,
                Is.EqualTo(
                    "LocationBackgroundVariants/" +
                    "bg_gangway_default_luggage"));
            Assert.That(target.CompletionFlag, Is.EqualTo(completionFlag));
            Assert.That(target.Points.Count, Is.EqualTo(pointCount));
        }

        [Test]
        public void GangwayTargets_UseDistinctAuthoredPolygons()
        {
            Assert.That(
                BackgroundInteractionShapeCatalog.TryGet(
                    NarrativeInvestigationCatalog.GangwayManifestTargetId,
                    "GANGWAY",
                    "bg_gangway_default_luggage",
                    out BackgroundInteractionShape manifest),
                Is.True);
            Assert.That(
                BackgroundInteractionShapeCatalog.TryGet(
                    NarrativeInvestigationCatalog.GangwaySignatureTargetId,
                    "GANGWAY",
                    "bg_gangway_default_luggage",
                    out BackgroundInteractionShape signature),
                Is.True);
            Assert.That(manifest.IsPresent, Is.True);
            Assert.That(signature.IsPresent, Is.True);
            Assert.That(
                manifest.NormalizedBounds.Overlaps(
                    signature.NormalizedBounds),
                Is.False);
        }

        [Test]
        public void GangwayOverlay_CreatesDirectPolygonButtonsForBothTargets()
        {
            GameObject root = new(
                "Narrative Investigation Overlay Test",
                typeof(RectTransform),
                typeof(NarrativeInvestigationHotspotOverlay));
            try
            {
                RectTransform content = root.GetComponent<RectTransform>();
                NarrativeInvestigationHotspotOverlay overlay =
                    root.GetComponent<NarrativeInvestigationHotspotOverlay>();
                overlay.Initialize(content);
                overlay.Show(
                    "GANGWAY",
                    "P-02",
                    new LocationBackgroundSelection(
                        null,
                        "bg_gangway_default_luggage",
                        string.Empty,
                        usesSerializedFallback: false));

                AssertDirectPolygonButton(
                    content,
                    NarrativeInvestigationCatalog
                        .GangwayManifestTargetId);
                AssertDirectPolygonButton(
                    content,
                    NarrativeInvestigationCatalog
                        .GangwaySignatureTargetId);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void AssertDirectPolygonButton(
            Transform content,
            string targetId)
        {
            Transform target = content.Find(
                $"NarrativeInvestigationHotspot_{targetId}");
            Assert.That(target, Is.Not.Null);
            Assert.That(target.GetComponent<Button>(), Is.Not.Null);
            Assert.That(
                target.GetComponent<Image>().raycastTarget,
                Is.True);
            Assert.That(
                target.GetComponent<PolygonHotspotRaycastFilter>(),
                Is.Not.Null);
        }
    }
}
