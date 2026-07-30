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
        public void HorizonDayTwoLayout_SeparatesHelenaAndCrewAttendant()
        {
            ApprovedBackgroundSemanticCatalog catalog =
                AssetDatabase.LoadAssetAtPath<
                    ApprovedBackgroundSemanticCatalog>(
                    ApprovedBackgroundSemanticCatalogBaker
                        .CatalogAssetPath);
            Assert.That(catalog, Is.Not.Null);

            ApprovedBackgroundSemanticSceneLayout layout =
                catalog.SceneLayouts.Single(value =>
                    value.SceneId == "D2-02");
            BackgroundSemanticCharacterSlotBinding helena =
                layout.Assignments.Single(value =>
                    value.CharacterId == "HELENA");
            BackgroundSemanticCharacterSlotBinding attendant =
                layout.Assignments.Single(value =>
                    value.CharacterId == "CREW_ATTENDANT");

            Assert.That(
                helena.SlotId,
                Is.EqualTo("near_right"),
                "The approved scene composition must remain unchanged.");
            Assert.That(
                attendant.SlotId,
                Is.EqualTo("near_far_right"));

            ApprovedBackgroundSemanticBinding binding =
                catalog.Bindings.First(value =>
                    value.LocationCode == "HORIZON" &&
                    value.VariantKey.Contains("bg_horizon_cleared_day"));
            ApprovedBackgroundSemanticResolver.SetCatalogForTests(
                catalog);
            try
            {
                Assert.That(
                    ApprovedBackgroundSemanticResolver.TryResolve(
                        "HORIZON",
                        binding.VariantKey,
                        binding.SourceSprite,
                        "D2-02",
                        out BackgroundSemanticRuntimeResolution resolution),
                    Is.True);

                BackgroundSemanticPlacementResult placement =
                    BackgroundSemanticPlacementResolver.Resolve(
                        resolution,
                        new[]
                        {
                            new BackgroundSemanticCharacterRequest(
                                "HELENA",
                                BackgroundSemanticCharacterRole.Main),
                            new BackgroundSemanticCharacterRequest(
                                "CREW_ATTENDANT",
                                BackgroundSemanticCharacterRole.Context)
                        });

                Assert.That(placement.IsValid, Is.True);
                BackgroundSemanticPlacementAssignment resolvedHelena =
                    placement.Assignments.Single(value =>
                        value.Character.CharacterId == "HELENA");
                BackgroundSemanticPlacementAssignment resolvedAttendant =
                    placement.Assignments.Single(value =>
                        value.Character.CharacterId == "CREW_ATTENDANT");
                Assert.That(
                    resolvedHelena.SilhouetteRect.Overlaps(
                        resolvedAttendant.SilhouetteRect),
                    Is.False,
                    "The isolated attendant crop must fit beside Helena " +
                    "without changing the approved scene slots.");
            }
            finally
            {
                ApprovedBackgroundSemanticResolver
                    .ResetCacheForTests();
            }
        }

        [Test]
        public void MedbayDayOneLayout_ShowsFocusTrioAndKeepsSupportingCastOffCamera()
        {
            ApprovedBackgroundSemanticCatalog catalog =
                AssetDatabase.LoadAssetAtPath<
                    ApprovedBackgroundSemanticCatalog>(
                    ApprovedBackgroundSemanticCatalogBaker
                        .CatalogAssetPath);
            Assert.That(catalog, Is.Not.Null);

            ApprovedBackgroundSemanticSceneLayout layout =
                catalog.SceneLayouts.Single(value =>
                    value.SceneId == "D1-07");
            Assert.That(layout.LocationCode, Is.EqualTo("MEDBAY"));
            Assert.That(layout.EnforceMeasuredAlphaBounds, Is.True);
            Assert.That(
                layout.Assignments.ToDictionary(
                    value => value.CharacterId,
                    value => value.SlotId,
                    StringComparer.OrdinalIgnoreCase),
                Is.EquivalentTo(
                    new System.Collections.Generic.Dictionary<
                        string,
                        string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["MARCUS"] = "near_left",
                        ["THOMAS"] = "near_center",
                        ["HELENA"] = "near_right"
                    }));
            Assert.That(
                layout.OffCameraCharacterIds,
                Is.EquivalentTo(new[] { "RICHARD", "SHIP_MEDIC" }));

            ApprovedBackgroundSemanticBinding binding =
                catalog.Bindings.Single(value =>
                    value.LocationCode == "MEDBAY" &&
                    value.VariantKey.Contains("bg_medbay_baseline"));
            ApprovedBackgroundSemanticResolver.SetCatalogForTests(
                catalog);
            try
            {
                Assert.That(
                    ApprovedBackgroundSemanticResolver.TryResolve(
                        "MEDBAY",
                        binding.VariantKey,
                        binding.SourceSprite,
                        "D1-07",
                        out BackgroundSemanticRuntimeResolution resolution),
                    Is.True);

                var requests = new[]
                {
                    new BackgroundSemanticCharacterRequest(
                        "SHIP_MEDIC",
                        BackgroundSemanticCharacterRole.Context),
                    new BackgroundSemanticCharacterRequest(
                        "RICHARD",
                        BackgroundSemanticCharacterRole.Main),
                    new BackgroundSemanticCharacterRequest(
                        "THOMAS",
                        BackgroundSemanticCharacterRole.Focus),
                    new BackgroundSemanticCharacterRequest(
                        "MARCUS",
                        BackgroundSemanticCharacterRole.Focus),
                    new BackgroundSemanticCharacterRequest(
                        "HELENA",
                        BackgroundSemanticCharacterRole.Focus)
                };
                var visibleRects = new[]
                {
                    new Rect(0f, 0f, 1f, 1f),
                    new Rect(.05f, 0f, .90f, 1f),
                    new Rect(.125f, 0f, .75f, 1f)
                };
                string[] aspectLabels = { "16:9", "16:10", "4:3" };
                float sourceAspect =
                    binding.SourceSprite.rect.width /
                    binding.SourceSprite.rect.height;
                for (int aspectIndex = 0;
                     aspectIndex < visibleRects.Length;
                     aspectIndex++)
                {
                    BackgroundSemanticPlacementResult placement =
                        BackgroundSemanticPlacementResolver.Resolve(
                            resolution,
                            requests,
                            visibleRects[aspectIndex],
                            sourceAspect);

                    Assert.That(
                        placement.IsValid,
                        Is.True,
                        aspectLabels[aspectIndex] + ": " +
                        string.Join(" | ", placement.Diagnostics));
                    Assert.That(
                        placement.Assignments,
                        Has.Count.EqualTo(3),
                        aspectLabels[aspectIndex]);
                    Assert.That(
                        placement.OffCameraCharacterIds,
                        Is.EquivalentTo(
                            new[] { "RICHARD", "SHIP_MEDIC" }),
                        aspectLabels[aspectIndex]);
                    for (int current = 0;
                         current < placement.Assignments.Count;
                         current++)
                    {
                        for (int previous = 0;
                             previous < current;
                             previous++)
                        {
                            Assert.That(
                                placement.Assignments[current]
                                    .SilhouetteRect.Overlaps(
                                        placement.Assignments[previous]
                                            .SilhouetteRect,
                                        true),
                                Is.False,
                                aspectLabels[aspectIndex]);
                        }
                    }
                }
            }
            finally
            {
                ApprovedBackgroundSemanticResolver
                    .ResetCacheForTests();
            }
        }

        [Test]
        public void VipLoungeDynamicPlacement_PreservesSeparatedMainCast()
        {
            ApprovedBackgroundSemanticCatalog catalog =
                AssetDatabase.LoadAssetAtPath<
                    ApprovedBackgroundSemanticCatalog>(
                    ApprovedBackgroundSemanticCatalogBaker
                        .CatalogAssetPath);
            Assert.That(catalog, Is.Not.Null);
            ApprovedBackgroundSemanticBinding binding =
                catalog.Bindings.First(value =>
                    value.LocationCode == "VIP_LOUNGE");

            ApprovedBackgroundSemanticResolver.SetCatalogForTests(
                catalog);
            try
            {
                Assert.That(
                    ApprovedBackgroundSemanticResolver.TryResolve(
                        "VIP_LOUNGE",
                        binding.VariantKey,
                        binding.SourceSprite,
                        out BackgroundSemanticRuntimeResolution resolution),
                    Is.True);

                BackgroundSemanticPlacementResult placement =
                    BackgroundSemanticPlacementResolver.Resolve(
                        resolution,
                        new[]
                        {
                            new BackgroundSemanticCharacterRequest(
                                "CLAIRE",
                                BackgroundSemanticCharacterRole.Main),
                            new BackgroundSemanticCharacterRequest(
                                "EVELYN",
                                BackgroundSemanticCharacterRole.Main)
                        });

                Assert.That(placement.IsValid, Is.True);
                Assert.That(placement.OffCameraCharacterIds, Is.Empty);
                BackgroundSemanticPlacementAssignment[] mains =
                    placement.Assignments.Where(value =>
                        value.Character.Role ==
                        BackgroundSemanticCharacterRole.Main).ToArray();
                Assert.That(mains, Has.Length.EqualTo(2));
                Assert.That(
                    mains.Select(value =>
                        value.Character.CharacterId),
                    Is.EquivalentTo(new[] { "CLAIRE", "EVELYN" }));
                Assert.That(
                    mains[0].SilhouetteRect.Overlaps(
                        mains[1].SilhouetteRect),
                    Is.False);
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
