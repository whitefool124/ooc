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
        public IReadOnlyDictionary<StatusType, int> Statuses => statuses;
        public bool IsAlive => Health > 0;
        public int EffectiveArmor => Math.Max(0, Armor - (HasStatus(StatusType.ArmorBreak) ? StatusStrength(StatusType.ArmorBreak, 2) : 0));
        public int EffectiveSpeed => Math.Max(1, Speed - (HasStatus(StatusType.Slow) ? 3 : 0));
        public int MovementRangeThisTurn { get; private set; }
        public int NaturalMovementRange => IsHero ? HeroBaseMovementRange : BaseMovementRange;
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

        public bool HasStatus(StatusType type) => statuses.TryGetValue(type, out int turns) && turns > 0;
        public int StatusDuration(StatusType type) => statuses.TryGetValue(type, out int turns) ? turns : 0;
        public int StatusStrength(StatusType type, int fallback = 0) => statusStrengths.TryGetValue(type, out int strength) ? strength : fallback;
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
        /// <summary>把所有技能冷却整体减一（最低 0）。用于塔之守卫换装时的并链效果。</summary>
        internal void ReduceCooldowns(int amount)
        {
            if (amount <= 0 || cooldowns.Count == 0) return;
            foreach (string key in cooldowns.Keys.ToArray()) cooldowns[key] = Math.Max(0, cooldowns[key] - amount);
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

        internal void BeginTurn(int actionPoints, bool slowedAtTurnStart = false)
        { ActionPoints = actionPoints; MovementRangeThisTurn = slowedAtTurnStart ? SlowedNaturalMovementRange : NaturalMovementRange; }
        internal void GrantActionPoints(int amount) { if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount)); ActionPoints = Math.Min(3, ActionPoints + amount); }
        internal void GrantBonusActionPoints(int amount) { if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount)); ActionPoints += amount; }
        internal void SetMovementRangeForTurn(int range) => MovementRangeThisTurn = Math.Max(MovementRangeThisTurn, range);
        internal void LimitMovementRangeForTurn(int range) => MovementRangeThisTurn = Math.Min(MovementRangeThisTurn, Math.Max(0, range));
        internal void MoveTo(GridPosition destination) => Position = destination;
        internal void SpendActionPoint(int amount) { if (amount < 0 || amount > ActionPoints) throw new InvalidOperationException("Unit does not have enough action points."); ActionPoints -= amount; }
        internal void TakeDamage(int amount)
        {
            if (IsSuperArmored || amount <= 0) return;
            // 引链：已放行的塔内机关替核心承受一次健康伤害；消耗由首领运行时在下一回合拆掉该机关。
            if (DamageAbsorptions > 0) { DamageAbsorptions--; return; }
            Health = Math.Max(0, Health - amount);
        }
        internal int AbsorbShield(int amount) { int absorbed = Math.Min(Shield, amount); Shield -= absorbed; return absorbed; }
        internal void ClearShield() => Shield = 0;
        internal void Heal(int amount) => Health = Math.Min(MaxHealth, Health + amount);
        internal void RestoreShield(int amount) => Shield = Math.Min(MaxShield, Shield + amount);
        internal void GrantShield(int amount) { if (amount > 0) Shield += amount; }
        internal void SpendMana(int amount) { if (amount < 0 || amount > Mana) throw new InvalidOperationException("\u4ee5\u592a\u4e0d\u8db3\u3002"); Mana -= amount; }
        internal void RestoreMana(int amount) => Mana = Math.Min(MaxMana, Mana + amount);
        internal void SetActionValue(int value) => ActionValue = Math.Max(0, Math.Min(199, value));
        internal void ChangeActionValue(int delta) => SetActionValue(ActionValue + delta);
        public void ApplyStatus(StatusType type, int duration) => ApplyStatus(type, duration, 0);
        public void ApplyStatus(StatusType type, int duration, int strength)
        {
            if (duration <= 0) return;
            statuses[type] = Math.Max(StatusDuration(type), duration);
            if (strength > 0) statusStrengths[type] = Math.Max(StatusStrength(type), strength);
        }
        internal void ClearStatus(StatusType type) { statuses.Remove(type); statusStrengths.Remove(type); }
        internal void SetStatusDuration(StatusType type, int duration)
        {
            if (duration <= 0) { ClearStatus(type); return; }
            statuses[type] = duration;
        }
        internal void ReduceStatusDuration(StatusType type, int amount)
        {
            if (amount <= 0 || !statuses.TryGetValue(type, out int duration)) return;
            int next = duration - amount;
            if (next > 0) statuses[type] = next;
            else { statuses.Remove(type); statusStrengths.Remove(type); }
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
            foreach (KeyValuePair<string, int> entry in cooldowns) clone.cooldowns[entry.Key] = entry.Value;
            return clone;
        }
    }
}
