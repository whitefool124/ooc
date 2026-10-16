using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using OCC.Combat;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OCC.Combat.EditorTools
{
    [InitializeOnLoad]
    public static class Hd2DContinuousBattleCapture
    {
        private const string Key="OCC.ContinuousBattleCapture20261012";
        private static string Family=>SessionState.GetString(Key+".family","sample");
        private static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-12_OCC战斗连续提升/"+(Family=="sample"?"live":"profiles/"+Family)));
        private static Type ViewType=>typeof(EditorWindow).Assembly.GetType("UnityEditor.PlayModeView");
        static Hd2DContinuousBattleCapture(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;}
        [MenuItem("OCC/Tools/Capture continuous battle material sample (no focus)")]
        public static void Begin()=>BeginFor("sample");
        [MenuItem("OCC/Tools/Capture battle profile/AcademyCourt (no focus)")] public static void Court()=>BeginFor("AcademyCourt");
        [MenuItem("OCC/Tools/Capture battle profile/Conservatory (no focus)")] public static void Conservatory()=>BeginFor("Conservatory");
        [MenuItem("OCC/Tools/Capture battle profile/StoneRoad (no focus)")] public static void Road()=>BeginFor("StoneRoad");
        [MenuItem("OCC/Tools/Capture battle profile/RuinSite (no focus)")] public static void Ruin()=>BeginFor("RuinSite");
        [MenuItem("OCC/Tools/Capture battle profile/InteriorHall (no focus)")] public static void Interior()=>BeginFor("InteriorHall");
        [MenuItem("OCC/Tools/Capture battle profile/AetherTower (no focus)")] public static void Tower()=>BeginFor("AetherTower");
        [MenuItem("OCC/Tools/Capture battle profile/Prism (no focus)")] public static void Prism()=>BeginFor("Prism");
        [MenuItem("OCC/Tools/Capture battle profile/Prism crystal candidate (no focus)")] public static void PrismCandidate()=>BeginFor("PrismCandidate");
        [MenuItem("OCC/Tools/Capture battle profile/Conservatory crystal candidate (no focus)")] public static void ConservatoryCandidate()=>BeginFor("ConservatoryCandidate");
        [MenuItem("OCC/Tools/Capture battle profile/Pressure crystal candidate (no focus)")] public static void PressureCandidate()=>BeginFor("PressureCandidate");
        [MenuItem("OCC/Tools/Capture battle profile/Pressure (no focus)")] public static void Pressure()=>BeginFor("Pressure");
        [MenuItem("OCC/Tools/Capture battle weather/Sunset (no focus)")] public static void Sunset()=>BeginFor("Sunset");
        [MenuItem("OCC/Tools/Capture battle weather/RainNight (no focus)")] public static void RainNight()=>BeginFor("RainNight");
        private static void BeginFor(string family)
        {
            if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets"||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)
                throw new InvalidOperationException("Main OCC idle Edit Mode required");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve dirty scene");
            if(SessionState.GetBool(Key,false))throw new InvalidOperationException("Live capture already active");
            SessionState.SetString(Key+".family",family);
            SessionState.SetBool(Key+".weatherApplied",false);
            SessionState.SetBool(Key+".review",Hd2DBattleCrystalCandidates.ReviewEnabled);
            SessionState.SetString(Key+".pipeline",UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?.name??"");
            var property=ViewType.GetProperty("enterPlayModeBehavior",BindingFlags.Public|BindingFlags.Instance);
            var open=ViewType.GetProperty("openWindowOnEnteringPlayMode",BindingFlags.Public|BindingFlags.Static);
            if(property==null||open==null)throw new InvalidOperationException("Unfocused Play API unavailable");
            var views=Resources.FindObjectsOfTypeAll(ViewType);
            SessionState.SetString(Key+".views",JsonConvert.SerializeObject(views.Select(v=>new ViewState{id=v.GetEntityId().ToString(),behavior=Convert.ToInt32(property.GetValue(v))}).ToArray()));
            SessionState.SetBool(Key+".open",(bool)open.GetValue(null));SessionState.SetBool(Key+".openHadKey",EditorPrefs.HasKey("OpenGameViewOnEnteringPlayMode"));
            foreach(var view in views)property.SetValue(view,Enum.Parse(property.PropertyType,"PlayUnfocused"));
            open.SetValue(null,false);
            Directory.CreateDirectory(Folder);
            SessionState.SetString(Key+".scene",SceneManager.GetActiveScene().path);SessionState.SetString(Key+".phase","enter");
            SessionState.SetBool(Key,true);SessionState.SetFloat(Key+".deadline",(float)EditorApplication.timeSinceStartup+75);
            EditorApplication.EnterPlaymode();
        }
        [Serializable] private sealed class ViewState{public string id;public int behavior;}
        private static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+".deadline",0))throw new TimeoutException("Single scene capture timeout");
                if(!Application.isPlaying)return;
                EditorApplication.QueuePlayerLoopUpdate();
                var host=UnityEngine.Object.FindAnyObjectByType<CombatPrototypeBootstrap>();if(host==null)return;
                string phase=SessionState.GetString(Key+".phase","");
                if(phase=="motion")
                {
                    if(Hd2DBattleMotionCapture.Step()){SessionState.SetString(Key+".phase","exit");EditorApplication.ExitPlaymode();}
                    return;
                }
                if(phase=="enter")
                {
                    Hd2DBattleCrystalCandidates.ReviewEnabled=Family.EndsWith("Candidate");
                    bool prism=Family=="Prism"||Family=="PrismCandidate"||Family=="Sunset"||Family=="RainNight";
                    bool pressure=Family=="PressureCandidate"||Family=="Pressure";
                    bool firstRun=Family=="Conservatory"||Family=="ConservatoryCandidate"||prism||pressure;
                    string firstNode=pressure?"X":prism?"B3":"B2";
                    string error=host.PreviewUiCaptureRun(!firstRun,firstRun?firstNode:null,seed:Family=="InteriorHall"?7022:7021);
                    if(error!=null)throw new InvalidOperationException(error);
                    if(firstRun)
                    {
                        string node=firstNode;
                        host.StartMapNodeCombat(node);SessionState.SetString(Key+".phase","render");
                        SessionState.SetFloat(Key+".readyAt",(float)EditorApplication.timeSinceStartup+6);return;
                    }
                    string selected=null;int nearest=int.MaxValue;
                    foreach(var node in host.CurrentMapRun.MapNodes.Where(n=>n.IsCombat))
                    {
                        if(!host.CurrentMapRun.TryGetEncounter(node.Id,out var encounter)||!FirstRegionLevelCatalog.TryFor(encounter.LevelId,out var level))continue;
                        if(Family!="sample")
                        {
                            if(firstRun&&node.Id==(Family=="Prism"?"B3":"B2")||Hd2DBattlefieldProfile.Resolve(level.Id).Environment.ToString()==Family)
                            {selected=node.Id;break;}
                            continue;
                        }
                        foreach(var terrain in level.Terrain)
                        {
                            bool paired=terrain.Kind==LevelTerrainKind.HeavyCover || terrain.Kind==LevelTerrainKind.LightCover &&
                                AcademyBattlefieldLayoutCatalog.CoverVariant(level.Id,terrain.Position,CoverType.Light)=="academy_light_book_crate_intact";
                            int distance=terrain.Position.ManhattanDistance(level.HeroSpawn);
                            if(paired&&distance<nearest){selected=node.Id;nearest=distance;}
                        }
                    }
                    if(selected==null)throw new InvalidOperationException("No actual encounter contains requested profile "+Family);
                    host.CurrentMapRun.PreviewUiCaptureNode(selected);host.StartMapNodeCombat(selected);SessionState.SetString(Key+".phase","render");
                    SessionState.SetFloat(Key+".readyAt",(float)EditorApplication.timeSinceStartup+6);return;
                }
                if(phase!="render"||EditorApplication.timeSinceStartup<SessionState.GetFloat(Key+".readyAt",0)||host.IsCombatEntryBlocking)return;
                var sample=UnityEngine.Object.FindAnyObjectByType<Hd2DBattlefieldSample>();if(sample==null||sample.RenderCamera==null||sample.RenderCamera.targetTexture==null)return;
                if((Family=="Sunset"||Family=="RainNight")&&!SessionState.GetBool(Key+".weatherApplied",false))
                {
                    Hd2DBattleWeatherPreview.Apply(sample,host,Family=="RainNight");
                    SessionState.SetBool(Key+".weatherApplied",true);
                    SessionState.SetFloat(Key+".readyAt",(float)EditorApplication.timeSinceStartup+6);return;
                }
                sample.RenderCamera.Render();sample.PumpLocalLightingReview();
                if(sample.LocalDiffuseUpdates==0)return;
                var source=sample.RenderCamera.targetTexture;
                var cameraData=sample.RenderCamera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                bool post=cameraData.renderPostProcessing;
                foreach(string variant in new[]{"full","no_local_lighting","no_contact","no_diffuse","no_post"})
                {
                sample.SetLocalLightingReview(variant!="no_local_lighting"&&variant!="no_contact",variant!="no_local_lighting"&&variant!="no_diffuse");
                cameraData.renderPostProcessing=variant!="no_post"&&post;
                sample.RenderCamera.Render();
                var active=RenderTexture.active;bool gamma=GL.sRGBWrite;
                var target=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
                var image=new Texture2D(source.width,source.height,TextureFormat.RGB24,false,false);
                try
                {
                    GL.sRGBWrite=QualitySettings.activeColorSpace==ColorSpace.Linear;Graphics.Blit(source,target);RenderTexture.active=target;
                    image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
                    File.WriteAllBytes(Path.Combine(Folder,variant+"_battle_rt.png"),image.EncodeToPNG());
                }
                finally{RenderTexture.active=active;GL.sRGBWrite=gamma;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(image);}
                }
                sample.SetLocalLightingReview(true,true);cameraData.renderPostProcessing=post;
                Hd2DBattleCanvasCapture.Capture(sample,1920,1080,Path.Combine(Folder,"full_output_offscreen_1920x1080.png"));
                Hd2DBattleCanvasCapture.Capture(sample,960,540,Path.Combine(Folder,"full_output_offscreen_960x540.png"));
                if(Family=="Sunset"||Family=="RainNight")Hd2DBattleWeatherPreview.WriteEvidence(sample,Folder,Family=="RainNight");
                Hd2DBattleLocalEvidence.Write(sample,Folder,Family.EndsWith("Candidate")||Family=="AcademyCourt"||Family=="Conservatory");
                var bound=sample.RenderCamera.transform.parent.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.gameObject.activeInHierarchy).Select(r=>r.sharedMaterial).Where(m=>m!=null).Distinct().ToArray();
                File.WriteAllText(Path.Combine(Folder,"capture.json"),JsonConvert.SerializeObject(new{
                    status="captured",source="Unity Editor Play Mode, real selected academy battlefield camera RenderTexture; isolated preview entry, not player build or normal-entry journey",
                    dataPath=Application.dataPath,level=host.CurrentLevelId,environment=Hd2DBattlefieldProfile.Resolve(host.CurrentLevelId).Environment.ToString(),play=Application.isPlaying,source.width,source.height,
                    reflectionCaptures=sample.ReflectionCaptures,sourceRadianceRange=sample.LocalCapturedRadianceRange,diffuseStatus=sample.LocalDiffuseStatus,diffuseUpdates=sample.LocalDiffuseUpdates,diffuseSurfaces=sample.LocalDiffuseSurfaces,
                    camera=sample.RenderCamera.name,postProcessing=sample.RenderCamera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>()?.renderPostProcessing,
                    mappedGroundMaterials=bound.Count(m=>m.IsKeywordEnabled("_OCC_ROUGHNESS_MAP")),mappedPropMaterials=bound.Count(m=>m.IsKeywordEnabled("_OCC_PROP_DATA")),
                    mappedEnvironment=bound.Where(m=>m.IsKeywordEnabled("_NORMALMAP")&&m.GetTexture("_BumpMap")!=null&&UnityEditor.AssetDatabase.GetAssetPath(m.GetTexture("_BumpMap")).Contains("RemainingSprites/")).Select(m=>new{albedo=m.mainTexture?.name,shader=m.shader.name,normal=UnityEditor.AssetDatabase.GetAssetPath(m.GetTexture("_BumpMap")),surface=m.HasProperty("_MetallicGlossMap")?UnityEditor.AssetDatabase.GetAssetPath(m.GetTexture("_MetallicGlossMap")):null}).ToArray(),
                    mappedGroundAttachments=bound.Where(m=>m.IsKeywordEnabled("_OCC_GROUND_DATA")).Select(m=>new{albedo=m.mainTexture?.name,normal=m.GetTexture("_BumpMap")?.name,surface=m.GetTexture("_GroundSurfaceData")?.name}).ToArray(),
                    mappedProps=bound.Where(m=>m.IsKeywordEnabled("_OCC_PROP_DATA")).Select(m=>new{albedo=m.mainTexture?.name,normal=m.GetTexture("_BumpMap")?.name,roughness=m.GetTexture("_RoughnessMap")?.name}).ToArray(),
                    mappedFaces=sample.RenderCamera.transform.parent.GetComponentsInChildren<MeshRenderer>(true)
                        .Where(r=>r.gameObject.activeInHierarchy&&r.sharedMaterial!=null&&r.sharedMaterial.IsKeywordEnabled("_OCC_PROP_DATA"))
                        .Select(r=>new{objectName=r.name,albedo=r.sharedMaterial.mainTexture?.name,
                            cameraFacingDot=Vector3.Dot(r.transform.TransformDirection(r.GetComponent<MeshFilter>().sharedMesh.normals[0]),(sample.RenderCamera.transform.position-r.bounds.center).normalized)}).ToArray(),
                    noWindowCreated=true,focusRequested=false,uiExcluded=true},Formatting.Indented));
                if(Family.EndsWith("Candidate")||Family=="Prism"||Family=="Pressure")Hd2DBattleLocalEvidence.CaptureCrystalStateFixture(sample,host,Folder);
                if(Enum.TryParse<Hd2DEnvironmentKind>(Family,out _))
                {Hd2DBattleMotionCapture.Begin(sample,Folder);SessionState.SetString(Key+".phase","motion");return;}
                SessionState.SetString(Key+".phase","exit");EditorApplication.ExitPlaymode();
            }
            catch(Exception e)
            {
                File.WriteAllText(Path.Combine(Folder,"failure.json"),JsonConvert.SerializeObject(new{error=e.ToString()},Formatting.Indented));
                SessionState.SetString(Key+".phase","exit");if(EditorApplication.isPlayingOrWillChangePlaymode)EditorApplication.ExitPlaymode();else Restore();
            }
        }
        private static void Changed(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Key,false))Restore();}
        private static void Restore()
        {
            Hd2DBattleMotionCapture.Restore();
            var property=ViewType.GetProperty("enterPlayModeBehavior",BindingFlags.Public|BindingFlags.Instance);
            var states=JsonConvert.DeserializeObject<ViewState[]>(SessionState.GetString(Key+".views","[]"));
            foreach(var view in Resources.FindObjectsOfTypeAll(ViewType))
            {var old=states.FirstOrDefault(s=>s.id==view.GetEntityId().ToString());if(old!=null)property.SetValue(view,Enum.ToObject(property.PropertyType,old.behavior));}
            ViewType.GetProperty("openWindowOnEnteringPlayMode",BindingFlags.Public|BindingFlags.Static).SetValue(null,SessionState.GetBool(Key+".open",true));
            if(!SessionState.GetBool(Key+".openHadKey",true))EditorPrefs.DeleteKey("OpenGameViewOnEnteringPlayMode");
            Hd2DBattleCrystalCandidates.ReviewEnabled=SessionState.GetBool(Key+".review",false);
            File.WriteAllText(Path.Combine(Folder,"cleanup.json"),JsonConvert.SerializeObject(new{play=Application.isPlaying,dirty=SceneManager.GetActiveScene().isDirty,scenePreserved=SceneManager.GetActiveScene().path==SessionState.GetString(Key+".scene",""),pipelineRestored=(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?.name??"")==SessionState.GetString(Key+".pipeline",""),windowPreferencesRestored=true},Formatting.Indented));
            SessionState.SetBool(Key,false);
        }
    }
}
