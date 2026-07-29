using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wake.Editor;
using Wake.Exploration;

namespace Wake.Tests
{
    public sealed class ApprovedBackgroundSemanticCatalogBakerTests
    {
        [Test]
        public void ApprovedCatalog_MatchesManifestSourcesAndBaselines()
        {
            var errors =
                ApprovedBackgroundSemanticCatalogBaker
                    .ValidateProject();

            Assert.That(
                errors,
                Is.Empty,
                string.Join(Environment.NewLine, errors));

            ApprovedBackgroundSemanticCatalog catalog =
                AssetDatabase.LoadAssetAtPath<
                    ApprovedBackgroundSemanticCatalog>(
                    ApprovedBackgroundSemanticCatalogBaker
                        .CatalogAssetPath);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.IsUsable, Is.True);
            Assert.That(catalog.Bindings, Is.Not.Empty);
            Assert.That(catalog.SceneLayouts, Is.Not.Empty);
            Assert.That(
                catalog.Bindings.All(binding =>
                    binding != null &&
                    binding.IsApproved &&
                    binding.SourceSprite != null),
                Is.True);
        }

        [Test]
        public void ApprovedCatalog_ExplicitlyExcludesUnusedLocations()
        {
            ApprovedBackgroundSemanticCatalog catalog =
                AssetDatabase.LoadAssetAtPath<
                    ApprovedBackgroundSemanticCatalog>(
                    ApprovedBackgroundSemanticCatalogBaker
                        .CatalogAssetPath);
            Assert.That(catalog, Is.Not.Null);

            string[] unused =
            {
                "LAUNDRY",
                "SERVICE_HUB",
                "STABILIZERS",
                "BALLAST_TANKS",
                "GENERATOR",
                "WORKSHOP"
            };
            Assert.That(
                catalog.Bindings.Any(binding =>
                    unused.Contains(
                        binding.LocationCode,
                        StringComparer.OrdinalIgnoreCase)),
                Is.False);
        }

        [Test]
        public void DeckSevenScene_ResolvesFixedLayoutAtPhysicalCrewStairsAlias()
        {
            ApprovedBackgroundSemanticCatalog catalog =
                AssetDatabase.LoadAssetAtPath<
                    ApprovedBackgroundSemanticCatalog>(
                    ApprovedBackgroundSemanticCatalogBaker
                        .CatalogAssetPath);
            Assert.That(catalog, Is.Not.Null);
            ApprovedBackgroundSemanticBinding binding =
                catalog.Bindings.Single(value =>
                    value.LocationCode == "CREW_STAIRS" &&
                    value.VariantKey ==
                    "LocationBackgroundVariants/" +
                    "bg_crew_stairs_default");

            ApprovedBackgroundSemanticResolver.SetCatalogForTests(
                catalog);
            try
            {
                bool found =
                    ApprovedBackgroundSemanticResolver.TryResolve(
                        "CREW_STAIRS",
                        binding.VariantKey,
                        binding.SourceSprite,
                        "D1-04",
                        binding.SourceImageHash,
                        ApprovedBackgroundSemanticCatalogBaker
                            .ComputeCurrentCastFingerprint("D1-04"),
                        out BackgroundSemanticRuntimeResolution
                            resolution);

                Assert.That(found, Is.True);
                Assert.That(resolution, Is.Not.Null);
                Assert.That(
                    resolution.HasFixedSceneLayout,
                    Is.True);
                Assert.That(
                    resolution.SceneLayout.LocationCode,
                    Is.EqualTo("CREW_STAIRS"));
                Assert.That(
                    resolution.SceneLayout.Assignments.Select(
                        value => value.CharacterId),
                    Does.Contain("CREW_ATTENDANT"));
                Assert.That(
                    resolution.SceneLayout.Assignments.Select(
                        value => value.CharacterId),
                    Does.Not.Contain("DANIEL"));
            }
            finally
            {
                ApprovedBackgroundSemanticResolver
                    .ResetCacheForTests();
            }
        }

        [Test]
        public void ScreenshotValidation_ReportsChangedImageHash()
        {
            string projectRoot =
                Directory.GetParent(Application.dataPath)?.FullName ??
                Directory.GetCurrentDirectory();
            string relativeFolder =
                "Temp/WakeSemanticBaselineHashTest";
            string fullFolder =
                Path.Combine(projectRoot, relativeFolder);
            string imagePath =
                Path.Combine(fullFolder, "scene.png");
            string manifestPath =
                Path.Combine(fullFolder, "baselines.json");
            Directory.CreateDirectory(fullFolder);
            try
            {
                File.WriteAllBytes(
                    imagePath,
                    Encoding.UTF8.GetBytes("changed regression image"));
                string json =
                    "{\n" +
                    "  \"schemaVersion\": \"1.0\",\n" +
                    "  \"runtimeConnected\": true,\n" +
                    "  \"approvalStatus\": \"Approved\",\n" +
                    "  \"reviewer\": \"test\",\n" +
                    "  \"revision\": 1,\n" +
                    "  \"approvedAtUtc\": \"2026-01-01T00:00:00Z\",\n" +
                    "  \"scenes\": [{\n" +
                    "    \"sceneId\": \"TEST-01\",\n" +
                    "    \"path\": \"" +
                    relativeFolder.Replace("\\", "/") +
                    "/scene.png\",\n" +
                    "    \"sha256\": \"" +
                    new string('0', 64) +
                    "\"\n" +
                    "  }]\n" +
                    "}";
                File.WriteAllText(
                    manifestPath,
                    json,
                    new UTF8Encoding(false));

                var errors =
                    ApprovedBackgroundSemanticCatalogBaker
                        .ValidateScreenshotBaselines(
                            manifestPath);

                Assert.That(
                    errors.Any(error =>
                        error.Contains(
                            ApprovedBackgroundSemanticCatalogBaker
                                .ScreenshotHashMismatchCode,
                            StringComparison.Ordinal)),
                    Is.True,
                    string.Join(Environment.NewLine, errors));
            }
            finally
            {
                if (Directory.Exists(fullFolder))
                {
                    Directory.Delete(
                        fullFolder,
                        recursive: true);
                }
            }
        }
    }
}
