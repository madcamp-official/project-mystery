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
            Assert.That(AmbientBarkCatalog.All.Count, Is.EqualTo(47));
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
        public void AmbientBarks_AreLocationSpecificAndCoverEveryLocation()
        {
            Assert.That(
                AmbientBarkCatalog.All.Any(item => item.Location == "ANY"),
                Is.False);
            Assert.That(
                AmbientBarkCatalog.All
                    .Select(item => item.Location)
                    .Distinct()
                    .OrderBy(item => item),
                Is.EquivalentTo(
                    AmbientBarkCatalog.SupportedLocations
                        .OrderBy(item => item)));
        }

        [Test]
        public void RestrictedEngineeringLocations_DoNotSpawnPassengers()
        {
            string[] restrictedLocations =
            {
                "SERVICE_RAIL", "BALLAST_CONTROL_ANNEX", "ENGINE_CONTROL",
                "CREW_STAIRS", "VAULT", "ARCHIVE", "SERVICE_HUB",
                "STABILIZERS", "BALLAST_TANKS", "GENERATOR", "WORKSHOP"
            };

            Assert.That(
                AmbientBarkCatalog.All
                    .Where(item =>
                        restrictedLocations.Contains(item.Location))
                    .All(item =>
                        item.Speaker.StartsWith("CREW_")),
                Is.True);
        }

        [Test]
        public void LocationSelection_ReturnsOnlyRelevantCharactersAndDialogue()
        {
            AmbientBarkRecord[] port =
                AmbientBarkCatalog.GetAvailable("PORT", null).ToArray();
            AmbientBarkRecord[] engine =
                AmbientBarkCatalog.GetAvailable(
                    "ENGINE_CONTROL",
                    null).ToArray();

            Assert.That(port, Has.Length.EqualTo(2));
            Assert.That(
                port.Select(item => item.Speaker),
                Is.EquivalentTo(
                    new[] { "CREW_ATTENDANT", "PASSENGER_A" }));
            Assert.That(engine, Has.Length.EqualTo(1));
            Assert.That(engine[0].Speaker, Is.EqualTo("CREW_ENGINEER"));
            Assert.That(engine[0].Text, Does.Contain("주기관 출력"));
        }

        [Test]
        public void RepeatedSpeakers_DoNotRepeatDialogueAcrossLocations()
        {
            var repeatedSpeakers = AmbientBarkCatalog.All
                .GroupBy(item => item.Speaker)
                .Where(group => group.Count() > 1)
                .ToArray();

            Assert.That(repeatedSpeakers, Is.Not.Empty);
            foreach (var appearances in repeatedSpeakers)
            {
                Assert.That(
                    appearances.All(item => item.Location != "ANY"),
                    Is.True,
                    appearances.Key);
                Assert.That(
                    appearances.Select(item => item.Text),
                    Is.Unique,
                    appearances.Key);
            }

            Assert.That(
                AmbientBarkCatalog.All.Select(item => item.Text),
                Is.Unique);
        }

        [Test]
        public void EveryAmbientSpeaker_HasWorldCharacterArtwork()
        {
            string[] speakers = AmbientBarkCatalog.All
                .Select(item => item.Speaker)
                .Distinct()
                .ToArray();

            foreach (string speaker in speakers)
            {
                Assert.That(
                    AmbientWorldCharacterCatalog.TryGetAsset(
                        speaker,
                        out AmbientWorldCharacterAsset asset),
                    Is.True,
                    speaker);
                Assert.That(asset.ResourcePath, Is.Not.Empty, speaker);
                Assert.That(asset.UvRect.width, Is.GreaterThan(0f), speaker);
                Assert.That(asset.UvRect.xMin, Is.GreaterThanOrEqualTo(0f));
                Assert.That(asset.UvRect.xMax, Is.LessThanOrEqualTo(1f));
            }
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
            Assert.That(AmbientInspectableCatalog.All.Count, Is.EqualTo(9));
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

        [Test]
        public void SharedPassengerDisplayName_DoesNotBreakPortraitLookup()
        {
            Assert.That(
                DialoguePortraitCatalog.TryGet(
                    "승객",
                    out DialoguePortraitDefinition genericPassenger),
                Is.True);
            Assert.That(
                genericPassenger.CharacterId,
                Is.EqualTo("PASSENGER_A"));
            Assert.That(
                DialoguePortraitCatalog.GetDisplayName("PASSENGER_F"),
                Is.EqualTo("승객"));
        }
    }
}
