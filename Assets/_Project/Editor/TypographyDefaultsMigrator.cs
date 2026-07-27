using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Wake.UI;

namespace Wake.Editor
{
    public static class TypographyDefaultsMigrator
    {
        public const string TmpSettingsPath =
            "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        public const string LegacyFontPath =
            "Assets/TextMesh Pro/Resources/Fonts & Materials/" +
            "LiberationSans SDF.asset";
        public const string UiScenePath =
            "Assets/_Project/Scenes/UI/UI Basic Scene.unity";

        [MenuItem("Wake/Typography/Migrate Project Defaults")]
        public static void MigrateProjectDefaults()
        {
            TypographyCatalog catalog =
                AssetDatabase.LoadAssetAtPath<TypographyCatalog>(
                    TypographyCatalogBuilder.CatalogPath);
            if (catalog == null || catalog.Body == null)
            {
                throw new InvalidOperationException(
                    "TypographyCatalog with a Body font is required.");
            }

            TMP_FontAsset legacy =
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    LegacyFontPath);
            if (legacy == null)
            {
                throw new InvalidOperationException(
                    "Legacy Liberation Sans TMP asset is missing.");
            }

            SetTmpDefault(catalog.Body);
            int migrated = MigrateScene(
                UiScenePath,
                legacy,
                catalog.Body);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Migrated TMP default and {migrated} scene labels " +
                $"to {catalog.Body.name}.");
        }

        public static bool ShouldReplace(
            TMP_FontAsset current,
            TMP_FontAsset legacy)
        {
            return current == null ||
                (legacy != null && current == legacy);
        }

        public static int MigrateTexts(
            IEnumerable<TMP_Text> labels,
            TMP_FontAsset legacy,
            TMP_FontAsset replacement)
        {
            if (labels == null || replacement == null)
            {
                return 0;
            }

            int count = 0;
            foreach (TMP_Text label in labels)
            {
                if (label == null ||
                    !ShouldReplace(label.font, legacy))
                {
                    continue;
                }

                Undo.RecordObject(label, "Migrate TMP default font");
                Material authoredMaterial = label.fontSharedMaterial;
                bool preserveMaterial = authoredMaterial != null &&
                    legacy != null &&
                    authoredMaterial != legacy.material &&
                    !AssetDatabase.Contains(authoredMaterial);
                label.font = replacement;
                if (preserveMaterial)
                {
                    Undo.RecordObject(
                        authoredMaterial,
                        "Migrate TMP material atlas");
                    authoredMaterial.mainTexture =
                        replacement.atlasTexture;
                    label.fontSharedMaterial = authoredMaterial;
                    EditorUtility.SetDirty(authoredMaterial);
                }
                else
                {
                    label.fontSharedMaterial = replacement.material;
                }
                label.SetAllDirty();
                EditorUtility.SetDirty(label);
                count++;
            }
            return count;
        }

        public static int MigrateScene(
            string scenePath,
            TMP_FontAsset legacy,
            TMP_FontAsset replacement)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                throw new ArgumentException(
                    "Scene path is required.",
                    nameof(scenePath));
            }
            if (replacement == null)
            {
                throw new ArgumentNullException(nameof(replacement));
            }

            Scene scene = EditorSceneManager.OpenScene(
                scenePath,
                OpenSceneMode.Single);
            TMP_Text[] labels = scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<TMP_Text>(true))
                .ToArray();
            int count = MigrateTexts(labels, legacy, replacement);
            if (count > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            return count;
        }

        public static void SetTmpDefault(TMP_FontAsset replacement)
        {
            if (replacement == null)
            {
                throw new ArgumentNullException(nameof(replacement));
            }

            TMP_Settings settings =
                AssetDatabase.LoadAssetAtPath<TMP_Settings>(
                    TmpSettingsPath);
            if (settings == null)
            {
                throw new InvalidOperationException(
                    "TMP Settings asset is missing.");
            }

            SerializedObject serialized = new(settings);
            serialized.FindProperty("m_defaultFontAsset")
                .objectReferenceValue = replacement;
            SerializedProperty fallbacks =
                serialized.FindProperty("m_fallbackFontAssets");
            if (!ContainsReference(fallbacks, replacement))
            {
                int index = fallbacks.arraySize;
                fallbacks.InsertArrayElementAtIndex(index);
                fallbacks.GetArrayElementAtIndex(index)
                    .objectReferenceValue = replacement;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            TMP_Settings.defaultFontAsset = replacement;
        }

        private static bool ContainsReference(
            SerializedProperty array,
            UnityEngine.Object value)
        {
            if (array == null || !array.isArray)
            {
                return false;
            }
            for (int index = 0; index < array.arraySize; index++)
            {
                if (array.GetArrayElementAtIndex(index)
                        .objectReferenceValue == value)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
