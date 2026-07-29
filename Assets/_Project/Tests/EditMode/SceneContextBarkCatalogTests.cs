using System.Linq;
using NUnit.Framework;
using Wake.Exploration;
using Wake.Narrative;

namespace Wake.Tests
{
    public sealed class SceneContextBarkCatalogTests
    {
        [Test]
        public void WorkbookContextBarks_CoverEveryProductionScene()
        {
            Assert.That(
                SceneContextBarkCatalog.All.Count,
                Is.EqualTo(41));
            Assert.That(
                SceneContextBarkCatalog.All.Select(bark => bark.Id),
                Is.Unique);

            string[] barkScenes = SceneContextBarkCatalog.All
                .Select(bark => bark.Condition.Substring("scene=".Length))
                .OrderBy(scene => scene)
                .ToArray();
            string[] scheduledScenes = ScenePresenceCatalog.All
                .Select(scene => scene.SceneId)
                .OrderBy(scene => scene)
                .ToArray();
            Assert.That(barkScenes, Is.EqualTo(scheduledScenes));
        }

        [Test]
        public void ContextBarks_MatchPresenceCatalogIdentityAndLocation()
        {
            foreach (AmbientBarkRecord bark in
                     SceneContextBarkCatalog.All)
            {
                string sceneId =
                    bark.Condition.Substring("scene=".Length);
                Assert.That(
                    ScenePresenceCatalog.TryGet(
                        sceneId,
                        out ScenePresenceRecord scene),
                    Is.True,
                    bark.Id);
                Assert.That(
                    bark.Id,
                    Is.EqualTo(scene.ContextBarkId),
                    sceneId);
                Assert.That(
                    bark.Speaker,
                    Is.EqualTo(scene.ContextSpeaker),
                    sceneId);
                Assert.That(
                    bark.Location,
                    Is.EqualTo(scene.FocusLocation),
                    sceneId);
            }
        }

        [TestCase(
            "D1-02",
            "DINING",
            "SCENE_D102_DINING",
            "이블린 씨는 자리를 뜨지 않고")]
        [TestCase(
            "D4-02",
            "CREW_STAIRS",
            "SCENE_D402_STAIRS",
            "아래쪽 계단을 통과한 사람은 없습니다")]
        [TestCase(
            "D6-03",
            "BALLAST_CONTROL_ANNEX",
            "SCENE_D603_BALLAST",
            "임원 권한 하나로")]
        [TestCase(
            "D8-03",
            "PORT",
            "SCENE_D803_PORT",
            "보안 통로로 인계")]
        public void SceneSelection_PrioritizesCorrectContextDialogue(
            string sceneId,
            string location,
            string expectedId,
            string expectedText)
        {
            AmbientBarkRecord[] selected =
                AmbientBarkCatalog.GetAvailable(
                        location,
                        null,
                        sceneId)
                    .ToArray();

            Assert.That(selected, Is.Not.Empty);
            Assert.That(selected[0].Id, Is.EqualTo(expectedId));
            Assert.That(selected[0].Text, Does.Contain(expectedText));
        }

        [Test]
        public void SceneSelection_DoesNotLeakEarlierSceneDialogue()
        {
            string[] sceneOne = AmbientBarkCatalog
                .GetAvailable("HORIZON", null, "D2-01", 10)
                .Select(bark => bark.Id)
                .ToArray();
            string[] sceneFive = AmbientBarkCatalog
                .GetAvailable("HORIZON", null, "D2-05", 10)
                .Select(bark => bark.Id)
                .ToArray();

            Assert.That(sceneOne, Does.Contain("SCENE_D201_HORIZON"));
            Assert.That(sceneOne, Does.Not.Contain("SCENE_D205_HORIZON"));
            Assert.That(sceneFive, Does.Contain("SCENE_D205_HORIZON"));
            Assert.That(sceneFive, Does.Not.Contain("SCENE_D201_HORIZON"));
        }

        [Test]
        public void DanielSearch_UsesCabinAttendantAsTheRemainingWitness()
        {
            AmbientBarkRecord bark = SceneContextBarkCatalog.All.Single(
                item => item.Id == "SCENE_D104_STAIRS");

            Assert.That(bark.Speaker, Is.EqualTo("CREW_ATTENDANT"));
            Assert.That(
                bark.Text,
                Does.Contain("제가 다니엘 머서 씨를 마지막으로 봤습니다")
                    .And.Contain("아래층으로 내려갔어요"));
        }

        [Test]
        public void ExplicitActiveScene_IsNormalizedAndPreferred()
        {
            Assert.That(
                AmbientBarkCatalog.ResolveCurrentSceneId(
                    null,
                    " d3-04 "),
                Is.EqualTo("D3-04"));
            Assert.That(
                AmbientBarkCatalog.ResolveCurrentSceneId(
                    null,
                    "unknown"),
                Is.EqualTo("P-01"));
        }

        [Test]
        public void ExistingAmbientReactions_RemainAvailable()
        {
            Assert.That(
                AmbientBarkCatalog.All.Count,
                Is.EqualTo(47));
            Assert.That(
                AmbientBarkCatalog.Contextual.Count,
                Is.EqualTo(41));
            Assert.That(
                AmbientBarkCatalog.All
                    .Select(bark => bark.Id)
                    .Intersect(
                        AmbientBarkCatalog.Contextual
                            .Select(bark => bark.Id)),
                Is.Empty);
        }

        [Test]
        public void EveryContextDialogue_HasReadableKoreanAndEmotion()
        {
            foreach (AmbientBarkRecord bark in
                     AmbientBarkCatalog.Contextual)
            {
                Assert.That(bark.Text, Is.Not.Empty, bark.Id);
                Assert.That(
                    bark.Text.Any(character =>
                        character >= '\uAC00' &&
                        character <= '\uD7A3'),
                    Is.True,
                    bark.Id);
                Assert.That(bark.Emotion, Is.Not.Empty, bark.Id);
                Assert.That(bark.Location, Is.Not.EqualTo("ANY"), bark.Id);
            }
        }
    }
}
