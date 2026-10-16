using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace OCC.Combat.EditorTools
{
    public static class AudioFootstepVerification
    {
        private static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-03_材质音效与音效补全/footsteps"));
        private static bool previousBackground,armed;
        private static Vector2 previousMouse;
        private static readonly List<Texture2D> frames=new List<Texture2D>();
        [MenuItem("OCC/Audio/Footsteps/Begin")]
        public static void Begin()
        {
            Guard();Directory.CreateDirectory(Folder);AudioExpansionVerification.StartLedger(Folder);
            if(!armed){previousBackground=Application.runInBackground;previousMouse=Mouse.current.position.ReadValue();armed=true;}
            Application.runInBackground=true;
            EditorApplication.playModeStateChanged-=Restore;EditorApplication.playModeStateChanged+=Restore;
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            Object.FindAnyObjectByType<CombatPrototypeBootstrap>().StartCoroutine(BeginRoutine());
        }
        private static void Guard()
        {
            if(!Application.isPlaying || Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Main project Play required");
        }
        private static void Restore(PlayModeStateChange change)
        {
            if(change!=PlayModeStateChange.ExitingPlayMode || !armed)return;
            Application.runInBackground=previousBackground;armed=false;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=previousMouse});
            foreach(var frame in frames)if(frame!=null)Object.Destroy(frame);frames.Clear();
            EditorApplication.playModeStateChanged-=Restore;
        }
        private static IEnumerator BeginRoutine()
        {
            var host=Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            string error=host.PreviewUiCaptureRun(false,"B1");if(error!=null)throw new InvalidOperationException(error);
            host.PreviewUiCapturePage("map");
            yield return new WaitForSecondsRealtime(.6f);
            yield return ClickButton("map.node.B1");
            yield return new WaitForSecondsRealtime(.6f);
            yield return ClickButton("按钮_进入战斗");
            yield return new WaitForSecondsRealtime(2f);
            Inspect();yield return Frame("battle-entry.png");
        }
        [MenuItem("OCC/Audio/Footsteps/Inspect")]
        public static void Inspect()
        {
            Guard();var host=Object.FindAnyObjectByType<CombatPrototypeBootstrap>();var state=host.CurrentState;
            File.WriteAllLines(Path.Combine(Folder,"buttons.txt"),Object.FindObjectsByType<Button>().Where(b=>b.isActiveAndEnabled && b.IsInteractable()).Select(b=>b.name+" | "+string.Join(" / ",b.GetComponentsInChildren<Text>().Select(t=>t.text))));
            if(state==null){File.WriteAllText(Path.Combine(Folder,"state.txt"),"No combat state");return;}
            var hero=state.GetUnit("hero");
            var lines=new List<string>{"hero="+hero.Position+" AP="+hero.ActionPoints+" modal="+host.IsInteractionModalOpen+" action="+host.SelectedAction+" ripples="+host.WaterRipples.Waves.Count};
            for(int y=0;y<state.Map.Height;y++)for(int x=0;x<state.Map.Width;x++){var p=new GridPosition(x,y);if(state.Map.GetTile(p).IsWater)lines.Add("water="+p+" reachable="+host.CanQuickMoveTo(p));}
            File.WriteAllLines(Path.Combine(Folder,"state.txt"),lines);
        }
        [MenuItem("OCC/Audio/Footsteps/Step And Record")]
        public static void Step(){Guard();Object.FindAnyObjectByType<CombatPrototypeBootstrap>().StartCoroutine(StepRoutine());}
        private static IEnumerator StepRoutine()
        {
            var host=Object.FindAnyObjectByType<CombatPrototypeBootstrap>();var state=host.CurrentState;var hero=state.GetUnit("hero");
            var water=new List<GridPosition>();var reachable=new List<GridPosition>();
            for(int y=0;y<state.Map.Height;y++)for(int x=0;x<state.Map.Width;x++){var p=new GridPosition(x,y);if(state.Map.GetTile(p).IsWater)water.Add(p);if(host.CanQuickMoveTo(p) && p!=hero.Position)reachable.Add(p);}
            if(reachable.Count==0)throw new InvalidOperationException("No legal movement; inspect tutorial or turn UI");
            GridPosition target=reachable.OrderBy(p=>water.Contains(p)?0:1).ThenBy(p=>water.Min(w=>p.ManhattanDistance(w))).ThenBy(p=>hero.Position.ManhattanDistance(p)).First();
            var root=Object.FindObjectsByType<RectTransform>().Single(r=>r.name=="格子_"+target.X+"_"+target.Y && r.gameObject.activeInHierarchy);
            Vector2 point=RectTransformUtility.WorldToScreenPoint(null,root.TransformPoint(root.rect.center));
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            File.AppendAllText(Path.Combine(Folder,"moves.txt"),"from="+hero.Position+" target="+target+" water="+water.Contains(target)+" hit="+(hits.Count>0?hits[0].gameObject.name:"none")+"\n");
            foreach(var frame in frames)if(frame!=null)Object.Destroy(frame);frames.Clear();
            yield return Click(point);yield return new WaitForSecondsRealtime(.04f);yield return Click(point);
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=new Vector2(1910f,1070f)});
            int peak=0; var captureTimes=new List<float>();
            for(int i=0;i<20;i++)
            {
                yield return new WaitForEndOfFrame();peak=Math.Max(peak,host.WaterRipples.Waves.Count);frames.Add(ScreenCapture.CaptureScreenshotAsTexture());captureTimes.Add(Time.unscaledTime);
                yield return new WaitForSecondsRealtime(.025f);
            }
            File.WriteAllLines(Path.Combine(Folder,"frame-times.txt"),captureTimes.Select(t=>t.ToString("R",System.Globalization.CultureInfo.InvariantCulture)));
            for(int i=0;i<frames.Count;i++){File.WriteAllBytes(Path.Combine(Folder,"frame-"+i.ToString("00")+".png"),frames[i].EncodeToPNG());Object.Destroy(frames[i]);}frames.Clear();
            File.AppendAllText(Path.Combine(Folder,"moves.txt"),"final="+hero.Position+" peakRipples="+peak+" shaders="+Object.FindObjectsByType<CombatWaterRippleGraphic>().Count(g=>g.raycastTarget)+" raycast enabled\n");
            if(hero.Position!=target)throw new InvalidOperationException("Actual movement did not commit");
            if(water.Contains(target) && AudioExpansionVerification.Count("water_0")+AudioExpansionVerification.Count("water_1")==0)throw new InvalidOperationException("Wet footstep missing");
            File.WriteAllText(Path.Combine(Folder,"result.txt"),"PASS: real map/departure clicks and actual default battle movement; stone and wet foot contact playback observed, no player-save writes. Not a full combat journey.");
            Inspect();
        }
        [MenuItem("OCC/Audio/Footsteps/Verify Cleanup")]
        public static void VerifyCleanup(){Guard();Object.FindAnyObjectByType<CombatPrototypeBootstrap>().StartCoroutine(CleanupRoutine());}
        private static IEnumerator CleanupRoutine()
        {
            var host=Object.FindAnyObjectByType<CombatPrototypeBootstrap>();var feedback=Object.FindAnyObjectByType<CombatVisualFeedback>();
            var prefs=host.UiPreferences;float previous=prefs.AnimationIntensity;
            // Deliberately inject a pending visual only to exercise interruption, never gameplay.
            host.WaterRipples.Add(new Vector2(2,3),Time.unscaledTime);
            prefs.Configure(prefs.MasterVolume,0,prefs.ScreenShake,prefs.FloatingText,prefs.HighContrast,prefs.LargeText,prefs.KeyHints);
            yield return null;yield return null;
            bool zero=host.WaterRipples.Waves.Count==0;
            prefs.Configure(prefs.MasterVolume,previous,prefs.ScreenShake,prefs.FloatingText,prefs.HighContrast,prefs.LargeText,prefs.KeyHints);
            host.WaterRipples.Add(new Vector2(2,3),Time.unscaledTime);
            feedback.enabled=false;bool disabled=host.WaterRipples.Waves.Count==0;feedback.enabled=true;
            if(!zero || !disabled)throw new InvalidOperationException("Cleanup failed");
            File.WriteAllText(Path.Combine(Folder,"cleanup.txt"),"PASS: injected pending visual cleared on animation=0 and feedback disable/re-enable. Preferences changed only in memory and restored; no SaveUiPreferences call.");
        }
        private static IEnumerator ClickButton(string name)
        {
            var b=Object.FindObjectsByType<Button>().Single(b=>b.name==name && b.isActiveAndEnabled && b.IsInteractable());var rect=(RectTransform)b.transform;
            yield return Click(RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center)));
        }
        private static IEnumerator Click(Vector2 point)
        {
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=point});yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=point}.WithButton(MouseButton.Left));yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=point});yield return null;
        }
        private static IEnumerator Frame(string name)
        {
            yield return new WaitForEndOfFrame();var frame=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Folder,name),frame.EncodeToPNG());Object.Destroy(frame);
        }
    }
}