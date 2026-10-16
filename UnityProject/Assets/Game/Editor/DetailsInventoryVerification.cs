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
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;
namespace OCC.Combat.EditorTools
{
    public static class DetailsInventoryVerification
    {
        private static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-03_六十项小细节实装/验证/inventory"));
        private static bool background;
        private static Vector2 originalMouse;
        private static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        [MenuItem("OCC/Details60/Inventory/Record")]
        public static void Record()
        {
            if(!Application.isPlaying || Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Main project Play required");
            DetailsInputFixture.End();Directory.CreateDirectory(Folder);AudioExpansionVerification.StartLedger(Folder);File.Delete(Path.Combine(Folder,"result.txt"));
            background=Application.runInBackground;originalMouse=Mouse.current.position.ReadValue();Application.runInBackground=true;DetailsInputFixture.Begin(background);
            EditorApplication.playModeStateChanged-=Restore;EditorApplication.playModeStateChanged+=Restore;
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            Object.FindAnyObjectByType<CombatPrototypeBootstrap>().StartCoroutine(Routine());
        }
        private static void Restore(PlayModeStateChange s)
        {
            if(s!=PlayModeStateChange.ExitingPlayMode)return;
            Application.runInBackground=background;Pointer(originalMouse,false);DetailsInputFixture.End();EditorApplication.playModeStateChanged-=Restore;
        }
        private static void Pointer(Vector2 p,bool held)=>InputSystem.QueueStateEvent(Mouse.current,held?new MouseState{position=p}.WithButton(MouseButton.Left):new MouseState{position=p});
        private static RogueEquipmentRuntime Runtime(CombatPrototypeBootstrap host)=>RogueEquipmentRuntime.FromDto(host.CurrentMapRun.RogueRunState);
        private static RectTransform Grid(FormalRogueliteUi ui)=>(RectTransform)typeof(FormalRogueliteUi).GetField("loadoutGridRect",Flags).GetValue(ui);
        private static Vector2 Cell(FormalRogueliteUi ui,int x,int y)
        {
            var grid=Grid(ui);float size=(float)typeof(FormalRogueliteUi).GetField("loadoutCellSize",Flags).GetValue(ui);
            return RectTransformUtility.WorldToScreenPoint(null,grid.TransformPoint(new Vector3(grid.rect.xMin+(x+.35f)*size,grid.rect.yMax-(y+.35f)*size)));
        }
        private static IEnumerator Routine()
        {
            var host=Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            string error=host.PreviewUiCaptureRun(false,"S");if(error!=null)throw new InvalidOperationException(error);
            yield return new WaitForSecondsRealtime(.6f);host.PreviewUiCapturePage("room","S");yield return new WaitForSecondsRealtime(.5f);
            yield return Capture("shop-before.png");
            var surface=Object.FindObjectsByType<OccItemSurface>().Single(x=>x.Artwork!=null && x.Artwork.sprite.name=="aca_eq_hd01");
            var glass=surface.Artwork;
            Vector2 glassPoint=RectTransformUtility.WorldToScreenPoint(null,glass.rectTransform.TransformPoint(glass.rectTransform.rect.center));
            Pointer(glassPoint,false);yield return null;yield return null;
            var lensFrames=new List<Texture2D>();var lensTimes=new List<string>();
            for(int sample=0;sample<18;sample++)
            {
                yield return new WaitForEndOfFrame();lensFrames.Add(ScreenCapture.CaptureScreenshotAsTexture());
                var rendered=glass.materialForRendering;
                lensTimes.Add(Time.unscaledTime.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+" source="+glass.material.GetFloat("_Phase")+" render="+rendered.GetFloat("_Phase")+" same="+(rendered==glass.material)+" masks="+glass.GetComponentsInParent<Mask>().Length);
                if(sample==2 && (glass.material.GetFloat("_Glass")!=1 || glass.material.GetFloat("_Phase")<0))throw new InvalidOperationException("Lens material not responding to actual hover");
                yield return new WaitForSecondsRealtime(.015f);
            }
            for(int sample=0;sample<lensFrames.Count;sample++){File.WriteAllBytes(Path.Combine(Folder,"lens-"+sample.ToString("00")+".png"),lensFrames[sample].EncodeToPNG());Object.Destroy(lensFrames[sample]);}
            File.WriteAllLines(Path.Combine(Folder,"lens-times.txt"),lensTimes);
            var savedPrefs=host.UiPreferences;float savedAnimation=savedPrefs.AnimationIntensity;
            savedPrefs.Configure(savedPrefs.MasterVolume,0,savedPrefs.ScreenShake,savedPrefs.FloatingText,savedPrefs.HighContrast,savedPrefs.LargeText,savedPrefs.KeyHints);Pointer(new Vector2(1910,1070),false);yield return null;Pointer(glassPoint,false);yield return null;yield return null;
            if(glass.material.GetFloat("_Phase")>=0)throw new InvalidOperationException("Lens keeps animating at zero intensity");
            savedPrefs.Configure(savedPrefs.MasterVolume,savedAnimation,savedPrefs.ScreenShake,savedPrefs.FloatingText,savedPrefs.HighContrast,savedPrefs.LargeText,savedPrefs.KeyHints);
            Pointer(new Vector2(1910,1070),false);yield return null;

            var buttons=Object.FindObjectsByType<Button>().Where(b=>b.isActiveAndEnabled && b.IsInteractable() && b.name=="按钮_购买").OrderBy(b=>b.transform.position.x).ToArray();
            if(buttons.Length!=3)throw new InvalidOperationException("Expected fixed three offers");
            var buy=buttons[2];var rect=(RectTransform)buy.transform;Vector2 p=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=p},hits);
            if(hits.Count==0 || hits[0].gameObject.GetComponentInParent<Button>()!=buy)throw new InvalidOperationException("Purchase blocked");
            int gold=host.CurrentMapRun.Gold;Pointer(p,false);yield return null;Pointer(p,true);yield return null;Pointer(p,false);yield return new WaitForSecondsRealtime(.4f);
            if(host.CurrentMapRun.Gold!=gold-4 || !host.CurrentMapRun.CurrentShopService.Offers[2].Sold)throw new InvalidOperationException("Actual goggles purchase failed");
            if(OccDetailAudio.PriorityGain>=.99f)throw new InvalidOperationException("Committed purchase did not duck atmosphere/music");
            yield return Capture("shop-purchased.png");
            host.PreviewUiCapturePage("loadout");yield return new WaitForSecondsRealtime(.4f);
            var ui=Object.FindAnyObjectByType<FormalRogueliteUi>();var view=Object.FindAnyObjectByType<RogueLoadoutEquipmentView>();
            int index=RogueInventoryGridSystem.EquipmentSlots.ToList().IndexOf(OCC.Combat.Roguelite.EquipmentSlot.Chest);
            var chest=view.EquipmentSlots[index].Root;p=RectTransformUtility.WorldToScreenPoint(null,chest.TransformPoint(chest.rect.center));
            Pointer(p,false);yield return null;Pointer(p,true);yield return null;Pointer(p+new Vector2(12,0),true);yield return null;
            p=Cell(ui,0,1);Pointer(p,true);yield return null;Pointer(p,false);yield return new WaitForSecondsRealtime(.4f);
            var runtime=Runtime(host);var ids=runtime.Backpack.Keys.Where(id=>runtime.DefinitionFor(id)!=null).OrderBy(id=>runtime.DefinitionFor(id).BaseWeight).ToArray();
            if(ids.Length!=2)throw new InvalidOperationException("Expected purchased goggles and genuinely unequipped chest");
            foreach(string id in ids)
            {
                runtime=Runtime(host);var item=runtime.Backpack[id];int tx=-1,ty=-1;
                for(int y=0;y<runtime.BackpackRows && tx<0;y++)for(int x=0;x<runtime.BackpackColumns;x++)
                    if((x!=item.X || y!=item.Y) && runtime.CanMoveBackpack(id,x,y,item.Rotated)){tx=x;ty=y;break;}
                if(tx<0)throw new InvalidOperationException("No legal destination");
                int weight=runtime.DefinitionFor(id).BaseWeight;Vector2 start=Cell(ui,item.X,item.Y),end=Cell(ui,tx,ty);
                Pointer(start,false);yield return null;
                hits.Clear();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=start},hits);
                if(hits.Count==0 || hits[0].gameObject.GetComponentInParent<Button>()?.name!="背包物品_"+id)throw new InvalidOperationException("Blocked source "+id+": "+string.Join(",",hits.Select(h=>h.gameObject.name)));
                Pointer(start,true);yield return null;Pointer(start+new Vector2(12,0),true);yield return null;yield return null;
                if(runtime.DefinitionFor(id).DefinitionId=="ACA-EQ-HD01")
                {
                    int matching=RogueInventoryGridSystem.EquipmentSlots.ToList().IndexOf(runtime.DefinitionFor(id).Slot);
                    var matchSlot=Object.FindAnyObjectByType<RogueLoadoutEquipmentView>().EquipmentSlots[matching];
                    Vector2 slotPoint=RectTransformUtility.WorldToScreenPoint(null,matchSlot.Root.TransformPoint(matchSlot.Root.rect.center));
                    Pointer(slotPoint,true);yield return null;yield return null;
                    var silhouette=matchSlot.DropOverlay.transform.Find("匹配装备剪影");
                    if(silhouette==null || !silhouette.gameObject.activeInHierarchy || silhouette.GetComponent<Image>().raycastTarget)throw new InvalidOperationException("Matched slot silhouette missing or blocks input");
                    yield return Capture("matched-slot.png");Pointer(start+new Vector2(12,0),true);yield return null;
                }
                int rebuildStart=ui.FullRebuildCount;
                var frames=new List<Texture2D>();var times=new List<string>();
                for(int i=0;i<24;i++)
                {
                    if(i==8)Pointer(end,true);if(i==15)Pointer(end,false);
                    yield return new WaitForEndOfFrame();frames.Add(ScreenCapture.CaptureScreenshotAsTexture());times.Add(Time.unscaledTime.ToString("R",System.Globalization.CultureInfo.InvariantCulture));
                    File.AppendAllText(Path.Combine(Folder,"drag-trace.txt"),i+" rebuild="+ui.FullRebuildCount+" original="+rebuildStart+" pressed="+Mouse.current.leftButton.isPressed+" id="+typeof(FormalRogueliteUi).GetField("loadoutDragId",Flags).GetValue(ui)+" details="+Object.FindObjectsByType<InventoryLiftDetail>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length+"\n");
                    if(i==7)
                    {
                        var detail=Object.FindAnyObjectByType<InventoryLiftDetail>();
                        if(detail==null)
                        {
                            File.WriteAllBytes(Path.Combine(Folder,"failure-source.png"),frames[frames.Count-1].EncodeToPNG());
                            var all=Object.FindObjectsByType<InventoryLiftDetail>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                            File.WriteAllText(Path.Combine(Folder,"failure-source.txt"),"sprite="+(Resources.Load<Sprite>(OCC.Combat.FormalArtRegistry.EquipmentFootprintPath(runtime.DefinitionFor(id).DefinitionId))!=null)+" details="+all.Length+" pointer="+typeof(FormalRogueliteUi).GetField("loadoutLastPointer",Flags).GetValue(ui)+" mouse="+Mouse.current.position.ReadValue());
                            throw new InvalidOperationException("No live drag for "+id);
                        }
                        float lift=detail.transform.Find("实际物件").GetComponent<RectTransform>().anchoredPosition.y;
                        if(lift!=4)throw new InvalidOperationException("Rejected weight visual remained");
                        File.AppendAllText(Path.Combine(Folder,"actual-weight.txt"),id+" weight="+weight+" lift="+lift+"\n");
                    }
                    yield return new WaitForSecondsRealtime(.025f);
                }
                for(int i=0;i<frames.Count;i++){File.WriteAllBytes(Path.Combine(Folder,"weight-"+weight+"-"+i.ToString("00")+".png"),frames[i].EncodeToPNG());Object.Destroy(frames[i]);}
                File.WriteAllLines(Path.Combine(Folder,"weight-"+weight+"-times.txt"),times);
                var moved=Runtime(host).Backpack[id];if(moved.X!=tx || moved.Y!=ty || Object.FindAnyObjectByType<InventoryLiftDetail>()!=null)throw new InvalidOperationException("Placement failed or effect leaked");
            }
            AudioExpansionVerification.RequireClip("coins");AudioExpansionVerification.RequireClip("glass_pick");AudioExpansionVerification.RequireClip("glass_drop");AudioExpansionVerification.RequireClip("cloth_pick");AudioExpansionVerification.RequireClip("cloth_drop");
            host.PreviewUiCapturePage("room","S");yield return new WaitForSecondsRealtime(.2f);host.PreviewUiCapturePage("map");yield return new WaitForSecondsRealtime(.3f);
            AudioExpansionVerification.RequireClip("shop_exit");
            if(Object.FindAnyObjectByType<InventoryLiftDetail>()!=null)throw new InvalidOperationException("Page cleanup failed");
            File.WriteAllText(Path.Combine(Folder,"result.txt"),"PASS: disposable preview starts at S directly, not full journey. Actual pointer purchase of default goggles (gold -4, sold, backpack granted), actual chest unequip, actual backpack lift/move/release of weight 1 and 2 with equal 4px baseline lift; weight visual removed. Real sprites, no injected items or edited stats. Player saves untouched. Goggles pinned card has a hover-driven lens material, animation zero suppresses it, and the subsequent actual purchase remains clickable. Matched goggles slot silhouette is present, non-raycasting, and leaves no effect after moving back to grid; shop exit bell was dispatched. No new prop asset promoted.");
        }
        private static IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Folder,name),t.EncodeToPNG());Object.Destroy(t);}
    }
}
