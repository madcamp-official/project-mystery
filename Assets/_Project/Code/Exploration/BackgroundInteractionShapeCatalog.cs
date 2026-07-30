using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
#endif

namespace Wake.Exploration
{
    public static partial class BackgroundInteractionShapeCatalog
    {
        private const string PortBaseHash =
            "cb763e64fc431558553c2eead94099bc0ab1053d5ac1b0e07958b95aafa5c80c";
        private const string PortEpilogueHash =
            "c22857a5da58ca1dbe361b370bd128abc9de1a9a855ff754e83047121804e266";
        private const string HorizonDiscoveryHash =
            "8327680e005064e2268a34cb4037662c06991df68e46c8f8c3da28b83e6c1516";
        private const string HorizonClearedHash =
            "9885039d169192a4c709682fe16d1ea934e8bb3c91f807cd99605b417c279d24";
        private const string HorizonFinaleHash =
            "d895f4252f4ca001ff92b56b2a229919147ce9a9076cdf4bfb99b6c91c99ea1f";
        private const string BallastAnnexHash =
            "81fdb18febd19922f0bb24dc68c28bf3b4523569c0254caf553682d46c5d1651";
        private const string SecurityHash =
            "081f96f54681e46f2743379b4b9d3ea0b138ebe328ecc074008b58a1a6af8624";
        private const string EngineControlHash =
            "f507597d0d777b5bf76bd780ca7bbabb5ee50506db9dc5bf80db8dc5b8f5e933";
        private const string ServiceRailHash =
            "9b008533c18305b11e3712f8bada7ea433620889f0659064d3528c4bb999d30f";
        private const string MedbayBaselineHash =
            "cd7907b6d751a347467bb285811fa1cfed3cbe3328f005149fa643878331d8cb";
        private const string MedbayForensicHash =
            "95fd5e454572262312f4ff50104001d4e6d1f7223370a7d10f5298e577b7d6d0";
        private const string MedbayDnaHash =
            "dcd511c2ea0dddd8ece8dd5819c7052199df7099171d9532d0e3a46f87edd0ba";
        private const string InterviewHash =
            "3f27edd6d3e1e400618175e8fb3a00e3997b7cdd8ee579dc830beb85ae2e7e9a";
        private const string PromenadeNightHash =
            "6ba30533ff3572faeb8d76891487179d3fe2b7d241b96470393ebb8857a036f2";
        private const string PromenadeSerializedHash =
            "78894e0f55be2e51cbe9258ae5e5ab1027bea3cb787c6b95c0f96ed57fbfb962";
        private const string ArchiveHash =
            "743dc3befc696c7731ae60fc8a83a49ad6083a6152766ee087f454f04cb6dc16";
        private const string AtriumChampagneHash =
            "244030aeb2d062120c00beca0d21902ac56b6be38964c5b1ac5904bcd998ee91";
        private const string VipLoungeHash =
            "b4a2f5f2692e1312cd68091cda1fdb40541221e0ab46fcf21e757acc9d617c46";
        private const string NewsLoungeD3Hash =
            "48258b7644dbf1623955556041e99e53179393b0d7849de3a42c31ddc996dde5";
        private const string NewsLoungeD6Hash =
            "38e9fc13386e9736fac0997027c21ff04da41257314ddc9a744124dec9f2fd03";
        private const string GangwayLuggageHash =
            "3300633db75beed1e24652a704b9f44fac3605637c11750d3d3cec4b82756781";
        private const string BallroomMaskHash =
            "4f829055e7ab3558057b9213a99dc55736af89802b508e3cc45acd0bb00d9dec";
        private const string OpenDeckSerializedHash =
            "8481e21e87db57303a85c5fca5fd5c9941a12ce2cf2baadf9feee3d707f435dd";
        private const string OpenDeckMorningHash =
            "2daf31211ebb0825aa9019c814da64a0df74f607d2a65f33b061880df376c422";

        private static readonly BackgroundInteractionShape[] Entries =
            BuildEntries();

        private static BackgroundInteractionShape[] BuildEntries()
        {
            var entries = new List<BackgroundInteractionShape>();
            PopulateEvidenceEntries(entries);
            PopulateInspectableEntries(entries);
            return entries.ToArray();
        }

        static partial void PopulateEvidenceEntries(
            List<BackgroundInteractionShape> entries);

        static partial void PopulateInspectableEntries(
            List<BackgroundInteractionShape> entries);

        public static IReadOnlyList<BackgroundInteractionShape> All =>
            Entries;

        public static bool TryGet(
            string objectId,
            string locationCode,
            string backgroundVariantKey,
            out BackgroundInteractionShape shape)
        {
            string normalizedObject =
                objectId?.Trim().ToUpperInvariant() ?? string.Empty;
            string normalizedLocation =
                locationCode?.Trim().ToUpperInvariant() ?? string.Empty;
            string normalizedVariant =
                BackgroundInteractionShape.NormalizeVariantKey(
                    backgroundVariantKey);

            shape = Entries.FirstOrDefault(entry =>
                entry.ObjectId == normalizedObject &&
                entry.LocationCode == normalizedLocation &&
                entry.BackgroundVariantKey == normalizedVariant);
            return shape != null;
        }

        public static bool TryGet(
            string objectId,
            string locationCode,
            LocationBackgroundSelection backgroundSelection,
            out BackgroundInteractionShape shape)
        {
            if (!TryGet(
                    objectId,
                    locationCode,
                    backgroundSelection.VariantKey,
                    out shape))
            {
                return false;
            }

#if UNITY_EDITOR
            if (!SourceImageMatches(shape, backgroundSelection.Sprite))
            {
                string warningKey =
                    $"{shape.BackgroundVariantKey}|{shape.SourceImageHash}";
                if (WarnedSourceMismatches.Add(warningKey))
                {
                    Debug.LogWarning(
                        "Interaction shape disabled because its source " +
                        $"background changed: {shape.BackgroundVariantKey}");
                }
                shape = null;
                return false;
            }
#endif
            return true;
        }

        public static bool HasAuthoredObject(
            string objectId,
            string locationCode)
        {
            string normalizedObject =
                objectId?.Trim().ToUpperInvariant() ?? string.Empty;
            string normalizedLocation =
                locationCode?.Trim().ToUpperInvariant() ?? string.Empty;
            return Entries.Any(entry =>
                entry.ObjectId == normalizedObject &&
                entry.LocationCode == normalizedLocation);
        }

        public static bool Validate(out IReadOnlyList<string> diagnostics)
        {
            var errors = new List<string>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (BackgroundInteractionShape entry in Entries)
            {
                string key =
                    $"{entry.ObjectId}|{entry.LocationCode}|" +
                    entry.BackgroundVariantKey;
                if (!keys.Add(key))
                    errors.Add($"{key}: duplicate interaction shape.");
                if (!entry.Validate(out string diagnostic))
                    errors.Add($"{key}: {diagnostic}");
            }

            diagnostics = errors;
            return errors.Count == 0;
        }

#if UNITY_EDITOR
        private sealed class SourceHashCacheEntry
        {
            public long Length;
            public long LastWriteTicks;
            public string Hash = string.Empty;
        }

        private static readonly Dictionary<string, SourceHashCacheEntry>
            SourceHashCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> WarnedSourceMismatches =
            new(StringComparer.Ordinal);

        internal static bool SourceImageMatches(
            BackgroundInteractionShape shape,
            Sprite sprite)
        {
            if (shape == null || sprite == null)
                return true;

            string assetPath = AssetDatabase.GetAssetPath(sprite);
            if (string.IsNullOrWhiteSpace(assetPath))
                return true;

            string repositoryRoot =
                Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(repositoryRoot))
                return false;

            string fullPath = Path.GetFullPath(
                Path.Combine(repositoryRoot, assetPath));
            if (!File.Exists(fullPath))
                return false;

            var file = new FileInfo(fullPath);
            if (!SourceHashCache.TryGetValue(
                    fullPath,
                    out SourceHashCacheEntry cached) ||
                cached.Length != file.Length ||
                cached.LastWriteTicks != file.LastWriteTimeUtc.Ticks)
            {
                using SHA256 sha = SHA256.Create();
                using FileStream stream = File.OpenRead(fullPath);
                cached = new SourceHashCacheEntry
                {
                    Length = file.Length,
                    LastWriteTicks = file.LastWriteTimeUtc.Ticks,
                    Hash = BitConverter.ToString(sha.ComputeHash(stream))
                        .Replace("-", string.Empty)
                        .ToLowerInvariant()
                };
                SourceHashCache[fullPath] = cached;
            }

            return string.Equals(
                cached.Hash,
                shape.SourceImageHash,
                StringComparison.Ordinal);
        }
#endif

        private static BackgroundInteractionShape Visible(
            string objectId,
            string locationCode,
            string variantKey,
            string sourceHash,
            params Vector2[] polygon)
        {
            Rect bounds = BoundsFor(polygon, .006f);
            Vector2 labelAnchor = new(
                bounds.center.x,
                Mathf.Clamp01(bounds.yMin - .012f));
            return new BackgroundInteractionShape(
                objectId,
                locationCode,
                variantKey,
                sourceHash,
                true,
                bounds,
                polygon,
                labelAnchor);
        }

        private static BackgroundInteractionShape Hidden(
            string objectId,
            string locationCode,
            string variantKey,
            string sourceHash) =>
            new(
                objectId,
                locationCode,
                variantKey,
                sourceHash,
                false,
                default,
                Array.Empty<Vector2>(),
                new Vector2(.5f, .5f));

        private static Rect BoundsFor(
            IReadOnlyList<Vector2> polygon,
            float padding)
        {
            float minX = polygon.Min(point => point.x);
            float maxX = polygon.Max(point => point.x);
            float minY = polygon.Min(point => point.y);
            float maxY = polygon.Max(point => point.y);
            minX = Mathf.Clamp01(minX - padding);
            maxX = Mathf.Clamp01(maxX + padding);
            minY = Mathf.Clamp01(minY - padding);
            maxY = Mathf.Clamp01(maxY + padding);
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }
    }
}
