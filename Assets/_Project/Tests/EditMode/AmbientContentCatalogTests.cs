using System.Linq;
using NUnit.Framework;
using Wake.Exploration;
using Wake.Narrative;

namespace Wake.Tests
{
    public sealed class AmbientContentCatalogTests
    {
        [Test]
        public void WorkbookAmbientBarks_AreAllRepresented()
        {
            Assert.That(AmbientBarkCatalog.All, Has.Count.EqualTo(32));
            Assert.That(
                AmbientBarkCatalog.All.Select(item => item.Id),
                Is.Unique);
            Assert.That(
                AmbientBarkCatalog.All.All(item =>
                    !string.IsNullOrWhiteSpace(item.Speaker) &&
                    !string.IsNullOrWhiteSpace(item.Text) &&
                    !string.IsNullOrWhiteSpace(item.Condition)),
                Is.True);
        }

        [Test]
        public void EveryAmbientSpeaker_HasUsablePortrait()
        {
            string[] speakers = AmbientBarkCatalog.All
                .Select(item => item.Speaker)
                .Distinct()
                .ToArray();

            Assert.That(speakers, Has.Length.EqualTo(9));
            foreach (string speaker in speakers)
            {
                Assert.That(
                    DialoguePortraitCatalog.TryGet(speaker, out _),
                    Is.True,
                    speaker);
                DialoguePortraitAsset asset =
                    DialoguePortraitCatalog.Resolve(
                        speaker,
                        PortraitEmotion.Neutral);
                Assert.That(asset.Found, Is.True, speaker);
                Assert.That(asset.Texture, Is.Not.Null, speaker);
                Assert.That(asset.UvRect.width, Is.EqualTo(.54f).Within(.001f));
            }
        }

        [Test]
        public void InspectableMacguffins_HaveUniqueContentAndValidCrops()
        {
            Assert.That(AmbientInspectableCatalog.All, Has.Count.EqualTo(9));
            Assert.That(
                AmbientInspectableCatalog.All.Select(item => item.Id),
                Is.Unique);
            Assert.That(
                AmbientInspectableCatalog.All.All(item =>
                    !string.IsNullOrWhiteSpace(item.Title) &&
                    !string.IsNullOrWhiteSpace(item.Description) &&
                    item.ImageUv.xMin >= 0f &&
                    item.ImageUv.yMin >= 0f &&
                    item.ImageUv.xMax <= 1f &&
                    item.ImageUv.yMax <= 1f),
                Is.True);
        }

        [Test]
        public void PassengerIds_AreNotCollapsedAtUnderscore()
        {
            Assert.That(
                DialoguePortraitCatalog.TryGet(
                    "PASSENGER_A",
                    out DialoguePortraitDefinition passengerA),
                Is.True);
            Assert.That(
                DialoguePortraitCatalog.TryGet(
                    "PASSENGER_F",
                    out DialoguePortraitDefinition passengerF),
                Is.True);
            Assert.That(
                passengerA.FallbackTexture,
                Is.Not.EqualTo(passengerF.FallbackTexture));
            Assert.That(
                passengerA.FallbackTexture,
                Is.EqualTo("AmbientCharacters/passenger_a"));
        }
    }
}
