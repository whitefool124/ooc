using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OCC.Combat.EditorTools
{
    // Six representative runtime stills, not the legacy QA/fixture capture suite.
    [InitializeOnLoad]
    public static class Hd2DManualFamilyStills
    {
        private const string Key="OCC.SixMaterialFamilyStills20261013";
        private static readonly string[] Families={"AcademyCourt","Conservatory","StoneRoad","RuinSite","InteriorHall","AetherTower"};
        private static string Root=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/"+
            ((SessionState.GetString(Key+".set","")=="light_hierarchy_families"||SessionState.GetString(Key+".set","")=="environment_finish_families")?"2026-10-14_OCC整体光影/":"2026-10-13_OCC战场层次提升/")+SessionState.GetString(Key+".set","material_families_v2")));
        static Hd2DManualFamilyStills(){EditorApplication.update+=Tick;}
        [MenuItem("OCC/Tools/Manual screenshots/Six material families")]
        public static void Begin()=>BeginSet("material_families_v2");
        [MenuItem("OCC/Tools/Manual screenshots/Six surface variety families")]
        public static void SurfaceVariety()=>BeginSet("surface_variety_families");
        [MenuItem("OCC/Tools/Manual screenshots/Six air depth families")]
        public static void AirDepth()=>BeginSet("air_depth_families");
        [MenuItem("OCC/Tools/Manual screenshots/Six surface zone families")]
        public static void SurfaceZones()=>BeginSet("surface_zone_families");
        [MenuItem("OCC/Tools/Manual screenshots/Six spatial frame families")]
        public static void SpatialFrames()=>BeginSet("spatial_frame_families");
        [MenuItem("OCC/Tools/Manual screenshots/Six native period families")]
        public static void NativePeriods()=>BeginSet("native_period_families");
        [MenuItem("OCC/Tools/Manual screenshots/Six contact shadow families")]
        public static void ContactShadows()=>BeginSet("contact_shadow_families");
        [MenuItem("OCC/Tools/Manual screenshots/Six light hierarchy families")]
        public static void LightHierarchy()=>BeginSet("light_hierarchy_families");
        [MenuItem("OCC/Tools/Manual screenshots/Six environment finish families")]
        public static void EnvironmentFinish()=>BeginSet("environment_finish_families");
        private static void BeginSet(string set)
        {
            if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets"||!Application.isBatchMode||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||SessionState.GetBool(Key,false))
                throw new InvalidOperationException("Idle main OCC background Editor required");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve dirty scene");
            SessionState.SetString(Key+".set",set);
            EditorSceneManager.OpenScene("Assets/Scenes/CombatPrototype.unity");Directory.CreateDirectory(Root);
            SessionState.SetInt(Key+".index",0);SessionState.SetInt(Key+".phase",0);
            SessionState.SetFloat(Key+".deadline",(float)EditorApplication.timeSinceStartup+150);
            SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
        }
        private static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||EditorApplication.isCompiling||EditorApplication.isUpdating||!Application.isPlaying)return;
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+".deadline",0))throw new TimeoutException("Six requested family stills timed out");
                EditorApplication.QueuePlayerLoopUpdate();
                var host=UnityEngine.Object.FindAnyObjectByType<CombatPrototypeBootstrap>();if(host==null)return;
                int index=SessionState.GetInt(Key+".index",0),phase=SessionState.GetInt(Key+".phase",0);string family=Families[index];
                if(phase==0)
                {
                    bool first=family=="AcademyCourt"||family=="Conservatory";string firstNode=family=="AcademyCourt"?"B3":"B2";
                    string error=host.PreviewUiCaptureRun(!first,first?firstNode:null,seed:family=="InteriorHall"?7022:7021);
                    if(error!=null)throw new InvalidOperationException(error);
                    string selected=first?firstNode:null;
                    if(!first)
                    {
                        foreach(var node in host.CurrentMapRun.MapNodes.Where(n=>n.IsCombat))
                            if(host.CurrentMapRun.TryGetEncounter(node.Id,out var encounter)&&FirstRegionLevelCatalog.TryFor(encounter.LevelId,out _)&&Hd2DBattlefieldProfile.Resolve(encounter.LevelId).Environment.ToString()==family)
                            {selected=node.Id;break;}
                        if(selected==null)throw new InvalidOperationException("Actual map has no requested material family: "+family);
                        host.CurrentMapRun.PreviewUiCaptureNode(selected);
                    }
                    host.StartMapNodeCombat(selected);SessionState.SetInt(Key+".phase",1);return;
                }
                if(host.IsCombatEntryBlocking)return;
                if(phase==1)
                {SessionState.SetFloat(Key+".ready",Time.unscaledTime+4);SessionState.SetInt(Key+".phase",2);return;}
                if(Time.unscaledTime<SessionState.GetFloat(Key+".ready",0))return;
                var sample=UnityEngine.Object.FindAnyObjectByType<Hd2DBattlefieldSample>();if(sample==null)return;
                if(Hd2DBattlefieldProfile.Resolve(host.CurrentLevelId).Environment.ToString()!=family)throw new InvalidOperationException("Rendered family mismatch: "+family);
                sample.RenderCamera.Render();sample.PumpLocalLightingReview();if(sample.LocalDiffuseUpdates==0)return;
                string folder=Path.Combine(Root,family);Directory.CreateDirectory(folder);
                Hd2DBattleCanvasCapture.Capture(sample,1920,1080,Path.Combine(folder,"day.png"));
                var renderers=sample.RenderCamera.transform.parent.GetComponentsInChildren<MeshRenderer>(true);
                File.WriteAllText(Path.Combine(folder,"capture.json"),JsonConvert.SerializeObject(new{
                    status="CAPTURED_FOR_MANUAL_REVIEW",dataPath=Application.dataPath,level=host.CurrentLevelId,environment=family,
                    source="Unity Editor Play Mode runtime battlefield/full HUD; isolated preview, not standalone Player or full journey acceptance",
                    resolution=new[]{1920,1080},focusRequested=false,suitesRun=false,humanReview="PENDING",
                    diffuseUpdates=sample.LocalDiffuseUpdates,diffuseSurfaces=sample.LocalDiffuseSurfaces,
                    lights=sample.RenderCamera.transform.parent.GetComponentsInChildren<Light>().Select(l=>new{name=l.name,intensity=l.intensity}).ToArray(),
                    shaders=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Select(m=>m.shader.name).Distinct().ToArray(),
                    gardenSurfaceVariants=sample.RenderCamera.transform.parent.GetComponentInChildren<Hd2DAcademyEnvironment>().GardenSurfaceVariants,
                    peripheralSurfaceVariants=renderers.Where(r=>r.name=="外围连续铺地").Select(r=>r.sharedMaterials.Length-sample.RenderCamera.transform.parent.GetComponentInChildren<Hd2DAcademyEnvironment>().GardenSurfaceVariants).FirstOrDefault(),
                    foliageMaterials=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null&&m.IsKeywordEnabled("_OCC_FOLIAGE_DATA")).Distinct().Count()
                },Formatting.Indented));
                if(index==Families.Length-1)
                {SessionState.SetBool(Key,false);EditorApplication.ExitPlaymode();return;}
                SessionState.SetInt(Key+".index",index+1);SessionState.SetInt(Key+".phase",0);
            }
            catch(Exception e)
            {
                File.WriteAllText(Path.Combine(Root,"failure.json"),JsonConvert.SerializeObject(new{family=Families[SessionState.GetInt(Key+".index",0)],error=e.ToString()},Formatting.Indented));
                SessionState.SetBool(Key,false);if(EditorApplication.isPlayingOrWillChangePlaymode)EditorApplication.ExitPlaymode();Debug.LogException(e);
            }
        }
    }
}
