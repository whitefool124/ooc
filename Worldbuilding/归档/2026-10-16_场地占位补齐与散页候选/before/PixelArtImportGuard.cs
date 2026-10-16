using System;
using UnityEditor;
using UnityEngine;

namespace OCC.Combat.Editor
{
    /// <summary>Applies the non-negotiable sampling policy to OCC art assets.</summary>
    internal sealed class PixelArtImportGuard : AssetPostprocessor
    {
        private const string ArtRoot = "Assets/Game/Resources/Art/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot, StringComparison.Ordinal)) return;
            TextureImporter importer = (TextureImporter)assetImporter;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
        }
    }
}
