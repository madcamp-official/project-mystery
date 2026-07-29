using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wake.Exploration;

namespace Wake.Tests
{
    public sealed class MainCharacterWorldPresentationTests
    {
        [Test]
        public void EveryMainCharacter_HasStandaloneWorldAsset()
        {
            foreach (string character in
                     ScenePresenceCatalog.MainCharacterIds)
            {
                Assert.That(
                    AmbientWorldCharacterCatalog.TryGetAsset(
                        character,
                        out AmbientWorldCharacterAsset asset),
                    Is.True,
                    character);
                Assert.That(
                    asset.ResourcePath,
                    Does.StartWith("WorldMainCharacters/"),
                    character);
                Assert.That(
                    asset.UvRect,
                    Is.EqualTo(new Rect(0f, 0f, 1f, 1f)),
                    character);
                Assert.That(
                    asset.CellAspectRatio,
                    Is.InRange(.55f, .86f),
                    character);
                Assert.That(
                    asset.VisibleVerticalSpan,
                    Is.GreaterThan(.80f),
                    character);
            }
        }

        [Test]
        public void EveryMainCharacterAsset_LoadsAsTransparentTexture()
        {
            foreach (string character in
                     ScenePresenceCatalog.MainCharacterIds)
            {
                AmbientWorldCharacterCatalog.TryGetAsset(
                    character,
                    out AmbientWorldCharacterAsset asset);
                Texture2D texture =
                    Resources.Load<Texture2D>(asset.ResourcePath);

                Assert.That(texture, Is.Not.Null, character);
                Assert.That(texture.width, Is.GreaterThan(900), character);
                Assert.That(texture.height, Is.GreaterThan(1300), character);
            }
        }

        [Test]
        public void PlayerViewpoint_IsNeverDuplicatedAsWorldActor()
        {
            foreach (ScenePresenceRecord scene in ScenePresenceCatalog.All)
            {
                string location = scene.GetLocation("ADRIAN");
                Assert.That(
                    ScenePresencePresentationPolicy
                        .Select(scene, location)
                        .Select(entry => entry.CharacterId),
                    Does.Not.Contain("ADRIAN"),
                    scene.SceneId);
            }
        }

        [Test]
        public void MurderVictim_IsNotRenderedStandingAfterDeath()
        {
            ScenePresenceCatalog.TryGet(
                "D1-05",
                out ScenePresenceRecord bodyMove);
            ScenePresenceCatalog.TryGet(
                "D8-03",
                out ScenePresenceRecord epilogue);

            Assert.That(
                ScenePresencePresentationPolicy
                    .Select(bodyMove, "HORIZON")
                    .Select(entry => entry.CharacterId),
                Does.Not.Contain("DANIEL"));
            Assert.That(
                ScenePresencePresentationPolicy
                    .Select(epilogue, "PORT")
                    .Select(entry => entry.CharacterId),
                Does.Not.Contain("DANIEL"));
        }

        [Test]
        public void CrowdedBallroom_PreservesLogicalCastButCapsForeground()
        {
            ScenePresenceCatalog.TryGet(
                "D1-03",
                out ScenePresenceRecord ballroom);

            SceneWorldCharacter[] all =
                ScenePresencePresentationPolicy
                    .Select(ballroom, "BALLROOM")
                    .ToArray();
            SceneWorldCharacter[] visible =
                ScenePresencePresentationPolicy
                    .SelectVisible(ballroom, "BALLROOM")
                    .ToArray();

            Assert.That(all.Length, Is.EqualTo(8));
            Assert.That(visible.Length, Is.EqualTo(3));
            Assert.That(
                visible.Select(entry => entry.CharacterId),
                Is.EqualTo(new[] { "DANIEL", "EVELYN", "RICHARD" }));
            Assert.That(
                all.Count(entry => entry.IsOffCamera),
                Is.EqualTo(5));
        }

        [Test]
        public void InjuryAndDetention_RemainVisibleAsExplicitStates()
        {
            ScenePresenceCatalog.TryGet(
                "D4-02",
                out ScenePresenceRecord injury);
            ScenePresenceCatalog.TryGet(
                "D8-02",
                out ScenePresenceRecord arrest);

            SceneWorldCharacter marcus =
                ScenePresencePresentationPolicy
                    .SelectVisible(injury, "CREW_STAIRS")
                    .Single(entry => entry.CharacterId == "MARCUS");
            SceneWorldCharacter evelyn =
                ScenePresencePresentationPolicy
                    .SelectVisible(arrest, "OPEN_DECK")
                    .Single(entry => entry.CharacterId == "EVELYN");

            Assert.That(
                marcus.State,
                Is.EqualTo(SceneCharacterState.Injured));
            Assert.That(
                evelyn.State,
                Is.EqualTo(SceneCharacterState.Detained));
            Assert.That(marcus.IsFocusParticipant, Is.True);
            Assert.That(evelyn.IsFocusParticipant, Is.True);
        }

        [Test]
        public void RemoteCharacters_AreNotBorrowedIntoCurrentLocation()
        {
            ScenePresenceCatalog.TryGet(
                "D3-03",
                out ScenePresenceRecord bridge);

            string[] visible =
                ScenePresencePresentationPolicy
                    .SelectVisible(bridge, "BRIDGE")
                    .Select(entry => entry.CharacterId)
                    .ToArray();

            Assert.That(
                visible,
                Is.EquivalentTo(new[] { "THOMAS", "OWEN" }));
            Assert.That(visible, Does.Not.Contain("RICHARD"));
            Assert.That(visible, Does.Not.Contain("EVELYN"));
        }
    }
}
