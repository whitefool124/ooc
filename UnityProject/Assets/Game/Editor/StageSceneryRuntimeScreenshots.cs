using System;
using System.Collections.Generic;
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
    [InitializeOnLoad]
    public static class StageSceneryRuntimeScreenshots
    {
        private const string Key="OCC.StageSceneryRuntimeScreenshots";
        private static readonly string[] Levels={"library_discipline","elite_foundry"};
        private static readonly string[] Variants={"library_discipline_a","elite_foundry_a"};
        private static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-16_关卡陈设盘点/runtime"));
        static StageSceneryRuntimeScreenshots(){EditorApplication.update+=Tick;}
        public static void Begin()
        {
            if(!Application.isBatchMode||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Idle background Editor required");
            StageSceneryBindingAudit.Write();
            Directory.CreateDirectory(Folder);EditorSceneManager.OpenScene("Assets/Scenes/CombatPrototype.unity");
            SessionState.SetBool(Key,true);SessionState.SetInt(Key+".phase",0);SessionState.SetInt(Key+".index",0);
            SessionState.SetFloat(Key+".deadline",(float)EditorApplication.timeSinceStartup+180);EditorApplication.EnterPlaymode();
        }
        private static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            try
            {
                int phase=SessionState.GetInt(Key+".phase",0);
                if(phase==10&&!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetBool(Key,false);File.WriteAllText(Path.Combine(Folder,"cleanup.json"),JsonConvert.SerializeObject(new{play=Application.isPlaying,dirty=SceneManager.GetActiveScene().isDirty,sceneSaved=false}));
                    EditorApplication.Exit(File.Exists(Path.Combine(Folder,"capture.json"))?0:1);return;
                }
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+".deadline",0))throw new TimeoutException("Scenery screenshot timeout");
                if(!Application.isPlaying)return;EditorApplication.QueuePlayerLoopUpdate();
                var host=UnityEngine.Object.FindAnyObjectByType<CombatPrototypeBootstrap>();if(host==null)return;
                int index=SessionState.GetInt(Key+".index",0);
                if(phase==0)
                {
                    string error=host.PreviewUiCaptureRun(true,null);if(error!=null)throw new InvalidOperationException(error);
                    var run=host.CurrentMapRun;var node=run.MapNodes.First(n=>n.Type==RogueliteMapNodeType.Elite);
                    // Only the disposable preview run's encounter binding changes.
                    ((IDictionary<string,string>)run.EncounterAssignments)[node.Id]=Variants[index];
                    run.PreviewUiCaptureNode(node.Id);EnemyUnitArtCandidates.ReviewEnabled=true;
                    host.StartMapNodeCombat(node.Id);SessionState.SetInt(Key+".phase",1);
                    SessionState.SetFloat(Key+".ready",(float)EditorApplication.timeSinceStartup+7);return;
                }
                if(phase!=1||host.IsCombatEntryBlocking||EditorApplication.timeSinceStartup<SessionState.GetFloat(Key+".ready",0))return;
                if(host.CurrentLevelId!=Levels[index])throw new InvalidOperationException("Unexpected review level: "+host.CurrentLevelId);
                var sample=UnityEngine.Object.FindAnyObjectByType<Hd2DBattlefieldSample>();if(sample==null)return;
                sample.RenderCamera.Render();sample.PumpLocalLightingReview();if(sample.LocalDiffuseUpdates==0)return;
                var fixtures=sample.RenderCamera.transform.parent.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("左侧外围陈设_")||r.name.StartsWith("后庭外围陈设_")).Select(r=>new{name=r.name,texture=r.sharedMaterial.mainTexture?.name}).ToArray();
                foreach(string id in AcademyBattlefieldLayoutCatalog.PeripheralFixtureIds(Levels[index]))if(!fixtures.Any(f=>f.texture==id))throw new InvalidOperationException("Fixture not actually bound: "+id);
                Hd2DBattleCanvasCapture.Capture(sample,1920,1080,Path.Combine(Folder,Levels[index]+".png"));
                Hd2DBattleCanvasCapture.Capture(sample,960,540,Path.Combine(Folder,Levels[index]+"_960.png"));
                File.WriteAllText(Path.Combine(Folder,Levels[index]+".json"),JsonConvert.SerializeObject(new{level=host.CurrentLevelId,fixtures,
                    source="Actual catalog encounter and runtime HUD in an isolated preview run; not a natural random journey or art approval",focusRequested=false},Formatting.Indented));
                if(index+1<Levels.Length){SessionState.SetInt(Key+".index",index+1);SessionState.SetInt(Key+".phase",0);return;}
                File.WriteAllText(Path.Combine(Folder,"capture.json"),JsonConvert.SerializeObject(new{status="CAPTURED_FOR_REVIEW",Application.dataPath,play=Application.isPlaying,levels=Levels,saveWritten=false}));
                SessionState.SetInt(Key+".phase",10);EditorApplication.ExitPlaymode();
            }
            catch(Exception e)
            {
                Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"failure.json"),e.ToString());Debug.LogException(e);
                SessionState.SetInt(Key+".phase",10);if(EditorApplication.isPlayingOrWillChangePlaymode)EditorApplication.ExitPlaymode();else{SessionState.SetBool(Key,false);EditorApplication.Exit(1);}
            }
        }
    }
}
