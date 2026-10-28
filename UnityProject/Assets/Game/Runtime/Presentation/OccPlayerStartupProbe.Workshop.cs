using System;
using System.Collections;
using System.Collections.Generic;
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
        // This first section starts with the actual main menu and uses the game's
        // own new-file/configuration/gift controls. It never constructs a run.
        private IEnumerator RenderWorkshopSample(CombatPrototypeBootstrap host)
        {
            string folder=Path.GetDirectoryName(output);
            workshopLampPreferencePresent=PlayerPrefs.HasKey(Hd2DDeskLampControl.PreferenceKey);
            workshopLampPreference=PlayerPrefs.GetFloat(Hd2DDeskLampControl.PreferenceKey,.4f);
            workshopPreferenceCaptured=true;
            ClickThroughRaycast(FindButton("按钮_开始游戏"));
            var opening=FindAnyObjectByType<FirstExperiencePrototypeController>();
            float entryDeadline=Time.realtimeSinceStartup+8;
            while(opening!=null&&opening.DebugStage!="Configuration"&&Time.realtimeSinceStartup<entryDeadline)yield return null;
            // The flow changes in the book's Update; the UI observes it on its
            // next Update and builds its lit geometry in LateUpdate.
            yield return new WaitForSecondsRealtime(.7f);
            if(opening==null||opening.DebugStage!="Configuration")throw new InvalidOperationException("Normal menu did not open character configuration: "+opening?.DebugStage);
            if(opening.FixedFirstExperienceEnabled)
            {ClickThroughRaycast(FindButton("按钮_固定首次体验开关"));yield return new WaitForSecondsRealtime(.4f);}
            ClickThroughRaycast(FindButton("按钮_以此角色开始"));yield return new WaitForSecondsRealtime(.7f);
            ClickThroughRaycast(FindButton("按钮_进入首次地图"));yield return new WaitForSecondsRealtime(1.3f);
            var tutorial=FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.name=="按钮_不需要教程");
            if(tutorial!=null){ClickThroughRaycast(tutorial);yield return new WaitForSecondsRealtime(.8f);}
            if(host.CurrentMapRun==null||!host.CurrentMapRun.OpeningGiftPending)
                throw new InvalidOperationException("Normal new-file route did not reach the opening gift");
            CaptureLoadout("normal_gift");
            ClickThroughRaycast(FindButton("按钮_守灯人查看配置"));yield return new WaitForSecondsRealtime(1);
            CaptureLoadout("normal_equipment");SnapshotWorkshop("normal_equipment");
            var view=FindAnyObjectByType<RogueLoadoutEquipmentView>();
            if(view==null)throw new InvalidOperationException("Gift configuration did not open the production loadout");
            Hover(view.EquipmentSlots[4].Button);yield return new WaitForSecondsRealtime(.4f);CaptureLoadout("normal_equipment_hover");
            var leavePointer=new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(view.EquipmentSlots[4].Button.gameObject,leavePointer,ExecuteEvents.pointerExitHandler);
            var lamp=FindObjectsByType<Slider>(FindObjectsSortMode.None).First(s=>s.GetComponentInParent<Hd2DDeskLampControl>()!=null);
            float previous=lamp.value;
            foreach(float value in new[]{0f,.4f,1f})
            {
                LampDrag(lamp,value);yield return new WaitForSecondsRealtime(.35f);
                CaptureLoadout("normal_light_"+Mathf.RoundToInt(value*100));
            }
            LampDrag(lamp,previous);yield return new WaitForSecondsRealtime(.4f);
            var performance=new List<WorkshopPerformance>();
            foreach(var resolution in new[]{new Vector2Int(1920,1080),new Vector2Int(960,540)})
            {
                Screen.SetResolution(resolution.x,resolution.y,FullScreenMode.Windowed);
                yield return new WaitForSecondsRealtime(1);
                var times=new List<float>();
                for(int i=0;i<240;i++){yield return null;times.Add(Time.unscaledDeltaTime*1000);}
                times.Sort();
                performance.Add(new WorkshopPerformance{width=Screen.width,height=Screen.height,quality=QualitySettings.names[QualitySettings.GetQualityLevel()],
                    p95ms=times[Mathf.CeilToInt(times.Count*.95f)-1],medianMs=times[times.Count/2],frames=times.Count});
            }
            File.WriteAllText(Path.Combine(folder,"performance.json"),JsonUtility.ToJson(new WorkshopPerformanceReport{
                device=SystemInfo.deviceModel,cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,measurements=performance.ToArray()},true));
            Screen.SetResolution(1920,1080,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.7f);
            ClickThroughRaycast(FindButton("按钮_术式编组"));yield return new WaitForSecondsRealtime(.7f);CaptureLoadout("normal_spells");
            for(int i=0;i<3;i++)
            {
                ClickThroughRaycast(FindSpellSlot(i==1?8:1));yield return new WaitForSecondsRealtime(.2f);
                CaptureLoadout("motion_spells_"+i);
            }
            ClickThroughRaycast(FindButton("按钮_返回守灯人"));yield return new WaitForSecondsRealtime(.7f);
            var gift=FindAnyObjectByType<LampkeeperGiftView>();
            if(gift==null)throw new InvalidOperationException("Loadout return did not restore gift page");
            int index=Array.FindIndex(host.CurrentMapRun.OpeningGift.CandidateIds,id=>id!="LK-05");
            string selected=host.CurrentMapRun.OpeningGift.CandidateIds[index];
            ClickThroughRaycast(gift.ChoiceButtons[index]);yield return new WaitForSecondsRealtime(.7f);
            if(host.CurrentMapRun.OpeningGift.SelectedId!=selected)throw new InvalidOperationException("Normal gift click did not select expected reward");
            CaptureLoadout("normal_gift_accepted");
            File.WriteAllText(Path.Combine(folder,"normal-entry.json"),"{\"status\":\"PASS\",\"route\":\"main menu > new file > character > normal academy > gift > equipment > light drag > spells > gift acceptance\",\"constructed_run\":false,\"isolated_save_scope\":true,\"focus_requested\":false}");

            // Separate local service/reward fixtures, without entering battle.
            // They exercise real UI callbacks but do not claim a complete journey.
            var run=RogueliteMapRun.CreateSubsequentAcademyRun(4311);
            run.ChooseOpeningGift(run.OpeningGift.CandidateIds.First(id=>id!="LK-05"&&id!="LK-06"));run.ConfirmAcademyDeparture();
            foreach(var type in new[]{RogueliteMapNodeType.Workshop,RogueliteMapNodeType.Medical})
            {
                var store=new PlayerPrefsRogueliteSaveStore();new RogueliteSaveGateway(store).SaveNewMapRun(run);
                var dto=Rogue11Serializer.Deserialize(store.GetString(RogueliteSaveGateway.MapRunKey));
                string node=run.MapNodes.First(n=>n.Type==type).Id;dto.CurrentNodeId=node;
                if(!dto.VisitedNodeIds.Contains(node))dto.VisitedNodeIds.Add(node);
                SaveAndResume(RogueliteMapRun.FromRogue11(dto),host);yield return new WaitForSecondsRealtime(.5f);
                FindAnyObjectByType<FormalRogueliteUi>().PreviewUiCapturePage("room",node);
                yield return new WaitForSecondsRealtime(.8f);CaptureLoadout("local_"+type.ToString().ToLowerInvariant());
                if(type==RogueliteMapNodeType.Workshop)
                {
                    ClickThroughRaycast(FindButton("页签_术式专精"));yield return new WaitForSecondsRealtime(.5f);CaptureLoadout("local_workshop_spells");
                }
            }
            run.SelectNode("dorm_drill");run.CompleteCurrentCombat();run.OpenPendingRewardChoices();
            SaveAndResume(run,host);yield return new WaitForSecondsRealtime(1);CaptureLoadout("local_reward");
            var reward=FindButton("揭晓卡牌_0");Hover(reward);yield return new WaitForSecondsRealtime(.3f);CaptureLoadout("local_reward_hover");
            ClickThroughRaycast(reward);yield return new WaitForSecondsRealtime(.6f);CaptureLoadout("local_reward_selected");
        }
        private bool workshopPreferenceCaptured,workshopLampPreferencePresent;
        private float workshopLampPreference;
        private void RestoreWorkshopPreference()
        {
            if(!workshopPreferenceCaptured)return;
            if(workshopLampPreferencePresent)PlayerPrefs.SetFloat(Hd2DDeskLampControl.PreferenceKey,workshopLampPreference);
            else PlayerPrefs.DeleteKey(Hd2DDeskLampControl.PreferenceKey);
            PlayerPrefs.Save();workshopPreferenceCaptured=false;
        }
        private static void LampDrag(Slider slider,float normalized)
        {
            var rect=slider.transform as RectTransform;
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,
                position=ScreenPoint(rect,new Vector2(Mathf.Lerp(rect.rect.xMin+8,rect.rect.xMax-8,normalized),rect.rect.center.y))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            if(!hits.Any(h=>h.gameObject==slider.gameObject||h.gameObject.transform.IsChildOf(slider.transform)))
                throw new InvalidOperationException("Lamp slider is obscured");
            var hit=hits.First(h=>h.gameObject==slider.gameObject||h.gameObject.transform.IsChildOf(slider.transform));
            pointer.pointerCurrentRaycast=hit;pointer.pointerPressRaycast=hit;
            ExecuteEvents.Execute(slider.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(slider.gameObject,pointer,ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(slider.gameObject,pointer,ExecuteEvents.pointerUpHandler);
            if(Mathf.Abs(slider.normalizedValue-normalized)>.08f)throw new InvalidOperationException("Lamp drag value mismatch: expected="+normalized+",actual="+slider.normalizedValue);
        }
        private void SnapshotWorkshop(string name)
        {
            var rows=FindObjectsByType<Image>(FindObjectsSortMode.None).Where(i=>i.isActiveAndEnabled&&i.sprite!=null)
                .Select(i=>new WorkshopSurface{name=i.name,texture=i.sprite.texture.name,ppu=i.sprite.pixelsPerUnit,
                    scale=i.transform.lossyScale,position=i.transform.position,border=i.sprite.border,type=i.type.ToString(),ppuMultiplier=i.pixelsPerUnitMultiplier}).ToArray();
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(output),name+"-surfaces.json"),JsonUtility.ToJson(new WorkshopSurfaces{surfaces=rows},true));
        }
        [Serializable] private sealed class WorkshopSurface {public string name,texture,type;public float ppu,ppuMultiplier;public Vector3 scale,position;public Vector4 border;}
        [Serializable] private sealed class WorkshopSurfaces {public WorkshopSurface[] surfaces;}
        [Serializable] private sealed class WorkshopPerformance {public int width,height,frames;public float medianMs,p95ms;public string quality;}
        [Serializable] private sealed class WorkshopPerformanceReport {public string device,cpu,gpu;public WorkshopPerformance[] measurements;}
    }
}
