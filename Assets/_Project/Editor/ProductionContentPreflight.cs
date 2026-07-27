using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Wake.Evidence;
using Wake.Exploration;
using Wake.Narrative;
using Wake.Puzzles;

namespace Wake.Editor
{
    public enum ProductionPreflightSeverity { Warning, Error }

    public sealed class ProductionPreflightDiagnostic
    {
        public ProductionPreflightDiagnostic(
            ProductionPreflightSeverity severity, string code,
            string path, string message)
        {
            Severity = severity;
            Code = code;
            Path = path;
            Message = message;
        }

        public ProductionPreflightSeverity Severity { get; }
        public string Code { get; }
        public string Path { get; }
        public string Message { get; }
        public override string ToString() =>
            $"{Severity} {Code} [{Path}]: {Message}";
    }

    public sealed class ProductionPreflightReport
    {
        public ProductionPreflightReport(
            IEnumerable<ProductionPreflightDiagnostic> diagnostics)
        {
            Diagnostics = (diagnostics ??
                Array.Empty<ProductionPreflightDiagnostic>())
                .GroupBy(item =>
                    (item.Severity, item.Code, item.Path, item.Message))
                .Select(group => group.First()).ToArray();
        }

        public IReadOnlyList<ProductionPreflightDiagnostic> Diagnostics { get; }
        public int ErrorCount => Diagnostics.Count(item =>
            item.Severity == ProductionPreflightSeverity.Error);
        public int WarningCount => Diagnostics.Count(item =>
            item.Severity == ProductionPreflightSeverity.Warning);
        public bool CanBuild => ErrorCount == 0;
    }

    public static class ProductionContentPreflight
    {
        public const string ScenePath =
            "Assets/_Project/Scenes/UI/UI Basic Scene.unity";
        public const string CsvPath = "Assets/_Project/Content/Dialogue/" +
            "Under_the_Horizon_Dialogue_KR.csv";
        public const string ChoicesPath = "Assets/_Project/Content/Dialogue/" +
            "Under_the_Horizon_Choices_KR.csv";
        public const string SceneIndexPath = "Assets/_Project/Content/Dialogue/" +
            "Under_the_Horizon_Scene_Index_KR.csv";
        private const string DialogueScriptPath =
            "Assets/_Project/Code/Narrative/DialogueDatabase.cs";
        private const string LocationFolder =
            "Assets/_Project/Content/Locations";
        private const string EvidenceFolder =
            "Assets/_Project/Content/Evidence";
        private const string PortraitFolder =
            "Assets/_Project/Resources/CharacterExpressions";
        private static readonly Regex GuidPattern = new(
            @"guid:\s*(?<guid>[0-9a-f]{32})",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex MissingScriptPattern = new(
            @"m_Script:\s*\{fileID:\s*0\}", RegexOptions.Compiled);
        private static readonly Regex ScriptPattern = new(
            @"m_Script:\s*\{fileID:\s*-?\d+,\s*guid:\s*" +
            @"(?<guid>[0-9a-f]{32}),\s*type:\s*3\}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static ProductionPreflightReport Run()
        {
            var items = new List<ProductionPreflightDiagnostic>();
            DialogueCsvParseResult dialogue = ValidateDialogue(items);
            ValidateSceneBinding(items);
            ValidateLocations(items);
            ValidateEvidence(items);
            ValidatePortraits(items);
            ValidateSerializedReferences(items);
            ValidateTextEncoding(items);
            AddWarnings(dialogue, items);
            return new ProductionPreflightReport(items);
        }

        public static void ThrowIfErrors(ProductionPreflightReport report)
        {
            if (report == null || report.CanBuild)
                return;
            string details = string.Join("\n", report.Diagnostics.Where(
                item => item.Severity == ProductionPreflightSeverity.Error));
            throw new BuildFailedException(
                $"프로덕션 콘텐츠 사전 검증 실패 ({report.ErrorCount}건)\n{details}");
        }

        [MenuItem("Wake/Production/Run Content Preflight")]
        public static void RunFromMenu()
        {
            ProductionPreflightReport report = Run();
            foreach (ProductionPreflightDiagnostic item in report.Diagnostics)
            {
                if (item.Severity == ProductionPreflightSeverity.Error)
                    Debug.LogError(item);
                else
                    Debug.LogWarning(item);
            }
            Debug.Log($"Production preflight: {report.ErrorCount} errors, " +
                      $"{report.WarningCount} warnings.");
        }

        private static DialogueCsvParseResult ValidateDialogue(
            ICollection<ProductionPreflightDiagnostic> items)
        {
            TextAsset csv = AssetDatabase.LoadAssetAtPath<TextAsset>(CsvPath);
            if (csv == null)
            {
                Error(items, "DIALOGUE_SHAPE", CsvPath,
                    "프로덕션 CSV를 찾을 수 없습니다.");
                return DialogueCsvParser.Parse(string.Empty);
            }
            DialogueCsvParseResult parsed = DialogueCsvParser.Parse(csv.text);
            int scenes = parsed.Records.Select(record => record.SceneId)
                .Distinct(StringComparer.Ordinal).Count();
            if (parsed.Records.Count !=
                    OfficialDialogueContractValidator.ExpectedDialogueCount ||
                scenes != OfficialDialogueContractValidator.ExpectedSceneCount)
                Error(items, "DIALOGUE_SHAPE", CsvPath,
                    $"1,063행/41개 장면이 필요합니다. " +
                    $"{parsed.Records.Count}행/{scenes}개 장면");
            TextAsset choices =
                AssetDatabase.LoadAssetAtPath<TextAsset>(ChoicesPath);
            TextAsset sceneIndex =
                AssetDatabase.LoadAssetAtPath<TextAsset>(SceneIndexPath);
            if (choices == null || sceneIndex == null)
            {
                Error(items, "DIALOGUE_SHAPE", CsvPath,
                    "Choice_Flow 또는 Scene_Index CSV가 없습니다.");
            }
            else
            {
                OfficialDialogueContractReport contract =
                    OfficialDialogueContractValidator.Validate(
                        csv.text, choices.text, sceneIndex.text);
                foreach (string error in contract.Errors)
                    Error(items, "DIALOGUE_SHAPE", CsvPath, error);
            }
            foreach (DialogueDiagnostic diagnostic in
                     DialogueContentValidator.Validate(csv.text).Diagnostics
                         .Where(item => item.Severity ==
                             DialogueDiagnosticSeverity.Error))
                Error(items, "DIALOGUE_SHAPE", CsvPath, diagnostic.ToString());
            foreach (IGrouping<string, DialogueRecord> duplicate in
                     parsed.Records.GroupBy(record => record.StableLineId,
                         StringComparer.Ordinal).Where(group => group.Count() > 1))
                Error(items, "STABLE_ID_DUPLICATE", CsvPath,
                    $"stable line ID 중복: {duplicate.Key}");
            return parsed;
        }

        private static void ValidateSceneBinding(
            ICollection<ProductionPreflightDiagnostic> items)
        {
            if (!File.Exists(ScenePath))
            {
                Error(items, "SCENE_DIALOGUE_SOURCE", ScenePath,
                    "UI Basic Scene을 찾을 수 없습니다.");
                return;
            }
            string scene = File.ReadAllText(ScenePath, Encoding.UTF8);
            string scriptGuid = AssetDatabase.AssetPathToGUID(DialogueScriptPath);
            string csvGuid = AssetDatabase.AssetPathToGUID(CsvPath);
            MatchCollection databases = Regex.Matches(scene,
                $@"m_Script:\s*\{{[^}}]*guid:\s*{scriptGuid}[^}}]*\}}" +
                @"(?<body>.*?)(?=--- !u!|\z)",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            bool bound = databases.Count == 1 &&
                databases[0].Groups["body"].Value.Contains(
                    $"csvFile: {{fileID: 4900000, guid: {csvGuid},",
                    StringComparison.Ordinal);
            if (!bound)
                Error(items, "SCENE_DIALOGUE_SOURCE", ScenePath,
                    "DialogueDatabase 하나가 프로덕션 CSV를 직접 참조해야 합니다.");
        }

        private static void ValidateLocations(
            ICollection<ProductionPreflightDiagnostic> items)
        {
            LocationDefinition[] assets = AssetDatabase
                .FindAssets("t:LocationDefinition", new[] { LocationFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<LocationDefinition>)
                .Where(asset => asset != null).ToArray();
            IReadOnlyList<LocationCatalogDiagnostic> diagnostics =
                CanonicalLocationCatalog.Validate(assets, ProductionSceneCatalog.All);
            foreach (LocationCatalogDiagnostic diagnostic in diagnostics.Where(
                         item => item.Severity ==
                             LocationCatalogDiagnosticSeverity.Error))
                Error(items, "LOCATION_ASSET_SET", LocationFolder,
                    $"{diagnostic.Code}: {diagnostic.Message}");
            string[] unresolved = diagnostics.Where(item => item.Severity ==
                    LocationCatalogDiagnosticSeverity.Warning)
                .Select(item => item.Code).Distinct().OrderBy(code => code).ToArray();
            if (unresolved.Length > 0)
                Warning(items, "UNRESOLVED_LOCATION", LocationFolder,
                    $"확정 배경이 없는 장소 {unresolved.Length}곳: " +
                    string.Join(", ", unresolved));
        }

        private static void ValidateEvidence(
            ICollection<ProductionPreflightDiagnostic> items)
        {
            foreach (CanonicalEvidenceEntry entry in CanonicalEvidenceCatalog.All)
            {
                string path = $"{EvidenceFolder}/EvidenceDefinition_" +
                              $"{entry.Id.Replace("-", string.Empty)}.asset";
                EvidenceDefinition asset =
                    AssetDatabase.LoadAssetAtPath<EvidenceDefinition>(path);
                if (asset == null || asset.EvidenceId != entry.Id ||
                    asset.DisplayName != entry.DisplayName ||
                    asset.Description != entry.Description ||
                    asset.Category != entry.Category ||
                    asset.IsDirect != entry.IsDirect ||
                    string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
                    Error(items, "EVIDENCE_ASSET_SET", path,
                        $"{entry.Id} 에셋 또는 canonical metadata가 없습니다.");
            }
        }

        private static void ValidatePortraits(
            ICollection<ProductionPreflightDiagnostic> items)
        {
            PortraitEmotion[] emotions = (PortraitEmotion[])
                Enum.GetValues(typeof(PortraitEmotion));
            foreach (DialoguePortraitDefinition portrait in
                     DialoguePortraitCatalog.All)
            {
                if (!portrait.UsesExpressionSprites)
                {
                    string fallbackPath =
                        $"Assets/_Project/Resources/" +
                        $"{portrait.FallbackTexture}.png";
                    if (AssetDatabase.LoadAssetAtPath<Texture2D>(
                            fallbackPath) == null)
                    {
                        Error(
                            items,
                            "PORTRAIT_ASSET_SET",
                            fallbackPath,
                            $"동적 인물 폴백 텍스처 누락: " +
                            $"{portrait.CharacterId}");
                    }
                    continue;
                }

                string path = $"{PortraitFolder}/portrait_" +
                              $"{portrait.ExpressionSheet}_expressions.png";
                HashSet<string> sprites = AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<Sprite>().Select(sprite => sprite.name)
                    .ToHashSet(StringComparer.Ordinal);
                foreach (PortraitEmotion emotion in emotions)
                {
                    string expected =
                        DialoguePortraitCatalog.GetSpriteName(portrait, emotion);
                    if (!sprites.Contains(expected))
                        Error(items, "PORTRAIT_ASSET_SET", path,
                            $"표정 sprite 누락: {expected}");
                }
            }
        }

        private static void ValidateSerializedReferences(
            ICollection<ProductionPreflightDiagnostic> items)
        {
            IEnumerable<string> scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled).Select(scene => scene.path);
            IEnumerable<string> prefabs = AssetDatabase.FindAssets(
                    "t:Prefab", new[] { "Assets/_Project/Prefabs" })
                .Select(AssetDatabase.GUIDToAssetPath);
            foreach (string path in scenes.Concat(prefabs).Distinct())
            {
                string yaml = File.ReadAllText(path, Encoding.UTF8);
                if (MissingScriptPattern.IsMatch(yaml))
                    Error(items, "SERIALIZED_REFERENCE", path,
                        "missing script 직접 참조가 있습니다.");
                foreach (Match script in ScriptPattern.Matches(yaml))
                {
                    string guid = script.Groups["guid"].Value;
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(assetPath) ||
                        AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath) == null)
                        Error(items, "SERIALIZED_REFERENCE", path,
                            $"연결할 수 없는 script GUID: {guid}");
                }
                foreach (string guid in GuidPattern.Matches(yaml)
                             .Select(match => match.Groups["guid"].Value)
                             .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    bool builtIn = guid.StartsWith("0000000000000000",
                        StringComparison.Ordinal);
                    if (!builtIn && string.IsNullOrEmpty(
                            AssetDatabase.GUIDToAssetPath(guid)))
                        Error(items, "SERIALIZED_REFERENCE", path,
                            $"연결할 수 없는 asset GUID: {guid}");
                }
            }
        }

        private static void ValidateTextEncoding(
            ICollection<ProductionPreflightDiagnostic> items)
        {
            string[] roots =
            {
                "Assets/_Project/Content", "Assets/_Project/Scenes",
                "Assets/_Project/Prefabs"
            };
            var extensions = new HashSet<string>(
                new[] { ".csv", ".asset", ".unity", ".prefab", ".md" },
                StringComparer.OrdinalIgnoreCase);
            var strictUtf8 = new UTF8Encoding(false, true);
            foreach (string path in roots.SelectMany(root =>
                         Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                         .Where(path => extensions.Contains(Path.GetExtension(path))))
            {
                try
                {
                    string text = strictUtf8.GetString(File.ReadAllBytes(path));
                    if (text.Contains('\uFFFD') ||
                        text.Contains("???", StringComparison.Ordinal) ||
                        text.Contains("占쏙옙", StringComparison.Ordinal))
                        Error(items, "TEXT_ENCODING", path,
                            "replacement character 또는 명백한 모지바케가 있습니다.");
                }
                catch (DecoderFallbackException)
                {
                    Error(items, "TEXT_ENCODING", path,
                        "UTF-8로 해석할 수 없는 바이트가 있습니다.");
                }
            }
        }

        private static void AddWarnings(
            DialogueCsvParseResult dialogue,
            ICollection<ProductionPreflightDiagnostic> items)
        {
            if (TimelinePuzzleCatalog.SourceMissingCount > 0)
                Warning(items, "TIMELINE_SOURCE_MISSING",
                    TimelinePuzzleCatalog.SceneId,
                    $"권위 자료가 없는 카드 " +
                    $"{TimelinePuzzleCatalog.SourceMissingCount}개");
            HashSet<string> voices = AssetDatabase.IsValidFolder(
                    "Assets/_Project/Resources/Voice")
                ? AssetDatabase.FindAssets("t:AudioClip",
                        new[] { "Assets/_Project/Resources/Voice" })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(Path.GetFileNameWithoutExtension)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int missingVoice = dialogue.Records.Count(record =>
                record.VoiceRequired && !voices.Contains(record.StableLineId));
            if (missingVoice > 0)
                Warning(items, "VOICE_CLIP_MISSING", CsvPath,
                    $"voice_required AudioClip 누락 {missingVoice}개");
            bool hasKoreanFont = AssetDatabase.FindAssets("t:Font",
                    new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<Font>)
                .Any(font => font != null &&
                    !font.name.Contains("LiberationSans",
                        StringComparison.OrdinalIgnoreCase) &&
                    font.HasCharacter('한'));
            if (!hasKoreanFont)
                Warning(items, "KOREAN_FONT_MISSING", "Assets",
                    "프로젝트에 번들된 한국어 글꼴이 없습니다.");
        }

        private static void Error(
            ICollection<ProductionPreflightDiagnostic> items,
            string code, string path, string message) =>
            Add(items, ProductionPreflightSeverity.Error, code, path, message);

        private static void Warning(
            ICollection<ProductionPreflightDiagnostic> items,
            string code, string path, string message) =>
            Add(items, ProductionPreflightSeverity.Warning, code, path, message);

        private static void Add(
            ICollection<ProductionPreflightDiagnostic> items,
            ProductionPreflightSeverity severity,
            string code, string path, string message) =>
            items.Add(new ProductionPreflightDiagnostic(
                severity, code, path, message));
    }

    public sealed class ProductionContentBuildGate : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report) =>
            ProductionContentPreflight.ThrowIfErrors(
                ProductionContentPreflight.Run());
    }
}
