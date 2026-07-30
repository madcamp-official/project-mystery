using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using Wake.Evidence;
using Wake.Exploration;

namespace Wake.EditorTools
{
    /// <summary>
    /// Exports a deterministic, editor-only inventory for semantic staging
    /// analysis. Nothing produced here is consumed by the game at runtime.
    /// </summary>
    public static class BackgroundSemanticAnalysisExporter
    {
        private const string ReviewRoot =
            "Documentation/CharacterStagingReview";
        private const string VariantFolder =
            "Assets/_Project/Resources/LocationBackgroundVariants";
        private const string LocationFolder =
            "Assets/_Project/Content/Locations";

        [MenuItem("Wake/Analysis/Export Background Semantic Inventory")]
        public static void Export()
        {
            string repositoryRoot =
                Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException(
                    "Could not resolve the repository root.");
            string dataDirectory =
                Path.Combine(repositoryRoot, ReviewRoot, "Data");
            Directory.CreateDirectory(dataDirectory);

            List<SemanticBackgroundRecord> backgrounds =
                CollectBackgrounds();
            List<SemanticProtectionRecord> protections =
                CollectProtections();
            List<SemanticSceneRecord> scenes =
                CollectScenes(backgrounds);

            var manifest = new BackgroundSemanticAnalysisManifest
            {
                schemaVersion = "1.0",
                generatedAtUtc =
                    DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
                playableLocationCodes = CanonicalLocationCatalog.Playable
                    .Select(item => item.Code)
                    .OrderBy(item => item, StringComparer.Ordinal)
                    .ToArray(),
                excludedUnusedLocationCodes = CanonicalLocationCatalog.Unused
                    .Select(item => item.Code)
                    .OrderBy(item => item, StringComparer.Ordinal)
                    .ToArray(),
                backgrounds = backgrounds.ToArray(),
                protections = protections.ToArray(),
                scenes = scenes.ToArray()
            };

            string outputPath =
                Path.Combine(
                    dataDirectory,
                    "background_analysis_inventory.json");
            File.WriteAllText(
                outputPath,
                JsonUtility.ToJson(manifest, true),
                new UTF8Encoding(false));

            Debug.Log(
                "Background semantic inventory exported: " +
                $"{backgrounds.Count} backgrounds, " +
                $"{protections.Count} protected regions, " +
                $"{scenes.Count} scene reviews -> {outputPath}");
        }

        private static List<SemanticBackgroundRecord> CollectBackgrounds()
        {
            var byPath = new Dictionary<string, SemanticBackgroundRecord>(
                StringComparer.OrdinalIgnoreCase);

            foreach (LocationBackgroundVariantBinding binding in
                     LocationBackgroundVariantCatalog.All)
            {
                string assetPath =
                    ResolveVariantAssetPath(binding.ResourceName);
                if (string.IsNullOrEmpty(assetPath))
                {
                    Debug.LogWarning(
                        "Semantic analysis skipped a missing approved " +
                        $"background: {binding.ResourceKey}");
                    continue;
                }

                SemanticBackgroundRecord record =
                    GetOrCreateBackground(
                        byPath,
                        assetPath,
                        "ApprovedVariant");
                AddDistinct(record.locationCodes, binding.LogicalLocationCode);
                AddDistinct(record.variantKeys, binding.ResourceKey);
                foreach (string sceneId in binding.SceneIds)
                    AddDistinct(record.sceneIds, sceneId);
            }

            string[] locationGuids = AssetDatabase.FindAssets(
                "t:LocationDefinition",
                new[] { LocationFolder });
            foreach (string guid in locationGuids)
            {
                string definitionPath =
                    AssetDatabase.GUIDToAssetPath(guid);
                LocationDefinition location =
                    AssetDatabase.LoadAssetAtPath<LocationDefinition>(
                        definitionPath);
                if (location == null ||
                    !CanonicalLocationCatalog.IsPlayable(
                        location.LocationCode) ||
                    location.BackgroundSprite == null)
                {
                    continue;
                }

                string assetPath =
                    AssetDatabase.GetAssetPath(location.BackgroundSprite);
                SemanticBackgroundRecord record =
                    GetOrCreateBackground(
                        byPath,
                        assetPath,
                        "LegacyBase");
                AddDistinct(record.locationCodes, location.LocationCode);
                AddDistinct(
                    record.variantKeys,
                    $"serialized:{Path.GetFileNameWithoutExtension(assetPath)}");
            }

            return byPath.Values
                .OrderBy(
                    item => item.sourceKind == "ApprovedVariant" ? 0 : 1)
                .ThenBy(item => item.assetPath, StringComparer.Ordinal)
                .Select(FinalizeRecord)
                .ToList();
        }

        private static SemanticBackgroundRecord GetOrCreateBackground(
            IDictionary<string, SemanticBackgroundRecord> byPath,
            string assetPath,
            string sourceKind)
        {
            if (byPath.TryGetValue(
                    assetPath,
                    out SemanticBackgroundRecord existing))
            {
                if (existing.sourceKind != sourceKind)
                    existing.sourceKind = "ApprovedVariantAndLegacyBase";
                return existing;
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            string fullPath = ToFullPath(assetPath);
            var record = new SemanticBackgroundRecord
            {
                profileId =
                    Path.GetFileNameWithoutExtension(assetPath),
                sourceKind = sourceKind,
                assetPath = assetPath.Replace('\\', '/'),
                sourceSha256 = ComputeSha256(fullPath),
                width = sprite != null
                    ? Mathf.RoundToInt(sprite.rect.width)
                    : 0,
                height = sprite != null
                    ? Mathf.RoundToInt(sprite.rect.height)
                    : 0,
                locationCodes = new List<string>(),
                variantKeys = new List<string>(),
                sceneIds = new List<string>()
            };
            byPath.Add(assetPath, record);
            return record;
        }

        private static SemanticBackgroundRecord FinalizeRecord(
            SemanticBackgroundRecord record)
        {
            record.locationCodes.Sort(StringComparer.Ordinal);
            record.variantKeys.Sort(StringComparer.Ordinal);
            record.sceneIds.Sort(StringComparer.Ordinal);
            return record;
        }

        private static List<SemanticProtectionRecord> CollectProtections()
        {
            var result = new List<SemanticProtectionRecord>();

            foreach (EvidenceLocationHotspotSpec evidence in
                     EvidenceLocationHotspotCatalog.All)
            {
                string canonicalLocation =
                    CanonicalLocationCatalog.FindSpec(evidence.LocationCode)
                        ?.Code ?? evidence.LocationCode;
                if (!CanonicalLocationCatalog.IsPlayable(canonicalLocation))
                    continue;

                EvidenceMasterCatalog.TryGet(
                    evidence.EvidenceId,
                    out EvidenceMasterRecord master);
                BackgroundInteractionShape[] authoredShapes =
                    BackgroundInteractionShapeCatalog.All
                        .Where(shape =>
                            shape.ObjectId == evidence.EvidenceId &&
                            shape.LocationCode == canonicalLocation)
                        .ToArray();
                if (authoredShapes.Length > 0)
                {
                    foreach (BackgroundInteractionShape shape in
                             authoredShapes)
                    {
                        result.Add(CreateEvidenceProtection(
                            evidence,
                            master,
                            canonicalLocation,
                            shape));
                    }
                    continue;
                }

                result.Add(CreateEvidenceProtection(
                    evidence,
                    master,
                    canonicalLocation,
                    null));
            }

            foreach (AmbientInspectableSpec inspectable in
                     AmbientInspectableCatalog.All)
            {
                string canonicalLocation =
                    CanonicalLocationCatalog.FindSpec(inspectable.Location)
                        ?.Code ?? inspectable.Location;
                if (!CanonicalLocationCatalog.IsPlayable(canonicalLocation))
                    continue;

                BackgroundInteractionShape[] authoredShapes =
                    BackgroundInteractionShapeCatalog.All
                        .Where(shape =>
                            shape.ObjectId == inspectable.Id &&
                            shape.LocationCode == canonicalLocation)
                        .ToArray();
                if (authoredShapes.Length > 0)
                {
                    foreach (BackgroundInteractionShape shape in
                             authoredShapes)
                    {
                        result.Add(CreateInspectableProtection(
                            inspectable,
                            canonicalLocation,
                            shape));
                    }
                    continue;
                }

                result.Add(CreateInspectableProtection(
                    inspectable,
                    canonicalLocation,
                    null));
            }

            return result
                .OrderBy(item => item.locationCode, StringComparer.Ordinal)
                .ThenBy(item => item.objectId, StringComparer.Ordinal)
                .ThenBy(item => item.variantKey, StringComparer.Ordinal)
                .ToList();
        }

        private static SemanticProtectionRecord CreateEvidenceProtection(
            EvidenceLocationHotspotSpec evidence,
            EvidenceMasterRecord master,
            string canonicalLocation,
            BackgroundInteractionShape shape)
        {
            Rect bounds = shape?.NormalizedBounds ??
                          evidence.NormalizedRect;
            return new SemanticProtectionRecord
            {
                locationCode = canonicalLocation,
                objectId = evidence.EvidenceId,
                kind = "Evidence",
                priority = "Hard",
                normalizedRect = SemanticRect.From(bounds),
                points = shape?.NormalizedPolygon
                    .Select(SemanticPoint.From)
                    .ToArray() ?? Array.Empty<SemanticPoint>(),
                variantKey = shape?.BackgroundVariantKey ?? string.Empty,
                isPresent = shape?.IsPresent ?? true,
                availableFromScene = evidence.AvailableFromScene,
                requiredEnding = evidence.RequiredEnding,
                argumentRole = master?.ArgumentRole ?? string.Empty,
                coverage = master?.Coverage ?? string.Empty,
                sourceScenes = master?.SourceScenes?.ToArray() ??
                               Array.Empty<string>()
            };
        }

        private static SemanticProtectionRecord CreateInspectableProtection(
            AmbientInspectableSpec inspectable,
            string canonicalLocation,
            BackgroundInteractionShape shape)
        {
            Rect bounds = shape?.NormalizedBounds ?? inspectable.Hotspot;
            return new SemanticProtectionRecord
            {
                locationCode = canonicalLocation,
                objectId = inspectable.Id,
                kind = "Inspectable",
                priority = "Soft",
                normalizedRect = SemanticRect.From(bounds),
                points = shape?.NormalizedPolygon
                    .Select(SemanticPoint.From)
                    .ToArray() ?? Array.Empty<SemanticPoint>(),
                variantKey = shape?.BackgroundVariantKey ?? string.Empty,
                isPresent = shape?.IsPresent ?? true,
                displayName = inspectable.Title,
                description = inspectable.Description,
                sourceScenes = Array.Empty<string>()
            };
        }

        private static List<SemanticSceneRecord> CollectScenes(
            IReadOnlyList<SemanticBackgroundRecord> backgrounds)
        {
            var result = new List<SemanticSceneRecord>();
            Dictionary<string, LocationDefinition> locationsByCode =
                CollectPlayableLocationDefinitions();
            foreach (ScenePresenceRecord scene in ScenePresenceCatalog.All)
            {
                string canonicalLocation =
                    CanonicalLocationCatalog.FindSpec(scene.FocusLocation)
                        ?.Code ?? scene.FocusLocation;
                if (!CanonicalLocationCatalog.IsPlayable(canonicalLocation))
                    continue;

                string variantKey =
                    LocationBackgroundVariantCatalog.ResolveResourceKey(
                        canonicalLocation,
                        scene.SceneId);
                SemanticBackgroundRecord background =
                    !string.IsNullOrEmpty(variantKey)
                        ? backgrounds.FirstOrDefault(item =>
                            item.variantKeys.Contains(
                                variantKey,
                                StringComparer.Ordinal))
                        : null;
                background ??= backgrounds.FirstOrDefault(item =>
                    item.sourceKind.Contains(
                        "LegacyBase",
                        StringComparison.Ordinal) &&
                    item.locationCodes.Contains(
                        canonicalLocation,
                        StringComparer.Ordinal));

                IReadOnlyList<SceneWorldCharacter> visible =
                    ScenePresencePresentationPolicy.SelectVisible(
                        scene,
                        scene.FocusLocation,
                        visibleLimit: 4);
                var cast = new List<SemanticCastRecord>();
                if (!string.IsNullOrWhiteSpace(scene.ContextSpeaker))
                {
                    cast.Add(new SemanticCastRecord
                    {
                        characterId = scene.ContextSpeaker,
                        role = "ContextNpc",
                        focus = false
                    });
                }
                cast.AddRange(visible.Select(character =>
                    new SemanticCastRecord
                    {
                        characterId = character.CharacterId,
                        role = "Main",
                        focus = character.IsFocusParticipant,
                        state = character.State.ToString()
                    }));

                locationsByCode.TryGetValue(
                    canonicalLocation,
                    out LocationDefinition location);
                result.Add(new SemanticSceneRecord
                {
                    sceneId = scene.SceneId,
                    locationCode = canonicalLocation,
                    backgroundProfileId =
                        background?.profileId ?? string.Empty,
                    variantKey = variantKey,
                    coverFocus = SemanticPoint.From(
                        location != null
                            ? location.BackgroundFocus
                            : new Vector2(.5f, .5f)),
                    coverZoom =
                        location != null ? location.BackgroundZoom : 1f,
                    cast = cast.ToArray()
                });
            }

            return result;
        }

        private static Dictionary<string, LocationDefinition>
            CollectPlayableLocationDefinitions()
        {
            var result = new Dictionary<string, LocationDefinition>(
                StringComparer.OrdinalIgnoreCase);
            string[] locationGuids = AssetDatabase.FindAssets(
                "t:LocationDefinition",
                new[] { LocationFolder });
            foreach (string guid in locationGuids)
            {
                LocationDefinition location =
                    AssetDatabase.LoadAssetAtPath<LocationDefinition>(
                        AssetDatabase.GUIDToAssetPath(guid));
                if (location == null ||
                    !CanonicalLocationCatalog.IsPlayable(
                        location.LocationCode))
                {
                    continue;
                }

                result[location.LocationCode] = location;
            }

            return result;
        }

        private static string ResolveVariantAssetPath(string resourceName)
        {
            string[] guids = AssetDatabase.FindAssets(
                $"{resourceName} t:Sprite",
                new[] { VariantFolder });
            return guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(path =>
                    string.Equals(
                        Path.GetFileNameWithoutExtension(path),
                        resourceName,
                        StringComparison.Ordinal));
        }

        private static void AddDistinct(
            ICollection<string> values,
            string value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                values.Contains(value, StringComparer.Ordinal))
            {
                return;
            }
            values.Add(value);
        }

        private static string ToFullPath(string assetPath)
        {
            string repositoryRoot =
                Directory.GetParent(Application.dataPath)?.FullName ??
                string.Empty;
            return Path.GetFullPath(
                Path.Combine(repositoryRoot, assetPath));
        }

        private static string ComputeSha256(string path)
        {
            using SHA256 sha = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(stream))
                .Replace("-", string.Empty)
                .ToLowerInvariant();
        }

        [Serializable]
        private sealed class BackgroundSemanticAnalysisManifest
        {
            public string schemaVersion;
            public string generatedAtUtc;
            public string[] playableLocationCodes;
            public string[] excludedUnusedLocationCodes;
            public SemanticBackgroundRecord[] backgrounds;
            public SemanticProtectionRecord[] protections;
            public SemanticSceneRecord[] scenes;
        }

        [Serializable]
        private sealed class SemanticBackgroundRecord
        {
            public string profileId;
            public string sourceKind;
            public string assetPath;
            public string sourceSha256;
            public int width;
            public int height;
            public List<string> locationCodes;
            public List<string> variantKeys;
            public List<string> sceneIds;
        }

        [Serializable]
        private sealed class SemanticProtectionRecord
        {
            public string locationCode;
            public string objectId;
            public string kind;
            public string priority;
            public string displayName;
            public string description;
            public SemanticRect normalizedRect;
            public SemanticPoint[] points;
            public string variantKey;
            public bool isPresent = true;
            public string availableFromScene;
            public string requiredEnding;
            public string argumentRole;
            public string coverage;
            public string[] sourceScenes;
        }

        [Serializable]
        private sealed class SemanticSceneRecord
        {
            public string sceneId;
            public string locationCode;
            public string backgroundProfileId;
            public string variantKey;
            public SemanticPoint coverFocus;
            public float coverZoom;
            public SemanticCastRecord[] cast;
        }

        [Serializable]
        private sealed class SemanticCastRecord
        {
            public string characterId;
            public string role;
            public bool focus;
            public string state;
        }

        [Serializable]
        private struct SemanticRect
        {
            public float x;
            public float y;
            public float width;
            public float height;

            public static SemanticRect From(Rect rect) =>
                new()
                {
                    x = rect.x,
                    y = rect.y,
                    width = rect.width,
                    height = rect.height
                };
        }

        [Serializable]
        private struct SemanticPoint
        {
            public float x;
            public float y;

            public static SemanticPoint From(Vector2 point) =>
                new()
                {
                    x = point.x,
                    y = point.y
                };
        }
    }
}
