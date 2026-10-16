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
    public static class Hd2DLiveAcademyCoverCapture
    {
        private const string Key="OCC.LiveAcademyCoverCapture20261012";
        private static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-12_OCC实际掩体材质接入/academy_live"));
        private static Type ViewType=>typeof(EditorWindow).Assembly.GetType("UnityEditor.PlayModeView");
        static Hd2DLiveAcademyCoverCapture(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;}
        [MenuItem("OCC/Tools/Capture live mapped academy covers (no focus)")]
        public static void Begin()
        {
            if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets"||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)
                throw new InvalidOperationException("Main OCC idle Edit Mode required");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve dirty scene");
            if(SessionState.GetBool(Key,false))throw new InvalidOperationException("Live capture already active");
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
                if(phase=="enter")
                {
                    string error=host.PreviewUiCaptureRun(true,null);if(error!=null)throw new InvalidOperationException(error);
                    string selected=null;int nearest=int.MaxValue;
                    foreach(var node in host.CurrentMapRun.MapNodes.Where(n=>n.IsCombat))
                    {
                        if(!host.CurrentMapRun.TryGetEncounter(node.Id,out var encounter)||!FirstRegionLevelCatalog.TryFor(encounter.LevelId,out var level))continue;
                        foreach(var terrain in level.Terrain)
                        {
                            bool paired=terrain.Kind==LevelTerrainKind.HeavyCover || terrain.Kind==LevelTerrainKind.LightCover &&
                                AcademyBattlefieldLayoutCatalog.CoverVariant(level.Id,terrain.Position,CoverType.Light)=="academy_light_book_crate_intact";
                            int distance=terrain.Position.ManhattanDistance(level.HeroSpawn);
                            if(paired&&distance<nearest){selected=node.Id;nearest=distance;}
                        }
                    }
                    if(selected==null)throw new InvalidOperationException("No actual academy encounter contains the paired cover assets");
                    host.CurrentMapRun.PreviewUiCaptureNode(selected);host.StartMapNodeCombat(selected);SessionState.SetString(Key+".phase","render");
                    SessionState.SetFloat(Key+".readyAt",(float)EditorApplication.timeSinceStartup+6);return;
                }
                if(phase!="render"||EditorApplication.timeSinceStartup<SessionState.GetFloat(Key+".readyAt",0)||host.IsCombatEntryBlocking)return;
                var sample=UnityEngine.Object.FindAnyObjectByType<Hd2DBattlefieldSample>();if(sample==null||sample.RenderCamera==null||sample.RenderCamera.targetTexture==null)return;
                sample.RenderCamera.Render();var source=sample.RenderCamera.targetTexture;
                var active=RenderTexture.active;bool gamma=GL.sRGBWrite;
                var target=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
                var image=new Texture2D(source.width,source.height,TextureFormat.RGB24,false,false);
                try
                {
                    GL.sRGBWrite=QualitySettings.activeColorSpace==ColorSpace.Linear;Graphics.Blit(source,target);RenderTexture.active=target;
                    image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
                    File.WriteAllBytes(Path.Combine(Folder,"academy_covers_live.png"),image.EncodeToPNG());
                }
                finally{RenderTexture.active=active;GL.sRGBWrite=gamma;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(image);}
                var bound=sample.RenderCamera.transform.parent.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.gameObject.activeInHierarchy).Select(r=>r.sharedMaterial).Where(m=>m!=null).Distinct().ToArray();
                File.WriteAllText(Path.Combine(Folder,"capture.json"),JsonConvert.SerializeObject(new{
                    status="captured",source="Unity Editor Play Mode, real selected academy battlefield camera RenderTexture; isolated preview entry, not player build or normal-entry journey",
                    dataPath=Application.dataPath,level=host.CurrentLevelId,play=Application.isPlaying,source.width,source.height,
                    camera=sample.RenderCamera.name,postProcessing=sample.RenderCamera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>()?.renderPostProcessing,
                    mappedGroundMaterials=bound.Count(m=>m.IsKeywordEnabled("_OCC_ROUGHNESS_MAP")),mappedPropMaterials=bound.Count(m=>m.IsKeywordEnabled("_OCC_PROP_DATA")),
                    mappedProps=bound.Where(m=>m.IsKeywordEnabled("_OCC_PROP_DATA")).Select(m=>new{albedo=m.mainTexture?.name,normal=m.GetTexture("_BumpMap")?.name,roughness=m.GetTexture("_RoughnessMap")?.name}).ToArray(),
                    mappedFaces=sample.RenderCamera.transform.parent.GetComponentsInChildren<MeshRenderer>(true)
                        .Where(r=>r.gameObject.activeInHierarchy&&r.sharedMaterial!=null&&r.sharedMaterial.IsKeywordEnabled("_OCC_PROP_DATA"))
                        .Select(r=>new{objectName=r.name,albedo=r.sharedMaterial.mainTexture?.name,
                            cameraFacingDot=Vector3.Dot(r.transform.TransformDirection(r.GetComponent<MeshFilter>().sharedMesh.normals[0]),(sample.RenderCamera.transform.position-r.bounds.center).normalized)}).ToArray(),
                    noWindowCreated=true,focusRequested=false,uiExcluded=true},Formatting.Indented));
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
            var property=ViewType.GetProperty("enterPlayModeBehavior",BindingFlags.Public|BindingFlags.Instance);
            var states=JsonConvert.DeserializeObject<ViewState[]>(SessionState.GetString(Key+".views","[]"));
            foreach(var view in Resources.FindObjectsOfTypeAll(ViewType))
            {var old=states.FirstOrDefault(s=>s.id==view.GetEntityId().ToString());if(old!=null)property.SetValue(view,Enum.ToObject(property.PropertyType,old.behavior));}
            ViewType.GetProperty("openWindowOnEnteringPlayMode",BindingFlags.Public|BindingFlags.Static).SetValue(null,SessionState.GetBool(Key+".open",true));
            if(!SessionState.GetBool(Key+".openHadKey",true))EditorPrefs.DeleteKey("OpenGameViewOnEnteringPlayMode");
            File.WriteAllText(Path.Combine(Folder,"cleanup.json"),JsonConvert.SerializeObject(new{play=Application.isPlaying,dirty=SceneManager.GetActiveScene().isDirty,scenePreserved=SceneManager.GetActiveScene().path==SessionState.GetString(Key+".scene",""),windowPreferencesRestored=true},Formatting.Indented));
            SessionState.SetBool(Key,false);
        }
    }
}
