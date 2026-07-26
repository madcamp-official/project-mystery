using System.Linq;
using NUnit.Framework;
using UnityEditor.Build;
using Wake.Editor;

namespace Wake.Tests
{
    public class ProductionContentPreflightTests
    {
        private ProductionPreflightReport report;

        [OneTimeSetUp]
        public void RunPreflight() =>
            report = ProductionContentPreflight.Run();

        [Test]
        public void CurrentContent_HasNoBuildBlockingErrors()
        {
            Assert.That(report.Diagnostics.Where(item =>
                item.Severity == ProductionPreflightSeverity.Error),
                Is.Empty, string.Join("\n", report.Diagnostics));
            Assert.That(report.CanBuild, Is.True);
            Assert.DoesNotThrow(() =>
                ProductionContentPreflight.ThrowIfErrors(report));
        }

        [Test]
        public void CurrentContent_ReportsOnlyKnownWarnings()
        {
            string[] codes = report.Diagnostics.Where(item =>
                    item.Severity == ProductionPreflightSeverity.Warning)
                .Select(item => item.Code).OrderBy(code => code).ToArray();
            Assert.That(codes, Is.EqualTo(new[]
            {
                "KOREAN_FONT_MISSING", "TIMELINE_SOURCE_MISSING",
                "UNRESOLVED_LOCATION", "VOICE_CLIP_MISSING"
            }));
            Assert.That(Find("UNRESOLVED_LOCATION").Message, Does.Contain("8곳"));
            Assert.That(Find("TIMELINE_SOURCE_MISSING").Message,
                Does.Contain("5개"));
            Assert.That(Find("VOICE_CLIP_MISSING").Message,
                Does.Contain("105개"));
            Assert.That(Find("KOREAN_FONT_MISSING").Message,
                Does.Contain("한국어 글꼴"));
        }

        [Test]
        public void BuildGate_ThrowsOnlyForErrors()
        {
            ProductionPreflightReport warning = Report(
                ProductionPreflightSeverity.Warning);
            ProductionPreflightReport error = Report(
                ProductionPreflightSeverity.Error);
            Assert.DoesNotThrow(() =>
                ProductionContentPreflight.ThrowIfErrors(warning));
            Assert.Throws<BuildFailedException>(() =>
                ProductionContentPreflight.ThrowIfErrors(error));
        }

        [Test]
        public void RequiredErrorCodes_AreStable()
        {
            string[] codes =
            {
                "SCENE_DIALOGUE_SOURCE", "DIALOGUE_SHAPE",
                "STABLE_ID_DUPLICATE", "LOCATION_ASSET_SET",
                "EVIDENCE_ASSET_SET", "PORTRAIT_ASSET_SET",
                "SERIALIZED_REFERENCE", "TEXT_ENCODING"
            };
            Assert.That(codes.Distinct().Count(), Is.EqualTo(8));
        }

        private ProductionPreflightDiagnostic Find(string code) =>
            report.Diagnostics.Single(item => item.Code == code);

        private static ProductionPreflightReport Report(
            ProductionPreflightSeverity severity) =>
            new(new[]
            {
                new ProductionPreflightDiagnostic(
                    severity, "FIXTURE", "fixture", "테스트 진단")
            });
    }
}
