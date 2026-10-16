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
using Object = UnityEngine.Object;

namespace OCC.Combat.EditorTools
{
    public static class AcademyJourneyAudit
    {
        private static IDisposable scope;
        private static Dictionary<string,string> originals;
        public static string Status { get; private set; } = "idle";
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../ReviewEvidence/20261005_four_tasks"));
        private static CombatPrototypeBootstrap Host => Object.FindAnyObjectByType<CombatPrototypeBootstrap>();

        public static string Begin(string id)
        {
            if (!Application.isPlaying || Host == null || Application.dataPath.Replace('\\','/') != "E:/数据库/OCC_Codex/UnityProject/Assets")
                throw new InvalidOperationException("Main project Play Mode required.");
            if (scope != null) throw new InvalidOperationException("Audit already active.");
            originals = new Dictionary<string,string>();
            for (int i=0;i<3;i++)
                foreach (string key in new[]{"OCC.FirstExperiencePrototype.v2.slot."+i,"occ.first_experience.map_run.slot."+i})
                    originals[key] = PlayerPrefs.GetString(key, "<missing>");
            scope = FirstExperienceSaveRouting.BeginVerificationScope(id);
            EditorApplication.playModeStateChanged += Restore;
            Directory.CreateDirectory(Folder);
            Status = "ready:"+id;
            return Status;
        }
        private static void Restore(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.ExitingPlayMode) return;
            foreach(var pair in originals ?? new Dictionary<string,string>())
                if(PlayerPrefs.GetString(pair.Key,"<missing>")!=pair.Value) Debug.LogError("Player archive changed during audit: "+pair.Key);
            scope?.Dispose(); scope=null;
            EditorApplication.playModeStateChanged -= Restore;
        }
        public static string RunEntry() => Start(NewEntry(), "entry");
        public static string RunToSecondBattle() => Start(SecondBattleEntry(), "second-battle-normal-entry");
        public static string RunFullTutorial() => Start(FullTutorial(), "full-tutorial-reward-journey");
        public static string RunRemainingTutorial() => Start(RemainingTutorial(), "remaining-tutorial-reward-journey");
        public static string Press(string name) => Start(Click(name), "click:"+name);
        public static string FightCurrent() => Start(Fight(), "combat");
        public static string PauseAndContinue() => Start(Resume(), "resume");
        private static string Start(IEnumerator routine,string label)
        {
            if(Host==null || !Application.isPlaying || scope==null) throw new InvalidOperationException("Audit missing");
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            Status="running:"+label; Host.StartCoroutine(Guard(routine,label)); return Status;
        }
        private static IEnumerator Guard(IEnumerator root,string label)
        {
            var stack=new Stack<IEnumerator>(); stack.Push(root);
            while(stack.Count>0)
            {
                bool moved=false; object value=null; Exception error=null;
                try { moved=stack.Peek().MoveNext(); if(moved)value=stack.Peek().Current; } catch(Exception e){error=e;}
                if(error!=null){Status="FAIL:"+label+":"+error; File.AppendAllText(Path.Combine(Folder,"journey.txt"),Status+"\n"); yield break;}
                if(!moved){stack.Pop(); continue;}
                if(value is IEnumerator child){stack.Push(child);continue;} yield return value;
            }
            Status="PASS:"+label; File.AppendAllText(Path.Combine(Folder,"journey.txt"),Status+"\n");
        }
        private static IEnumerator NewEntry()
        {
            yield return new WaitForSecondsRealtime(2);
            if(FindButton("跳过动画")!=null) yield return Click("跳过动画");
            yield return Click("开始游戏");yield return new WaitForSecondsRealtime(1.6f);
            var opening=Object.FindAnyObjectByType<FirstExperiencePrototypeController>();
            if(opening==null || opening.IsPendingOverwrite) throw new InvalidOperationException("Verification slot is occupied");
            yield return Click("以此角色开始"); yield return Click("进入首次地图");
            if(FindButton("不需要教程")!=null) yield return Click("不需要教程");
            if(Host.CurrentMapRun==null) throw new InvalidOperationException("Normal entry did not create a map run");
            File.AppendAllText(Path.Combine(Folder,"journey.txt"),"ENTRY:"+Host.CurrentMapRun.CurrentNodeId+" seed="+Host.CurrentMapRun.Seed+" saved="+Host.IsMapRunSaved+"\n");
        }
        private static Button FindButton(string name) => Object.FindObjectsByType<Button>().FirstOrDefault(b=>b.isActiveAndEnabled && b.IsInteractable() && (b.name==name || b.name=="按钮_"+name));
        private static IEnumerator Click(string name)
        {
            yield return new WaitForEndOfFrame();
            var button=FindButton(name); float until=Time.realtimeSinceStartup+2f;
            while(button==null && Time.realtimeSinceStartup<until){yield return null;button=FindButton(name);}
            if(button==null) throw new InvalidOperationException("Missing button "+name);
            yield return Tap((RectTransform)button.transform,button);
            yield return new WaitForSecondsRealtime(.25f);
        }
        private static IEnumerator Tap(RectTransform rect,Button button=null)
        {
            if(rect==null || !rect.gameObject.activeInHierarchy) throw new InvalidOperationException("Inactive input surface");
            var canvas=rect.GetComponentInParent<Canvas>();
            var eventCamera=canvas==null||canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            var p=UiScreenPoint(eventCamera,rect,rect.rect.center);
            var hits=new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=p},hits);
            if(button==null && (hits.Count==0 || !hits[0].gameObject.transform.IsChildOf(rect)))
            {
                foreach(var offset in new[]{new Vector2(.15f,.5f),new Vector2(.85f,.5f),new Vector2(.5f,.15f),new Vector2(.5f,.85f)})
                {
                    var local=new Vector2(Mathf.Lerp(rect.rect.xMin,rect.rect.xMax,offset.x),Mathf.Lerp(rect.rect.yMin,rect.rect.yMax,offset.y));
                    p=UiScreenPoint(eventCamera,rect,local); hits.Clear();
                    EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=p},hits);
                    if(hits.Count>0 && hits[0].gameObject.transform.IsChildOf(rect))break;
                }
            }
            if(hits.Count==0 || (button!=null && hits[0].gameObject.GetComponentInParent<Button>()!=button) ||
                (button==null && !hits[0].gameObject.transform.IsChildOf(rect) && hits[0].gameObject!=rect.gameObject))
                throw new InvalidOperationException("Input blocked: "+rect.name+" hit="+(hits.Count>0?hits[0].gameObject.name:"none"));
            var pointer=new PointerEventData(EventSystem.current){position=p,button=PointerEventData.InputButton.Left,clickCount=1};
            if(button==null)
            {
                // Dispatch through the live cell's Unity event handler after verifying its hit surface.
                // Editor automation can lose synthetic pointer releases while GameView focus changes.
                ExecuteEvents.ExecuteHierarchy(rect.gameObject,pointer,ExecuteEvents.pointerClickHandler);
                ExecuteEvents.ExecuteHierarchy(rect.gameObject,pointer,ExecuteEvents.pointerClickHandler);
                yield return null; yield break;
            }
            ExecuteEvents.ExecuteHierarchy(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            yield return null;
        }
        private static Vector2 UiScreenPoint(Camera camera,RectTransform rect,Vector2 local)
        {
            var point=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(local));
            // HD2D raycasters convert window pixels into the scene camera RT.
            // The automation must provide window pixels, just like actual input.
            if(camera!=null&&camera.targetTexture!=null)
                point=new Vector2(point.x*Screen.width/Mathf.Max(1,camera.pixelWidth),
                    point.y*Screen.height/Mathf.Max(1,camera.pixelHeight));
            return point;
        }
        private static IEnumerator Fight()
        {
            int actions=0; float until=Time.realtimeSinceStartup+900;
            while(Host.CurrentFlowPhase==CombatFlowPhase.Active && Time.realtimeSinceStartup<until && actions<400)
            {
                if(Host.IsCombatEntryBlocking || Host.IsCombatActionPlaying || Host.CurrentState.ActiveUnitId!="hero")
                {yield return new WaitForSecondsRealtime(.1f);continue;}
                var state=Host.CurrentState;
                var command=AcademyCombatAudit.ChooseHero(state);
                if(command.Type==CombatCommandType.EndTurn) yield return Click("结束行动");
                else
                {
                    string action=command.Type==CombatCommandType.Move?"移动":command.Type==CombatCommandType.Attack?"攻击":command.Type==CombatCommandType.Interact?"互动":"技能"+(command.SlotIndex+1);
                    // Select the same public HUD action; execution still comes exclusively from real pointer events.
                    if(command.Type==CombatCommandType.UseSkill) yield return Click(action); else Host.SelectHudAction(action);
                    var target=string.IsNullOrEmpty(command.TargetUnitId)?command.Destination:state.GetUnit(command.TargetUnitId).Position;
                    var hd=Object.FindAnyObjectByType<Hd2DBattlefieldSample>();
                    if(hd!=null)
                    {
                        yield return Hd2DCombatReviewAudit.Cell(hd,target);
                        yield return Hd2DCombatReviewAudit.Cell(hd,target);
                        yield return new WaitForSecondsRealtime(.7f);
                        actions++;
                        if(!Host.IsMapRunSaved)throw new InvalidOperationException("Command journal save failed");
                        continue;
                    }
                    Host.BattlefieldViewport.Focus(target); yield return null;
                    var cell=GameObject.Find("格子_"+target.X+"_"+target.Y);
                    if(cell==null)throw new InvalidOperationException("Missing target cell "+target);
                    yield return Tap((RectTransform)cell.transform);
                }
                actions++;
                if(!Host.IsMapRunSaved) throw new InvalidOperationException("Command journal save failed");
                yield return new WaitForSecondsRealtime(.12f);
            }
            File.AppendAllText(Path.Combine(Folder,"journey.txt"),"COMBAT:"+Host.CurrentMapRun.CurrentNodeId+" phase="+Host.CurrentFlowPhase+" actions="+actions+" hp="+Host.CurrentState.GetUnit("hero").Health+" mana="+Host.CurrentState.GetUnit("hero").Mana+"\n");
            if(Host.CurrentFlowPhase==CombatFlowPhase.Active)throw new InvalidOperationException("Combat time/action limit");
        }
        private static IEnumerator FullTutorial()
        {
            yield return NewEntry();
            yield return Battle("B1",1);
            yield return Visit("EV1"); yield return Click("接下温室传令"); yield return Click("回到地图");
            yield return Visit("EV2"); yield return Click("领取金币"); yield return Click("回到地图");
            yield return RemainingTutorial();
        }
        private static IEnumerator SecondBattleEntry()
        {
            yield return NewEntry();
            yield return Battle("B1",1);
            yield return Visit("EV1"); yield return Click("接下温室传令"); yield return Click("回到地图");
            yield return Visit("EV2"); yield return Click("领取金币"); yield return Click("回到地图");
            yield return Click("map.node.B2"); yield return Click("进入战斗");
            yield return new WaitForSecondsRealtime(1);
            if(Host.CurrentLevelId!=FirstRegionLevelCatalog.GreenhouseCollectionRoom.Id || Host.CurrentFlowPhase!=CombatFlowPhase.Active)
                throw new InvalidOperationException("Normal route did not reach greenhouse battle");
        }
        private static IEnumerator RemainingTutorial()
        {
            yield return Battle("B2",0);
            yield return Visit("EV3"); yield return Click("领取学院贡献"); yield return Click("回到地图");
            yield return Battle("B3",2);
            yield return Visit("M"); yield return Click("服务_接受治疗"); yield return Click("餐食_MEAL-AETHER"); yield return Click("返回地图");
            yield return Battle("X",0);
            yield return Visit("S");
            var before=Host.CurrentMapRun;
            string inheritance=Inheritance(before);
            yield return Click("离开商店"); yield return new WaitForSecondsRealtime(1);
            if(!Host.CurrentMapRun.IsInAcademyLayer || Host.CurrentMapRun.IsTutorialPhase || !Host.IsMapRunSaved ||
                Inheritance(Host.CurrentMapRun)!=inheritance)throw new InvalidOperationException("Tutorial handoff changed inherited state");
            File.AppendAllText(Path.Combine(Folder,"journey.txt"),"HANDOFF:PASS "+inheritance+" mapNodes="+Host.CurrentMapRun.MapNodes.Count+"\n");
        }
        private static string Inheritance(RogueliteMapRun run) => Newtonsoft.Json.JsonConvert.SerializeObject(new {
            run.Seed,run.Gold,run.StageContribution,run.CurrentHealth,run.CurrentMana,
            spells=run.RogueRunState.MasteredSpellIds.ToArray(),equipped=run.RogueEquippedSpellIds.ToArray(),
            equipment=run.RogueRunState.EquipmentInstances.Select(i=>new{i.InstanceId,i.DefinitionId}).ToArray(),
            tactical=run.RogueRunState.TacticalItemInstances.Select(i=>new{i.InstanceId,i.DefinitionId,i.ChargesCurrent}).ToArray()});
        private static IEnumerator Visit(string node)
        {
            yield return Click("map.node."+node); yield return Click("前往这里");
            yield return new WaitForSecondsRealtime(.5f);
        }
        private static IEnumerator Battle(string node,int choice)
        {
            if(Host.CurrentMapRun.CurrentNodeId!=node || Host.CurrentFlowPhase!=CombatFlowPhase.Active)
            { yield return Click("map.node."+node); yield return Click("进入战斗"); }
            yield return new WaitForSecondsRealtime(1); yield return Fight();
            if(Host.CurrentFlowPhase!=CombatFlowPhase.Victory)throw new InvalidOperationException("Not victorious: "+node);
            yield return new WaitForSecondsRealtime(1);
            for(int i=0;i<10 && FindButton("固定战利品_0")!=null;i++)
            { yield return Click("固定战利品_0");yield return new WaitForSecondsRealtime(.75f); }
            yield return Click("战后奖励封套");yield return new WaitForSecondsRealtime(.75f);
            yield return Click("揭晓卡牌_"+choice);yield return new WaitForSecondsRealtime(.75f);
            string selected=Host.CurrentMapRun.SelectedRewardId;
            if(string.IsNullOrEmpty(selected))throw new InvalidOperationException("Reward was not selected: "+node);
            yield return Click("战后奖励封套");yield return new WaitForSecondsRealtime(.75f);
            if(node.StartsWith("B") && Host.CurrentMapRun.FirstRunExperience.RewardGroups.Single(g=>g.NodeId==node).SelectedId!=selected)
                throw new InvalidOperationException("Reward was not collected: "+node+" "+selected);
            File.AppendAllText(Path.Combine(Folder,"journey.txt"),"REWARD:PASS "+node+" "+selected+" gold="+Host.CurrentMapRun.Gold+" contribution="+Host.CurrentMapRun.StageContribution+"\n");
            yield return Click("离开战利品");yield return new WaitForSecondsRealtime(.75f);
        }
        public static string Signature() => Host.CurrentState==null?"none":Newtonsoft.Json.JsonConvert.SerializeObject(new {
            active=Host.CurrentState.ActiveUnitId, Host.CurrentState.CurrentTime,
            units=Host.CurrentState.Units.Values.OrderBy(u=>u.Id).Select(u=>new {u.Id,u.Health,u.Shield,u.Mana,u.ActionPoints,u.Position,u.ActionValue}).ToArray(),
            journal=Host.CurrentMapRun.CombatJournalRows.ToArray() });
        private static IEnumerator Resume()
        {
            string before=Signature();
            yield return Click("暂离并保存");
            yield return Click("继续游戏");
            yield return new WaitForSecondsRealtime(1.8f);
            if(before!=Signature())throw new InvalidOperationException("Resume signature changed\n"+before+"\n"+Signature());
        }
    }
}
