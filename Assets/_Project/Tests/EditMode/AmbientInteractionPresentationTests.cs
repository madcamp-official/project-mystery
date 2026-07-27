using NUnit.Framework;
using UnityEngine;
using Wake.Exploration;

namespace Wake.Tests
{
    public sealed class AmbientInteractionPresentationTests
    {
        [Test]
        public void CharacterPlacement_SingleCharacterUsesCenter()
        {
            AmbientCharacterPlacement placement =
                AmbientInteractionPresentation.CharacterPlacement(0, 1);

            Assert.That(placement.AnchorX, Is.EqualTo(0.5f));
            Assert.That(
                placement.Size,
                Is.EqualTo(new Vector2(184f, 72f)));
        }

        [Test]
        public void CharacterPlacement_GroupStaysInsideSafeHorizontalBand()
        {
            AmbientCharacterPlacement first =
                AmbientInteractionPresentation.CharacterPlacement(0, 9);
            AmbientCharacterPlacement middle =
                AmbientInteractionPresentation.CharacterPlacement(4, 9);
            AmbientCharacterPlacement last =
                AmbientInteractionPresentation.CharacterPlacement(8, 9);

            Assert.That(
                first.AnchorX,
                Is.EqualTo(AmbientInteractionPresentation.MinimumAnchorX));
            Assert.That(middle.AnchorX, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(
                last.AnchorX,
                Is.EqualTo(AmbientInteractionPresentation.MaximumAnchorX));
        }

        [Test]
        public void CharacterPlacement_ClampsOutOfRangeIndex()
        {
            AmbientCharacterPlacement before =
                AmbientInteractionPresentation.CharacterPlacement(-4, 3);
            AmbientCharacterPlacement after =
                AmbientInteractionPresentation.CharacterPlacement(8, 3);

            Assert.That(
                before.AnchorX,
                Is.EqualTo(AmbientInteractionPresentation.MinimumAnchorX));
            Assert.That(
                after.AnchorX,
                Is.EqualTo(AmbientInteractionPresentation.MaximumAnchorX));
        }

        [Test]
        public void CharacterLabel_ExposesAvailabilityWithoutColor()
        {
            Assert.That(
                AmbientInteractionPresentation.CharacterLabel("Claire"),
                Is.EqualTo("Claire\n대화 가능"));
            Assert.That(
                AmbientInteractionPresentation.CharacterLabel(
                    "Claire",
                    isAvailable: false),
                Is.EqualTo("Claire\n대화 완료"));
            Assert.That(
                AmbientInteractionPresentation.CharacterLabel(" "),
                Does.StartWith("탑승객"));
        }

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
            var character =
                AmbientInteractionPresentation.CharacterColors();
            var hotspot =
                AmbientInteractionPresentation.HotspotColors();

            Assert.That(
                character.highlightedColor,
                Is.Not.EqualTo(character.normalColor));
            Assert.That(
                character.pressedColor,
                Is.Not.EqualTo(character.highlightedColor));
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
    }
}
