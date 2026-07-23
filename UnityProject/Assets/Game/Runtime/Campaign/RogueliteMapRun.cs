using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public enum RogueliteMapNodeType { Start, Combat, Elite, Event, Workshop, Shop, Rest, Treasure, Finale }
    public enum RogueliteMapNodeVisualState { Current, Available, Locked, Cleared, Visited, Known, Unknown }

    public sealed class RogueliteMapNode
    {
        public string Id { get; }
        public RogueliteMapNodeType Type { get; }
        public IReadOnlyList<string> NextIds { get; }
        public string DisplayName { get; }
        public string Summary { get; }
        public int GridX { get; }
        public int GridY { get; }
        public int RequiredAccessCards { get; }
        public int GrantedAccessCards { get; }
        public bool IsCombat => Type == RogueliteMapNodeType.Combat || Type == RogueliteMapNodeType.Elite || Type == RogueliteMapNodeType.Finale;

        public RogueliteMapNode(string id, RogueliteMapNodeType type, string displayName, string summary, int gridX, int gridY, int requiredAccessCards, int grantedAccessCards, params string[] nextIds)
        {
            Id = id; Type = type; DisplayName = displayName; Summary = summary; GridX = gridX; GridY = gridY;
            RequiredAccessCards = requiredAccessCards; GrantedAccessCards = grantedAccessCards; NextIds = nextIds ?? Array.Empty<string>();
        }
    }

    public enum RogueliteRewardKind { Weapon, Spell }
    public enum RogueliteNodeContentEffect { Supplies, ScoutingBeacon, AccessCard, Reward, Aether }
    public sealed class RogueliteNodeContentChoice
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Preview { get; }
        public RogueliteNodeContentEffect Effect { get; }
        public string RewardId { get; }
        public bool RequiresCombat { get; }
        public string CombatMissionId { get; }
        public int PartsCost { get; }
        public int AetherCost { get; }
        public RogueliteNodeContentChoice(string id, string displayName, string preview, RogueliteNodeContentEffect effect, string rewardId = null, bool requiresCombat = false, string combatMissionId = null, int partsCost = 0, int aetherCost = 0)
        { Id = id; DisplayName = displayName; Preview = preview; Effect = effect; RewardId = rewardId; RequiresCombat = requiresCombat; CombatMissionId = combatMissionId; PartsCost = partsCost; AetherCost = aetherCost; }
    }

    public static class RogueliteNodeContentCatalog
    {
        public static IReadOnlyList<RogueliteNodeContentChoice> ChoicesFor(RogueliteMapNode node)
        {
            switch (node.Type)
            {
                case RogueliteMapNodeType.Event:
                    return new[]
                    {
                        new RogueliteNodeContentChoice("survey", "低风险勘测", "收益：+1 侦测信标；无额外战斗。", RogueliteNodeContentEffect.ScoutingBeacon),
                        new RogueliteNodeContentChoice("purify", "净化导管", "收益：+1 补给、+1 以太；无额外战斗。", RogueliteNodeContentEffect.Aether),
                        new RogueliteNodeContentChoice("overload", "超载回收", "收益：+1 权限卡；后果：进入一场额外战斗。", RogueliteNodeContentEffect.AccessCard, requiresCombat: true, combatMissionId: "relay_event")
                    };
                case RogueliteMapNodeType.Rest:
                    return new[]
                    {
                        new RogueliteNodeContentChoice("field_repair", "现场整备", "收益：+1 补给；无敌情推进。", RogueliteNodeContentEffect.Supplies),
                        new RogueliteNodeContentChoice("scan_routes", "校准信标", "收益：+1 侦测信标；无额外战斗。", RogueliteNodeContentEffect.ScoutingBeacon)
                    };
                case RogueliteMapNodeType.Workshop:
                    return new[]
                    {
                        new RogueliteNodeContentChoice("wand_calibration", "以太聚焦校准", "收益：获得以太聚焦手杖；替换操作将在 R2-03 工坊系统开放。", RogueliteNodeContentEffect.Reward, "arcane_wand"),
                        new RogueliteNodeContentChoice("supply_strip", "拆解补给", "收益：+1 补给；无额外战斗。", RogueliteNodeContentEffect.Supplies)
                    };
                case RogueliteMapNodeType.Shop:
                    return new[]
                    {
                        new RogueliteNodeContentChoice("medical_cache", "医疗补给", "价格：2 零件；收益：+1 补给。", RogueliteNodeContentEffect.Supplies, partsCost: 2),
                        new RogueliteNodeContentChoice("signal_contract", "情报合约", "价格：1 零件 + 1 以太；收益：+1 侦测信标。", RogueliteNodeContentEffect.ScoutingBeacon, partsCost: 1, aetherCost: 1)
                    };
                case RogueliteMapNodeType.Treasure:
                    return new[]
                    {
                        new RogueliteNodeContentChoice("vault_wand", "以太器械箱", "收益：获得以太手杖。", RogueliteNodeContentEffect.Reward, "aether_wand"),
                        new RogueliteNodeContentChoice("vault_spell", "术式档案箱", "收益：获得火矢术式。", RogueliteNodeContentEffect.Reward, "fire_bolt")
                    };
                default: return Array.Empty<RogueliteNodeContentChoice>();
            }
        }
    }

    public sealed class RogueliteReward
    {
        public string Id { get; }
        public string DisplayName { get; }
        public RogueliteRewardKind Kind { get; }
        public string BuildPath { get; }
        public WeaponDefinition Weapon { get; }
        public SkillDefinition Spell { get; }
        public RogueliteReward(string id, string displayName, WeaponDefinition weapon, string buildPath) { Id = id; DisplayName = displayName; Kind = RogueliteRewardKind.Weapon; Weapon = weapon; BuildPath = buildPath; }
        public RogueliteReward(string id, string displayName, SkillDefinition spell, string buildPath) { Id = id; DisplayName = displayName; Kind = RogueliteRewardKind.Spell; Spell = spell; BuildPath = buildPath; }
    }

    public static class RogueliteMapCatalog
    {
        // This is an orthogonal room graph: every listed connection is reciprocated by its neighbor.
        public static readonly IReadOnlyList<RogueliteMapNode> Nodes = new[]
        {
            new RogueliteMapNode("start", RogueliteMapNodeType.Start, "前线入口", "区域入口。", 0, 2, 0, 0, "rail_patrol", "depot_wreck", "supply_checkpoint"),
            new RogueliteMapNode("rail_patrol", RogueliteMapNodeType.Combat, "铁路巡逻", "清除巡逻队。", 1, 2, 0, 0, "start", "switchyard", "relay_raid", "supply_checkpoint"),
            new RogueliteMapNode("depot_wreck", RogueliteMapNodeType.Combat, "货场残骸", "夺回侧线。", 1, 1, 0, 0, "start", "switchyard"),
            new RogueliteMapNode("supply_checkpoint", RogueliteMapNodeType.Shop, "补给检查站", "补给与零件交易。", 1, 3, 0, 0, "start", "rail_patrol", "field_workshop"),
            new RogueliteMapNode("switchyard", RogueliteMapNodeType.Event, "道岔信号", "风险与收益预览事件。", 2, 1, 0, 0, "depot_wreck", "rail_patrol", "signal_hub", "relay_event"),
            new RogueliteMapNode("relay_raid", RogueliteMapNodeType.Combat, "野战中继", "破坏敌方中继器。", 2, 2, 0, 0, "rail_patrol", "relay_event", "med_bay", "field_workshop"),
            new RogueliteMapNode("field_workshop", RogueliteMapNodeType.Workshop, "野战工坊", "更换与维护构筑。", 2, 3, 0, 0, "supply_checkpoint", "relay_raid", "med_bay", "permit_archive"),
            new RogueliteMapNode("signal_hub", RogueliteMapNodeType.Combat, "信号枢纽", "清除主干站守军。", 3, 1, 0, 0, "switchyard", "relay_event", "elite_foundry"),
            new RogueliteMapNode("relay_event", RogueliteMapNodeType.Event, "中继站事件", "回访时收益递减。", 3, 2, 0, 0, "switchyard", "relay_raid", "signal_hub", "med_bay", "gatehouse"),
            new RogueliteMapNode("med_bay", RogueliteMapNodeType.Rest, "战地医疗站", "恢复与休整。", 3, 3, 0, 0, "relay_raid", "field_workshop", "relay_event", "permit_archive", "sealed_market"),
            new RogueliteMapNode("elite_foundry", RogueliteMapNodeType.Elite, "精英铸造厂", "高风险精英战斗。", 4, 1, 0, 0, "signal_hub", "gatehouse", "transmission_tower"),
            new RogueliteMapNode("gatehouse", RogueliteMapNodeType.Combat, "阀门关卡", "打开通往深层的战线。", 4, 2, 0, 0, "relay_event", "elite_foundry", "sealed_market", "aether_refinery"),
            new RogueliteMapNode("sealed_market", RogueliteMapNodeType.Shop, "封存商行", "双货币交易点。", 4, 3, 0, 0, "med_bay", "gatehouse", "permit_archive", "aether_refinery", "safety_room"),
            new RogueliteMapNode("permit_archive", RogueliteMapNodeType.Event, "许可档案", "可预览的档案提取；完成后获得权限卡。", 4, 4, 0, 1, "field_workshop", "med_bay", "sealed_market", "safety_room"),
            new RogueliteMapNode("transmission_tower", RogueliteMapNodeType.Combat, "传输塔", "权限门后的战斗节点。", 5, 1, 1, 0, "elite_foundry", "aether_refinery", "core_approach"),
            new RogueliteMapNode("aether_refinery", RogueliteMapNodeType.Event, "以太精炼厂", "可预览的高收益事件。", 5, 2, 0, 0, "gatehouse", "sealed_market", "transmission_tower", "safety_room", "core_vault"),
            new RogueliteMapNode("safety_room", RogueliteMapNodeType.Rest, "安全舱", "无敌情推进的休整点。", 5, 3, 0, 0, "sealed_market", "permit_archive", "aether_refinery", "core_vault"),
            new RogueliteMapNode("core_approach", RogueliteMapNodeType.Elite, "核心前哨", "核心区精英守备。", 6, 1, 1, 0, "transmission_tower", "core_vault", "core_finale"),
            new RogueliteMapNode("core_vault", RogueliteMapNodeType.Treasure, "核心库房", "战利品节点。", 6, 2, 1, 0, "aether_refinery", "safety_room", "core_approach", "core_finale"),
            new RogueliteMapNode("core_finale", RogueliteMapNodeType.Finale, "区域核心", "击败区域核心首领。", 7, 1, 1, 0, "core_approach", "core_vault")
        };
        public static readonly IReadOnlyList<RogueliteReward> Rewards = new[]
        {
            new RogueliteReward("war_hammer", "破甲战锤", CombatCatalog.Hammer, "突击"),
            new RogueliteReward("aether_wand", "以太手杖", CombatCatalog.Wand, "控制"),
            new RogueliteReward("fire_bolt", "火矢术式", CombatCatalog.FireBolt, "突击"),
            new RogueliteReward("frost_bind", "冰缚术式", CombatCatalog.FrostBind, "控制"),
            new RogueliteReward("arcane_wand", "以太聚焦手杖", StageTwoBuilds.ArcaneWand, "以太")
        };
        public static RogueliteMapNode Node(string id) => Nodes.First(node => node.Id == id);
        public static IReadOnlyList<RogueliteReward> RollRewards(int seed, int completedCombatCount)
        {
            var random = new Random(seed + completedCombatCount * 7919);
            return Rewards.OrderBy(_ => random.Next()).Take(3).ToArray();
        }
    }

    public sealed class RogueliteMapRun
    {
        private readonly HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal) { "start" };
        private readonly HashSet<string> completed = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> claimedRewards = new List<string>();
        public int Seed { get; }
        public string CurrentNodeId { get; private set; } = "start";
        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int AccessCards { get; private set; }
        public int Supplies { get; private set; }
        public int ScoutingBeacons { get; private set; }
        public int Parts { get; private set; } = 4;
        public int Aether { get; private set; } = 2;
        public string RegionBossId { get; private set; }
        public string EquippedWeaponId { get; private set; }
        public string EquippedSpellId { get; private set; }
        public bool IsAetherCalibrated { get; private set; }
        public string PendingContentChoiceId { get; private set; }
        public string PendingContentCombatMissionId { get; private set; }
        public bool AwaitingReward { get; private set; }
        public bool IsComplete => completed.Contains("core_finale") && !AwaitingReward;
        public IReadOnlyCollection<string> UnlockedNodes => visited;
        public IReadOnlyCollection<string> VisitedNodes => visited;
        public IReadOnlyCollection<string> CompletedNodes => completed;
        public IReadOnlyList<string> ClaimedRewards => claimedRewards;
        public IReadOnlyList<RogueliteReward> CurrentRewards => AwaitingReward ? RogueliteMapCatalog.RollRewards(Seed, completed.Count) : Array.Empty<RogueliteReward>();
        public IReadOnlyList<RogueliteNodeContentChoice> CurrentContentChoices => RogueliteNodeContentCatalog.ChoicesFor(RogueliteMapCatalog.Node(CurrentNodeId));
        public bool HasPendingContentCombat => !string.IsNullOrEmpty(PendingContentCombatMissionId);
        public RogueliteMapRun(int seed) { Seed = seed; RegionBossId = seed % 2 == 0 ? "core_overseer" : "purifier_overseer"; }

        public bool IsAdjacentToCurrent(string nodeId) => RogueliteMapCatalog.Node(CurrentNodeId).NextIds.Contains(nodeId);
        public bool IsNodeAvailable(string nodeId)
        {
            RogueliteMapNode node = RogueliteMapCatalog.Node(nodeId);
            return IsAdjacentToCurrent(nodeId) && AccessCards >= node.RequiredAccessCards;
        }
        public bool IsNodeKnown(string nodeId) => visited.Contains(nodeId) || RogueliteMapCatalog.Node(CurrentNodeId).NextIds.Contains(nodeId);
        public RogueliteMapNodeVisualState VisualStateFor(string nodeId)
        {
            RogueliteMapNode node = RogueliteMapCatalog.Node(nodeId);
            if (node.Id == CurrentNodeId) return RogueliteMapNodeVisualState.Current;
            if (completed.Contains(node.Id)) return RogueliteMapNodeVisualState.Cleared;
            if (IsNodeAvailable(node.Id)) return RogueliteMapNodeVisualState.Available;
            if (IsAdjacentToCurrent(node.Id) && AccessCards < node.RequiredAccessCards) return RogueliteMapNodeVisualState.Locked;
            if (visited.Contains(node.Id) || IsNodeKnown(node.Id)) return RogueliteMapNodeVisualState.Known;
            return RogueliteMapNodeVisualState.Unknown;
        }
        public IReadOnlyList<RogueliteMapNode> AvailableNodes => RogueliteMapCatalog.Nodes.Where(node => IsNodeAvailable(node.Id)).ToArray();
        public void SelectNode(string nodeId)
        {
            if (!IsNodeAvailable(nodeId)) throw new InvalidOperationException("Node is not adjacent or its permission gate is locked.");
            CurrentNodeId = nodeId; visited.Add(nodeId);
        }
        public void CompleteCurrentCombat()
        {
            RogueliteMapNode node = RogueliteMapCatalog.Node(CurrentNodeId);
            if (!node.IsCombat || completed.Contains(node.Id)) throw new InvalidOperationException("Current node is not an active combat.");
            Complete(node, true);
        }
        public void CompleteCurrentNode()
        {
            RogueliteMapNode node = RogueliteMapCatalog.Node(CurrentNodeId);
            if (node.Type == RogueliteMapNodeType.Start || completed.Contains(node.Id)) throw new InvalidOperationException("Current node is not available.");
            Complete(node, node.IsCombat);
        }
        public void ChooseCurrentNodeContent(string choiceId)
        {
            RogueliteMapNode node = RogueliteMapCatalog.Node(CurrentNodeId);
            if (node.IsCombat || completed.Contains(node.Id) || HasPendingContentCombat) throw new InvalidOperationException("Current node content is not available.");
            RogueliteNodeContentChoice choice = CurrentContentChoices.FirstOrDefault(item => item.Id == choiceId) ?? throw new InvalidOperationException("Unknown node content choice.");
            if (Parts < choice.PartsCost || Aether < choice.AetherCost) throw new InvalidOperationException("Insufficient parts or aether.");
            Parts -= choice.PartsCost; Aether -= choice.AetherCost;
            PendingContentChoiceId = choice.Id;
            if (choice.RequiresCombat) { PendingContentCombatMissionId = choice.CombatMissionId; return; }
            ApplyContentChoice(choice); Complete(node, false); PendingContentChoiceId = null;
        }
        public void CompletePendingContentCombat()
        {
            if (!HasPendingContentCombat) throw new InvalidOperationException("No event combat is active.");
            RogueliteNodeContentChoice choice = CurrentContentChoices.First(item => item.Id == PendingContentChoiceId);
            ApplyContentChoice(choice); Complete(RogueliteMapCatalog.Node(CurrentNodeId), false);
            PendingContentChoiceId = null; PendingContentCombatMissionId = null;
        }
        private void ApplyContentChoice(RogueliteNodeContentChoice choice)
        {
            if (choice.Effect == RogueliteNodeContentEffect.Supplies) Supplies++;
            else if (choice.Effect == RogueliteNodeContentEffect.ScoutingBeacon) ScoutingBeacons++;
            else if (choice.Effect == RogueliteNodeContentEffect.AccessCard) AccessCards++;
            else if (choice.Effect == RogueliteNodeContentEffect.Aether) { Supplies++; Aether++; }
            else if (choice.Effect == RogueliteNodeContentEffect.Reward && !claimedRewards.Contains(choice.RewardId)) claimedRewards.Add(choice.RewardId);
        }
        private void Complete(RogueliteMapNode node, bool offerReward)
        {
            completed.Add(node.Id); Experience++; if (Experience >= Level) Level++;
            if (node.IsCombat) { Parts += 2; Aether++; }
            AccessCards += node.GrantedAccessCards;
            AwaitingReward = offerReward;
        }
        public void ClaimReward(string rewardId) { if (!AwaitingReward || CurrentRewards.All(reward => reward.Id != rewardId)) throw new InvalidOperationException("Reward is not available."); claimedRewards.Add(rewardId); AwaitingReward = false; }
        public void EquipReward(string rewardId)
        {
            RogueliteReward reward = claimedRewards.Contains(rewardId) ? RogueliteMapCatalog.Rewards.First(item => item.Id == rewardId) : throw new InvalidOperationException("Reward is not owned.");
            if (reward.Kind == RogueliteRewardKind.Weapon) EquippedWeaponId = rewardId; else EquippedSpellId = rewardId;
        }
        public void CalibrateAether()
        {
            if (Aether < 2) throw new InvalidOperationException("Insufficient aether for calibration.");
            Aether -= 2; IsAetherCalibrated = true;
        }
        public string ToJson() => string.Join("|", "map5", Seed, RegionBossId, CurrentNodeId, Level, Experience, AccessCards, Supplies, ScoutingBeacons, Parts, Aether, EquippedWeaponId ?? string.Empty, EquippedSpellId ?? string.Empty, IsAetherCalibrated ? "1" : "0", PendingContentChoiceId ?? string.Empty, PendingContentCombatMissionId ?? string.Empty, string.Join(",", visited), string.Join(",", completed), string.Join(",", claimedRewards), AwaitingReward ? "1" : "0");
        public static RogueliteMapRun FromJson(string json)
        {
            string[] parts = (json ?? throw new ArgumentNullException(nameof(json))).Split('|');
            if (parts.Length == 9 && parts[0] == "map1") return FromMap1(parts);
            if (parts.Length == 10 && parts[0] == "map2") return FromMap2(parts);
            if (parts.Length == 14 && parts[0] == "map3") return FromMap3(parts);
            if (parts.Length == 19 && parts[0] == "map4") return FromMap4(parts);
            if (parts.Length != 20 || parts[0] != "map5") throw new InvalidOperationException("Unsupported map run save version.");
            var run = new RogueliteMapRun(int.Parse(parts[1])) { RegionBossId = parts[2], CurrentNodeId = parts[3], Level = int.Parse(parts[4]), Experience = int.Parse(parts[5]), AccessCards = int.Parse(parts[6]), Supplies = int.Parse(parts[7]), ScoutingBeacons = int.Parse(parts[8]), Parts = int.Parse(parts[9]), Aether = int.Parse(parts[10]), EquippedWeaponId = parts[11], EquippedSpellId = parts[12], IsAetherCalibrated = parts[13] == "1", PendingContentChoiceId = parts[14], PendingContentCombatMissionId = parts[15], AwaitingReward = parts[19] == "1" };
            Restore(run.visited, parts[16]); Restore(run.completed, parts[17]); run.claimedRewards.AddRange(parts[18].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)); return run;
        }
        private static RogueliteMapRun FromMap4(string[] parts)
        {
            var run = new RogueliteMapRun(int.Parse(parts[1])) { CurrentNodeId = parts[2], Level = int.Parse(parts[3]), Experience = int.Parse(parts[4]), AccessCards = int.Parse(parts[5]), Supplies = int.Parse(parts[6]), ScoutingBeacons = int.Parse(parts[7]), Parts = int.Parse(parts[8]), Aether = int.Parse(parts[9]), EquippedWeaponId = parts[10], EquippedSpellId = parts[11], IsAetherCalibrated = parts[12] == "1", PendingContentChoiceId = parts[13], PendingContentCombatMissionId = parts[14], AwaitingReward = parts[18] == "1" };
            Restore(run.visited, parts[15]); Restore(run.completed, parts[16]); run.claimedRewards.AddRange(parts[17].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)); return run;
        }
        private static RogueliteMapRun FromMap3(string[] parts)
        {
            var run = new RogueliteMapRun(int.Parse(parts[1])) { CurrentNodeId = parts[2], Level = int.Parse(parts[3]), Experience = int.Parse(parts[4]), AccessCards = int.Parse(parts[5]), Supplies = int.Parse(parts[6]), ScoutingBeacons = int.Parse(parts[7]), PendingContentChoiceId = parts[8], PendingContentCombatMissionId = parts[9], AwaitingReward = parts[13] == "1" };
            Restore(run.visited, parts[10]); Restore(run.completed, parts[11]); run.claimedRewards.AddRange(parts[12].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)); return run;
        }
        private static RogueliteMapRun FromMap2(string[] parts)
        {
            var run = new RogueliteMapRun(int.Parse(parts[1])) { CurrentNodeId = parts[2], Level = int.Parse(parts[3]), Experience = int.Parse(parts[4]), AccessCards = int.Parse(parts[5]), AwaitingReward = parts[9] == "1" };
            Restore(run.visited, parts[6]); Restore(run.completed, parts[7]); run.claimedRewards.AddRange(parts[8].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)); return run;
        }
        private static RogueliteMapRun FromMap1(string[] parts)
        {
            var run = new RogueliteMapRun(int.Parse(parts[1])) { CurrentNodeId = parts[2], Level = int.Parse(parts[3]), Experience = int.Parse(parts[4]), AwaitingReward = parts[8] == "1" };
            Restore(run.visited, parts[5]); Restore(run.completed, parts[6]); run.claimedRewards.AddRange(parts[7].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            if (run.visited.Contains("core_finale")) run.AccessCards = 1;
            return run;
        }
        private static void Restore(HashSet<string> destination, string source)
        {
            destination.Clear(); foreach (string id in source.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) if (RogueliteMapCatalog.Nodes.Any(node => node.Id == id)) destination.Add(id);
            destination.Add("start");
        }
        public void ApplyBuild(UnitState hero)
        {
            if (!string.IsNullOrEmpty(EquippedWeaponId)) hero.Equip(RogueliteMapCatalog.Rewards.First(item => item.Id == EquippedWeaponId).Weapon, CombatCatalog.Shield, hero.SkillOne, hero.SkillTwo);
            if (!string.IsNullOrEmpty(EquippedSpellId)) hero.Equip(hero.MainHand, CombatCatalog.Shield, RogueliteMapCatalog.Rewards.First(item => item.Id == EquippedSpellId).Spell, hero.SkillTwo);
            if (IsAetherCalibrated) hero.Armor += 1;
        }
    }
}
