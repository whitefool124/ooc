using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public enum RogueliteMapNodeType { Start, Combat, Finale }
    public sealed class RogueliteMapNode
    {
        public string Id { get; }
        public RogueliteMapNodeType Type { get; }
        public IReadOnlyList<string> NextIds { get; }
        public RogueliteMapNode(string id, RogueliteMapNodeType type, params string[] nextIds) { Id = id; Type = type; NextIds = nextIds ?? Array.Empty<string>(); }
    }
    public enum RogueliteRewardKind { Weapon, Spell }
    public sealed class RogueliteReward
    {
        public string Id { get; }
        public string DisplayName { get; }
        public RogueliteRewardKind Kind { get; }
        public WeaponDefinition Weapon { get; }
        public SkillDefinition Spell { get; }
        public RogueliteReward(string id, string displayName, WeaponDefinition weapon) { Id = id; DisplayName = displayName; Kind = RogueliteRewardKind.Weapon; Weapon = weapon; }
        public RogueliteReward(string id, string displayName, SkillDefinition spell) { Id = id; DisplayName = displayName; Kind = RogueliteRewardKind.Spell; Spell = spell; }
    }
    public static class RogueliteMapCatalog
    {
        public static readonly IReadOnlyList<RogueliteMapNode> Nodes = new[]
        {
            new RogueliteMapNode("start", RogueliteMapNodeType.Start, "rail_patrol", "relay_raid"),
            new RogueliteMapNode("rail_patrol", RogueliteMapNodeType.Combat, "core_finale"),
            new RogueliteMapNode("relay_raid", RogueliteMapNodeType.Combat, "core_finale"),
            new RogueliteMapNode("core_finale", RogueliteMapNodeType.Finale)
        };
        public static readonly IReadOnlyList<RogueliteReward> Rewards = new[]
        {
            new RogueliteReward("war_hammer", "破甲战锤", CombatCatalog.Hammer),
            new RogueliteReward("aether_wand", "以太手杖", CombatCatalog.Wand),
            new RogueliteReward("fire_bolt", "火矢术式", CombatCatalog.FireBolt),
            new RogueliteReward("frost_bind", "冰缚术式", CombatCatalog.FrostBind),
            new RogueliteReward("arcane_wand", "以太聚焦手杖", StageTwoBuilds.ArcaneWand)
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
        private readonly HashSet<string> unlocked = new HashSet<string>(StringComparer.Ordinal) { "start", "rail_patrol", "relay_raid" };
        private readonly HashSet<string> completed = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> claimedRewards = new List<string>();
        public int Seed { get; }
        public string CurrentNodeId { get; private set; } = "start";
        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public bool AwaitingReward { get; private set; }
        public bool IsComplete => completed.Contains("core_finale") && !AwaitingReward;
        public IReadOnlyCollection<string> UnlockedNodes => unlocked;
        public IReadOnlyCollection<string> CompletedNodes => completed;
        public IReadOnlyList<string> ClaimedRewards => claimedRewards;
        public IReadOnlyList<RogueliteReward> CurrentRewards => AwaitingReward ? RogueliteMapCatalog.RollRewards(Seed, completed.Count) : Array.Empty<RogueliteReward>();
        public RogueliteMapRun(int seed) { Seed = seed; }
        public void SelectNode(string nodeId) { if (!unlocked.Contains(nodeId) || RogueliteMapCatalog.Node(nodeId).Type == RogueliteMapNodeType.Start) throw new InvalidOperationException("Node is not available."); CurrentNodeId = nodeId; }
        public void CompleteCurrentCombat()
        {
            RogueliteMapNode node = RogueliteMapCatalog.Node(CurrentNodeId);
            if (node.Type == RogueliteMapNodeType.Start || completed.Contains(node.Id)) throw new InvalidOperationException("Current node is not an active combat.");
            completed.Add(node.Id); Experience++; if (Experience >= Level) Level++; foreach (string next in node.NextIds) unlocked.Add(next); AwaitingReward = true;
        }
        public void ClaimReward(string rewardId) { if (!AwaitingReward || CurrentRewards.All(reward => reward.Id != rewardId)) throw new InvalidOperationException("Reward is not available."); claimedRewards.Add(rewardId); AwaitingReward = false; }
        public string ToJson() => string.Join("|", "map1", Seed, CurrentNodeId, Level, Experience, string.Join(",", unlocked), string.Join(",", completed), string.Join(",", claimedRewards), AwaitingReward ? "1" : "0");
        public static RogueliteMapRun FromJson(string json)
        {
            string[] parts = json.Split('|'); if (parts.Length != 9 || parts[0] != "map1") throw new InvalidOperationException("Unsupported map run save version.");
            var run = new RogueliteMapRun(int.Parse(parts[1])) { CurrentNodeId = parts[2], Level = int.Parse(parts[3]), Experience = int.Parse(parts[4]), AwaitingReward = parts[8] == "1" };
            run.unlocked.Clear(); foreach (string id in parts[5].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) run.unlocked.Add(id);
            foreach (string id in parts[6].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) run.completed.Add(id);
            run.claimedRewards.AddRange(parts[7].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)); return run;
        }
        public void ApplyBuild(UnitState hero)
        {
            foreach (string id in claimedRewards)
            {
                RogueliteReward reward = RogueliteMapCatalog.Rewards.First(item => item.Id == id);
                if (reward.Kind == RogueliteRewardKind.Weapon) hero.Equip(reward.Weapon, CombatCatalog.Shield, hero.SkillOne, hero.SkillTwo);
                else hero.Equip(hero.MainHand, CombatCatalog.Shield, reward.Spell, hero.SkillTwo);
            }
        }
    }
}
