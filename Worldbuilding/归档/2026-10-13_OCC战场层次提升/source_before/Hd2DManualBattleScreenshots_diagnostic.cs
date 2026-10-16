using System;
using System.IO;
using Newtonsoft.Json;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OCC.Combat.EditorTools
{
    // Three requested stills, without opening/focusing Game View or running QA suites.
    [InitializeOnLoad]
    public static class Hd2DManualBattleScreenshots
    {
        private const string Key="OCC.ManualBattleScreenshots20261013";
        private static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,
            "../../Worldbuilding/归档/2026-10-13_OCC战场层次提升/"+SessionState.GetString(Key+".label","manual")));
        static Hd2DManualBattleScreenshots(){EditorApplication.update+=Tick;}
        [MenuItem("OCC/Tools/Manual screenshots/Before layout")]
        public static void Before()=>Begin("before_layout");
        [MenuItem("OCC/Tools/Manual screenshots/After layout")]
        public static void After()=>Begin("after_layout");

        public static void Begin(string label)
        {
            if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets"||!Application.isBatchMode||
                EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||SessionState.GetBool(Key,false))
                throw new InvalidOperationException("Idle main OCC background Editor required");
            if(label!="before_layout"&&label!="after_layout")throw new ArgumentException("Unknown screenshot set");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Preserve dirty scene");
            EditorSceneManager.OpenScene("Assets/Scenes/CombatPrototype.unity");
            SessionState.SetString(Key+".label",label);SessionState.SetInt(Key+".phase",-1);
            SessionState.SetFloat(Key+".deadline",(float)EditorApplication.timeSinceStartup+60);
            Directory.CreateDirectory(Folder);SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+".deadline",0))throw new TimeoutException("Requested screenshots timed out");
                if(!Application.isPlaying)return;
                EditorApplication.QueuePlayerLoopUpdate();
                var host=UnityEngine.Object.FindAnyObjectByType<CombatPrototypeBootstrap>();if(host==null)return;
                int phase=SessionState.GetInt(Key+".phase",-1);
                if(phase==-1)
                {
                    string error=host.PreviewUiCaptureRun(false,"B3");if(error!=null)throw new InvalidOperationException(error);
                    host.StartMapNodeCombat("B3");SessionState.SetInt(Key+".phase",0);Wait(6);return;
                }
                if(EditorApplication.timeSinceStartup<SessionState.GetFloat(Key+".ready",0)||host.IsCombatEntryBlocking)return;
                var sample=UnityEngine.Object.FindAnyObjectByType<Hd2DBattlefieldSample>();if(sample==null)return;
                sample.RenderCamera.Render();sample.PumpLocalLightingReview();
                if(sample.LocalDiffuseUpdates==0)return;
                string name=phase==0?"day":phase==1?"sunset":"rain_night";
                Hd2DBattleCanvasCapture.Capture(sample,1920,1080,Path.Combine(Folder,name+".png"));
                if(phase==2)
                {
                    if(SessionState.GetString(Key+".label","")=="after_layout")CaptureWetPatternDiagnostic(sample);
                    File.WriteAllText(Path.Combine(Folder,"capture.json"),JsonConvert.SerializeObject(new{
                        status="CAPTURED_FOR_MANUAL_REVIEW",dataPath=Application.dataPath,level=host.CurrentLevelId,
                        source="Unity Editor Play Mode runtime battlefield and full HUD, isolated preview run; not a standalone Player or full journey acceptance",
                        resolution=new[]{1920,1080},captures=new[]{"day.png","sunset.png","rain_night.png"},focusRequested=false,
                        humanReview="PENDING",suitesRun=false},Formatting.Indented));
                    SessionState.SetBool(Key,false);EditorApplication.ExitPlaymode();return;
                }
                if(!sample.TrySetAtmosphere(phase==0?Hd2DBattleTimeOfDay.Sunset:Hd2DBattleTimeOfDay.Night,
                    phase==0?Hd2DBattleWeather.Clear:Hd2DBattleWeather.Rain,0))throw new InvalidOperationException("Atmosphere unavailable");
                SessionState.SetInt(Key+".phase",phase+1);Wait(4);
            }
            catch(Exception e)
            {
                File.WriteAllText(Path.Combine(Folder,"failure.json"),JsonConvert.SerializeObject(new{error=e.ToString()},Formatting.Indented));
                SessionState.SetBool(Key,false);if(EditorApplication.isPlayingOrWillChangePlaymode)EditorApplication.ExitPlaymode();Debug.LogException(e);
            }
        }
        private static void Wait(float seconds)=>SessionState.SetFloat(Key+".ready",(float)EditorApplication.timeSinceStartup+seconds);
        private static void CaptureWetPatternDiagnostic(Hd2DBattlefieldSample sample)
        {
            var root=sample.RenderCamera.transform.parent;
            var swaps=new System.Collections.Generic.Dictionary<Material,Shader>();
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))foreach(var material in renderer.sharedMaterials)
                if(material!=null&&material.shader.name=="OCC/HD2D/WetCourtyard"&&!swaps.ContainsKey(material))swaps.Add(material,material.shader);
            try
            {
                foreach(var pair in swaps){pair.Key.shader=Shader.Find("OCC/HD2D/BakedArchitecture");pair.Key.EnableKeyword("_OCC_WEATHER_WET");}
                Hd2DBattleCanvasCapture.Capture(sample,1920,1080,Path.Combine(Folder,"rain_night_without_wet_pattern.png"));
            }
            finally
            {foreach(var pair in swaps){pair.Key.shader=pair.Value;pair.Key.DisableKeyword("_OCC_WEATHER_WET");}}
        }
    }
}
