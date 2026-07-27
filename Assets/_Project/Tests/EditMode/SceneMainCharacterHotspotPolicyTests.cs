using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wake.Exploration;

namespace Wake.Tests
{
    public sealed class SceneMainCharacterHotspotPolicyTests
    {
        [Test]
        public void EveryCanonicalLocation_ProvidesLightingFallback()
        {
            foreach (CanonicalLocationSpec location in
                     CanonicalLocationCatalog.All)
            {
                AmbientWorldStageProfile profile =
                    AmbientWorldStageCatalog.GetLocationProfile(
                        location.Code,
                        0,
                        1,
                        mainCharacter: true);

                Assert.That(
                    profile.LightTint.a,
                    Is.EqualTo(1f).Within(.001f),
                    location.Code);
                Assert.That(
                    profile.Saturation,
                    Is.InRange(.35f, 1.1f),
                    location.Code);
                Assert.That(
                    profile.Exposure,
                    Is.InRange(.45f, 1.2f),
                    location.Code);
                Assert.That(
                    profile.Contrast,
                    Is.InRange(.55f, 1.2f),
                    location.Code);
                Assert.That(
                    profile.ShadowOpacity,
                    Is.InRange(.2f, .6f),
                    location.Code);
            }
        }

        [Test]
        public void MultiActorLayout_UsesSeparatedGroundAnchors()
        {
            foreach (CanonicalLocationSpec location in
                     CanonicalLocationCatalog.All)
            {
                Vector2[] anchors = Enumerable.Range(0, 3)
                    .Select(index =>
                        AmbientWorldStageCatalog.GetLocationProfile(
                            location.Code,
                            index,
                            3,
                            mainCharacter: index > 0).Anchor)
                    .ToArray();

                Assert.That(
                    Mathf.Abs(anchors[0].x - anchors[1].x),
                    Is.GreaterThan(.2f),
                    location.Code);
                Assert.That(
                    Mathf.Abs(anchors[1].x - anchors[2].x),
                    Is.GreaterThan(.2f),
                    location.Code);
                Assert.That(
                    anchors.All(anchor => anchor.y <= .06f),
                    Is.True,
                    $"{location.Code} actors must stay grounded");
            }
        }

        [Test]
        public void MainCharacters_AreScaledAboveAmbientActors()
        {
            foreach (CanonicalLocationSpec location in
                     CanonicalLocationCatalog.All)
            {
                AmbientWorldStageProfile ambient =
                    AmbientWorldStageCatalog.GetLocationProfile(
                        location.Code,
                        1,
                        3,
                        mainCharacter: false);
                AmbientWorldStageProfile main =
                    AmbientWorldStageCatalog.GetLocationProfile(
                        location.Code,
                        1,
                        3,
                        mainCharacter: true);

                Assert.That(
                    main.NormalizedHeight,
                    Is.GreaterThan(ambient.NormalizedHeight),
                    location.Code);
                Assert.That(
                    main.NormalizedHeight,
                    Is.LessThanOrEqualTo(.70f),
                    location.Code);
                Assert.That(
                    main.Anchor,
                    Is.EqualTo(ambient.Anchor),
                    location.Code);
            }
        }

        [Test]
        public void LocationProfile_PreservesAuthoredColorGrade()
        {
            Assert.That(
                AmbientWorldStageCatalog.TryGet(
                    "WORKSHOP",
                    "WORKSHOP_MACHINIST",
                    out AmbientWorldStageProfile authored),
                Is.True);

            AmbientWorldStageProfile generated =
                AmbientWorldStageCatalog.GetLocationProfile(
                    "WORKSHOP",
                    1,
                    3,
                    mainCharacter: true);

            Assert.That(generated.LightTint, Is.EqualTo(authored.LightTint));
            Assert.That(generated.Saturation, Is.EqualTo(authored.Saturation));
            Assert.That(generated.Exposure, Is.EqualTo(authored.Exposure));
            Assert.That(generated.Contrast, Is.EqualTo(authored.Contrast));
            Assert.That(generated.Softness, Is.EqualTo(authored.Softness));
            Assert.That(
                generated.ShadowDirection,
                Is.EqualTo(authored.ShadowDirection));
        }

        [Test]
        public void MainCharacterLines_AreCharacterSpecific()
        {
            string[] ids =
                ScenePresenceCatalog.MainCharacterIds
                    .Where(character => character != "ADRIAN")
                    .ToArray();
            string[] lines = ids
                .Select(character =>
                    MainCharacterWorldLineCatalog.Get(
                        character,
                        SceneCharacterState.Normal))
                .ToArray();

            Assert.That(lines, Is.Unique);
            Assert.That(
                lines.All(line => line.Length >= 20),
                Is.True);
            Assert.That(
                MainCharacterWorldLineCatalog.Get(
                    "HELENA",
                    SceneCharacterState.Normal),
                Does.Contain("의학"));
            Assert.That(
                MainCharacterWorldLineCatalog.Get(
                    "THOMAS",
                    SceneCharacterState.Normal),
                Does.Contain("장비"));
        }

        [Test]
        public void InjuryAndDetention_OverrideNormalConversation()
        {
            string injured =
                MainCharacterWorldLineCatalog.Get(
                    "MARCUS",
                    SceneCharacterState.Injured);
            string detained =
                MainCharacterWorldLineCatalog.Get(
                    "EVELYN",
                    SceneCharacterState.Detained);

            Assert.That(injured, Does.Contain("부상"));
            Assert.That(detained, Does.Contain("경비"));
            Assert.That(
                MainCharacterWorldLineCatalog.GetEmotion(
                    SceneCharacterState.Injured),
                Is.EqualTo("strained"));
            Assert.That(
                MainCharacterWorldLineCatalog.GetEmotion(
                    SceneCharacterState.Detained),
                Is.EqualTo("guarded"));
        }

        [Test]
        public void CompletedConversationLines_AreCharacterSpecificAndEffectFree()
        {
            string[] ids =
                ScenePresenceCatalog.MainCharacterIds
                    .Where(character => character != "ADRIAN")
                    .ToArray();
            string[] lines = ids
                .Select(character =>
                    MainCharacterWorldLineCatalog.GetCompleted(
                        character,
                        SceneCharacterState.Normal))
                .ToArray();

            Assert.That(lines, Is.Unique);
            Assert.That(lines.All(line => line.Length >= 15), Is.True);
            Assert.That(
                MainCharacterWorldLineCatalog.GetCompleted(
                    "MARCUS",
                    SceneCharacterState.Detained),
                Does.Contain("이미 진술을 마쳤습니다"));
        }

        [Test]
        public void ScenePolicyAndStagePolicy_FitThreeActorBudget()
        {
            foreach (ScenePresenceRecord scene in ScenePresenceCatalog.All)
            {
                SceneWorldCharacter[] mainCharacters =
                    ScenePresencePresentationPolicy
                        .SelectVisible(
                            scene,
                            scene.FocusLocation,
                            visibleLimit: 2)
                        .ToArray();

                Assert.That(
                    mainCharacters.Length + 1,
                    Is.LessThanOrEqualTo(3),
                    scene.SceneId);
                Assert.That(
                    mainCharacters.All(character =>
                        AmbientWorldCharacterCatalog.TryGetAsset(
                            character.CharacterId,
                            out _)),
                    Is.True,
                    scene.SceneId);
            }
        }
    }
}
