using UnityEditor;

namespace Wake.Editor
{
    public sealed class MapLayerAssetImporter : AssetPostprocessor
    {
        private const string LayerRoot =
            "Assets/_Project/Resources/Maps/DeckLayers/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(
                    LayerRoot,
                    System.StringComparison.Ordinal))
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.textureCompression =
                TextureImporterCompression.CompressedHQ;
        }

        [InitializeOnLoadMethod]
        private static void ConfigureExistingLayers()
        {
            EditorApplication.delayCall += () =>
            {
                string[] guids = AssetDatabase.FindAssets(
                    "t:Texture2D",
                    new[] { LayerRoot.TrimEnd('/') });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (AssetImporter.GetAtPath(path) is not
                        TextureImporter importer)
                    {
                        continue;
                    }

                    bool needsImport =
                        importer.textureType != TextureImporterType.Sprite ||
                        importer.spriteImportMode != SpriteImportMode.Single ||
                        importer.mipmapEnabled ||
                        !importer.alphaIsTransparency ||
                        importer.wrapMode != UnityEngine.TextureWrapMode.Clamp;
                    if (!needsImport)
                        continue;

                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
                    importer.textureCompression =
                        TextureImporterCompression.CompressedHQ;
                    importer.SaveAndReimport();
                }
            };
        }
    }
}
