using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public enum UiPresentationArea
    {
        Flow,
        MapStructure,
        MapResources,
        Settings,
        Combat,
        Settlement
    }

    public readonly struct UiPresentationChange
    {
        public UiPresentationArea Area { get; }
        public int Version { get; }

        public UiPresentationChange(UiPresentationArea area, int version)
        {
            Area = area;
            Version = version;
        }
    }

    public sealed class UiPresentationVersions
    {
        private readonly Dictionary<UiPresentationArea, int> versions = new Dictionary<UiPresentationArea, int>();
        public event Action<UiPresentationChange> Changed;

        public int Version(UiPresentationArea area) => versions.TryGetValue(area, out int value) ? value : 0;

        public void Mark(UiPresentationArea area)
        {
            int next = Version(area) + 1;
            versions[area] = next;
            Changed?.Invoke(new UiPresentationChange(area, next));
        }
    }

    public readonly struct CombatTurnTrackEntry
    {
        public int Order { get; }
        public string UnitId { get; }
        public string DisplayName { get; }
        public string VitalityText { get; }
        public bool IsHero { get; }
        public bool IsActive { get; }

        public CombatTurnTrackEntry(int order, UnitState unit, bool isActive)
        {
            Order = order;
            UnitId = unit.Id;
            DisplayName = unit.DisplayName;
            VitalityText = unit.Health + " 生命";
            IsHero = unit.IsHero;
            IsActive = isActive;
        }
    }

    public static class CombatTurnTrackPresentation
    {
        public static IReadOnlyList<CombatTurnTrackEntry> Build(CombatState state, int limit)
        {
            if (state == null || limit <= 0) return Array.Empty<CombatTurnTrackEntry>();
            return state.Units.Values
                .Where(unit => unit.IsAlive)
                .OrderBy(unit => unit.Id == state.ActiveUnitId ? 0 : 1)
                .ThenBy(unit => unit.InitiativeTime)
                .ThenBy(unit => unit.Id, StringComparer.Ordinal)
                .Take(limit)
                .Select((unit, index) => new CombatTurnTrackEntry(index + 1, unit, unit.Id == state.ActiveUnitId))
                .ToArray();
        }
    }

    public readonly struct RogueliteMapPresentationModel : IEquatable<RogueliteMapPresentationModel>
    {
        public int Seed { get; }
        public string CurrentNodeId { get; }
        public int Level { get; }
        public int Experience { get; }
        public int Parts { get; }
        public int Aether { get; }
        public int Supplies { get; }
        public int Scouting { get; }
        public int AccessCards { get; }
        public bool AwaitingReward { get; }

        private RogueliteMapPresentationModel(RogueliteMapRun run)
        {
            Seed = run.Seed;
            CurrentNodeId = run.CurrentNodeId ?? string.Empty;
            Level = run.Level;
            Experience = run.Experience;
            Parts = run.Parts;
            Aether = run.Aether;
            Supplies = run.Supplies;
            Scouting = run.ScoutingBeacons;
            AccessCards = run.AccessCards;
            AwaitingReward = run.AwaitingReward;
        }

        public static RogueliteMapPresentationModel From(RogueliteMapRun run) => run == null ? default : new RogueliteMapPresentationModel(run);

        public bool Equals(RogueliteMapPresentationModel other) => Seed == other.Seed && CurrentNodeId == other.CurrentNodeId && Level == other.Level &&
            Experience == other.Experience && Parts == other.Parts && Aether == other.Aether && Supplies == other.Supplies && Scouting == other.Scouting &&
            AccessCards == other.AccessCards && AwaitingReward == other.AwaitingReward;
        public override bool Equals(object obj) => obj is RogueliteMapPresentationModel other && Equals(other);
        public override int GetHashCode() => Seed;
    }

    public enum RogueliteMapRouteVisualState
    {
        Unknown,
        Known,
        Available,
        Safe,
        Locked
    }

    public static class RogueliteMapVisualPresentation
    {
        public static string FocusKey(string nodeId) => "map.node." + (nodeId ?? string.Empty);

        public static string StateLabel(RogueliteMapNodeVisualState state)
        {
            switch (state)
            {
                case RogueliteMapNodeVisualState.Current: return "当前位置";
                case RogueliteMapNodeVisualState.Available: return "可前往";
                case RogueliteMapNodeVisualState.Cleared: return "已完成";
                case RogueliteMapNodeVisualState.Visited: return "已访问";
                case RogueliteMapNodeVisualState.Locked: return "还不能去";
                case RogueliteMapNodeVisualState.Known: return "已经发现";
                default: return "还看不清";
            }
        }

        public static string StateGlyph(RogueliteMapNodeVisualState state)
        {
            switch (state)
            {
                case RogueliteMapNodeVisualState.Current: return "现";
                case RogueliteMapNodeVisualState.Available: return "可";
                case RogueliteMapNodeVisualState.Cleared: return "清";
                case RogueliteMapNodeVisualState.Visited: return "访";
                case RogueliteMapNodeVisualState.Locked: return "锁";
                case RogueliteMapNodeVisualState.Known: return "知";
                default: return "?";
            }
        }

        public static RogueliteMapRouteVisualState RouteState(RogueliteMapNodeVisualState from, RogueliteMapNodeVisualState to)
        {
            if (from == RogueliteMapNodeVisualState.Unknown || to == RogueliteMapNodeVisualState.Unknown) return RogueliteMapRouteVisualState.Unknown;
            if ((from == RogueliteMapNodeVisualState.Current && to == RogueliteMapNodeVisualState.Available) ||
                (to == RogueliteMapNodeVisualState.Current && from == RogueliteMapNodeVisualState.Available)) return RogueliteMapRouteVisualState.Available;
            if ((from == RogueliteMapNodeVisualState.Cleared || from == RogueliteMapNodeVisualState.Current) &&
                (to == RogueliteMapNodeVisualState.Cleared || to == RogueliteMapNodeVisualState.Current)) return RogueliteMapRouteVisualState.Safe;
            if (from == RogueliteMapNodeVisualState.Locked || to == RogueliteMapNodeVisualState.Locked) return RogueliteMapRouteVisualState.Locked;
            return RogueliteMapRouteVisualState.Known;
        }

        public static string RouteGlyph(RogueliteMapRouteVisualState state)
        {
            switch (state)
            {
                case RogueliteMapRouteVisualState.Available: return "可";
                case RogueliteMapRouteVisualState.Safe: return "安";
                case RogueliteMapRouteVisualState.Locked: return "锁";
                case RogueliteMapRouteVisualState.Known: return "联";
                default: return "?";
            }
        }

        public static string RestrictionText(RogueliteMapRun run, RogueliteMapNode node)
        {
            if (run == null || node == null) return "这里现在去不了";
            RogueliteMapNodeVisualState state = run.VisualStateFor(node.Id);
            if (state == RogueliteMapNodeVisualState.Unknown) return "还看不清这里";
            if (node.Id == run.CurrentNodeId) return "你就在这里";
            if (run.CompletedNodes.Contains(node.Id)) return "已经处理妥当，可以再去看看";
            if (RogueliteUiPreferences.CanTravelTo(run, node)) return "可以直接前往";
            if (run.IsAcademyFinaleGateLocked(node))
                return "还不能参加终考：再完成 " + Math.Max(0, AcademyMapTuning.BossMinimumProgress - run.AcademyProgress) +
                    " 个地点，并拿到 " + Math.Max(0, AcademyMapTuning.CorePermitRequirement - run.CorePermits) + " 枚核心许可";
            if (state == RogueliteMapNodeVisualState.Locked) return "核心许可不足：需要 " + node.RequiredAccessCards + "，当前 " + run.ProgressPermits;
            return "这里离得太远，请先走到相邻地点";
        }

        public static string AcademyStatus(RogueliteMapRun run)
        {
            if (run == null) return "现在还看不到学期日程";
            string phase = run.AcademyPhase == AcademyMapPhase.Consolidation ? "学期将尽" :
                run.AcademyPhase == AcademyMapPhase.TransitionReady ? "终考将至" : "日程还宽裕";
            string finale = run.CanChallengeAcademyFinale ? "现在可以参加终考" :
                "参加终考前，还要完成 " + Math.Max(0, AcademyMapTuning.BossMinimumProgress - run.AcademyProgress) + " 个地点并拿到 " +
                Math.Max(0, AcademyMapTuning.CorePermitRequirement - run.CorePermits) + " 枚许可";
            return "学期进度 " + run.StageTime + "/" + AcademyMapTuning.TransitionProgress + " · " + phase +
                " · 核心许可 " + run.CorePermits + "/" + AcademyMapTuning.CorePermitRequirement + " · " + finale;
        }

        public static string ConnectionSummary(RogueliteMapRun run, RogueliteMapNode node)
        {
            if (run == null || node == null || run.VisualStateFor(node.Id) == RogueliteMapNodeVisualState.Unknown) return "附近的路还看不清";
            string[] known = node.NextIds.Select(RogueliteMapCatalog.Node)
                .Where(next => run.VisualStateFor(next.Id) != RogueliteMapNodeVisualState.Unknown)
                .Select(next => next.DisplayName).ToArray();
            return known.Length == 0 ? "附近还没有发现别的去处" : "从这里还能去：" + string.Join(" / ", known);
        }
    }

    public readonly struct UiOperationAvailability
    {
        public bool CanExecute { get; }
        public string Status { get; }
        public string Reason { get; }

        public UiOperationAvailability(bool canExecute, string status, string reason)
        {
            CanExecute = canExecute;
            Status = status ?? string.Empty;
            Reason = reason ?? string.Empty;
        }
    }

    public static class RogueliteEconomyPresentation
    {
        public static UiOperationAvailability ForNodeChoice(RogueliteMapRun run, RogueliteNodeContentChoice choice)
        {
            if (run == null || choice == null) return Blocked("不可执行", "选项不可用");
            if (run.UsesRogue11)
            {
                if (run.Gold < choice.GoldCost) return Blocked("金币不足", "需要 " + choice.GoldCost + " 金币；当前 " + run.Gold);
                if (run.StageContribution < choice.ContributionCost) return Blocked("学院贡献不足", "需要 " + choice.ContributionCost + " 学院贡献；当前 " + run.StageContribution);
                if (choice.HealthGain < 0 && run.CurrentHealth + choice.HealthGain <= 0) return Blocked("生命不足", "该选择会使生命归零");
                if (!string.IsNullOrEmpty(choice.RewardId) && run.ClaimedRewards.Contains(choice.RewardId)) return Blocked("已经拿过", "这里没有第二件了");
                if (!CanAcceptRogueContent(run, choice.RewardId)) return Blocked("行囊装不下", "先整理出一块能放下这件东西的位置");
                return new UiOperationAvailability(true, choice.RequiresCombat ? "可以出发" : "可以选择", CostText(choice));
            }
            if (run.Parts < choice.PartsCost) return Blocked("零件不足", "需要 " + choice.PartsCost + " 零件；当前 " + run.Parts);
            if (run.Aether < choice.AetherCost) return Blocked("以太不足", "需要 " + choice.AetherCost + " 以太；当前 " + run.Aether);
            if (!string.IsNullOrEmpty(choice.RewardId) && run.ClaimedRewards.Contains(choice.RewardId)) return Blocked("已经拿过", "这里没有第二件了");
            ItemDefinition item = string.IsNullOrEmpty(choice.RewardId) ? null : ItemCatalog.All.FirstOrDefault(candidate => candidate.Id == choice.RewardId);
            if (item != null && !CanAccept(run, item)) return Blocked("背包空间不足", "需要可容纳 " + item.Width + "×" + item.Height + " 的空位");
            return new UiOperationAvailability(true, choice.PartsCost + choice.AetherCost > 0 ? "可以买下" : "可以选择", CostText(choice));
        }

        public static string NodeChoiceSummary(RogueliteMapRun run, RogueliteNodeContentChoice choice, UiOperationAvailability availability)
        {
            if (choice == null) return string.Empty;
            if (!availability.CanExecute) return availability.Reason;
            List<string> costs = new List<string>();
            if (choice.GoldCost > 0) costs.Add(choice.GoldCost + "金");
            if (choice.ContributionCost > 0) costs.Add(choice.ContributionCost + "学院贡献");
            if (choice.HealthGain < 0) costs.Add((-choice.HealthGain) + "生命");
            if (run == null || !run.UsesRogue11)
            {
                if (choice.PartsCost > 0) costs.Add(choice.PartsCost + "零件");
                if (choice.AetherCost > 0) costs.Add(choice.AetherCost + "以太");
            }

            List<string> outcomes = new List<string>();
            if (choice.GoldGain > 0) outcomes.Add("+" + choice.GoldGain + "金");
            if (choice.ContributionGain > 0) outcomes.Add("+" + choice.ContributionGain + "学院贡献");
            if (choice.HealthGain > 0) outcomes.Add("+" + choice.HealthGain + "生命");
            if (choice.ManaGain > 0) outcomes.Add("+" + choice.ManaGain + "魔力");
            if (choice.GrantsCorePermit) outcomes.Add("核心许可");
            string rewardName = RewardDisplayName(choice.RewardId);
            if (!string.IsNullOrEmpty(rewardName)) outcomes.Add(rewardName);
            if (choice.RequiresCombat) outcomes.Insert(0, outcomes.Count == 0 ? "进入战斗" : "胜利");
            if (outcomes.Count == 0) outcomes.Add("完成这里的事情");
            return (costs.Count == 0 ? "不用花东西" : "花 " + string.Join(" + ", costs)) + "；" + string.Join(" + ", outcomes);
        }

        public static UiOperationAvailability ForReward(RogueliteMapRun run, RogueliteReward reward)
        {
            if (run == null || reward == null || !run.AwaitingReward) return Blocked("现在不能拿", "这场战斗没有留下可带走的东西");
            if (reward.Kind == RogueliteRewardKind.Item && !CanAccept(run, reward.Item))
                return Blocked("行囊放不下", "先整理出一块 " + reward.Item.Width + "×" + reward.Item.Height + " 的空位");
            if (run.UsesRogue11 && reward.Kind == RogueliteRewardKind.Equipment && !CanAcceptRogueContent(run, reward.Id))
                return Blocked("行囊放不下", "先整理出一块能放下这件装备的位置");
            if (reward.Kind != RogueliteRewardKind.Item && run.ClaimedRewards.Contains(reward.Id)) return Blocked("已经拿过了", "这件奖励已经带走");
            if (FireSpellCatalog.All.Any(spell => spell.Id == reward.Id) && run.OwnedFireSpellIds.Contains(reward.Id)) return Blocked("已经学会了", "术式册里已经有这道术式");
            return new UiOperationAvailability(true, "可以带走", reward.Kind == RogueliteRewardKind.Item ? "会放进行囊" : "会收进术式册");
        }

        public static UiOperationAvailability ForEquipment(RogueliteMapRun run, RogueliteReward reward)
        {
            if (run == null || reward == null || !run.ClaimedRewards.Contains(reward.Id)) return Blocked("还没拿到", "先取得这件奖励");
            bool equipped = reward.Kind == RogueliteRewardKind.Weapon ? run.EquippedWeaponId == reward.Id : reward.Kind == RogueliteRewardKind.Spell && run.EquippedSpellId == reward.Id;
            if (equipped) return Blocked("已经装备", "你现在正在使用它");
            if (reward.Kind == RogueliteRewardKind.Weapon && run.EquippedFireSpellIds.Where(id => !string.IsNullOrEmpty(id)).Select(FireSpellCatalog.Get)
                .Any(spell => !FireSpellCatalog.IsWeaponCompatible(spell, reward.Weapon)))
                return Blocked("术式不兼容", "先在工坊调整与该武器冲突的已装备术式");
            if (reward.Kind == RogueliteRewardKind.Item) return Blocked("请在背包装备", "法宝与消耗品不使用工坊装备槽");
            return new UiOperationAvailability(true, "可装备", reward.Kind == RogueliteRewardKind.Weapon ? WeaponComparison(run.EquippedWeapon, reward.Weapon) : "将替换技能槽 1");
        }

        public static string RewardComparison(RogueliteMapRun run, RogueliteReward reward)
        {
            if (run == null || reward == null) return string.Empty;
            if (reward.Kind == RogueliteRewardKind.Weapon) return WeaponComparison(run.EquippedWeapon, reward.Weapon);
            if (reward.Kind == RogueliteRewardKind.Item) return CanAccept(run, reward.Item) ? "行囊里放得下" : "行囊已经装不下了";
            if (reward.Kind == RogueliteRewardKind.Equipment) return "领取后放进行囊，可在战斗外装备";
            return "领取后收进术式册，不会立刻换上";
        }

        private static string WeaponComparison(WeaponDefinition current, WeaponDefinition candidate)
        {
            if (current == null || candidate == null) return string.Empty;
            return "对比当前：伤害 " + Signed(candidate.Damage - current.Damage) + " / 射程 " + Signed(candidate.Range - current.Range) + " / 穿甲 " + Signed(candidate.ArmorPierce - current.ArmorPierce);
        }

        private static string CostText(RogueliteNodeContentChoice choice)
        {
            if (choice.GoldCost > 0 || choice.ContributionCost > 0)
                return "需要 " + choice.GoldCost + " 金币 / " + choice.ContributionCost + " 学院贡献";
            if (choice.PartsCost == 0 && choice.AetherCost == 0) return "不用花东西";
            return "需要 " + choice.PartsCost + " 零件 / " + choice.AetherCost + " 以太";
        }

        private static string RewardDisplayName(string rewardId)
        {
            if (string.IsNullOrEmpty(rewardId)) return string.Empty;
            OCC.Combat.Roguelite.RogueContentCatalog catalog = OCC.Combat.Roguelite.RogueContentCatalog.CreateAcademyV01();
            OCC.Combat.Roguelite.SpellDefinition spell = catalog.Spells.FirstOrDefault(value => value.DefinitionId == rewardId);
            if (spell != null) return spell.DisplayName;
            OCC.Combat.Roguelite.EquipmentDefinition equipment = catalog.Equipment.FirstOrDefault(value => value.DefinitionId == rewardId);
            if (equipment != null) return equipment.DisplayName;
            OCC.Combat.Roguelite.TacticalItemDefinition tactical = catalog.TacticalItems.FirstOrDefault(value => value.DefinitionId == rewardId);
            return tactical?.DisplayName ?? rewardId;
        }

        private static bool CanAccept(RogueliteMapRun run, ItemDefinition item)
        {
            if (run?.Inventory == null || item == null) return false;
            return run.Inventory.FindFirstFit(new ItemInstance("__ui_preview__", item.Id, int.MaxValue)).Success;
        }

        private static bool CanAcceptRogueContent(RogueliteMapRun run, string rewardId)
        {
            if (string.IsNullOrEmpty(rewardId)) return true;
            OCC.Combat.Roguelite.RogueContentCatalog catalog = OCC.Combat.Roguelite.RogueContentCatalog.CreateAcademyV01();
            if (catalog.Spells.Any(value => value.DefinitionId == rewardId)) return true;
            OCC.Combat.Roguelite.RogueEquipmentRuntime runtime = OCC.Combat.Roguelite.RogueEquipmentRuntime.FromDto(run.RogueRunState);
            OCC.Combat.Roguelite.EquipmentDefinition equipment = catalog.Equipment.FirstOrDefault(value => value.DefinitionId == rewardId);
            if (equipment != null)
            {
                OCC.Combat.Roguelite.RogueEquipmentInstance preview = runtime.CreateInstance("__content_preview__", rewardId,
                    equipment.AllowedRarities[0], int.MaxValue, "preview");
                return runtime.AddToBackpack(preview);
            }
            OCC.Combat.Roguelite.TacticalItemDefinition tactical = catalog.TacticalItems.FirstOrDefault(value => value.DefinitionId == rewardId);
            if (tactical == null) return true;
            OCC.Combat.Roguelite.RogueTacticalItemInstance item = runtime.CreateTacticalItem("__content_preview__", rewardId, int.MaxValue, "preview");
            return runtime.AddTacticalToBackpack(item);
        }

        private static string Signed(int value) => value > 0 ? "+" + value : value.ToString();
        private static UiOperationAvailability Blocked(string status, string reason) => new UiOperationAvailability(false, status, reason);
    }

    public readonly struct CombatHudPresentationModel : IEquatable<CombatHudPresentationModel>
    {
        public string ActiveUnitId { get; }
        public int ActiveActionPoints { get; }
        public int Health { get; }
        public int Shield { get; }
        public int Mana { get; }
        public string SelectedAction { get; }
        public string SelectedTargetId { get; }
        public bool OutcomeVisible { get; }
        public string EventHead { get; }
        public string TimelineKey { get; }
        public string HeroKey { get; }
        public string EnemyKey { get; }
        public string EventKey { get; }

        private CombatHudPresentationModel(CombatState state, string selectedAction, string selectedTargetId, bool outcomeVisible)
        {
            UnitState active = state.GetUnit(state.ActiveUnitId);
            UnitState hero = state.GetUnit("hero");
            ActiveUnitId = state.ActiveUnitId ?? string.Empty;
            ActiveActionPoints = active == null ? -1 : active.ActionPoints;
            Health = hero == null ? 0 : hero.Health;
            Shield = hero == null ? 0 : hero.Shield;
            Mana = hero == null ? 0 : hero.Mana;
            SelectedAction = selectedAction ?? string.Empty;
            SelectedTargetId = selectedTargetId ?? string.Empty;
            OutcomeVisible = outcomeVisible;
            EventHead = state.EventLog.Count == 0 ? string.Empty : state.EventLog[0];
            EventKey = string.Join("|", state.EventLog.Take(5));
            TimelineKey = string.Join("|", state.Units.Values.Where(unit => unit.IsAlive).OrderBy(unit => unit.InitiativeTime)
                .Select(unit => unit.Id + ":" + unit.Health + ":" + unit.Shield + ":" + unit.InitiativeTime));
            HeroKey = hero == null ? string.Empty : string.Join("|", hero.MainHand?.Id ?? string.Empty, hero.Armor, hero.ActionPoints,
                hero.SkillOne == null ? 0 : hero.Cooldown(hero.SkillOne), hero.SkillTwo == null ? 0 : hero.Cooldown(hero.SkillTwo),
                string.Join(",", hero.Statuses.OrderBy(item => item.Key).Select(item => item.Key + ":" + item.Value)),
                string.Join(",", state.ItemQuickbar.Select(instanceId =>
                {
                    ItemInstance item = state.ItemInventory.Get(instanceId);
                    return item == null ? string.Empty : item.InstanceId + ":" + item.DefinitionId + ":" + item.RemainingUses;
                })));
            EnemyKey = string.Join("|", state.Units.Values.Where(unit => !unit.IsHero && unit.IsAlive).OrderBy(unit => unit.Id, StringComparer.Ordinal)
                .Select(unit => string.Join(":", unit.Id, unit.Health, unit.Shield, unit.Mana, unit.ActionPoints,
                    unit.SkillOne == null ? 0 : unit.Cooldown(unit.SkillOne), unit.SkillTwo == null ? 0 : unit.Cooldown(unit.SkillTwo),
                    string.Join(",", unit.Statuses.OrderBy(item => item.Key).Select(item => item.Key + "=" + item.Value)))));
        }

        public static CombatHudPresentationModel From(CombatState state, string selectedAction, string selectedTargetId, bool outcomeVisible) =>
            state == null ? default : new CombatHudPresentationModel(state, selectedAction, selectedTargetId, outcomeVisible);

        public bool Equals(CombatHudPresentationModel other) => ActiveUnitId == other.ActiveUnitId && ActiveActionPoints == other.ActiveActionPoints &&
            Health == other.Health && Shield == other.Shield && Mana == other.Mana && SelectedAction == other.SelectedAction &&
            SelectedTargetId == other.SelectedTargetId && OutcomeVisible == other.OutcomeVisible && EventHead == other.EventHead && EventKey == other.EventKey &&
            TimelineKey == other.TimelineKey && HeroKey == other.HeroKey && EnemyKey == other.EnemyKey;
        public override bool Equals(object obj) => obj is CombatHudPresentationModel other && Equals(other);
        public override int GetHashCode() => (ActiveUnitId ?? string.Empty).GetHashCode();
    }

    public readonly struct SettlementPresentationModel : IEquatable<SettlementPresentationModel>
    {
        public int Seed { get; }
        public bool Visible { get; }
        public int Level { get; }
        public int Experience { get; }
        public string RewardKey { get; }

        private SettlementPresentationModel(RogueliteMapRun run)
        {
            Seed = run.Seed;
            Visible = run.AwaitingReward;
            Level = run.Level;
            Experience = run.Experience;
            RewardKey = string.Join("|", run.CurrentFireSpellChoices.Select(spell => spell.Id).Concat(run.CurrentRewards.Select(reward => reward.Id)));
        }

        public static SettlementPresentationModel From(RogueliteMapRun run) => run == null ? default : new SettlementPresentationModel(run);
        public bool Equals(SettlementPresentationModel other) => Seed == other.Seed && Visible == other.Visible && Level == other.Level && Experience == other.Experience && RewardKey == other.RewardKey;
        public override bool Equals(object obj) => obj is SettlementPresentationModel other && Equals(other);
        public override int GetHashCode() => Seed;
    }
}
