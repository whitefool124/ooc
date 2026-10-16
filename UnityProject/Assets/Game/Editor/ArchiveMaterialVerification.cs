using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

namespace OCC.Combat.EditorTools
{
    public static class ArchiveMaterialVerification
    {
        private static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-03_小细节第四批图鉴材质/验证"));
        private static CombatPrototypeBootstrap host;private static float animation;private static bool background;
        [MenuItem("OCC/Details60/Archive Material/Record")]
        public static void Record()
        {
            if(!Application.isPlaying || Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Main Play required");
            DetailsInputFixture.End();background=Application.runInBackground;DetailsInputFixture.Begin(background);
            host=Object.FindAnyObjectByType<CombatPrototypeBootstrap>();animation=host.UiPreferences.AnimationIntensity;
            Directory.CreateDirectory(Folder);File.Delete(Path.Combine(Folder,"result.txt"));
            EditorApplication.playModeStateChanged-=Restore;EditorApplication.playModeStateChanged+=Restore;
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();host.StartCoroutine(Guard(Routine()));
        }
        private static void Restore(PlayModeStateChange state){if(state!=PlayModeStateChange.ExitingPlayMode)return;Motion(animation);DetailsInputFixture.End();EditorApplication.playModeStateChanged-=Restore;}
        private static void Motion(float v){if(host==null)return;var p=host.UiPreferences;p.Configure(p.MasterVolume,v,p.ScreenShake,p.FloatingText,p.HighContrast,p.LargeText,p.KeyHints);}
        private static IEnumerator Guard(IEnumerator routine)
        {
            var stack=new Stack<IEnumerator>();stack.Push(routine);
            while(stack.Count>0)
            {
                object current=null;bool moved=false;Exception failure=null;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}catch(Exception e){failure=e;}
                if(failure!=null){File.WriteAllText(Path.Combine(Folder,"result.txt"),"FAIL: "+failure);Motion(animation);DetailsInputFixture.End();Debug.LogException(failure);yield break;}
                if(!moved){stack.Pop();continue;}if(current is IEnumerator child){stack.Push(child);continue;}yield return current;
            }
        }
        private static void Pointer(Vector2 p,bool down=false)=>InputSystem.QueueStateEvent(Mouse.current,down?new MouseState{position=p}.WithButton(MouseButton.Left):new MouseState{position=p});
        private static Vector2 Center(RectTransform r)=>RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));
        private static IEnumerator Click(string name)
        {
            yield return new WaitForEndOfFrame();var button=Object.FindObjectsByType<Button>().Single(x=>x.isActiveAndEnabled && x.name==name);
            Vector2 p=Center((RectTransform)button.transform);var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=p},hits);
            if(hits.Count==0 || hits[0].gameObject.GetComponentInParent<Button>()!=button)throw new InvalidOperationException("Blocked category "+name);
            Pointer(p);yield return null;Pointer(p,true);yield return null;Pointer(p);yield return new WaitForSecondsRealtime(.12f);
        }
        private static Texture2D Crop(Image artwork)
        {
            var corners=new Vector3[4];artwork.rectTransform.GetWorldCorners(corners);
            int x=Mathf.RoundToInt(corners[0].x),y=Mathf.RoundToInt(corners[0].y),w=Mathf.RoundToInt(corners[2].x-corners[0].x),h=Mathf.RoundToInt(corners[2].y-corners[0].y);
            var screen=ScreenCapture.CaptureScreenshotAsTexture();var crop=new Texture2D(w,h,TextureFormat.RGB24,false);
            crop.SetPixels(screen.GetPixels(x,y,w,h));crop.Apply();Object.Destroy(screen);return crop;
        }
        private static IEnumerator Routine()
        {
            string error=host.PreviewUiCaptureRun(false,"S");if(error!=null)throw new InvalidOperationException(error);
            yield return new WaitForSecondsRealtime(.4f);host.PreviewUiCapturePage("encyclopedia");yield return new WaitForSecondsRealtime(.3f);
            yield return Click("按钮_百科_装备");
            foreach(string name in new[]{"aca_eq_ch03","aca_eq_ch01","aca_eq_hd01"})
            {
                var surface=Object.FindObjectsByType<OccItemSurface>().Single(x=>x.Artwork.sprite.name==name);
                var image=surface.Artwork;var scroll=image.GetComponentInParent<ScrollRect>();
                var card=(RectTransform)surface.transform;
                scroll.StopMovement();scroll.content.anchoredPosition=new Vector2(0,Mathf.Clamp(-card.anchoredPosition.y-40,0,scroll.content.rect.height-scroll.viewport.rect.height));
                Pointer(new Vector2(1908,70));yield return new WaitForSecondsRealtime(.12f);yield return new WaitForEndOfFrame();
                var context=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Folder,name+"-context.png"),context.EncodeToPNG());Object.Destroy(context);
                float side=image.material.GetFloat("_SideLight");
                image.material.SetFloat("_SideLight",0);image.materialForRendering.SetFloat("_SideLight",0);yield return new WaitForEndOfFrame();
                var unlit=Crop(image);File.WriteAllBytes(Path.Combine(Folder,name+"-unlit.png"),unlit.EncodeToPNG());Object.Destroy(unlit);
                image.material.SetFloat("_SideLight",side);image.materialForRendering.SetFloat("_SideLight",side);yield return new WaitForEndOfFrame();
                var lit=Crop(image);File.WriteAllBytes(Path.Combine(Folder,name+"-lit.png"),lit.EncodeToPNG());Object.Destroy(lit);
                Pointer(Center(image.rectTransform));yield return null;yield return null;
                var frames=new List<Texture2D>();var trace=new List<string>();bool active=false;
                for(int i=0;i<22;i++)
                {
                    yield return new WaitForEndOfFrame();float phase=image.material.GetFloat("_Phase"),rendered=image.materialForRendering.GetFloat("_Phase");
                    if(Mathf.Abs(phase-rendered)>.0001f)throw new InvalidOperationException("Stencil clone phase stale");
                    active|=phase>=0;frames.Add(Crop(image));trace.Add(Time.unscaledTime.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+" phase="+phase+" rendered="+rendered);
                    yield return new WaitForSecondsRealtime(.018f);
                }
                if(!active)throw new InvalidOperationException("Real hover did not arrive at "+name);
                for(int i=0;i<frames.Count;i++){File.WriteAllBytes(Path.Combine(Folder,name+"-"+i.ToString("00")+".png"),frames[i].EncodeToPNG());Object.Destroy(frames[i]);}
                File.WriteAllLines(Path.Combine(Folder,name+"-times.txt"),trace);
                Motion(0);Pointer(new Vector2(1908,70));yield return null;Pointer(Center(image.rectTransform));yield return null;yield return null;
                if(image.materialForRendering.GetFloat("_Phase")>=0)throw new InvalidOperationException("Zero animation did not stop rendered phase");
                Motion(animation);Pointer(new Vector2(1908,70));yield return null;
                if(image.materialForRendering.GetFloat("_Phase")>=0)throw new InvalidOperationException("Hover exit left phase active");
            }
            yield return Click("按钮_百科_术式");yield return new WaitForSecondsRealtime(.12f);
            if(Object.FindObjectsByType<OccItemSurface>().Length!=0)throw new InvalidOperationException("Old equipment material owner leaked across category");
            host.PreviewUiCapturePage("map");yield return new WaitForSecondsRealtime(.2f);
            if(Object.FindAnyObjectByType<OccItemSurface>()!=null)throw new InvalidOperationException("Material owner leaked across page");
            File.WriteAllText(Path.Combine(Folder,"result.txt"),"PASS: real equipment/spell category clicks, actual pointer hover on three real icons under Mask; 22 buffered runtime crops per material; rendered stencil phase equals source, zero animation stops phase, exit clears and category/page clean up. Static side-light comparisons included. Disposable memory preview, no player save writes; visual pixel evidence evaluated separately.");
            DetailsInputFixture.End();
        }
    }
}
