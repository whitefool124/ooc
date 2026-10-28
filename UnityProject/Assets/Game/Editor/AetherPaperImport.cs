using System;
using UnityEditor;
using UnityEngine;

namespace OCC.Combat.EditorTools
{
    public static class AetherPaperImport
    {
        public static void Apply()
        {
            Import("Assets/Game/Resources/Art/AetherPaper/ThinPaper.png",4);
            Import("Assets/Game/Resources/Art/AetherPaper/SquareButton.png",2);
        }
        private static void Import(string path,int edge)
        {
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new InvalidOperationException("Thin paper delivery missing.");
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=32;importer.spriteBorder=Vector4.one*edge;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
            importer.filterMode=FilterMode.Point;importer.wrapMode=TextureWrapMode.Clamp;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(sprite==null||sprite.pixelsPerUnit!=32||sprite.border!=Vector4.one*edge)
                throw new InvalidOperationException("Thin paper importer readback failed.");
        }
    }
}
