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
            settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,2f/64);
            importer.SetTextureSettings(settings);
        }
    }
}
