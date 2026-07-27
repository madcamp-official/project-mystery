using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Wake.UI;

namespace Wake.Editor
{
    public static class TypographyReleasePreflight
    {
        public const string MissingCatalogCode = "TYPOGRAPHY_CATALOG_MISSING";
        public const string MissingRoleCode = "TYPOGRAPHY_ROLE_MISSING";
        public const string DefaultFontCode = "TMP_DEFAULT_FONT_MISMATCH";
        public const string MissingGlyphCode = "TYPOGRAPHY_GLYPH_MISSING";

        [MenuItem("Wake/Typography/Validate Release Setup")]
        public static void ValidateProject()
        {
            TypographyCatalog catalog =
                AssetDatabase.LoadAssetAtPath<TypographyCatalog>(
                    TypographyCatalogBuilder.CatalogPath);
            string corpus = File.Exists(TypographyGlyphPreflight.CorpusPath)
                ? File.ReadAllText(TypographyGlyphPreflight.CorpusPath)
                : string.Empty;
            IReadOnlyList<string> errors = Validate(
                catalog,
                TMP_Settings.defaultFontAsset,
                corpus);
            ThrowIfInvalid(errors);
            UnityEngine.Debug.Log(
                "Typography release setup validation passed.");
        }

        public static IReadOnlyList<string> Validate(
            TypographyCatalog catalog,
            TMP_FontAsset defaultFont,
            string corpus)
        {
            List<string> errors = new();
            if (catalog == null)
            {
                errors.Add(
                    $"{MissingCatalogCode}: " +
                    $"{TypographyCatalogBuilder.CatalogPath}");
                return errors;
            }

            IReadOnlyList<TypographyRole> missingRoles =
                catalog.GetMissingRoles(includeSpecialRoles: true);
            if (missingRoles.Count > 0)
            {
                errors.Add(
                    $"{MissingRoleCode}: " +
                    string.Join(", ", missingRoles));
            }

            if (catalog.Body == null || defaultFont != catalog.Body)
            {
                errors.Add(
                    $"{DefaultFontCode}: TMP default must match Body");
            }

            IReadOnlyList<MissingGlyph> missingGlyphs =
                TypographyGlyphPreflight.Validate(
                    catalog,
                    corpus,
                    tryAddCharacters: false);
            if (missingGlyphs.Count > 0)
            {
                string preview = string.Join(
                    ", ",
                    missingGlyphs.Take(20).Select(item =>
                        $"{item.Role}:U+{(int)item.Character:X4}"));
                errors.Add(
                    $"{MissingGlyphCode}: {missingGlyphs.Count} " +
                    $"assignments ({preview})");
            }

            return errors;
        }

        public static void ThrowIfInvalid(
            IReadOnlyList<string> errors)
        {
            if (errors == null || errors.Count == 0)
            {
                return;
            }

            throw new BuildFailedException(
                "Typography release preflight failed:\n" +
                string.Join("\n", errors));
        }
    }

    public sealed class TypographyReleaseBuildGate :
        IPreprocessBuildWithReport
    {
        public int callbackOrder => 1;

        public void OnPreprocessBuild(BuildReport report)
        {
            TypographyReleasePreflight.ValidateProject();
        }
    }
}
