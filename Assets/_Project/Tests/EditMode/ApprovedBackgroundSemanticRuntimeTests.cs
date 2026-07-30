using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wake.Exploration;

namespace Wake.Tests
{
    public sealed class ApprovedBackgroundSemanticRuntimeTests
    {
        private const string SourceHash =
            "0123456789abcdef0123456789abcdef" +
            "0123456789abcdef0123456789abcdef";
        private const string SemanticHash =
            "11111111111111111111111111111111" +
            "11111111111111111111111111111111";
        private const string CastFingerprint =
            "22222222222222222222222222222222" +
            "22222222222222222222222222222222";
        private const string VariantKey =
            "LocationBackgroundVariants/bg_runtime_test";

        private readonly List<UnityEngine.Object> createdObjects = new();

        [SetUp]
        public void SetUp()
        {
            ApprovedBackgroundSemanticResolver.ResetCacheForTests();
        }

        [TearDown]
        public void TearDown()
        {
            ApprovedBackgroundSemanticResolver.ResetCacheForTests();
            foreach (UnityEngine.Object value in createdObjects)
            {
                if (value != null)
                    UnityEngine.Object.DestroyImmediate(value);
            }
            createdObjects.Clear();
        }

        [Test]
        public void Catalog_ExposesBakerIdentityAndApprovalMetadata()
        {
            Sprite sprite = CreateSprite("bg_runtime_test");
            BackgroundSemanticProfile profile = CreateProfile(
                VariantKey,
                Array.Empty<BackgroundSemanticSlot>());
            ApprovedBackgroundSemanticBinding binding =
                CreateBinding(sprite, profile);
            ApprovedBackgroundSemanticCatalog catalog =
                CreateCatalog(binding);

            Assert.That(binding.SourceSprite, Is.SameAs(sprite));
            Assert.That(
                binding.AssetPath,
                Is.EqualTo(
                    "Assets/_Project/Resources/" +
                    "LocationBackgroundVariants/bg_runtime_test.png"));
            Assert.That(binding.SourceSha256, Is.EqualTo(SourceHash));
            Assert.That(
                binding.SemanticContentHash,
                Is.EqualTo(SemanticHash));
            Assert.That(catalog.Reviewer, Is.EqualTo("project-owner"));
            Assert.That(catalog.Revision, Is.EqualTo(2));
            Assert.That(catalog.ApprovedWarnings, Is.True);
            Assert.That(catalog.ApprovedWarningCount, Is.EqualTo(3));
        }

        [Test]
        public void Resolver_MissingDatabaseReturnsFalseForLegacyFallback()
        {
            Sprite sprite = CreateSprite("bg_runtime_test");
            ApprovedBackgroundSemanticResolver.SetCatalogForTests(null);

            bool found = ApprovedBackgroundSemanticResolver.TryResolve(
                "ATRIUM",
                VariantKey,
                sprite,
                out _);

            Assert.That(found, Is.False);
        }

        [Test]
        public void Resolver_RejectsUnapprovedOrMismatchedIdentity()
        {
            Sprite sprite = CreateSprite("bg_runtime_test");
            Sprite otherSprite = CreateSprite("bg_runtime_test_copy");
            BackgroundSemanticProfile profile = CreateProfile(
                VariantKey,
                Array.Empty<BackgroundSemanticSlot>());
            ApprovedBackgroundSemanticBinding binding =
                CreateBinding(sprite, profile);
            ApprovedBackgroundSemanticCatalog catalog =
                CreateCatalog(binding);
            ApprovedBackgroundSemanticResolver.SetCatalogForTests(catalog);

            Assert.That(
                ApprovedBackgroundSemanticResolver.TryResolve(
                    "ATRIUM",
                    VariantKey,
                    sprite,
                    out _),
                Is.True);
            Assert.That(
                ApprovedBackgroundSemanticResolver.TryResolve(
                    "BALLROOM",
                    VariantKey,
                    sprite,
                    out _),
                Is.False);
            Assert.That(
                ApprovedBackgroundSemanticResolver.TryResolve(
                    "ATRIUM",
                    "LocationBackgroundVariants/wrong",
                    sprite,
                    out _),
                Is.False);
            Assert.That(
                ApprovedBackgroundSemanticResolver.TryResolve(
                    "ATRIUM",
                    VariantKey,
                    otherSprite,
                    out _),
                Is.False);
            Assert.That(
                ApprovedBackgroundSemanticResolver.TryResolve(
                    "ATRIUM",
                    VariantKey,
                    sprite,
                    string.Empty,
                    SemanticHash,
                    out _),
                Is.False,
                "The external source-image hash must match the source hash.");

            ApprovedBackgroundSemanticBinding unapproved =
                CreateBinding(
                    sprite,
                    profile,
                    approved: false);
            ApprovedBackgroundSemanticResolver.SetCatalogForTests(
                CreateCatalog(unapproved));
            Assert.That(
                ApprovedBackgroundSemanticResolver.TryResolve(
                    "ATRIUM",
                    VariantKey,
                    sprite,
                    out _),
                Is.False);

            ApprovedBackgroundSemanticResolver.SetCatalogForTests(
                CreateCatalog(binding, approved: false));
            Assert.That(
                ApprovedBackgroundSemanticResolver.TryResolve(
                    "ATRIUM",
                    VariantKey,
                    sprite,
                    out _),
                Is.False);
        }

        [Test]
        public void Resolver_UsesSerializedSpriteNameForLegacyVariant()
        {
            Sprite sprite = CreateSprite("bg_legacy_room");
            string legacyKey =
                ApprovedBackgroundSemanticResolver
                    .BuildSerializedVariantKey(sprite);
            BackgroundSemanticProfile profile = CreateProfile(
                legacyKey,
                Array.Empty<BackgroundSemanticSlot>());
            ApprovedBackgroundSemanticBinding binding =
                CreateBinding(
                    sprite,
                    profile,
                    variantKey: legacyKey);
            ApprovedBackgroundSemanticResolver.SetCatalogForTests(
                CreateCatalog(binding));

            bool found = ApprovedBackgroundSemanticResolver.TryResolve(
                "ATRIUM",
                string.Empty,
                sprite,
                out BackgroundSemanticRuntimeResolution resolution);

            Assert.That(found, Is.True);
            Assert.That(
                resolution.Binding.VariantKey,
                Is.EqualTo("serialized:bg_legacy_room"));
        }

        [Test]
        public void Resolver_ValidatesExactProfileAndCastFingerprint()
        {
            Sprite sprite = CreateSprite("bg_runtime_test");
            BackgroundSemanticSlot slot = Slot(
                "focus",
                .72f,
                BackgroundSemanticSlotRole.Focus);
            BackgroundSemanticProfile profile = CreateProfile(
                VariantKey,
                new[] { slot });
            ApprovedBackgroundSemanticBinding binding =
                CreateBinding(sprite, profile);
            var layout = new ApprovedBackgroundSemanticSceneLayout(
                "D3-02",
                "ATRIUM",
                VariantKey,
                SourceHash,
                approved: true,
                new[]
                {
                    new BackgroundSemanticCharacterSlotBinding(
                        "DANIEL",
                        "focus",
                        BackgroundSemanticCharacterRole.Focus)
                },
                backgroundProfileId: profile.ProfileId,
                castFingerprint: CastFingerprint);
            ApprovedBackgroundSemanticResolver.SetCatalogForTests(
                CreateCatalog(binding, sceneLayouts: new[] { layout }));

            Assert.That(
                ApprovedBackgroundSemanticResolver.TryResolve(
                    "ATRIUM",
                    VariantKey,
                    sprite,
                    "D3-02",
                    SourceHash,
                    CastFingerprint,
                    out BackgroundSemanticRuntimeResolution resolution),
                Is.True);
            Assert.That(resolution.SceneLayout, Is.SameAs(layout));
            Assert.That(
                resolution.CastFingerprint,
                Is.EqualTo(CastFingerprint));
            Assert.That(
                ApprovedBackgroundSemanticResolver.TryResolve(
                    "ATRIUM",
                    VariantKey,
                    sprite,
                    "D3-02",
                    SourceHash,
                    SemanticHash,
                    out _),
                Is.False);
        }

        [Test]
        public void Resolver_RejectsSceneLayoutWithDuplicateSlot()
        {
            Sprite sprite = CreateSprite("bg_runtime_test");
            BackgroundSemanticSlot slot = Slot("shared", .5f);
            BackgroundSemanticProfile profile = CreateProfile(
                VariantKey,
                new[] { slot });
            ApprovedBackgroundSemanticBinding binding =
                CreateBinding(sprite, profile);
            var layout = new ApprovedBackgroundSemanticSceneLayout(
                "D3-02",
                "ATRIUM",
                VariantKey,
                SourceHash,
                approved: true,
                new[]
                {
                    new BackgroundSemanticCharacterSlotBinding(
                        "DANIEL",
                        "shared",
                        BackgroundSemanticCharacterRole.Focus),
                    new BackgroundSemanticCharacterSlotBinding(
                        "HELENA",
                        "shared",
                        BackgroundSemanticCharacterRole.Main)
                },
                backgroundProfileId: profile.ProfileId,
                castFingerprint: CastFingerprint);
            ApprovedBackgroundSemanticResolver.SetCatalogForTests(
                CreateCatalog(binding, sceneLayouts: new[] { layout }));

            Assert.That(
                ApprovedBackgroundSemanticResolver.TryResolve(
                    "ATRIUM",
                    VariantKey,
                    sprite,
                    "D3-02",
                    out _),
                Is.False);
        }

        [Test]
        public void Placement_FixedSceneLayoutTakesPrecedence()
        {
            Sprite sprite = CreateSprite("bg_runtime_test");
            BackgroundSemanticSlot dynamicFirst = Slot(
                "left",
                .25f,
                confidence: .98f);
            BackgroundSemanticSlot fixedFocus = Slot(
                "right",
                .75f,
                confidence: .50f);
            BackgroundSemanticProfile profile = CreateProfile(
                VariantKey,
                new[] { dynamicFirst, fixedFocus });
            ApprovedBackgroundSemanticBinding binding =
                CreateBinding(sprite, profile);
            var layout = new ApprovedBackgroundSemanticSceneLayout(
                "D3-02",
                "ATRIUM",
                VariantKey,
                SourceHash,
                approved: true,
                new[]
                {
                    new BackgroundSemanticCharacterSlotBinding(
                        "DANIEL",
                        "right",
                        BackgroundSemanticCharacterRole.Focus)
                },
                new[] { "CREW_ATTENDANT" },
                profile.ProfileId,
                CastFingerprint);
            BackgroundSemanticRuntimeResolution resolution =
                Resolve(
                    binding,
                    sprite,
                    layout,
                    "D3-02");

            BackgroundSemanticPlacementResult result =
                BackgroundSemanticPlacementResolver.Resolve(
                    resolution,
                    new[]
                    {
                        Request(
                            "CREW_ATTENDANT",
                            BackgroundSemanticCharacterRole.Context),
                        Request(
                            "DANIEL",
                            BackgroundSemanticCharacterRole.Focus)
                    });

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.UsedFixedSceneLayout, Is.True);
            Assert.That(result.Assignments, Has.Count.EqualTo(1));
            Assert.That(
                result.Assignments[0].Slot.Id,
                Is.EqualTo("right"));
            Assert.That(
                result.Assignments[0].FixedBySceneLayout,
                Is.True);
            Assert.That(
                result.OffCameraCharacterIds,
                Does.Contain("CREW_ATTENDANT"));
        }

        [Test]
        public void Placement_FixedCollisionUsesApprovedHorizontalAdjustment()
        {
            Sprite sprite = CreateSprite("bg_runtime_test");
            BackgroundSemanticSlot danielSlot = Slot(
                "daniel-fixed",
                .55f,
                normalizedHeight: .50f,
                confidence: .90f);
            BackgroundSemanticSlot porterSlot = Slot(
                "porter-fixed",
                .60f,
                normalizedHeight: .50f,
                confidence: .95f);
            BackgroundSemanticSlot protectedFallback = Slot(
                "porter-fallback",
                .20f,
                normalizedHeight: .50f,
                confidence: .80f);
            var clue = new BackgroundSemanticZone(
                "fallback-clue",
                BackgroundSemanticZoneKind.Protected,
                new Rect(.15f, .03f, .10f, .45f));
            BackgroundSemanticProfile profile = CreateProfile(
                VariantKey,
                new[]
                {
                    danielSlot,
                    porterSlot,
                    protectedFallback
                },
                new[] { clue });
            ApprovedBackgroundSemanticBinding binding =
                CreateBinding(sprite, profile);
            var layout = new ApprovedBackgroundSemanticSceneLayout(
                "P-01",
                "ATRIUM",
                VariantKey,
                SourceHash,
                approved: true,
                new[]
                {
                    new BackgroundSemanticCharacterSlotBinding(
                        "DANIEL",
                        danielSlot.Id,
                        BackgroundSemanticCharacterRole.Focus),
                    new BackgroundSemanticCharacterSlotBinding(
                        "DOCK_PORTER",
                        porterSlot.Id,
                        BackgroundSemanticCharacterRole.Context)
                },
                backgroundProfileId: profile.ProfileId,
                castFingerprint: CastFingerprint);
            ApprovedBackgroundSemanticCatalog catalog =
                CreateCatalog(
                    binding,
                    sceneLayouts: new[] { layout });
            var resolution =
                new BackgroundSemanticRuntimeResolution(
                    binding,
                    layout,
                    catalog);

            BackgroundSemanticPlacementResult result =
                BackgroundSemanticPlacementResolver.Resolve(
                    resolution,
                    new[]
                    {
                        CatalogRequest(
                            "DOCK_PORTER",
                            BackgroundSemanticCharacterRole.Context),
                        CatalogRequest(
                            "DANIEL",
                            BackgroundSemanticCharacterRole.Focus)
                    });

            Assert.That(
                BackgroundSemanticPlacementResolver
                    .IsGenericSlotAllowed(
                        profile,
                        protectedFallback),
                Is.False);
            Assert.That(result.Assignments, Has.Count.EqualTo(2));
            Assert.That(
                result.Assignments.Select(value => value.Slot.Id),
                Is.EquivalentTo(
                    new[] { "daniel-fixed", "porter-fixed" }));
            Assert.That(
                result.Assignments.All(value =>
                    value.FixedBySceneLayout),
                Is.True);
            Assert.That(result.OffCameraCharacterIds, Is.Empty);
            Assert.That(result.IsValid, Is.True);
            Assert.That(
                result.Assignments.Any(value =>
                {
                    float originalX =
                        value.Character.CharacterId == "DANIEL"
                            ? danielSlot.Anchor.x
                            : porterSlot.Anchor.x;
                    return Mathf.Abs(
                               value.Slot.Anchor.x - originalX) >
                           .0001f;
                }),
                Is.True);
            Assert.That(
                result.Diagnostics.Any(value =>
                    value.Contains(
                        "shifted horizontally",
                        StringComparison.Ordinal)),
                Is.True);
        }

        [Test]
        public void Placement_BacktrackingPreservesDenseFivePersonFixedCast()
        {
            Sprite sprite = CreateSprite("bg_runtime_test");
            BackgroundSemanticSlot[] slots =
            {
                Slot(
                    "near_left",
                    .31f,
                    normalizedHeight: .61f,
                    y: .05f,
                    footprintWidth: .10f,
                    depth: .96f),
                Slot(
                    "near_mid_left",
                    .39f,
                    normalizedHeight: .58f,
                    y: .08f,
                    footprintWidth: .10f,
                    depth: .92f),
                Slot(
                    "near_center",
                    .47f,
                    normalizedHeight: .60f,
                    y: .06f,
                    footprintWidth: .10f,
                    depth: .95f),
                Slot(
                    "near_mid_right",
                    .56f,
                    normalizedHeight: .58f,
                    y: .08f,
                    footprintWidth: .10f,
                    depth: .92f),
                Slot(
                    "near_right",
                    .64f,
                    normalizedHeight: .60f,
                    y: .06f,
                    footprintWidth: .10f,
                    depth: .95f)
            };
            BackgroundSemanticProfile profile = CreateProfile(
                VariantKey,
                slots);
            ApprovedBackgroundSemanticBinding binding =
                CreateBinding(sprite, profile);
            var layout = new ApprovedBackgroundSemanticSceneLayout(
                "D1-07",
                "ATRIUM",
                VariantKey,
                SourceHash,
                approved: true,
                new[]
                {
                    new BackgroundSemanticCharacterSlotBinding(
                        "HELENA",
                        "near_left",
                        BackgroundSemanticCharacterRole.Focus),
                    new BackgroundSemanticCharacterSlotBinding(
                        "MARCUS",
                        "near_mid_left",
                        BackgroundSemanticCharacterRole.Focus),
                    new BackgroundSemanticCharacterSlotBinding(
                        "THOMAS",
                        "near_center",
                        BackgroundSemanticCharacterRole.Focus),
                    new BackgroundSemanticCharacterSlotBinding(
                        "RICHARD",
                        "near_mid_right",
                        BackgroundSemanticCharacterRole.Main),
                    new BackgroundSemanticCharacterSlotBinding(
                        "SHIP_MEDIC",
                        "near_right",
                        BackgroundSemanticCharacterRole.Context)
                },
                backgroundProfileId: profile.ProfileId,
                castFingerprint: CastFingerprint);
            ApprovedBackgroundSemanticCatalog catalog =
                CreateCatalog(
                    binding,
                    sceneLayouts: new[] { layout });
            var resolution =
                new BackgroundSemanticRuntimeResolution(
                    binding,
                    layout,
                    catalog);

            BackgroundSemanticPlacementResult result =
                BackgroundSemanticPlacementResolver.Resolve(
                    resolution,
                    new[]
                    {
                        CatalogRequest(
                            "SHIP_MEDIC",
                            BackgroundSemanticCharacterRole.Context),
                        CatalogRequest(
                            "RICHARD",
                            BackgroundSemanticCharacterRole.Main),
                        CatalogRequest(
                            "THOMAS",
                            BackgroundSemanticCharacterRole.Focus),
                        CatalogRequest(
                            "MARCUS",
                            BackgroundSemanticCharacterRole.Focus),
                        CatalogRequest(
                            "HELENA",
                            BackgroundSemanticCharacterRole.Focus)
                    });

            var originals = slots.ToDictionary(
                value => value.Id,
                StringComparer.OrdinalIgnoreCase);
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Assignments, Has.Count.EqualTo(5));
            Assert.That(result.OffCameraCharacterIds, Is.Empty);
            Assert.That(
                result.Assignments.All(value =>
                    value.FixedBySceneLayout),
                Is.True);
            Assert.That(
                result.Assignments.Any(value =>
                    Mathf.Abs(
                        value.Slot.Anchor.x -
                        originals[value.Slot.Id].Anchor.x) >
                    .0001f),
                Is.True);

            foreach (BackgroundSemanticPlacementAssignment assignment in
                     result.Assignments)
            {
                BackgroundSemanticSlot original =
                    originals[assignment.Slot.Id];
                Assert.That(
                    Mathf.Abs(
                        assignment.Slot.Anchor.x -
                        original.Anchor.x),
                    Is.LessThanOrEqualTo(
                        original.FootprintSize.x * .5f +
                        .0001f));
                Assert.That(
                    assignment.Slot.Anchor.y,
                    Is.EqualTo(original.Anchor.y));
                Assert.That(
                    assignment.Slot.NormalizedHeight,
                    Is.EqualTo(original.NormalizedHeight));
                Assert.That(
                    assignment.Slot.Depth01,
                    Is.EqualTo(original.Depth01));
            }

            Assert.That(
                BackgroundSemanticPlacementResolver.Validate(
                    result.Assignments,
                    new Rect(0f, 0f, 1f, 1f),
                    out string diagnostic),
                Is.True,
                diagnostic);
        }

        [Test]
        public void Silhouette_UsesConservativeHorizontalAlphaBounds()
        {
            Assert.That(
                AmbientWorldCharacterCatalog.TryGetAsset(
                    "DANIEL",
                    out AmbientWorldCharacterAsset daniel),
                Is.True);
            Assert.That(daniel.VisibleLeftMargin, Is.EqualTo(.258f));
            Assert.That(daniel.VisibleRightMargin, Is.EqualTo(.321f));

            Assert.That(
                AmbientWorldCharacterCatalog.TryGetAsset(
                    "DOCK_PORTER",
                    out AmbientWorldCharacterAsset porter),
                Is.True);
            Assert.That(porter.VisibleLeftMargin, Is.EqualTo(.092f));
            Assert.That(porter.VisibleRightMargin, Is.EqualTo(.261f));

            Assert.That(
                AmbientWorldCharacterCatalog.TryGetAsset(
                    "PASSENGER_A",
                    out AmbientWorldCharacterAsset expressionAtlas),
                Is.True);
            Assert.That(
                expressionAtlas.UvRect.width,
                Is.EqualTo(.25f));
            Assert.That(
                expressionAtlas.VisibleLeftMargin,
                Is.EqualTo(.038f));
            Assert.That(
                expressionAtlas.VisibleRightMargin,
                Is.EqualTo(.061f));

            Assert.That(
                AmbientWorldCharacterCatalog.TryGetAsset(
                    "VAULT_GUARD",
                    out AmbientWorldCharacterAsset vaultGuard),
                Is.True);
            Assert.That(
                AmbientWorldCharacterCatalog.TryGetAsset(
                    "CREW_SECURITY",
                    out AmbientWorldCharacterAsset crewSecurity),
                Is.True);
            Assert.That(
                vaultGuard.ResourcePath,
                Is.EqualTo(crewSecurity.ResourcePath));
            Assert.That(
                vaultGuard.VisibleLeftMargin,
                Is.EqualTo(crewSecurity.VisibleLeftMargin));
            Assert.That(
                vaultGuard.VisibleRightMargin,
                Is.EqualTo(crewSecurity.VisibleRightMargin));

            var slot = new BackgroundSemanticSlot(
                "alpha-bounded",
                new Vector2(.50f, .04f),
                depth01: .8f,
                normalizedHeight: .50f,
                footprintSize: new Vector2(.30f, .15f),
                facing: BackgroundSemanticFacing.Right,
                confidence:
                    new BackgroundSemanticConfidence(
                        .8f,
                        "review"));
            var measuredAsset = new AmbientWorldCharacterAsset(
                string.Empty,
                new Rect(0f, 0f, 1f, 1f),
                1f,
                0f,
                0f,
                visibleLeftMargin: .20f,
                visibleRightMargin: .30f);

            Rect silhouette =
                BackgroundSemanticPlacementResolver
                    .CalculateSilhouetteRect(
                        slot,
                        measuredAsset,
                        backgroundAspectRatio: 1f);

            Assert.That(
                silhouette.x,
                Is.EqualTo(.35f).Within(.0001f));
            Assert.That(
                silhouette.width,
                Is.EqualTo(.25f).Within(.0001f),
                "Collision bounds must never be narrower than the " +
                "measured alpha silhouette.");
            Assert.That(
                silhouette.center.x,
                Is.EqualTo(.475f).Within(.0001f),
                "Asymmetric alpha margins must shift the visible bounds.");

            var reviewBoundedSlot = new BackgroundSemanticSlot(
                "review-bounded",
                new Vector2(.50f, .04f),
                depth01: .8f,
                normalizedHeight: .50f,
                footprintSize: new Vector2(.10f, .15f),
                facing: BackgroundSemanticFacing.Right,
                confidence:
                    new BackgroundSemanticConfidence(
                        .8f,
                        "review"));
            Rect reviewBounded =
                BackgroundSemanticPlacementResolver
                    .CalculateSilhouetteRect(
                        reviewBoundedSlot,
                        measuredAsset,
                        backgroundAspectRatio: 1f);
            Assert.That(
                reviewBounded.width,
                Is.EqualTo(.25f).Within(.0001f),
                "A narrow review footprint must not under-report the " +
                "measured alpha silhouette.");
            Assert.That(
                reviewBounded.center.x,
                Is.EqualTo(silhouette.center.x).Within(.0001f));

            var mirroredSlot = new BackgroundSemanticSlot(
                "mirrored-alpha-bounded",
                new Vector2(.50f, .04f),
                depth01: .8f,
                normalizedHeight: .50f,
                footprintSize: new Vector2(.30f, .15f),
                facing: BackgroundSemanticFacing.Left,
                confidence:
                    new BackgroundSemanticConfidence(
                        .8f,
                        "review"));
            Rect mirrored =
                BackgroundSemanticPlacementResolver
                    .CalculateSilhouetteRect(
                        mirroredSlot,
                        measuredAsset,
                        backgroundAspectRatio: 1f);
            Assert.That(
                mirrored.center.x,
                Is.EqualTo(.525f).Within(.0001f),
                "Mirroring must swap asymmetric alpha margins.");
            Assert.That(
                mirrored.width,
                Is.EqualTo(silhouette.width).Within(.0001f));
        }

        [Test]
        public void Placement_SortsFocusMainContextAndUsesUniqueSlots()
        {
            BackgroundSemanticProfile profile = CreateProfile(
                VariantKey,
                new[]
                {
                    Slot("center", .50f, confidence: .95f),
                    Slot("left", .22f, confidence: .85f),
                    Slot("right", .78f, confidence: .75f)
                });
            BackgroundSemanticRuntimeResolution resolution =
                new(
                    CreateBinding(
                        CreateSprite("bg_runtime_test"),
                        profile));

            BackgroundSemanticPlacementResult result =
                BackgroundSemanticPlacementResolver.Resolve(
                    resolution,
                    new[]
                    {
                        Request(
                            "CREW_ATTENDANT",
                            BackgroundSemanticCharacterRole.Context),
                        Request(
                            "HELENA",
                            BackgroundSemanticCharacterRole.Main),
                        Request(
                            "DANIEL",
                            BackgroundSemanticCharacterRole.Focus)
                    });

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Assignments, Has.Count.EqualTo(3));
            Assert.That(
                result.Assignments[0].Character.CharacterId,
                Is.EqualTo("DANIEL"));
            Assert.That(
                result.Assignments[0].Slot.Id,
                Is.EqualTo("center"));
            Assert.That(
                result.Assignments[1].Character.CharacterId,
                Is.EqualTo("HELENA"));
            Assert.That(
                result.Assignments
                    .Select(value => value.Slot.Id)
                    .Distinct()
                    .Count(),
                Is.EqualTo(3));
        }

        [Test]
        public void Placement_RejectsSilhouetteOverlapAndMovesRemainderOffCamera()
        {
            BackgroundSemanticProfile profile = CreateProfile(
                VariantKey,
                new[]
                {
                    Slot("first", .49f, normalizedHeight: .50f),
                    Slot("overlap", .51f, normalizedHeight: .50f)
                });
            BackgroundSemanticRuntimeResolution resolution =
                new(
                    CreateBinding(
                        CreateSprite("bg_runtime_test"),
                        profile));
            AmbientWorldCharacterAsset wideAsset =
                new(
                    string.Empty,
                    new Rect(0f, 0f, 1f, 1f),
                    1f,
                    0f,
                    0f);

            BackgroundSemanticPlacementResult result =
                BackgroundSemanticPlacementResolver.Resolve(
                    resolution,
                    new[]
                    {
                        new BackgroundSemanticCharacterRequest(
                            "DANIEL",
                            BackgroundSemanticCharacterRole.Focus,
                            wideAsset),
                        new BackgroundSemanticCharacterRequest(
                            "CREW_ATTENDANT",
                            BackgroundSemanticCharacterRole.Context,
                            wideAsset)
                    },
                    new Rect(0f, 0f, 1f, 1f),
                    backgroundAspectRatio: 1f);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Assignments, Has.Count.EqualTo(1));
            Assert.That(
                result.Assignments[0].Character.CharacterId,
                Is.EqualTo("DANIEL"));
            Assert.That(
                result.OffCameraCharacterIds,
                Does.Contain("CREW_ATTENDANT"));
        }

        [Test]
        public void Placement_RejectsSlotsOutsideVisibleNormalizedRect()
        {
            BackgroundSemanticProfile profile = CreateProfile(
                VariantKey,
                new[]
                {
                    Slot("cropped-right", .78f, confidence: .99f),
                    Slot("visible-left", .22f, confidence: .50f)
                });
            BackgroundSemanticRuntimeResolution resolution =
                new(
                    CreateBinding(
                        CreateSprite("bg_runtime_test"),
                        profile));

            BackgroundSemanticPlacementResult result =
                BackgroundSemanticPlacementResolver.Resolve(
                    resolution,
                    new[]
                    {
                        Request(
                            "DANIEL",
                            BackgroundSemanticCharacterRole.Focus)
                    },
                    new Rect(0f, 0f, .50f, 1f),
                    backgroundAspectRatio: 1f);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Assignments, Has.Count.EqualTo(1));
            Assert.That(
                result.Assignments[0].Slot.Id,
                Is.EqualTo("visible-left"));
        }

        [Test]
        public void Placement_ApprovedFixedLayoutMayCrossProtectedZone()
        {
            Sprite sprite = CreateSprite("bg_runtime_test");
            BackgroundSemanticSlot protectedSlot = Slot(
                "owner-approved",
                .50f);
            var protectedZone = new BackgroundSemanticZone(
                "main-clue",
                BackgroundSemanticZoneKind.Protected,
                new Rect(.45f, .03f, .10f, .20f));
            BackgroundSemanticProfile profile = CreateProfile(
                VariantKey,
                new[] { protectedSlot },
                new[] { protectedZone });
            ApprovedBackgroundSemanticBinding binding =
                CreateBinding(sprite, profile);
            var layout = new ApprovedBackgroundSemanticSceneLayout(
                "D3-02",
                "ATRIUM",
                VariantKey,
                SourceHash,
                approved: true,
                new[]
                {
                    new BackgroundSemanticCharacterSlotBinding(
                        "DANIEL",
                        "owner-approved",
                        BackgroundSemanticCharacterRole.Focus,
                        hardProtectionOverlap: true)
                },
                backgroundProfileId: profile.ProfileId,
                castFingerprint: CastFingerprint);
            ApprovedBackgroundSemanticCatalog catalog =
                CreateCatalog(
                    binding,
                    sceneLayouts: new[] { layout });
            var strictLayout =
                new ApprovedBackgroundSemanticSceneLayout(
                    "D3-02",
                    "ATRIUM",
                    VariantKey,
                    SourceHash,
                    approved: true,
                    new[]
                    {
                        new BackgroundSemanticCharacterSlotBinding(
                            "DANIEL",
                            "owner-approved",
                            BackgroundSemanticCharacterRole.Focus)
                    },
                    backgroundProfileId: profile.ProfileId,
                    castFingerprint: CastFingerprint);
            ApprovedBackgroundSemanticCatalog strictCatalog =
                CreateCatalog(
                    binding,
                    sceneLayouts: new[] { strictLayout });
            var fixedResolution =
                new BackgroundSemanticRuntimeResolution(
                    binding,
                    layout,
                    catalog);
            var strictResolution =
                new BackgroundSemanticRuntimeResolution(
                    binding,
                    strictLayout,
                    strictCatalog);
            var genericResolution =
                new BackgroundSemanticRuntimeResolution(
                    binding,
                    catalog: catalog);
            BackgroundSemanticCharacterRequest[] cast =
            {
                Request(
                    "DANIEL",
                    BackgroundSemanticCharacterRole.Focus)
            };

            BackgroundSemanticPlacementResult fixedResult =
                BackgroundSemanticPlacementResolver.Resolve(
                    fixedResolution,
                    cast);
            BackgroundSemanticPlacementResult genericResult =
                BackgroundSemanticPlacementResolver.Resolve(
                    genericResolution,
                    cast);
            BackgroundSemanticPlacementResult strictResult =
                BackgroundSemanticPlacementResolver.Resolve(
                    strictResolution,
                    cast);

            Assert.That(
                BackgroundSemanticPlacementResolver
                    .IsGenericSlotAllowed(
                        profile,
                        protectedSlot),
                Is.False);
            Assert.That(
                BackgroundSemanticPlacementResolver
                    .IsApprovedFixedSlotAllowed(
                        profile,
                        protectedSlot),
                Is.True);
            Assert.That(fixedResult.Assignments, Has.Count.EqualTo(1));
            Assert.That(
                fixedResult.Assignments[0].FixedBySceneLayout,
                Is.True);
            Assert.That(
                strictLayout.Assignments[0].HardProtectionOverlap,
                Is.False);
            Assert.That(strictResult.Assignments, Has.Count.EqualTo(1));
            Assert.That(strictResult.OffCameraCharacterIds, Is.Empty);
            Assert.That(genericResult.Assignments, Is.Empty);
            Assert.That(
                genericResult.OffCameraCharacterIds,
                Does.Contain("DANIEL"));
        }

        [Test]
        public void Placement_WalkableValidationUsesFootAnchorNotFootprint()
        {
            var tallFootprintSlot = new BackgroundSemanticSlot(
                "floor-anchor",
                new Vector2(.50f, .04f),
                depth01: .9f,
                normalizedHeight: .55f,
                footprintSize: new Vector2(.10f, .36f),
                confidence:
                    new BackgroundSemanticConfidence(.9f, "review"));
            var shallowFloor = new BackgroundSemanticPolygon(new[]
            {
                new Vector2(.20f, .02f),
                new Vector2(.80f, .02f),
                new Vector2(.75f, .12f),
                new Vector2(.25f, .12f)
            });
            BackgroundSemanticProfile profile = CreateProfile(
                VariantKey,
                new[] { tallFootprintSlot },
                walkablePolygon: shallowFloor);
            var resolution = new BackgroundSemanticRuntimeResolution(
                CreateBinding(
                    CreateSprite("bg_runtime_test"),
                    profile));

            BackgroundSemanticPlacementResult result =
                BackgroundSemanticPlacementResolver.Resolve(
                    resolution,
                    new[]
                    {
                        Request(
                            "DANIEL",
                            BackgroundSemanticCharacterRole.Focus)
                    });

            Assert.That(
                tallFootprintSlot.FootprintRect.yMax,
                Is.GreaterThan(shallowFloor.Bounds.yMax));
            Assert.That(
                BackgroundSemanticPlacementResolver
                    .IsGenericSlotAllowed(
                        profile,
                        tallFootprintSlot),
                Is.True);
            Assert.That(result.Assignments, Has.Count.EqualTo(1));
        }

        [Test]
        public void Placement_GenericProtectsClueSilhouetteButFixedMayOverlap()
        {
            Sprite sprite = CreateSprite("bg_runtime_test");
            BackgroundSemanticSlot slot = Slot(
                "clue-overlap",
                .50f,
                normalizedHeight: .50f);
            var protectedZone = new BackgroundSemanticZone(
                "clue-above-feet",
                BackgroundSemanticZoneKind.Protected,
                new Rect(.44f, .30f, .12f, .15f));
            BackgroundSemanticProfile profile = CreateProfile(
                VariantKey,
                new[] { slot },
                new[] { protectedZone });
            ApprovedBackgroundSemanticBinding binding =
                CreateBinding(sprite, profile);
            var layout = new ApprovedBackgroundSemanticSceneLayout(
                "D3-02",
                "ATRIUM",
                VariantKey,
                SourceHash,
                approved: true,
                new[]
                {
                    new BackgroundSemanticCharacterSlotBinding(
                        "DANIEL",
                        slot.Id,
                        BackgroundSemanticCharacterRole.Focus,
                        hardProtectionOverlap: true)
                },
                backgroundProfileId: profile.ProfileId,
                castFingerprint: CastFingerprint);
            ApprovedBackgroundSemanticCatalog catalog =
                CreateCatalog(
                    binding,
                    sceneLayouts: new[] { layout });
            BackgroundSemanticCharacterRequest[] cast =
            {
                Request(
                    "DANIEL",
                    BackgroundSemanticCharacterRole.Focus)
            };

            BackgroundSemanticPlacementResult generic =
                BackgroundSemanticPlacementResolver.Resolve(
                    new BackgroundSemanticRuntimeResolution(
                        binding,
                        catalog: catalog),
                    cast);
            BackgroundSemanticPlacementResult fixedResult =
                BackgroundSemanticPlacementResolver.Resolve(
                    new BackgroundSemanticRuntimeResolution(
                        binding,
                        layout,
                        catalog),
                    cast);

            Assert.That(
                BackgroundSemanticPlacementResolver
                    .IsGenericSlotAllowed(profile, slot),
                Is.True,
                "The foot anchor is below the protected clue.");
            Assert.That(generic.Assignments, Is.Empty);
            Assert.That(
                generic.OffCameraCharacterIds,
                Does.Contain("DANIEL"));
            Assert.That(fixedResult.Assignments, Has.Count.EqualTo(1));
        }

        [Test]
        public void Placement_ForbiddenZoneUsesFootAnchorOnly()
        {
            BackgroundSemanticSlot slot = Slot(
                "foreground-occlusion",
                .50f,
                normalizedHeight: .50f);
            var foregroundZone = new BackgroundSemanticZone(
                "foreground-furniture",
                BackgroundSemanticZoneKind.Forbidden,
                new Rect(.44f, .30f, .12f, .15f));
            BackgroundSemanticProfile profile = CreateProfile(
                VariantKey,
                new[] { slot },
                new[] { foregroundZone });
            var resolution = new BackgroundSemanticRuntimeResolution(
                CreateBinding(
                    CreateSprite("bg_runtime_test"),
                    profile));
            BackgroundSemanticCharacterRequest character = Request(
                "DANIEL",
                BackgroundSemanticCharacterRole.Focus);
            Rect silhouette =
                BackgroundSemanticPlacementResolver
                    .CalculateSilhouetteRect(
                        slot,
                        character.CharacterAsset,
                        16f / 9f);

            BackgroundSemanticPlacementResult result =
                BackgroundSemanticPlacementResolver.Resolve(
                    resolution,
                    new[] { character });

            Assert.That(
                silhouette.Overlaps(
                    foregroundZone.NormalizedRect,
                    true),
                Is.True);
            Assert.That(
                BackgroundSemanticPlacementResolver
                    .IsGenericSlotAllowed(profile, slot),
                Is.True,
                "Only the foot anchor is forbidden for foreground zones.");
            Assert.That(result.Assignments, Has.Count.EqualTo(1));
        }

        [Test]
        public void StageAdapter_UsesSlotPerspectiveFacingAndVisualGrade()
        {
            BackgroundSemanticSlot slot = Slot(
                "graded",
                .72f,
                normalizedHeight: .52f,
                facing: BackgroundSemanticFacing.Left);
            BackgroundSemanticProfile profile = CreateProfile(
                VariantKey,
                new[] { slot });
            var grade = new BackgroundSemanticSlotVisualGrade(
                "graded",
                new Color(.5f, 1f, .5f, 1f),
                saturationMultiplier: .8f,
                exposureMultiplier: 1.1f,
                contrastMultiplier: .9f,
                softnessOffset: .1f,
                shadowOpacityMultiplier: .5f,
                groundShadowScale: .7f,
                shadowDistance: .02f);
            ApprovedBackgroundSemanticBinding binding =
                CreateBinding(
                    CreateSprite("bg_runtime_test"),
                    profile,
                    grades: new[] { grade });

            bool created = BackgroundSemanticStageAdapter.TryCreate(
                binding,
                slot,
                out AmbientWorldStageProfile stage);

            Assert.That(created, Is.True);
            Assert.That(stage.Anchor, Is.EqualTo(slot.Anchor));
            Assert.That(stage.NormalizedHeight, Is.EqualTo(.52f));
            Assert.That(stage.Mirror, Is.True);
            Assert.That(stage.LightTint.r, Is.EqualTo(.45f).Within(.001f));
            Assert.That(stage.LightTint.g, Is.EqualTo(.8f).Within(.001f));
            Assert.That(stage.Exposure, Is.EqualTo(.88f).Within(.001f));
            Assert.That(stage.Saturation, Is.EqualTo(.56f).Within(.001f));
            Assert.That(stage.Contrast, Is.EqualTo(.72f).Within(.001f));
            Assert.That(stage.Softness, Is.EqualTo(.35f).Within(.001f));
            Assert.That(
                stage.ShadowOpacity,
                Is.EqualTo(.2f).Within(.001f));
            Assert.That(
                stage.GroundShadowScale,
                Is.EqualTo(.7f).Within(.001f));
            Assert.That(
                stage.ShadowDirection.magnitude,
                Is.EqualTo(.02f).Within(.001f));
        }

        private BackgroundSemanticRuntimeResolution Resolve(
            ApprovedBackgroundSemanticBinding binding,
            Sprite sprite,
            ApprovedBackgroundSemanticSceneLayout layout,
            string sceneId)
        {
            ApprovedBackgroundSemanticResolver.SetCatalogForTests(
                CreateCatalog(binding, sceneLayouts: new[] { layout }));
            Assert.That(
                ApprovedBackgroundSemanticResolver.TryResolve(
                    "ATRIUM",
                    binding.VariantKey,
                    sprite,
                    sceneId,
                    out BackgroundSemanticRuntimeResolution resolution),
                Is.True);
            return resolution;
        }

        private ApprovedBackgroundSemanticCatalog CreateCatalog(
            ApprovedBackgroundSemanticBinding binding,
            bool approved = true,
            IEnumerable<ApprovedBackgroundSemanticSceneLayout>
                sceneLayouts = null,
            bool approvedWarnings = true,
            int approvedWarningCount = 3)
        {
            ApprovedBackgroundSemanticCatalog catalog =
                ScriptableObject.CreateInstance<
                    ApprovedBackgroundSemanticCatalog>();
            createdObjects.Add(catalog);
            catalog.Initialize(
                new[] { binding },
                sceneLayouts,
                approved,
                ApprovedBackgroundSemanticCatalog.CurrentSchemaVersion,
                valueReviewer: "project-owner",
                valueRevision: 2,
                valueApprovedAtUtc: "2026-07-29T18:32:30Z",
                valueApprovedWarnings: approvedWarnings,
                valueApprovedWarningCount:
                    approvedWarningCount,
                valueSourceInventoryGeneratedAtUtc:
                    "2026-07-29T18:27:07Z");
            return catalog;
        }

        private static ApprovedBackgroundSemanticBinding CreateBinding(
            Sprite sprite,
            BackgroundSemanticProfile profile,
            bool approved = true,
            string variantKey = VariantKey,
            IEnumerable<BackgroundSemanticSlotVisualGrade> grades = null)
        {
            return new ApprovedBackgroundSemanticBinding(
                "ATRIUM",
                variantKey,
                sprite,
                SourceHash,
                approved,
                profile,
                grades,
                reviewer: "project-owner",
                approvalRevision: 2,
                assetPath:
                    "Assets/_Project/Resources/" +
                    "LocationBackgroundVariants/bg_runtime_test.png",
                semanticContentHash: SemanticHash);
        }

        private static BackgroundSemanticProfile CreateProfile(
            string variantKey,
            IEnumerable<BackgroundSemanticSlot> slots,
            IEnumerable<BackgroundSemanticZone> zones = null,
            BackgroundSemanticPolygon walkablePolygon = null)
        {
            return new BackgroundSemanticProfile(
                "bg_runtime_test",
                "ATRIUM",
                variantKey,
                SourceHash,
                new BackgroundSemanticStatus(
                    BackgroundSemanticProfileState.Approved,
                    "approved",
                    "project-owner",
                    revision: 2),
                new BackgroundSemanticConfidence(
                    .96f,
                    "review",
                    manuallyVerified: true),
                walkablePolygon ??
                new BackgroundSemanticPolygon(new[]
                {
                    new Vector2(.02f, .02f),
                    new Vector2(.98f, .02f),
                    new Vector2(.98f, .90f),
                    new Vector2(.02f, .90f)
                }),
                zones ?? Array.Empty<BackgroundSemanticZone>(),
                slots,
                new BackgroundSemanticLight(
                    new Color(.9f, .8f, .7f, 1f),
                    new Vector2(.2f, .6f),
                    exposure: .8f,
                    saturation: .7f,
                    contrast: .8f,
                    softness: .25f,
                    shadowOpacity: .4f,
                    confidence:
                        new BackgroundSemanticConfidence(
                            .9f,
                            "review")),
                AnimationCurve.Linear(0f, .4f, 1f, .65f),
                generatorSeed: 7,
                requestedSlotCount: 0,
                minimumSlotSpacing: .1f,
                polygonEdgeClearance: 0f,
                generatedFootprintSize: new Vector2(.08f, .15f));
        }

        private static BackgroundSemanticSlot Slot(
            string id,
            float x,
            BackgroundSemanticSlotRole allowedRoles =
                BackgroundSemanticSlotRole.Any,
            float normalizedHeight = .38f,
            float confidence = .8f,
            BackgroundSemanticFacing facing =
                BackgroundSemanticFacing.Automatic,
            float y = .04f,
            float footprintWidth = .08f,
            float depth = .8f)
        {
            return new BackgroundSemanticSlot(
                id,
                new Vector2(x, y),
                depth01: depth,
                normalizedHeight,
                new Vector2(footprintWidth, .15f),
                facing,
                allowedRoles,
                BackgroundSemanticSlotOrigin.Authored,
                confidence:
                    new BackgroundSemanticConfidence(
                        confidence,
                        "review"));
        }

        private static BackgroundSemanticCharacterRequest Request(
            string characterId,
            BackgroundSemanticCharacterRole role)
        {
            var compactAsset = new AmbientWorldCharacterAsset(
                string.Empty,
                new Rect(0f, 0f, 1f, 1f),
                .42f,
                0f,
                0f);
            return new BackgroundSemanticCharacterRequest(
                characterId,
                role,
                compactAsset);
        }

        private static BackgroundSemanticCharacterRequest CatalogRequest(
            string characterId,
            BackgroundSemanticCharacterRole role)
        {
            Assert.That(
                AmbientWorldCharacterCatalog.TryGetAsset(
                    characterId,
                    out AmbientWorldCharacterAsset asset),
                Is.True,
                $"Missing character asset '{characterId}'.");
            return new BackgroundSemanticCharacterRequest(
                characterId,
                role,
                asset);
        }

        private Sprite CreateSprite(string name)
        {
            var texture = new Texture2D(
                160,
                90,
                TextureFormat.RGBA32,
                mipChain: false);
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 160f, 90f),
                new Vector2(.5f, .5f));
            sprite.name = name;
            createdObjects.Add(sprite);
            createdObjects.Add(texture);
            return sprite;
        }
    }
}
