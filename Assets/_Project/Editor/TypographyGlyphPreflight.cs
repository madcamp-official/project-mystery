using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using Wake.UI;

namespace Wake.Editor
{
    public readonly struct MissingGlyph
    {
        public MissingGlyph(TypographyRole role, char character)
        {
            Role = role;
            Character = character;
        }

        public TypographyRole Role { get; }
        public char Character { get; }
    }

    public static class TypographyGlyphPreflight
    {
        public const string CorpusPath =
            "Assets/_Project/Fonts/TMP/ProjectGlyphs.txt";

        private static readonly HashSet<string> SourceExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".asset", ".cs", ".csv", ".json", ".txt", ".unity"
            };

        private static readonly HashSet<char> CorruptionSentinels =
            new("占媛寃遺鍮紐吏⑺");

        public static IReadOnlyList<TypographyRole> RequiredRoles { get; } =
            new[]
            {
                TypographyRole.Body,
                TypographyRole.BodyRegular,
                TypographyRole.Choice,
                TypographyRole.SpeakerName,
                TypographyRole.Heading,
                TypographyRole.HeadingStrong,
                TypographyRole.Technical,
                TypographyRole.TechnicalStrong,
                TypographyRole.Handwritten,
                TypographyRole.SpecialAlert,
                TypographyRole.SpecialComic
            };

        [MenuItem("Wake/Typography/Collect Project Glyphs")]
        public static void CollectProjectGlyphs()
        {
            IReadOnlyList<string> paths = FindTextAssetPaths();
            string corpus = CollectCharacters(
                paths.Select(File.ReadAllText));
            Directory.CreateDirectory(Path.GetDirectoryName(CorpusPath));
            File.WriteAllText(
                CorpusPath,
                corpus + Environment.NewLine,
                new UTF8Encoding(false));
            AssetDatabase.ImportAsset(CorpusPath);
            Debug.Log(
                $"Collected {corpus.Length} unique glyphs from " +
                $"{paths.Count} project text assets.");
        }

        [MenuItem("Wake/Typography/Prepare Project Glyphs")]
        public static void PrepareProjectGlyphs()
        {
            TypographyCatalog catalog =
                AssetDatabase.LoadAssetAtPath<TypographyCatalog>(
                    TypographyCatalogBuilder.CatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException(
                    "Build the TypographyCatalog before preparing glyphs.");
            }

            string corpus = ReadCorpus();
            IReadOnlyList<MissingGlyph> missing =
                Validate(catalog, corpus, tryAddCharacters: true);
            MarkFontsDirty(catalog);
            AssetDatabase.SaveAssets();
            if (missing.Count > 0)
            {
                throw new InvalidOperationException(
                    FormatMissing(missing));
            }
        }

        [MenuItem("Wake/Typography/Validate Release Glyphs")]
        public static void ValidateReleaseGlyphs()
        {
            TypographyCatalog catalog =
                AssetDatabase.LoadAssetAtPath<TypographyCatalog>(
                    TypographyCatalogBuilder.CatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException(
                    "TypographyCatalog asset is missing.");
            }

            IReadOnlyList<MissingGlyph> missing =
                Validate(catalog, ReadCorpus(), tryAddCharacters: false);
            if (missing.Count > 0)
            {
                throw new InvalidOperationException(
                    FormatMissing(missing));
            }

            Debug.Log("Typography release glyph validation passed.");
        }

        public static string CollectCharacters(
            IEnumerable<string> contents)
        {
            SortedSet<char> characters = new();
            if (contents == null)
            {
                return string.Empty;
            }

            foreach (string content in contents)
            {
                if (string.IsNullOrEmpty(content))
                {
                    continue;
                }

                foreach (char character in content)
                {
                    if (!char.IsControl(character) &&
                        !char.IsWhiteSpace(character) &&
                        !char.IsSurrogate(character) &&
                        !CorruptionSentinels.Contains(character))
                    {
                        characters.Add(character);
                    }
                }
            }
            return new string(characters.ToArray());
        }

        public static bool IsSupportedSource(string path)
        {
            if (string.IsNullOrWhiteSpace(path) ||
                !SourceExtensions.Contains(Path.GetExtension(path)))
            {
                return false;
            }

            string normalized = path.Replace('\\', '/');
            return !normalized.Contains("/Editor/", StringComparison.Ordinal) &&
                !normalized.Contains("/Fonts/", StringComparison.Ordinal) &&
                !normalized.Contains("/Tests/", StringComparison.Ordinal);
        }

        public static IReadOnlyList<MissingGlyph> Validate(
            TypographyCatalog catalog,
            string corpus,
            bool tryAddCharacters)
        {
            List<MissingGlyph> missing = new();
            if (catalog == null || string.IsNullOrEmpty(corpus))
            {
                return missing;
            }

            foreach (TypographyRole role in RequiredRoles)
            {
                TMP_FontAsset font = catalog.Resolve(role);
                if (font == null)
                {
                    foreach (char character in corpus)
                    {
                        missing.Add(new MissingGlyph(role, character));
                    }
                    continue;
                }

                foreach (char character in corpus)
                {
                    if (!font.HasCharacter(
                            character,
                            searchFallbacks: true,
                            tryAddCharacter: tryAddCharacters))
                    {
                        missing.Add(new MissingGlyph(role, character));
                    }
                }
            }
            return missing;
        }

        private static IReadOnlyList<string> FindTextAssetPaths()
        {
            return AssetDatabase.FindAssets(
                    string.Empty,
                    new[] { "Assets/_Project" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(IsSupportedSource)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }

        private static string ReadCorpus()
        {
            if (!File.Exists(CorpusPath))
            {
                throw new FileNotFoundException(
                    "Collect project glyphs before validation.",
                    CorpusPath);
            }
            return CollectCharacters(new[] { File.ReadAllText(CorpusPath) });
        }

        private static void MarkFontsDirty(TypographyCatalog catalog)
        {
            foreach (TypographyRole role in RequiredRoles)
            {
                TMP_FontAsset font = catalog.Resolve(role);
                if (font != null)
                {
                    // Keep the bundled source font available at runtime so
                    // newly introduced Korean syllables can be added without
                    // rebuilding every static atlas by hand.
                    font.atlasPopulationMode =
                        AtlasPopulationMode.Dynamic;
                    EditorUtility.SetDirty(font);
                }
            }
        }

        private static string FormatMissing(
            IReadOnlyList<MissingGlyph> missing)
        {
            string preview = string.Join(
                ", ",
                missing.Take(40).Select(
                    item => $"{item.Role}:U+{(int)item.Character:X4}"));
            return $"Missing {missing.Count} required glyph assignments. " +
                preview;
        }
    }
}
