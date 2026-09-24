using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public sealed class UnitState
    {
        public const int BaseMovementRange = 5;
        public const int HeroBaseMovementRange = 3;
        public const int SlowedMovementRange = 3;
        public const int HeroSlowedMovementRange = 2;
        public const int HeroBaseHealth = 50;

        private readonly Dictionary<StatusType, int> statuses = new Dictionary<StatusType, int>();
        private readonly Dictionary<StatusType, int> statusStrengths = new Dictionary<StatusType, int>();
        private readonly Dictionary<StatusType, string> statusSources = new Dictionary<StatusType, string>();
        private readonly Dictionary<StatusType, int> statusTriggerCounts = new Dictionary<StatusType, int>();
        private readonly Dictionary<StatusType, int> statusAppliedOrders = new Dictionary<StatusType, int>();
        private readonly List<AttributeStatusInstance> attributeStatuses = new List<AttributeStatusInstance>();
        private int nextStatusOrder;
        private readonly Dictionary<string, int> cooldowns = new Dictionary<string, int>(StringComparer.Ordinal);

        public string Id { get; }
        public bool IsHero { get; }
        public GridPosition Position { get; private set; }
        public int ActionPoints { get; private set; }
        public string DisplayName { get; set; }
        public string EnemyArchetypeId { get; private set; }
        public int Health { get; private set; } = 12;
        public int MaxHealth { get; private set; }
        public int Mana { get; private set; }
        public int MaxMana { get; private set; }
        public int Armor { get; set; } = 1;
        public int Shield { get; private set; } = 2;
        public int MaxShield { get; set; } = 6;
        public int Block { get; set; } = 1;
        public int Speed { get; set; } = 10;
        public int ActionValue { get; private set; }
        public WeaponDefinition MainHand { get; private set; } = CombatCatalog.Rifle;
        public WeaponDefinition OffHand => null;
        public SkillDefinition SkillOne { get; private set; } = CombatCatalog.FireBolt;
        public SkillDefinition SkillTwo { get; private set; } = CombatCatalog.FrostBind;
        public IReadOnlyDictionary<StatusType, int> Statuses
        {
            get
            {
                if (attributeStatuses.Count == 0) return statuses;
                var result = new Dictionary<StatusType, int>(statuses);
                foreach (AttributeStatusInstance instance in attributeStatuses)
                    result[instance.Type] = Math.Max(result.TryGetValue(instance.Type, out int previous) ? previous : 0, instance.Duration);
                return result;
            }
        }
        public bool IsAlive => Health > 0;
        public int EffectiveArmor => Math.Max(0, Armor);
        public int EffectiveSpeed => Math.Max(1, Speed + StatusStrength(StatusType.Speed));
        public int MovementRangeThisTurn { get; private set; }
        public int NaturalMovementRange => IsHero ? HeroBaseMovementRange : BaseMovementRange;
        public int EffectiveRange(int baseRange) => Math.Max(0, baseRange + StatusStrength(StatusType.Range));
        public int SlowedNaturalMovementRange => IsHero ? HeroSlowedMovementRange : SlowedMovementRange;
        /// <summary>霸体：处于该状态时不会受到健康伤害，用于封存塔首领的阶段〇走流程。</summary>
        public bool IsSuperArmored { get; internal set; }
        /// <summary>还能替本单位承受多少次健康伤害（引链：机关代核心承伤）。</summary>
        public int DamageAbsorptions { get; internal set; }

        public UnitState(string id, bool isHero, GridPosition position)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A unit id is required.", nameof(id));
            Id = id; IsHero = isHero; Position = position; DisplayName = id;
            MaxHealth = isHero ? HeroBaseHealth : 12; Health = MaxHealth; MaxMana = isHero ? 6 : 4; Mana = MaxMana;
            MovementRangeThisTurn = NaturalMovementRange;
        }

        public bool HasStatus(StatusType type) => StatusDuration(type) > 0;
        public int StatusDuration(StatusType type) => IsAttributeStatus(type)
            ? attributeStatuses.Where(instance => instance.Type == type).Select(instance => instance.Duration).DefaultIfEmpty().Max()
            : statuses.TryGetValue(type, out int turns) ? turns : 0;
        public int StatusStrength(StatusType type, int fallback = 0) => IsAttributeStatus(type)
            ? attributeStatuses.Where(instance => instance.Type == type).Select(instance => instance.Value).DefaultIfEmpty(fallback).Sum()
            : statusStrengths.TryGetValue(type, out int strength) ? strength : fallback;
        public string StatusSource(StatusType type) => IsAttributeStatus(type)
            ? string.Join("、", attributeStatuses.Where(instance => instance.Type == type).Select(instance => instance.SourceId)
                .Where(source => !string.IsNullOrEmpty(source)).Distinct())
            : statusSources.TryGetValue(type, out string source) ? source : string.Empty;
        public int StatusTriggerCount(StatusType type) => IsAttributeStatus(type)
            ? attributeStatuses.Where(instance => instance.Type == type).Sum(instance => instance.TriggerCount)
            : statusTriggerCounts.TryGetValue(type, out int count) ? count : 0;
        public int StatusAppliedOrder(StatusType type) => IsAttributeStatus(type)
            ? attributeStatuses.Where(instance => instance.Type == type).Select(instance => instance.Order).DefaultIfEmpty(-1).Min()
            : statusAppliedOrders.TryGetValue(type, out int order) ? order : -1;
        public static bool IsAttributeStatus(StatusType type) => type == StatusType.Agility || type == StatusType.Strength ||
            type == StatusType.SpellPower || type == StatusType.Speed || type == StatusType.Range ||
            type == StatusType.DamageTaken || type == StatusType.ShieldEfficiency || type == StatusType.ShieldGrant ||
            type == StatusType.Control;
        public bool IsSkillReady(SkillDefinition skill) => skill != null && (!cooldowns.TryGetValue(skill.Id, out int turns) || turns <= 0);
        public int Cooldown(SkillDefinition skill) => skill != null && cooldowns.TryGetValue(skill.Id, out int turns) ? turns : 0;
        public void Equip(WeaponDefinition mainHand, WeaponDefinition offHand, SkillDefinition skillOne, SkillDefinition skillTwo)
        {
            MainHand = mainHand ?? MainHand; SkillOne = skillOne ?? SkillOne; SkillTwo = skillTwo ?? SkillTwo;
        }
        public void Equip(WeaponDefinition weapon, SkillDefinition skillOne, SkillDefinition skillTwo)
        { MainHand = weapon ?? MainHand; SkillOne = skillOne ?? SkillOne; SkillTwo = skillTwo ?? SkillTwo; }
        internal void AssignEnemyArchetype(string archetypeId) => EnemyArchetypeId = archetypeId;
        /// <summary>清除第二技能槽：用于只有一套公开战法、其余手段由运行时结算的单位。</summary>
        internal void ClearSecondarySkill() => SkillTwo = null;
        /// <summary>把仍在冷却的技能整体减一，最低保留 1 回合。</summary>
        internal void ReduceCooldowns(int amount)
        {
            if (amount <= 0 || cooldowns.Count == 0) return;
            foreach (string key in cooldowns.Keys.ToArray()) cooldowns[key] = Math.Max(1, cooldowns[key] - amount);
        }
        public void ConfigureVitality(int maxHealth)
        {
            if (maxHealth < 1) throw new ArgumentOutOfRangeException(nameof(maxHealth));
            MaxHealth = maxHealth; Health = maxHealth;
        }
        public void ConfigureMana(int maxMana, int currentMana = -1)
        {
            if (maxMana < 0) throw new ArgumentOutOfRangeException(nameof(maxMana));
            MaxMana = maxMana; Mana = currentMana < 0 ? maxMana : Math.Min(maxMana, Math.Max(0, currentMana));
        }

        internal void BeginTurn(int actionPoints, int agilityAtTurnStart = 0)
        { ActionPoints = actionPoints; MovementRangeThisTurn = Math.Max(0, NaturalMovementRange + agilityAtTurnStart); }
        internal void GrantActionPoints(int amount) { if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount)); ActionPoints = Math.Min(3, ActionPoints + amount); }
        internal void GrantBonusActionPoints(int amount) { if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount)); ActionPoints += amount; }
        internal void SetMovementRangeForTurn(int range) => MovementRangeThisTurn = Math.Max(MovementRangeThisTurn, range);
        internal void LimitMovementRangeForTurn(int range) => MovementRangeThisTurn = Math.Min(MovementRangeThisTurn, Math.Max(0, range));
        internal void MoveTo(GridPosition destination) => Position = destination;
        internal void SpendActionPoint(int amount) { if (amount < 0 || amount > ActionPoints) throw new InvalidOperationException("Unit does not have enough action points."); ActionPoints -= amount; }
        internal void TakeDamage(int amount)
        {
            if (IsSuperArmored || HasStatus(StatusType.Invulnerable) || amount <= 0) return;
            // 引链：已放行的塔内机关替核心承受一次健康伤害；消耗由首领运行时在下一回合拆掉该机关。
            if (DamageAbsorptions > 0) { DamageAbsorptions--; return; }
            Health = Math.Max(0, Health - amount);
        }
        internal int AbsorbShield(int amount) { int absorbed = Math.Min(Shield, amount); Shield -= absorbed; return absorbed; }
        internal void ClearShield() => Shield = 0;
        internal void Heal(int amount) => Health = Math.Min(MaxHealth, Health + amount);
        internal void RestoreShield(int amount)
        {
            if (HasStatus(StatusType.BreakStance)) return;
            Shield = Math.Min(MaxShield, Shield + Math.Max(0, amount + StatusStrength(StatusType.ShieldEfficiency)));
        }
        internal void GrantShield(int amount)
        {
            if (HasStatus(StatusType.BreakStance)) return;
            Shield += Math.Max(0, amount + StatusStrength(StatusType.ShieldEfficiency));
        }
        internal void SpendMana(int amount) { if (amount < 0 || amount > Mana) throw new InvalidOperationException("\u4ee5\u592a\u4e0d\u8db3\u3002"); Mana -= amount; }
        internal void RestoreMana(int amount) => Mana = Math.Min(MaxMana, Mana + amount);
        internal void SetActionValue(int value) => ActionValue = Math.Max(0, Math.Min(199, value));
        internal void ChangeActionValue(int delta) => SetActionValue(ActionValue + delta);
        public void ApplyStatus(StatusType type, int duration) => ApplyStatus(type, duration, 0, null);
        public void ApplyStatus(StatusType type, int duration, int strength) => ApplyStatus(type, duration, strength, null);
        public void ApplyStatus(StatusType type, int duration, int strength, string sourceId)
        {
            if (duration <= 0) return;
            // 束缚在目标回合开始扣减；保留本次应受限回合，避免“1 回合”在行动前直接失效。
            if (type == StatusType.Bound && duration < int.MaxValue) duration++;
            if (IsAttributeStatus(type))
            {
                if (strength != 0) attributeStatuses.Add(new AttributeStatusInstance(type, duration, strength, sourceId, nextStatusOrder++));
                return;
            }
            if (!statuses.ContainsKey(type))
            {
                statusTriggerCounts[type] = 0;
                statusAppliedOrders[type] = nextStatusOrder++;
            }
            statuses[type] = Math.Max(StatusDuration(type), duration);
            bool stronger = strength != 0 && (!statusStrengths.TryGetValue(type, out int previousStrength) ||
                Math.Abs((long)strength) > Math.Abs((long)previousStrength));
            if (stronger) statusStrengths[type] = strength;
            if (!string.IsNullOrWhiteSpace(sourceId) && (stronger || !statusSources.ContainsKey(type)))
                statusSources[type] = sourceId;
        }
        internal void RecordStatusTrigger(StatusType type)
        {
            if (IsAttributeStatus(type))
            {
                foreach (AttributeStatusInstance instance in attributeStatuses.Where(instance => instance.Type == type)) instance.TriggerCount++;
                return;
            }
            if (HasStatus(type)) statusTriggerCounts[type] = StatusTriggerCount(type) + 1;
        }
        internal void ClearStatus(StatusType type)
        {
            if (IsAttributeStatus(type)) { attributeStatuses.RemoveAll(instance => instance.Type == type); return; }
            statuses.Remove(type); statusStrengths.Remove(type); statusSources.Remove(type);
            statusTriggerCounts.Remove(type); statusAppliedOrders.Remove(type);
        }
        internal void SetStatusDuration(StatusType type, int duration)
        {
            if (duration <= 0) { ClearStatus(type); return; }
            if (IsAttributeStatus(type))
            {
                foreach (AttributeStatusInstance instance in attributeStatuses.Where(instance => instance.Type == type)) instance.Duration = duration;
                return;
            }
            statuses[type] = duration;
        }
        internal void ReduceStatusDuration(StatusType type, int amount)
        {
            if (IsAttributeStatus(type))
            {
                if (amount <= 0) return;
                foreach (AttributeStatusInstance instance in attributeStatuses.Where(instance => instance.Type == type && instance.Duration != int.MaxValue))
                    instance.Duration -= amount;
                attributeStatuses.RemoveAll(instance => instance.Duration <= 0);
                return;
            }
            if (amount <= 0 || !statuses.TryGetValue(type, out int duration)) return;
            int next = duration - amount;
            if (next > 0) statuses[type] = next;
            else ClearStatus(type);
        }
        internal void SetCooldown(SkillDefinition skill) { if (skill != null && skill.Cooldown > 0) cooldowns[skill.Id] = skill.Cooldown; }
        internal void TickCooldowns() => Tick(cooldowns);
        private static void Tick(Dictionary<string, int> values)
        {
            List<string> remove = new List<string>();
            List<string> keys = new List<string>(values.Keys);
            foreach (string key in keys) { int next = values[key] - 1; if (next <= 0) remove.Add(key); else values[key] = next; }
            foreach (string key in remove) values.Remove(key);
        }
        internal UnitState Clone()
        {
            UnitState clone = new UnitState(Id, IsHero, Position) { DisplayName = DisplayName, EnemyArchetypeId = EnemyArchetypeId, Armor = Armor, Shield = Shield, MaxShield = MaxShield, Block = Block, Speed = Speed, MainHand = MainHand, SkillOne = SkillOne, SkillTwo = SkillTwo };
            clone.Health = Health; clone.Mana = Mana; clone.ActionPoints = ActionPoints; clone.ActionValue = ActionValue; clone.MovementRangeThisTurn = MovementRangeThisTurn;
            clone.MaxHealth = MaxHealth; clone.MaxMana = MaxMana; clone.IsSuperArmored = IsSuperArmored; clone.DamageAbsorptions = DamageAbsorptions;
            foreach (KeyValuePair<StatusType, int> entry in statuses) clone.statuses[entry.Key] = entry.Value;
            foreach (KeyValuePair<StatusType, int> entry in statusStrengths) clone.statusStrengths[entry.Key] = entry.Value;
            foreach (KeyValuePair<StatusType, string> entry in statusSources) clone.statusSources[entry.Key] = entry.Value;
            foreach (KeyValuePair<StatusType, int> entry in statusTriggerCounts) clone.statusTriggerCounts[entry.Key] = entry.Value;
            foreach (KeyValuePair<StatusType, int> entry in statusAppliedOrders) clone.statusAppliedOrders[entry.Key] = entry.Value;
            foreach (AttributeStatusInstance instance in attributeStatuses) clone.attributeStatuses.Add(instance.Clone());
            clone.nextStatusOrder = nextStatusOrder;
            foreach (KeyValuePair<string, int> entry in cooldowns) clone.cooldowns[entry.Key] = entry.Value;
            return clone;
        }

        private sealed class AttributeStatusInstance
        {
            public readonly StatusType Type;
            public readonly int Value;
            public readonly string SourceId;
            public readonly int Order;
            public int Duration;
            public int TriggerCount;
            public AttributeStatusInstance(StatusType type, int duration, int value, string sourceId, int order)
            { Type = type; Duration = duration; Value = value; SourceId = sourceId; Order = order; }
            public AttributeStatusInstance Clone() => new AttributeStatusInstance(Type, Duration, Value, SourceId, Order)
            { TriggerCount = TriggerCount };
        }
    }
}
