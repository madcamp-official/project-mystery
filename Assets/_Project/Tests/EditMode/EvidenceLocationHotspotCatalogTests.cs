using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Wake.Exploration;

namespace Wake.Tests.EditMode
{
    public class EvidenceLocationHotspotCatalogTests
    {
        [Test]
        public void Catalog_CoversEveryCanonicalEvidenceExactlyOnce()
        {
            string[] ids = EvidenceLocationHotspotCatalog.All
                .Select(entry => entry.EvidenceId)
                .OrderBy(id => id)
                .ToArray();

            CollectionAssert.AreEqual(
                Enumerable.Range(1, 18).Select(index => $"C-{index:00}"),
                ids);
        }

        [Test]
        public void Catalog_HotspotRectsStayInsideBackground()
        {
            foreach (EvidenceLocationHotspotSpec entry in
                     EvidenceLocationHotspotCatalog.All)
            {
                Rect rect = entry.NormalizedRect;
                Assert.That(rect.width, Is.GreaterThan(0f), entry.EvidenceId);
                Assert.That(rect.height, Is.GreaterThan(0f), entry.EvidenceId);
                Assert.That(rect.xMin, Is.InRange(0f, 1f), entry.EvidenceId);
                Assert.That(rect.yMin, Is.InRange(0f, 1f), entry.EvidenceId);
                Assert.That(rect.xMax, Is.InRange(0f, 1f), entry.EvidenceId);
                Assert.That(rect.yMax, Is.InRange(0f, 1f), entry.EvidenceId);
            }
        }

        [Test]
        public void Catalog_OnlyEndingEvidenceRequiresAnEnding()
        {
            var gated = EvidenceLocationHotspotCatalog.All
                .Where(entry => !string.IsNullOrEmpty(entry.RequiredEnding))
                .ToArray();

            Assert.That(gated, Has.Length.EqualTo(1));
            Assert.That(gated[0].EvidenceId, Is.EqualTo("C-18"));
            Assert.That(gated[0].RequiredEnding, Is.EqualTo("A"));
        }

        [TestCase("C-06", "BALLAST_CONTROL_ANNEX", "D6-03")]
        [TestCase("C-13", "INTERVIEW", "D5-03")]
        [TestCase("C-15", "MEDBAY", "D4-04")]
        [TestCase("C-16", "MEDBAY", "D7-02")]
        public void Catalog_ApprovedBackgroundEvidenceUsesExpectedScene(
            string evidenceId,
            string expectedLocation,
            string expectedScene)
        {
            EvidenceLocationHotspotSpec entry =
                EvidenceLocationHotspotCatalog.All.Single(
                    candidate => candidate.EvidenceId == evidenceId);

            Assert.That(entry.LocationCode, Is.EqualTo(expectedLocation));
            Assert.That(entry.AvailableFromScene, Is.EqualTo(expectedScene));
        }

        [Test]
        public void PortEvidenceOverlay_UsesTransparentPolygonTarget()
        {
            GameObject root = new(
                "Evidence Overlay Test",
                typeof(RectTransform),
                typeof(EvidenceLocationHotspotOverlay));
            try
            {
                RectTransform content = root.GetComponent<RectTransform>();
                EvidenceLocationHotspotOverlay overlay =
                    root.GetComponent<EvidenceLocationHotspotOverlay>();
                overlay.Initialize(content);
                overlay.Show(
                    "PORT",
                    new LocationBackgroundSelection(
                        null,
                        "serialized:bg_location_port_evidence",
                        string.Empty,
                        usesSerializedFallback: false));

                Transform target = content.Find("EvidenceHotspot_C-01");
                Assert.That(target, Is.Not.Null);
                Assert.That(
                    target.GetComponent<Image>().color,
                    Is.EqualTo(Color.clear));
                Assert.That(
                    target.GetComponent<Button>().transition,
                    Is.EqualTo(Selectable.Transition.None));
                Assert.That(target.GetComponent<Outline>(), Is.Null);
                Assert.That(
                    target.GetComponent<PolygonHotspotRaycastFilter>(),
                    Is.Not.Null);
                Assert.That(
                    target.Find("Interaction Label"),
                    Is.Null);
                Assert.That(
                    target.Find("Accessibility Focus Marker"),
                    Is.Not.Null);
                Assert.That(
                    content.Find("EvidenceHotspot_C-18"),
                    Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EvidenceOverlay_WithoutApprovedPolygonFailsClosed()
        {
            GameObject root = new(
                "Evidence Overlay Test",
                typeof(RectTransform),
                typeof(EvidenceLocationHotspotOverlay));
            try
            {
                RectTransform content = root.GetComponent<RectTransform>();
                EvidenceLocationHotspotOverlay overlay =
                    root.GetComponent<EvidenceLocationHotspotOverlay>();
                overlay.Initialize(content);
                overlay.Show(
                    "PORT",
                    new LocationBackgroundSelection(
                        null,
                        "serialized:unapproved_port_background",
                        string.Empty,
                        usesSerializedFallback: false));

                Assert.That(
                    content.Find("EvidenceHotspot_C-01"),
                    Is.Null);
                Assert.That(
                    content.Find("EvidenceHotspot_C-18"),
                    Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
