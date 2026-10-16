using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OCC.Combat.Presentation;
using OCC.Combat.Roguelite;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

namespace OCC.Combat.EditorTools
{
    public static class WorkshopMaterialVerification
    {
        private static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-04_小细节第六批工坊材质/验证"));
        private static CombatPrototypeBootstrap host;private static float motion;
        [MenuItem("OCC/Details60/Workshop Material/Record")]
        public static void Record()
        {
            if(!Application.isPlaying || Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Main Play required");
            Directory.CreateDirectory(Folder);File.Delete(Path.Combine(Folder,"result.txt"));
            host=Object.FindAnyObjectByType<CombatPrototypeBootstrap>();motion=host.UiPreferences.AnimationIntensity;
            DetailsInputFixture.Begin(Application.runInBackground);EditorApplication.playModeStateChanged-=Restore;EditorApplication.playModeStateChanged+=Restore;
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();host.StartCoroutine(Guard(Routine()));
        }
        private static void Motion(float v){var p=host.UiPreferences;p.Configure(p.MasterVolume,v,p.ScreenShake,p.FloatingText,p.HighContrast,p.LargeText,p.KeyHints);}
        private static void Finish(){if(host!=null)Motion(motion);DetailsInputFixture.End();}
        private static void Restore(PlayModeStateChange s){if(s!=PlayModeStateChange.ExitingPlayMode)return;Finish();EditorApplication.playModeStateChanged-=Restore;}
        private static IEnumerator Guard(IEnumerator root)
        {
            var stack=new Stack<IEnumerator>();stack.Push(root);
            while(stack.Count>0){bool moved=false;object value=null;Exception error=null;
                try{moved=stack.Peek().MoveNext();if(moved)value=stack.Peek().Current;}catch(Exception e){error=e;}
                if(error!=null){File.WriteAllText(Path.Combine(Folder,"result.txt"),"FAIL: "+error);Finish();Debug.LogException(error);yield break;}
                if(!moved){stack.Pop();continue;}if(value is IEnumerator child){stack.Push(child);continue;}yield return value;}
        }
        private static void Pointer(Vector2 p,bool down=false)=>InputSystem.QueueStateEvent(Mouse.current,down?new MouseState{position=p}.WithButton(MouseButton.Left):new MouseState{position=p});
        private static IEnumerator Click(string name)
        {
            yield return new WaitForEndOfFrame();var b=Object.FindObjectsByType<Button>().Single(x=>x.isActiveAndEnabled && x.IsInteractable() && x.name==name);
            var r=(RectTransform)b.transform;Vector2 p=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=p},hits);
            if(hits.Count==0 || hits[0].gameObject.GetComponentInParent<Button>()!=b)throw new InvalidOperationException("Blocked "+name);
            File.AppendAllText(Path.Combine(Folder,"clicks.txt"),name+" hit="+hits[0].gameObject.name+"\n");
            Pointer(p);yield return null;Pointer(p,true);yield return null;Pointer(p);yield return new WaitForSecondsRealtime(.18f);
            Pointer(new Vector2(1908,70));yield return null;
        }
        private static Image Target()=>Object.FindObjectsByType<Image>().Single(x=>x.isActiveAndEnabled && x.name=="目标图标");
        private static IEnumerator Shot(string name,Image crop=null)
        {
            yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();
            if(crop!=null){var c=new Vector3[4];crop.rectTransform.GetWorldCorners(c);var image=new Texture2D(Mathf.RoundToInt(c[2].x-c[0].x),Mathf.RoundToInt(c[2].y-c[0].y),TextureFormat.RGB24,false);image.SetPixels(t.GetPixels(Mathf.RoundToInt(c[0].x),Mathf.RoundToInt(c[0].y),image.width,image.height));image.Apply();Object.Destroy(t);t=image;}
            File.WriteAllBytes(Path.Combine(Folder,name+".png"),t.EncodeToPNG());Object.Destroy(t);
        }
        private static void Warm(Image target,float value){target.material.SetFloat("_WorkshopWarm",value);target.materialForRendering.SetFloat("_WorkshopWarm",value);}
        private static IEnumerator Routine()
        {
            string error=host.PreviewUiCaptureRun(false,"B1");if(error!=null)throw new InvalidOperationException(error);
            var run=host.CurrentMapRun;
            // Isolated preparation, not an end-to-end played journey or natural rare-item acquisition.
            run.CompleteCurrentCombat();run.ClaimReward("F-P-R19");run.SelectNode("EV1");run.ChooseCurrentNodeContent("FIRST-EV1-ACCEPT-DELIVERY");
            run.SelectNode("EV2");run.ChooseCurrentNodeContent(run.CurrentContentChoices[0].Id);run.SelectNode("B2");run.CompleteCurrentCombat();
            run.ClaimReward(run.CurrentFirstRunRewardIds[0]);run.SelectNode("EV3");run.ChooseCurrentNodeContent(run.CurrentContentChoices[1].Id);
            var runtime=RogueEquipmentRuntime.FromDto(run.RogueRunState);
            if(!runtime.TryAddEquipmentFromLoot("vfx-metal-sample","ACA-EQ-CH03","reward"))throw new InvalidOperationException("Metal sample cannot fit legal inventory");
            runtime.WriteToDto(run.RogueRunState);
            yield return new WaitForSecondsRealtime(.5f);host.SelectMapNode("W");yield return new WaitForSecondsRealtime(.4f);host.PreviewUiCapturePage("room","W");yield return new WaitForSecondsRealtime(.3f);
            yield return Click("页签_装备锻造");yield return Click("工坊卡_ACA-EQ-CH01");
            yield return new WaitForSecondsRealtime(.3f);
            var cloth=Target();if(cloth.GetComponent<OccItemSurface>()!=null)throw new InvalidOperationException("Cloth got metallic warm surface");
            yield return Shot("cloth",cloth);yield return Shot("cloth-context");
            yield return Click("工坊卡_ACA-EQ-CH03");var metal=Target();var surface=metal.GetComponent<OccItemSurface>();
            yield return new WaitForSecondsRealtime(.3f);
            if(surface==null || metal.raycastTarget || metal.material.GetFloat("_WorkshopWarm")<=0)throw new InvalidOperationException("Metal reflection absent");
            var material=metal.material;float warm=material.GetFloat("_WorkshopWarm");
            yield return Shot("metal-context");yield return Shot("metal-warm",metal);
            Warm(metal,0);yield return Shot("metal-neutral",metal);Warm(metal,warm);yield return Shot("metal-restored",metal);
            Motion(0);yield return null;yield return Shot("metal-animation-zero",metal);
            if(material.GetFloat("_Phase")>=0)throw new InvalidOperationException("Static warm light unexpectedly animates");Motion(motion);
            yield return Click("页签_术式专精");yield return new WaitForSecondsRealtime(.1f);
            if(Object.FindAnyObjectByType<OccItemSurface>()!=null)throw new InvalidOperationException("Warm material leaked to spell page");
            yield return Click("页签_装备锻造");yield return Click("工坊卡_ACA-EQ-CH01");
            if(Target().GetComponent<OccItemSurface>()!=null)throw new InvalidOperationException("Metal surface carried into cloth");
            yield return Click("工坊卡_ACA-EQ-CH03");yield return Click("按钮_确认安装");yield return new WaitForSecondsRealtime(.3f);
            if(!run.CurrentWorkshopService.ForgeCompleted || run.CurrentWorkshopService.ForgedTargetId!="vfx-metal-sample")throw new InvalidOperationException("Actual forge commit absent");
            yield return Shot("committed-context");yield return Shot("committed-metal",Target());
            yield return Click("按钮_返回地图");yield return new WaitForSecondsRealtime(.2f);
            if(Object.FindAnyObjectByType<OccItemSurface>()!=null)throw new InvalidOperationException("Surface survived actual page exit");
            host.PreviewUiCapturePage("room","W");yield return new WaitForSecondsRealtime(.3f);
            yield return Shot("reopened-metal",Target());
            File.WriteAllText(Path.Combine(Folder,"result.txt"),"PASS: real Input System target/tab/forge/back clicks. Cloth has no metallic surface; metal warm light restored and stays static at animation zero. Switching targets/tabs and leaving clear old material owners; real forge commits to selected sample and reopening retains local surface. Metal rare item was added via legal inventory API to disposable memory preview; progression prepared by model, not played; no natural acquisition, player disk-save or full journey verification. Pixel comparison separate.");Finish();
        }
    }
}
