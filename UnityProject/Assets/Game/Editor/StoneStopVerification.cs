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
    public static class StoneStopVerification
    {
        private static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-03_小细节第五批石路停步/验证"));
        private static CombatPrototypeBootstrap host;
        private static float originalMotion;
        private static readonly List<Texture2D> frames=new List<Texture2D>();
        [MenuItem("OCC/Details60/Stone Stop/Record")]
        public static void Record()
        {
            if(!Application.isPlaying || Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Main Play required");
            Directory.CreateDirectory(Folder);File.Delete(Path.Combine(Folder,"result.txt"));
            host=Object.FindAnyObjectByType<CombatPrototypeBootstrap>();originalMotion=host.UiPreferences.AnimationIntensity;
            DetailsInputFixture.Begin(Application.runInBackground);
            EditorApplication.playModeStateChanged-=Restore;EditorApplication.playModeStateChanged+=Restore;
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            host.StartCoroutine(Guard(Routine()));
        }
        private static void Restore(PlayModeStateChange state){if(state!=PlayModeStateChange.ExitingPlayMode)return;Finish();EditorApplication.playModeStateChanged-=Restore;}
        private static void Motion(float value){var p=host.UiPreferences;p.Configure(p.MasterVolume,value,p.ScreenShake,p.FloatingText,p.HighContrast,p.LargeText,p.KeyHints);}
        private static void Finish(){if(host!=null)Motion(originalMotion);DetailsInputFixture.End();foreach(var t in frames)if(t!=null)Object.Destroy(t);frames.Clear();}
        private static IEnumerator Guard(IEnumerator routine)
        {
            var stack=new Stack<IEnumerator>();stack.Push(routine);
            while(stack.Count>0)
            {
                bool moved=false;object value=null;Exception error=null;
                try{moved=stack.Peek().MoveNext();if(moved)value=stack.Peek().Current;}catch(Exception e){error=e;}
                if(error!=null){File.WriteAllText(Path.Combine(Folder,"result.txt"),"FAIL: "+error);Finish();Debug.LogException(error);yield break;}
                if(!moved){stack.Pop();continue;}if(value is IEnumerator child){stack.Push(child);continue;}yield return value;
            }
        }
        private static Vector2 Center(RectTransform r)=>RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));
        private static void Pointer(Vector2 p,bool down=false)=>InputSystem.QueueStateEvent(Mouse.current,down?new MouseState{position=p}.WithButton(MouseButton.Left):new MouseState{position=p});
        private static IEnumerator Click(Vector2 p)
        {
            Pointer(p);yield return null;Pointer(p,true);yield return null;Pointer(p);yield return null;
        }
        private static IEnumerator Button(string name)
        {
            yield return new WaitForEndOfFrame();var b=Object.FindObjectsByType<Button>().Single(x=>x.isActiveAndEnabled && x.name==name && x.IsInteractable());
            Vector2 p=Center((RectTransform)b.transform);var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=p},hits);
            if(hits.Count==0 || hits[0].gameObject.GetComponentInParent<Button>()!=b)throw new InvalidOperationException("Blocked button "+name);
            yield return Click(p);
        }
        private static IEnumerator Routine()
        {
            string error=host.PreviewUiCaptureRun(false,"B1");if(error!=null)throw new InvalidOperationException(error);
            yield return new WaitForSecondsRealtime(.6f);yield return Button("map.node.B1");
            yield return new WaitForSecondsRealtime(.5f);yield return Button("按钮_进入战斗");
            yield return new WaitForSecondsRealtime(2.5f);
            float deadline=Time.unscaledTime+12f;
            while(Object.FindObjectsByType<RectTransform>().Any(x=>x.name=="战斗入场演出" && x.gameObject.activeInHierarchy))
            {
                if(Time.unscaledTime>deadline)throw new InvalidOperationException("Entry sequence did not finish");
                yield return new WaitForSecondsRealtime(.05f);
            }
            var hero=host.CurrentState.GetUnit("hero");var origin=hero.Position;
            var candidates=new List<GridPosition>();var traces=new List<string>();
            foreach(var graphic in Object.FindObjectsByType<CombatStopDustGraphic>())
            {
                var parent=graphic.transform.parent;var name=parent.name.Split('_');if(name.Length!=3)continue;
                var p=new GridPosition(int.Parse(name[1]),int.Parse(name[2]));
                var floor=parent.GetComponentsInChildren<RawImage>().FirstOrDefault(x=>x.name=="地面");
                string texture=floor?.texture?.name ?? "none";traces.Add(p+" "+texture+" legal="+host.CanQuickMoveTo(p));
                var prop=parent.GetComponentsInChildren<RawImage>(true).FirstOrDefault(x=>x.name=="地形物件");
                if(prop!=null && prop.gameObject.activeInHierarchy)continue;
                var tile=host.CurrentState.Map.GetTile(p);
                if(tile.Cover!=CoverType.None || tile.IsLootChest || tile.IsObjective || tile.IsDevice ||
                    host.CurrentState.Loot?.Position==p || host.CurrentState.LootSource?.Position==p)continue;
                if(p.ManhattanDistance(origin)>=2 && host.CanQuickMoveTo(p) &&
                    (texture.StartsWith("academy_block_road_") || texture.StartsWith("academy_block_court_")) && !host.CurrentState.Map.GetTile(p).IsWater)candidates.Add(p);
            }
            File.WriteAllLines(Path.Combine(Folder,"candidates.txt"),traces);
            if(candidates.Count==0)throw new InvalidOperationException("No actual reachable dry stone stop");
            GridPosition target=default;RectTransform root=null;Vector2 point=default;
            foreach(var candidate in candidates.OrderBy(p=>p.ManhattanDistance(origin)).ThenBy(p=>p.Y).ThenBy(p=>p.X))
            {
                var candidateRoot=Object.FindObjectsByType<RectTransform>().Single(r=>r.name=="格子_"+candidate.X+"_"+candidate.Y && r.gameObject.activeInHierarchy);
                Vector2 center=Center(candidateRoot);var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=center},hits);
                File.AppendAllText(Path.Combine(Folder,"hit-candidates.txt"),candidate+" screen="+center+" hit="+(hits.Count>0?hits[0].gameObject.name:"none")+"\n");
                if(hits.Count==0 || !hits[0].gameObject.transform.IsChildOf(candidateRoot))continue;
                target=candidate;root=candidateRoot;point=center;break;
            }
            if(root==null)throw new InvalidOperationException("No actually hit visible stone cell");
            yield return Click(point);yield return new WaitForSecondsRealtime(.04f);yield return Click(point);
            Pointer(new Vector2(1910,1070));int peak=0,meshPeak=0;var times=new List<string>();
            for(int i=0;i<28;i++)
            {
                yield return new WaitForEndOfFrame();peak=Math.Max(peak,host.StopDust.Stops.Count);
                var graphic=root.GetComponentInChildren<CombatStopDustGraphic>();var mesh=graphic.canvasRenderer.GetMesh();if(mesh!=null)meshPeak=Math.Max(meshPeak,mesh.vertexCount);
                frames.Add(ScreenCapture.CaptureScreenshotAsTexture());times.Add(Time.unscaledTime.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+" stops="+host.StopDust.Stops.Count);
                yield return new WaitForSecondsRealtime(.018f);
            }
            File.WriteAllLines(Path.Combine(Folder,"times.txt"),times);
            for(int i=0;i<frames.Count;i++){File.WriteAllBytes(Path.Combine(Folder,"frame-"+i.ToString("00")+".png"),frames[i].EncodeToPNG());Object.Destroy(frames[i]);}frames.Clear();
            var corners=new Vector3[4];root.GetWorldCorners(corners);
            File.WriteAllText(Path.Combine(Folder,"motion.txt"),"from="+origin+" target="+target+" final="+hero.Position+" peak="+peak+" meshVertices="+meshPeak+" screen="+Screen.width+"x"+Screen.height+"\nrect="+string.Join(";",corners.Select(v=>v.ToString())));
            if(hero.Position!=target || peak!=1 || meshPeak!=16)throw new InvalidOperationException("Real stop or rendered four-mote mesh absent");
            if(host.StopDust.Stops.Count!=0)throw new InvalidOperationException("Dust lifetime failed");
            if(Object.FindObjectsByType<CombatStopDustGraphic>().Any(x=>x.raycastTarget))throw new InvalidOperationException("Dust intercepts input");
            host.StopDust.Add(target,Vector2.right,Time.unscaledTime);Motion(0);yield return null;yield return null;
            if(host.StopDust.Stops.Count!=0)throw new InvalidOperationException("Animation zero leaves dust");
            Motion(originalMotion);host.StopDust.Add(target,Vector2.right,Time.unscaledTime);
            var feedback=Object.FindAnyObjectByType<CombatVisualFeedback>();feedback.enabled=false;
            if(host.StopDust.Stops.Count!=0)throw new InvalidOperationException("Disabled feedback leaves dust");feedback.enabled=true;
            host.StopDust.Add(target,Vector2.right,Time.unscaledTime);
            var view=Object.FindAnyObjectByType<FormalBattlefieldView>();view.enabled=false;
            if(host.StopDust.Stops.Count!=0)throw new InvalidOperationException("Disabled view leaves dust");view.enabled=true;
            host.StopDust.Add(target,Vector2.right,Time.unscaledTime);
            feedback.ResetBattleFeedback();yield return null;yield return null;
            if(host.StopDust.Stops.Count!=0)throw new InvalidOperationException("Hidden battlefield leaves dust");
            File.WriteAllText(Path.Combine(Folder,"result.txt"),"PASS: actual map node and departure buttons, actual legal bare-stone move of at least two cells and rendered four-mote mesh at final cell. Lifetime, no raycasts, animation zero, feedback disable, view disable and ResetBattleFeedback cleanup verified. Disposable memory journey; no player save writes. Cleanup uses injected presentation only, not a fake move. Normal post-battle return-to-map and completed settlement journey not verified.");Finish();
        }
    }
}
