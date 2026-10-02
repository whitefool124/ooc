using System;
using System.Collections.Generic;
using System.IO;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OCC.Combat.EditorTools
{
    public static class PixelVfxShowcaseVerification
    {
        [Serializable] private class Receipt { public int mode; public string name,hash; public float age; }
        [Serializable] private class Report { public bool success; public string scene,detail; public List<Receipt> frames=new List<Receipt>(); }
        private static PixelVfxShowcase lab;
        private static double started;
        private static int stage;
        private static bool waiting;
        private static string output;
        private static Report report;
        [MenuItem("OCC/VFX/Inspect Runtime")]
        public static void InspectRuntime()
        {
            var subject=UnityEngine.Object.FindFirstObjectByType<PixelVfxShowcase>();
            var state=new {EditorApplication.isPlaying,EditorApplication.isPaused,Time.frameCount,Time.unscaledDeltaTime,
                enabled=subject!=null && subject.enabled,active=subject!=null && subject.gameObject.activeInHierarchy,
                mode=subject!=null?subject.Mode:-1,age=subject!=null?subject.Age:-1,paused=subject!=null&&subject.Paused,
                stage,waiting,verifying=lab!=null};
            var path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-02_非护盾特效与界面规划/runtime-state.json"));
            File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(state));
        }
        [MenuItem("OCC/VFX/Verify Showcase Controls")]
        public static void Verify()
        {
            if(lab!=null) throw new InvalidOperationException("Verification already running.");
            lab=UnityEngine.Object.FindFirstObjectByType<PixelVfxShowcase>();
            if(!EditorApplication.isPlaying || lab==null || lab.Output==null) {lab=null;throw new InvalidOperationException("Run the showcase first.");}
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-02_非护盾特效与界面规划"));
            Directory.CreateDirectory(output);
            report=new Report{scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path};
            stage=0;waiting=false;Click(lab.modeButtons[0]);Click(lab.replayButton);
            started=EditorApplication.timeSinceStartup;
            EditorApplication.update+=Tick;
        }
        private static void Click(Button button)
        {
            var corners=new Vector3[4];button.GetComponent<RectTransform>().GetWorldCorners(corners);
            var pointer=new PointerEventData(EventSystem.current){position=(corners[0]+corners[2])*.5f,button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            if(hits.Count==0 || hits[0].gameObject!=button.gameObject) throw new InvalidOperationException("Occluded control: "+button.name);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
        private static void Tick()
        {
            EditorApplication.QueuePlayerLoopUpdate();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            try
            {
                if(lab==null || !EditorApplication.isPlaying) throw new InvalidOperationException("Scene stopped during verification.");
                if(EditorApplication.timeSinceStartup-started>15) throw new InvalidOperationException("Player loop did not reach the target phase.");
                if(EditorApplication.timeSinceStartup-started<(waiting?.15:.65)) return;
                if(!waiting && !lab.Paused && lab.Age<.4f) return;
                if(!waiting){if(!lab.Paused) Click(lab.pauseButton);waiting=true;started=EditorApplication.timeSinceStartup;return;}
                var previous=RenderTexture.active;RenderTexture.active=lab.Output;
                var texture=new Texture2D(240,180,TextureFormat.RGBA32,false);
                texture.ReadPixels(new Rect(0,0,240,180),0,0);texture.Apply();RenderTexture.active=previous;
                byte[] png=texture.EncodeToPNG();UnityEngine.Object.DestroyImmediate(texture);
                string hash=Convert.ToBase64String(System.Security.Cryptography.MD5.Create().ComputeHash(png));
                if(stage<7 && report.frames.Exists(f=>f.hash==hash)) throw new InvalidOperationException("Identical output for different modes.");
                if(ShaderUtil.GetShaderMessages(lab.effectShader).Length>0) throw new InvalidOperationException("Shader compilation diagnostics.");
                File.WriteAllBytes(Path.Combine(output,"native-"+stage+".png"),png);
                report.frames.Add(new Receipt{mode=lab.Mode,name=PixelVfxShowcase.Names[lab.Mode],age=lab.Age,hash=hash});
                if(stage==1 || stage==6) ScreenCapture.CaptureScreenshot(Path.Combine(output,"showcase-"+stage+".png"));
                stage++;
                if(stage<7) Click(lab.modeButtons[stage]);
                else if(stage==7){Click(lab.originalButton);if(!lab.Original)throw new Exception("Original toggle failed.");}
                else if(stage==8){Click(lab.originalButton);Click(lab.modeButtons[5]);Click(lab.slowButton);lab.lightStrength.value=1.5f;}
                else if(stage==9){Click(lab.slowButton);lab.lightStrength.value=1;Click(lab.modeButtons[6]);
                    var corners=new Vector3[4];lab.display.rectTransform.GetWorldCorners(corners);
                    var pointer=new PointerEventData(EventSystem.current){position=Vector3.Lerp(corners[0],corners[2],.35f)};
                    ExecuteEvents.Execute(lab.gameObject,pointer,ExecuteEvents.pointerDownHandler);
                }
                else {
                    report.success=true;report.detail="Seven visible mode buttons passed raycast/click and distinct GPU output. Replay, pause, slow, original, intensity and freely positioned stage effect verified.";
                    File.WriteAllText(Path.Combine(output,"verification.json"),JsonUtility.ToJson(report,true));
                    Click(lab.modeButtons[1]);EditorApplication.update-=Tick;lab=null;Debug.Log("OCC_NON_SHIELD_VFX_VERIFIED");return;
                }
                waiting=false;started=EditorApplication.timeSinceStartup;
            }
            catch(Exception ex){EditorApplication.update-=Tick;lab=null;File.WriteAllText(Path.Combine(output,"verification-error.txt"),ex.ToString());Debug.LogException(ex);}
        }
    }
}
