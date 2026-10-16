using System;
using System.Collections.Generic;
using System.Linq;
using OCC.Combat.Roguelite;
using UnityEngine;
using UnityEngine.UI;
using RogueEquipmentSlot = OCC.Combat.Roguelite.EquipmentSlot;

namespace OCC.Combat.Presentation
{
    public sealed partial class FormalRogueliteUi
    {
        private readonly Dictionary<RogueEquipmentSlot,string> displayedEquipment = new Dictionary<RogueEquipmentSlot,string>();
        private string[] displayedQuickbar = Array.Empty<string>();
        private readonly HashSet<RogueEquipmentSlot> pendingBuckles = new HashSet<RogueEquipmentSlot>();
        private int pendingLinkSlot = -1;
        private Vector2 pendingLinkSource;

        private void CaptureCommittedLoadout(UiVisualEvent signal)
        {
            if(overlay!=UiOverlay.Loadout || loadoutSection!=LoadoutSection.Equipment || content==null ||
                bootstrap.CurrentMapRun?.RogueRunState==null)return;
            var now=RogueEquipmentRuntime.FromDto(bootstrap.CurrentMapRun.RogueRunState);
            if(signal.Subject=="inventory-drop")
                foreach(var pair in displayedEquipment)
                {
                    now.Equipped.TryGetValue(pair.Key,out string id);
                    if(id!=pair.Value)pendingBuckles.Add(pair.Key);
                }
            if(signal.Subject!="inventory-link" || string.IsNullOrEmpty(signal.Origin))return;
            string[] current=now.ItemQuickbarInstanceIds;
            for(int i=0;i<current.Length;i++)
            {
                if(current[i]!=signal.Origin || i>=displayedQuickbar.Length || current[i]==displayedQuickbar[i])continue;
                var view=content.GetComponentInChildren<RogueLoadoutEquipmentView>();
                var source=view!=null?view.BackpackItemsRoot.GetComponentsInChildren<Button>().FirstOrDefault(x=>x.name=="背包物品_"+signal.Origin):null;
                if(source==null)continue;
                var rect=(RectTransform)source.transform;
                pendingLinkSource=LoadoutEffectPoint(rect,rect.rect.center);
                pendingLinkSlot=i;
            }
        }
        private void RememberLoadout(RogueEquipmentRuntime runtime)
        {
            displayedEquipment.Clear();
            foreach(var slot in RogueInventoryGridSystem.EquipmentSlots)
            {runtime.Equipped.TryGetValue(slot,out string id);displayedEquipment[slot]=id;}
            displayedQuickbar=runtime.ItemQuickbarInstanceIds;
        }
        private void PlayCommittedLoadout()
        {
            if(overlay==UiOverlay.Loadout && loadoutSection==LoadoutSection.Equipment)
            {
                foreach(var slot in pendingBuckles)
                    if(loadoutEquipmentSlotRects.TryGetValue(slot,out var root))root.GetComponentInChildren<LoadoutBuckleDetail>()?.PlayCommitted();
                if(pendingLinkSlot>=0 && loadoutQuickbarSlotRects.TryGetValue(pendingLinkSlot,out var target))
                {
                    Vector2 end=LoadoutEffectPoint(target,target.rect.center);
                    var view=content.GetComponentInChildren<RogueLoadoutEquipmentView>();
                    if(view==null)return;
                    float gutter=LoadoutEffectPoint(view.BackpackPanel,new Vector2(view.BackpackPanel.rect.xMin-12,0)).x;
                    float belowRow=LoadoutEffectPoint(target,new Vector2(0,target.rect.yMin-8)).y;
                    LoadoutLinkDetail.Spawn((RectTransform)content.transform,pendingLinkSource,end,gutter,belowRow,()=>bootstrap.UiPreferences.AnimationIntensity);
                }
            }
            pendingBuckles.Clear(); pendingLinkSlot=-1;
        }
        private Vector2 LoadoutEffectPoint(RectTransform source,Vector2 local)
        {
            var canvas=source.GetComponentInParent<Canvas>();
            Vector3 world=source.TransformPoint(local);
            if(canvas!=null&&canvas.renderMode==RenderMode.WorldSpace&&canvas.worldCamera!=null)
            {
                Vector3 point=canvas.worldCamera.WorldToViewportPoint(world);
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)content.transform,
                    new Vector2(point.x*Screen.width,point.y*Screen.height),null,out var result);
                return result;
            }
            return content.transform.InverseTransformPoint(world);
        }
    }
}
