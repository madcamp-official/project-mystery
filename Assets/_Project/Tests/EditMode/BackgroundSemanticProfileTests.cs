using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wake.Exploration;

namespace Wake.Tests
{
    public sealed class BackgroundSemanticProfileTests
    {
        private const string ValidHash =
            "0123456789abcdef0123456789abcdef" +
            "0123456789abcdef0123456789abcdef";

        [Test]
        public void SemanticModels_AreSerializableAndRoundTripCoreData()
        {
            Type[] serializableTypes =
            {
                typeof(BackgroundSemanticProfile),
                typeof(BackgroundSemanticZone),
                typeof(BackgroundSemanticPolygon),
                typeof(BackgroundSemanticSlot),
                typeof(BackgroundSemanticLight),
                typeof(BackgroundSemanticConfidence),
                typeof(BackgroundSemanticStatus)
            };
            foreach (Type type in serializableTypes)
            {
                Assert.That(
                    Attribute.IsDefined(type, typeof(SerializableAttribute)),
                    Is.True,
                    type.Name);
            }

            BackgroundSemanticProfile source = CreateProfile(
                slots: new[]
                {
                    new BackgroundSemanticSlot(
                        "left",
                        new Vector2(.22f, .18f),
                        .75f,
                        .58f,
                        new Vector2(.08f, .24f),
                        BackgroundSemanticFacing.Right,
                        BackgroundSemanticSlotRole.Main,
                        reservationKey: "DANIEL",
                        confidence: new BackgroundSemanticConfidence(
                            .95f,
                            "manual",
                            manuallyVerified: true))
                });

            string json = JsonUtility.ToJson(source);
            BackgroundSemanticProfile restored =
                JsonUtility.FromJson<BackgroundSemanticProfile>(json);

            Assert.That(restored.ProfileId, Is.EqualTo("ATRIUM.default"));
            Assert.That(restored.LocationCode, Is.EqualTo("ATRIUM"));
            Assert.That(restored.VariantId, Is.EqualTo("default"));
            Assert.That(restored.WalkablePolygons, Has.Count.EqualTo(1));
            Assert.That(restored.WalkablePolygon.Vertices, Has.Count.EqualTo(4));
            Assert.That(restored.Zones, Has.Count.EqualTo(2));
            Assert.That(restored.Slots, Has.Count.EqualTo(1));
            Assert.That(
                restored.Slots[0].ReservationKey,
                Is.EqualTo("DANIEL"));
            Assert.That(
                restored.Slots[0].Confidence.Level,
                Is.EqualTo(BackgroundSemanticConfidenceLevel.Verified));
            Assert.That(
                restored.Status.State,
                Is.EqualTo(BackgroundSemanticProfileState.Approved));
        }

        [Test]
        public void Generator_IsDeterministicAndAvoidsRestrictedZones()
        {
            BackgroundSemanticProfile profile = CreateProfile();
            var settings =
                new BackgroundSemanticSlotGenerationSettings(
                    requestedCount: 5,
                    sampleCount: 1200,
                    minimumSpacing: .12f,
                    edgeClearance: .015f,
                    footprintSize: new Vector2(.08f, .22f),
                    seed: 7341);

            BackgroundSemanticSlotGenerationResult first =
                BackgroundSemanticSlotGenerator.Generate(
                    profile,
                    settings);
            BackgroundSemanticSlotGenerationResult second =
                BackgroundSemanticSlotGenerator.Generate(
                    profile,
                    settings);

            Assert.That(first.Slots, Has.Count.EqualTo(5));
            Assert.That(second.Slots, Has.Count.EqualTo(first.Slots.Count));
            for (int index = 0; index < first.Slots.Count; index++)
            {
                Assert.That(
                    second.Slots[index].Id,
                    Is.EqualTo(first.Slots[index].Id));
                Assert.That(
                    second.Slots[index].Anchor,
                    Is.EqualTo(first.Slots[index].Anchor));
                Assert.That(
                    BackgroundSemanticSlotGenerator.IsSlotAllowed(
                        profile,
                        first.Slots[index].Anchor,
                        first.Slots[index].FootprintSize,
                        settings.EdgeClearance),
                    Is.True,
                    first.Slots[index].Id);

                foreach (BackgroundSemanticZone zone in profile.Zones)
                {
                    Assert.That(
                        first.Slots[index].FootprintRect.Overlaps(
                            zone.ExpandedRect,
                            true),
                        Is.False,
                        $"{first.Slots[index].Id}/{zone.Id}");
                }
            }

            for (int firstIndex = 0;
                 firstIndex < first.Slots.Count;
                 firstIndex++)
            {
                for (int secondIndex = firstIndex + 1;
                     secondIndex < first.Slots.Count;
                     secondIndex++)
                {
                    Assert.That(
                        Vector2.Distance(
                            first.Slots[firstIndex].Anchor,
                            first.Slots[secondIndex].Anchor),
                        Is.GreaterThanOrEqualTo(
                            settings.MinimumSpacing - .0001f));
                }
            }
        }

        [Test]
        public void Validator_AcceptsValidProfileAndGeneratedSlots()
        {
            BackgroundSemanticProfile profile = CreateProfile();
            BackgroundSemanticSlotGenerationResult generated =
                BackgroundSemanticSlotGenerator.Generate(profile);

            var diagnostics = BackgroundSemanticValidator.Validate(
                profile,
                generated.Slots,
                ValidHash);

            Assert.That(
                BackgroundSemanticValidator.HasErrors(diagnostics),
                Is.False,
                string.Join(
                    Environment.NewLine,
                    diagnostics.Select(item =>
                        $"{item.Code}: {item.Message}")));
        }

        [Test]
        public void Validator_ReportsChangedSourceImageHash()
        {
            BackgroundSemanticProfile profile = CreateProfile();
            const string changedHash =
                "ffffffffffffffffffffffffffffffff" +
                "ffffffffffffffffffffffffffffffff";

            var diagnostics = BackgroundSemanticValidator.Validate(
                profile,
                expectedSourceImageHash: changedHash);

            Assert.That(
                diagnostics.Select(item => item.Code),
                Does.Contain(
                    BackgroundSemanticDiagnosticCode.SourceHashMismatch));
        }

        [Test]
        public void Validator_ReportsHashDuplicatesRangesAndRestrictedSlots()
        {
            var duplicateAndBlocked = new[]
            {
                new BackgroundSemanticSlot(
                    "duplicate",
                    new Vector2(.48f, .20f),
                    .5f,
                    .55f,
                    new Vector2(.12f, .30f)),
                new BackgroundSemanticSlot(
                    "duplicate",
                    new Vector2(.48f, .20f),
                    1.2f,
                    .95f,
                    new Vector2(.12f, .30f))
            };
            BackgroundSemanticProfile profile = CreateProfile(
                sourceHash: "not-a-sha256",
                zones: new[]
                {
                    new BackgroundSemanticZone(
                        "blocked",
                        BackgroundSemanticZoneKind.Forbidden,
                        new Rect(.40f, .18f, .20f, .35f)),
                    new BackgroundSemanticZone(
                        "blocked",
                        BackgroundSemanticZoneKind.Protected,
                        new Rect(.72f, .20f, .18f, .28f))
                },
                slots: duplicateAndBlocked);

            var codes = BackgroundSemanticValidator.Validate(profile)
                .Select(item => item.Code)
                .ToArray();

            Assert.That(
                codes,
                Does.Contain(
                    BackgroundSemanticDiagnosticCode.InvalidSourceHash));
            Assert.That(
                codes,
                Does.Contain(
                    BackgroundSemanticDiagnosticCode.DuplicateZoneId));
            Assert.That(
                codes,
                Does.Contain(
                    BackgroundSemanticDiagnosticCode.DuplicateSlotId));
            Assert.That(
                codes,
                Does.Contain(
                    BackgroundSemanticDiagnosticCode.DuplicateSlotAnchor));
            Assert.That(
                codes,
                Does.Contain(
                    BackgroundSemanticDiagnosticCode.InvalidSlot));
            Assert.That(
                codes,
                Does.Contain(
                    BackgroundSemanticDiagnosticCode
                        .SlotIntersectsRestrictedZone));
        }

        [Test]
        public void Generator_SupportsWalkableIslandsAndIgnoresUncertainZones()
        {
            var polygons = new[]
            {
                new BackgroundSemanticPolygon(new[]
                {
                    new Vector2(.05f, .05f),
                    new Vector2(.42f, .05f),
                    new Vector2(.42f, .38f),
                    new Vector2(.05f, .38f)
                }),
                new BackgroundSemanticPolygon(new[]
                {
                    new Vector2(.58f, .05f),
                    new Vector2(.95f, .05f),
                    new Vector2(.95f, .38f),
                    new Vector2(.58f, .38f)
                })
            };
            var profile = new BackgroundSemanticProfile(
                "SERVICE_RAIL.islands",
                "SERVICE_RAIL",
                "islands",
                ValidHash,
                new BackgroundSemanticStatus(
                    BackgroundSemanticProfileState.Approved),
                new BackgroundSemanticConfidence(.88f, "manual"),
                polygons,
                new[]
                {
                    new BackgroundSemanticZone(
                        "uncertain-grate",
                        BackgroundSemanticZoneKind.Uncertain,
                        new Rect(.05f, .05f, .37f, .33f))
                },
                Array.Empty<BackgroundSemanticSlot>(),
                new BackgroundSemanticLight(),
                AnimationCurve.Linear(0f, .42f, 1f, .62f),
                generatorSeed: 81,
                requestedSlotCount: 2,
                minimumSlotSpacing: .40f,
                polygonEdgeClearance: .01f,
                generatedFootprintSize: new Vector2(.06f, .20f));

            BackgroundSemanticSlotGenerationResult generated =
                BackgroundSemanticSlotGenerator.Generate(
                    profile,
                    new BackgroundSemanticSlotGenerationSettings(
                        requestedCount: 2,
                        sampleCount: 256,
                        minimumSpacing: .40f,
                        edgeClearance: .01f,
                        footprintSize: new Vector2(.06f, .20f),
                        seed: 81));

            Assert.That(generated.Slots, Has.Count.EqualTo(2));
            Assert.That(
                generated.Slots.Any(slot => slot.Anchor.x < .5f),
                Is.True);
            Assert.That(
                generated.Slots.Any(slot => slot.Anchor.x > .5f),
                Is.True);
            Assert.That(
                BackgroundSemanticSlotGenerator.IsSlotAllowed(
                    profile,
                    new Vector2(.20f, .08f),
                    new Vector2(.06f, .20f),
                    .01f),
                Is.True,
                "Low-confidence geometry is review metadata, not a hard obstacle.");
            Assert.That(
                BackgroundSemanticValidator.HasErrors(
                    BackgroundSemanticValidator.Validate(
                        profile,
                        generated.Slots,
                        ValidHash)),
                Is.False);
        }

        [Test]
        public void UnusedProfile_GeneratesNoSlotsAndReportsRetainedData()
        {
            BackgroundSemanticProfile profile = CreateProfile(
                status: new BackgroundSemanticStatus(
                    BackgroundSemanticProfileState.Unused),
                requestedSlotCount: 3);

            BackgroundSemanticSlotGenerationResult generated =
                BackgroundSemanticSlotGenerator.Generate(profile);
            var diagnostics =
                BackgroundSemanticValidator.Validate(profile);

            Assert.That(generated.Slots, Is.Empty);
            Assert.That(
                diagnostics.Select(item => item.Code),
                Does.Contain(
                    BackgroundSemanticDiagnosticCode
                        .UnusedProfileContainsSemanticData));
        }

        private static BackgroundSemanticProfile CreateProfile(
            string sourceHash = ValidHash,
            BackgroundSemanticStatus status = null,
            BackgroundSemanticZone[] zones = null,
            BackgroundSemanticSlot[] slots = null,
            int requestedSlotCount = 5)
        {
            return new BackgroundSemanticProfile(
                "ATRIUM.default",
                "ATRIUM",
                "default",
                sourceHash,
                status ?? new BackgroundSemanticStatus(
                    BackgroundSemanticProfileState.Approved,
                    "reviewed",
                    "test",
                    revision: 1),
                new BackgroundSemanticConfidence(
                    .94f,
                    "manual",
                    manuallyVerified: true),
                new BackgroundSemanticPolygon(new[]
                {
                    new Vector2(.05f, .08f),
                    new Vector2(.95f, .08f),
                    new Vector2(.90f, .72f),
                    new Vector2(.10f, .72f)
                }),
                zones ?? new[]
                {
                    new BackgroundSemanticZone(
                        "main-clue",
                        BackgroundSemanticZoneKind.Protected,
                        new Rect(.41f, .22f, .18f, .30f),
                        clearance: .01f),
                    new BackgroundSemanticZone(
                        "service-door",
                        BackgroundSemanticZoneKind.Forbidden,
                        new Rect(.74f, .28f, .16f, .27f),
                        clearance: .01f)
                },
                slots ?? Array.Empty<BackgroundSemanticSlot>(),
                new BackgroundSemanticLight(
                    new Color(.95f, .87f, .74f, 1f),
                    new Vector2(.30f, .60f),
                    .82f,
                    .72f,
                    .88f,
                    .28f,
                    .34f,
                    new BackgroundSemanticConfidence(.82f, "sampled")),
                AnimationCurve.Linear(0f, .42f, 1f, .62f),
                generatorSeed: 7341,
                requestedSlotCount: requestedSlotCount,
                minimumSlotSpacing: .12f,
                polygonEdgeClearance: .015f,
                generatedFootprintSize: new Vector2(.08f, .22f));
        }
    }
}
