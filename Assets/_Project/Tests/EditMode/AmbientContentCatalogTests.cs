using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Wake.Exploration;
using Wake.Narrative;

namespace Wake.Tests
{
    public sealed class AmbientContentCatalogTests
    {
        [Test]
        public void WorkbookAmbientBarks_AreAllRepresented()
        {
            Assert.That(AmbientBarkCatalog.All.Count, Is.EqualTo(96));
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
        public void PlayerFacingAmbientText_UsesKoreanExceptApprovedTokens()
        {
            var latin = new Regex("[A-Za-z]");
            var allowed = new Regex(
                @"(?<![A-Za-z])(?:DNA|COO|VIP|kg|cm)(?![A-Za-z])",
                RegexOptions.IgnoreCase);
            string[] violations = AmbientBarkCatalog.All
                .Concat(AmbientBarkCatalog.Contextual)
                .Select(item => new
                {
                    item.Id,
                    Text = allowed.Replace(item.Text, string.Empty)
                })
                .Where(item => latin.IsMatch(item.Text))
                .Select(item => $"{item.Id}: {item.Text}")
                .Concat(
                    AmbientInspectableCatalog.All
                        .Select(item => new
                        {
                            item.Id,
                            Text = allowed.Replace(
                                $"{item.Title} {item.Description}",
                                string.Empty)
                        })
                        .Where(item => latin.IsMatch(item.Text))
                        .Select(item => $"{item.Id}: {item.Text}"))
                .ToArray();

            Assert.That(violations, Is.Empty);
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
                        !item.Speaker.StartsWith("PASSENGER_")),
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
                    new[] { "DOCK_PORTER", "PASSENGER_A" }));
            Assert.That(engine, Has.Length.EqualTo(1));
            Assert.That(engine[0].Speaker, Is.EqualTo("CHIEF_ENGINEER"));
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
                Assert.That(
                    asset.VisibleBottomMargin,
                    Is.InRange(0f, 0.15f),
                    speaker);
                Assert.That(
                    asset.VisibleTopMargin,
                    Is.InRange(0f, 0.13f),
                    speaker);
                Assert.That(
                    asset.VisibleVerticalSpan,
                    speaker == "CREW_ATTENDANT" ||
                    speaker == "CREW_ATTENDANT_BALLROOM"
                        ? Is.InRange(0.98f, 1f)
                        : Is.InRange(0.72f, 0.98f),
                    $"{speaker}: only tightly isolated attendant crops " +
                    "may use the full UV height.");
            }
        }

        [Test]
        public void EveryAmbientSpeaker_HasUsablePortrait()
        {
            string[] speakers = AmbientBarkCatalog.All
                .Select(item => item.Speaker)
                .Distinct()
                .ToArray();

            Assert.That(speakers, Has.Length.EqualTo(35));
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
                Assert.That(asset.UvRect.width, Is.GreaterThan(0f));
                Assert.That(asset.UvRect.xMin, Is.GreaterThanOrEqualTo(0f));
                Assert.That(asset.UvRect.xMax, Is.LessThanOrEqualTo(1f));
            }
        }

        [Test]
        public void SpecialistPortrait_ChangesCropWithAnxietyEmotion()
        {
            DialoguePortraitAsset neutral =
                DialoguePortraitCatalog.Resolve(
                    "SECURITY_OPERATOR",
                    PortraitEmotion.Neutral);
            DialoguePortraitAsset concerned =
                DialoguePortraitCatalog.Resolve(
                    "SECURITY_OPERATOR",
                    PortraitEmotion.Concerned);
            DialoguePortraitAsset angry =
                DialoguePortraitCatalog.Resolve(
                    "SECURITY_OPERATOR",
                    PortraitEmotion.Angry);

            Assert.That(neutral.Found, Is.True);
            Assert.That(concerned.Texture, Is.SameAs(neutral.Texture));
            Assert.That(angry.Texture, Is.SameAs(neutral.Texture));
            Assert.That(neutral.UvRect.x, Is.EqualTo(0.25f));
            Assert.That(concerned.UvRect.x, Is.EqualTo(0.50f));
            Assert.That(angry.UvRect.x, Is.EqualTo(0.75f));
            Assert.That(neutral.UvRect.width, Is.EqualTo(0.25f));
        }

        [Test]
        public void EveryAmbientRole_HasAUniqueLocationStageProfile()
        {
            var expectedPairs = AmbientBarkCatalog.All
                .Select(item => $"{item.Location}|{item.Speaker}")
                .Distinct()
                .OrderBy(item => item)
                .ToArray();
            var stagedPairs = AmbientWorldStageCatalog.All
                .Select(item => $"{item.Location}|{item.Speaker}")
                .OrderBy(item => item)
                .ToArray();

            Assert.That(stagedPairs, Is.Unique);
            Assert.That(stagedPairs, Is.EquivalentTo(expectedPairs));
            foreach (AmbientWorldStageRecord stage in
                     AmbientWorldStageCatalog.All)
            {
                Assert.That(stage.Profile.Anchor.x, Is.InRange(0f, 1f));
                Assert.That(stage.Profile.Anchor.y, Is.InRange(0f, 1f));
                Assert.That(
                    stage.Profile.NormalizedHeight,
                    Is.InRange(0.2f, 0.9f));
                Assert.That(stage.Profile.LightTint.a, Is.EqualTo(1f));
                Assert.That(stage.Profile.ShadowOpacity, Is.InRange(0f, 1f));
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
        public void BoardingGuide_IsLimitedToOpeningPortBackground()
        {
            AmbientInspectableSpec guide =
                AmbientInspectableCatalog.All.Single(item =>
                    item.Id == "PROP_BROCHURE");

            Assert.That(
                guide.IsAvailable(
                    "P-01",
                    "serialized:bg_location_port_evidence"),
                Is.True);
            Assert.That(
                guide.IsAvailable(
                    "P-01",
                    "LocationBackgroundVariants/bg_location_port_evidence.png"),
                Is.True);
            Assert.That(
                guide.IsAvailable(
                    "D8-03",
                    "LocationBackgroundVariants/bg_port_d8_epilogue"),
                Is.False);
            Assert.That(
                guide.IsAvailable(
                    "P-01",
                    "LocationBackgroundVariants/bg_port_d8_epilogue"),
                Is.False);
        }

        [Test]
        public void BoardingGuideOverlay_UsesTransparentPolygonTarget()
        {
            GameObject root = new(
                "Inspectable Overlay Test",
                typeof(RectTransform),
                typeof(AmbientInspectableOverlay));
            try
            {
                RectTransform content = root.GetComponent<RectTransform>();
                AmbientInspectableOverlay overlay =
                    root.GetComponent<AmbientInspectableOverlay>();
                overlay.Initialize(content);
                overlay.Show(
                    "PORT",
                    "P-01",
                    new LocationBackgroundSelection(
                        null,
                        "serialized:bg_location_port_evidence",
                        string.Empty,
                        usesSerializedFallback: false));

                Transform target =
                    content.Find("AmbientInspectable_PROP_BROCHURE");
                Assert.That(target, Is.Not.Null);
                Assert.That(
                    target.GetComponent<Image>().color,
                    Is.EqualTo(Color.clear));
                Assert.That(
                    target.GetComponent<Button>().transition,
                    Is.EqualTo(Selectable.Transition.None));
                Assert.That(target.GetComponent<Outline>(), Is.Null);
                Assert.That(
                    target.GetComponent<PolygonHotspotRaycastFilter>(),
                    Is.Not.Null);
                Assert.That(
                    target.Find("Interaction Label"),
                    Is.Null);
                Assert.That(
                    target.Find("Accessibility Focus Marker"),
                    Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void InspectableOverlay_WithoutApprovedPolygonFailsClosed()
        {
            GameObject root = new(
                "Inspectable Overlay Test",
                typeof(RectTransform),
                typeof(AmbientInspectableOverlay));
            try
            {
                RectTransform content = root.GetComponent<RectTransform>();
                AmbientInspectableOverlay overlay =
                    root.GetComponent<AmbientInspectableOverlay>();
                overlay.Initialize(content);
                overlay.Show(
                    "ATRIUM",
                    "P-01",
                    new LocationBackgroundSelection(
                        null,
                        "serialized:unapproved_atrium_background",
                        string.Empty,
                        usesSerializedFallback: false));

                Assert.That(
                    content.Find("AmbientInspectable_PROP_CHAMPAGNE"),
                    Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
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
                Is.EqualTo(
                    "AmbientCharacters/passenger_a_expressions"));
        }

        [TestCase("PASSENGER_A")]
        [TestCase("PASSENGER_F")]
        [TestCase("CREW_ATTENDANT")]
        [TestCase("CREW_ENGINEER")]
        [TestCase("CREW_SECURITY")]
        public void PassengerAndCrewPortraits_ChangeCropWithEmotion(
            string characterId)
        {
            DialoguePortraitAsset neutral =
                DialoguePortraitCatalog.Resolve(
                    characterId,
                    PortraitEmotion.Neutral);
            DialoguePortraitAsset concerned =
                DialoguePortraitCatalog.Resolve(
                    characterId,
                    PortraitEmotion.Concerned);
            DialoguePortraitAsset angry =
                DialoguePortraitCatalog.Resolve(
                    characterId,
                    PortraitEmotion.Angry);

            Assert.That(neutral.Found, Is.True);
            Assert.That(concerned.Found, Is.True);
            Assert.That(angry.Found, Is.True);
            Assert.That(concerned.Texture, Is.SameAs(neutral.Texture));
            Assert.That(angry.Texture, Is.SameAs(neutral.Texture));
            Assert.That(neutral.UvRect.x, Is.EqualTo(0.25f));
            Assert.That(concerned.UvRect.x, Is.EqualTo(0.5f));
            Assert.That(angry.UvRect.x, Is.EqualTo(0.75f));
        }

        [TestCase("PASSENGER_A", "passenger_a")]
        [TestCase("PASSENGER_F", "passenger_f")]
        [TestCase("CREW_ENGINEER", "crew_engineer")]
        [TestCase("CREW_SECURITY", "crew_security")]
        public void PassengerAndCrewWorldFigures_UseExpressionSheetFullBody(
            string characterId,
            string resourceName)
        {
            Assert.That(
                AmbientWorldCharacterCatalog.TryGetAsset(
                    characterId,
                    out AmbientWorldCharacterAsset asset),
                Is.True);
            Assert.That(
                asset.ResourcePath,
                Is.EqualTo(
                    $"AmbientCharacters/{resourceName}_expressions"));
            Assert.That(asset.UvRect.x, Is.Zero);
            Assert.That(asset.UvRect.width, Is.EqualTo(0.25f));
        }

        [Test]
        public void CrewAttendantWorldFigure_UsesIsolatedAtlasRegion()
        {
            Assert.That(
                AmbientWorldCharacterCatalog.TryGetAsset(
                    "CREW_ATTENDANT",
                    out AmbientWorldCharacterAsset attendant),
                Is.True);
            Assert.That(
                AmbientWorldCharacterCatalog.TryGetAsset(
                    "CREW_ATTENDANT_BALLROOM",
                    out AmbientWorldCharacterAsset ballroomAttendant),
                Is.True);

            Assert.That(
                attendant.ResourcePath,
                Is.EqualTo(
                    "AmbientCharacters/world_atlas_crew_passengers_ab"));
            Assert.That(
                attendant.UvRect.x,
                Is.EqualTo(96f / 1774f).Within(.000001f));
            Assert.That(
                attendant.UvRect.y,
                Is.EqualTo(108f / 887f).Within(.000001f));
            Assert.That(
                attendant.UvRect.width,
                Is.EqualTo(213f / 1774f).Within(.000001f));
            Assert.That(
                attendant.UvRect.height,
                Is.EqualTo(728f / 887f).Within(.000001f));
            Assert.That(
                attendant.CellAspectRatio,
                Is.EqualTo(213f / 728f).Within(.000001f));
            Assert.That(
                ballroomAttendant.ResourcePath,
                Is.EqualTo(attendant.ResourcePath));
            Assert.That(
                ballroomAttendant.UvRect,
                Is.EqualTo(attendant.UvRect));
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
