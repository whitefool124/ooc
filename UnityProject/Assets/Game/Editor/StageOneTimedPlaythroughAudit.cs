using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.EditorTools
{
    public static partial class StageOnePlaythroughAudit
    {
        private static bool timedGateChecked;
        public static IEnumerator RunTimedInTest()
        {
            if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets"||!Application.isPlaying||Host==null)
                throw new InvalidOperationException("Main project runtime required");
            inTest=true;recoveryOnly=false;actions=0;captures=0;started=true;finished=false;timedGateChecked=false;
            bool balanced=Environment.GetEnvironmentVariable("OCC_BALANCE_UI")=="1";
            folderOverride=Path.GetFullPath(Path.Combine(Application.dataPath,balanced ? "../../Worldbuilding/归档/2026-10-22_第一阶段战斗测试与平衡/runtime" : "../../Worldbuilding/归档/2026-10-21_第一阶段继续试玩/runtime"));
            Directory.CreateDirectory(Folder);originalSaves=new Dictionary<string,string>();
            for(int i=0;i<3;i++)foreach(var key in new[]{"OCC.FirstExperiencePrototype.v2.slot."+i,"occ.first_experience.map_run.slot."+i})
                originalSaves[key]=PlayerPrefs.GetString(key,"<missing>");
            saveScope=FirstExperienceSaveRouting.BeginVerificationScope(balanced ? "stage-one-balanced-20261021" : "stage-one-timed-route-20261021");
            FirstExperienceSaveRouting.SelectSlot(0);
            var store=new PlayerPrefsRogueliteSaveStore();
            if(!store.HasKey(RogueliteSaveGateway.MapRunKey))store.SetString(RogueliteSaveGateway.MapRunKey,File.ReadAllText(Path.GetFullPath(Path.Combine(Application.dataPath,
                "../../Worldbuilding/归档/2026-10-21_第一阶段卡死修复/preserved_stage13_slot.txt"))));
            store.Flush();Application.logMessageReceived+=RecordRuntimeError;
            yield return Guard(TimedJourney());
            folderOverride=null;
        }
        private static IEnumerator TimedJourney()
        {
            yield return new WaitForSecondsRealtime(3);
            Host.GetComponent<FirstExperiencePrototypeController>().ShowLanding();
            yield return Click("继续游戏");yield return WaitForMap();
            Snapshot("continued_saved_round");
            Log("TIMED ROUTE continued seed="+Host.CurrentMapRun.Seed+" time="+Host.CurrentMapRun.StageTime);
            yield return Academy(true);
            if(!timedGateChecked||!Host.CurrentMapRun.IsComplete)throw new InvalidOperationException("Timed route did not reach final summary");
            Snapshot("timed_summary");
            File.WriteAllText(Path.Combine(Folder,"completed.json"),JsonConvert.SerializeObject(new{
                resumedRealStage13Snapshot=true,timedGateChecked,actions,seed=Host.CurrentMapRun.Seed,
                time=Host.CurrentMapRun.StageTime,completed=Host.CurrentMapRun.CompletedNodes.ToArray()},Formatting.Indented));
        }
        private static IEnumerator CheckTimedGate()
        {
            var run=Host.CurrentMapRun;
            if(run.AwaitingReward||run.LootPendingExit)throw new InvalidOperationException("Timed gate interrupted rewards");
            if(run.MapNodes.Any(n=>n.Type!=RogueliteMapNodeType.Finale&&!run.VisitedNodes.Contains(n.Id)&&run.IsNodeAvailable(n.Id)))
                throw new InvalidOperationException("Timed gate still permits a fresh normal node");
            Snapshot("timed_gate");
            File.WriteAllText(Path.Combine(Folder,"timed_gate.json"),JsonConvert.SerializeObject(new{
                time=run.StageTime,pending=run.IsTransitionPending,run.AwaitingReward,run.LootPendingExit,
                finaleAvailable=run.IsNodeAvailable(RogueliteAcademyLayerCatalog.FinaleNodeId),
                completed=run.CompletedNodes.ToArray()},Formatting.Indented));
            yield return Click("背包");yield return Click("返回地图");
            if(!Host.CurrentMapRun.IsTransitionPending)throw new InvalidOperationException("Loadout return lost timed gate");
            timedGateChecked=true;Log("TIMED GATE rewards settled; fresh ordinary routes locked; loadout return PASS");
        }
    }
}
