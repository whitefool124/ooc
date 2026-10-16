using System;
using UnityEditor;
using UnityEngine;

namespace OCC.Combat.EditorTools
{
    public sealed class EnemyUnitCandidateImporter:AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/Game/Resources/Art/EnemyUnitCandidates32x64/",StringComparison.Ordinal))return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=32;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteMeshType=SpriteMeshType.FullRect;
            importer.GetSourceTextureWidthAndHeight(out int width,out int height);
            if(width%32!=0||height%32!=0||width>64||height>64||width<32||height<32)
                throw new InvalidOperationException("Invalid native candidate canvas: "+assetPath);
            settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,2f/height);
            importer.SetTextureSettings(settings);
        }
    }
}
