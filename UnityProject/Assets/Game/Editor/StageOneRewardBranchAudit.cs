using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using OCC.Combat.Presentation;
using OCC.Combat.Roguelite;
using UnityEngine;
using Object=UnityEngine.Object;

namespace OCC.Combat.EditorTools
{
    public static partial class StageOnePlaythroughAudit
    {
        // Branch fixtures prepare legal post-combat saves. All subsequent actions
        // use the production host, save gateway and EventSystem raycast clicks.
        public static IEnumerator RunRewardsInTest()
        {
            if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets" || !Application.isPlaying || Host==null)
                throw new InvalidOperationException("Main project runtime required");
            inTest=true; recoveryOnly=false; actions=0; captures=0; started=true; finished=false;
            folderOverride=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-22_第一阶段奖励界面分支验收/runtime"));
            Directory.CreateDirectory(Folder); originalSaves=new Dictionary<string,string>();
            for(int i=0;i<3;i++) foreach(string key in new[]{"OCC.FirstExperiencePrototype.v2.slot."+i,"occ.first_experience.map_run.slot."+i})
                originalSaves[key]=PlayerPrefs.GetString(key,"<missing>");
            const string reminder="OCC.LootUnclaimedExitReminderShown";
            bool hadReminder=PlayerPrefs.HasKey(reminder); int oldReminder=PlayerPrefs.GetInt(reminder);
            saveScope=FirstExperienceSaveRouting.BeginVerificationScope("reward-branches-20261022");
            FirstExperienceSaveRouting.SelectSlot(0); Application.logMessageReceived+=RecordRuntimeError;
            yield return Guard(RewardRuntimeBranches());
            if(hadReminder)PlayerPrefs.SetInt(reminder,oldReminder);else PlayerPrefs.DeleteKey(reminder);
            PlayerPrefs.Save();
            File.WriteAllText(Path.Combine(Folder,"save_isolation.json"),JsonConvert.SerializeObject(new{
                playerSlotsUnchanged=originalSaves.All(p=>PlayerPrefs.GetString(p.Key,"<missing>")==p.Value),
                reminderRestored=PlayerPrefs.HasKey(reminder)==hadReminder&&PlayerPrefs.GetInt(reminder)==oldReminder,
                fixturePrepared=true,normalFullPlaythrough=false,actions},Formatting.Indented));
            folderOverride=null;
        }
        private static RogueliteMapRun RewardFixture(string node,int seed=4306)
        {
            var run=RogueliteMapRun.CreateSubsequentAcademyRun(seed);
            run.ChooseOpeningGift(run.OpeningGift.CandidateIds.First(id=>id!="LK-05"&&id!="LK-06"));
            run.ConfirmAcademyDeparture();
            var baselineStore=new PlayerPrefsRogueliteSaveStore();
            var baselineGateway=new RogueliteSaveGateway(baselineStore);
            if(!baselineGateway.SaveMapRun(run))throw new InvalidOperationException(baselineGateway.LastError);
            var dto=Rogue11Serializer.Deserialize(baselineStore.GetString(RogueliteSaveGateway.MapRunKey));
            dto.CurrentNodeId=node;dto.CompletedNodeIds.Remove(node);
            if(node=="core_finale")dto.StageTime=AcademyMapTuning.TransitionProgress;
            if(!dto.VisitedNodeIds.Contains(node))dto.VisitedNodeIds.Add(node);
            run=RogueliteMapRun.FromRogue11(dto); run.CompleteCurrentCombat();return run;
        }
        private static IEnumerator RewardLoad(RogueliteMapRun run)
        {
            var gateway=new RogueliteSaveGateway(new PlayerPrefsRogueliteSaveStore());
            if(!gateway.SaveMapRun(run))throw new InvalidOperationException(gateway.LastError);
            Host.GetComponent<FirstExperiencePrototypeController>().enabled=false;
            if(!Host.EnterFirstExperienceFromOpening(true))throw new InvalidOperationException("Formal continue entry failed");yield return new WaitForSecondsRealtime(.6f);
            if(!Host.IsMapRunSaved)throw new InvalidOperationException("Fixture did not load through production host");
        }
        private static IEnumerator RewardReload()
        {
            if(!Host.EnterFirstExperienceFromOpening(true))throw new InvalidOperationException("Formal continue entry failed");yield return new WaitForSecondsRealtime(.5f);
        }
        private static IEnumerator RewardClaimAllFixed()
        {
            for(int safety=0;safety<6;safety++)
            {
                var row=Buttons().FirstOrDefault(b=>b.name.StartsWith("固定战利品_"));
                if(row==null)yield break;
                yield return Click(row.name);
            }
            throw new InvalidOperationException("Fixed loot failed to clear");
        }
        private static IEnumerator RewardChoose(int index,bool dismantle=false)
        {
            if(!Host.CurrentMapRun.RewardChoicesOpened)yield return Click("战后奖励封套");
            yield return Click("揭晓卡牌_"+index);
            string id=Host.CurrentMapRun.SelectedRewardId;
            yield return Click(dismantle?"拆解_"+id:"战后奖励封套");
        }
        private static IEnumerator RewardFollowup()
        {
            if(Host.CurrentMapRun.PendingRewardStepId=="followup")yield return RewardChoose(0);
        }
        private static IEnumerator RewardRuntimeBranches()
        {
            yield return new WaitForSecondsRealtime(3);
            foreach(var gift in LampkeeperGiftCatalog.All)
            {
                RogueliteMapRun giftRun=null;
                for(int seed=6000;seed<6100;seed++)
                {
                    var candidate=RogueliteMapRun.CreateSubsequentAcademyRun(seed);
                    if(candidate.OpeningGift.CandidateIds.Contains(gift.Id)){giftRun=candidate;break;}
                }
                if(giftRun==null)throw new InvalidOperationException("Gift seed not found");
                int giftIndex=Array.IndexOf(giftRun.OpeningGift.CandidateIds,gift.Id);int beforeGiftGold=giftRun.Gold;
                yield return RewardLoad(giftRun);yield return Click("守灯人选择_"+giftIndex);
                if(gift.Id=="LK-05")yield return Click("守灯人选择_0");
                if(Host.CurrentMapRun.OpeningGiftPending||Host.CurrentMapRun.OpeningGift.SelectedId!=gift.Id)
                    throw new InvalidOperationException("Gift selection failed: "+gift.Id);
                int giftedGold=Host.CurrentMapRun.Gold;
                if(gift.Id=="LK-06"&&giftedGold!=beforeGiftGold+12)throw new InvalidOperationException("Gold gift amount mismatch");
                yield return RewardReload();
                if(Host.CurrentMapRun.OpeningGiftPending||!Host.CurrentMapRun.DeparturePending||Host.CurrentMapRun.Gold!=giftedGold)
                    throw new InvalidOperationException("Gift resumed incorrectly: "+gift.Id);
                Log("PASS opening gift "+gift.Id+" selection and disk resume");
            }
            // Both event receipt branches: resources already paid vs pending physical item.
            foreach(string content in new[]{"EV01","EV09"})
            {
                var initial=RogueliteMapRun.CreateSubsequentAcademyRun(4306);
                initial.ChooseOpeningGift(initial.OpeningGift.CandidateIds.First(id=>id!="LK-05"&&id!="LK-06"));
                initial.ConfirmAcademyDeparture();
                var eventStore=new PlayerPrefsRogueliteSaveStore();var eventGateway=new RogueliteSaveGateway(eventStore);
                if(!eventGateway.SaveMapRun(initial))throw new InvalidOperationException(eventGateway.LastError);
                var eventDto=Rogue11Serializer.Deserialize(eventStore.GetString(RogueliteSaveGateway.MapRunKey));
                eventDto.CurrentNodeId=RogueliteAcademyLayerCatalog.ContentMappings.First(m=>m.ContentTableId==content).NodeId;
                if(!eventDto.VisitedNodeIds.Contains(eventDto.CurrentNodeId))eventDto.VisitedNodeIds.Add(eventDto.CurrentNodeId);
                var eventRun=RogueliteMapRun.FromRogue11(eventDto);
                eventRun.ChooseCurrentNodeContent(content=="EV01"?"EV01_drill":"EV09_escort");
                eventRun.CompletePendingContentCombat();
                yield return RewardLoad(eventRun);
                int eventGold=Host.CurrentMapRun.Gold;
                if(content=="EV01")
                {
                    if(Host.CurrentMapRun.PendingResourceReceipt==null)throw new InvalidOperationException("Event resource receipt missing");
                    Snapshot("event_resource_receipt");yield return RewardReload();
                    yield return Click("离开战利品");
                    if(Host.CurrentMapRun.Gold!=eventGold)throw new InvalidOperationException("Receipt confirmation paid twice");
                }
                else
                {
                    string id=Host.CurrentMapRun.CurrentRewards.Single().Id;
                    var itemButton=Buttons().FirstOrDefault(b=>b.name=="reward.first");
                    if(itemButton==null)throw new InvalidOperationException("Event item button missing: "+string.Join(",",Buttons().Select(b=>b.name)));
                    yield return Click(itemButton.name);yield return Click("离开战利品");
                }
                if(Host.CurrentMapRun.AwaitingReward||Host.CurrentMapRun.LootPendingExit)throw new InvalidOperationException("Event reward exit blocked");
                Log("PASS event "+content);
            }
            var full=RewardFixture("core_finale");var fullInventory=RogueEquipmentRuntime.FromDto(full.RogueRunState);
            int filled=0;
            while(filled<100&&fullInventory.TryAddContainerItem("full-"+filled,AcademyBattleRewardCatalog.ForgeLoad))filled++;
            full.RogueRunState.ForgeMaterialCount+=filled;fullInventory.WriteToDto(full.RogueRunState);
            yield return RewardLoad(full);yield return Click("战后奖励封套");
            int fullChoice=Host.CurrentMapRun.CurrentRewards.Select((r,i)=>new{r,i}).First(p=>p.r.Kind==RogueliteRewardKind.Equipment).i;
            yield return Click("揭晓卡牌_"+fullChoice);string fullSelection=Host.CurrentMapRun.SelectedRewardId;
            yield return Click("战后奖励封套");
            if(Host.CurrentMapRun.PendingRewardStepId!="main"||Host.CurrentMapRun.SelectedRewardId!=fullSelection)
                throw new InvalidOperationException("Full inventory consumed reward");
            yield return Click("整理行囊");yield return Click("返回战利品");
            if(Host.CurrentMapRun.SelectedRewardId!=fullSelection)throw new InvalidOperationException("Full inventory return lost choice");
            Snapshot("full_backpack_return");yield return Click("离开战利品");
            if(Find("确认")!=null)yield return Click("确认");
            if(!Host.CurrentMapRun.IsComplete)throw new InvalidOperationException("Full backpack prevented leaving boss loot");
            Log("PASS full backpack refusal, F41 return and leave");
            // A selected card survives F41 and a genuine disk reload.
            yield return RewardLoad(RewardFixture("dorm_drill"));
            yield return Click("战后奖励封套");yield return Click("返回清点");yield return Click("战后奖励封套");
            yield return Click("揭晓卡牌_0");string selected=Host.CurrentMapRun.SelectedRewardId;
            yield return Click("整理行囊");yield return Click("返回战利品");
            if(Host.CurrentMapRun.SelectedRewardId!=selected)throw new InvalidOperationException("F41 lost selected reward");
            yield return RewardReload();
            if(Host.CurrentMapRun.SelectedRewardId!=selected)throw new InvalidOperationException("Reload lost selected reward");
            Snapshot("selected_after_inventory_and_reload");
            yield return Click("战后奖励封套");yield return RewardFollowup();yield return RewardClaimAllFixed();yield return Click("离开战利品");
            if(Host.CurrentMapRun.AwaitingReward||Host.CurrentMapRun.LootPendingExit)throw new InvalidOperationException("Normal rewards blocked exit");
            Log("PASS normal choice, reveal back, F41 return, selected reload, followup, fixed loot");

            // Inject one storage write failure into the actual production coordinator.
            yield return RewardLoad(RewardFixture("core_finale"));
            var field=typeof(CombatPrototypeBootstrap).GetField("mapSaves",BindingFlags.Instance|BindingFlags.NonPublic);
            var previous=field.GetValue(Host);var store=new RewardFailStore();
            field.SetValue(Host,new RogueliteMapSaveCoordinator(new RogueliteSaveGateway(store)));
            int gold=Host.CurrentMapRun.Gold; store.FailNext=true;
            yield return Click("固定战利品_0");
            if(Host.CurrentMapRun.Gold!=gold||!Host.IsMapRunSaved)throw new InvalidOperationException("Fixed reward rollback did not restore disk state");
            yield return Click("固定战利品_0");
            if(Host.CurrentMapRun.Gold!=gold+10)throw new InvalidOperationException("Fixed reward retry blocked or duplicated");
            yield return Click("战后奖励封套");yield return Click("揭晓卡牌_0");
            selected=Host.CurrentMapRun.SelectedRewardId;store.FailNext=true;
            yield return Click("战后奖励封套");
            if(Host.CurrentMapRun.SelectedRewardId!=selected||Host.CurrentMapRun.PendingRewardStepId!="main")throw new InvalidOperationException("Choice rollback lost selection");
            yield return Click("战后奖励封套");
            if(Host.CurrentMapRun.PendingRewardStepId!="collected")throw new InvalidOperationException("Choice retry blocked");
            field.SetValue(Host,previous);yield return RewardClaimAllFixed();yield return Click("离开战利品");
            Log("PASS production save failures and retries, both fixed resource and selected choice");

            // Elite main reward -> fixed material -> extra drop, including dismantle.
            yield return RewardLoad(RewardFixture("supply_depot"));
            int equipmentIndex=Host.CurrentMapRun.CurrentRewards.Select((r,i)=>new{r,i}).First(p=>p.r.Kind==RogueliteRewardKind.Equipment).i;
            int materialBefore=Host.CurrentMapRun.ForgeMaterialCount;
            yield return RewardChoose(equipmentIndex,true);
            if(Host.CurrentMapRun.ForgeMaterialCount!=materialBefore+1)throw new InvalidOperationException("Dismantling failed");
            if(Host.CurrentMapRun.PendingRewardStepId!="followup")throw new InvalidOperationException("Elite extra drop missing");
            yield return RewardReload();yield return RewardFollowup();yield return RewardClaimAllFixed();
            Snapshot("elite_all_claimed");yield return Click("离开战利品");Log("PASS elite dismantle, saved followup, fixed materials");

            // Partial collection: cancel warning, then confirm loss of only unclaimed loot.
            yield return RewardLoad(RewardFixture("core_finale"));
            gold=Host.CurrentMapRun.Gold;yield return Click("固定战利品_0");
            PlayerPrefs.DeleteKey("OCC.LootUnclaimedExitReminderShown");
            yield return Click("离开战利品");yield return Click("取消");
            if(!Host.CurrentMapRun.AwaitingReward)throw new InvalidOperationException("Cancel left loot");
            Snapshot("partial_leave_cancelled");yield return Click("离开战利品");yield return Click("确认");
            if(!Host.CurrentMapRun.IsComplete||Host.CurrentMapRun.Gold!=gold+10)throw new InvalidOperationException("Partial leave lost paid gold or blocked summary");
            Snapshot("boss_summary");yield return Click("返回主界面");yield return new WaitForSecondsRealtime(.8f);
            yield return Click("开始游戏");yield return Click("以此角色开始");yield return new WaitForSecondsRealtime(.8f);
            if(Host.CurrentMapRun.IsComplete||!Host.CurrentMapRun.OpeningGiftPending||Host.CurrentMapRun.IsTutorialPhase)
                throw new InvalidOperationException("Summary continue did not create fresh normal round");
            Snapshot("next_round_gift");yield return Click("守灯人选择_0");
            if(Host.CurrentMapRun.OpeningGiftPending)yield return Click("守灯人选择_0");
            yield return Click("确认出发");yield return RewardReload();
            if(Host.CurrentMapRun.OpeningGiftPending||Host.CurrentMapRun.DeparturePending||Host.CurrentMapRun.IsComplete)
                throw new InvalidOperationException("Next round departure or resume failed");
            Snapshot("next_round_map");
            File.WriteAllText(Path.Combine(Folder,"completed.json"),JsonConvert.SerializeObject(new{
                fixturePrepared=true,normalFullPlaythrough=false,actualRaycastClicks=File.ReadAllLines(Path.Combine(Folder,"journey.txt")).Count(line=>line.Contains(" CLICK ")),
                branchChecks=new[]{"all-six-opening-gifts-resume","event-resource-receipt","event-item-reward","full-backpack-F41-exit","three-choice","reveal-back-reopen","F41-return","selected-disk-resume","fixed-save-retry","choice-save-retry","equipment-dismantle","elite-followup-resume","fixed-material","partial-leave-cancel-confirm","boss-summary","next-normal-round-gift-departure-resume"},
                nextSeed=Host.CurrentMapRun.Seed,nextCompletedNodes=Host.CurrentMapRun.CompletedNodes,actualScreen=new[]{Screen.width,Screen.height}},Formatting.Indented));
        }
        public static IEnumerator RunRewardRenderProbeInTest()
        {
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-22_第一阶段奖励界面分支验收/render_probe"));
            Directory.CreateDirectory(folder);
            using(FirstExperienceSaveRouting.BeginVerificationScope("reward-render-probe-20261022"))
            {
                FirstExperienceSaveRouting.SelectSlot(0);yield return new WaitForSecondsRealtime(2);
                var run=RewardFixture("core_finale");var gateway=new RogueliteSaveGateway(new PlayerPrefsRogueliteSaveStore());
                if(!gateway.SaveMapRun(run)||!Host.EnterFirstExperienceFromOpening(true))throw new InvalidOperationException("Render probe could not load reward");
                Host.GetComponent<FirstExperiencePrototypeController>().enabled=false;
                yield return new WaitForSecondsRealtime(3);
                var renderers=Object.FindObjectsByType<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
                var materials=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).GroupBy(m=>m.shader.name+"|"+string.Join(",",m.shaderKeywords)).Select(g=>g.First()).ToArray();
                File.WriteAllText(Path.Combine(folder,"materials.json"),JsonConvert.SerializeObject(materials.Select(m=>new{
                    m.name,shader=m.shader.name,supported=m.shader.isSupported,keywords=m.shaderKeywords,passes=m.passCount,
                    errors=UnityEditor.ShaderUtil.GetShaderMessages(m.shader).Select(e=>new{e.message,e.file,e.line,severity=e.severity.ToString()})}),Formatting.Indented));
                var stages=Object.FindObjectsByType<MainMenuHd2DStage>().Where(st=>st.isActiveAndEnabled&&st.Output!=null).ToArray();
                foreach(var stage in stages)
                {
                    stage.sceneCamera.Render();var before=RenderTexture.active;RenderTexture.active=stage.Output;
                    var tex=new Texture2D(stage.Output.width,stage.Output.height,TextureFormat.RGB24,false);
                    tex.ReadPixels(new Rect(0,0,tex.width,tex.height),0,0);tex.Apply();
                    File.WriteAllBytes(Path.Combine(folder,"stage_"+stage.gameObject.layer+".png"),tex.EncodeToPNG());
                    float pink=tex.GetPixels32().Count(c=>c.r>230&&c.g<25&&c.b>230)/(float)(tex.width*tex.height);
                    File.AppendAllText(Path.Combine(folder,"magenta_check.txt"),stage.gameObject.layer+" "+pink+"\n");
                    if(pink>.05f)throw new InvalidOperationException("HD2D error magenta covers "+pink+" of live stage");
                    RenderTexture.active=before;Object.Destroy(tex);
                }
                File.WriteAllText(Path.Combine(folder,"pipeline.json"),JsonConvert.SerializeObject(new{
                    quality=QualitySettings.names[QualitySettings.GetQualityLevel()],
                    defaultPipeline=UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline?.name,
                    activePipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?.name},Formatting.Indented));
                Hd2DBattleCanvasCapture.Capture(null,1920,1080,Path.Combine(folder,"reward_1920.png"));
                Hd2DBattleCanvasCapture.Capture(null,960,540,Path.Combine(folder,"reward_960.png"));
                File.WriteAllText(Path.Combine(folder,"shader_after_render.json"),JsonConvert.SerializeObject(materials.Select(m=>new{
                    shader=m.shader.name,supported=m.shader.isSupported,errors=UnityEditor.ShaderUtil.GetShaderMessages(m.shader).Select(e=>new{e.message,e.file,e.line,severity=e.severity.ToString()})}),Formatting.Indented));
                var battleRun=RogueliteMapRun.CreateSubsequentAcademyRun(4311);
                battleRun.ChooseOpeningGift(battleRun.OpeningGift.CandidateIds.First(id=>id!="LK-05"&&id!="LK-06"));
                battleRun.ConfirmAcademyDeparture();battleRun.SelectNode("dorm_drill");battleRun.BeginCombatJournal();
                var battleGateway=new RogueliteSaveGateway(new PlayerPrefsRogueliteSaveStore());
                if(!battleGateway.SaveMapRun(battleRun))throw new InvalidOperationException("Battle render fixture save failed: "+battleGateway.LastError);
                if(!Host.EnterFirstExperienceFromOpening(true))throw new InvalidOperationException("Battle render host could not resume");
                yield return new WaitForSecondsRealtime(3);
                var battlefield=Object.FindAnyObjectByType<Hd2DBattlefieldSample>();
                if(battlefield==null)throw new InvalidOperationException("Battlefield sample missing");
                battlefield.RenderCamera.Render();
                Hd2DBattleCanvasCapture.Capture(battlefield,1920,1080,Path.Combine(folder,"battle_1920.png"));
                Hd2DBattleCanvasCapture.Capture(battlefield,960,540,Path.Combine(folder,"battle_960.png"));

            }
        }

        private sealed class RewardFailStore:IRogueliteSaveStore
        {
            private readonly PlayerPrefsRogueliteSaveStore inner=new PlayerPrefsRogueliteSaveStore();
            public bool FailNext;
            public bool HasKey(string key)=>inner.HasKey(key);
            public string GetString(string key,string fallback="")=>inner.GetString(key,fallback);
            public void SetString(string key,string value){if(FailNext){FailNext=false;throw new IOException("Intentional isolated reward save failure");}inner.SetString(key,value);}
            public void DeleteKey(string key)=>inner.DeleteKey(key);
            public void Flush()=>inner.Flush();
        }
    }
}
