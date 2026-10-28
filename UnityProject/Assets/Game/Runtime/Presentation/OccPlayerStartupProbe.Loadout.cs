using System;
using System.Collections;
using System.IO;
using System.Linq;
using OCC.Combat.Roguelite;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public sealed partial class OccPlayerStartupProbe
    {
        private IEnumerator RenderLoadoutSample(CombatPrototypeBootstrap host)
        {
            var run=RogueliteMapRun.CreateSubsequentAcademyRun(4311);
            run.ChooseOpeningGift(run.OpeningGift.CandidateIds.First(id=>id!="LK-05"&&id!="LK-06"));
            run.ConfirmAcademyDeparture();SaveAndResume(run,host);
            yield return new WaitForSecondsRealtime(1.2f);
            ClickThroughRaycast(FindButton("按钮_背包"));
            yield return new WaitForSecondsRealtime(1.3f);CaptureLoadout("equipment");
            var view=FindAnyObjectByType<RogueLoadoutEquipmentView>();
            if(view==null)throw new InvalidOperationException("Map backpack did not open equipment.");
            var runtime=RogueEquipmentRuntime.FromDto(host.CurrentMapRun.RogueRunState);
            var slot=RogueInventoryGridSystem.EquipmentSlots.Select((s,i)=>new{s,i}).First(s=>runtime.Equipped.ContainsKey(s.s)&&!string.IsNullOrEmpty(runtime.Equipped[s.s])&&s.s!=OCC.Combat.Roguelite.EquipmentSlot.Backpack);
            string item=runtime.Equipped[slot.s];
            Hover(view.EquipmentSlots[slot.i].Button);yield return new WaitForSecondsRealtime(.5f);CaptureLoadout("equipment_hover");
            Drag(view.EquipmentSlots[slot.i].Root,view.BackpackGrid,new Vector2(16,-16));
            yield return new WaitForSecondsRealtime(.8f);CaptureLoadout("unequipped");
            runtime=RogueEquipmentRuntime.FromDto(host.CurrentMapRun.RogueRunState);
            if(!runtime.Backpack.ContainsKey(item))throw new InvalidOperationException("Equipment drag did not unequip into backpack.");
            view=FindAnyObjectByType<RogueLoadoutEquipmentView>();
            var inventory=FindButton("背包物品_"+item);
            Drag((RectTransform)inventory.transform,view.EquipmentSlots[slot.i].Root,view.EquipmentSlots[slot.i].Root.rect.center);
            yield return new WaitForSecondsRealtime(.8f);CaptureLoadout("reequipped");
            if(RogueEquipmentRuntime.FromDto(host.CurrentMapRun.RogueRunState).Equipped[slot.s]!=item)
                throw new InvalidOperationException("Backpack drag did not equip into its slot.");
            ClickThroughRaycast(FindButton("按钮_术式编组"));
            yield return new WaitForSecondsRealtime(.8f);CaptureLoadout("spells");
            ClickThroughRaycast(FindSpellSlot(5));
            yield return new WaitForSecondsRealtime(.5f);CaptureLoadout("spells_5_8");
            var eighth=FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name.StartsWith("按钮_8  ",StringComparison.Ordinal));
            ClickThroughRaycast(eighth);yield return new WaitForSecondsRealtime(.5f);CaptureLoadout("spells_slot8");
            var assign=FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name.StartsWith("按钮_已在槽",StringComparison.Ordinal));
            if(assign.interactable)throw new InvalidOperationException("Duplicate spell action was not disabled.");
            CaptureLoadout("spells_duplicate_blocked");
            // Starter spells already occupy 1-4; duplicates are correctly refused.
            if(!string.IsNullOrEmpty(host.CurrentMapRun.RogueRunState.EquippedSpellIds[7]))
                throw new InvalidOperationException("Duplicate spell unexpectedly accepted into slot eight.");
            string candidate=host.CurrentMapRun.RogueRunState.MasteredSpellIds.OrderBy(id=>id,StringComparer.Ordinal).First();
            int original=Array.IndexOf(host.CurrentMapRun.RogueRunState.EquippedSpellIds,candidate);
            ClickThroughRaycast(FindSpellSlot(1));yield return new WaitForSecondsRealtime(.4f);
            ClickThroughRaycast(FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name.StartsWith("按钮_"+(original+1)+"  ",StringComparison.Ordinal)));
            yield return new WaitForSecondsRealtime(.4f);
            ClickThroughRaycast(FindButton("按钮_移除槽位"));yield return new WaitForSecondsRealtime(.4f);
            ClickThroughRaycast(FindSpellSlot(5));yield return new WaitForSecondsRealtime(.4f);
            ClickThroughRaycast(FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name.StartsWith("按钮_8  ",StringComparison.Ordinal)));
            yield return new WaitForSecondsRealtime(.4f);
            ClickThroughRaycast(FindButton("按钮_装入或替换"));yield return new WaitForSecondsRealtime(.5f);CaptureLoadout("spells_moved_to_8");
            string assigned=host.CurrentMapRun.RogueRunState.EquippedSpellIds[7];
            if(string.IsNullOrEmpty(assigned))throw new InvalidOperationException("Slot eight could not be filled.");
            ClickThroughRaycast(FindButton("按钮_移除槽位"));yield return new WaitForSecondsRealtime(.5f);CaptureLoadout("spells_removed");
            if(!string.IsNullOrEmpty(host.CurrentMapRun.RogueRunState.EquippedSpellIds[7]))throw new InvalidOperationException("Slot eight could not be cleared.");
            ClickThroughRaycast(FindSpellSlot(1));yield return new WaitForSecondsRealtime(.4f);
            ClickThroughRaycast(FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name.StartsWith("按钮_"+(original+1)+"  ",StringComparison.Ordinal)));
            yield return new WaitForSecondsRealtime(.4f);
            ClickThroughRaycast(FindButton("按钮_装入或替换"));yield return new WaitForSecondsRealtime(.4f);
            ClickThroughRaycast(FindButton("按钮_装备、背包与战术栏"));yield return new WaitForSecondsRealtime(.6f);
            CaptureLoadout("equipment_return");
            ClickThroughRaycast(FindButton("按钮_返回地图"));yield return new WaitForSecondsRealtime(.6f);CaptureLoadout("map_return");
            SaveAndResume(host.CurrentMapRun,host);yield return new WaitForSecondsRealtime(.8f);
            ClickThroughRaycast(FindButton("按钮_背包"));yield return new WaitForSecondsRealtime(.8f);CaptureLoadout("equipment_restored");
            if(RogueEquipmentRuntime.FromDto(host.CurrentMapRun.RogueRunState).Equipped[slot.s]!=item)
                throw new InvalidOperationException("Loadout changes did not survive continue.");
            // Additional local inventory fixtures use the same production UI/save path.
            var catalog=RogueContentCatalog.CreateAcademyV01();
            runtime=RogueEquipmentRuntime.FromDto(host.CurrentMapRun.RogueRunState);
            if(!runtime.TryAddContainerItem("loadout-tool",catalog.TacticalItems.First().DefinitionId))
                throw new InvalidOperationException("Tactical fixture did not fit.");
            runtime.WriteToDto(host.CurrentMapRun.RogueRunState);
            SaveAndResume(host.CurrentMapRun,host);yield return new WaitForSecondsRealtime(.7f);
            if(FindAnyObjectByType<RogueLoadoutEquipmentView>()==null)ClickThroughRaycast(FindButton("按钮_背包"));
            yield return new WaitForSecondsRealtime(.7f);CaptureLoadout("backpack_item");
            view=FindAnyObjectByType<RogueLoadoutEquipmentView>();
            Drag((RectTransform)FindButton("背包物品_loadout-tool").transform,view.TacticalSlots[3].Root,view.TacticalSlots[3].Root.rect.center);
            yield return new WaitForSecondsRealtime(.7f);CaptureLoadout("quickbar_linked");
            runtime=RogueEquipmentRuntime.FromDto(host.CurrentMapRun.RogueRunState);
            if(runtime.ItemQuickbarInstanceIds[3]!="loadout-tool"||!runtime.Backpack.ContainsKey("loadout-tool"))
                throw new InvalidOperationException("Tactical link lost the item or used the wrong slot.");
            for(int i=0;i<100;i++)if(!runtime.TryAddContainerItem("loadout-fill-"+i,"FORGE-LOAD"))break;
            int filled=RogueInventoryPresentation.Build(runtime).Sum(entry=>entry.Width*entry.Height);
            if(filled!=runtime.BackpackColumns*runtime.BackpackRows)throw new InvalidOperationException("Full bag fixture is not actually full.");
            runtime.WriteToDto(host.CurrentMapRun.RogueRunState);
            // Material stock and placements are separate production records.
            // A fixture must bind both; placement rows alone cannot grant stock.
            int forge=runtime.Backpack.Keys.Count(id=>runtime.MaterialIdFor(id)=="FORGE-LOAD");
            var fullDto=host.CurrentMapRun.RogueRunState;
            fullDto.MaterialStockRows.RemoveAll(row=>row.StartsWith("FORGE-LOAD=",StringComparison.Ordinal));
            fullDto.MaterialStockRows.Add("FORGE-LOAD="+forge);fullDto.ForgeMaterialCount=forge;
            var full=RogueliteMapRun.FromRogue11(host.CurrentMapRun.RogueRunState);
            SaveAndResume(full,host);yield return new WaitForSecondsRealtime(.7f);
            var fullRuntime=RogueEquipmentRuntime.FromDto(host.CurrentMapRun.RogueRunState);
            if(RogueInventoryPresentation.Build(fullRuntime).Sum(entry=>entry.Width*entry.Height)!=filled)
                throw new InvalidOperationException("Full bag fixture was overwritten during save/continue.");
            if(FindAnyObjectByType<RogueLoadoutEquipmentView>()==null)ClickThroughRaycast(FindButton("按钮_背包"));
            yield return new WaitForSecondsRealtime(.7f);CaptureLoadout("backpack_full");
            view=FindAnyObjectByType<RogueLoadoutEquipmentView>();
            string bag=runtime.Equipped[OCC.Combat.Roguelite.EquipmentSlot.Backpack];
            Drag(view.EquipmentSlots[7].Root,view.BackpackGrid,new Vector2(16,-16));
            yield return new WaitForSecondsRealtime(.7f);CaptureLoadout("full_bag_rejected");
            if(RogueEquipmentRuntime.FromDto(host.CurrentMapRun.RogueRunState).Equipped[OCC.Combat.Roguelite.EquipmentSlot.Backpack]!=bag)
                throw new InvalidOperationException("Full bag allowed a capacity reduction.");
            var basic=RogueliteMapRun.CreateSubsequentAcademyRun(4311);
            basic.ChooseOpeningGift(basic.OpeningGift.CandidateIds.First(id=>id!="LK-05"&&id!="LK-06"));basic.ConfirmAcademyDeparture();
            runtime=RogueEquipmentRuntime.FromDto(basic.RogueRunState);
            if(!runtime.Unequip(OCC.Combat.Roguelite.EquipmentSlot.Backpack))throw new InvalidOperationException("Base capacity fixture failed.");
            runtime.WriteToDto(basic.RogueRunState);SaveAndResume(basic,host);yield return new WaitForSecondsRealtime(.7f);
            if(FindAnyObjectByType<RogueLoadoutEquipmentView>()==null)ClickThroughRaycast(FindButton("按钮_背包"));
            yield return new WaitForSecondsRealtime(.7f);CaptureLoadout("base_capacity");
            view=FindAnyObjectByType<RogueLoadoutEquipmentView>();
            if(view.BackpackCells.Count(c=>c.gameObject.activeInHierarchy)!=24)throw new InvalidOperationException("Base backpack did not show 24 real cells.");
        }
        private static Button FindSpellSlot(int index)=>FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name.StartsWith("按钮_"+index+"  ",StringComparison.Ordinal));
        private static Button FindButton(string name)=>FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.name==name&&b.isActiveAndEnabled)
            ??throw new InvalidOperationException("Missing loadout button: "+name);
        private void CaptureLoadout(string name)
        {
            string folder=Path.GetDirectoryName(output);
            OccPlayerRenderCapture.Capture(null,1920,1080,Path.Combine(folder,name+".png"));
            OccPlayerRenderCapture.Capture(null,960,540,Path.Combine(folder,name+"_960.png"));
            var view=FindAnyObjectByType<RogueLoadoutEquipmentView>();
            if(view!=null)
            {
                var text=string.Join("\n",view.TacticalSlots.Select((slot,index)=>index+":"+slot.Quantity.text+",active="+slot.Quantity.gameObject.activeInHierarchy));
                text+="\n"+string.Join("\n",FindObjectsByType<Text>(FindObjectsSortMode.None).Where(label=>label.name=="剩余次数 清晰文字")
                    .Select(label=>label.text+",enabled="+label.enabled+",position="+label.rectTransform.position+",size="+label.rectTransform.rect.size));
                File.WriteAllText(Path.Combine(folder,name+"_counts.txt"),text);
            }
        }
        private static Vector2 ScreenPoint(RectTransform rect,Vector2 point)
        {
            var canvas=rect.GetComponentInParent<Canvas>();
            var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:Hd2DSceneGraphicRaycaster.ResolveEventCamera(canvas.worldCamera);
            return RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(point));
        }
        private static void Hover(Button button)
        {
            var rect=(RectTransform)button.transform;
            var pointer=new PointerEventData(EventSystem.current){position=ScreenPoint(rect,rect.rect.center)};
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerEnterHandler);
        }
        private static void Drag(RectTransform source,RectTransform target,Vector2 local)
        {
            var pointer=new PointerEventData(EventSystem.current){position=ScreenPoint(source,source.rect.center),button=PointerEventData.InputButton.Left};
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            var first=hits.FirstOrDefault(h=>ExecuteEvents.GetEventHandler<IPointerClickHandler>(h.gameObject)!=null);
            var handler=first.gameObject==null?null:ExecuteEvents.GetEventHandler<IPointerClickHandler>(first.gameObject);
            if(handler!=source.gameObject)throw new InvalidOperationException("Drag source obscured: "+source.name+" by "+(handler==null?"<none>":handler.name));
            pointer.pointerCurrentRaycast=first;
            pointer.pressPosition=pointer.position;pointer.pointerDrag=source.gameObject;
            ExecuteEvents.Execute(source.gameObject,pointer,ExecuteEvents.beginDragHandler);
            pointer.position=ScreenPoint(target,local);
            ExecuteEvents.Execute(source.gameObject,pointer,ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(source.gameObject,pointer,ExecuteEvents.endDragHandler);
        }
    }
}
