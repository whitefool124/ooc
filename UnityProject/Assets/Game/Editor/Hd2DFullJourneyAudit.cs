using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using OCC.Combat.Presentation;
using OCC.Combat.Roguelite;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace OCC.Combat.EditorTools
{
    // Normal UI route, production save adapter in a separate verification namespace.
    // Every action checks the actual EventSystem hit before dispatch. No window focus,
    // preview run, debug victories, extra money, equipment or altered combat statistics.
    [InitializeOnLoad]
    public static class Hd2DFullJourneyAudit
    {
        private const string Key="OCC.Hd2DFullJourney";
        private static bool BranchMode=>SessionState.GetBool(Key+".branches",false);
        private static bool SupplementMode=>SessionState.GetBool(Key+".supplement",false);
        private static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-16_HD2D完整流程交互验收/"+(SupplementMode?"runtime_supplement":BranchMode?"runtime_branches":"runtime")));
        private static CombatPrototypeBootstrap Host=>Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
        private static IDisposable saveScope;private static Dictionary<string,string> originalSaves;
        private static int actions,captures;private static bool started,finished;
        static Hd2DFullJourneyAudit(){EditorApplication.update+=Tick;}
        public static void BeginBranches(){SessionState.SetBool(Key+".branches",true);Begin();}
        public static void BeginSupplement(){SessionState.SetBool(Key+".supplement",true);Begin();}
        public static void Begin()
        {
            if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets"||!Application.isBatchMode||EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Idle main batch Editor required");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Dirty scene must be preserved");
            Directory.CreateDirectory(Folder);SessionState.SetBool(Key,true);
            EditorSceneManager.OpenScene("Assets/Scenes/CombatPrototype.unity");EditorApplication.EnterPlaymode();
        }
        private static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(Application.isPlaying)EditorApplication.QueuePlayerLoopUpdate();
            if(finished&&!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetBool(Key,false);File.WriteAllText(Path.Combine(Folder,"cleanup.json"),JsonConvert.SerializeObject(new{play=Application.isPlaying,dirty=SceneManager.GetActiveScene().isDirty,sceneSaved=false}));
                EditorApplication.Exit(File.Exists(Path.Combine(Folder,"failure.json"))?1:0);return;
            }
            if(!Application.isPlaying||Host==null||started)return;
            started=true;originalSaves=new Dictionary<string,string>();
            for(int i=0;i<3;i++)foreach(var key in new[]{"OCC.FirstExperiencePrototype.v2.slot."+i,"occ.first_experience.map_run.slot."+i})originalSaves[key]=PlayerPrefs.GetString(key,"<missing>");
            saveScope=FirstExperienceSaveRouting.BeginVerificationScope(SupplementMode?"hd2d-supplement-20261016-interaction":BranchMode?"hd2d-branches-20261016-interaction":"hd2d-full-20261016-interaction");
            Host.StartCoroutine(Guard(SupplementMode?Supplement():BranchMode?Branches():Journey()));
        }
        private static IEnumerator Guard(IEnumerator routine)
        {
            var stack=new Stack<IEnumerator>();stack.Push(routine);
            while(stack.Count>0)
            {
                bool moved=false;object value=null;Exception error=null;
                try{moved=stack.Peek().MoveNext();if(moved)value=stack.Peek().Current;}catch(Exception e){error=e;}
                if(error!=null)
                {
                    Snapshot("failure");File.WriteAllText(Path.Combine(Folder,"failure.json"),JsonConvert.SerializeObject(new{error=error.ToString(),actions,buttons=Buttons().Select(b=>b.name).ToArray()},Formatting.Indented));
                    Debug.LogError(error);break;
                }
                if(!moved){stack.Pop();continue;}
                if(value is IEnumerator child){stack.Push(child);continue;}yield return value;
            }
            bool untouched=originalSaves.All(p=>PlayerPrefs.GetString(p.Key,"<missing>")==p.Value);
            File.WriteAllText(Path.Combine(Folder,"save_isolation.json"),JsonConvert.SerializeObject(new{playerSlotsUnchanged=untouched,normalUiEntry=!SupplementMode,debugStateCreated=SupplementMode,actions}));
            saveScope.Dispose();saveScope=null;finished=true;EditorApplication.ExitPlaymode();
        }
        private static Button[] Buttons()=>Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b=>b.isActiveAndEnabled&&b.IsInteractable()).ToArray();
        private static Button Find(string name)=>Buttons().FirstOrDefault(b=>b.name==name||b.name=="按钮_"+name||b.name=="服务_"+name||
            b.name.StartsWith("位置行动_")&&b.GetComponentsInChildren<Text>().Any(t=>t.text.StartsWith(name)));
        private static void Log(string message)=>File.AppendAllText(Path.Combine(Folder,"journey.txt"),DateTime.UtcNow.ToString("O")+" "+message+"\n");
        private static Vector2 ScreenPoint(RectTransform rect,Vector2? local=null)
        {
            var canvas=rect.GetComponentInParent<Canvas>();var camera=canvas==null||canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            var p=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(local??rect.rect.center));
            if(camera!=null&&camera.targetTexture!=null)p=new Vector2(p.x*Screen.width/camera.pixelWidth,p.y*Screen.height/camera.pixelHeight);
            return p;
        }
        private static IEnumerator Click(string name)
        {
            float end=Time.realtimeSinceStartup+10;Button button;
            while((button=Find(name))==null&&Time.realtimeSinceStartup<end)yield return null;
            if(button==null)throw new InvalidOperationException("Missing button: "+name+" available="+string.Join(",",Buttons().Select(b=>b.name)));
            yield return null;Canvas.ForceUpdateCanvases();
            yield return Pointer(ScreenPoint((RectTransform)button.transform),button.gameObject);
            Log("CLICK "+name);yield return new WaitForSecondsRealtime(.65f);
        }
        private static IEnumerator Pointer(Vector2 point,GameObject expected,PointerEventData.InputButton button=PointerEventData.InputButton.Left)
        {
            var e=new PointerEventData(EventSystem.current){position=point,button=button,clickCount=1};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(e,hits);
            if(hits.Count==0||!hits[0].gameObject.transform.IsChildOf(expected.transform))throw new InvalidOperationException("Input blocked: "+expected.name+" at "+point+" hit="+(hits.Count==0?"none":hits[0].gameObject.name));
            e.pointerCurrentRaycast=hits[0];e.pointerPressRaycast=hits[0];e.pressPosition=point;
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,e,ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,e,ExecuteEvents.pointerDownHandler);yield return null;
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,e,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,e,ExecuteEvents.pointerClickHandler);
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,e,ExecuteEvents.pointerExitHandler);yield return null;
        }
        private static IEnumerator Cell(GridPosition position,PointerEventData.InputButton button=PointerEventData.InputButton.Left)
        {
            var sample=Object.FindAnyObjectByType<Hd2DBattlefieldSample>();if(sample==null)throw new InvalidOperationException("Missing live HD2D battlefield");
            var v=sample.RenderCamera.WorldToViewportPoint(sample.RenderCamera.transform.parent.TransformPoint(new Vector3(position.X+.5f,0,position.Y+.5f)));
            var rect=(RectTransform)sample.transform;var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(new Vector2(rect.rect.xMin+v.x*rect.rect.width,rect.rect.yMin+v.y*rect.rect.height)));
            yield return Pointer(point,sample.gameObject,button);
        }
        private static void Snapshot(string name)
        {
            name=(++captures).ToString("D3")+"_"+name;var sample=Object.FindAnyObjectByType<Hd2DBattlefieldSample>();
            try{Hd2DBattleCanvasCapture.Capture(sample,1920,1080,Path.Combine(Folder,name+".png"));Hd2DBattleCanvasCapture.Capture(sample,960,540,Path.Combine(Folder,name+"_960.png"));}
            catch(Exception e){Log("CAPTURE FAILURE "+e.Message);}
            File.WriteAllText(Path.Combine(Folder,name+".json"),JsonConvert.SerializeObject(new{screen=new[]{Screen.width,Screen.height},node=Host?.CurrentMapRun?.CurrentNodeId,flow=Host?.CurrentFlowPhase.ToString(),buttons=Buttons().Select(b=>b.name).ToArray(),uiText=Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Where(t=>t.isActiveAndEnabled).Select(t=>t.text).ToArray()},Formatting.Indented));
        }
        private static IEnumerator Journey()
        {
            yield return new WaitForSecondsRealtime(4);if(Find("跳过动画")!=null)yield return Click("跳过动画");
            Snapshot("landing");
            if(Find("继续游戏")!=null){yield return Click("继续游戏");yield return new WaitForSecondsRealtime(2);Log("CONTINUE same acceptance archive");}
            else
            {
                yield return Click(Find("开始游戏")!=null?"开始游戏":"开始新游戏");
                yield return new WaitForSecondsRealtime(2);Snapshot("character");yield return Click("以此角色开始");yield return Click("进入首次地图");
                if(Find("不需要教程")!=null)yield return Click("不需要教程");
            }
            if(Host.CurrentMapRun==null||!Host.IsMapRunSaved)throw new InvalidOperationException("Normal entry failed to create saved run");
            if(Host.CurrentMapRun.AwaitingReward)yield return Rewards(Host.CurrentMapRun.CurrentNodeId=="B1"?1:Host.CurrentMapRun.CurrentNodeId=="B3"?2:0);
            if(!Host.CurrentMapRun.IsInAcademyLayer)
            {
                Snapshot("tutorial_map");if(!Done("B1"))yield return Battle("B1",1,true);
                if(!Done("EV1")){yield return Visit("EV1");yield return Click("接下温室传令");yield return Click("回到地图");}
                if(!Done("EV2")){yield return Visit("EV2");yield return Click("领取金币");yield return Click("回到地图");}
                if(!Done("B2"))yield return Battle("B2",0);
                if(!Done("EV3")){yield return Visit("EV3");yield return Click("领取学院贡献");yield return Click("回到地图");}
                if(!Done("B3")){yield return Visit("W");Snapshot("workshop");yield return Click("返回地图");yield return Battle("B3",2);}
                if(!Done("X"))
                {
                    yield return Visit("M");Snapshot("medical");
                    if(Find("服务_接受治疗")!=null)yield return Click("服务_接受治疗");
                    if(Find("餐食_MEAL-AETHER")!=null)yield return Click("餐食_MEAL-AETHER");yield return Click("返回地图");yield return Battle("X",0);
                }
                yield return Visit("S");Snapshot("shop");yield return Click("离开商店");
            }
            if(!Host.CurrentMapRun.IsInAcademyLayer)throw new InvalidOperationException("Tutorial did not hand off");Snapshot("academy_map");
            yield return Academy();
            Snapshot("summary");Log("COMPLETE normal-entry-to-summary actions="+actions);
            File.WriteAllText(Path.Combine(Folder,"completed.json"),JsonConvert.SerializeObject(new{normalEntry=true,actions,completedNodes=Host.CurrentMapRun.CompletedNodes.ToArray()}));
        }
        private static bool Done(string node)=>Host.CurrentMapRun.CompletedNodes.Contains(node);
        private static IEnumerator Supplement()
        {
            yield return new WaitForSecondsRealtime(3);
            string error=Host.PreviewUiCaptureRun(false,"S");if(error!=null)throw new InvalidOperationException(error);
            yield return new WaitForSecondsRealtime(8);error=Host.PreviewUiCapturePage("room","S");if(error!=null)throw new InvalidOperationException(error);
            yield return new WaitForSecondsRealtime(2);Snapshot("shop_before");
            int gold=Host.CurrentMapRun.Gold;yield return Click("购买");
            if(Host.CurrentMapRun.Gold>=gold)throw new InvalidOperationException("Shop did not complete purchase");Log("SHOP purchase PASS "+gold+"->"+Host.CurrentMapRun.Gold);
            Snapshot("shop_after");yield return Click("打开行囊");yield return new WaitForSecondsRealtime(1);Snapshot("shop_inventory");
            yield return Click("返回地图");yield return new WaitForSecondsRealtime(1);Snapshot("map_reading");
            error=Host.PreviewUiCaptureRun(false,"W");if(error!=null)throw new InvalidOperationException(error);
            yield return new WaitForSecondsRealtime(8);error=Host.PreviewUiCapturePage("room","W");if(error!=null)throw new InvalidOperationException(error);
            yield return new WaitForSecondsRealtime(2);Snapshot("workshop_reading");
            var card=Buttons().FirstOrDefault(b=>b.name.StartsWith("工坊卡_"));if(card!=null)yield return Click(card.name);
            yield return new WaitForSecondsRealtime(1);Snapshot("workshop_selected");yield return Click("页签_术式专精");yield return new WaitForSecondsRealtime(1);Snapshot("specialization_tab");
            File.WriteAllText(Path.Combine(Folder,"completed.json"),JsonConvert.SerializeObject(new{isolatedSupplement=true,shopPurchase=true,workshopTab=true,normalJourneyEvidence=false,actualScreen=new[]{Screen.width,Screen.height}}));
        }
        private static IEnumerator Branches()
        {
            yield return new WaitForSecondsRealtime(4);if(Find("跳过动画")!=null)yield return Click("跳过动画");
            if(Find("继续游戏")!=null){yield return Click("继续游戏");yield return new WaitForSecondsRealtime(2);}
            else
            {
                yield return Click(Find("开始游戏")!=null?"开始游戏":"开始新游戏");yield return new WaitForSecondsRealtime(2);
                yield return Click("以此角色开始");yield return Click("进入首次地图");if(Find("不需要教程")!=null)yield return Click("不需要教程");
            }
            Log("BRANCH actual screen="+Screen.width+"x"+Screen.height);
            if(Host.CurrentMapRun.AwaitingReward)yield return Rewards(1);
            if(!Done("B1"))yield return Battle("B1",1);
            if(!Done("EV1")){yield return Visit("EV1");yield return Click("接下温室传令");yield return Click("回到地图");}
            if(!Done("EV2")){yield return Visit("EV2");yield return Click("领取金币");yield return Click("回到地图");}
            if(Host.CurrentMapRun.CurrentNodeId!="B2")
            {yield return InterfaceProbe();yield return Click("map.node.B2");yield return Click("进入战斗");yield return new WaitForSecondsRealtime(9);}
            int health=Host.CurrentMapRun.CurrentHealth;int turns=0;
            while(Host.CurrentFlowPhase==CombatFlowPhase.Active&&turns<100)
            {
                if(Host.IsCombatEntryBlocking||Host.IsCombatActionPlaying||Host.CurrentState.ActiveUnitId!="hero"){yield return new WaitForSecondsRealtime(.1f);continue;}
                yield return Click("结束行动");turns++;
            }
            if(Host.CurrentFlowPhase!=CombatFlowPhase.Defeat)throw new InvalidOperationException("Natural defeat not reached");
            yield return new WaitForSecondsRealtime(1);Snapshot("death_outcome");yield return Click("结果重开");yield return Click("确认");yield return new WaitForSecondsRealtime(9);
            if(Host.CurrentFlowPhase!=CombatFlowPhase.Active||Host.CurrentState.GetUnit("hero").Health!=health)throw new InvalidOperationException("Death retry did not restore entry health");Log("DEATH retry PASS");
            string signature=AcademyJourneyAudit.Signature();yield return Click("离开战斗");yield return Click("取消");
            if(signature!=AcademyJourneyAudit.Signature())throw new InvalidOperationException("Cancel abandon changed battle");
            yield return Click("离开战斗");yield return Click("确认");yield return Click("结果返回");yield return new WaitForSecondsRealtime(1);
            if(Host.CurrentMapRun.RunEndReason!="abandon"||!Host.CurrentMapRun.IsComplete)throw new InvalidOperationException("Abandon result incorrect");
            Snapshot("abandon_summary");yield return Click("返回主界面");Snapshot("returned_home");
            File.WriteAllText(Path.Combine(Folder,"completed.json"),JsonConvert.SerializeObject(new{naturalDefeat=true,retryRestoredEntry=true,abandonCancelSafe=true,abandonSaved=true,returnedHome=true,actualScreen=new[]{Screen.width,Screen.height}}));
        }
        private static IEnumerator Visit(string node)
        {yield return Click("map.node."+node);yield return Click("前往这里");yield return new WaitForSecondsRealtime(1.2f);}
        private static IEnumerator Battle(string node,int choice,bool resume=false)
        {
            if(Host.CurrentMapRun.CurrentNodeId!=node||Host.CurrentFlowPhase!=CombatFlowPhase.Active)
            {yield return Click("map.node."+node);yield return Click("进入战斗");}yield return new WaitForSecondsRealtime(1.5f);
            yield return Fight(resume);if(Host.CurrentFlowPhase!=CombatFlowPhase.Victory)throw new InvalidOperationException("Battle lost: "+node);
            yield return Rewards(choice);Log("BATTLE PASSED "+node);
        }
        private static IEnumerator Fight(bool resume=false)
        {
            int count=0,unchanged=0;float end=Time.realtimeSinceStartup+900;
            while(Host.CurrentFlowPhase==CombatFlowPhase.Active&&Time.realtimeSinceStartup<end&&count<400)
            {
                if(Find("跳过")!=null){yield return Click("跳过");continue;}
                if(Host.IsCombatEntryBlocking||Host.IsCombatActionPlaying||Host.CurrentState.ActiveUnitId!="hero"){yield return new WaitForSecondsRealtime(.1f);continue;}
                if(resume&&count==1)
                {
                    string before=AcademyJourneyAudit.Signature();yield return Click("暂离并保存");yield return Click("继续游戏");yield return new WaitForSecondsRealtime(2);
                    if(AcademyJourneyAudit.Signature()!=before)throw new InvalidOperationException("Resume changed battle");resume=false;Log("RESUME PASSED");continue;
                }
                var state=Host.CurrentState;var hero=state.GetUnit("hero");var cmd=AcademyCombatAudit.ChooseHero(state);string beforeState=AcademyJourneyAudit.Signature();
                if(cmd.Type==CombatCommandType.EndTurn)yield return Click("结束行动");
                else
                {
                    var target=string.IsNullOrEmpty(cmd.TargetUnitId)?cmd.Destination:state.GetUnit(cmd.TargetUnitId).Position;
                    if(cmd.Type==CombatCommandType.UseSkill)yield return Click("技能"+(cmd.SlotIndex+1));
                    else {yield return Cell(target,PointerEventData.InputButton.Right);yield return Click(cmd.Type==CombatCommandType.Move?"移动到这里":cmd.Type==CombatCommandType.Attack?"攻击":"与这里互动");}
                    if(cmd.Type!=CombatCommandType.Move&&Host.CurrentFlowPhase==CombatFlowPhase.Active&&beforeState==AcademyJourneyAudit.Signature())
                    {yield return Cell(target);if(Host.CurrentFlowPhase==CombatFlowPhase.Active&&beforeState==AcademyJourneyAudit.Signature())yield return Cell(target);}yield return new WaitForSecondsRealtime(.8f);
                }
                unchanged=beforeState==AcademyJourneyAudit.Signature()?unchanged+1:0;
                if(unchanged>=3)throw new InvalidOperationException("Three commands did not change state: "+cmd.Type+" action="+Host.SelectedAction);
                actions++;count++;if(!Host.IsMapRunSaved)throw new InvalidOperationException("Command save failed");
                if(count%12==0)Log("FIGHT "+Host.CurrentMapRun.CurrentNodeId+" commands="+count+" hp="+hero.Health);
            }
            Snapshot("battle_"+Host.CurrentMapRun.CurrentNodeId+"_"+Host.CurrentFlowPhase);
            if(Host.CurrentFlowPhase==CombatFlowPhase.Active)throw new InvalidOperationException("Battle did not finish");
        }
        private static IEnumerator Rewards(int choice)
        {
            yield return new WaitForSecondsRealtime(1.5f);
            for(int i=0;i<25;i++)
            {
                if(Find("揭晓卡牌_"+choice)!=null){Snapshot("reveal");yield return Click("揭晓卡牌_"+choice);}
                else if(Find("揭晓卡牌_0")!=null)yield return Click("揭晓卡牌_0");
                else if(Find("返回清点")!=null)yield return Click("返回清点");
                else if(Find("固定战利品_0")!=null)yield return Click("固定战利品_0");
                else if(Find("战后奖励封套")!=null)yield return Click("战后奖励封套");
                else if(Find("reward.first")!=null)yield return Click("reward.first");
                else break;
            }
            Snapshot("rewards");if(Find("离开战利品")!=null)yield return Click("离开战利品");
        }
        private static IEnumerator Academy()
        {
            if(Find("返回地图")!=null)yield return Click("返回地图");
            if(Host.CurrentFlowPhase==CombatFlowPhase.Active&&Host.IsBattlefieldVisible)
            {yield return Fight();if(Host.CurrentFlowPhase==CombatFlowPhase.Victory)yield return Rewards(0);else throw new InvalidOperationException("Resumed battle lost");}
            yield return InterfaceProbe();
            for(int step=0;step<75;step++)
            {
                if(Find("离开战利品")!=null)yield return Rewards(0);
                if(Host.CurrentMapRun.IsComplete)yield break;
                var run=Host.CurrentMapRun;
                var next=run.MapNodes.Where(n=>!run.CompletedNodes.Contains(n.Id)&&run.IsNodeAvailable(n.Id)&&n.Id!=run.CurrentNodeId)
                    .OrderBy(n=>n.Type==RogueliteMapNodeType.Finale?0:n.Type==RogueliteMapNodeType.Medical?1:n.IsCombat?3:2).FirstOrDefault();
                if(next==null)next=Backtrack(run);
                if(next==null)throw new InvalidOperationException("No reachable academy frontier");
                Log("NEXT "+next.Id+" "+next.Type);yield return Click("map.node."+next.Id);
                if(run.CompletedNodes.Contains(next.Id))
                {yield return Click("再去看看");yield return Click("回到地图");Log("BACKTRACK "+next.Id);continue;}
                if(next.IsCombat)
                {
                    yield return Click("进入战斗");
                    if(Find("确认")!=null)
                    {
                        string current=run.CurrentNodeId;Snapshot("finale_confirmation");
                        var under=Find("进入战斗");var hits=new List<RaycastResult>();
                        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=ScreenPoint((RectTransform)under.transform)},hits);
                        if(hits.Count>0&&hits[0].gameObject.transform.IsChildOf(under.transform))throw new InvalidOperationException("Confirmation leaks clicks to departure");
                        yield return Click("取消");if(run.CurrentNodeId!=current)throw new InvalidOperationException("Cancel changed current node");
                        yield return Click("进入战斗");yield return Click("确认");Log("FINALE confirmation/cancel/modal-block PASS");
                    }
                    yield return Fight();
                    if(Host.CurrentFlowPhase!=CombatFlowPhase.Victory)throw new InvalidOperationException("Academy battle lost: "+next.Id);
                    yield return Rewards(0);if(next.Type==RogueliteMapNodeType.Finale)yield break;
                }
                else
                {
                    yield return Click("前往这里");yield return new WaitForSecondsRealtime(1);Snapshot("node_"+next.Id);
                    if(next.Type==RogueliteMapNodeType.Shop&&Find("购买")!=null)
                    {int gold=run.Gold;yield return Click("购买");Log("SHOP gold="+gold+"->"+run.Gold);}
                    else if(next.Type==RogueliteMapNodeType.Workshop)
                    {
                        var card=Buttons().FirstOrDefault(b=>b.name.StartsWith("工坊卡_"));
                        if(card!=null){yield return Click(card.name);if(Find("确认安装")!=null)yield return Click("确认安装");}
                    }
                    else
                    {
                        var choice=run.CurrentContentChoices.FirstOrDefault(c=>RogueliteEconomyPresentation.ForNodeChoice(run,c).CanExecute);
                        if(choice!=null)yield return Click(choice.DisplayName);
                    }
                    yield return new WaitForSecondsRealtime(1);
                    if(Host.CurrentFlowPhase==CombatFlowPhase.Active&&Host.IsBattlefieldVisible)
                    {yield return Fight();if(Host.CurrentFlowPhase!=CombatFlowPhase.Victory)throw new InvalidOperationException("Event combat lost");yield return Rewards(0);}
                    else if(Find("回到地图")!=null)yield return Click("回到地图");
                    else if(Find("离开商店")!=null)yield return Click("离开商店");
                    else if(Find("返回地图")!=null)yield return Click("返回地图");
                }
            }
            throw new InvalidOperationException("Academy node limit");
        }
        private static RogueliteMapNode Backtrack(RogueliteMapRun run)
        {
            var queue=new Queue<List<string>>();queue.Enqueue(new List<string>{run.CurrentNodeId});var seen=new HashSet<string>{run.CurrentNodeId};
            while(queue.Count>0)
            {
                var path=queue.Dequeue();var current=run.MapNode(path.Last());
                foreach(var node in run.MapNodes.Where(n=>n.Type!=RogueliteMapNodeType.Finale&&(current.NextIds.Contains(n.Id)||n.NextIds.Contains(current.Id))))
                {
                    if(!seen.Add(node.Id))continue;var next=new List<string>(path){node.Id};
                    if(!run.CompletedNodes.Contains(node.Id))return run.MapNode(next[1]);
                    queue.Enqueue(next);
                }
            }
            return null;
        }
        private static IEnumerator InterfaceProbe()
        {
            var viewport=Object.FindAnyObjectByType<RogueMapViewportController>();
            if(viewport!=null)
            {
                var hitRect=(RectTransform)typeof(RogueMapViewportController).GetField("viewport",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(viewport);
                var p=ScreenPoint(hitRect);var e=new PointerEventData(EventSystem.current){position=p,button=PointerEventData.InputButton.Left,scrollDelta=Vector2.up};
                var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(e,hits);
                if(hits.Count==0||!hits[0].gameObject.transform.IsChildOf(hitRect))throw new InvalidOperationException("Map scroll surface blocked at "+p+" hit="+(hits.Count==0?"none":hits[0].gameObject.name));
                int zoom=viewport.ZoomIndex;ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,e,ExecuteEvents.scrollHandler);yield return null;
                if(viewport.ZoomIndex==zoom)throw new InvalidOperationException("Map wheel did not zoom");
                var before=viewport.MapContent.anchoredPosition;
                ExecuteEvents.Execute(viewport.gameObject,e,ExecuteEvents.beginDragHandler);e.delta=new Vector2(110,50);e.position+=e.delta;
                ExecuteEvents.Execute(viewport.gameObject,e,ExecuteEvents.dragHandler);ExecuteEvents.Execute(viewport.gameObject,e,ExecuteEvents.endDragHandler);yield return null;
                if(viewport.MapContent.anchoredPosition==before)throw new InvalidOperationException("Map drag did not pan");
                e.scrollDelta=Vector2.down;ExecuteEvents.Execute(viewport.gameObject,e,ExecuteEvents.scrollHandler);yield return null;Log("MAP wheel/pan PASS");
            }
            yield return Click("背包");yield return new WaitForSecondsRealtime(1.2f);Snapshot("loadout_before");yield return null;yield return null;
            var ui=Object.FindAnyObjectByType<FormalRogueliteUi>();var flags=BindingFlags.NonPublic|BindingFlags.Instance;
            var runtime=(RogueEquipmentRuntime)typeof(FormalRogueliteUi).GetField("loadoutDragRuntime",flags).GetValue(ui);
            var grid=(RectTransform)typeof(FormalRogueliteUi).GetField("loadoutGridRect",flags).GetValue(ui);
            float size=(float)typeof(FormalRogueliteUi).GetField("loadoutCellSize",flags).GetValue(ui);
            var items=RogueInventoryPresentation.Build(runtime);var item=items.OrderBy(i=>i.IsEquipment?0:1).FirstOrDefault();
            if(item==null)throw new InvalidOperationException("No inventory item for drag probe");
            Vector2Int? destination=null;
            for(int y=0;y<=runtime.BackpackRows-item.Height&&destination==null;y++)for(int x=0;x<=runtime.BackpackColumns-item.Width;x++)
                if((x!=item.X||y!=item.Y)&&runtime.CanMoveBackpack(item.InstanceId,x,y,item.Rotated))
                {destination=new Vector2Int(x,y);break;}
            if(!destination.HasValue)throw new InvalidOperationException("No free rectangle for drag probe");
            var source=Find("背包物品_"+item.InstanceId);if(source==null)throw new InvalidOperationException("Missing item control");
            var start=ScreenPoint((RectTransform)source.transform);var local=(Vector2)grid.InverseTransformPoint(source.transform.TransformPoint(((RectTransform)source.transform).rect.center));
            var end=ScreenPoint(grid,local+new Vector2((destination.Value.x-item.X)*size,-(destination.Value.y-item.Y)*size));
            var pointer=new PointerEventData(EventSystem.current){position=start,pressPosition=start,button=PointerEventData.InputButton.Left};
            var raycasts=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,raycasts);
            if(raycasts.Count==0||!raycasts[0].gameObject.transform.IsChildOf(source.transform))throw new InvalidOperationException("Inventory pickup blocked");
            pointer.pointerCurrentRaycast=pointer.pointerPressRaycast=raycasts[0];
            ExecuteEvents.ExecuteHierarchy(source.gameObject,pointer,ExecuteEvents.pointerEnterHandler);yield return new WaitForSecondsRealtime(.8f);
            Log("HOVER visible="+Object.FindObjectsByType<FormalHoverTooltip>().Any(t=>t.IsVisible));
            ExecuteEvents.ExecuteHierarchy(source.gameObject,pointer,ExecuteEvents.pointerExitHandler);
            // Capture may resize the offscreen canvas for a frame. Resolve the
            // pointer against the settled live paper immediately before pickup.
            start=ScreenPoint((RectTransform)source.transform);local=(Vector2)grid.InverseTransformPoint(source.transform.TransformPoint(((RectTransform)source.transform).rect.center));
            end=ScreenPoint(grid,local+new Vector2((destination.Value.x-item.X)*size,-(destination.Value.y-item.Y)*size));pointer.position=start;pointer.pressPosition=start;
            var localArgs=new object[]{start,Vector2.zero};typeof(FormalRogueliteUi).GetMethod("TryLoadoutLocalPointer",flags).Invoke(ui,localArgs);
            var actualStart=(Vector2)localArgs[1];int grabX=Mathf.FloorToInt(actualStart.x/size)-item.X,grabY=Mathf.FloorToInt(-actualStart.y/size)-item.Y;
            end=ScreenPoint(grid,new Vector2((destination.Value.x+grabX+.5f)*size,-(destination.Value.y+grabY+.5f)*size));
            var endArgs=new object[]{end,Vector2.zero};typeof(FormalRogueliteUi).GetMethod("TryLoadoutLocalPointer",flags).Invoke(ui,endArgs);
            Log("DRAG planned="+destination+" source="+item.X+","+item.Y+" local="+local+" roundtrip="+localArgs[1]+" end="+endArgs[1]+" cell="+size+" screen="+Screen.width+"x"+Screen.height);
            ExecuteEvents.ExecuteHierarchy(source.gameObject,pointer,ExecuteEvents.pointerDownHandler);yield return null;
            pointer.delta=end-start;pointer.position=end;ExecuteEvents.ExecuteHierarchy(source.gameObject,pointer,ExecuteEvents.dragHandler);yield return null;
            ExecuteEvents.ExecuteHierarchy(source.gameObject,pointer,ExecuteEvents.pointerUpHandler);yield return new WaitForSecondsRealtime(1);
            runtime=(RogueEquipmentRuntime)typeof(FormalRogueliteUi).GetField("loadoutDragRuntime",flags).GetValue(ui);
            var placement=runtime.Backpack[item.InstanceId];
            if(placement.X!=destination.Value.x||placement.Y!=destination.Value.y||!Host.IsMapRunSaved)throw new InvalidOperationException("Inventory drop not saved: expected="+destination+" actual="+placement.X+","+placement.Y);
            Snapshot("loadout_after_drag");Log("INVENTORY drag/drop/save PASS "+item.InstanceId+" "+placement.X+","+placement.Y);
            yield return Click("返回地图");
        }
    }
}
