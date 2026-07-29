using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wake.Exploration;
using Wake.Narrative;

namespace Wake.Tests
{
    public sealed class LocationBackgroundAnimationCatalogTests
    {
        private const string BackgroundFolder =
            "Assets/_Project/Art/Backgrounds/Locations";

        [Test]
        public void StoryLocations_MapTwentyThreeLogicalCodesToNineteenVisuals()
        {
            string[] storyLocations = CanonicalLocationCatalog.StoryRelevant
                .Select(location => location.Code)
                .OrderBy(code => code, StringComparer.Ordinal)
                .ToArray();
            string[] boundLocations =
                LocationBackgroundAnimationCatalog.Bindings
                    .Select(binding => binding.LogicalLocationCode)
                    .OrderBy(code => code, StringComparer.Ordinal)
                    .ToArray();

            Assert.That(storyLocations, Has.Length.EqualTo(23));
            Assert.That(boundLocations, Has.Length.EqualTo(23));
            Assert.That(boundLocations, Is.Unique);
            Assert.That(boundLocations, Is.EqualTo(storyLocations));
            Assert.That(
                LocationBackgroundAnimationCatalog.All,
                Has.Count.EqualTo(19));
            Assert.That(
                LocationBackgroundAnimationCatalog.All
                    .Select(profile => profile.Id),
                Is.Unique);
            Assert.That(
                LocationBackgroundAnimationCatalog.Bindings
                    .Select(binding => binding.ProfileId)
                    .Distinct(StringComparer.Ordinal)
                    .Count(),
                Is.EqualTo(19));
        }

        [TestCase("SECURITY", "INTERVIEW")]
        [TestCase("NEWS_LOUNGE", "CABIN_DANIEL")]
        [TestCase("ENGINE_CONTROL", "BRIDGE")]
        [TestCase("SERVICE7", "CREW_STAIRS")]
        public void ArtSharingLocations_ResolveToTheSameProfile(
            string firstLocation,
            string secondLocation)
        {
            Assert.That(
                LocationBackgroundAnimationCatalog.TryGet(
                    firstLocation,
                    out LocationBackgroundAnimationProfile first),
                Is.True);
            Assert.That(
                LocationBackgroundAnimationCatalog.TryGet(
                    secondLocation,
                    out LocationBackgroundAnimationProfile second),
                Is.True);

            Assert.That(second, Is.SameAs(first));
            Assert.That(second.Id, Is.EqualTo(first.Id));
            Assert.That(
                second.SourceSpriteFileName,
                Is.EqualTo(first.SourceSpriteFileName));
        }

        [Test]
        public void EveryScheduledNarrativeAlias_ResolvesThroughCanonicalCatalog()
        {
            string[] narrativeCodes = ProductionSceneCatalog.All
                .Select(scene => scene.NarrativeLocationCode)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            foreach (string narrativeCode in narrativeCodes)
            {
                Assert.That(
                    LocationBackgroundAnimationCatalog.TryGet(
                        $"  {narrativeCode.ToLowerInvariant()}  ",
                        out LocationBackgroundAnimationProfile profile),
                    Is.True,
                    narrativeCode);
                Assert.That(profile, Is.Not.Null, narrativeCode);
            }

            Assert.That(
                LocationBackgroundAnimationCatalog.TryGet(
                    "UNKNOWN_LOCATION",
                    out _),
                Is.False);
        }

        [Test]
        public void Profiles_ReferenceTheNineteenAuthoredBackgroundsInUse()
        {
            string[] sourceFiles =
                LocationBackgroundAnimationCatalog.All
                    .Select(profile => profile.SourceSpriteFileName)
                    .ToArray();

            Assert.That(sourceFiles, Has.Length.EqualTo(19));
            Assert.That(sourceFiles, Is.Unique);
            foreach (string sourceFile in sourceFiles)
            {
                Assert.That(sourceFile, Is.Not.Empty);
                Assert.That(
                    File.Exists(Path.Combine(
                        BackgroundFolder,
                        sourceFile)),
                    Is.True,
                    sourceFile);
            }

            foreach (LocationBackgroundProfileBinding binding in
                     LocationBackgroundAnimationCatalog.Bindings)
            {
                CanonicalLocationSpec location =
                    CanonicalLocationCatalog.FindSpec(
                        binding.LogicalLocationCode);
                Assert.That(location, Is.Not.Null);
                Assert.That(
                    LocationBackgroundAnimationCatalog.TryGet(
                        binding.LogicalLocationCode,
                        out LocationBackgroundAnimationProfile profile),
                    Is.True);
                Assert.That(
                    profile.SourceSpriteFileName,
                    Is.EqualTo(location.SpriteFileName),
                    binding.LogicalLocationCode);
            }
        }

        [Test]
        public void Effects_AreNormalizedBoundedAndReadyForUiRendering()
        {
            var movingTypes = new HashSet<LocationBackgroundEffectType>
            {
                LocationBackgroundEffectType.LinearSweep,
                LocationBackgroundEffectType.DriftingMotes,
                LocationBackgroundEffectType.DriftingSteam,
                LocationBackgroundEffectType.OccasionalSpark,
                LocationBackgroundEffectType.FullBackgroundDrift,
                LocationBackgroundEffectType.FullBackgroundShake
            };

            foreach (LocationBackgroundAnimationProfile profile in
                     LocationBackgroundAnimationCatalog.All)
            {
                Assert.That(profile.Effects.Count, Is.InRange(3, 5), profile.Id);

                foreach (LocationBackgroundEffectSpec effect in
                         profile.Effects)
                {
                    Rect rect = effect.NormalizedRect;
                    Assert.That(rect.width, Is.GreaterThan(0f), profile.Id);
                    Assert.That(rect.height, Is.GreaterThan(0f), profile.Id);
                    Assert.That(rect.xMin, Is.InRange(0f, 1f), profile.Id);
                    Assert.That(rect.yMin, Is.InRange(0f, 1f), profile.Id);
                    Assert.That(rect.xMax, Is.InRange(0f, 1f), profile.Id);
                    Assert.That(rect.yMax, Is.InRange(0f, 1f), profile.Id);
                    Assert.That(
                        effect.NormalizedAnchor.x,
                        Is.InRange(rect.xMin, rect.xMax),
                        profile.Id);
                    Assert.That(
                        effect.NormalizedAnchor.y,
                        Is.InRange(rect.yMin, rect.yMax),
                        profile.Id);
                    Assert.That(
                        effect.Color.a,
                        Is.InRange(float.Epsilon, 1f),
                        profile.Id);
                    Assert.That(
                        effect.Intensity,
                        Is.InRange(float.Epsilon, 1f),
                        profile.Id);
                    Assert.That(
                        effect.DurationSeconds,
                        Is.GreaterThan(0f),
                        profile.Id);
                    Assert.That(
                        effect.FrequencyHz,
                        Is.GreaterThan(0f),
                        profile.Id);
                    Assert.That(
                        effect.MaxElementCount,
                        Is.InRange(1, 32),
                        profile.Id);
                    Assert.That(effect.Seed, Is.GreaterThan(0), profile.Id);
                    Assert.That(
                        effect.NormalizedTravel,
                        Is.InRange(0f, 2f),
                        profile.Id);

                    if (movingTypes.Contains(effect.Type))
                    {
                        Assert.That(
                            effect.Direction.sqrMagnitude,
                            Is.EqualTo(1f).Within(.001f),
                            $"{profile.Id}/{effect.Type}");
                        Assert.That(
                            effect.NormalizedTravel,
                            Is.GreaterThan(0f),
                            $"{profile.Id}/{effect.Type}");
                    }
                }
            }
        }

        [Test]
        public void Catalog_ProvidesEveryPlannedOverlayPrimitive()
        {
            LocationBackgroundEffectType[] authoredTypes =
                LocationBackgroundAnimationCatalog.All
                    .SelectMany(profile => profile.Effects)
                    .Select(effect => effect.Type)
                    .Distinct()
                    .OrderBy(type => type)
                    .ToArray();

            Assert.That(
                authoredTypes,
                Is.EquivalentTo(
                    Enum.GetValues(typeof(LocationBackgroundEffectType))));
        }

        [Test]
        public void CatalogCollections_AreReadOnlyAndProfilesResolveById()
        {
            Assert.That(
                LocationBackgroundAnimationCatalog.All,
                Is.InstanceOf<ReadOnlyCollection<
                    LocationBackgroundAnimationProfile>>());
            Assert.That(
                LocationBackgroundAnimationCatalog.Bindings,
                Is.InstanceOf<ReadOnlyCollection<
                    LocationBackgroundProfileBinding>>());

            foreach (LocationBackgroundAnimationProfile profile in
                     LocationBackgroundAnimationCatalog.All)
            {
                Assert.That(
                    profile.Effects,
                    Is.InstanceOf<ReadOnlyCollection<
                        LocationBackgroundEffectSpec>>());
                Assert.That(
                    LocationBackgroundAnimationCatalog.TryGetById(
                        profile.Id,
                        out LocationBackgroundAnimationProfile resolved),
                    Is.True);
                Assert.That(resolved, Is.SameAs(profile));
            }
        }
    }
}
