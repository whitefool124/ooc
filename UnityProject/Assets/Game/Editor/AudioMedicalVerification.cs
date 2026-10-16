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
    public static class AudioMedicalVerification
    {
        private static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-03_材质音效与音效补全/medical"));
        private static bool background;
        private static Vector2 mouse;
        private static Texture2D texture;
        [MenuItem("OCC/Audio/Medical/Record")]
        public static void Record()
        {
            if(!Application.isPlaying || Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Main project Play required");
            Directory.CreateDirectory(Folder);AudioExpansionVerification.StartLedger(Folder);background=Application.runInBackground;mouse=Mouse.current.position.ReadValue();Application.runInBackground=true;
            EditorApplication.playModeStateChanged-=Restore;EditorApplication.playModeStateChanged+=Restore;
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            var formal=Resources.Load<Sprite>(HotDrinkSteamDetail.ArtPath);
            if(formal!=null)
                File.WriteAllText(Path.Combine(Folder,"formal-resource.txt"),AssetDatabase.GetAssetPath(formal)+" GUID="+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(formal)));
            else
            {
                // A decoded, unapproved asset may be reviewed only inside this Editor fixture.
                texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.LoadImage(File.ReadAllBytes(Path.Combine(Folder,"hot-drink.png")));
                texture.filterMode=FilterMode.Point;texture.wrapMode=TextureWrapMode.Clamp;
                HotDrinkSteamDetail.ReviewSprite=Sprite.Create(texture,new Rect(0,0,37,37),new Vector2(.5f,.5f),32);
            }
            Object.FindAnyObjectByType<CombatPrototypeBootstrap>().StartCoroutine(Routine());
        }
        private static void Restore(PlayModeStateChange s)
        {
            if(s!=PlayModeStateChange.ExitingPlayMode)return;
            Application.runInBackground=background;InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=mouse});
            if(HotDrinkSteamDetail.ReviewSprite!=null)Object.Destroy(HotDrinkSteamDetail.ReviewSprite);
            HotDrinkSteamDetail.ReviewSprite=null;if(texture!=null)Object.Destroy(texture);texture=null;
            EditorApplication.playModeStateChanged-=Restore;
        }
        private static IEnumerator Routine()
        {
            var host=Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            string error=host.PreviewUiCaptureRun(false,"B1");if(error!=null)throw new InvalidOperationException(error);
            var run=host.CurrentMapRun;
            run.CompleteCurrentCombat();run.ClaimReward("F-P-R19");run.SelectNode("EV1");run.ChooseCurrentNodeContent("FIRST-EV1-ACCEPT-DELIVERY");
            run.SelectNode("EV2");run.ChooseCurrentNodeContent(run.CurrentContentChoices[0].Id);run.SelectNode("B2");run.CompleteCurrentCombat();
            run.ClaimReward(run.CurrentFirstRunRewardIds[0]);run.SelectNode("EV3");run.ChooseCurrentNodeContent(run.CurrentContentChoices[1].Id);
            run.SelectNode("B3");run.CompleteCurrentCombat();run.ClaimReward(run.CurrentFirstRunRewardIds[0]);
            yield return new WaitForSecondsRealtime(.6f);host.SelectMapNode("M");yield return new WaitForSecondsRealtime(.6f);
            host.PreviewUiCapturePage("room","M");yield return new WaitForSecondsRealtime(.6f);
            var steam=Object.FindAnyObjectByType<HotDrinkSteamDetail>();
            if(steam==null || !steam.EmitsSteam || steam.raycastTarget)throw new InvalidOperationException("Missing available drink steam");
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=new Vector2(1910,1070)});yield return null;
            var frames=new List<Texture2D>();var times=new List<string>();
            for(int i=0;i<36;i++){yield return new WaitForEndOfFrame();frames.Add(ScreenCapture.CaptureScreenshotAsTexture());times.Add(Time.unscaledTime.ToString("R",System.Globalization.CultureInfo.InvariantCulture));yield return new WaitForSecondsRealtime(.035f);}
            for(int i=0;i<frames.Count;i++){File.WriteAllBytes(Path.Combine(Folder,"steam-"+i.ToString("00")+".png"),frames[i].EncodeToPNG());Object.Destroy(frames[i]);}
            File.WriteAllLines(Path.Combine(Folder,"frame-times.txt"),times);
            var prefs=host.UiPreferences;float old=prefs.AnimationIntensity;
            prefs.Configure(prefs.MasterVolume,0,prefs.ScreenShake,prefs.FloatingText,prefs.HighContrast,prefs.LargeText,prefs.KeyHints);yield return null;yield return null;
            if(steam.EmitsSteam)throw new InvalidOperationException("Animation zero still emits");yield return Capture("animation-zero.png");
            prefs.Configure(prefs.MasterVolume,old,prefs.ScreenShake,prefs.FloatingText,prefs.HighContrast,prefs.LargeText,prefs.KeyHints);
            int contribution=run.StageContribution;yield return Click("按钮_餐食_MEAL-AETHER");yield return new WaitForSecondsRealtime(.3f);
            steam=Object.FindAnyObjectByType<HotDrinkSteamDetail>();
            if(steam==null || steam.EmitsSteam || !host.CurrentMapRun.CurrentMedicalService.MealUsed || host.CurrentMapRun.StageContribution!=contribution-1)throw new InvalidOperationException("Meal result did not stop supply");
            yield return Capture("purchased.png");yield return Click("按钮_返回地图");yield return new WaitForSecondsRealtime(.3f);
            if(Object.FindAnyObjectByType<HotDrinkSteamDetail>()!=null)throw new InvalidOperationException("Steam survived page exit");
            host.PreviewUiCapturePage("room","M");yield return new WaitForSecondsRealtime(.3f);
            steam=Object.FindAnyObjectByType<HotDrinkSteamDetail>();if(steam==null || steam.EmitsSteam)throw new InvalidOperationException("Reopened meal supply replayed");
            yield return Capture("reopened.png");
            AudioExpansionVerification.RequireClip("meal");
            File.WriteAllText(Path.Combine(Folder,"result.txt"),"PASS: disposable in-memory journey prepared through model battle completion; real Input System clicks on hot drink and return, top raycast hit buttons; contribution spent once, chosen meal recorded; sold/animation-zero/exit/reopen stop steam. Player saves and preference saves untouched. Formal resource preferred; see formal-resource.txt if present. Review fallback only when formal asset is missing.");
        }
        private static IEnumerator Click(string name)
        {
            // Medical page intentionally has both a top bar and footer return button.
            var choices=Object.FindObjectsByType<Button>().Where(b=>b.isActiveAndEnabled && b.IsInteractable() && b.name==name).ToArray();
            if(choices.Length==0 || (choices.Length>1 && name!="按钮_返回地图"))throw new InvalidOperationException("Ambiguous/missing button: "+name);
            var b=choices.OrderByDescending(b=>RectTransformUtility.WorldToScreenPoint(null,b.transform.position).y).First();
            var r=(RectTransform)b.transform;Vector2 p=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));
            if(name=="按钮_餐食_MEAL-AETHER")p=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(new Vector3(77,-29)));
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=p},hits);
            if(hits.Count==0 || hits[0].gameObject.GetComponentInParent<Button>()!=b)throw new InvalidOperationException("Blocked "+name);
            File.AppendAllText(Path.Combine(Folder,"clicks.txt"),name+" hit="+hits[0].gameObject.name+"\n");
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=p});yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=p}.WithButton(MouseButton.Left));yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=p});yield return null;
        }
        private static IEnumerator Capture(string name)
        {
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=new Vector2(1910,1070)});yield return null;yield return null;yield return new WaitForEndOfFrame();
            var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Folder,name),t.EncodeToPNG());Object.Destroy(t);
        }
    }
}
