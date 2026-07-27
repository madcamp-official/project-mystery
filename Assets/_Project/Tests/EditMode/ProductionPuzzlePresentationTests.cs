using System.Linq;
using NUnit.Framework;
using Wake.Puzzles;
using Wake.UI;

namespace Wake.Tests
{
    public class ProductionPuzzlePresentationTests
    {
        [TestCase("D2-02", ProductionPuzzleCatalog.BloodPattern)]
        [TestCase("d6-02", ProductionPuzzleCatalog.CargoRailBranch)]
        public void Catalog_ResolvesPuzzleByScene(
            string sceneId,
            string expectedPuzzleId)
        {
            Assert.That(
                ProductionPuzzleCatalog.TryGetByScene(
                    sceneId,
                    out ProductionPuzzleDefinition definition),
                Is.True);
            Assert.That(definition.Id, Is.EqualTo(expectedPuzzleId));
        }

        [Test]
        public void BloodSelections_HaveSourceBackedKoreanLabels()
        {
            ProductionPuzzleCatalog.TryGet(
                ProductionPuzzleCatalog.BloodPattern,
                out ProductionPuzzleDefinition definition);

            var views = ProductionPuzzlePresentation.CreateSelections(
                definition,
                new[] { "center_mismatch" },
                0);

            Assert.That(views.Count, Is.EqualTo(3));
            Assert.That(views.Select(item => item.Label), Is.EqualTo(new[]
            {
                "비산혈 없음",
                "혈흔 중심 불일치",
                "수직 낙하 흔적"
            }));
            Assert.That(views.Single(item => item.IsSelected).Id,
                Is.EqualTo("center_mismatch"));
        }

        [Test]
        public void Selection_ExposesTextStateBeyondColor()
        {
            ProductionPuzzleCatalog.TryGet(
                ProductionPuzzleCatalog.CargoRailBranch,
                out ProductionPuzzleDefinition definition);
            var views = ProductionPuzzlePresentation.CreateSelections(
                definition,
                new[] { "weight_86kg" },
                0);

            Assert.That(
                views.Single(item => item.Id == "weight_86kg").AccessibleLabel,
                Does.StartWith("선택됨:"));
            Assert.That(
                views.Where(item => item.Id != "weight_86kg")
                    .All(item => item.AccessibleLabel.StartsWith("선택 안 됨:")),
                Is.True);
        }

        [Test]
        public void HintLevels_ExposeObjectiveEvidenceAndFiltering()
        {
            ProductionPuzzleCatalog.TryGet(
                ProductionPuzzleCatalog.CargoRailBranch,
                out ProductionPuzzleDefinition definition);

            Assert.That(
                ProductionPuzzlePresentation.GetHint(definition, 1),
                Does.Contain("22:18"));
            Assert.That(
                ProductionPuzzlePresentation.GetHint(definition, 2),
                Does.Contain("3개"));
            Assert.That(
                ProductionPuzzlePresentation.GetHint(definition, 2),
                Does.Not.Match(@"\bC-\d{2}\b"));
            Assert.That(
                ProductionPuzzlePresentation.GetHint(definition, 3),
                Does.Contain("비활성화"));
        }

        [Test]
        public void ThirdHint_LeavesRequiredSelectionsAvailable()
        {
            ProductionPuzzleCatalog.TryGet(
                ProductionPuzzleCatalog.BloodPattern,
                out ProductionPuzzleDefinition definition);

            var views = ProductionPuzzlePresentation.CreateSelections(
                definition,
                null,
                3);

            Assert.That(views.All(item => item.IsRequired), Is.True);
            Assert.That(views.All(item => item.IsAvailable), Is.True);
        }

        [Test]
        public void UnknownScene_DoesNotInventPuzzleContent()
        {
            Assert.That(
                ProductionPuzzleCatalog.TryGetByScene("D6-05", out _),
                Is.False);
        }
    }
}
