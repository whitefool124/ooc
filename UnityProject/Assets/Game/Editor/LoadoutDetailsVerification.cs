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
using Object = UnityEngine.Object;
using GearSlot = OCC.Combat.Roguelite.EquipmentSlot;

namespace OCC.Combat.EditorTools
{
    public static class LoadoutDetailsVerification
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-03_小细节第三批装备关联/验证"));
        private static CombatPrototypeBootstrap host;
        private static bool background;
        private static float animation;
        private static readonly BindingFlags Flags = BindingFlags.Instance|BindingFlags.NonPublic;
        [MenuItem("OCC/Details60/Loadout Details/Record")]
        public static void Record()
        {
            if(!Application.isPlaying || Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Main Play required");
            DetailsInputFixture.End(); background=Application.runInBackground; DetailsInputFixture.Begin(background);
            host=Object.FindAnyObjectByType<CombatPrototypeBootstrap>(); animation=host.UiPreferences.AnimationIntensity;
            Directory.CreateDirectory(Folder); File.Delete(Path.Combine(Folder,"result.txt"));
            AudioExpansionVerification.StartLedger(Folder);
            EditorApplication.playModeStateChanged-=Restore;EditorApplication.playModeStateChanged+=Restore;
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            host.StartCoroutine(Guard(Routine()));
        }
        private static void Restore(PlayModeStateChange state)
        {if(state!=PlayModeStateChange.ExitingPlayMode)return;Motion(animation);DetailsInputFixture.End();EditorApplication.playModeStateChanged-=Restore;}
        // Flatten nested iterators so their exceptions are also written to the result file.
        private static IEnumerator Guard(IEnumerator routine)
        {
            var stack=new Stack<IEnumerator>();stack.Push(routine);
            while(stack.Count>0)
            {
                object current=null;bool moved=false;Exception failure=null;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}catch(Exception e){failure=e;}
                if(failure!=null){File.WriteAllText(Path.Combine(Folder,"result.txt"),"FAIL: "+failure);Motion(animation);DetailsInputFixture.End();Debug.LogException(failure);yield break;}
                if(!moved){stack.Pop();continue;}
                if(current is IEnumerator child){stack.Push(child);continue;}yield return current;
            }
        }
        private static void Motion(float value){if(host==null)return;var p=host.UiPreferences;p.Configure(p.MasterVolume,value,p.ScreenShake,p.FloatingText,p.HighContrast,p.LargeText,p.KeyHints);}
        private static void Pointer(Vector2 p,bool down)=>InputSystem.QueueStateEvent(Mouse.current,down?new MouseState{position=p}.WithButton(MouseButton.Left):new MouseState{position=p});
        private static Vector2 Center(RectTransform r)=>RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));
        private static FormalRogueliteUi Ui()=>Object.FindAnyObjectByType<FormalRogueliteUi>();
        private static RogueEquipmentRuntime Runtime()=>RogueEquipmentRuntime.FromDto(host.CurrentMapRun.RogueRunState);
        private static RogueLoadoutEquipmentView View()=>Object.FindAnyObjectByType<RogueLoadoutEquipmentView>();
        private static RogueLoadoutSlotView Slot(GearSlot slot)=>View().EquipmentSlots[RogueInventoryGridSystem.EquipmentSlots.ToList().IndexOf(slot)];
        private static LoadoutBuckleDetail Buckle(GearSlot slot)=>Slot(slot).Root.GetComponentInChildren<LoadoutBuckleDetail>();
        private static Vector2 Cell(int x,int y)
        {
            var ui=Ui();var grid=(RectTransform)typeof(FormalRogueliteUi).GetField("loadoutGridRect",Flags).GetValue(ui);
            float size=(float)typeof(FormalRogueliteUi).GetField("loadoutCellSize",Flags).GetValue(ui);
            return RectTransformUtility.WorldToScreenPoint(null,grid.TransformPoint(new Vector3(grid.rect.xMin+(x+.35f)*size,grid.rect.yMax-(y+.35f)*size)));
        }
        private static IEnumerator Click(Button button)
        {
            var p=Center((RectTransform)button.transform);var hits=new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=p},hits);
            if(hits.Count==0 || hits[0].gameObject.GetComponentInParent<Button>()!=button)throw new InvalidOperationException("Click blocked: "+button.name);
            Pointer(p,false);yield return null;Pointer(p,true);yield return null;Pointer(p,false);yield return new WaitForSecondsRealtime(.15f);
        }
        private static IEnumerator Drag(Vector2 start,Vector2 end)
        {
            Pointer(start,false);yield return null;Pointer(start,true);yield return null;
            Pointer(start+new Vector2(12,0),true);yield return null;yield return null;
            Pointer(end,true);yield return null;yield return null;Pointer(end,false);yield return null;
        }
        private static IEnumerator Sample(string prefix,bool buckle)
        {
            Pointer(new Vector2(1908,70),false);yield return null;
            var frames=new List<Texture2D>();var trace=new List<string>();bool active=false;
            for(int i=0;i<20;i++)
            {
                yield return new WaitForEndOfFrame();
                bool moving=buckle?Buckle(GearSlot.Chest).IsAnimating:Object.FindAnyObjectByType<LoadoutLinkDetail>()!=null;
                active|=moving;frames.Add(ScreenCapture.CaptureScreenshotAsTexture());
                trace.Add(Time.unscaledTime.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+" active="+moving);
                yield return new WaitForSecondsRealtime(.015f);
            }
            for(int i=0;i<frames.Count;i++){File.WriteAllBytes(Path.Combine(Folder,prefix+"-"+i.ToString("00")+".png"),frames[i].EncodeToPNG());Object.Destroy(frames[i]);}
            File.WriteAllLines(Path.Combine(Folder,prefix+"-times.txt"),trace);
            if(!active)throw new InvalidOperationException("No actual "+prefix+" animation");
            if(Object.FindObjectsByType<LoadoutBuckleDetail>().Any(x=>x.IsAnimating) || Object.FindAnyObjectByType<LoadoutLinkDetail>()!=null)throw new InvalidOperationException("Transient did not settle");
        }
        private static IEnumerator Routine()
        {
            string error=host.PreviewUiCaptureRun(false,"S");if(error!=null)throw new InvalidOperationException(error);
            yield return new WaitForSecondsRealtime(.6f);host.PreviewUiCapturePage("room","S");yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();
            foreach(int index in new[]{0,2})
            {
                var buttons=Object.FindObjectsByType<Button>().Where(x=>x.name=="按钮_购买" || x.name=="按钮_已售罄").OrderBy(x=>x.transform.position.x).ToArray();
                yield return Click(buttons[index]);yield return new WaitForSecondsRealtime(.2f);
            }
            host.PreviewUiCapturePage("loadout");yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();
            if(Object.FindObjectsByType<LoadoutBuckleDetail>().Length!=9 || !Buckle(GearSlot.Chest).Closed)throw new InvalidOperationException("Initial buckle state incorrect");
            if(Object.FindObjectsByType<LoadoutBuckleDetail>().Any(x=>x.raycastTarget || x.IsAnimating))throw new InvalidOperationException("Opening motion or intercepted input");
            string chest=Runtime().Equipped[GearSlot.Chest];int tx=-1,ty=-1;
            for(int y=0;y<Runtime().BackpackRows && tx<0;y++)for(int x=0;x<Runtime().BackpackColumns;x++)if(Runtime().CanUnequipToBackpack(GearSlot.Chest,x,y,false)){tx=x;ty=y;break;}
            if(tx<0)throw new InvalidOperationException("No real room for chest");
            yield return Drag(Center(Slot(GearSlot.Chest).Root),Cell(tx,ty));yield return Sample("open",true);
            if(Buckle(GearSlot.Chest).Closed || !Runtime().Backpack.ContainsKey(chest))throw new InvalidOperationException("Unequip state incorrect");
            var item=Runtime().Backpack[chest];yield return Drag(Cell(item.X,item.Y),Center(Slot(GearSlot.Chest).Root));yield return Sample("close",true);
            if(!Buckle(GearSlot.Chest).Closed || Runtime().Equipped[GearSlot.Chest]!=chest)throw new InvalidOperationException("Equip failed");
            string goggles=Runtime().Backpack.Keys.Single(id=>Runtime().DefinitionFor(id)?.DefinitionId=="ACA-EQ-HD01");
            item=Runtime().Backpack[goggles];yield return Drag(Cell(item.X,item.Y),Center(Slot(GearSlot.Chest).Root));yield return new WaitForSecondsRealtime(.12f);
            if(Runtime().Equipped[GearSlot.Chest]!=chest || Object.FindObjectsByType<LoadoutBuckleDetail>().Any(x=>x.IsAnimating))throw new InvalidOperationException("Invalid slot animated");
            item=Runtime().Backpack[goggles];yield return Drag(Cell(item.X,item.Y),new Vector2(1908,1040));yield return new WaitForSecondsRealtime(.12f);
            if(Object.FindObjectsByType<LoadoutBuckleDetail>().Any(x=>x.IsAnimating) || Object.FindAnyObjectByType<LoadoutLinkDetail>()!=null)throw new InvalidOperationException("Cancelled drag animated");
            string tool=Runtime().Backpack.Keys.Single(id=>Runtime().TacticalItem(id)?.DefinitionId=="G-T08");
            item=Runtime().Backpack[tool];yield return Drag(Cell(item.X,item.Y),Center(View().TacticalSlots[0].Root));yield return Sample("link",false);
            if(Runtime().ItemQuickbarInstanceIds[0]!=tool || !Runtime().Backpack.ContainsKey(tool))throw new InvalidOperationException("Link moved item out of backpack");
            // Selecting the same real item then its already-associated slot must not replay.
            item=Runtime().Backpack[tool];yield return Drag(Cell(item.X,item.Y),Center(View().TacticalSlots[0].Root));yield return new WaitForSecondsRealtime(.12f);
            if(Object.FindAnyObjectByType<LoadoutLinkDetail>()!=null)throw new InvalidOperationException("Identical association replayed");
            Motion(0);item=Runtime().Backpack[tool];yield return Drag(Cell(item.X,item.Y),Center(View().TacticalSlots[1].Root));yield return new WaitForSecondsRealtime(.12f);
            if(Runtime().ItemQuickbarInstanceIds[1]!=tool || Object.FindAnyObjectByType<LoadoutLinkDetail>()!=null)throw new InvalidOperationException("Zero animation link failed");
            item=Runtime().Backpack[goggles];yield return Drag(Cell(item.X,item.Y),Center(Slot(GearSlot.Head).Root));yield return new WaitForSecondsRealtime(.12f);
            if(!Buckle(GearSlot.Head).Closed || Object.FindObjectsByType<LoadoutBuckleDetail>().Any(x=>x.IsAnimating))throw new InvalidOperationException("Zero animation buckle failed");
            Motion(animation);host.PreviewUiCapturePage("map");yield return new WaitForSecondsRealtime(.2f);
            if(Object.FindAnyObjectByType<LoadoutBuckleDetail>()!=null || Object.FindAnyObjectByType<LoadoutLinkDetail>()!=null)throw new InvalidOperationException("Page exit leaked");
            host.PreviewUiCapturePage("loadout");yield return new WaitForSecondsRealtime(.2f);
            if(Object.FindObjectsByType<LoadoutBuckleDetail>().Any(x=>x.IsAnimating) || Object.FindAnyObjectByType<LoadoutLinkDetail>()!=null)throw new InvalidOperationException("Reopen replayed");
            AudioExpansionVerification.RequireClip("cloth_drop");AudioExpansionVerification.RequireClip("latch");
            File.WriteAllText(Path.Combine(Folder,"result.txt"),"PASS: disposable S purchase of real wedge/goggles; real Input System chest unequip/re-equip with opened/closed buckles, invalid equipment target and outside cancellation do not animate; real wedge quickbar association retains backpack item; identical association no replay; animation zero preserves head equip and quickbar reassignment without transient; exit clears and reopen restores static state without replay. Memory gateway; no player-slot writes, full journey, disk-failure or acoustic acceptance claim.");
            DetailsInputFixture.End();
        }
    }
}
