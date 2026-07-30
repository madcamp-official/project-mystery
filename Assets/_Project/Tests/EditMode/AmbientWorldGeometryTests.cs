using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wake.Exploration;
using Wake.Narrative;

namespace Wake.Tests
{
    public sealed class AmbientWorldGeometryTests
    {
        private static readonly Vector2 ReferenceResolution =
            new(2880f, 1800f);
        private static readonly Vector2 HdResolution =
            new(1920f, 1080f);

        [Test]
        public void EveryStagedCharacter_UsesVisibleBodyHeight()
        {
            foreach (AmbientWorldStageRecord record in
                     AmbientWorldStageCatalog.All)
            {
                Assert.That(
                    AmbientWorldCharacterCatalog.TryGetAsset(
                        record.Speaker,
                        out AmbientWorldCharacterAsset asset),
                    Is.True,
                    record.Speaker);
                AmbientWorldLayoutMetrics metrics =
                    AmbientWorldGeometry.Calculate(
                        ReferenceResolution,
                        record.Profile,
                        asset);
                float expected =
                    ReferenceResolution.y *
                    record.Profile.NormalizedHeight;

                Assert.That(
                    metrics.VisibleHeight,
                    Is.EqualTo(expected).Within(.01f),
                    $"{record.Location}|{record.Speaker}");
                Assert.That(
                    metrics.RectSize.y,
                    asset.VisibleVerticalSpan >= .999f
                        ? Is.GreaterThanOrEqualTo(metrics.VisibleHeight)
                        : Is.GreaterThan(metrics.VisibleHeight),
                    $"{record.Location}|{record.Speaker}");
            }
        }

        [Test]
        public void EveryStagedCharacter_KeepsVisibleFeetOnAnchor()
        {
            foreach (AmbientWorldStageRecord record in
                     AmbientWorldStageCatalog.All)
            {
                AmbientWorldCharacterCatalog.TryGetAsset(
                    record.Speaker,
                    out AmbientWorldCharacterAsset asset);
                AmbientWorldLayoutMetrics metrics =
                    AmbientWorldGeometry.Calculate(
                        ReferenceResolution,
                        record.Profile,
                        asset);

                Assert.That(
                    AmbientWorldGeometry.VisibleFootOffset(
                        metrics,
                        asset),
                    Is.EqualTo(0f).Within(.01f),
                    $"{record.Location}|{record.Speaker}");
                Assert.That(
                    metrics.VisibleFootY,
                    Is.EqualTo(
                        record.Profile.Anchor.y *
                        ReferenceResolution.y).Within(.01f));
            }
        }

        [Test]
        public void EveryStagedCharacter_StaysBelowHudSafeBand()
        {
            foreach (AmbientWorldStageRecord record in
                     AmbientWorldStageCatalog.All)
            {
                AmbientWorldCharacterCatalog.TryGetAsset(
                    record.Speaker,
                    out AmbientWorldCharacterAsset asset);
                AmbientWorldLayoutMetrics metrics =
                    AmbientWorldGeometry.Calculate(
                        ReferenceResolution,
                        record.Profile,
                        asset);

                Assert.That(
                    AmbientWorldGeometry.FitsVerticalStage(
                        metrics,
                        ReferenceResolution.y),
                    Is.True,
                    $"{record.Location}|{record.Speaker}");
            }
        }

        [Test]
        public void LayoutScale_IsResolutionIndependent()
        {
            AmbientWorldStageCatalog.TryGet(
                "LAUNDRY",
                "LAUNDRY_SUPERVISOR",
                out AmbientWorldStageProfile stage);
            AmbientWorldCharacterCatalog.TryGetAsset(
                "LAUNDRY_SUPERVISOR",
                out AmbientWorldCharacterAsset asset);
            AmbientWorldLayoutMetrics reference =
                AmbientWorldGeometry.Calculate(
                    ReferenceResolution,
                    stage,
                    asset);
            AmbientWorldLayoutMetrics hd =
                AmbientWorldGeometry.Calculate(
                    HdResolution,
                    stage,
                    asset);

            Assert.That(
                reference.VisibleHeight / ReferenceResolution.y,
                Is.EqualTo(hd.VisibleHeight / HdResolution.y)
                    .Within(.0001f));
            Assert.That(
                reference.VisibleFootY / ReferenceResolution.y,
                Is.EqualTo(hd.VisibleFootY / HdResolution.y)
                    .Within(.0001f));
        }

        [Test]
        public void LaundrySupervisor_UsesForegroundHumanScale()
        {
            AmbientWorldStageRecord laundry =
                AmbientWorldStageCatalog.All.Single(record =>
                    record.Location == "LAUNDRY" &&
                    record.Speaker == "LAUNDRY_SUPERVISOR");
            AmbientWorldCharacterCatalog.TryGetAsset(
                laundry.Speaker,
                out AmbientWorldCharacterAsset asset);
            AmbientWorldLayoutMetrics metrics =
                AmbientWorldGeometry.Calculate(
                    ReferenceResolution,
                    laundry.Profile,
                    asset);

            Assert.That(
                metrics.VisibleHeight / ReferenceResolution.y,
                Is.EqualTo(.60f).Within(.001f));
            Assert.That(
                metrics.RectSize.y / ReferenceResolution.y,
                Is.GreaterThan(.70f));
            Assert.That(
                metrics.GroundShadowSize.x,
                Is.GreaterThan(100f));
        }

        [Test]
        public void EveryDialogueStage_HasGeometryData()
        {
            string[] dialoguePairs = AmbientBarkCatalog.All
                .Select(item => $"{item.Location}|{item.Speaker}")
                .Distinct()
                .OrderBy(item => item)
                .ToArray();
            string[] geometryPairs = AmbientWorldStageCatalog.All
                .Select(item => $"{item.Location}|{item.Speaker}")
                .OrderBy(item => item)
                .ToArray();

            Assert.That(geometryPairs, Is.EquivalentTo(dialoguePairs));
        }
    }
}
