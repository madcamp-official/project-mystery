using System;
using System.Linq;
using NUnit.Framework;
using Wake.Exploration;

namespace Wake.Tests
{
    public sealed class ScenePresenceCatalogTests
    {
        [Test]
        public void WorkbookSchedule_ContainsFortyOneOrderedScenes()
        {
            Assert.That(ScenePresenceCatalog.All, Has.Count.EqualTo(41));
            Assert.That(
                ScenePresenceCatalog.All.Select(record => record.SceneId),
                Is.Unique);
            Assert.That(
                ScenePresenceCatalog.All.Select(record => record.Order),
                Is.EqualTo(Enumerable.Range(0, 41)));
        }

        [Test]
        public void EveryScene_PlacesEveryMainCharacterOnce()
        {
            foreach (ScenePresenceRecord scene in ScenePresenceCatalog.All)
            {
                Assert.That(
                    scene.CharacterLocations.Keys,
                    Is.EquivalentTo(ScenePresenceCatalog.MainCharacterIds),
                    scene.SceneId);
                Assert.That(
                    scene.CharacterLocations.Values.All(location =>
                        CanonicalLocationCatalog.FindSpec(location) != null),
                    Is.True,
                    scene.SceneId);

                foreach (string character in
                         ScenePresenceCatalog.MainCharacterIds)
                {
                    Assert.That(
                        scene.GetLocation(character),
                        Is.Not.Empty,
                        $"{scene.SceneId}|{character}");
                }
            }
        }

        [Test]
        public void EveryScene_HasCanonicalFocusAndContextNpc()
        {
            foreach (ScenePresenceRecord scene in ScenePresenceCatalog.All)
            {
                Assert.That(
                    CanonicalLocationCatalog.FindSpec(scene.FocusLocation),
                    Is.Not.Null,
                    scene.SceneId);
                Assert.That(scene.ContextBarkId, Is.Not.Empty, scene.SceneId);
                Assert.That(scene.ContextSpeaker, Is.Not.Empty, scene.SceneId);
            }
        }

        [Test]
        public void MurderNight_PreservesVictimAndAlibiTimeline()
        {
            ScenePresenceCatalog.TryGet(
                "D1-03",
                out ScenePresenceRecord ballroom);
            ScenePresenceCatalog.TryGet(
                "D1-04",
                out ScenePresenceRecord stairs);
            ScenePresenceCatalog.TryGet(
                "D1-05",
                out ScenePresenceRecord bodyMove);

            Assert.That(
                ballroom.GetCharactersAt("BALLROOM"),
                Is.EquivalentTo(ScenePresenceCatalog.MainCharacterIds));
            Assert.That(
                stairs.GetLocation("DANIEL"),
                Is.EqualTo("CREW_STAIRS"));
            Assert.That(
                stairs.GetLocation("EVELYN"),
                Is.EqualTo("VIP_LOUNGE"));
            Assert.That(
                bodyMove.GetLocation("DANIEL"),
                Is.EqualTo("HORIZON"));
            Assert.That(
                bodyMove.GetState("DANIEL"),
                Is.EqualTo(SceneCharacterState.Deceased));
        }

        [Test]
        public void MarcusInjury_ChangesStateAndLocationWithoutInventingAttacker()
        {
            ScenePresenceCatalog.TryGet(
                "D4-02",
                out ScenePresenceRecord accident);
            ScenePresenceCatalog.TryGet(
                "D4-04",
                out ScenePresenceRecord interview);
            ScenePresenceCatalog.TryGet(
                "D6-01",
                out ScenePresenceRecord recovered);

            Assert.That(
                accident.GetLocation("MARCUS"),
                Is.EqualTo("CREW_STAIRS"));
            Assert.That(
                accident.GetState("MARCUS"),
                Is.EqualTo(SceneCharacterState.Injured));
            Assert.That(
                interview.GetLocation("MARCUS"),
                Is.EqualTo("MEDBAY"));
            Assert.That(
                interview.GetState("MARCUS"),
                Is.EqualTo(SceneCharacterState.Injured));
            Assert.That(
                recovered.GetLocation("MARCUS"),
                Is.EqualTo("SECURITY"));
            Assert.That(
                recovered.GetState("MARCUS"),
                Is.EqualTo(SceneCharacterState.Normal));
        }

        [Test]
        public void Finale_SeparatesArrestAndDisembarkationRoutes()
        {
            ScenePresenceCatalog.TryGet(
                "D8-02",
                out ScenePresenceRecord arrest);
            ScenePresenceCatalog.TryGet(
                "D8-03",
                out ScenePresenceRecord epilogue);

            Assert.That(
                arrest.GetLocation("EVELYN"),
                Is.EqualTo("OPEN_DECK"));
            Assert.That(
                arrest.GetLocation("MARCUS"),
                Is.EqualTo("OPEN_DECK"));
            Assert.That(
                arrest.GetState("EVELYN"),
                Is.EqualTo(SceneCharacterState.Detained));
            Assert.That(
                epilogue.GetLocation("EVELYN"),
                Is.EqualTo("GANGWAY"));
            Assert.That(
                epilogue.GetLocation("DANIEL"),
                Is.EqualTo("PORT"));
            Assert.That(
                epilogue.GetState("DANIEL"),
                Is.EqualTo(SceneCharacterState.Deceased));
        }

        [TestCase("P-01", "PORT", "DOCK_PORTER")]
        [TestCase("D3-04", "VAULT", "VAULT_GUARD")]
        [TestCase("D6-03", "BALLAST_CONTROL_ANNEX", "BALLAST_CONTROLLER")]
        [TestCase("D8-03", "PORT", "DOCK_PORTER")]
        public void ContextNpc_MatchesSceneLocation(
            string sceneId,
            string location,
            string speaker)
        {
            Assert.That(
                ScenePresenceCatalog.TryGet(sceneId, out ScenePresenceRecord scene),
                Is.True);
            Assert.That(scene.FocusLocation, Is.EqualTo(location));
            Assert.That(scene.ContextSpeaker, Is.EqualTo(speaker));
        }

        [Test]
        public void UnknownSceneOrCharacter_ReturnsNoPlacement()
        {
            Assert.That(
                ScenePresenceCatalog.TryGet("UNKNOWN", out _),
                Is.False);
            ScenePresenceCatalog.TryGet(
                "D1-01",
                out ScenePresenceRecord scene);
            Assert.That(scene.GetLocation("UNKNOWN"), Is.Empty);
            Assert.That(
                scene.GetCharactersAt("UNKNOWN"),
                Is.Empty);
            Assert.That(
                scene.GetState("UNKNOWN"),
                Is.EqualTo(SceneCharacterState.Normal));
        }
    }
}
