using NUnit.Framework;
using UnityEngine;
using Wake.Exploration;

namespace Wake.Tests
{
    public sealed class AmbientInteractionPresentationTests
    {
        [Test]
        public void HotspotLabel_ExposesInteractionAndObjectName()
        {
            Assert.That(
                AmbientInteractionPresentation.HotspotLabel("서비스 벨"),
                Is.EqualTo("조사 · 서비스 벨"));
            Assert.That(
                AmbientInteractionPresentation.HotspotLabel(null),
                Is.EqualTo("조사"));
        }

        [Test]
        public void ClampHotspot_NormalizesAndClipsToBackground()
        {
            Rect result =
                AmbientInteractionPresentation.ClampHotspot(
                    Rect.MinMaxRect(1.2f, 0.8f, -0.2f, 1.4f));

            Assert.That(result.xMin, Is.Zero);
            Assert.That(result.xMax, Is.EqualTo(1f));
            Assert.That(result.yMin, Is.EqualTo(0.8f));
            Assert.That(result.yMax, Is.EqualTo(1f));
        }

        [Test]
        public void PopupSize_PreservesMarginsAndMaximumContentWidth()
        {
            Assert.That(
                AmbientInteractionPresentation.PopupSize(
                    new Vector2(1920f, 1080f)),
                Is.EqualTo(new Vector2(720f, 780f)));
            Assert.That(
                AmbientInteractionPresentation.PopupSize(
                    new Vector2(640f, 480f)),
                Is.EqualTo(new Vector2(512f, 384f)));
        }

        [Test]
        public void ButtonColors_ProvideVisibleHoverAndPressStates()
        {
            var worldCharacter =
                AmbientInteractionPresentation.CharacterSpriteColors(
                    new Color(.72f, .84f, .91f, 1f));
            var hotspot =
                AmbientInteractionPresentation.HotspotColors();

            Assert.That(
                worldCharacter.normalColor,
                Is.EqualTo(new Color(.72f, .84f, .91f, 1f)));
            Assert.That(
                worldCharacter.highlightedColor,
                Is.Not.EqualTo(worldCharacter.normalColor));
            Assert.That(
                worldCharacter.pressedColor,
                Is.Not.EqualTo(worldCharacter.highlightedColor));
            Assert.That(
                hotspot.normalColor.a,
                Is.GreaterThan(0f));
            Assert.That(
                hotspot.highlightedColor.a,
                Is.GreaterThan(hotspot.normalColor.a));
            Assert.That(
                hotspot.pressedColor,
                Is.Not.EqualTo(hotspot.highlightedColor));
        }

        [Test]
        public void StageProfiles_AvoidForegroundFurnitureAndMatchLighting()
        {
            Assert.That(
                AmbientWorldStageCatalog.TryGet(
                    "WORKSHOP",
                    "WORKSHOP_MACHINIST",
                    out AmbientWorldStageProfile workshop),
                Is.True);
            Assert.That(
                AmbientWorldStageCatalog.TryGet(
                    "ENGINE_CONTROL",
                    "CHIEF_ENGINEER",
                    out AmbientWorldStageProfile engine),
                Is.True);
            Assert.That(
                AmbientWorldStageCatalog.TryGet(
                    "PROMENADE",
                    "PASSENGER_PROMENADE_2",
                    out AmbientWorldStageProfile promenade),
                Is.True);
            Assert.That(
                AmbientWorldStageCatalog.TryGet(
                    "BALLAST_TANKS",
                    "BALLAST_CONTROLLER_TANKS",
                    out AmbientWorldStageProfile ballast),
                Is.True);

            Assert.That(workshop.Anchor.x, Is.LessThan(.25f));
            Assert.That(engine.Anchor.x, Is.LessThan(.3f));
            Assert.That(promenade.Anchor.x, Is.GreaterThan(.5f));
            Assert.That(ballast.Anchor.x, Is.InRange(.5f, .65f));
            Assert.That(
                workshop.LightTint,
                Is.Not.EqualTo(ballast.LightTint));
            Assert.That(workshop.ShadowOpacity, Is.GreaterThan(.4f));
            Assert.That(ballast.ShadowDirection.x, Is.LessThan(0f));
        }

        [Test]
        public void LaundryStage_GroundsCharacterAndUsesMutedRoomGrade()
        {
            Assert.That(
                AmbientWorldStageCatalog.TryGet(
                    "LAUNDRY",
                    "LAUNDRY_SUPERVISOR",
                    out AmbientWorldStageProfile laundry),
                Is.True);
            Assert.That(laundry.Anchor.x, Is.EqualTo(.38f).Within(.001f));
            Assert.That(laundry.Anchor.y, Is.EqualTo(.055f).Within(.001f));
            Assert.That(laundry.NormalizedHeight, Is.EqualTo(.60f));
            Assert.That(
                laundry.Anchor.y + laundry.NormalizedHeight,
                Is.LessThan(.70f));
            Assert.That(laundry.Exposure, Is.LessThan(.85f));
            Assert.That(laundry.Saturation, Is.LessThan(.75f));
            Assert.That(laundry.Softness, Is.GreaterThan(.25f));
        }

        [Test]
        public void StageProfiles_UseHumanScaleWithoutLeavingTheBackground()
        {
            foreach (AmbientWorldStageRecord stage in
                     AmbientWorldStageCatalog.All)
            {
                Assert.That(
                    stage.Profile.NormalizedHeight,
                    Is.InRange(.50f, .65f),
                    $"{stage.Location}|{stage.Speaker}");
                Assert.That(
                    stage.Profile.Anchor.y +
                    stage.Profile.NormalizedHeight,
                    Is.LessThan(.75f),
                    $"{stage.Location}|{stage.Speaker}");
            }
        }
    }
}
