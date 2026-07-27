using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Wake.Editor
{
    public static class AssetMetaIntegrityValidator
    {
        public const string AssetsRoot = "Assets";
        public const string DevelopmentPlanPath =
            "Assets/Docs/Under_the_Horizon_Unity_3인_개발계획.md";
        public const string DevelopmentPlanMetaPath =
            DevelopmentPlanPath + ".meta";
        public const string DevelopmentPlanGuid =
            "5709a28604936344699505ec0200ec23";

        private static readonly Regex GuidPattern = new(
            @"^guid:\s*(?<guid>[0-9a-f]{32})\s*$",
            RegexOptions.Compiled |
            RegexOptions.IgnoreCase |
            RegexOptions.Multiline);

        public static IReadOnlyList<ProductionPreflightDiagnostic> Validate()
        {
            var diagnostics = new List<ProductionPreflightDiagnostic>();
            ValidateSidecars(diagnostics);
            ValidateGuids(diagnostics);
            ValidateDevelopmentPlan(diagnostics);
            return diagnostics;
        }

        private static void ValidateSidecars(
            ICollection<ProductionPreflightDiagnostic> diagnostics)
        {
            foreach (string path in Directory
                         .EnumerateFileSystemEntries(
                             AssetsRoot, "*", SearchOption.AllDirectories)
                         .Select(Normalize)
                         .OrderBy(path => path, StringComparer.Ordinal))
            {
                if (path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    string assetPath = path[..^".meta".Length];
                    if (!File.Exists(assetPath) &&
                        !Directory.Exists(assetPath))
                    {
                        Error(
                            diagnostics,
                            "ASSET_META_ORPHAN",
                            path,
                            "대응하는 에셋이나 폴더가 없는 메타 파일입니다.");
                    }

                    continue;
                }

                string metaPath = path + ".meta";
                if (!File.Exists(metaPath))
                {
                    Error(
                        diagnostics,
                        "ASSET_META_MISSING",
                        path,
                        "에셋 또는 폴더에 대응하는 메타 파일이 없습니다.");
                }
            }
        }

        private static void ValidateGuids(
            ICollection<ProductionPreflightDiagnostic> diagnostics)
        {
            var pathsByGuid =
                new Dictionary<string, List<string>>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (string path in Directory
                         .EnumerateFiles(
                             AssetsRoot, "*.meta", SearchOption.AllDirectories)
                         .Select(Normalize)
                         .OrderBy(path => path, StringComparer.Ordinal))
            {
                Match match = GuidPattern.Match(File.ReadAllText(path));
                if (!match.Success)
                {
                    Error(
                        diagnostics,
                        "ASSET_META_GUID_MISSING",
                        path,
                        "메타 파일에 유효한 32자리 GUID가 없습니다.");
                    continue;
                }

                string guid = match.Groups["guid"].Value;
                if (!pathsByGuid.TryGetValue(guid, out List<string> paths))
                {
                    paths = new List<string>();
                    pathsByGuid.Add(guid, paths);
                }

                paths.Add(path);
            }

            foreach ((string guid, List<string> paths) in pathsByGuid
                         .Where(pair => pair.Value.Count > 1)
                         .OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                Error(
                    diagnostics,
                    "ASSET_META_GUID_DUPLICATE",
                    string.Join(", ", paths),
                    $"여러 메타 파일이 동일한 GUID를 사용합니다: {guid}");
            }
        }

        private static void ValidateDevelopmentPlan(
            ICollection<ProductionPreflightDiagnostic> diagnostics)
        {
            if (!File.Exists(DevelopmentPlanPath) ||
                !File.Exists(DevelopmentPlanMetaPath))
            {
                Error(
                    diagnostics,
                    "DEVELOPMENT_PLAN_META",
                    DevelopmentPlanPath,
                    "개발 계획 문서와 같은 이름의 메타 파일이 필요합니다.");
                return;
            }

            Match match = GuidPattern.Match(
                File.ReadAllText(DevelopmentPlanMetaPath));
            if (!match.Success ||
                !string.Equals(
                    match.Groups["guid"].Value,
                    DevelopmentPlanGuid,
                    StringComparison.OrdinalIgnoreCase))
            {
                Error(
                    diagnostics,
                    "DEVELOPMENT_PLAN_META",
                    DevelopmentPlanMetaPath,
                    $"개발 계획 문서 GUID는 {DevelopmentPlanGuid}여야 합니다.");
            }
        }

        private static string Normalize(string path) =>
            path.Replace('\\', '/');

        private static void Error(
            ICollection<ProductionPreflightDiagnostic> diagnostics,
            string code,
            string path,
            string message) =>
            diagnostics.Add(new ProductionPreflightDiagnostic(
                ProductionPreflightSeverity.Error,
                code,
                path,
                message));
    }
}
