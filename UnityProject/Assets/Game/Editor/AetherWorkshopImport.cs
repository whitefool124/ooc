using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OCC.Combat.EditorTools
{
    public static class AetherWorkshopImport
    {
        public static void Apply()
        {
            string path=Application.dataPath.Replace('\\','/');
            if(path!="E:/数据库/OCC_Codex/UnityProject/Assets"&&path!="D:/occart/OCC_latest/UnityProject/Assets")
                throw new InvalidOperationException("Wrong main-project dataPath: "+path);
            for(int i=0;i<SceneManager.sceneCount;i++)
            {
                var scene=SceneManager.GetSceneAt(i);
                Debug.Log("AETHER_WORKSHOP_PREFLIGHT dataPath="+path+" scene="+scene.path+" dirty="+scene.isDirty+" active="+(scene==SceneManager.GetActiveScene()));
                if(scene.isDirty)throw new InvalidOperationException("Unsaved scene: "+scene.path);
            }
            Import("Folio",9);Import("Canvas",12);Import("Dossier",5);Import("Socket",4);Import("Clamp",0);
            Import("ToolRoll",new Vector4(12,3,12,3));
            Import("EmptySocket",3);
        }
        private static void Import(string name,int border)=>Import(name,Vector4.one*border);
        private static void Import(string name,Vector4 border)
        {
            string path="Assets/Game/Resources/Art/AcademyWorkshop20261023/"+name+".png";
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new FileNotFoundException("Workshop delivery missing",path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=32;importer.spriteBorder=border;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;
            importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(sprite==null||sprite.pixelsPerUnit!=32||sprite.border!=border)
                throw new InvalidOperationException("Workshop importer readback failed: "+path);
            Debug.Log("AETHER_WORKSHOP_IMPORT "+name+" guid="+AssetDatabase.AssetPathToGUID(path)+" size="+sprite.rect.size+" ppu="+sprite.pixelsPerUnit);
        }
    }
}
