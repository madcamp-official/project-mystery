using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace Wake.Tests
{
    public sealed class RuntimeTypographyMigrationTests
    {
        private const string RuntimeCodeRoot = "Assets/_Project/Code";

        [Test]
        public void RuntimeControllers_DoNotUseCompatibilityFontProperty()
        {
            IReadOnlyList<string> offenders = FindRuntimeSources(
                "StatusHUDController.RuntimeKoreanFont",
                "StatusHUDController.cs");

            Assert.That(
                offenders,
                Is.Empty,
                "새 UI는 TypographyRole을 사용해야 합니다: " +
                string.Join(", ", offenders));
        }

        [Test]
        public void RuntimeControllers_DoNotReferenceLiberationSans()
        {
            IReadOnlyList<string> offenders = FindRuntimeSources(
                "LiberationSans");

            Assert.That(
                offenders,
                Is.Empty,
                "런타임 코드에 Liberation Sans 참조가 남았습니다: " +
                string.Join(", ", offenders));
        }

        [Test]
        public void TheoryBoard_UsesSemanticTypographySurface()
        {
            string source = ReadRuntimeSource(
                "UI/EvidenceTheoryBoardController.cs");

            Assert.That(
                source,
                Does.Contain("FeatureTypography.ApplyTheoryBoard"));
            Assert.That(
                source,
                Does.Not.Contain("RuntimeKoreanFont"));
        }

        [Test]
        public void StartMenu_UsesHeadingTypographyRole()
        {
            string manager = ReadRuntimeSource("UI/UIManager.cs");
            string policy = ReadRuntimeSource("UI/FeatureTypography.cs");

            Assert.That(
                manager,
                Does.Contain("FeatureTypography.ApplyMenuAction"));
            Assert.That(
                policy,
                Does.Contain("TypographyRole.Heading"));
        }

        [Test]
        public void Toast_RuntimeSurfaceRemainsDisabled()
        {
            string source = ReadRuntimeSource("UI/ToastController.cs");

            Assert.That(
                source,
                Does.Contain("RuntimeSurfaceEnabled => false"));
            Assert.That(
                source,
                Does.Not.Contain("new GameObject(\"Toast\""));
            Assert.That(
                source,
                Does.Not.Contain("BuildToastUi"));
        }

        [Test]
        public void CompatibilityProperty_RemainsAvailableForLegacyCallers()
        {
            string source = ReadRuntimeSource("UI/StatusHUDController.cs");

            Assert.That(
                source,
                Does.Contain("public static TMP_FontAsset RuntimeKoreanFont"));
            Assert.That(
                source,
                Does.Contain("TypographyService.Resolve(TypographyRole.Body)"));
        }

        private static IReadOnlyList<string> FindRuntimeSources(
            string needle,
            params string[] excludedFileNames)
        {
            HashSet<string> excluded = new(
                excludedFileNames ?? System.Array.Empty<string>());
            return AssetDatabase.FindAssets("t:MonoScript", new[] { RuntimeCodeRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".cs"))
                .Where(path => !excluded.Contains(Path.GetFileName(path)))
                .Where(path => File.ReadAllText(path).Contains(needle))
                .OrderBy(path => path)
                .ToArray();
        }

        private static string ReadRuntimeSource(string relativePath)
        {
            string path = $"{RuntimeCodeRoot}/{relativePath}";
            Assert.That(
                File.Exists(path),
                Is.True,
                $"런타임 소스를 찾지 못했습니다: {path}");
            return File.ReadAllText(path);
        }
    }
}
