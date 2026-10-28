using System;
using System.Collections.Generic;
using System.Linq;
using OCC.Combat.Roguelite;

namespace OCC.Combat
{
    public sealed partial class AcademyTutorialModelExercise
    {
        private sealed class PracticeStore : IRogueliteSaveStore
        {
            private readonly Dictionary<string,string> values = new Dictionary<string,string>();
            public bool FailNextFlush;
            public bool HasKey(string key) => values.ContainsKey(key);
            public string GetString(string key, string fallback = "") => values.TryGetValue(key, out string value) ? value : fallback;
            public void SetString(string key, string value) => values[key] = value;
            public void DeleteKey(string key) => values.Remove(key);
            public void Flush() { if (!FailNextFlush) return; FailNextFlush = false; throw new InvalidOperationException("教学演练中的写入失败"); }
        }
        private static RogueliteMapRun FixedReward()
        {
            var run = RogueliteMapRun.CreateFirstRunV1(7021); run.AcknowledgeFirstRunOrigin(); run.SelectNode("B1"); run.CompleteCurrentCombat(); return run;
        }
        private static RogueliteMapRun EliteReward()
        {
            var run = FixedReward(); run.ClaimReward("F-P-R19");
            run.SelectNode("EV1"); run.ChooseCurrentNodeContent("FIRST-EV1-ACCEPT-DELIVERY");
            run.SelectNode("EV2"); run.ChooseCurrentNodeContent("FIRST-EV2-GOLD");
            run.SelectNode("B2"); run.CompleteCurrentCombat(); run.ClaimReward("F-P-U04");
            run.SelectNode("EV3"); run.ChooseCurrentNodeContent("FIRST-EV3-CONTRIBUTION");
            run.SelectNode("B3"); run.CompleteCurrentCombat(); run.ClaimReward("F-P-M12");
            run.SelectNode("X"); run.CompleteCurrentCombat(); return run;
        }
        private static RogueliteMapRun ReadyForRandomLayer()
        { var run = EliteReward(); run.ClaimReward("PASSIVE-ELITE-01"); run.SelectNode("S"); return run; }
        private static Model SafeSave()
        {
            var store = new PracticeStore(); var gateway = new RogueliteSaveGateway(store); var run = RogueliteMapRun.CreateSubsequentAcademyRun(4312);
            bool readBack = false, preserved = false; string status = "尚未保存";
            return new Model
            {
                Prompt = "这里演练一次保存成功和一次保存失败。两种结果都不会写入真实档案。",
                Read = () => status + "\n已保存记录 " + (store.HasKey(RogueliteSaveGateway.MapRunKey) ? "存在" : "无"),
                Steps = new Action[]
                {
                    () => { bool saved = gateway.SaveNewMapRun(run); readBack = saved && gateway.TryLoadMapRun(out var loaded) && loaded.Seed == run.Seed; status = readBack ? "保存成功，读回一致" : "保存未通过"; },
                    () => { string before = store.GetString(RogueliteSaveGateway.MapRunKey); run.RogueRunState.Gold++; store.FailNextFlush = true; bool saved = gateway.SaveMapRun(run); preserved = !saved && store.GetString(RogueliteSaveGateway.MapRunKey) == before; status = preserved ? "保存失败，原进度仍保留" : "原记录未能验证"; }
                },
                Labels = new[] { "保存演练进度", "演练保存失败与保护" }, Verify = () => readBack && preserved,
                Fingerprint = () => status + "|" + store.GetString(RogueliteSaveGateway.MapRunKey), Proofs = new[] { "save-readback", "failed-save-preserved" }
            };
        }
        private static Model PauseAndAbandon()
        {
            var run = Service("dorm_drill"); var store = new PracticeStore(); var gateway = new RogueliteSaveGateway(store); var navigation = new UiNavigationState(UiScreen.Combat, "");
            string before = Save(run); bool resumed = false, previewed = false;
            return new Model
            {
                Prompt = "返回首页是保存中断，主动放弃是结束本轮。此处只预览放弃后果。",
                Read = () => "当前地点 " + run.MapNode(run.CurrentNodeId).DisplayName + "\n本轮 " + (run.IsComplete ? "已结束" : "未结束") + "\n原状态恢复 " + (resumed ? "一致" : "待核对"),
                Steps = new Action[]
                {
                    () => { gateway.SaveNewMapRun(run); gateway.TryLoadMapRun(out var loaded); run = loaded; resumed = Save(run) == before; },
                    () => { previewed = navigation.ResolveBack() == UiBackAction.RequestLeaveCombat; }
                },
                Labels = new[] { "保存并继续演练轮次", "只查看主动放弃的后果" },
                Verify = () => resumed && previewed && !run.IsComplete, Fingerprint = () => Save(run), Proofs = new[] { "resume-unchanged", "abandon-preview" }
            };
        }
        private static Model GiftChoice()
        {
            var run = RogueliteMapRun.CreateSubsequentAcademyRun(4312); string chosen = run.OpeningGift.CandidateIds.First(id => id != "LK-05");
            return new Model
            {
                Prompt = "开局赠礼只选一份，本轮有效。演练使用示例候选，不推荐选择答案。", ActionLabel = "领取一份演练赠礼",
                Read = () => "外层候选 " + run.OpeningGift.CandidateIds.Length + " 份\n已选 " + (string.IsNullOrEmpty(run.OpeningGift.SelectedId) ? "无" : LampkeeperGiftCatalog.Get(run.OpeningGift.SelectedId).Name) + "\n本轮有效",
                Apply = () => run.ChooseOpeningGift(chosen), Verify = () => run.OpeningGift.Resolved && Restore(run).OpeningGift.SelectedId == chosen,
                Fingerprint = () => run.OpeningGift.Serialize(), Proofs = new[] { "gift-chosen", "gift-source" }
            };
        }
        private static Model MapTravel()
        {
            var run = RogueliteMapRun.CreateSubsequentAcademyRun(4312, false); run.ConfirmAcademyDeparture(); bool previewed = false;
            string before = run.CurrentNodeId; const string target = "dorm_drill";
            return new Model
            {
                Prompt = "选中地点只查看摘要；确认出发才改变当前位置。", Read = () => "当前位置 " + run.MapNode(run.CurrentNodeId).DisplayName + "\n示例目的地 " + run.MapNode(target).DisplayName + "\n可达 " + (run.IsNodeAvailable(target) ? "是" : "已进入或当前不可达"),
                Steps = new Action[] { () => previewed = run.CurrentNodeId == before && run.IsNodeAvailable(target), () => run.SelectNode(target) },
                Labels = new[] { "查看示例节点摘要", "确认进入演练地点" }, Verify = () => previewed && run.CurrentNodeId == target,
                Fingerprint = () => run.CurrentNodeId, Proofs = new[] { "node-preview", "node-entered" }
            };
        }
        private static RogueliteMapRun EventFixture()
        {
            var source = Service("academy_gate"); var dto = source.ExportRogue11();
            dto.NodeContentAssignments.RemoveAll(value => value.StartsWith("academy_gate="));
            dto.NodeContentAssignments.Add("academy_gate=EV01");
            return RogueliteMapRun.FromRogue11(dto);
        }
        private static Model EventCost()
        {
            var run = EventFixture(); var choice = run.CurrentContentChoices.First(c => !c.RequiresCombat); int contribution = run.StageContribution; int time = run.StageTime;
            bool cancelled = false, committed = false;
            var navigation = new UiNavigationState(UiScreen.Map, "演练节点"); navigation.OpenOverlay(UiOverlay.NodeRoom, "演练节点"); string original = Save(run);
            return new Model
            {
                Prompt = "事件在确认前公开费用和结果。返回不会扣费；确认后才结算。",
                Read = () => choice.DisplayName + "\n" + choice.Preview + "\n当前贡献 " + run.StageContribution + "　时序 " + run.StageTime,
                Steps = new Action[] { () => { navigation.CloseOverlay(); cancelled = navigation.Overlay == UiOverlay.None && Save(run) == original; }, () => { run.ChooseCurrentNodeContent(choice.Id); committed = run.CompletedNodes.Contains(run.CurrentNodeId); } },
                Labels = new[] { "查看费用后返回", "确认演练事件" },
                Verify = () => cancelled && committed && run.StageContribution == contribution - choice.ContributionCost + choice.ContributionGain && run.StageTime == time + AcademyMapTuning.TimeCost(run.MapNode(run.CurrentNodeId)),
                Fingerprint = () => Save(run), Proofs = new[] { "event-preview", "event-cancel", "event-commit", "cost-before", "cost-after" }
            };
        }
        private static Model EventRevisit()
        {
            var run = EventFixture(); var choice = run.CurrentContentChoices.First(c => !c.RequiresCombat); bool noDuplicate = false;
            return new Model
            {
                Prompt = "领取后再查看同一事件，不会重复发放。所得物有唯一去向。",
                Read = () => "事件 " + (run.CompletedNodes.Contains(run.CurrentNodeId) ? "已完成" : "未完成") + "\n战术物品 " + run.RogueRunState.TacticalItemInstances.Count + "\n贡献 " + run.StageContribution,
                Steps = new Action[] { () => run.ChooseCurrentNodeContent(choice.Id), () => { Save(run); var reward = run.CurrentRewards.First(); run.SelectPendingReward(reward.Id); run.ClaimLootChoice(reward.Id); }, () => { string before = Save(run); try { run.ChooseCurrentNodeContent(choice.Id); } catch (InvalidOperationException) { } noDuplicate = Save(run) == before; } },
                Labels = new[] { "确认演练事件内容", "从战果清单实际领取", "再次查看已完成的事件" }, Verify = () => noDuplicate && run.RogueRunState.TacticalItemInstances.Count(i => i.DefinitionId == choice.RewardId) == 1,
                Fingerprint = () => Save(run), Proofs = new[] { "event-claimed", "revisit-no-duplicate" }
            };
        }
        private static Model TermClock()
        {
            var run = Service("academy_gate"); bool readClock = false, blocked = false;
            return new Model
            {
                Prompt = "学院时序按地点行动结算。达到公开门槛时进入首领阶段，不按现实时间倒数。",
                Read = () => "学院时序 " + run.StageTime + "\n强制转换门槛 " + AcademyMapTuning.TransitionProgress + "\n首领资格 " + (run.CanChallengeAcademyFinale ? "可挑战" : "尚未满足"),
                Steps = new Action[] { () => readClock = run.StageTime == 0 && AcademyMapTuning.TimeCost(RogueliteMapNodeType.Event) > 0, () => { var dto = run.ExportRogue11(); dto.StageTime = AcademyMapTuning.TransitionProgress; run = RogueliteMapRun.FromRogue11(dto); blocked = run.IsTransitionPending && run.CanChallengeAcademyFinale; } },
                Labels = new[] { "查看节点成本与时序", "查看门槛时的阶段变化" }, Verify = () => readClock && blocked,
                Fingerprint = () => run.StageTime + "|" + run.IsTransitionPending, Proofs = new[] { "time-preview", "boss-gate" }
            };
        }
        private static Model FinaleGate()
        {
            var run = Service("academy_gate"); var dto = run.ExportRogue11(); dto.StageTime = AcademyMapTuning.TransitionProgress;
            dto.CompletedNodeIds.Add(run.CurrentNodeId); run = RogueliteMapRun.FromRogue11(dto); bool qualified = false;
            return new Model
            {
                Prompt = "首领入口公开资格和不可逆变化。确认后普通路线关闭。",
                Read = () => "首领资格 " + (run.CanChallengeAcademyFinale ? "可挑战" : "未满足") + "\n当前位置 " + run.MapNode(run.CurrentNodeId).DisplayName + "\n普通路线 " + (run.CurrentNodeId == RogueliteAcademyLayerCatalog.FinaleNodeId ? "已关闭" : "进入首领后关闭"),
                Steps = new Action[] { () => qualified = run.IsNodeAvailable(RogueliteAcademyLayerCatalog.FinaleNodeId), () => run.SelectNode(RogueliteAcademyLayerCatalog.FinaleNodeId) },
                Labels = new[] { "查看资格与阶段后果", "确认进入演练首领阶段" }, Verify = () => qualified && run.CurrentNodeId == RogueliteAcademyLayerCatalog.FinaleNodeId && !run.IsNodeAvailable("dorm_drill"),
                Fingerprint = () => run.CurrentNodeId, Proofs = new[] { "gate-preview", "transition-confirm" }
            };
        }
        private static Model RewardReadOnly()
        {
            var run = FixedReward(); int gold = run.Gold; bool unchanged = false;
            return new Model
            {
                Prompt = "战果只读，不等于已领取。清单中的固定所得与候选分别处理。",
                Read = () => "本场战斗已完成\n金币 " + run.Gold + "\n待领金币 " + run.RogueRunState.PendingRewardGold + "\n候选 " + run.CurrentRewards.Count + " 项",
                Apply = () => { run.OpenPendingRewardChoices(); unchanged = run.Gold == gold; }, ActionLabel = "从战果查看领取清单",
                Verify = () => unchanged && run.RewardChoicesOpened && run.AwaitingReward,
                Fingerprint = () => Save(run), Proofs = new[] { "settlement-view", "reward-view" }
            };
        }
        private static Model RewardSelection()
        {
            var run = FixedReward(); string choice = run.CurrentRewards.First().Id; int mastered = run.RogueRunState.MasteredSpellIds.Count; bool selectedOnly = false;
            return new Model
            {
                Prompt = "选择先固定候选，实际领取才写入所得。",
                Read = () => "候选 " + (string.IsNullOrEmpty(run.SelectedRewardId) ? "尚未选择" : "已选定，待领取") + "\n掌握术式 " + run.RogueRunState.MasteredSpellIds.Count + "\n选择和领取分开保存",
                Steps = new Action[] { () => { run.OpenPendingRewardChoices(); run.SelectPendingReward(choice); selectedOnly = run.RogueRunState.MasteredSpellIds.Count == mastered && Restore(run).SelectedRewardId == choice; }, () => run.ClaimLootChoice(choice) },
                Labels = new[] { "选定一个演练候选", "实际领取这一条目" }, Verify = () => selectedOnly && run.RogueRunState.MasteredSpellIds.Contains(choice) && run.RogueRunState.MasteredSpellIds.Count == mastered + 1,
                Fingerprint = () => Save(run), Proofs = new[] { "candidate-select", "reward-claim" }
            };
        }
        private static Model FailureBoundaries()
        {
            var fixedRun = RogueliteMapRun.CreateFirstRunV1(7021); fixedRun.AcknowledgeFirstRunOrigin(); fixedRun.SelectNode("B1"); fixedRun.BeginCombatJournal();
            var normal = Service("dorm_drill"); normal.BeginCombatJournal();
            var ended = Service("dorm_drill"); var abandoned = Service("dorm_drill");
            var combat = StatusFixture(); var combatFlow = new CombatFlowController();
            combatFlow.Configure(new MissionPreparation().Configure("retry-practice", "失败与重试演练", "演练靶"), combat);
            combatFlow.OpenBriefing(); combatFlow.BeginCombat();
            int entryHealth = combat.GetUnit("hero").Health;
            combat.GetUnit("hero").TakeDamage(int.MaxValue); combat.EvaluateOutcome(); combatFlow.RefreshOutcome();
            bool retry = false, normalRetry = false, paused = false;
            return new Model
            {
                Prompt = "固定教学段和常规战败都可恢复本场战前状态。明确放弃或在失败页确认结束本轮才会关闭本轮；暂停只保存中断。",
                ReadCombat = () => combatFlow.State,
                Read = () => "固定教学段 " + (retry ? "战前状态已恢复" : "可重试") + "\n常规战败 " + (normalRetry ? "已重试，生命恢复为 " + combatFlow.State.GetUnit("hero").Health : "可重试") + "\n结束本轮 " + (ended.IsComplete ? "已保存战败结果" : "未确认") + "\n常规放弃 " + (abandoned.IsComplete ? "本轮已结束" : "未确认"),
                Steps = new Action[]
                {
                    () => { fixedRun.RestartCombatJournalPreservingBalance(); retry = fixedRun.HasActiveCombat; },
                    () => { normal.RestartCombatJournalPreservingBalance(); combatFlow.TacticalRestart(); combatFlow.ResumeAfterRestart(); normalRetry = !normal.IsComplete && normal.HasActiveCombat && !combatFlow.State.IsDefeat && combatFlow.State.GetUnit("hero").Health == entryHealth; },
                    () => ended.CloseAsFailure(false),
                    () => abandoned.CloseAsFailure(true),
                    () => { string before = Save(normal); paused = Save(Restore(normal)) == before; }
                },
                Labels = new[] { "重试固定教学段的演练快照", "重试常规战败的演练快照", "确认结束已战败的演练轮次", "查看明确放弃后的结算", "暂停后继续常规演练" },
                Verify = () => retry && normalRetry && !normal.IsComplete && ended.RunEndReason == "death" && abandoned.RunEndReason == "abandon" && paused,
                Fingerprint = () => normalRetry + "|" + combatFlow.State.GetUnit("hero").Health + "|" + ended.RunEndReason + "|" + abandoned.RunEndReason + "|" + fixedRun.HasActiveCombat,
                Proofs = new[] { "fixed-retry", "normal-retry", "normal-defeat", "normal-abandon", "pause-resume" }
            };
        }
        private static Model SameRoundHandoff()
        {
            var run = ReadyForRandomLayer(); string id = run.RogueRunState.RunId; int gold = run.Gold; string[] mastered = run.RogueRunState.MasteredSpellIds.ToArray();
            return new Model
            {
                Prompt = "固定段结束后仍是同一轮，资源、术式和物品剩余次数继承。", ActionLabel = "从演练固定段继续随机地图",
                Read = () => "当前区域 " + (run.IsInAcademyLayer ? "学院随机地图" : "固定前段") + "\n金币 " + run.Gold + "\n已掌握术式 " + run.RogueRunState.MasteredSpellIds.Count,
                Apply = run.CompleteFirstRunExperience, Verify = () => run.IsInAcademyLayer && run.RogueRunState.RunId == id && run.Gold == gold && mastered.All(run.RogueRunState.MasteredSpellIds.Contains),
                Fingerprint = () => Save(run), Proofs = new[] { "same-run", "inherited", "random-layer" }
            };
        }
        private static Model NewRound()
        {
            var store = new PracticeStore(); var gateway = new RogueliteSaveGateway(store); var run = Service("dorm_drill"); run.CloseAsFailure(true); gateway.SaveNewMapRun(run);
            store.SetString("teaching.long-term", "档案与已学记录"); string previous = run.RogueRunState.RunId; bool created = false;
            return new Model
            {
                Prompt = "新一轮重置本轮资源和赠礼。档案与已学记录独立保存。只有确认后才创建。", ActionLabel = "确认创建演练新一轮",
                Read = () => "本轮金币 " + run.Gold + "\n本轮状态 " + (run.IsComplete ? "已结束" : "新轮进行中") + "\n档案与已学记录 " + (store.GetString("teaching.long-term") == "档案与已学记录" ? "保留" : "未保留"),
                Apply = () => { var result = new RogueliteMapSaveCoordinator(gateway).TryStartSubsequentAcademyRound(4313); if (result.Success) { run = result.Run; created = true; } },
                Verify = () => created && run.RogueRunState.RunId != previous && run.Gold == 8 && run.OpeningGiftPending && store.GetString("teaching.long-term") == "档案与已学记录",
                Fingerprint = () => Save(run), Proofs = new[] { "reset-preview", "preserve-preview", "new-round-choice" }
            };
        }
        private static Model ProtectUnfinishedRound()
        {
            var run = Service("dorm_drill"); var store = new PracticeStore(); var gateway = new RogueliteSaveGateway(store); gateway.SaveNewMapRun(run); string before = store.GetString(RogueliteSaveGateway.MapRunKey); bool blocked = false;
            var confirmation = new UiConfirmationSession(); bool cancelled = false;
            return new Model
            {
                Prompt = "还有未完成轮次时，先核对覆盖对象。取消或未确认不能删除原档。",
                Read = () => "原轮次 " + (run.IsComplete ? "已结束" : "未完成") + "\n原档案 " + (store.GetString(RogueliteSaveGateway.MapRunKey) == before ? "保留" : "已改变"),
                Steps = new Action[]
                {
                    () => { var result = new RogueliteMapSaveCoordinator(gateway).TryStartSubsequentAcademyRound(4313); blocked = !result.Success && store.GetString(RogueliteSaveGateway.MapRunKey) == before; confirmation.Open(new UiConfirmationRequest(UiConfirmationKind.ReplaceExistingRun, "覆盖演练轮次？", "当前轮次未完成。确认覆盖会失去本轮进度。", "确认覆盖"), () => gateway.SaveNewMapRun(RogueliteMapRun.CreateSubsequentAcademyRun(4313))); },
                    () => { confirmation.Cancel(); cancelled = store.GetString(RogueliteSaveGateway.MapRunKey) == before; }
                },
                Labels = new[] { "查看已有轮次的提示", "取消，保留原档案" },
                Verify = () => blocked && cancelled, Fingerprint = () => store.GetString(RogueliteSaveGateway.MapRunKey) + "|" + confirmation.IsOpen, Proofs = new[] { "unfinished-warning", "cancel-preserved" }
            };
        }
    }
}
