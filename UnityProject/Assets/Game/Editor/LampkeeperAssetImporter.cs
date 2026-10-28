using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using OCC.Combat.Presentation;

namespace OCC.Combat.EditorTools
{
    public static class LampkeeperAssetImporter
    {
        public static void UpdateOwnedView()
        {
            if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Wrong project");
            for(int i=0;i<SceneManager.sceneCount;i++)
                if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Dirty scene must be preserved.");
            const string prefab="Assets/Game/Resources/UI/HD2D/lampkeeper.prefab";
            File.Copy(prefab,Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-21_守灯人赠礼实现/lampkeeper_before_readability.prefab")),false);
            var view=LampkeeperGiftView.CreateTemplate();
            try { if(PrefabUtility.SaveAsPrefabAsset(view,prefab)==null)throw new InvalidOperationException("Cannot save owned gift view"); }
            finally { UnityEngine.Object.DestroyImmediate(view); }
            EditorApplication.Exit(0);
        }
        public static void Build()
        {
            if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Wrong project");
            for(int i=0;i<SceneManager.sceneCount;i++)
            {
                var scene=SceneManager.GetSceneAt(i);Debug.Log("Lampkeeper preflight: "+scene.path+" dirty="+scene.isDirty);
                if(scene.isDirty)throw new InvalidOperationException("Dirty scene must be preserved.");
            }
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
            string destination="Assets/Game/Resources/Art/Lampkeeper";
            Directory.CreateDirectory(Path.Combine(root,"UnityProject",destination));
            foreach(var id in new[]{"keeper","LK-01","LK-02","LK-03","LK-04","LK-05","LK-06"})
            {
                string target=destination+"/"+id+".png";
                string full=Path.Combine(root,"UnityProject",target);
                if(File.Exists(full))throw new InvalidOperationException("Candidate already exists; inspect before replacing: "+target);
                File.Copy(Path.Combine(root,"ArtSource/Lampkeeper_20261021/delivery",id+".png"),full);
                AssetDatabase.ImportAsset(target,ImportAssetOptions.ForceSynchronousImport);
                var importer=AssetImporter.GetAtPath(target) as TextureImporter;
                if(importer==null)throw new InvalidOperationException("No importer "+target);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;
                importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(target);
                if(sprite==null)throw new InvalidOperationException("No sprite "+target);
            }
            const string prefab="Assets/Game/Resources/UI/HD2D/lampkeeper.prefab";
            if(AssetDatabase.LoadAssetAtPath<GameObject>(prefab)!=null)throw new InvalidOperationException("Gift prefab already exists; inspect before replacing.");
            var view=LampkeeperGiftView.CreateTemplate();
            try
            {
                var saved=PrefabUtility.SaveAsPrefabAsset(view,prefab);
                if(saved==null||saved.GetComponent<LampkeeperGiftView>()==null)throw new InvalidOperationException("Gift prefab save failed.");
            }
            finally{UnityEngine.Object.DestroyImmediate(view);}
            Debug.Log("Lampkeeper: seven processed QA_PENDING display sprites and new gift-view prefab imported. No scenes saved.");
            EditorApplication.Exit(0);
        }
    }
}
