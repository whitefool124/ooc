using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace OCC.Combat.EditorTools
{
    public static class ApprovedHotDrinkImport
    {
        [MenuItem("OCC/VFX/Hot Drink/Import Approved Cup")]
        public static void Import()
        {
            if(Application.isPlaying || Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Main project Edit required");
            const string path="Assets/Game/Resources/Art/FormalMedicalDetails/hot_drink.png";
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new InvalidOperationException("Missing cup importer");
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=32;
            importer.filterMode=FilterMode.Point;importer.wrapMode=TextureWrapMode.Clamp;importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.isReadable=true;importer.alphaIsTransparency=true;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-03_背景植物样板与热饮入库"));
            File.WriteAllText(Path.Combine(folder,"cup-import.json"),"{\"guid\":\""+AssetDatabase.AssetPathToGUID(path)+"\",\"point\":"+(importer.filterMode==FilterMode.Point?"true":"false")+",\"readable\":"+(importer.isReadable?"true":"false")+",\"ppu\":"+importer.spritePixelsPerUnit+",\"compression\":\""+importer.textureCompression+"\",\"mipmaps\":"+(importer.mipmapEnabled?"true":"false")+"}");
        }
    }
}
