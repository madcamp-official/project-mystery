using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using Wake.UI;

namespace Wake.Editor
{
    public readonly struct FontBuildSpec
    {
        public FontBuildSpec(
            TypographyRole role,
            string sourcePath,
            string assetName,
            string catalogProperty,
            bool optional = false)
        {
            Role = role;
            SourcePath = sourcePath;
            AssetName = assetName;
            CatalogProperty = catalogProperty;
            Optional = optional;
        }

        public TypographyRole Role { get; }
        public string SourcePath { get; }
        public string AssetName { get; }
        public string CatalogProperty { get; }
        public bool Optional { get; }
    }

    public static class TypographyCatalogBuilder
    {
        public const string OutputRoot =
            "Assets/_Project/Resources/Typography";
        public const string CatalogPath =
            OutputRoot + "/TypographyCatalog.asset";

        private const int SamplingPointSize = 60;
        private const int AtlasPadding = 6;
        private const int AtlasSize = 1024;

        private static readonly FontBuildSpec[] FontSpecifications =
        {
            new(
                TypographyRole.Body,
                "Assets/_Project/Fonts/Source/Pretendard/" +
                "Pretendard-Medium.ttf",
                "Pretendard Medium SDF",
                "body"),
            new(
                TypographyRole.BodyRegular,
                "Assets/_Project/Fonts/Source/Pretendard/" +
                "Pretendard-Regular.ttf",
                "Pretendard Regular SDF",
                "bodyRegular"),
            new(
                TypographyRole.Choice,
                "Assets/_Project/Fonts/Source/Pretendard/" +
                "Pretendard-SemiBold.ttf",
                "Pretendard SemiBold SDF",
                "choice"),
            new(
                TypographyRole.SpeakerName,
                "Assets/_Project/Fonts/Source/SUITE/SUITE-Bold.ttf",
                "SUITE Bold SDF",
                "speakerName"),
            new(
                TypographyRole.Heading,
                "Assets/_Project/Fonts/Source/SUITE/SUITE-SemiBold.ttf",
                "SUITE SemiBold SDF",
                "heading"),
            new(
                TypographyRole.HeadingStrong,
                "Assets/_Project/Fonts/Source/SUITE/SUITE-ExtraBold.ttf",
                "SUITE ExtraBold SDF",
                "headingStrong"),
            new(
                TypographyRole.Technical,
                "Assets/_Project/Fonts/Source/IBMPlexMono/" +
                "IBMPlexMono-Medium.ttf",
                "IBM Plex Mono Medium SDF",
                "technical"),
            new(
                TypographyRole.TechnicalStrong,
                "Assets/_Project/Fonts/Source/IBMPlexMono/" +
                "IBMPlexMono-SemiBold.ttf",
                "IBM Plex Mono SemiBold SDF",
                "technicalStrong"),
            new(
                TypographyRole.Handwritten,
                "Assets/_Project/Fonts/Source/Special/" +
                "GowunDodum-Regular.ttf",
                "Gowun Dodum Regular SDF",
                "handwritten",
                optional: true),
            new(
                TypographyRole.SpecialAlert,
                "Assets/_Project/Fonts/Source/Special/" +
                "BlackHanSans-Regular.ttf",
                "Black Han Sans Regular SDF",
                "specialAlert",
                optional: true),
            new(
                TypographyRole.SpecialComic,
                "Assets/_Project/Fonts/Source/Special/Jua-Regular.ttf",
                "Jua Regular SDF",
                "specialComic",
                optional: true)
        };

        public static IReadOnlyList<FontBuildSpec> Specifications =>
            FontSpecifications;

        [MenuItem("Wake/Typography/Rebuild Font Assets")]
        public static void RebuildFontAssets()
        {
            EnsureFolder(OutputRoot);
            Dictionary<TypographyRole, TMP_FontAsset> fonts = new();

            try
            {
                for (int index = 0; index < FontSpecifications.Length; index++)
                {
                    FontBuildSpec specification = FontSpecifications[index];
                    EditorUtility.DisplayProgressBar(
                        "Wake Typography",
                        $"Building {specification.AssetName}",
                        (float)index / FontSpecifications.Length);
                    fonts.Add(
                        specification.Role,
                        BuildFontAsset(specification));
                }

                ConfigureFallbacks(fonts);
                BuildCatalog(fonts);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                TypographyService.ClearCache();
                Debug.Log(
                    $"Built {fonts.Count} typography font assets and catalog.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static TMP_FontAsset BuildFontAsset(
            FontBuildSpec specification)
        {
            Font source = AssetDatabase.LoadAssetAtPath<Font>(
                specification.SourcePath);
            if (source == null)
            {
                throw new InvalidOperationException(
                    $"Missing font source: {specification.SourcePath}");
            }

            string assetPath =
                $"{OutputRoot}/{specification.AssetName}.asset";
            AssetDatabase.DeleteAsset(assetPath);

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                source,
                SamplingPointSize,
                AtlasPadding,
                GlyphRenderMode.SDFAA,
                AtlasSize,
                AtlasSize,
                AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);
            if (fontAsset == null)
            {
                throw new InvalidOperationException(
                    $"TMP failed to build {specification.AssetName}.");
            }

            fontAsset.name = specification.AssetName;
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            fontAsset.isMultiAtlasTexturesEnabled = true;
            AssetDatabase.CreateAsset(fontAsset, assetPath);
            AddGeneratedSubAssets(fontAsset);
            EditorUtility.SetDirty(fontAsset);
            return fontAsset;
        }

        private static void AddGeneratedSubAssets(TMP_FontAsset fontAsset)
        {
            if (fontAsset.material != null &&
                !AssetDatabase.Contains(fontAsset.material))
            {
                fontAsset.material.name = $"{fontAsset.name} Material";
                AssetDatabase.AddObjectToAsset(
                    fontAsset.material,
                    fontAsset);
            }

            foreach (Texture2D texture in fontAsset.atlasTextures)
            {
                if (texture == null || AssetDatabase.Contains(texture))
                {
                    continue;
                }

                texture.name = $"{fontAsset.name} Atlas";
                AssetDatabase.AddObjectToAsset(texture, fontAsset);
            }
        }

        private static void ConfigureFallbacks(
            IReadOnlyDictionary<TypographyRole, TMP_FontAsset> fonts)
        {
            TMP_FontAsset body = fonts[TypographyRole.Body];
            foreach (KeyValuePair<TypographyRole, TMP_FontAsset> pair in fonts)
            {
                if (pair.Key == TypographyRole.Body)
                {
                    continue;
                }

                pair.Value.fallbackFontAssetTable =
                    new List<TMP_FontAsset> { body };
                EditorUtility.SetDirty(pair.Value);
            }
        }

        private static void BuildCatalog(
            IReadOnlyDictionary<TypographyRole, TMP_FontAsset> fonts)
        {
            TypographyCatalog catalog =
                AssetDatabase.LoadAssetAtPath<TypographyCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<TypographyCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            SerializedObject serialized = new(catalog);
            foreach (FontBuildSpec specification in FontSpecifications)
            {
                serialized.FindProperty(specification.CatalogProperty)
                    .objectReferenceValue = fonts[specification.Role];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string segment in path
                         .Split('/')
                         .Skip(1))
            {
                string next = $"{current}/{segment}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segment);
                }

                current = next;
            }
        }
    }
}
