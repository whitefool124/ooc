using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OCC.Combat.EditorTools
{
    // Isolated in-memory artwork staging, rendered by the real battlefield/HUD.
    // No combat turns, scene saves, formal artwork replacement or player saves.
    [InitializeOnLoad]
    public static class EnemyFieldRuntimeScreenshots
    {
        private const string Key="OCC.EnemyFieldRuntimeScreenshots";
        private static bool ResetReview=>SessionState.GetBool(Key+".resetReview",false);
        private static bool DeviceReview=>SessionState.GetBool(Key+".deviceReview",false);
        private static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,DeviceReview?
            "../../Worldbuilding/归档/2026-10-16_装置与碎晶辨识/runtime":ResetReview?
            "../../Worldbuilding/归档/2026-10-16_五类场地美术重制/runtime":"../../Worldbuilding/归档/2026-10-16_四敌人与场地实机图"));
        static EnemyFieldRuntimeScreenshots(){EditorApplication.update+=Tick;}
        public static void BeginResetReview(){SessionState.SetBool(Key+".resetReview",true);Begin();}
        public static void BeginDeviceReview(){SessionState.SetBool(Key+".deviceReview",true);SessionState.SetBool(Key+".resetReview",true);Begin();}
        public static void Begin()
        {
            if(!Application.isBatchMode||Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets"||
                EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)
                throw new InvalidOperationException("Idle main OCC background Editor required");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve dirty scene");
            Directory.CreateDirectory(Folder);
            File.WriteAllText(Path.Combine(Folder,"initial_state.json"),JsonConvert.SerializeObject(new{dataPath=Application.dataPath,
                activeScene=SceneManager.GetActiveScene().path,loadedScenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>new{path=SceneManager.GetSceneAt(i).path,dirty=SceneManager.GetSceneAt(i).isDirty})},Formatting.Indented));
            EditorSceneManager.OpenScene("Assets/Scenes/CombatPrototype.unity");
            SessionState.SetBool(Key,true);SessionState.SetInt(Key+".phase",-1);
            SessionState.SetFloat(Key+".deadline",(float)EditorApplication.timeSinceStartup+150);
            EditorApplication.EnterPlaymode();
        }
        private static void Wait(float seconds)=>SessionState.SetFloat(Key+".ready",(float)EditorApplication.timeSinceStartup+seconds);
        private static void Move(UnitState unit,int x,int y)=>typeof(UnitState).GetMethod("MoveTo",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(unit,new object[]{new GridPosition(x,y)});
        private static void Stage(CombatPrototypeBootstrap host,bool second,bool elements)
        {
            EnemyUnitArtCandidates.ReviewEnabled=true;BattlefieldFieldArtCandidates.ReviewEnabled=true;
            var state=host.CurrentState;
            if(state.Map.Width<5||state.Map.Height<4)throw new InvalidOperationException("Staging requires at least 5x4 battlefield");
            for(int y=0;y<state.Map.Height;y++)for(int x=0;x<state.Map.Width;x++)state.Map.SetTile(new GridPosition(x,y),new TileState());
            var enemies=state.Units.Values.Where(u=>!u.IsHero&&u.IsAlive).ToArray();
            if(enemies.Length!=2)throw new InvalidOperationException("Expected two B3 review actors");
            string[] ids=second?new[]{"wind_librarian","legacy_storekeeper"}:new[]{"signal_keeper","elder_tracker_hound"};
            Move(state.GetUnit("hero"),0,3);
            for(int i=0;i<2;i++){EnemyArchetypes.Get(ids[i]).Apply(enemies[i]);Move(enemies[i],2+i*2,3);}
            state.Map.SetTile(new GridPosition(0,1),new TileState{IsLoosePaper=true});
            state.Map.SetTile(new GridPosition(1,1),new TileState{HasTrace=true});
            state.Map.SetTile(new GridPosition(3,1),new TileState{IsBindingMark=true});
            state.Map.SetTile(new GridPosition(4,1),new TileState{HasPaperScreen=true});
            if(elements&&!DeviceReview)
            {
                state.Map.SetTile(new GridPosition(0,0),new TileState{IsWater=true});
                state.Map.SetTile(new GridPosition(1,0),new TileState{IsWater=true});
                state.Map.SetTile(new GridPosition(3,0),new TileState{SmokeExpiresAt=state.CurrentTime+100});
                host.CurrentFireBattle.CreateOrRefreshFireground(new GridPosition(4,0),8,3,"art-review","hero");
            }
            if(DeviceReview)
            {
                BattlefieldDeviceArtCandidates.ReviewEnabled=true;
                for(int y=0;y<state.Map.Height;y++)for(int x=0;x<state.Map.Width;x++)state.Map.SetTile(new GridPosition(x,y),new TileState());
                int durability=elements?0:second?5:24;
                state.Map.SetTile(new GridPosition(0,1),new TileState{IsOverloadDevice=true,Durability=durability});
                state.Map.SetTile(new GridPosition(2,1),new TileState{IsWardGenerator=true,Durability=durability,IsCrystalShard=true});
                state.Map.SetTile(new GridPosition(4,1),new TileState{IsTowerMechanism=true,Durability=durability,IsReleased=second,MechanismKind=1});
                state.Map.SetTile(new GridPosition(0,0),new TileState{IsCrystalShard=true});
                state.Map.SetTile(new GridPosition(1,0),new TileState{IsCrystalShard=true,IsLoosePaper=true});
            }
            host.SelectHudAction("移动");
            if(ResetReview)
            {
                var selection=(CombatSelectionController)typeof(CombatPrototypeBootstrap).GetField("selection",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(host);
                // Match the existing context-hover state with no offered action:
                // keep the legal selected action, suppress only its range overlay.
                selection.SetMenuPreview(string.Empty,state.GetUnit("hero").Position);
            }
            typeof(CombatPrototypeBootstrap).GetMethod("MarkPresentation",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(host,new object[]{UiPresentationArea.Combat});
        }
        private static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            try
            {
                int phase=SessionState.GetInt(Key+".phase",-1);
                if(phase==10&&!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetBool(Key,false);
                    File.WriteAllText(Path.Combine(Folder,"cleanup.json"),JsonConvert.SerializeObject(new{play=Application.isPlaying,dirty=SceneManager.GetActiveScene().isDirty,sceneSaved=false}));
                    EditorApplication.Exit(File.Exists(Path.Combine(Folder,"capture.json"))?0:1);return;
                }
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+".deadline",0))throw new TimeoutException("Enemy field capture timed out");
                if(!Application.isPlaying)return;
                EditorApplication.QueuePlayerLoopUpdate();
                var host=UnityEngine.Object.FindAnyObjectByType<CombatPrototypeBootstrap>();if(host==null)return;
                if(phase==-1)
                {
                    string error=host.PreviewUiCaptureRun(false,"B3");if(error!=null)throw new InvalidOperationException(error);
                    host.StartMapNodeCombat("B3");SessionState.SetInt(Key+".phase",0);Wait(8);return;
                }
                if(host.IsCombatEntryBlocking||EditorApplication.timeSinceStartup<SessionState.GetFloat(Key+".ready",0))return;
                var sample=UnityEngine.Object.FindAnyObjectByType<Hd2DBattlefieldSample>();if(sample==null)return;
                if(phase==0||phase==2||phase==4)
                {
                    Stage(host,phase>=2,phase==4);
                    sample.TrySetAtmosphere(Hd2DBattleTimeOfDay.SceneDefault,Hd2DBattleWeather.Clear,0);
                    SessionState.SetInt(Key+".phase",phase+1);Wait(5);return;
                }
                if(phase!=1&&phase!=3&&phase!=5)return;
                string name=phase==1?"01_signal_keeper_and_hound":phase==3?"02_librarian_and_storekeeper":"03_field_effects";
                if(DeviceReview)name=phase==1?"01_devices_and_shards":phase==3?"02_damaged_devices":"03_destroyed_devices";
                sample.RenderCamera.Render();sample.PumpLocalLightingReview();
                Hd2DBattleCanvasCapture.Capture(sample,1920,1080,Path.Combine(Folder,name+".png"));
                if(ResetReview&&phase==5)Hd2DBattleCanvasCapture.Capture(sample,960,540,Path.Combine(Folder,name+"_960.png"));
                File.WriteAllText(Path.Combine(Folder,name+".json"),JsonConvert.SerializeObject(new{
                    source="Actual Unity Editor Play Mode battlefield and runtime HUD; isolated in-memory artwork staging, not natural encounter or full journey",
                    enemies=host.CurrentState.Units.Values.Where(u=>!u.IsHero).Select(u=>new{id=u.EnemyArchetypeId,name=u.DisplayName,position=u.Position.ToString(),texture=host.PresentBattlefieldCell(u.Position).UnitTexture?.name}),
                    fields=sample.FrameModels.Where(p=>!string.IsNullOrEmpty(p.Value.FieldEffectLabel)||p.Value.EnvironmentTexture!=null).Select(p=>new{position=p.Key.ToString(),label=p.Value.FieldEffectLabel,texture=p.Value.EnvironmentTexture?.name}),
                    materials=sample.FrameModels.Where(p=>!string.IsNullOrEmpty(p.Value.GroundMaterialLabel)).Select(p=>new{position=p.Key.ToString(),label=p.Value.GroundMaterialLabel,texture=p.Value.GroundMaterialTexture?.name,objectTexture=p.Value.ObjectTexture?.name,effectTexture=p.Value.EnvironmentTexture?.name}),
                    devices=sample.FrameModels.Where(p=>host.CurrentState.Map.GetTile(p.Key).IsDeviceLike).Select(p=>new{position=p.Key.ToString(),label=p.Value.ObjectLabel,texture=p.Value.ObjectTexture?.name,durability=host.CurrentState.Map.GetTile(p.Key).Durability}),
                    resolution=new[]{1920,1080},focusRequested=false,candidates="QA_PENDING"},Formatting.Indented));
                if(phase==5)
                {
                    File.WriteAllText(Path.Combine(Folder,"capture.json"),JsonConvert.SerializeObject(new{status="CAPTURED_FOR_REVIEW",Application.dataPath,play=Application.isPlaying,formalReplacement=false,saveWritten=false}));
                    SessionState.SetInt(Key+".phase",10);EditorApplication.ExitPlaymode();return;
                }
                SessionState.SetInt(Key+".phase",phase+1);Wait(.2f);
            }
            catch(Exception e)
            {
                Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"failure.json"),e.ToString());
                Debug.LogException(e);SessionState.SetInt(Key+".phase",10);
                if(EditorApplication.isPlayingOrWillChangePlaymode)EditorApplication.ExitPlaymode();else{SessionState.SetBool(Key,false);EditorApplication.Exit(1);}
            }
        }
    }
}
