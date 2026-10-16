using System;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OCC.Combat.EditorTools
{
    public static class BreachRamResourceAdoption
    {
        private const string Source="Assets/Game/Resources/Art/EnemyUnitCandidates32x64/breach_ram.png";
        private const string Target="Assets/Game/Resources/Art/FormalUnits64/breach_ram.png";
        private const string ExpectedGuid="b05755e06f213d24686d0c5345b35505";
        public static void Apply()
        {
            if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets"||EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Idle main project required");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve dirty scene");
            if(AssetDatabase.AssetPathToGUID(Target)!=ExpectedGuid)
            {
                if(AssetDatabase.AssetPathToGUID(Source)!=ExpectedGuid||!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(Target)))throw new InvalidOperationException("Unexpected resource GUID or destination");
                string error=AssetDatabase.MoveAsset(Source,Target);if(!string.IsNullOrEmpty(error))throw new InvalidOperationException(error);
            }
            AssetDatabase.ImportAsset(Target,ImportAssetOptions.ForceSynchronousImport);
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Target);
            var importer=(TextureImporter)AssetImporter.GetAtPath(Target);
            if(sprite==null||sprite.pixelsPerUnit!=32||Mathf.Abs(sprite.pivot.y-2)>.001||sprite.texture.width!=64||importer.filterMode!=FilterMode.Point||importer.mipmapEnabled||importer.textureCompression!=TextureImporterCompression.Uncompressed)
                throw new InvalidOperationException("Formal importer mismatch");
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-16_楔角正式接入与剩余待办"));Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"adoption.json"),JsonConvert.SerializeObject(new{source=Source,target=Target,guid=AssetDatabase.AssetPathToGUID(Target),preservedGuid=true,sprite.pixelsPerUnit,width=sprite.texture.width,height=sprite.texture.height,pivot=new[]{sprite.pivot.x,sprite.pivot.y},filter=importer.filterMode.ToString(),compression=importer.textureCompression.ToString(),importer.mipmapEnabled,sceneSaved=false,authorization="User explicitly approved this exact sprite and requested formal resource adoption on2026-10-16"},Formatting.Indented));
        }
    }
}
