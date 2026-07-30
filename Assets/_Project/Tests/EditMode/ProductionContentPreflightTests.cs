using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using Wake.Editor;
using Wake.Narrative;

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
                "VOICE_CLIP_MISSING"
            }));
            Assert.That(Find("VOICE_CLIP_MISSING").Message,
                Does.Contain("677개"));
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
                "SERIALIZED_REFERENCE", "TEXT_ENCODING",
                "ASSET_META_MISSING", "ASSET_META_ORPHAN",
                "ASSET_META_GUID_MISSING", "ASSET_META_GUID_DUPLICATE",
                "DEVELOPMENT_PLAN_META"
            };
            Assert.That(codes.Distinct().Count(), Is.EqualTo(13));
        }

        [Test]
        public void CurrentProject_HasStableAssetMetadata()
        {
            Assert.That(
                AssetMetaIntegrityValidator.Validate(),
                Is.Empty);
            Assert.That(
                AssetDatabase.AssetPathToGUID(
                    AssetMetaIntegrityValidator.DevelopmentPlanPath),
                Is.EqualTo(
                    AssetMetaIntegrityValidator.DevelopmentPlanGuid));
        }

        [Test]
        public void PortraitContract_SeparatesCoreExpressionsAndAmbientFallbacks()
        {
            DialoguePortraitDefinition[] expressions =
                DialoguePortraitCatalog.All
                    .Where(item => item.UsesExpressionSprites)
                    .ToArray();
            DialoguePortraitDefinition[] fallbacks =
                DialoguePortraitCatalog.All
                    .Where(item => !item.UsesExpressionSprites)
                    .ToArray();

            Assert.That(expressions, Has.Length.EqualTo(9));
            Assert.That(fallbacks, Has.Length.EqualTo(35));
            Assert.That(
                fallbacks.All(item =>
                    item.FallbackTexture.StartsWith(
                        "AmbientCharacters/")),
                Is.True);
            foreach (DialoguePortraitDefinition portrait in fallbacks)
            {
                string path =
                    $"Assets/_Project/Resources/" +
                    $"{portrait.FallbackTexture}.png";
                Assert.That(
                    AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(
                        path),
                    Is.Not.Null,
                    portrait.CharacterId);
            }
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
