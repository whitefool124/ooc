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
    [InitializeOnLoad]
    public static class BreachRamRuntimeScreenshot
    {
        private const string Key="OCC.BreachRamRuntimeScreenshot";
        private static bool Formal=>SessionState.GetBool(Key+".formal",false);
        private static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,Formal?
            "../../Worldbuilding/归档/2026-10-16_楔角正式接入与剩余待办/runtime":
            "../../Worldbuilding/归档/2026-10-16_楔角旧形象与文字反馈/runtime"));
        static BreachRamRuntimeScreenshot(){EditorApplication.update+=Tick;}
        public static void BeginFormal()
        {
            BreachRamResourceAdoption.Apply();SessionState.SetBool(Key+".formal",true);Begin();
        }
        public static void Begin()
        {
            if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets"||!Application.isBatchMode||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Idle main background Editor required");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve dirty scenes");
            Directory.CreateDirectory(Folder);EditorSceneManager.OpenScene("Assets/Scenes/CombatPrototype.unity");
            SessionState.SetBool(Key,true);SessionState.SetInt(Key+".phase",0);
            SessionState.SetFloat(Key+".deadline",(float)EditorApplication.timeSinceStartup+150);EditorApplication.EnterPlaymode();
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
                    EditorApplication.Exit(File.Exists(Path.Combine(Folder,"ram.png"))?0:1);return;
                }
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+".deadline",0))throw new TimeoutException("Ram screenshot timeout");
                if(!Application.isPlaying)return;EditorApplication.QueuePlayerLoopUpdate();
                var host=UnityEngine.Object.FindAnyObjectByType<CombatPrototypeBootstrap>();if(host==null)return;
                if(phase==0)
                {
                    string error=host.PreviewUiCaptureRun(false,"X");if(error!=null)throw new InvalidOperationException(error);
                    EnemyUnitArtCandidates.ReviewEnabled=!Formal;host.StartMapNodeCombat("X");
                    SessionState.SetInt(Key+".phase",1);SessionState.SetFloat(Key+".ready",(float)EditorApplication.timeSinceStartup+10);return;
                }
                if(phase!=1||host.IsCombatEntryBlocking||EditorApplication.timeSinceStartup<SessionState.GetFloat(Key+".ready",0))return;
                if(host.CurrentLevelId!="first_elite_three_material_pressure")throw new InvalidOperationException("Unexpected level: "+host.CurrentLevelId);
                var sample=UnityEngine.Object.FindAnyObjectByType<Hd2DBattlefieldSample>();if(sample==null)return;
                var unit=host.CurrentState.Units.Values.First(u=>u.EnemyArchetypeId=="breach_ram");
                var assets=new CombatFormalVisualAssets();assets.LoadRuntime();if(assets.Unit(unit).name!="breach_ram")throw new InvalidOperationException("Candidate not bound");
                sample.RenderCamera.Render();sample.PumpLocalLightingReview();if(sample.LocalDiffuseUpdates==0)return;
                Hd2DBattleCanvasCapture.Capture(sample,1920,1080,Path.Combine(Folder,"ram.png"));
                Hd2DBattleCanvasCapture.Capture(sample,960,540,Path.Combine(Folder,"ram_960.png"));
                File.WriteAllText(Path.Combine(Folder,"capture.json"),JsonConvert.SerializeObject(new{level=host.CurrentLevelId,unit.Id,texture=assets.Unit(unit).name,resource=FormalArtRegistry.UnitPath("breach_ram"),candidateReviewEnabled=EnemyUnitArtCandidates.ReviewEnabled,ppu=32,canvas="64x64",isolatedPreviewRun=true,focusRequested=false,saveWritten=false},Formatting.Indented));
                SessionState.SetInt(Key+".phase",10);EditorApplication.ExitPlaymode();
            }
            catch(Exception e)
            {
                File.WriteAllText(Path.Combine(Folder,"failure.json"),e.ToString());Debug.LogException(e);
                SessionState.SetInt(Key+".phase",10);if(EditorApplication.isPlayingOrWillChangePlaymode)EditorApplication.ExitPlaymode();else{SessionState.SetBool(Key,false);EditorApplication.Exit(1);}
            }
        }
    }
}
