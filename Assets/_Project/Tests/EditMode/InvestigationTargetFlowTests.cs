using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Wake.Evidence;
using Wake.Exploration;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class InvestigationTargetFlowTests
    {
        [Test]
        public void PilotTargets_HaveRequiredNormalizedInspectionPoints()
        {
            Assert.That(
                InvestigationTargetCatalog.Pilot.Select(item => item.EvidenceId),
                Is.EqualTo(new[]
                {
                    "C-01", "C-02", "C-03", "C-04", "C-05", "C-07"
                }));

            foreach (InvestigationTargetDefinition target in
                     InvestigationTargetCatalog.Pilot)
            {
                Assert.That(target.Points.Count, Is.GreaterThanOrEqualTo(2));
                Assert.That(target.Points, Has.All.Matches<InspectionPointDefinition>(
                    point =>
                        point.Required &&
                        !string.IsNullOrWhiteSpace(point.PointId) &&
                        !string.IsNullOrWhiteSpace(point.Observation) &&
                        point.NormalizedRect.xMin >= 0f &&
                        point.NormalizedRect.yMin >= 0f &&
                        point.NormalizedRect.xMax <= 1f &&
                        point.NormalizedRect.yMax <= 1f));
            }
        }

        [Test]
        public void AllRequiredPoints_MustBeInspectedBeforeCompletion()
        {
            Assert.That(
                InvestigationTargetCatalog.TryGet(
                    "C-01",
                    out InvestigationTargetDefinition invitation),
                Is.True);
            var inspected = new HashSet<string>();
            Assert.That(invitation.IsComplete(inspected.Contains), Is.False);

            inspected.Add(invitation.Points[0].PointId);
            Assert.That(invitation.IsComplete(inspected.Contains), Is.False);

            foreach (InspectionPointDefinition point in invitation.Points)
                inspected.Add(point.PointId);
            Assert.That(invitation.IsComplete(inspected.Contains), Is.True);
        }

        [Test]
        public void OpeningInvestigation_DoesNotImmediatelyGrantEvidence()
        {
            GameObject canvasObject = new(
                "Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster));
            GameObject inventoryObject = new(
                "Evidence Inventory",
                typeof(EvidenceInventory));
            InvestigationScreenController controller =
                canvasObject.AddComponent<InvestigationScreenController>();
            try
            {
                controller.Initialize(canvasObject.transform);
                Assert.That(controller.Begin("C-01"), Is.True);
                Assert.That(EvidenceInventory.Instance.Contains("C-01"), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
                Object.DestroyImmediate(inventoryObject);
            }
        }

        [Test]
        public void InvestigationOverlay_BlocksInputWithItsOwnRaycaster()
        {
            GameObject canvasObject = new(
                "Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster));
            InvestigationScreenController controller =
                canvasObject.AddComponent<InvestigationScreenController>();
            try
            {
                controller.Initialize(canvasObject.transform);
                Transform root = canvasObject.transform.Find(
                    "Investigation Screen");
                Assert.That(root, Is.Not.Null);
                Canvas overlay = root.GetComponent<Canvas>();
                Assert.That(overlay, Is.Not.Null);
                Assert.That(overlay.overrideSorting, Is.True);
                Assert.That(
                    root.GetComponent<GraphicRaycaster>(),
                    Is.Not.Null);
                Assert.That(
                    root.GetComponent<Image>().raycastTarget,
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }
    }
}
