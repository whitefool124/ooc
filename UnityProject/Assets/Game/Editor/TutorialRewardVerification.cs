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
    public static class TutorialRewardVerification
    {
        private static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-04_第二关奖励修复/验证"));
        [MenuItem("OCC/Verification/Tutorial Rewards")]
        public static void Record()
        {
            if(!Application.isPlaying || Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Main Play required");
            Directory.CreateDirectory(Folder);File.Delete(Path.Combine(Folder,"runtime.txt"));DetailsInputFixture.Begin(Application.runInBackground);
            EditorApplication.playModeStateChanged-=Restore;EditorApplication.playModeStateChanged+=Restore;
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();Object.FindAnyObjectByType<CombatPrototypeBootstrap>().StartCoroutine(Guard(Routine()));
        }
        private static void Restore(PlayModeStateChange s){if(s!=PlayModeStateChange.ExitingPlayMode)return;DetailsInputFixture.End();EditorApplication.playModeStateChanged-=Restore;}
        private static IEnumerator Guard(IEnumerator root)
        {
            var stack=new Stack<IEnumerator>();stack.Push(root);
            while(stack.Count>0){bool moved=false;object value=null;Exception error=null;try{moved=stack.Peek().MoveNext();if(moved)value=stack.Peek().Current;}catch(Exception e){error=e;}
                if(error!=null){File.WriteAllText(Path.Combine(Folder,"runtime.txt"),"FAIL: "+error);DetailsInputFixture.End();Debug.LogException(error);yield break;}
                if(!moved){stack.Pop();continue;}if(value is IEnumerator child){stack.Push(child);continue;}yield return value;}
        }
        private static IEnumerator Click(string name)
        {
            yield return new WaitForEndOfFrame();var button=Object.FindObjectsByType<Button>().Single(b=>b.isActiveAndEnabled && b.IsInteractable() && b.name==name);
            var r=(RectTransform)button.transform;var p=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=p},hits);
            if(hits.Count==0 || hits[0].gameObject.GetComponentInParent<Button>()!=button)throw new InvalidOperationException("Blocked "+name);
            File.AppendAllText(Path.Combine(Folder,"clicks.txt"),name+" hit="+hits[0].gameObject.name+"\n");
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=p});yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=p}.WithButton(MouseButton.Left));yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=p});yield return new WaitForSecondsRealtime(.25f);
        }
        private static IEnumerator Shot(string name)
        {
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=new Vector2(1910,1070)});yield return null;yield return new WaitForEndOfFrame();
            var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Folder,name+".png"),t.EncodeToPNG());Object.Destroy(t);
        }
        private static IEnumerator Routine()
        {
            var host=Object.FindAnyObjectByType<CombatPrototypeBootstrap>();string error=host.PreviewUiCaptureRun(false,"B1",special:"reward");if(error!=null)throw new InvalidOperationException(error);
            yield return new WaitForSecondsRealtime(.5f);var run=host.CurrentMapRun;
            foreach(var entry in new[]{new{node="B1",index=1,id="F-P-R19"},new{node="B2",index=0,id="F-P-U04"},new{node="B3",index=2,id="F-P-M12"}})
            {
                if(entry.node!="B1")
                {
                    if(entry.node=="B2"){run.SelectNode("EV1");run.ChooseCurrentNodeContent("FIRST-EV1-ACCEPT-DELIVERY");run.SelectNode("EV2");run.ChooseCurrentNodeContent("FIRST-EV2-GOLD");}
                    else{run.SelectNode("EV3");run.ChooseCurrentNodeContent(run.CurrentContentChoices[1].Id);}
                    run.SelectNode(entry.node);run.CompleteCurrentCombat();Object.FindAnyObjectByType<RogueliteSettlementPresentation>().RefreshNow();yield return new WaitForSecondsRealtime(.25f);
                }
                if(run.RewardChoicesOpened || !string.IsNullOrEmpty(run.SelectedRewardId))throw new InvalidOperationException("Stale group state at "+entry.node);
                yield return Shot(entry.node+"-envelope");yield return Click("固定战利品_0");yield return Click("固定战利品_0");
                yield return Click("战后奖励封套");yield return Shot(entry.node+"-choices");
                yield return Click("揭晓卡牌_"+entry.index);
                if(run.SelectedRewardId!=entry.id)throw new InvalidOperationException("Wrong selected reward "+entry.node);
                yield return Click("战后奖励封套");
                if(!run.RogueRunState.MasteredSpellIds.Contains(entry.id) || run.HasUnclaimedLoot)throw new InvalidOperationException("Reward not actually collected at "+entry.node);
                yield return Click("离开战利品");
                if(run.RewardChoicesOpened || !string.IsNullOrEmpty(run.SelectedRewardId) || run.LootPendingExit || run.AwaitingReward)throw new InvalidOperationException("Group cleanup failed "+entry.node);
            }
            File.WriteAllText(Path.Combine(Folder,"runtime.txt"),"PASS: actual Input System reveal/select/collect/fixed resource/leave clicks for B1, B2 and B3 in one disposable memory run. All three groups start unopened, grant selected spells and clean up. Battles/events prepared by model, not end-to-end played; no player slot or disk-save writes. Legacy serialization repair and valid selection preservation tested separately.");DetailsInputFixture.End();
        }
    }
}
