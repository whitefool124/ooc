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
