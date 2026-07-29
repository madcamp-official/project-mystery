using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Wake.Exploration;

namespace Wake.Editor
{
    /// <summary>
    /// Bakes the project-owner-approved semantic review into the runtime
    /// Resources catalog. The source JSON remains the authoring source of
    /// truth; the generated ScriptableObject is the only data runtime code
    /// reads.
    /// </summary>
    public static class ApprovedBackgroundSemanticCatalogBaker
    {
        public const string RuntimeManifestPath =
            "Documentation/CharacterStagingReview/Data/" +
            "approved_background_semantic_runtime.json";
        public const string ScreenshotBaselinePath =
            "Documentation/CharacterStagingReview/Data/" +
            "scene_screenshot_baselines.json";
        public const string CatalogFolder =
            "Assets/_Project/Resources/BackgroundSemantics";
        public const string CatalogAssetPath =
            CatalogFolder +
            "/ApprovedBackgroundSemanticCatalog.asset";

        public const string MissingManifestCode =
            "SEMANTIC_RUNTIME_MANIFEST_MISSING";
        public const string InvalidManifestCode =
            "SEMANTIC_RUNTIME_MANIFEST_INVALID";
        public const string SourceHashMismatchCode =
            "SEMANTIC_SOURCE_HASH_MISMATCH";
        public const string MissingSpriteCode =
            "SEMANTIC_SOURCE_SPRITE_MISSING";
        public const string CatalogMismatchCode =
            "SEMANTIC_CATALOG_MISMATCH";
        public const string MissingScreenshotCode =
            "SEMANTIC_SCREENSHOT_MISSING";
        public const string ScreenshotHashMismatchCode =
            "SEMANTIC_SCREENSHOT_HASH_MISMATCH";
        public const string ReceiptMismatchCode =
            "SEMANTIC_BAKE_RECEIPT_MISMATCH";
        public const string CastFingerprintMismatchCode =
            "SEMANTIC_CAST_FINGERPRINT_MISMATCH";
        public const string RuntimePlacementInvalidCode =
            "SEMANTIC_RUNTIME_PLACEMENT_INVALID";
        public const string MissingCharacterAssetCode =
            "SEMANTIC_VISIBLE_CHARACTER_ASSET_MISSING";

        private const string SchemaVersion = "1.0";
        private const float FloatTolerance = .0001f;
        private static readonly string[] ExplicitlyUnusedLocations =
        {
            "BALLAST_TANKS",
            "GENERATOR",
            "LAUNDRY",
            "SERVICE_HUB",
            "STABILIZERS",
            "WORKSHOP"
        };

        [MenuItem(
            "Wake/Exploration/Bake Approved Background Semantics")]
        public static ApprovedBackgroundSemanticCatalog Bake()
        {
            RuntimeManifest manifest = LoadRuntimeManifest(
                RuntimeManifestPath,
                out List<string> errors);
            ScreenshotBaselineManifest baselines =
                LoadScreenshotBaselines(
                    ScreenshotBaselinePath,
                    errors);
            ValidateManifest(manifest, errors);
            ValidateScreenshotBaselines(
                baselines,
                manifest,
                errors);
            ThrowIfInvalid(errors);

            List<ApprovedBackgroundSemanticBinding> bindings =
                BuildBindings(manifest);
            List<ApprovedBackgroundSemanticSceneLayout> layouts =
                BuildSceneLayouts(manifest, bindings);

            EnsureAssetFolder(CatalogFolder);
            ApprovedBackgroundSemanticCatalog catalog =
                AssetDatabase.LoadAssetAtPath<
                    ApprovedBackgroundSemanticCatalog>(
                    CatalogAssetPath);
            if (catalog == null)
            {
                if (File.Exists(ToFullPath(CatalogAssetPath)))
                {
                    throw new BuildFailedException(
                        $"{CatalogAssetPath} exists but is not an " +
                        "ApprovedBackgroundSemanticCatalog.");
                }

                catalog = ScriptableObject.CreateInstance<
                    ApprovedBackgroundSemanticCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
            }

            catalog.Initialize(
                bindings,
                layouts,
                valueApproved: true,
                valueSchemaVersion:
                    ApprovedBackgroundSemanticCatalog
                        .CurrentSchemaVersion,
                valueReviewer: manifest.reviewer,
                valueRevision: manifest.revision,
                valueApprovedAtUtc: manifest.approvedAtUtc,
                valueApprovedWarnings: manifest.approvedWarnings,
                valueApprovedWarningCount:
                    manifest.approvedWarningCount,
                valueSourceInventoryGeneratedAtUtc:
                    manifest.sourceInventoryGeneratedAtUtc);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            WriteBakeReceipt(
                BuildReceipt(manifest, baselines));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            IReadOnlyList<string> validationErrors =
                ValidateProject();
            ThrowIfInvalid(validationErrors);
            Debug.Log(
                $"Approved background semantic catalog baked: " +
                $"{bindings.Count} bindings, {layouts.Count} scene " +
                $"layouts -> {CatalogAssetPath}");
            return AssetDatabase.LoadAssetAtPath<
                ApprovedBackgroundSemanticCatalog>(
                CatalogAssetPath);
        }

        [MenuItem(
            "Wake/Exploration/Validate Approved Background Semantics")]
        public static void ValidateProjectFromMenu()
        {
            IReadOnlyList<string> errors = ValidateProject();
            ThrowIfInvalid(errors);
            Debug.Log(
                "Approved background semantic catalog validation passed.");
        }

        public static IReadOnlyList<string> ValidateProject()
        {
            var errors = new List<string>();
            RuntimeManifest manifest = LoadRuntimeManifest(
                RuntimeManifestPath,
                out List<string> manifestErrors);
            errors.AddRange(manifestErrors);
            ScreenshotBaselineManifest baselines =
                LoadScreenshotBaselines(
                    ScreenshotBaselinePath,
                    errors);
            ValidateManifest(manifest, errors);
            ValidateScreenshotBaselines(
                baselines,
                manifest,
                errors);

            ApprovedBackgroundSemanticCatalog catalog =
                AssetDatabase.LoadAssetAtPath<
                    ApprovedBackgroundSemanticCatalog>(
                    CatalogAssetPath);
            if (catalog == null)
            {
                errors.Add(
                    $"{CatalogMismatchCode}: runtime catalog is missing " +
                    $"at {CatalogAssetPath}.");
                return Deduplicate(errors);
            }

            if (!catalog.IsUsable)
            {
                errors.Add(
                    $"{CatalogMismatchCode}: runtime catalog is not " +
                    "approved or has an unsupported schema version.");
            }
            else if (manifest != null &&
                     (catalog.Reviewer != manifest.reviewer ||
                      catalog.Revision != manifest.revision ||
                      catalog.ApprovedAtUtc !=
                      manifest.approvedAtUtc ||
                      catalog.ApprovedWarnings !=
                      manifest.approvedWarnings ||
                      catalog.ApprovedWarningCount !=
                      manifest.approvedWarningCount ||
                      catalog.SourceInventoryGeneratedAtUtc !=
                      manifest.sourceInventoryGeneratedAtUtc))
            {
                errors.Add(
                    $"{CatalogMismatchCode}: catalog approval metadata " +
                    "does not match the approved runtime manifest.");
            }

            if (manifest != null)
            {
                ValidateCatalogAgainstManifest(
                    catalog,
                    manifest,
                    errors);
                ValidateBakeReceipt(
                    manifest,
                    baselines,
                    errors);
            }

            return Deduplicate(errors);
        }

        /// <summary>
        /// Standalone baseline validation entry point used by regression
        /// tooling and EditMode tests.
        /// </summary>
        public static IReadOnlyList<string> ValidateScreenshotBaselines(
            string baselineManifestPath)
        {
            var errors = new List<string>();
            ScreenshotBaselineManifest baselines =
                LoadScreenshotBaselines(
                    baselineManifestPath,
                    errors);
            ValidateScreenshotBaselines(
                baselines,
                runtimeManifest: null,
                errors);
            return Deduplicate(errors);
        }

        public static string ComputeFileSha256(string path)
        {
            string fullPath = ToFullPath(path);
            using SHA256 sha = SHA256.Create();
            using FileStream stream = File.OpenRead(fullPath);
            return ToLowerHex(sha.ComputeHash(stream));
        }

        public static string ComputeCurrentCastFingerprint(
            string sceneId)
        {
            if (!ScenePresenceCatalog.TryGet(
                    sceneId,
                    out ScenePresenceRecord scene))
            {
                return string.Empty;
            }

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(scene.ContextSpeaker))
            {
                parts.Add(
                    $"{scene.ContextSpeaker}/ContextNpc/false");
            }

            foreach (SceneWorldCharacter character in
                     ScenePresencePresentationPolicy.SelectVisible(
                         scene,
                         scene.FocusLocation,
                         visibleLimit: 4))
            {
                parts.Add(
                    $"{character.CharacterId}/Main/" +
                    character.IsFocusParticipant
                        .ToString()
                        .ToLowerInvariant());
            }

            string canonical = string.Join(
                "|",
                parts.OrderBy(
                    value => value,
                    StringComparer.Ordinal));
            using SHA256 sha = SHA256.Create();
            return ToLowerHex(
                sha.ComputeHash(
                    new UTF8Encoding(false).GetBytes(canonical)));
        }

        public static void ThrowIfInvalid(
            IReadOnlyList<string> errors)
        {
            if (errors == null || errors.Count == 0)
                return;

            throw new BuildFailedException(
                "Approved background semantic preflight failed:\n" +
                string.Join("\n", errors));
        }

        private static RuntimeManifest LoadRuntimeManifest(
            string path,
            out List<string> errors)
        {
            errors = new List<string>();
            string fullPath = ToFullPath(path);
            if (!File.Exists(fullPath))
            {
                errors.Add(
                    $"{MissingManifestCode}: {path}");
                return null;
            }

            try
            {
                RuntimeManifest manifest =
                    JsonUtility.FromJson<RuntimeManifest>(
                        File.ReadAllText(fullPath, Encoding.UTF8));
                if (manifest == null)
                {
                    errors.Add(
                        $"{InvalidManifestCode}: could not parse {path}.");
                }

                return manifest;
            }
            catch (Exception exception)
            {
                errors.Add(
                    $"{InvalidManifestCode}: {path}: " +
                    exception.Message);
                return null;
            }
        }

        private static ScreenshotBaselineManifest
            LoadScreenshotBaselines(
                string path,
                ICollection<string> errors)
        {
            string fullPath = ToFullPath(path);
            if (!File.Exists(fullPath))
            {
                errors.Add(
                    $"{MissingScreenshotCode}: baseline manifest is " +
                    $"missing at {path}.");
                return null;
            }

            try
            {
                ScreenshotBaselineManifest manifest =
                    JsonUtility.FromJson<ScreenshotBaselineManifest>(
                        File.ReadAllText(fullPath, Encoding.UTF8));
                if (manifest == null)
                {
                    errors.Add(
                        $"{InvalidManifestCode}: could not parse {path}.");
                }

                return manifest;
            }
            catch (Exception exception)
            {
                errors.Add(
                    $"{InvalidManifestCode}: {path}: " +
                    exception.Message);
                return null;
            }
        }

        private static void ValidateManifest(
            RuntimeManifest manifest,
            ICollection<string> errors)
        {
            if (manifest == null)
                return;

            if (!string.Equals(
                    manifest.schemaVersion,
                    SchemaVersion,
                    StringComparison.Ordinal) ||
                !manifest.runtimeConnected ||
                !string.Equals(
                    manifest.approvalStatus,
                    "Approved",
                    StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(manifest.reviewer) ||
                manifest.revision < 1 ||
                string.IsNullOrWhiteSpace(manifest.approvedAtUtc))
            {
                errors.Add(
                    $"{InvalidManifestCode}: runtime manifest must be " +
                    "approved, runtime-connected, reviewed, and revisioned.");
            }

            HashSet<string> unused = NormalizeSet(
                manifest.excludedUnusedLocationCodes);
            if (!unused.SetEquals(ExplicitlyUnusedLocations))
            {
                errors.Add(
                    $"{InvalidManifestCode}: excluded locations must be " +
                    string.Join(", ", ExplicitlyUnusedLocations) + ".");
            }

            RuntimeProfile[] profiles =
                manifest.profiles ?? Array.Empty<RuntimeProfile>();
            if (profiles.Length == 0)
            {
                errors.Add(
                    $"{InvalidManifestCode}: no approved profiles exist.");
                return;
            }

            var profileIds = new HashSet<string>(
                StringComparer.Ordinal);
            var bindingKeys = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            var boundLocations = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            foreach (RuntimeProfile profile in profiles)
            {
                if (profile == null ||
                    string.IsNullOrWhiteSpace(profile.profileId) ||
                    !profileIds.Add(profile.profileId))
                {
                    errors.Add(
                        $"{InvalidManifestCode}: profile IDs must be " +
                        "non-empty and unique.");
                    continue;
                }

                ValidateSha256(
                    profile.sourceSha256,
                    $"{profile.profileId} source",
                    errors);
                ValidateSha256(
                    profile.semanticContentHash,
                    $"{profile.profileId} semantic content",
                    errors);

                string[] locations = NormalizeValues(
                    profile.locationCodes);
                string[] variants = NormalizeValues(
                    profile.variantKeys,
                    uppercase: false);
                if (locations.Length == 0 || variants.Length == 0)
                {
                    errors.Add(
                        $"{InvalidManifestCode}: {profile.profileId} " +
                        "must bind at least one location and variant.");
                }

                foreach (string location in locations)
                {
                    boundLocations.Add(location);
                    if (unused.Contains(location))
                    {
                        errors.Add(
                            $"{InvalidManifestCode}: unused location " +
                            $"{location} is present in " +
                            $"{profile.profileId}.");
                    }

                    foreach (string variant in variants)
                    {
                        if (!bindingKeys.Add(
                                BindingKey(location, variant)))
                        {
                            errors.Add(
                                $"{InvalidManifestCode}: duplicate " +
                                $"binding {location}/{variant}.");
                        }
                    }
                }

                ValidateSourceAsset(profile, errors);
                ValidateProfileGeometry(profile, errors);
            }

            var currentPlayableLocations = new HashSet<string>(
                CanonicalLocationCatalog.Playable.Select(
                    location => location.Code),
                StringComparer.OrdinalIgnoreCase);
            if (!boundLocations.SetEquals(currentPlayableLocations))
            {
                errors.Add(
                    $"{InvalidManifestCode}: approved bindings must cover " +
                    "every current playable location exactly; missing=" +
                    string.Join(
                        ",",
                        currentPlayableLocations.Except(
                            boundLocations,
                            StringComparer.OrdinalIgnoreCase)) +
                    "; extra=" +
                    string.Join(
                        ",",
                        boundLocations.Except(
                            currentPlayableLocations,
                            StringComparer.OrdinalIgnoreCase)) +
                    ".");
            }

            var profileById = profiles
                .Where(profile =>
                    profile != null &&
                    !string.IsNullOrWhiteSpace(profile.profileId))
                .GroupBy(profile => profile.profileId, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.First(),
                    StringComparer.Ordinal);
            var scenes = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            foreach (RuntimeSceneLayout scene in
                     manifest.sceneLayouts ??
                     Array.Empty<RuntimeSceneLayout>())
            {
                ValidateScene(
                    scene,
                    profileById,
                    unused,
                    scenes,
                    errors);
            }

            var currentScenes = new HashSet<string>(
                ScenePresenceCatalog.All.Select(scene => scene.SceneId),
                StringComparer.OrdinalIgnoreCase);
            if (!scenes.SetEquals(currentScenes))
            {
                errors.Add(
                    $"{InvalidManifestCode}: approved scene layouts must " +
                    "cover every current production scene exactly once.");
            }
        }

        private static void ValidateSourceAsset(
            RuntimeProfile profile,
            ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(profile.assetPath) ||
                !profile.assetPath.StartsWith(
                    "Assets/",
                    StringComparison.Ordinal))
            {
                errors.Add(
                    $"{InvalidManifestCode}: {profile.profileId} has an " +
                    "invalid assetPath.");
                return;
            }

            string fullPath = ToFullPath(profile.assetPath);
            if (!File.Exists(fullPath))
            {
                errors.Add(
                    $"{MissingSpriteCode}: source image is missing for " +
                    $"{profile.profileId}: {profile.assetPath}.");
                return;
            }

            string actualHash = ComputeFileSha256(profile.assetPath);
            if (!string.Equals(
                    actualHash,
                    profile.sourceSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(
                    $"{SourceHashMismatchCode}: {profile.profileId}: " +
                    $"expected {profile.sourceSha256}, actual " +
                    $"{actualHash}. Re-export and re-approve the image.");
            }

            Sprite sprite =
                AssetDatabase.LoadAssetAtPath<Sprite>(
                    profile.assetPath);
            if (sprite == null)
            {
                errors.Add(
                    $"{MissingSpriteCode}: {profile.assetPath} is not " +
                    "imported as a Sprite.");
            }
        }

        private static void ValidateProfileGeometry(
            RuntimeProfile profile,
            ICollection<string> errors)
        {
            RuntimePolygon[] walkable =
                profile.walkablePolygons ??
                Array.Empty<RuntimePolygon>();
            if (walkable.Length == 0 ||
                walkable.Any(value =>
                    value == null ||
                    value.points == null ||
                    value.points.Length < 3))
            {
                errors.Add(
                    $"{InvalidManifestCode}: {profile.profileId} needs " +
                    "valid walkable polygons.");
            }

            RuntimeSlot[] slots =
                profile.candidateSlots ?? Array.Empty<RuntimeSlot>();
            var slotIds = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            foreach (RuntimeSlot slot in slots)
            {
                if (slot == null ||
                    string.IsNullOrWhiteSpace(slot.id) ||
                    !slotIds.Add(slot.id) ||
                    slot.foot == null ||
                    !IsNormalized(slot.foot.x) ||
                    !IsNormalized(slot.foot.y) ||
                    !IsNormalized(slot.depth01) ||
                    slot.normalizedHeight <= 0f ||
                    slot.normalizedHeight > 1f ||
                    !IsNormalized(slot.confidence) ||
                    slot.grade == null ||
                    !TryParseHtmlColor(
                        slot.grade.tintHex,
                        out _))
                {
                    errors.Add(
                        $"{InvalidManifestCode}: {profile.profileId} has " +
                        "an invalid or duplicate candidate slot.");
                    break;
                }
            }

            if (slots.Length == 0)
            {
                errors.Add(
                    $"{InvalidManifestCode}: {profile.profileId} has no " +
                    "approved candidate slots.");
            }
        }

        private static void ValidateScene(
            RuntimeSceneLayout scene,
            IReadOnlyDictionary<string, RuntimeProfile> profileById,
            ISet<string> unused,
            ISet<string> seenScenes,
            ICollection<string> errors)
        {
            if (scene == null ||
                string.IsNullOrWhiteSpace(scene.sceneId) ||
                !seenScenes.Add(scene.sceneId) ||
                string.IsNullOrWhiteSpace(scene.locationCode) ||
                unused.Contains(
                    NormalizeCode(scene.locationCode)) ||
                string.IsNullOrWhiteSpace(
                    scene.backgroundProfileId) ||
                !profileById.TryGetValue(
                    scene.backgroundProfileId,
                    out RuntimeProfile profile))
            {
                errors.Add(
                    $"{InvalidManifestCode}: scene layouts must have a " +
                    "unique ID, playable location, and existing profile.");
                return;
            }

            string location = NormalizeCode(scene.locationCode);
            if (!NormalizeSet(profile.locationCodes).Contains(location))
            {
                errors.Add(
                    $"{InvalidManifestCode}: {scene.sceneId} location " +
                    $"{location} is not bound to " +
                    $"{profile.profileId}.");
            }

            if (ScenePresenceCatalog.TryGet(
                    scene.sceneId,
                    out ScenePresenceRecord currentScene))
            {
                string currentLocation =
                    CanonicalLocationCatalog.FindSpec(
                        currentScene.FocusLocation)?.Code ??
                    currentScene.FocusLocation;
                string currentVariant =
                    LocationBackgroundVariantCatalog.ResolveResourceKey(
                        currentLocation,
                        currentScene.SceneId);
                if (!string.Equals(
                        location,
                        NormalizeCode(currentLocation),
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        scene.variantKey?.Trim() ?? string.Empty,
                        currentVariant?.Trim() ?? string.Empty,
                        StringComparison.Ordinal))
                {
                    errors.Add(
                        $"{InvalidManifestCode}: {scene.sceneId} " +
                        "location/variant no longer matches the current " +
                        "scene catalogs. Re-export and re-approve.");
                }
            }

            string variant = ResolveSceneVariant(scene, profile);
            if (string.IsNullOrEmpty(variant) ||
                !NormalizeValues(
                    profile.variantKeys,
                    uppercase: false)
                    .Contains(variant, StringComparer.Ordinal))
            {
                errors.Add(
                    $"{InvalidManifestCode}: {scene.sceneId} variant " +
                    "does not match its profile.");
            }

            ValidateSha256(
                scene.castFingerprint,
                $"{scene.sceneId} cast fingerprint",
                errors);
            string currentCastFingerprint =
                ComputeCurrentCastFingerprint(scene.sceneId);
            if (string.IsNullOrEmpty(currentCastFingerprint) ||
                !string.Equals(
                    currentCastFingerprint,
                    scene.castFingerprint,
                    StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(
                    $"{CastFingerprintMismatchCode}: " +
                    $"{scene.sceneId}: approved " +
                    $"{scene.castFingerprint}, current " +
                    $"{currentCastFingerprint}. Re-export and " +
                    "re-approve scene staging.");
            }

            var characters = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            var slotIds = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            var validSlots = new HashSet<string>(
                (profile.candidateSlots ??
                 Array.Empty<RuntimeSlot>())
                .Where(slot => slot != null)
                .Select(slot => slot.id),
                StringComparer.OrdinalIgnoreCase);
            foreach (RuntimeAssignment assignment in
                     scene.assignments ??
                     Array.Empty<RuntimeAssignment>())
            {
                string character =
                    NormalizeCode(assignment?.characterId);
                if (string.IsNullOrEmpty(character) ||
                    !characters.Add(character))
                {
                    errors.Add(
                        $"{InvalidManifestCode}: {scene.sceneId} has a " +
                        "duplicate or empty cast assignment.");
                    continue;
                }

                if (assignment.offCamera)
                    continue;
                if (!AmbientWorldCharacterCatalog.TryGetAsset(
                        character,
                        out _))
                {
                    errors.Add(
                        $"{MissingCharacterAssetCode}: " +
                        $"{scene.sceneId}/{character} has no runtime " +
                        "world character asset.");
                }
                if (string.IsNullOrWhiteSpace(assignment.slotId) ||
                    !validSlots.Contains(assignment.slotId) ||
                    !slotIds.Add(assignment.slotId))
                {
                    errors.Add(
                        $"{InvalidManifestCode}: {scene.sceneId} has an " +
                        "invalid or duplicate visible slot assignment.");
                }
            }

            foreach (string value in
                     scene.offCameraCharacters ??
                     Array.Empty<string>())
            {
                string character = NormalizeCode(value);
                if (string.IsNullOrEmpty(character) ||
                    !characters.Contains(character))
                {
                    errors.Add(
                        $"{InvalidManifestCode}: {scene.sceneId} has an " +
                        "off-camera character missing from assignments.");
                }
            }
        }

        private static void ValidateScreenshotBaselines(
            ScreenshotBaselineManifest baselines,
            RuntimeManifest runtimeManifest,
            ICollection<string> errors)
        {
            if (baselines == null)
                return;

            if (!string.Equals(
                    baselines.schemaVersion,
                    SchemaVersion,
                    StringComparison.Ordinal) ||
                !baselines.runtimeConnected ||
                !string.Equals(
                    baselines.approvalStatus,
                    "Approved",
                    StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(baselines.reviewer) ||
                baselines.revision < 1)
            {
                errors.Add(
                    $"{InvalidManifestCode}: screenshot baselines must " +
                    "be approved and runtime-connected.");
            }

            var sceneIds = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            foreach (ScreenshotBaseline scene in
                     baselines.scenes ??
                     Array.Empty<ScreenshotBaseline>())
            {
                if (scene == null ||
                    string.IsNullOrWhiteSpace(scene.sceneId) ||
                    !sceneIds.Add(scene.sceneId) ||
                    string.IsNullOrWhiteSpace(scene.path))
                {
                    errors.Add(
                        $"{InvalidManifestCode}: screenshot scene IDs and " +
                        "paths must be non-empty and unique.");
                    continue;
                }

                ValidateSha256(
                    scene.sha256,
                    $"{scene.sceneId} screenshot",
                    errors);
                if (!TryResolveProjectPath(
                        scene.path,
                        out string fullPath))
                {
                    errors.Add(
                        $"{InvalidManifestCode}: screenshot path escapes " +
                        $"the project: {scene.path}.");
                    continue;
                }

                if (!File.Exists(fullPath))
                {
                    errors.Add(
                        $"{MissingScreenshotCode}: {scene.sceneId}: " +
                        $"{scene.path}.");
                    continue;
                }

                string actualHash = ComputeFileSha256(fullPath);
                if (!string.Equals(
                        actualHash,
                        scene.sha256,
                        StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add(
                        $"{ScreenshotHashMismatchCode}: " +
                        $"{scene.sceneId}: expected {scene.sha256}, " +
                        $"actual {actualHash}.");
                }
            }

            if (runtimeManifest == null)
                return;

            HashSet<string> runtimeScenes = NormalizeSet(
                (runtimeManifest.sceneLayouts ??
                 Array.Empty<RuntimeSceneLayout>())
                .Select(scene => scene?.sceneId));
            if (!sceneIds.SetEquals(runtimeScenes))
            {
                errors.Add(
                    $"{InvalidManifestCode}: screenshot baselines must " +
                    "cover every approved scene exactly once.");
            }

            if (!string.Equals(
                    baselines.reviewer,
                    runtimeManifest.reviewer,
                    StringComparison.Ordinal) ||
                baselines.revision != runtimeManifest.revision ||
                !string.Equals(
                    baselines.approvedAtUtc,
                    runtimeManifest.approvedAtUtc,
                    StringComparison.Ordinal))
            {
                errors.Add(
                    $"{InvalidManifestCode}: screenshot and runtime " +
                    "approval metadata do not match.");
            }
        }

        private static List<ApprovedBackgroundSemanticBinding>
            BuildBindings(RuntimeManifest manifest)
        {
            var bindings =
                new List<ApprovedBackgroundSemanticBinding>();
            foreach (RuntimeProfile source in manifest.profiles)
            {
                Sprite sprite =
                    AssetDatabase.LoadAssetAtPath<Sprite>(
                        source.assetPath);
                foreach (string location in
                         NormalizeValues(source.locationCodes))
                {
                    foreach (string variant in
                             NormalizeValues(
                                 source.variantKeys,
                                 uppercase: false))
                    {
                        BackgroundSemanticProfile profile =
                            BuildProfile(
                                source,
                                location,
                                variant,
                                manifest);
                        bindings.Add(
                            new ApprovedBackgroundSemanticBinding(
                                location,
                                variant,
                                sprite,
                                source.sourceSha256,
                                approved: true,
                                profile,
                                BuildSlotGrades(source),
                                manifest.reviewer,
                                manifest.revision,
                                source.assetPath,
                                source.semanticContentHash));
                    }
                }
            }

            return bindings
                .OrderBy(binding => binding.LocationCode)
                .ThenBy(binding => binding.VariantKey)
                .ToList();
        }

        private static BackgroundSemanticProfile BuildProfile(
            RuntimeProfile source,
            string location,
            string variant,
            RuntimeManifest manifest)
        {
            var walkable = (source.walkablePolygons ??
                            Array.Empty<RuntimePolygon>())
                .Select(ToPolygon)
                .ToArray();
            var zones = new List<BackgroundSemanticZone>();
            AddZones(
                zones,
                source.forbiddenZones,
                BackgroundSemanticZoneKind.Forbidden);
            AddZones(
                zones,
                source.protectedZones,
                BackgroundSemanticZoneKind.Protected);
            AddZones(
                zones,
                source.uncertainZones,
                BackgroundSemanticZoneKind.Uncertain);
            var slots = (source.candidateSlots ??
                         Array.Empty<RuntimeSlot>())
                .Select(slot => new BackgroundSemanticSlot(
                    slot.id,
                    new Vector2(slot.foot.x, slot.foot.y),
                    slot.depth01,
                    slot.normalizedHeight,
                    new Vector2(.10f, .36f),
                    BackgroundSemanticFacing.Automatic,
                    BackgroundSemanticSlotRole.Any,
                    BackgroundSemanticSlotOrigin.Authored,
                    confidence: new BackgroundSemanticConfidence(
                        slot.confidence,
                        "approved-semantic-review",
                        manuallyVerified: true)))
                .ToArray();
            Vector2 direction = source.lighting?.direction != null
                ? new Vector2(
                    source.lighting.direction.x,
                    source.lighting.direction.y)
                : new Vector2(.35f, .65f);
            if (direction.sqrMagnitude > Mathf.Epsilon)
                direction.Normalize();
            var light = new BackgroundSemanticLight(
                Color.white,
                direction,
                exposure: 1f,
                saturation: 1f,
                contrast: 1f,
                softness: 0f,
                shadowOpacity: .35f,
                confidence: new BackgroundSemanticConfidence(
                    1f,
                    "approved-semantic-review",
                    manuallyVerified: true));
            float averageConfidence = slots.Length == 0
                ? 0f
                : Mathf.Clamp01(
                    (source.candidateSlots ??
                     Array.Empty<RuntimeSlot>())
                    .Average(slot => slot.confidence));
            string note =
                $"Semantic content SHA-256: " +
                $"{source.semanticContentHash}; source asset: " +
                $"{source.assetPath}; approved at " +
                $"{manifest.approvedAtUtc}.";

            return new BackgroundSemanticProfile(
                source.profileId,
                location,
                variant,
                source.sourceSha256,
                new BackgroundSemanticStatus(
                    BackgroundSemanticProfileState.Approved,
                    note,
                    manifest.reviewer,
                    manifest.revision),
                new BackgroundSemanticConfidence(
                    averageConfidence,
                    "approved-semantic-review",
                    manuallyVerified: true),
                walkable,
                zones,
                slots,
                light,
                BuildDepthCurve(source),
                generatorSeed: StableSeed(source.profileId),
                requestedSlotCount: slots.Length,
                minimumSlotSpacing: 0f,
                polygonEdgeClearance: 0f,
                generatedFootprintSize: new Vector2(.10f, .36f));
        }

        private static IEnumerable<BackgroundSemanticSlotVisualGrade>
            BuildSlotGrades(RuntimeProfile profile)
        {
            foreach (RuntimeSlot slot in
                     profile.candidateSlots ??
                     Array.Empty<RuntimeSlot>())
            {
                RuntimeGrade grade = slot.grade;
                TryParseHtmlColor(
                    grade.tintHex,
                    out Color tint);
                yield return new BackgroundSemanticSlotVisualGrade(
                    slot.id,
                    tint,
                    grade.saturation,
                    grade.exposure,
                    grade.contrast,
                    grade.softness,
                    shadowOpacityMultiplier: 1f,
                    groundShadowScale: .62f,
                    shadowDistance: .018f);
            }
        }

        private static List<ApprovedBackgroundSemanticSceneLayout>
            BuildSceneLayouts(
                RuntimeManifest manifest,
                IReadOnlyList<ApprovedBackgroundSemanticBinding>
                    bindings)
        {
            var profiles = (manifest.profiles ??
                            Array.Empty<RuntimeProfile>())
                .ToDictionary(
                    profile => profile.profileId,
                    StringComparer.Ordinal);
            var layouts =
                new List<ApprovedBackgroundSemanticSceneLayout>();
            foreach (RuntimeSceneLayout source in
                     manifest.sceneLayouts ??
                     Array.Empty<RuntimeSceneLayout>())
            {
                RuntimeProfile runtimeProfile =
                    profiles[source.backgroundProfileId];
                string variant =
                    ResolveSceneVariant(source, runtimeProfile);
                ApprovedBackgroundSemanticBinding[] matchingBindings =
                    bindings.Where(value =>
                            string.Equals(
                                value.Profile.ProfileId,
                                source.backgroundProfileId,
                                StringComparison.Ordinal) &&
                            string.Equals(
                                value.VariantKey,
                                variant,
                                StringComparison.Ordinal))
                        .ToArray();
                var assignments =
                    (source.assignments ??
                     Array.Empty<RuntimeAssignment>())
                    .Where(value =>
                        value != null &&
                        !value.offCamera)
                    .Select(value =>
                        new BackgroundSemanticCharacterSlotBinding(
                            value.characterId,
                            value.slotId,
                            ToRole(value),
                            value.hardProtectionOverlap))
                    .ToArray();
                var offCamera = new HashSet<string>(
                    NormalizeValues(
                        source.offCameraCharacters),
                    StringComparer.OrdinalIgnoreCase);
                foreach (RuntimeAssignment value in
                         source.assignments ??
                         Array.Empty<RuntimeAssignment>())
                {
                    if (value != null && value.offCamera)
                        offCamera.Add(NormalizeCode(value.characterId));
                }

                foreach (ApprovedBackgroundSemanticBinding binding in
                         matchingBindings)
                {
                    layouts.Add(
                        new ApprovedBackgroundSemanticSceneLayout(
                            source.sceneId,
                            binding.LocationCode,
                            variant,
                            binding.SourceImageHash,
                            approved: true,
                            assignments,
                            offCamera,
                            source.backgroundProfileId,
                            source.castFingerprint));
                }
            }

            return layouts
                .OrderBy(layout => layout.SceneId)
                .ThenBy(layout => layout.LocationCode)
                .ThenBy(layout => layout.VariantKey)
                .ToList();
        }

        private static void ValidateCatalogAgainstManifest(
            ApprovedBackgroundSemanticCatalog catalog,
            RuntimeManifest manifest,
            ICollection<string> errors)
        {
            List<ApprovedBackgroundSemanticBinding> expected =
                BuildBindings(manifest);
            var actualByKey = catalog.Bindings
                .Where(value => value != null)
                .GroupBy(
                    value => BindingKey(
                        value.LocationCode,
                        value.VariantKey),
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToArray(),
                    StringComparer.OrdinalIgnoreCase);
            if (catalog.Bindings.Count != expected.Count)
            {
                errors.Add(
                    $"{CatalogMismatchCode}: expected {expected.Count} " +
                    $"bindings, found {catalog.Bindings.Count}.");
            }

            foreach (ApprovedBackgroundSemanticBinding value in expected)
            {
                string key = BindingKey(
                    value.LocationCode,
                    value.VariantKey);
                if (!actualByKey.TryGetValue(
                        key,
                        out ApprovedBackgroundSemanticBinding[] matches) ||
                    matches.Length != 1 ||
                    !BindingsEqual(matches[0], value))
                {
                    errors.Add(
                        $"{CatalogMismatchCode}: stale or missing " +
                        $"binding {key}. Re-bake the catalog.");
                }
            }

            foreach (ApprovedBackgroundSemanticBinding binding in
                     catalog.Bindings.Where(value => value != null))
            {
                if (ExplicitlyUnusedLocations.Contains(
                        binding.LocationCode,
                        StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add(
                        $"{CatalogMismatchCode}: unused location " +
                        $"{binding.LocationCode} is runtime-bound.");
                }

                if (!binding.IsApproved)
                {
                    errors.Add(
                        $"{CatalogMismatchCode}: unapproved binding " +
                        $"{binding.LocationCode}/{binding.VariantKey}.");
                }

                string spritePath =
                    AssetDatabase.GetAssetPath(binding.SourceSprite);
                if (string.IsNullOrEmpty(spritePath) ||
                    !File.Exists(ToFullPath(spritePath)))
                {
                    errors.Add(
                        $"{MissingSpriteCode}: source sprite is missing " +
                        $"for {binding.LocationCode}/" +
                        $"{binding.VariantKey}.");
                }
                else
                {
                    string actualHash =
                        ComputeFileSha256(spritePath);
                    if (!string.Equals(
                            actualHash,
                            binding.SourceImageHash,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add(
                            $"{SourceHashMismatchCode}: runtime binding " +
                            $"{binding.LocationCode}/" +
                            $"{binding.VariantKey} no longer matches " +
                            "its source image.");
                    }
                }
            }

            List<ApprovedBackgroundSemanticSceneLayout>
                expectedLayouts =
                    BuildSceneLayouts(manifest, expected);
            var actualLayouts = catalog.SceneLayouts
                .Where(layout => layout != null)
                .GroupBy(
                    SceneLayoutKey,
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToArray(),
                    StringComparer.OrdinalIgnoreCase);
            if (catalog.SceneLayouts.Count != expectedLayouts.Count)
            {
                errors.Add(
                    $"{CatalogMismatchCode}: expected " +
                    $"{expectedLayouts.Count} scene layouts, found " +
                    $"{catalog.SceneLayouts.Count}.");
            }

            foreach (ApprovedBackgroundSemanticSceneLayout expectedLayout
                     in expectedLayouts)
            {
                if (!actualLayouts.TryGetValue(
                        SceneLayoutKey(expectedLayout),
                        out ApprovedBackgroundSemanticSceneLayout[]
                            matches) ||
                    matches.Length != 1 ||
                    !SceneLayoutsEqual(matches[0], expectedLayout))
                {
                    errors.Add(
                        $"{CatalogMismatchCode}: stale or missing scene " +
                        $"layout {SceneLayoutKey(expectedLayout)}.");
                    continue;
                }

                ApprovedBackgroundSemanticBinding binding =
                    catalog.Bindings.SingleOrDefault(value =>
                        value != null &&
                        string.Equals(
                            value.LocationCode,
                            matches[0].LocationCode,
                            StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(
                            value.VariantKey,
                            matches[0].VariantKey,
                            StringComparison.Ordinal));
                if (binding == null ||
                    !matches[0].IsValidFor(binding.Profile))
                {
                    errors.Add(
                        $"{CatalogMismatchCode}: scene layout " +
                        $"{matches[0].SceneId} fails profile integrity.");
                }
            }

            ValidateRuntimePlacements(
                catalog,
                manifest,
                errors);
        }

        private static void ValidateRuntimePlacements(
            ApprovedBackgroundSemanticCatalog catalog,
            RuntimeManifest manifest,
            ICollection<string> errors)
        {
            var sourceScenes = (manifest.sceneLayouts ??
                                Array.Empty<RuntimeSceneLayout>())
                .ToDictionary(
                    scene => NormalizeCode(scene.sceneId),
                    StringComparer.OrdinalIgnoreCase);
            foreach (ApprovedBackgroundSemanticSceneLayout layout in
                     catalog.SceneLayouts.Where(value => value != null))
            {
                if (!sourceScenes.TryGetValue(
                        layout.SceneId,
                        out RuntimeSceneLayout source))
                {
                    continue;
                }

                ApprovedBackgroundSemanticBinding binding =
                    catalog.Bindings.SingleOrDefault(value =>
                        value != null &&
                        value.LocationCode == layout.LocationCode &&
                        value.VariantKey == layout.VariantKey &&
                        value.Profile.ProfileId ==
                        layout.BackgroundProfileId);
                if (binding == null)
                    continue;

                BackgroundSemanticCharacterRequest[] requests =
                    (source.assignments ??
                     Array.Empty<RuntimeAssignment>())
                    .Where(value => value != null)
                    .Select(value =>
                        new BackgroundSemanticCharacterRequest(
                            value.characterId,
                            ToRole(value)))
                    .ToArray();
                BackgroundSemanticPlacementResult result =
                    BackgroundSemanticPlacementResolver.Resolve(
                        new BackgroundSemanticRuntimeResolution(
                            binding,
                            layout,
                            catalog),
                        requests);
                var expectedOffCamera = new HashSet<string>(
                    NormalizeValues(
                        source.offCameraCharacters),
                    StringComparer.OrdinalIgnoreCase);
                foreach (RuntimeAssignment assignment in
                         source.assignments ??
                         Array.Empty<RuntimeAssignment>())
                {
                    if (assignment != null && assignment.offCamera)
                    {
                        expectedOffCamera.Add(
                            NormalizeCode(assignment.characterId));
                    }
                }

                var actualOffCamera = new HashSet<string>(
                    result.OffCameraCharacterIds,
                    StringComparer.OrdinalIgnoreCase);
                var actualAssignments = result.Assignments
                    .ToDictionary(
                        assignment =>
                            assignment.Character.CharacterId,
                        StringComparer.OrdinalIgnoreCase);
                var expectedVisible = new HashSet<string>(
                    (source.assignments ??
                     Array.Empty<RuntimeAssignment>())
                    .Where(assignment =>
                        assignment != null &&
                        !assignment.offCamera)
                    .Select(assignment =>
                        NormalizeCode(assignment.characterId)),
                    StringComparer.OrdinalIgnoreCase);
                bool visibleCastPreserved =
                    expectedVisible.SetEquals(
                        actualAssignments.Keys);
                if (!result.IsValid ||
                    !actualOffCamera.SetEquals(expectedOffCamera) ||
                    !visibleCastPreserved)
                {
                    errors.Add(
                        $"{RuntimePlacementInvalidCode}: " +
                        $"{SceneLayoutKey(layout)} does not preserve " +
                        "the approved safe cast layout. off-camera=" +
                        string.Join(",", actualOffCamera) +
                        "; diagnostics=" +
                        string.Join(" | ", result.Diagnostics));
                }
            }
        }

        private static bool BindingsEqual(
            ApprovedBackgroundSemanticBinding actual,
            ApprovedBackgroundSemanticBinding expected)
        {
            if (actual == null ||
                expected == null ||
                !actual.IsApproved ||
                actual.Reviewer != expected.Reviewer ||
                actual.ApprovalRevision !=
                expected.ApprovalRevision ||
                actual.SourceSprite != expected.SourceSprite ||
                actual.AssetPath != expected.AssetPath ||
                !string.Equals(
                    actual.SourceImageHash,
                    expected.SourceImageHash,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    actual.SemanticContentHash,
                    expected.SemanticContentHash,
                    StringComparison.OrdinalIgnoreCase) ||
                actual.Profile == null ||
                expected.Profile == null ||
                actual.Profile.ProfileId !=
                expected.Profile.ProfileId ||
                actual.Profile.LocationCode !=
                expected.Profile.LocationCode ||
                actual.Profile.VariantId !=
                expected.Profile.VariantId ||
                actual.Profile.Status.State !=
                BackgroundSemanticProfileState.Approved ||
                actual.Profile.Status.Note !=
                expected.Profile.Status.Note ||
                actual.Profile.WalkablePolygons.Count !=
                expected.Profile.WalkablePolygons.Count ||
                actual.Profile.Zones.Count !=
                expected.Profile.Zones.Count ||
                actual.Profile.Slots.Count !=
                expected.Profile.Slots.Count ||
                actual.SlotVisualGrades.Count !=
                expected.SlotVisualGrades.Count)
            {
                return false;
            }

            for (int index = 0;
                 index < actual.Profile.WalkablePolygons.Count;
                 index++)
            {
                IReadOnlyList<Vector2> first =
                    actual.Profile.WalkablePolygons[index].Vertices;
                IReadOnlyList<Vector2> second =
                    expected.Profile.WalkablePolygons[index].Vertices;
                if (first.Count != second.Count)
                    return false;
                for (int point = 0; point < first.Count; point++)
                {
                    if (!Approximately(first[point], second[point]))
                        return false;
                }
            }

            for (int index = 0;
                 index < actual.Profile.Zones.Count;
                 index++)
            {
                BackgroundSemanticZone first =
                    actual.Profile.Zones[index];
                BackgroundSemanticZone second =
                    expected.Profile.Zones[index];
                if (first.Id != second.Id ||
                    first.Kind != second.Kind ||
                    !Approximately(
                        first.NormalizedRect,
                        second.NormalizedRect))
                {
                    return false;
                }
            }

            for (int index = 0;
                 index < actual.Profile.Slots.Count;
                 index++)
            {
                BackgroundSemanticSlot first =
                    actual.Profile.Slots[index];
                BackgroundSemanticSlot second =
                    expected.Profile.Slots[index];
                if (first.Id != second.Id ||
                    !Approximately(first.Anchor, second.Anchor) ||
                    !Approximately(
                        first.Depth01,
                        second.Depth01) ||
                    !Approximately(
                        first.NormalizedHeight,
                        second.NormalizedHeight))
                {
                    return false;
                }
            }

            for (int index = 0;
                 index < actual.SlotVisualGrades.Count;
                 index++)
            {
                BackgroundSemanticSlotVisualGrade first =
                    actual.SlotVisualGrades[index];
                BackgroundSemanticSlotVisualGrade second =
                    expected.SlotVisualGrades[index];
                if (first.SlotId != second.SlotId ||
                    !Approximately(
                        first.LightTintMultiplier,
                        second.LightTintMultiplier) ||
                    !Approximately(
                        first.SaturationMultiplier,
                        second.SaturationMultiplier) ||
                    !Approximately(
                        first.ExposureMultiplier,
                        second.ExposureMultiplier) ||
                    !Approximately(
                        first.ContrastMultiplier,
                        second.ContrastMultiplier) ||
                    !Approximately(
                        first.SoftnessOffset,
                        second.SoftnessOffset))
                {
                    return false;
                }
            }

            return Approximately(
                       actual.Profile.Light.Direction,
                       expected.Profile.Light.Direction) &&
                   Approximately(
                       actual.Profile.Light.Exposure,
                       expected.Profile.Light.Exposure) &&
                   Approximately(
                       actual.Profile.Light.Saturation,
                       expected.Profile.Light.Saturation) &&
                   Approximately(
                       actual.Profile.Light.Contrast,
                       expected.Profile.Light.Contrast) &&
                   Approximately(
                       actual.Profile.Light.Softness,
                       expected.Profile.Light.Softness);
        }

        private static bool SceneLayoutsEqual(
            ApprovedBackgroundSemanticSceneLayout actual,
            ApprovedBackgroundSemanticSceneLayout expected)
        {
            if (actual == null ||
                expected == null ||
                !actual.Approved ||
                actual.SceneId != expected.SceneId ||
                actual.LocationCode != expected.LocationCode ||
                actual.VariantKey != expected.VariantKey ||
                actual.BackgroundProfileId !=
                expected.BackgroundProfileId ||
                !string.Equals(
                    actual.CastFingerprint,
                    expected.CastFingerprint,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    actual.SourceImageHash,
                    expected.SourceImageHash,
                    StringComparison.OrdinalIgnoreCase) ||
                actual.Assignments.Count !=
                expected.Assignments.Count ||
                actual.OffCameraCharacterIds.Count !=
                expected.OffCameraCharacterIds.Count)
            {
                return false;
            }

            for (int index = 0;
                 index < actual.Assignments.Count;
                 index++)
            {
                BackgroundSemanticCharacterSlotBinding first =
                    actual.Assignments[index];
                BackgroundSemanticCharacterSlotBinding second =
                    expected.Assignments[index];
                if (first.CharacterId != second.CharacterId ||
                    first.SlotId != second.SlotId ||
                    first.Role != second.Role ||
                    first.HardProtectionOverlap !=
                    second.HardProtectionOverlap)
                {
                    return false;
                }
            }

            return new HashSet<string>(
                    actual.OffCameraCharacterIds,
                    StringComparer.OrdinalIgnoreCase)
                .SetEquals(expected.OffCameraCharacterIds);
        }

        private static void WriteBakeReceipt(BakeReceipt receipt)
        {
            AssetImporter importer =
                AssetImporter.GetAtPath(CatalogAssetPath);
            if (importer == null)
            {
                throw new BuildFailedException(
                    "Could not write semantic bake receipt to " +
                    CatalogAssetPath + ".");
            }

            importer.userData =
                JsonUtility.ToJson(receipt);
            importer.SaveAndReimport();
        }

        private static void ValidateBakeReceipt(
            RuntimeManifest manifest,
            ScreenshotBaselineManifest baselines,
            ICollection<string> errors)
        {
            AssetImporter importer =
                AssetImporter.GetAtPath(CatalogAssetPath);
            if (importer == null ||
                string.IsNullOrWhiteSpace(importer.userData))
            {
                errors.Add(
                    $"{ReceiptMismatchCode}: catalog bake receipt is " +
                    "missing.");
                return;
            }

            BakeReceipt actual;
            try
            {
                actual =
                    JsonUtility.FromJson<BakeReceipt>(
                        importer.userData);
            }
            catch (Exception exception)
            {
                errors.Add(
                    $"{ReceiptMismatchCode}: invalid receipt: " +
                    exception.Message);
                return;
            }

            BakeReceipt expected =
                BuildReceipt(manifest, baselines);
            if (actual == null ||
                actual.runtimeManifestSha256 !=
                expected.runtimeManifestSha256 ||
                actual.screenshotBaselineManifestSha256 !=
                expected.screenshotBaselineManifestSha256 ||
                actual.reviewer != expected.reviewer ||
                actual.revision != expected.revision ||
                !ReceiptEntriesEqual(
                    actual.profileSemantics,
                    expected.profileSemantics) ||
                !ReceiptEntriesEqual(
                    actual.sceneCasts,
                    expected.sceneCasts))
            {
                errors.Add(
                    $"{ReceiptMismatchCode}: approved JSON, semantic " +
                    "content/cast fingerprint, screenshot baselines, or " +
                    "catalog bake is stale.");
            }
        }

        private static BakeReceipt BuildReceipt(
            RuntimeManifest manifest,
            ScreenshotBaselineManifest baselines)
        {
            return new BakeReceipt
            {
                schemaVersion = 1,
                runtimeManifestSha256 =
                    ComputeFileSha256(RuntimeManifestPath),
                screenshotBaselineManifestSha256 =
                    ComputeFileSha256(ScreenshotBaselinePath),
                reviewer = manifest.reviewer,
                revision = manifest.revision,
                approvedAtUtc = manifest.approvedAtUtc,
                profileSemantics = (manifest.profiles ??
                                    Array.Empty<RuntimeProfile>())
                    .OrderBy(profile => profile.profileId)
                    .Select(profile => new ReceiptEntry
                    {
                        id = profile.profileId,
                        value = profile.semanticContentHash
                    })
                    .ToArray(),
                sceneCasts = (manifest.sceneLayouts ??
                              Array.Empty<RuntimeSceneLayout>())
                    .OrderBy(scene => scene.sceneId)
                    .Select(scene => new ReceiptEntry
                    {
                        id = scene.sceneId,
                        value = scene.castFingerprint
                    })
                    .ToArray(),
                screenshotCount =
                    baselines?.scenes?.Length ?? 0
            };
        }

        private static bool ReceiptEntriesEqual(
            ReceiptEntry[] first,
            ReceiptEntry[] second)
        {
            first ??= Array.Empty<ReceiptEntry>();
            second ??= Array.Empty<ReceiptEntry>();
            return first.Length == second.Length &&
                   first.OrderBy(value => value.id)
                       .Zip(
                           second.OrderBy(value => value.id),
                           (left, right) =>
                               left != null &&
                               right != null &&
                               left.id == right.id &&
                               left.value == right.value)
                       .All(value => value);
        }

        private static void AddZones(
            ICollection<BackgroundSemanticZone> target,
            RuntimeZone[] source,
            BackgroundSemanticZoneKind kind)
        {
            int index = 0;
            foreach (RuntimeZone zone in
                     source ?? Array.Empty<RuntimeZone>())
            {
                index++;
                RuntimeRect sourceRect =
                    HasArea(zone.normalizedRect)
                        ? zone.normalizedRect
                        : zone.rect;
                Rect rect = ToRect(
                    sourceRect,
                    zone.points);
                string id = !string.IsNullOrWhiteSpace(zone.objectId)
                    ? zone.objectId
                    : !string.IsNullOrWhiteSpace(zone.kind)
                        ? zone.kind
                        : $"{kind}_{index:D2}";
                target.Add(
                    new BackgroundSemanticZone(
                        id,
                        kind,
                        rect,
                        clearance: 0f,
                        enabled: true,
                        confidence:
                            new BackgroundSemanticConfidence(
                                1f,
                                "approved-semantic-review",
                                manuallyVerified: true)));
            }
        }

        private static BackgroundSemanticPolygon ToPolygon(
            RuntimePolygon source)
        {
            if (source?.points != null &&
                source.points.Length >= 3)
            {
                return new BackgroundSemanticPolygon(
                    source.points.Select(point =>
                        new Vector2(point.x, point.y)));
            }

            Rect rect = ToRect(source?.rect, null);
            return new BackgroundSemanticPolygon(new[]
            {
                new Vector2(rect.xMin, rect.yMin),
                new Vector2(rect.xMax, rect.yMin),
                new Vector2(rect.xMax, rect.yMax),
                new Vector2(rect.xMin, rect.yMax)
            });
        }

        private static Rect ToRect(
            RuntimeRect source,
            RuntimePoint[] points)
        {
            if (source != null)
            {
                return new Rect(
                    source.x,
                    source.y,
                    source.width,
                    source.height);
            }

            if (points == null || points.Length == 0)
                return default;
            float minX = points.Min(point => point.x);
            float minY = points.Min(point => point.y);
            float maxX = points.Max(point => point.x);
            float maxY = points.Max(point => point.y);
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        private static bool HasArea(RuntimeRect value) =>
            value != null &&
            value.width > 0f &&
            value.height > 0f;

        private static AnimationCurve BuildDepthCurve(
            RuntimeProfile profile)
        {
            RuntimeSlot[] slots = (profile.candidateSlots ??
                                   Array.Empty<RuntimeSlot>())
                .Where(slot => slot != null)
                .OrderBy(slot => slot.depth01)
                .ToArray();
            if (slots.Length < 2)
            {
                return AnimationCurve.Linear(
                    0f,
                    slots.FirstOrDefault()?.normalizedHeight ?? .42f,
                    1f,
                    slots.FirstOrDefault()?.normalizedHeight ?? .62f);
            }

            var keys = slots
                .GroupBy(slot =>
                    Mathf.Round(slot.depth01 * 1000f) / 1000f)
                .Select(group => new Keyframe(
                    group.Key,
                    group.Average(slot =>
                        slot.normalizedHeight)))
                .OrderBy(key => key.time)
                .ToArray();
            return new AnimationCurve(keys);
        }

        private static BackgroundSemanticCharacterRole ToRole(
            RuntimeAssignment assignment)
        {
            if (assignment.focus)
                return BackgroundSemanticCharacterRole.Focus;
            return string.Equals(
                    assignment.role,
                    "ContextNpc",
                    StringComparison.OrdinalIgnoreCase)
                ? BackgroundSemanticCharacterRole.Context
                : BackgroundSemanticCharacterRole.Main;
        }

        private static string ResolveSceneVariant(
            RuntimeSceneLayout scene,
            RuntimeProfile profile)
        {
            if (!string.IsNullOrWhiteSpace(scene.variantKey))
                return scene.variantKey.Trim();
            return NormalizeValues(
                    profile.variantKeys,
                    uppercase: false)
                .FirstOrDefault() ?? string.Empty;
        }

        private static bool TryParseHtmlColor(
            string value,
            out Color color)
        {
            if (ColorUtility.TryParseHtmlString(
                    value ?? string.Empty,
                    out color))
            {
                return true;
            }

            color = Color.white;
            return false;
        }

        private static void ValidateSha256(
            string value,
            string label,
            ICollection<string> errors)
        {
            if (value == null ||
                value.Length != 64 ||
                value.Any(character =>
                    !Uri.IsHexDigit(character)))
            {
                errors.Add(
                    $"{InvalidManifestCode}: {label} is not SHA-256.");
            }
        }

        private static void EnsureAssetFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;
            string parent = Path.GetDirectoryName(folder)
                ?.Replace('\\', '/');
            string name = Path.GetFileName(folder);
            if (string.IsNullOrEmpty(parent) ||
                string.IsNullOrEmpty(name))
            {
                throw new BuildFailedException(
                    $"Invalid asset folder: {folder}");
            }

            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static string ToFullPath(string path)
        {
            if (Path.IsPathRooted(path))
                return Path.GetFullPath(path);
            return Path.GetFullPath(
                Path.Combine(ProjectRoot, path));
        }

        private static bool TryResolveProjectPath(
            string path,
            out string fullPath)
        {
            fullPath = ToFullPath(path);
            string prefix =
                ProjectRoot.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            return fullPath.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase);
        }

        private static string ProjectRoot =>
            Directory.GetParent(Application.dataPath)?.FullName ??
            Directory.GetCurrentDirectory();

        private static string ToLowerHex(byte[] bytes) =>
            BitConverter.ToString(bytes)
                .Replace("-", string.Empty)
                .ToLowerInvariant();

        private static string NormalizeCode(string value) =>
            value?.Trim().ToUpperInvariant() ?? string.Empty;

        private static string[] NormalizeValues(
            IEnumerable<string> values,
            bool uppercase = true) =>
            (values ?? Array.Empty<string>())
            .Select(value =>
                uppercase
                    ? NormalizeCode(value)
                    : value?.Trim() ?? string.Empty)
            .Where(value => !string.IsNullOrEmpty(value))
            .Distinct(
                uppercase
                    ? StringComparer.OrdinalIgnoreCase
                    : StringComparer.Ordinal)
            .ToArray();

        private static HashSet<string> NormalizeSet(
            IEnumerable<string> values) =>
            new(
                NormalizeValues(values),
                StringComparer.OrdinalIgnoreCase);

        private static IReadOnlyList<string> Deduplicate(
            IEnumerable<string> errors) =>
            (errors ?? Array.Empty<string>())
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        private static string BindingKey(
            string location,
            string variant) =>
            $"{NormalizeCode(location)}|" +
            $"{variant?.Trim() ?? string.Empty}";

        private static string SceneLayoutKey(
            ApprovedBackgroundSemanticSceneLayout layout) =>
            layout == null
                ? string.Empty
                : $"{layout.SceneId}|" +
                  $"{layout.LocationCode}|" +
                  $"{layout.VariantKey}";

        private static int StableSeed(string value)
        {
            unchecked
            {
                int hash = 17;
                foreach (char character in value ?? string.Empty)
                    hash = hash * 31 + character;
                return hash == int.MinValue
                    ? int.MaxValue
                    : Mathf.Abs(hash);
            }
        }

        private static bool IsNormalized(float value) =>
            !float.IsNaN(value) &&
            !float.IsInfinity(value) &&
            value >= 0f &&
            value <= 1f;

        private static bool Approximately(
            float first,
            float second) =>
            Mathf.Abs(first - second) <= FloatTolerance;

        private static bool Approximately(
            Vector2 first,
            Vector2 second) =>
            Approximately(first.x, second.x) &&
            Approximately(first.y, second.y);

        private static bool Approximately(
            Color first,
            Color second) =>
            Approximately(first.r, second.r) &&
            Approximately(first.g, second.g) &&
            Approximately(first.b, second.b) &&
            Approximately(first.a, second.a);

        private static bool Approximately(
            Rect first,
            Rect second) =>
            Approximately(first.x, second.x) &&
            Approximately(first.y, second.y) &&
            Approximately(first.width, second.width) &&
            Approximately(first.height, second.height);

#pragma warning disable CS0649 // Populated by Unity JsonUtility.
        [Serializable]
        private sealed class RuntimeManifest
        {
            public string schemaVersion;
            public bool runtimeConnected;
            public string approvalStatus;
            public string reviewer;
            public int revision;
            public string approvedAtUtc;
            public bool approvedWarnings;
            public int approvedWarningCount;
            public string sourceInventoryGeneratedAtUtc;
            public string[] excludedUnusedLocationCodes;
            public RuntimeProfile[] profiles;
            public RuntimeSceneLayout[] sceneLayouts;
        }

        [Serializable]
        private sealed class RuntimeProfile
        {
            public string profileId;
            public string assetPath;
            public string sourceSha256;
            public string[] locationCodes;
            public string[] variantKeys;
            public RuntimePolygon[] walkablePolygons;
            public RuntimeZone[] forbiddenZones;
            public RuntimeZone[] uncertainZones;
            public RuntimeZone[] protectedZones;
            public RuntimePerspective perspective;
            public RuntimeLighting lighting;
            public RuntimeSlot[] candidateSlots;
            public string semanticContentHash;
        }

        [Serializable]
        private sealed class RuntimePolygon
        {
            public RuntimeRect rect;
            public RuntimePoint[] points;
        }

        [Serializable]
        private sealed class RuntimeZone
        {
            public string kind;
            public string objectId;
            public RuntimeRect rect;
            public RuntimeRect normalizedRect;
            public RuntimePoint[] points;
        }

        [Serializable]
        private sealed class RuntimePerspective
        {
            public float horizonY;
            public RuntimePoint vanishingPoint;
            public float confidence;
        }

        [Serializable]
        private sealed class RuntimeLighting
        {
            public RuntimePoint direction;
            public float temperatureKelvin;
            public string note;
        }

        [Serializable]
        private sealed class RuntimeSlot
        {
            public string id;
            public RuntimePoint foot;
            public float depth01;
            public float normalizedHeight;
            public float confidence;
            public RuntimeGrade grade;
        }

        [Serializable]
        private sealed class RuntimeGrade
        {
            public string tintHex;
            public float[] sampledRgb;
            public float saturation;
            public float exposure;
            public float contrast;
            public float softness;
        }

        [Serializable]
        private sealed class RuntimeSceneLayout
        {
            public string sceneId;
            public string locationCode;
            public string backgroundProfileId;
            public string variantKey;
            public RuntimePoint coverFocus;
            public float coverZoom;
            public string castFingerprint;
            public RuntimeAssignment[] assignments;
            public string[] offCameraCharacters;
        }

        [Serializable]
        private sealed class RuntimeAssignment
        {
            public string characterId;
            public string role;
            public bool focus;
            public string state;
            public string slotId;
            public bool offCamera;
            public bool hardProtectionOverlap;
        }

        [Serializable]
        private sealed class RuntimePoint
        {
            public float x;
            public float y;
        }

        [Serializable]
        private sealed class RuntimeRect
        {
            public float x;
            public float y;
            public float width;
            public float height;
        }

        [Serializable]
        private sealed class ScreenshotBaselineManifest
        {
            public string schemaVersion;
            public bool runtimeConnected;
            public string approvalStatus;
            public string reviewer;
            public int revision;
            public string approvedAtUtc;
            public ScreenshotBaseline[] scenes;
        }

        [Serializable]
        private sealed class ScreenshotBaseline
        {
            public string sceneId;
            public string path;
            public string sha256;
        }
#pragma warning restore CS0649

        [Serializable]
        private sealed class BakeReceipt
        {
            public int schemaVersion;
            public string runtimeManifestSha256;
            public string screenshotBaselineManifestSha256;
            public string reviewer;
            public int revision;
            public string approvedAtUtc;
            public ReceiptEntry[] profileSemantics;
            public ReceiptEntry[] sceneCasts;
            public int screenshotCount;
        }

        [Serializable]
        private sealed class ReceiptEntry
        {
            public string id;
            public string value;
        }
    }

    public sealed class ApprovedBackgroundSemanticBuildGate :
        IPreprocessBuildWithReport
    {
        public int callbackOrder => 2;

        public void OnPreprocessBuild(BuildReport report)
        {
            ApprovedBackgroundSemanticCatalogBaker.ThrowIfInvalid(
                ApprovedBackgroundSemanticCatalogBaker
                    .ValidateProject());
        }
    }
}
