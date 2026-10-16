using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OCC.Combat.EditorTools
{
    // Small compiled commands avoid Roslyn eval and never create/focus windows or enter Play Mode.
    public static class Hd2DLocalIntegrationTools
    {
        private static void Guard()
        {
            if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")
                throw new InvalidOperationException("Main OCC project required");
            if(Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Idle Edit Mode required");
            for(int i=0;i<SceneManager.sceneCount;i++)
                if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Dirty scene: preserve user changes");
        }
        [MenuItem("OCC/Tools/Audit HD2D integration (no focus)")]
        public static void AuditIntegration()
        {
            Guard();
            const string reviewKey="OCC.Hd2D.CoverageJourney";
            // Disarm the interrupted review callback; never resume it on the next Play entry.
            SessionState.SetBool(reviewKey,false);SessionState.SetBool(reviewKey+".greenhouse",false);
            SessionState.SetString(reviewKey+".phase","cancelled");
            var reviewTick=typeof(Hd2DBattlefieldCoverageAudit).GetMethod("ReviewTick",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            if(reviewTick!=null)EditorApplication.update-=(EditorApplication.CallbackFunction)Delegate.CreateDelegate(typeof(EditorApplication.CallbackFunction),reviewTick);
            const string asset="Assets/Game/Resources/Art/HD2DAcademyCandidates/greenhouse_facade.png";
            var importer=AssetImporter.GetAtPath(asset) as TextureImporter;
            if(importer==null)throw new InvalidOperationException("Decoded greenhouse candidate is not imported");
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;
            importer.spritePixelsPerUnit=32;
            importer.filterMode=FilterMode.Point;importer.wrapMode=TextureWrapMode.Clamp;
            importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(asset);
            if(texture==null || texture.width!=256 || texture.height!=96)throw new InvalidOperationException("Invalid greenhouse delivery");
            var levels=Hd2DBattlefieldProfile.Levels.Select(level=>
            {
                var p=Hd2DBattlefieldProfile.Resolve(level.Id);
                var masonry=Resources.Load<Texture2D>(p.MasonryResource);
                if(masonry==null || masonry.width!=32 || masonry.height!=32 || masonry.filterMode!=FilterMode.Point)
                    throw new InvalidOperationException("Invalid native masonry: "+level.Id);
                return new{level.Id,level.DisplayName,environment=p.Environment.ToString(),p.MasonryResource,p.WaterGarden,p.Trees};
            }).ToArray();
            var shaders=new[]{"OCC/HD2D/BakedArchitecture","OCC/HD2D/WetCourtyard","OCC/HD2D/GroundedSprite"}.Select(name=>
            {
                var shader=Shader.Find(name);
                if(shader==null || !shader.isSupported)throw new InvalidOperationException("Unsupported shader: "+name);
                return new{name,messages=ShaderUtil.GetShaderMessages(shader).Select(m=>new{m.message,severity=m.severity.ToString()}).ToArray()};
            }).ToArray();
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-12_OCC场景类型全量接入"));
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"integration_audit.json"),JsonConvert.SerializeObject(new{
                scope="profile routing/import/shader checks only; no Play Mode or journey",dataPath=Application.dataPath,
                scene=SceneManager.GetActiveScene().path,dirty=SceneManager.GetActiveScene().isDirty,
                candidate=new{asset,guid=AssetDatabase.AssetPathToGUID(asset),texture.width,texture.height,filter=texture.filterMode.ToString(),status="QA_PENDING"},levels,shaders,
                reviewDisabled=!SessionState.GetBool(reviewKey,false)},Formatting.Indented));
        }
    }
}
