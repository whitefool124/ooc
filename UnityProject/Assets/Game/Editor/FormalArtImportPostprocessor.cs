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
            bool resourceBar32 = assetPath.Contains("/FormalResourceBars32/", StringComparison.Ordinal);
            bool unitBar16 = assetPath.Contains("/FormalUnitBars16/", StringComparison.Ordinal);
            importer.spriteImportMode = resourceBar32 || unitBar16 ? SpriteImportMode.Multiple : SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            bool semanticMicroIcon = assetPath.Contains("/FormalCommandIcons16/", StringComparison.Ordinal) ||
                                     assetPath.Contains("/FormalIntentIcons16/", StringComparison.Ordinal) ||
                                     assetPath.Contains("/FormalCombatIndicators16/", StringComparison.Ordinal) ||
                                     assetPath.Contains("/FormalMapStateIcons16/", StringComparison.Ordinal) ||
                                     assetPath.Contains("/FormalItemSemanticIcons16/", StringComparison.Ordinal) ||
                                     assetPath.Contains("/FormalEquipmentSlotIcons16/", StringComparison.Ordinal);
            bool resourceMetricIcon = assetPath.Contains("/FormalResourceIcons12/", StringComparison.Ordinal);
            importer.spritePixelsPerUnit = resourceMetricIcon ? 12f : semanticMicroIcon || unitBar16 ? 16f : 32f;
            if (resourceBar32 || unitBar16)
            {
                string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                Rect rect;
                Vector4 border;
                if (resourceBar32 && file == "health")
                {
                    rect = new Rect(2f, 1f, 27f, 3f);
                    border = new Vector4(1f, 1f, 1f, 1f);
                }
                else if (resourceBar32 && file == "mana")
                {
                    rect = new Rect(10f, 1f, 11f, 11f);
                    border = new Vector4(1f, 1f, 1f, 1f);
                }
                else if (resourceBar32 && file == "shield")
                {
                    rect = new Rect(3f, 1f, 25f, 24f);
                    border = new Vector4(2f, 2f, 2f, 2f);
                }
                else
                {
                    rect = new Rect(2f, 1f, 12f, 12f);
                    border = new Vector4(1f, 1f, 1f, 1f);
                }
                importer.spritesheet = new[]
                {
                    new SpriteMetaData
                    {
                        name = file + "_bar",
                        rect = rect,
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(.5f, .5f),
                        border = border
                    }
                };
            }
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            if (assetPath.Contains("FormalUISkin16/"))
            {
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                settings.spritePivot = new Vector2(.5f, .5f);
                settings.spriteBorder = new Vector4(4f, 4f, 4f, 4f);
            }
            else if (assetPath.Contains("FormalUnits64/") || assetPath.Contains("FormalEnemyAnimations64/"))
            {
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(.5f, .90625f); // logical X=32, Y=58 on a 64px cell
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
