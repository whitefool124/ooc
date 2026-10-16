using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace OCC.Combat.EditorTools
{
    // Uses normal UI event handlers and camera-projected raycast hits in an isolated save scope.
    public static class Hd2DCombatReviewAudit
    {
        public static string Status{get;private set;}="idle";
        public static int Commands{get;private set;}
        public static int Moves{get;private set;}
        public static int Attacks{get;private set;}
        public static string Evidence{get;private set;}="";
        private static CombatPrototypeBootstrap Host=>Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
        public static string Enter()
        {
            if(Application.dataPath!="E:/数据库/OCC_Codex/UnityProject/Assets"||!Application.isPlaying||Host==null)throw new InvalidOperationException("Main project Play required");
            AcademyJourneyAudit.Begin("hd2d-playable-native-input-"+Guid.NewGuid().ToString("N"));Host.EnterMainMenuFromOpening();
            Commands=Moves=Attacks=0;Evidence="";Host.StartCoroutine(Guard(Entry()));return Status;
        }
        public static string Fight(){Host.StartCoroutine(Guard(Combat()));return Status;}
        private static IEnumerator Guard(IEnumerator root)
        {
            Status="running";var stack=new Stack<IEnumerator>();stack.Push(root);
            while(stack.Count>0)
            {
                bool moved=false;object value=null;Exception error=null;
                try{moved=stack.Peek().MoveNext();if(moved)value=stack.Peek().Current;}catch(Exception e){error=e;}
                if(error!=null){Status="FAIL: "+error;Debug.LogError(Status);yield break;}
                if(!moved){stack.Pop();continue;}if(value is IEnumerator child){stack.Push(child);continue;}yield return value;
            }
            Status="PASS commands="+Commands+" moves="+Moves+" attacks="+Attacks+" "+Evidence;
        }
        private static Button Button(string name)=>Object.FindObjectsByType<Button>().FirstOrDefault(b=>b.isActiveAndEnabled&&b.IsInteractable()&&(b.name==name||b.name=="按钮_"+name));
        private static IEnumerator Click(string name)
        {
            float until=Time.realtimeSinceStartup+5;Button button;
            while((button=Button(name))==null&&Time.realtimeSinceStartup<until)yield return null;
            if(button==null)throw new InvalidOperationException("Missing button "+name);
            yield return new WaitForSecondsRealtime(.2f);
            var r=(RectTransform)button.transform;
            yield return Pointer(RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center)),button.gameObject,MouseButton.Left);
            yield return new WaitForSecondsRealtime(.4f);
        }
        private static IEnumerator Pointer(Vector2 point,GameObject expected,MouseButton button)
        {
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            if(hits.Count==0||!hits[0].gameObject.transform.IsChildOf(expected.transform))throw new InvalidOperationException("Pointer blocked at "+point+" expected="+expected.name+" hit="+(hits.Count==0?"none":hits[0].gameObject.name));
            var eventData=new PointerEventData(EventSystem.current){position=point,button=button==MouseButton.Right?PointerEventData.InputButton.Right:PointerEventData.InputButton.Left,clickCount=1};
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,eventData,ExecuteEvents.pointerClickHandler);
            yield return null;yield return null;
        }
        private static Vector2 Point(Hd2DBattlefieldSample sample,GridPosition p)
        {
            var world=sample.RenderCamera.transform.parent;
            var v=sample.RenderCamera.WorldToViewportPoint(world.TransformPoint(new Vector3(p.X+.5f,0,p.Y+.5f)));
            var rect=(RectTransform)sample.transform;
            return RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(new Vector2(rect.rect.xMin+v.x*rect.rect.width,rect.rect.yMin+v.y*rect.rect.height)));
        }
        internal static IEnumerator Cell(Hd2DBattlefieldSample sample,GridPosition p,MouseButton button=MouseButton.Left)
        {yield return Pointer(Point(sample,p),sample.gameObject,button);}
        private static IEnumerator Entry()
        {
            yield return new WaitForSecondsRealtime(2);yield return Click("开始游戏");yield return new WaitForSecondsRealtime(1.6f);
            yield return Click("以此角色开始");yield return Click("进入首次地图");yield return Click("不需要教程");
            yield return Click("map.node.B1");yield return Click("进入战斗");
            while(Host.IsInteractionModalOpen)yield return null;
            var view=Object.FindAnyObjectByType<FormalBattlefieldView>();if(view==null)throw new InvalidOperationException("Missing battlefield");
            yield return new WaitForSecondsRealtime(.5f);
            var sample=view.Hd2DPreview;if(sample==null)throw new InvalidOperationException("Normal entry did not create HD2D battlefield");
            Evidence="projection="+sample.CheckCellRoundTrips();
            yield return Cell(sample,Host.CurrentState.GetUnit("hero").Position,MouseButton.Right);
            if(!Host.IsInteractionModalOpen)throw new InvalidOperationException("Right menu did not open");
            var point=Point(sample,Host.CurrentState.GetUnit("hero").Position);
            ExecuteEvents.Execute(sample.gameObject,new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left,clickCount=1},ExecuteEvents.pointerClickHandler);yield return null;
            if(Host.IsInteractionModalOpen)throw new InvalidOperationException("Context menu did not close");Evidence+=" context=PASS";
        }
        private static IEnumerator Combat()
        {
            var sample=Object.FindAnyObjectByType<Hd2DBattlefieldSample>();if(sample==null)throw new InvalidOperationException("Missing sample");
            int limit=50;
            while(Host.CurrentFlowPhase==CombatFlowPhase.Active&&Commands<limit)
            {
                if(Host.IsInteractionModalOpen||Host.CurrentState.ActiveUnitId!="hero"){yield return new WaitForSecondsRealtime(.05f);continue;}
                var state=Host.CurrentState;var command=AcademyCombatAudit.ChooseHero(state);
                if(command.Type==CombatCommandType.EndTurn){yield return Click("结束行动");Commands++;continue;}
                var target=string.IsNullOrEmpty(command.TargetUnitId)?command.Destination:state.GetUnit(command.TargetUnitId).Position;
                if(command.Type==CombatCommandType.Move)
                {
                    Host.SelectHudAction("移动");
                    for(int i=0;i<3&&Host.SelectedAction!="移动";i++)
                    {
                        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.Escape));yield return null;yield return null;
                        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;
                    }
                    if(Host.SelectedAction!="移动")throw new InvalidOperationException("Could not leave self spell selection");
                    yield return Cell(sample,state.GetUnit("hero").Position);
                }
                else if(command.Type==CombatCommandType.UseSkill)yield return Click("技能"+(command.SlotIndex+1));
                else Host.SelectHudAction(command.Type==CombatCommandType.Attack?"攻击":"互动");
                int before=state.GetUnit("hero").ActionPoints;var from=state.GetUnit("hero").Position;
                yield return Cell(sample,target);
                if(state.GetUnit("hero").ActionPoints!=before)throw new InvalidOperationException("First click unexpectedly committed");
                yield return Cell(sample,target);yield return new WaitForSecondsRealtime(.05f);
                if(state.GetUnit("hero").ActionPoints==before)throw new InvalidOperationException("Second click did not commit "+command.Type+" "+target);
                if(!Host.IsMapRunSaved)throw new InvalidOperationException("Journal save failed");
                Commands++;if(command.Type==CombatCommandType.Move)Moves++;else Attacks++;
                yield return new WaitForSecondsRealtime(.7f);
                if(command.Type==CombatCommandType.Move&&state.GetUnit("hero").Position==from)throw new InvalidOperationException("Hero did not move");
                // Victory releases the battlefield world; only project while it is active.
                if(Host.CurrentFlowPhase==CombatFlowPhase.Active&&sample.CheckCellRoundTrips()!=state.Map.Width*state.Map.Height)throw new InvalidOperationException("Projection changed");
            }
            if(Host.CurrentFlowPhase!=CombatFlowPhase.Victory||Moves==0||Attacks==0)throw new InvalidOperationException("Incomplete combat "+Host.CurrentFlowPhase);
            Evidence+=" victory=PASS saved="+Host.IsMapRunSaved;
        }
    }
}
