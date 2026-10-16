using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OCC.Combat.EditorTools
{
    [InitializeOnLoad]
    public static class Hd2DBattlefieldCoverageAudit
    {
        private const string ReviewKey="OCC.Hd2D.CoverageJourney";
        private static double nextRecord;
        static Hd2DBattlefieldCoverageAudit()
        {
            if(SessionState.GetBool(ReviewKey,false))EditorApplication.update+=ReviewTick;
        }
        public static void StartReviewSession()
        {
            if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")
                throw new InvalidOperationException("Wrong project");
            var scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).ToArray();
            if(scenes.Any(s=>s.isDirty))throw new InvalidOperationException("Dirty scene: stop review startup");
            // Session-only recovery for two concurrent Editors. Preserve authentication,
            // settings assets, and the other project's listener.
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Unity.Pipeline.Editor.PipelineServerStartup")).FirstOrDefault(t=>t!=null);
            var server=type?.GetProperty("Server",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public)?.GetValue(null);
            if(server==null)throw new InvalidOperationException("Pipeline server not loaded");
            server.GetType().GetMethod("Stop").Invoke(server,null);
            server.GetType().GetMethod("Start").Invoke(server,new object[]{7801});
            var tickType=type.Assembly.GetType("Unity.Pipeline.Editor.Commands.AutoTickCommand");
            tickType.GetMethod("SetAutoTick",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public)
                .Invoke(null,new object[]{true,16,true});
            Debug.Log("OCC review session ready; main project verified; clean scenes; Pipeline 7801");
            Run();
            if(Environment.GetCommandLineArgs().Contains("-occ-full-review"))
            {
                var gameView=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                var window=EditorWindow.GetWindow(gameView);
                var size=gameView.GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
                SessionState.SetInt(ReviewKey+".size",(int)size.GetValue(window));
                bool small=Environment.GetCommandLineArgs().Contains("-occ-review-540");
                SessionState.SetBool(ReviewKey+".small",small);size.SetValue(window,ResolveGameViewSize(small?960:1920,small?540:1080));window.Repaint();
                SessionState.SetBool(ReviewKey,true);SessionState.SetString(ReviewKey+".phase","enter");
                SessionState.SetString(ReviewKey+".captured","");
                EditorApplication.update-=ReviewTick;EditorApplication.update+=ReviewTick;
            }
        }
        private static object GameViewSizeGroup()
        {
            const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.FlattenHierarchy;
            var assembly=typeof(EditorWindow).Assembly;
            var type=assembly.GetType("UnityEditor.GameViewSizes");
            var instance=type.GetProperty("instance",flags).GetValue(null);
            return type.GetMethod("GetGroup",flags).Invoke(instance,new[]{Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeGroupType"),"Standalone")});
        }
        private static int ResolveGameViewSize(int width,int height)
        {
            const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
            var group=GameViewSizeGroup();var type=group.GetType();
            int count=(int)type.GetMethod("GetTotalCount",flags).Invoke(group,null);
            SessionState.SetInt(ReviewKey+".temporarySize",-1);
            for(int i=0;i<count;i++)
            {
                var size=type.GetMethod("GetGameViewSize",flags).Invoke(group,new object[]{i});
                if((int)size.GetType().GetProperty("width",flags).GetValue(size)==width&&
                    (int)size.GetType().GetProperty("height",flags).GetValue(size)==height)return i;
            }
            var assembly=typeof(EditorWindow).Assembly;var kind=assembly.GetType("UnityEditor.GameViewSizeType");
            var sizeType=assembly.GetType("UnityEditor.GameViewSize");
            var created=sizeType.GetConstructor(flags,null,new[]{kind,typeof(int),typeof(int),typeof(string)},null)
                .Invoke(new object[]{Enum.Parse(kind,"FixedResolution"),width,height,"OCC temporary review"});
            type.GetMethod("AddCustomSize",flags).Invoke(group,new[]{created});
            int index=(int)type.GetMethod("GetTotalCount",flags).Invoke(group,null)-1;
            int builtin=(int)type.GetMethod("GetBuiltinCount",flags).Invoke(group,null);
            SessionState.SetInt(ReviewKey+".temporarySize",index-builtin);return index;
        }
        private static string ReviewFolder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../ReviewEvidence/20261012_hd2d_coverage",
            SessionState.GetBool(ReviewKey+".small",false)?"540":"1080"));
        private static void ReviewTick()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            string phase=SessionState.GetString(ReviewKey+".phase","");
            if(!Application.isPlaying)
            {
                if(phase=="enter") {SessionState.SetString(ReviewKey+".phase","start");EditorApplication.isPlaying=true;}
                else if(phase=="exit")
                {
                    File.WriteAllText(Path.Combine(ReviewFolder,"cleanup.json"),JsonConvert.SerializeObject(new{
                        play=Application.isPlaying,dirty=SceneManager.GetActiveScene().isDirty,
                        battleWorlds=UnityEngine.Object.FindObjectsByType<Hd2DBattlefieldSample>(FindObjectsSortMode.None).Length}));
                    var gameView=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                    gameView.GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic)
                        .SetValue(EditorWindow.GetWindow(gameView),SessionState.GetInt(ReviewKey+".size",3));
                    int temporary=SessionState.GetInt(ReviewKey+".temporarySize",-1);
                    if(temporary>=0)
                    {
                        var group=GameViewSizeGroup();
                        group.GetType().GetMethod("RemoveCustomSize",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic)
                            .Invoke(group,new object[]{temporary});
                    }
                    SessionState.SetBool(ReviewKey,false);EditorApplication.update-=ReviewTick;
                }
                return;
            }
            var host=UnityEngine.Object.FindAnyObjectByType<CombatPrototypeBootstrap>();if(host==null)return;
            Directory.CreateDirectory(ReviewFolder);
            if(phase=="start")
            {
                AcademyJourneyAudit.Begin("occ-full-coverage-20261012-"+Guid.NewGuid().ToString("N"));AcademyJourneyAudit.RunFullTutorial();
                SessionState.SetString(ReviewKey+".phase","running");
            }
            if(EditorApplication.timeSinceStartup<nextRecord)return;nextRecord=EditorApplication.timeSinceStartup+2;
            var sample=UnityEngine.Object.FindAnyObjectByType<Hd2DBattlefieldSample>();
            string node=host.CurrentMapRun?.CurrentNodeId;
            string captured=SessionState.GetString(ReviewKey+".captured","");
            if(sample!=null&&!host.IsCombatEntryBlocking&&!captured.Contains("|"+node+"|"))
            {
                int projected=sample.CheckCellRoundTrips();
                ScreenCapture.CaptureScreenshot(Path.Combine(ReviewFolder,node+".png"));
                File.WriteAllText(Path.Combine(ReviewFolder,node+".json"),JsonConvert.SerializeObject(new{
                    level=host.CurrentLevelId,node,projection=projected,width=Screen.width,height=Screen.height,
                    profile=Hd2DBattlefieldProfile.Resolve(host.CurrentLevelId).FloorTheme.ToString(),
                    localCrystalLights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Count(l=>l.name.StartsWith("蓄能晶簇局部光_")),
                    waterBodies=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Count(r=>r.sharedMaterial!=null&&r.sharedMaterial.shader.name=="OCC/HD2D/CourtyardWater")}));
                SessionState.SetString(ReviewKey+".captured",captured+"|"+node+"|");
            }
            File.WriteAllText(Path.Combine(ReviewFolder,"journey-status.json"),JsonConvert.SerializeObject(new{
                status=AcademyJourneyAudit.Status,node,level=host.CurrentLevelId,phase=host.CurrentFlowPhase.ToString(),saved=host.IsMapRunSaved}));
            if(AcademyJourneyAudit.Status.StartsWith("PASS:full-tutorial")||AcademyJourneyAudit.Status.StartsWith("FAIL:"))
            {SessionState.SetString(ReviewKey+".phase","exit");EditorApplication.isPlaying=false;}
        }
        // Catalog/asset checks only. Does not pretend to launch or visually approve every battle.
        public static string Run()
        {
            if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")
                throw new InvalidOperationException("Main OCC project required");
            var assets=new CombatFormalVisualAssets();assets.LoadRuntime();
            var rows=new List<object>();int failures=0;
            foreach(var level in Hd2DBattlefieldProfile.Levels)
            {
                var errors=new List<string>();var profile=Hd2DBattlefieldProfile.Resolve(level.Id);
                try
                {
                    var build=FirstRegionLevelBuilder.Build(level);
                    var presenter=new CombatBattlefieldCellPresenter(new BattlefieldPresentationAdapter(),assets);
                    var selection=new CombatSelectionController();
                    for(int y=0;y<level.Height;y++)for(int x=0;x<level.Width;x++)
                    {
                        var model=presenter.Build(build.State,level,null,selection,false,null,new GridPosition(x,y),
                            _=>null,(_,__)=>null,_=>null,_=>null);
                        if(model.FloorTexture==null)errors.Add("Missing floor: "+x+","+y);
                    }
                    foreach(var unit in build.State.Units.Values)
                    {
                        var texture=assets.Unit(unit);
                        if(texture==null)errors.Add("Missing unit: "+unit.Id);
                        else if(texture.filterMode!=FilterMode.Point)errors.Add("Unit sampling: "+unit.Id);
                    }
                    foreach(var placement in level.Terrain)
                        if(!build.State.Map.IsInside(placement.Position))errors.Add("Terrain outside map: "+placement.Position);
                    foreach(var placement in AcademyBattlefieldLayoutCatalog.VisualModules(level.Id))
                        if(placement.X<0||placement.X+placement.WidthCells>level.Width)errors.Add("Visual module outside map");
                    foreach(var shader in new[]{profile.GroundShader,"OCC/HD2D/CourtyardWater","OCC/HD2D/GroundedSprite"})
                        if(Shader.Find(shader)==null||!Shader.Find(shader).isSupported)errors.Add("Unsupported shader: "+shader);
                }
                catch(Exception ex){errors.Add(ex.ToString());}
                failures+=errors.Count;
                rows.Add(new{id=level.Id,name=level.DisplayName,width=level.Width,height=level.Height,
                    family=profile.FloorTheme.ToString(),wetStone=profile.WetStone,
                    waterCells=level.Terrain.Count(t=>t.Kind==LevelTerrainKind.Water),
                    crystalCells=level.Terrain.Count(t=>t.Kind==LevelTerrainKind.AetherCrystal),
                    reflection=profile.LocalReflection,errors,visualApproval="QA_PENDING"});
            }
            var result=JsonConvert.SerializeObject(new{scope="catalog-build-and-assets; not normal-entry visual acceptance",
                project=Application.dataPath,scene=SceneManager.GetActiveScene().path,
                count=rows.Count,failures,levels=rows},Formatting.Indented);
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../ReviewEvidence/20261012_hd2d_coverage"));
            Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"catalog.json"),result);
            return result;
        }
    }
}
