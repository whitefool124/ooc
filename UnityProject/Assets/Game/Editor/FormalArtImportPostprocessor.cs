using System;
using UnityEditor;
using UnityEngine;

namespace OCC.Combat.Editor
{
    public sealed class FormalArtImportPostprocessor : AssetPostprocessor
    {
        private const string FormalRoot = "Assets/Game/Resources/Art/Formal";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(FormalRoot, StringComparison.Ordinal)) return;
            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            bool semanticMicroIcon = assetPath.Contains("/FormalIntentIcons16/", StringComparison.Ordinal) ||
                                     assetPath.EndsWith("/action_point.png", StringComparison.Ordinal) ||
                                     assetPath.EndsWith("/mana.png", StringComparison.Ordinal) ||
                                     assetPath.EndsWith("/notice.png", StringComparison.Ordinal);
            importer.spritePixelsPerUnit = semanticMicroIcon ? 16f : 32f;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (assetPath.Contains("FormalUISkin16/"))
            {
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                settings.spritePivot = new Vector2(.5f, .5f);
                settings.spriteBorder = new Vector4(4f, 4f, 4f, 4f);
            }
            else if (assetPath.Contains("FormalUnits64/"))
            {
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(.5f, .09375f); // logical X=32, Y=58 on a 64px cell
            }
            else
            {
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                settings.spritePivot = new Vector2(.5f, .5f);
            }
            importer.SetTextureSettings(settings);
        }

        [MenuItem("OCC/Formal Art/Reimport All")]
        public static void ReimportAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Game/Resources/Art" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith(FormalRoot, StringComparison.Ordinal))
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }
    }
}
