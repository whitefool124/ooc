using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OCC.Combat.Presentation;
using OCC.Combat.Roguelite;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace OCC.Combat.EditorTools
{
    public static class AudioInventoryNegativeVerification
    {
        private static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-03_材质音效与音效补全/negative"));
        private static bool background;
        private static Vector2 mouse;
        [MenuItem("OCC/Audio/Negative/Record")]
        public static void Record()
        {
            if(!Application.isPlaying || Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Main project Play required");
            Directory.CreateDirectory(Folder);AudioExpansionVerification.StartLedger(Folder);background=Application.runInBackground;mouse=Mouse.current.position.ReadValue();Application.runInBackground=true;
            EditorApplication.playModeStateChanged-=Restore;EditorApplication.playModeStateChanged+=Restore;
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            Object.FindAnyObjectByType<CombatPrototypeBootstrap>().StartCoroutine(Routine());
        }
        private static void Restore(PlayModeStateChange s)
        {
            if(s!=PlayModeStateChange.ExitingPlayMode)return;
            Application.runInBackground=background;InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=mouse});EditorApplication.playModeStateChanged-=Restore;
        }
        private static RogueEquipmentRuntime Runtime(CombatPrototypeBootstrap host)=>RogueEquipmentRuntime.FromDto(host.CurrentMapRun.RogueRunState);
        private static void Pointer(Vector2 p,bool held)=>InputSystem.QueueStateEvent(Mouse.current,held?new MouseState{position=p}.WithButton(MouseButton.Left):new MouseState{position=p});
        private static IEnumerator Routine()
        {
            var host=Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            string error=host.PreviewUiCaptureRun(false,"B1");if(error!=null)throw new InvalidOperationException(error);
            yield return new WaitForSecondsRealtime(.6f);host.PreviewUiCapturePage("loadout");yield return new WaitForSecondsRealtime(.6f);
            var ui=Object.FindAnyObjectByType<FormalRogueliteUi>();
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var grid=(RectTransform)typeof(FormalRogueliteUi).GetField("loadoutGridRect",flags).GetValue(ui);
            float cell=(float)typeof(FormalRogueliteUi).GetField("loadoutCellSize",flags).GetValue(ui);
            var runtime=Runtime(host);
            if(runtime.Backpack.Count==0)
            {
                // Default character starts equipped. Use the actual equipped-slot drag to
                // put the existing chest piece into the backpack, without injecting items.
                var view=Object.FindAnyObjectByType<RogueLoadoutEquipmentView>();
                int chestIndex=RogueInventoryGridSystem.EquipmentSlots.ToList().IndexOf(OCC.Combat.Roguelite.EquipmentSlot.Chest);
                var root=view.EquipmentSlots[chestIndex].Root;
                Vector2 source=RectTransformUtility.WorldToScreenPoint(null,root.TransformPoint(root.rect.center));
                Vector2 destination=RectTransformUtility.WorldToScreenPoint(null,grid.TransformPoint(new Vector3(grid.rect.xMin+cell*.35f,grid.rect.yMax-cell*.35f)));
                Pointer(source,false);yield return null;Pointer(source,true);yield return null;Pointer(source+new Vector2(12,0),true);yield return null;
                Pointer(destination,true);yield return null;yield return null;Pointer(destination,false);yield return new WaitForSecondsRealtime(.4f);
                runtime=Runtime(host);grid=(RectTransform)typeof(FormalRogueliteUi).GetField("loadoutGridRect",flags).GetValue(ui);
            }
            var items=runtime.Backpack.Keys.Select(id=>new RogueInventoryItemPresentation(runtime,id)).OrderByDescending(i=>i.IsEquipment).ToArray();
            if(items.Length==0)throw new InvalidOperationException("Default journey backpack empty");
            var item=items[0];int tx=-1,ty=-1;
            for(int y=0;y<runtime.BackpackRows && tx<0;y++)for(int x=0;x<runtime.BackpackColumns;x++)
                if((x!=item.X || y!=item.Y) && runtime.CanMoveBackpack(item.InstanceId,x,y,item.Rotated)){tx=x;ty=y;break;}
            if(tx<0)throw new InvalidOperationException("No legal destination");
            Func<int,int,Vector2> point=(x,y)=>RectTransformUtility.WorldToScreenPoint(null,grid.TransformPoint(new Vector3(grid.rect.xMin+(x+.35f)*cell,grid.rect.yMax-(y+.35f)*cell)));
            Vector2 from=point(item.X,item.Y),to=point(tx,ty);
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=from},hits);
            if(hits.Count==0 || hits[0].gameObject.GetComponentInParent<Button>()?.name!="背包物品_"+item.InstanceId)throw new InvalidOperationException("Source blocked: "+string.Join(",",hits.Select(h=>h.gameObject.name)));
            File.WriteAllText(Path.Combine(Folder,"input.txt"),"source="+item.InstanceId+" hit="+hits[0].gameObject.name+" cell="+cell+" from="+from+" to="+to);
            yield return Capture("before.png");
            Pointer(from,false);yield return null;Pointer(from,true);yield return null;Pointer(from+new Vector2(12,0),true);yield return null;yield return null;
            var detail=Object.FindAnyObjectByType<InventoryLiftDetail>();
            if(detail==null || detail.transform.Find("实际物件").GetComponent<Image>().raycastTarget)throw new InvalidOperationException("Real item missing or intercepts input");
            var frames=new List<Texture2D>();var times=new List<string>();
            for(int i=0;i<28;i++)
            {
                if(i==10)Pointer(to,true);
                if(i==19)Pointer(to,false);
                yield return new WaitForEndOfFrame();frames.Add(ScreenCapture.CaptureScreenshotAsTexture());times.Add(Time.unscaledTime.ToString("R",System.Globalization.CultureInfo.InvariantCulture));
                yield return new WaitForSecondsRealtime(.025f);
            }
            for(int i=0;i<frames.Count;i++){File.WriteAllBytes(Path.Combine(Folder,"drag-"+i.ToString("00")+".png"),frames[i].EncodeToPNG());Object.Destroy(frames[i]);}
            File.WriteAllLines(Path.Combine(Folder,"frame-times.txt"),times);
            runtime=Runtime(host);var moved=runtime.Backpack[item.InstanceId];
            if(moved.X!=tx || moved.Y!=ty || Object.FindAnyObjectByType<InventoryLiftDetail>()!=null)throw new InvalidOperationException("Drop failed or effect leaked");
            yield return Capture("placed.png");
            int committedDrops=AudioExpansionVerification.Count("cloth_drop");
            // A 2x3 chest at the final cell extends beyond capacity: red preview,
            // unsuccessful release, unchanged model and no settlement effect.
            grid=(RectTransform)typeof(FormalRogueliteUi).GetField("loadoutGridRect",flags).GetValue(ui);
            from=point(tx,ty);Vector2 illegal=point(runtime.BackpackColumns-1,runtime.BackpackRows-1);
            Pointer(from,false);yield return null;Pointer(from,true);yield return null;Pointer(from+new Vector2(12,0),true);yield return null;
            Pointer(illegal,true);yield return null;yield return null;
            var ghost=GameObject.Find("战外背包拖拽预览");
            if(ghost==null || ghost.GetComponent<Image>().color.r<=ghost.GetComponent<Image>().color.g)throw new InvalidOperationException("Invalid preview not red");
            yield return Capture("invalid-preview.png");Pointer(illegal,false);yield return new WaitForSecondsRealtime(.3f);
            moved=Runtime(host).Backpack[item.InstanceId];if(moved.X!=tx || moved.Y!=ty || Object.FindAnyObjectByType<InventoryLiftDetail>()!=null)throw new InvalidOperationException("Invalid drop changed placement or settled");
            // Actual pointer cancellation outside the grid must preserve the committed placement.
            grid=(RectTransform)typeof(FormalRogueliteUi).GetField("loadoutGridRect",flags).GetValue(ui);
            from=point(tx,ty);Pointer(from,false);yield return null;Pointer(from,true);yield return null;Pointer(from+new Vector2(12,0),true);yield return null;
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.R));yield return null;yield return null;
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
            detail=Object.FindAnyObjectByType<InventoryLiftDetail>();
            if(detail==null || Mathf.Abs(Mathf.DeltaAngle(detail.transform.Find("实际物件").localEulerAngles.z,-90))>.1f)throw new InvalidOperationException("Rotated preview icon did not follow actual R input");
            yield return Capture("rotated-preview.png");
            Pointer(new Vector2(1910,1070),true);yield return null;Pointer(new Vector2(1910,1070),false);yield return null;yield return new WaitForSecondsRealtime(.3f);
            moved=Runtime(host).Backpack[item.InstanceId];if(moved.X!=tx || moved.Y!=ty || Object.FindAnyObjectByType<InventoryLiftDetail>()!=null)throw new InvalidOperationException("Cancelled drag changed placement or leaked");
            if(AudioExpansionVerification.Count("cloth_drop")!=committedDrops)throw new InvalidOperationException("Rejected/cancelled drag played success");
            var prefs=host.UiPreferences;float old=prefs.AnimationIntensity;
            prefs.Configure(prefs.MasterVolume,0,prefs.ScreenShake,prefs.FloatingText,prefs.HighContrast,prefs.LargeText,prefs.KeyHints);
            // Page rebuilt on cancellation; resolve the grid again before zero-motion drag.
            grid=(RectTransform)typeof(FormalRogueliteUi).GetField("loadoutGridRect",flags).GetValue(ui);from=point(tx,ty);
            Pointer(from,false);yield return null;Pointer(from,true);yield return null;Pointer(from+new Vector2(12,0),true);yield return null;yield return null;
            detail=Object.FindAnyObjectByType<InventoryLiftDetail>();
            if(detail==null || detail.transform.Find("接触影").GetComponent<Image>().enabled || detail.transform.Find("实际物件").GetComponent<RectTransform>().anchoredPosition!=Vector2.zero)throw new InvalidOperationException("Zero motion still animated");
            yield return Capture("animation-zero.png");
            host.PreviewUiCapturePage("map");yield return new WaitForSecondsRealtime(.3f);Pointer(from,false);
            if(Object.FindAnyObjectByType<InventoryLiftDetail>()!=null)throw new InvalidOperationException("Detail survived page exit");
            prefs.Configure(prefs.MasterVolume,old,prefs.ScreenShake,prefs.FloatingText,prefs.HighContrast,prefs.LargeText,prefs.KeyHints);
            File.WriteAllText(Path.Combine(Folder,"result.txt"),"PASS: default disposable journey inventory; real Input System unequip chest/source raycast/drag/legal release and placement committed; invalid red drop and outside cancel preserve placement; actual R input rotates the real preview icon; real icon never raycasts; zero motion hides shadow and lift; page exit cleanup. Existing formal item sprites reused. Player saves and preferences untouched. No full combat journey claim.");
        }
        private static IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Folder,name),t.EncodeToPNG());Object.Destroy(t);
        }
    }
}
