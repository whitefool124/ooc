using System;
using System.Linq;
using OCC.Combat.Roguelite;

namespace OCC.Combat
{
    public sealed partial class AcademyTutorialModelExercise
    {
        private static Model ConsequenceConfirmation()
        {
            var run = Service("dorm_drill"); string before = Save(run); var confirmation = new UiConfirmationSession(); bool cancelled = false;
            return new Model
            {
                Prompt = "确认框会说明对象和后果。这里先查看，再取消，原档案保持不变。",
                Read = () => confirmation.IsOpen ? confirmation.Request.Title + "\n" + confirmation.Request.Message : "确认框已关闭\n演练档案仍保留",
                Steps = new Action[] { () => confirmation.Open(new UiConfirmationRequest(UiConfirmationKind.ReplaceExistingRun, "替换这份演练档案？", "确认后会失去此演练轮次。取消会保留原状态。", "确认替换"), () => run.RogueRunState.Gold = 0), () => { confirmation.Cancel(); cancelled = Save(run) == before; } },
                Labels = new[] { "打开后果确认", "取消，保留原轮次" }, Verify = () => cancelled && !confirmation.IsOpen,
                Fingerprint = () => Save(run) + "|" + confirmation.IsOpen, Proofs = new[] { "consequence-preview", "cancel-unchanged" }
            };
        }
        private static Model RewardVariant(string lesson, string kind)
        {
            if (lesson == "R05") return Dismantle(kind);
            var run = kind == "equipment" || kind == "artifact" || kind == "material" ? EliteReward() : FixedReward();
            int gold = run.Gold, contribution = run.StageContribution, equipment = run.RogueRunState.EquipmentInstances.Count, artifacts = run.RogueRunState.TacticalItemInstances.Count;
            string spell = run.CurrentRewards.First().Id;
            string material = run.RogueRunState.PendingFixedMaterialIds.FirstOrDefault() ?? AcademyBattleRewardCatalog.ForgeLoad;
            int materialBefore = run.AcademyMaterialCount(material); bool received = false;
            Func<string> read = () => "金币 " + run.Gold + "\n学院贡献 " + run.StageContribution + "\n装备实例 " + run.RogueRunState.EquipmentInstances.Count + "\n战术物品实例 " + run.RogueRunState.TacticalItemInstances.Count + "\n已掌握术式 " + run.RogueRunState.MasteredSpellIds.Count + "\n本次材料 " + run.AcademyMaterialCount(material);
            return new Model
            {
                Prompt = "这次只核对一种所得的去向。领取后按实际记录增加，不重复发放。", ActionLabel = "领取并查找这一项所得",
                Read = read,
                Apply = () =>
                {
                    switch (kind)
                    {
                        case "gold": run.ClaimFixedLoot("gold"); received = run.Gold > gold; break;
                        case "contribution": run.ClaimFixedLoot("contribution"); received = run.StageContribution > contribution; break;
                        case "equipment": run.ClaimFixedLoot("elite-head"); received = run.RogueRunState.EquipmentInstances.Count == equipment + 1; break;
                        case "artifact": run.ClaimFixedLoot("elite-brace"); received = run.RogueRunState.TacticalItemInstances.Count == artifacts + 1 && run.RogueRunState.TacticalItemInstances.Last().ChargesCurrent == 3; break;
                        case "spell": run.OpenPendingRewardChoices(); run.SelectPendingReward(spell); run.ClaimLootChoice(spell); received = run.RogueRunState.MasteredSpellIds.Contains(spell); break;
                        case "material": run.ClaimFixedLoot("elite-material"); received = run.AcademyMaterialCount(material) == materialBefore + 1; break;
                    }
                },
                Verify = () => received && Save(Restore(run)) == Save(run), Fingerprint = () => Save(run), Proofs = new[] { kind }
            };
        }
        private static Model Dismantle(string kind)
        {
            var run = Service("dorm_drill"); run.CompleteCurrentCombat();
            var reward = run.CurrentRewards.First(r => kind == "forge-material" ? r.Equipment != null : r.RogueSpell != null);
            if (reward.RogueSpell != null && !run.RogueRunState.MasteredSpellIds.Contains(reward.Id)) run.RogueRunState.MasteredSpellIds.Add(reward.Id);
            string material = kind == "forge-material" ? AcademyBattleRewardCatalog.ForgeLoad : AcademyBattleRewardCatalog.SpecAmplify;
            int initial = run.AcademyMaterialCount(material); bool selected = false;
            return new Model
            {
                Prompt = "拆解会失去该项待领内容，换成公开的材料。先预览，再单独确认。",
                Read = () => reward.DisplayName + "\n本次去向 " + (kind == "forge-material" ? "锻造材料" : "专精材料") + "\n该材料现有 " + run.AcademyMaterialCount(material),
                Steps = new Action[] { () => { run.OpenPendingRewardChoices(); run.SelectPendingReward("dismantle:" + reward.Id); selected = run.AcademyMaterialCount(material) == initial; }, () => run.ClaimLootChoice("dismantle:" + reward.Id) },
                Labels = new[] { "查看拆解后的唯一去向", "确认拆解这一演练条目" },
                Verify = () => selected && run.AcademyMaterialCount(material) == initial + 1,
                Fingerprint = () => Save(run), Proofs = new[] { "dismantle-preview", "dismantle-commit", kind }
            };
        }
        private static Model RewardOverflow()
        {
            var run = Service("dorm_drill"); run.CompleteCurrentCombat(); var reward = run.CurrentRewards.First(r => r.Equipment != null);
            var items = RogueEquipmentRuntime.FromDto(run.RogueRunState);
            for (int i = 0; i < items.BackpackColumns * items.BackpackRows + 1; i++)
                if (!items.TryAddEquipmentFromLoot("teaching-fill-" + i, "ACA-EQ-AC01", "teaching")) break;
            items.WriteToDto(run.RogueRunState);
            string[] candidates = run.CurrentRewards.Select(r => r.Id).ToArray(); bool preserved = false, cancelled = false, reloaded = false;
            var confirmation = new UiConfirmationSession();
            return new Model
            {
                Prompt = "放不下时不吞物。离开前会提示未领取项；取消后可继续整理，读档不重抽。",
                Read = () => "待领取候选 " + run.CurrentRewards.Count + " 项\n背包已放满\n离开确认 " + (confirmation.IsOpen ? "待决定" : "已取消或尚未打开"),
                Steps = new Action[]
                {
                    () => { string before = Save(run); try { run.ClaimLootChoice(reward.Id); } catch (InvalidOperationException) { } preserved = Save(run) == before; },
                    () => confirmation.Open(new UiConfirmationRequest(UiConfirmationKind.AbandonReward, "离开演练领取清单？", "离开会放弃尚未领取的条目。", "放弃并离开"), run.LeaveLoot),
                    () => { string before = Save(run); confirmation.Cancel(); cancelled = Save(run) == before; },
                    () => { run = Restore(run); reloaded = run.CurrentRewards.Select(r => r.Id).SequenceEqual(candidates); }
                },
                Labels = new[] { "查看容量不足的保留结果", "查看未领取项的离开提示", "取消离开", "重新读回同一份候选" },
                Verify = () => preserved && cancelled && reloaded,
                Fingerprint = () => Save(run) + "|" + confirmation.IsOpen, Proofs = new[] { "overflow-preserved", "leave-cancel", "reload-same" }
            };
        }
        private static Model RewardDetails()
        {
            var run = FixedReward(); var reward = run.CurrentRewards.First(); bool opened = false;
            string[] candidates = run.CurrentRewards.Select(r => r.Id).ToArray();
            var spell = FireSpellCatalog.Get(reward.Id);
            if (!FireSpellCardCopyCatalog.TryGet(spell.Id, out string cardCopy)) throw new InvalidOperationException("Missing production card copy.");
            string rarity = spell.Rarity == FireSpellRarity.Common ? "普通" : spell.Rarity == FireSpellRarity.Uncommon ? "罕见" : "稀有";
            return new Model
            {
                Prompt = "品质与完整效果在详情中查看。重新打开不会改变候选，不需要记强弱排行。", ActionLabel = "查看示例候选详情",
                Read = () => opened ? reward.DisplayName + "\n品质 " + rarity + "\n" + cardCopy : "示例候选 " + reward.DisplayName + "\n打开详情查看品质与完整效果",
                Apply = () => { opened = true; run = Restore(run); }, Verify = () => opened && run.CurrentRewards.Select(r => r.Id).SequenceEqual(candidates),
                Fingerprint = () => opened + "|" + string.Join("|", run.CurrentRewards.Select(r => r.Id)), Proofs = new[] { "rarity-view", "detail-view" }
            };
        }
        private static Model VictorySummary()
        {
            var source = Service("academy_gate"); var dto = source.ExportRogue11(); dto.CurrentNodeId = RogueliteAcademyLayerCatalog.FinaleNodeId; dto.StageTime = AcademyMapTuning.TransitionProgress;
            var run = RogueliteMapRun.FromRogue11(dto); run.CompleteCurrentCombat(); Save(run);
            return new Model
            {
                Prompt = "总结前先逐项处理待领内容。确认关闭本轮后，只保留已经领取的结果。",
                Read = () => "待领取 " + (run.HasUnclaimedLoot ? "仍有条目" : "已处理完") + "\n本轮状态 " + (run.IsComplete ? "胜利总结" : "等待处理战果"),
                Steps = new Action[] { () => run.ClaimFixedLoot("gold"), () => run.ClaimFixedLoot("contribution"), () => { var choice = run.CurrentRewards.First(); run.OpenPendingRewardChoices(); run.SelectPendingReward(choice.Id); run.ClaimLootChoice(choice.Id); }, run.LeaveLoot },
                Labels = new[] { "领取本次金币", "领取本次贡献", "领取一个演练候选", "查看完整胜利总结" },
                Verify = () => !run.HasUnclaimedLoot && run.IsComplete && run.RunEndReason == "victory", Fingerprint = () => Save(run), Proofs = new[] { "pending-resolved", "run-summary" }
            };
        }
    }
}
