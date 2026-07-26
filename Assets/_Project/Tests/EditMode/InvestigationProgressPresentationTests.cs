using System.Linq;
using NUnit.Framework;
using Wake.Narrative;
using Wake.UI;

namespace Wake.Tests
{
    public sealed class InvestigationProgressPresentationTests
    {
        [Test]
        public void EmptyProgress_UsesOfficialFortyOneSceneTotal()
        {
            InvestigationProgressView view =
                InvestigationProgressPresentation.Create(
                    System.Array.Empty<string>(),
                    ProductionSceneCatalog.All.Select(scene => scene.SceneId));

            Assert.That(view.Completed, Is.Zero);
            Assert.That(view.Total, Is.EqualTo(41));
            Assert.That(view.Normalized, Is.Zero);
            Assert.That(view.IsComplete, Is.False);
            Assert.That(view.Label, Is.EqualTo("수사 진행  0/41"));
        }

        [Test]
        public void Progress_CountsOnlyRegisteredScenes()
        {
            InvestigationProgressView view =
                InvestigationProgressPresentation.Create(
                    new[] { "P-01", "P-02", "UNKNOWN" },
                    ProductionSceneCatalog.All.Select(scene => scene.SceneId));

            Assert.That(view.Completed, Is.EqualTo(2));
            Assert.That(view.Total, Is.EqualTo(41));
            Assert.That(view.Normalized, Is.EqualTo(2f / 41f));
            Assert.That(view.Label, Is.EqualTo("수사 진행  2/41"));
        }

        [Test]
        public void Progress_NormalizesCaseWhitespaceAndDuplicates()
        {
            InvestigationProgressView view =
                InvestigationProgressPresentation.Create(
                    new[] { " p-01 ", "P-01", "p-02", "", null },
                    new[] { "P-01", "P-02", "P-03" });

            Assert.That(view.Completed, Is.EqualTo(2));
            Assert.That(view.Total, Is.EqualTo(3));
            Assert.That(view.Normalized, Is.EqualTo(2f / 3f));
        }

        [Test]
        public void CompleteProgress_IsReportedWithoutTheorySlots()
        {
            string[] scenes = ProductionSceneCatalog.All
                .Select(scene => scene.SceneId)
                .ToArray();

            InvestigationProgressView view =
                InvestigationProgressPresentation.Create(scenes, scenes);

            Assert.That(view.Completed, Is.EqualTo(41));
            Assert.That(view.Total, Is.EqualTo(41));
            Assert.That(view.Normalized, Is.EqualTo(1f));
            Assert.That(view.IsComplete, Is.True);
            Assert.That(view.Label, Is.EqualTo("수사 진행  41/41"));
        }

        [Test]
        public void MissingExpectedCatalog_ProducesSafeZeroView()
        {
            InvestigationProgressView view =
                InvestigationProgressPresentation.Create(
                    new[] { "P-01" },
                    null);

            Assert.That(view.Completed, Is.Zero);
            Assert.That(view.Total, Is.Zero);
            Assert.That(view.Normalized, Is.Zero);
            Assert.That(view.IsComplete, Is.False);
            Assert.That(view.Label, Is.EqualTo("수사 진행  0/0"));
        }

        [Test]
        public void OfficialCatalog_ContainsUniqueSceneIds()
        {
            string[] sceneIds = ProductionSceneCatalog.All
                .Select(scene => scene.SceneId)
                .ToArray();

            Assert.That(sceneIds, Has.Length.EqualTo(41));
            Assert.That(
                sceneIds.Distinct(System.StringComparer.Ordinal).Count(),
                Is.EqualTo(41));
        }
    }
}
