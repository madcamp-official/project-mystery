using System.Linq;
using NUnit.Framework;
using Wake.Narrative;

namespace Wake.Tests
{
    public sealed class ProductionSceneReferenceTests
    {
        [TestCase("D8-03_A")]
        [TestCase("D8-03_B")]
        [TestCase("D8-03_C")]
        [TestCase("D8-03_BAD")]
        [TestCase(" d8-03_c ")]
        public void Normalize_MapsRouteEpiloguesToOfficialScene(
            string source)
        {
            Assert.That(
                ProductionSceneReference.Normalize(source),
                Is.EqualTo("D8-03"));
        }

        [TestCase("P-01", "P-01")]
        [TestCase(" d2-04 ", "D2-04")]
        [TestCase("", "")]
        [TestCase(null, "")]
        public void Normalize_NormalizesRegularSceneIds(
            string source,
            string expected)
        {
            Assert.That(
                ProductionSceneReference.Normalize(source),
                Is.EqualTo(expected));
        }

        [TestCase("D8-03_A", true)]
        [TestCase("D8-03_C", true)]
        [TestCase("D8-03_BAD", true)]
        [TestCase("D8-03", false)]
        [TestCase("D7-04", false)]
        public void IsRouteSpecificEpilogue_RecognizesOnlyAliases(
            string source,
            bool expected)
        {
            Assert.That(
                ProductionSceneReference.IsRouteSpecificEpilogue(source),
                Is.EqualTo(expected));
        }

        [Test]
        public void NormalizeDistinct_RemovesAliasesAndDuplicates()
        {
            var result = ProductionSceneReference.NormalizeDistinct(new[]
            {
                "D8-02",
                "D8-03_C",
                "D8-03",
                "d8-03_bad",
                " ",
                null
            });

            Assert.That(result, Is.EqualTo(new[] { "D8-02", "D8-03" }));
        }

        [Test]
        public void NormalizeDistinct_PreservesFirstSeenOrder()
        {
            var result = ProductionSceneReference.NormalizeDistinct(new[]
            {
                "D3-04",
                "D1-02",
                "D3-04",
                "D2-06"
            });

            Assert.That(
                result.ToArray(),
                Is.EqualTo(new[] { "D3-04", "D1-02", "D2-06" }));
        }

        [Test]
        public void NormalizeDistinct_AcceptsNullCollection()
        {
            Assert.That(
                ProductionSceneReference.NormalizeDistinct(null),
                Is.Empty);
        }
    }
}
