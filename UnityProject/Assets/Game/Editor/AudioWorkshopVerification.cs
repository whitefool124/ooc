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
    public static class AudioWorkshopVerification
    {
        private static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-03_材质音效与音效补全/workshop"));
        private static bool background;
        private static Vector2 mouse;
        [MenuItem("OCC/Audio/Workshop/Record")]
        public static void Record()
        {
            if(!Application.isPlaying || Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Main project Play required");
            Directory.CreateDirectory(Folder);AudioExpansionVerification.StartLedger(Folder);background=Application.runInBackground;mouse=Mouse.current.position.ReadValue();Application.runInBackground=true;
            EditorApplication.playModeStateChanged-=Restore;EditorApplication.playModeStateChanged+=Restore;
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            Object.FindAnyObjectByType<CombatPrototypeBootstrap>().StartCoroutine(Routine());
        }
        private static void Restore(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.ExitingPlayMode)return;
            Application.runInBackground=background;InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=mouse});
            EditorApplication.playModeStateChanged-=Restore;
        }
        private static IEnumerator Routine()
        {
            var host=Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            string error=host.PreviewUiCaptureRun(false,"B1");if(error!=null)throw new InvalidOperationException(error);
            var run=host.CurrentMapRun;
            // Disposable setup only; battles are settled through the model, not played end to end.
            run.CompleteCurrentCombat();run.ClaimReward("F-P-R19");run.SelectNode("EV1");run.ChooseCurrentNodeContent("FIRST-EV1-ACCEPT-DELIVERY");
            run.SelectNode("EV2");run.ChooseCurrentNodeContent(run.CurrentContentChoices[0].Id);run.SelectNode("B2");run.CompleteCurrentCombat();
            run.ClaimReward(run.CurrentFirstRunRewardIds[0]);run.SelectNode("EV3");run.ChooseCurrentNodeContent(run.CurrentContentChoices[1].Id);
            yield return new WaitForSecondsRealtime(.6f);host.SelectMapNode("W");yield return new WaitForSecondsRealtime(.6f);
            host.PreviewUiCapturePage("room","W");yield return new WaitForSecondsRealtime(.6f);
            yield return Snapshot("empty.png");
            yield return Click("工坊卡_ACA-EQ-CH01");
            var positioning=new List<Texture2D>();var positioningTimes=new List<string>();
            for(int i=0;i<8;i++)
            {
                yield return new WaitForEndOfFrame();positioning.Add(ScreenCapture.CaptureScreenshotAsTexture());
                positioningTimes.Add(Time.unscaledTime.ToString("R",System.Globalization.CultureInfo.InvariantCulture));
                yield return new WaitForSecondsRealtime(.025f);
            }
            for(int i=0;i<positioning.Count;i++){File.WriteAllBytes(Path.Combine(Folder,"position-"+i.ToString("00")+".png"),positioning[i].EncodeToPNG());Object.Destroy(positioning[i]);}
            File.WriteAllLines(Path.Combine(Folder,"position-times.txt"),positioningTimes);
            yield return Snapshot("selected.png");
            var before=Object.FindAnyObjectByType<WorkshopClampDetail>();
            if(before==null || before.Motion.Completed || before.raycastTarget)throw new InvalidOperationException("Invalid precommit clamp");
            yield return Click("按钮_确认安装");
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=new Vector2(1910,1070)});
            var frames=new List<Texture2D>();var samples=new List<string>();int maxCount=0;
            for(int i=0;i<18;i++)
            {
                yield return new WaitForEndOfFrame();var clamp=Object.FindAnyObjectByType<WorkshopClampDetail>();
                if(clamp!=null){maxCount=Math.Max(maxCount,clamp.Motion.CommitCount);samples.Add(Time.unscaledTime.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+" jaws="+clamp.Motion.JawOffset(Time.unscaledTime,1)+" lever="+clamp.Motion.LeverOffset(Time.unscaledTime,1)+" commits="+clamp.Motion.CommitCount);}
                frames.Add(ScreenCapture.CaptureScreenshotAsTexture());yield return new WaitForSecondsRealtime(.025f);
            }
            for(int i=0;i<frames.Count;i++){File.WriteAllBytes(Path.Combine(Folder,"frame-"+i.ToString("00")+".png"),frames[i].EncodeToPNG());Object.Destroy(frames[i]);}
            File.WriteAllLines(Path.Combine(Folder,"motion.txt"),samples);
            var final=Object.FindAnyObjectByType<WorkshopClampDetail>();
            if(maxCount!=1 || !final.Motion.Completed || final.Motion.JawOffset(Time.unscaledTime,1)!=0)throw new InvalidOperationException("Commit or settling failed");
            yield return Click("按钮_返回地图");yield return new WaitForSecondsRealtime(.4f);
            if(Object.FindAnyObjectByType<WorkshopClampDetail>()!=null)throw new InvalidOperationException("Clamp survived page exit");
            host.PreviewUiCapturePage("room","W");yield return new WaitForSecondsRealtime(.1f);
            final=Object.FindAnyObjectByType<WorkshopClampDetail>();
            if(final.Motion.CommitCount!=0 || final.Motion.JawOffset(Time.unscaledTime,1)!=0)throw new InvalidOperationException("Reopened completion replayed");
            yield return Snapshot("reopened.png");
            yield return Click("页签_术式专精");yield return new WaitForSecondsRealtime(.2f);
            string spellCard=Object.FindObjectsByType<Button>().First(b=>b.isActiveAndEnabled && b.IsInteractable() && b.name.StartsWith("工坊卡_")).name;
            var prefs=host.UiPreferences;float oldIntensity=prefs.AnimationIntensity;
            prefs.Configure(prefs.MasterVolume,0,prefs.ScreenShake,prefs.FloatingText,prefs.HighContrast,prefs.LargeText,prefs.KeyHints);
            yield return Click(spellCard);yield return null;
            final=Object.FindAnyObjectByType<WorkshopClampDetail>();
            if(final.Motion.JawOffset(Time.unscaledTime,1)!=0 || final.Motion.CommitCount!=0)throw new InvalidOperationException("Animation zero left a pending motion");
            prefs.Configure(prefs.MasterVolume,oldIntensity,prefs.ScreenShake,prefs.FloatingText,prefs.HighContrast,prefs.LargeText,prefs.KeyHints);
            yield return Snapshot("specialization.png");
            AudioExpansionVerification.RequireClip("forge");
            File.WriteAllText(Path.Combine(Folder,"result.txt"),"PASS: real Input System clicks + top raycast on target card and confirm; success event consumed once in disposable memory journey; completion settles, exit removes graphic, reopen has no completion replay. Formal bench reused, no asset replacement/player-slot write. Not a full battle journey, disk-save acceptance or runtime disk-failure test.");
        }
        private static IEnumerator Click(string name)
        {
            var b=Object.FindObjectsByType<Button>().Single(b=>b.isActiveAndEnabled && b.IsInteractable() && b.name==name);
            var rect=(RectTransform)b.transform;Vector2 p=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=p},hits);
            if(hits.Count==0 || hits[0].gameObject.GetComponentInParent<Button>()!=b)throw new InvalidOperationException("Blocked click: "+name);
            File.AppendAllText(Path.Combine(Folder,"clicks.txt"),name+" hit="+hits[0].gameObject.name+"\n");
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=p});yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=p}.WithButton(MouseButton.Left));yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=p});yield return null;
        }
        private static IEnumerator Snapshot(string name)
        {
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=new Vector2(1910,1070)});yield return null;yield return null;yield return new WaitForEndOfFrame();
            var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Folder,name),t.EncodeToPNG());Object.Destroy(t);
        }
    }
}
