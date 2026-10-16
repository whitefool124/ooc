using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OCC.Combat.EditorTools
{
    public static class StageOneArtworkAdoption
    {
        public static void Apply()
        {
            if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets"||EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Idle main OCC required");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve dirty scenes");
            const string art="Assets/Game/Resources/Art/";
            var records=new List<object>();
            foreach(var pair in new[]{new[]{"EnemyUnitCandidates32x64","FormalUnits64"},new[]{"FieldEffectCandidates32","FormalBattlefieldFields32"},new[]{"DeviceMaterialCandidates32","FormalBattlefieldDevices32"}})
            {
                string targetFolder=art+pair[1];if(!AssetDatabase.IsValidFolder(targetFolder))AssetDatabase.CreateFolder(art.TrimEnd('/'),pair[1]);
                foreach(string source in Directory.GetFiles(art+pair[0],"*.png"))
                {
                    string from=source.Replace('\\','/'),to=targetFolder+"/"+Path.GetFileName(source),guid=AssetDatabase.AssetPathToGUID(from);
                    if(!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(to)))throw new InvalidOperationException("Destination already exists: "+to);
                    string error=AssetDatabase.MoveAsset(from,to);if(!string.IsNullOrEmpty(error))throw new InvalidOperationException(error);
                    AssetDatabase.ImportAsset(to,ImportAssetOptions.ForceSynchronousImport);
                    if(AssetDatabase.AssetPathToGUID(to)!=guid)throw new InvalidOperationException("GUID changed: "+to);
                    var importer=(TextureImporter)AssetImporter.GetAtPath(to);var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(to);
                    if(sprite==null||sprite.pixelsPerUnit!=32||importer.filterMode!=FilterMode.Point||importer.mipmapEnabled||importer.textureCompression!=TextureImporterCompression.Uncompressed)throw new InvalidOperationException("Adopted importer invalid: "+to);
                    records.Add(new{source=from,target=to,guid,width=sprite.texture.width,height=sprite.texture.height,pivot=new[]{sprite.pivot.x,sprite.pivot.y}});
                }
            }
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-16_第一阶段待办清空"));Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"adopted_assets.json"),JsonConvert.SerializeObject(records,Formatting.Indented));
        }
    }
}
