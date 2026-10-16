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
using UnityEngine.UI;

namespace OCC.Combat.EditorTools
{
    [InitializeOnLoad]
    public static class StageOneClosureCapture
    {
        private const string Key="OCC.StageOneClosureCapture";
        private static readonly string[] Names={"01_keeper_hound","02_librarian_storekeeper","03_fields","04_devices","05_ram_occlusion","06_medical","07_workshop","08_shop","09_event","10_victory","11_death","12_abandon","13_academy_medical"};
        private static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-16_第一阶段待办清空/runtime"));
        static StageOneClosureCapture(){EditorApplication.update+=Tick;}
        public static void BeginAffected(){SessionState.SetInt(Key+".start",4);Begin();}
        public static void BeginUiOnly(){SessionState.SetInt(Key+".start",5);Begin();}
        public static void Begin()
        {
            if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets"||!Application.isBatchMode||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Idle main batch Editor required");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve dirty scenes");
            Directory.CreateDirectory(Folder);EditorSceneManager.OpenScene("Assets/Scenes/CombatPrototype.unity");
            SessionState.SetBool(Key,true);SessionState.SetInt(Key+".index",SessionState.GetInt(Key+".start",0));SessionState.SetInt(Key+".phase",0);
            SessionState.SetFloat(Key+".deadline",(float)EditorApplication.timeSinceStartup+360);EditorApplication.EnterPlaymode();
        }
        private static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            try
            {
                int phase=SessionState.GetInt(Key+".phase",0),index=SessionState.GetInt(Key+".index",0);
                if(phase==10&&!EditorApplication.isPlayingOrWillChangePlaymode)
                {SessionState.SetBool(Key,false);File.WriteAllText(Path.Combine(Folder,"cleanup.json"),JsonConvert.SerializeObject(new{play=Application.isPlaying,dirty=SceneManager.GetActiveScene().isDirty,sceneSaved=false}));EditorApplication.Exit(File.Exists(Path.Combine(Folder,Names.Last()+".json"))&&!File.Exists(Path.Combine(Folder,"failure.json"))?0:1);return;}
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+".deadline",0))throw new TimeoutException("Closure capture timeout");
                if(!Application.isPlaying)return;EditorApplication.QueuePlayerLoopUpdate();
                var host=UnityEngine.Object.FindAnyObjectByType<CombatPrototypeBootstrap>();if(host==null)return;
                if(phase==0)
                {
                    string node=index<4?"B3":index==4?"X":index==5?"M":index==6?"W":index==7?"S":index==8?"EV1":index==12?"layer_medical":"X";
                    string special=index==9?"victory":index==10?"failure":index==11?"abandon":null;
                    string error=host.PreviewUiCaptureRun(index==12,node,special:special);if(error!=null)throw new InvalidOperationException(error);
                    EnemyUnitArtCandidates.ReviewEnabled=false;BattlefieldFieldArtCandidates.ReviewEnabled=false;BattlefieldDeviceArtCandidates.ReviewEnabled=false;
                    if(index<5)host.StartMapNodeCombat(node);else if(index<9||index==12){error=host.PreviewUiCapturePage("room",node);if(error!=null)throw new InvalidOperationException(error);}
                    SessionState.SetInt(Key+".phase",1);SessionState.SetFloat(Key+".ready",(float)EditorApplication.timeSinceStartup+8);return;
                }
                if(EditorApplication.timeSinceStartup<SessionState.GetFloat(Key+".ready",0)||host.IsCombatEntryBlocking)return;
                if(phase==1&&(index>=5&&index<=8||index==12))
                {
                    // Wait for Update to enter the Map screen before opening its
                    // overlay; that screen transition intentionally clears overlays.
                    string error=host.PreviewUiCapturePage("room",host.CurrentMapRun.CurrentNodeId);
                    if(error!=null)throw new InvalidOperationException(error);
                    SessionState.SetInt(Key+".phase",2);SessionState.SetFloat(Key+".ready",(float)EditorApplication.timeSinceStartup+5);return;
                }
                if(phase==1&&index<4)
                {
                    SessionState.SetBool("OCC.EnemyFieldRuntimeScreenshots.deviceReview",index==3);
                    typeof(EnemyFieldRuntimeScreenshots).GetMethod("Stage",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{host,index>0,index==2});
                    SessionState.SetBool("OCC.EnemyFieldRuntimeScreenshots.deviceReview",false);
                    EnemyUnitArtCandidates.ReviewEnabled=false;BattlefieldFieldArtCandidates.ReviewEnabled=false;BattlefieldDeviceArtCandidates.ReviewEnabled=false;
                    var selection=(CombatSelectionController)typeof(CombatPrototypeBootstrap).GetField("selection",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(host);
                    selection.SetMenuPreview(string.Empty,host.CurrentState.GetUnit("hero").Position);
                    SessionState.SetInt(Key+".phase",2);SessionState.SetFloat(Key+".ready",(float)EditorApplication.timeSinceStartup+3);return;
                }
                var sample=index<5?UnityEngine.Object.FindAnyObjectByType<Hd2DBattlefieldSample>():null;
                if(index<5&&sample==null)return;
                if(index==4)
                {
                    if(host.CurrentLevelId!="first_elite_three_material_pressure")throw new InvalidOperationException("Wrong ram level: "+host.CurrentLevelId);
                    sample.SendMessage("LateUpdate",SendMessageOptions.DontRequireReceiver);
                }
                Hd2DBattleCanvasCapture.Capture(sample,1920,1080,Path.Combine(Folder,Names[index]+".png"));
                Hd2DBattleCanvasCapture.Capture(sample,960,540,Path.Combine(Folder,Names[index]+"_960.png"));
                File.WriteAllText(Path.Combine(Folder,Names[index]+".json"),JsonConvert.SerializeObject(new{caseName=Names[index],candidateReview=false,focusRequested=false,saveWritten=false,isolatedPreview=true,
                    source=index<4?"In-memory local art staging on the actual B3 renderer":index==4?"Actual FIRST-X encounter":"Actual runtime node room or summary UI",
                    level=host.CurrentLevelId,flow=host.CurrentFlowPhase.ToString(),
                    units=sample?.FrameModels.Where(p=>p.Value.UnitTexture!=null).Select(p=>p.Value.UnitTexture.name).ToArray(),
                    uiText=UnityEngine.Object.FindObjectsByType<Text>().Where(t=>t.isActiveAndEnabled).Select(t=>t.text).Where(t=>!string.IsNullOrEmpty(t)).Distinct().ToArray()},Formatting.Indented));
                if(index==Names.Length-1){SessionState.SetInt(Key+".phase",10);EditorApplication.ExitPlaymode();return;}
                SessionState.SetInt(Key+".index",index+1);SessionState.SetInt(Key+".phase",0);
            }
            catch(Exception e)
            {File.WriteAllText(Path.Combine(Folder,"failure.json"),e.ToString());Debug.LogException(e);SessionState.SetInt(Key+".phase",10);if(EditorApplication.isPlayingOrWillChangePlaymode)EditorApplication.ExitPlaymode();else{SessionState.SetBool(Key,false);EditorApplication.Exit(1);}}
        }
    }
}
