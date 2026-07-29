using UnityEditor;
using UnityEngine;

namespace Wake.Editor
{
    public sealed class LocationBackgroundVariantAssetImporter :
        AssetPostprocessor
    {
        private const string VariantRoot =
            "Assets/_Project/Resources/LocationBackgroundVariants/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(
                    VariantRoot,
                    System.StringComparison.Ordinal))
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = false;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.textureCompression =
                TextureImporterCompression.CompressedHQ;
            importer.compressionQuality = 50;
        }
    }
}
