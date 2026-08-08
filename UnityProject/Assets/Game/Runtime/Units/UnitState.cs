using System;
using System.Collections.Generic;

namespace OCC.Combat
{
    public sealed class UnitState
    {
        private readonly Dictionary<StatusType, int> statuses = new Dictionary<StatusType, int>();
        private readonly Dictionary<StatusType, int> statusStrengths = new Dictionary<StatusType, int>();
        private readonly Dictionary<string, int> cooldowns = new Dictionary<string, int>(StringComparer.Ordinal);

        public string Id { get; }
        public bool IsHero { get; }
        public GridPosition Position { get; private set; }
        public Facing Facing { get; private set; }
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
        public int InitiativeTime { get; private set; }
        public WeaponDefinition MainHand { get; private set; } = CombatCatalog.Rifle;
        public WeaponDefinition OffHand { get; private set; } = CombatCatalog.Shield;
        public SkillDefinition SkillOne { get; private set; } = CombatCatalog.FireBolt;
        public SkillDefinition SkillTwo { get; private set; } = CombatCatalog.FrostBind;
        public IReadOnlyDictionary<StatusType, int> Statuses => statuses;
        public bool IsAlive => Health > 0;
        public int EffectiveArmor => Math.Max(0, Armor - (HasStatus(StatusType.ArmorBreak) ? StatusStrength(StatusType.ArmorBreak, 2) : 0));
        public int EffectiveSpeed => Math.Max(1, Speed - (HasStatus(StatusType.Slow) ? 3 : 0));
        public int MovementRangeThisTurn { get; private set; } = 3;

        public UnitState(string id, bool isHero, GridPosition position, Facing facing)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A unit id is required.", nameof(id));
            Id = id; IsHero = isHero; Position = position; Facing = facing; DisplayName = id;
            MaxHealth = isHero ? 18 : 12; Health = MaxHealth; MaxMana = isHero ? 6 : 4; Mana = MaxMana;
        }

        public bool HasStatus(StatusType type) => statuses.TryGetValue(type, out int turns) && turns > 0;
        public int StatusDuration(StatusType type) => statuses.TryGetValue(type, out int turns) ? turns : 0;
        public int StatusStrength(StatusType type, int fallback = 0) => statusStrengths.TryGetValue(type, out int strength) ? strength : fallback;
        public bool IsSkillReady(SkillDefinition skill) => skill != null && (!cooldowns.TryGetValue(skill.Id, out int turns) || turns <= 0);
        public int Cooldown(SkillDefinition skill) => skill != null && cooldowns.TryGetValue(skill.Id, out int turns) ? turns : 0;
        public void Equip(WeaponDefinition mainHand, WeaponDefinition offHand, SkillDefinition skillOne, SkillDefinition skillTwo)
        {
            MainHand = mainHand ?? MainHand; OffHand = offHand ?? OffHand; SkillOne = skillOne ?? SkillOne; SkillTwo = skillTwo ?? SkillTwo;
        }
        internal void AssignEnemyArchetype(string archetypeId) => EnemyArchetypeId = archetypeId;
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

        internal void BeginTurn(int actionPoints) { ActionPoints = actionPoints; MovementRangeThisTurn = 3; }
        internal void GrantActionPoints(int amount) { if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount)); ActionPoints = Math.Min(3, ActionPoints + amount); }
        internal void SetMovementRangeForTurn(int range) => MovementRangeThisTurn = Math.Max(MovementRangeThisTurn, range);
        internal void LimitMovementRangeForTurn(int range) => MovementRangeThisTurn = Math.Min(MovementRangeThisTurn, Math.Max(0, range));
        internal void MoveTo(GridPosition destination, Facing facing) { Position = destination; Facing = facing; }
        internal void TurnInPlace(Facing facing) => Facing = facing;
        internal void SpendActionPoint(int amount) { if (amount < 0 || amount > ActionPoints) throw new InvalidOperationException("Unit does not have enough action points."); ActionPoints -= amount; }
        internal void TakeDamage(int amount) => Health = Math.Max(0, Health - amount);
        internal int AbsorbShield(int amount) { int absorbed = Math.Min(Shield, amount); Shield -= absorbed; return absorbed; }
        internal void Heal(int amount) => Health = Math.Min(MaxHealth, Health + amount);
        internal void RestoreShield(int amount) => Shield = Math.Min(MaxShield, Shield + amount);
        internal void GrantShield(int amount) { if (amount > 0) Shield += amount; }
        internal void SpendMana(int amount) { if (amount < 0 || amount > Mana) throw new InvalidOperationException("\u4ee5\u592a\u4e0d\u8db3\u3002"); Mana -= amount; }
        internal void RestoreMana(int amount) => Mana = Math.Min(MaxMana, Mana + amount);
        internal void SetInitiativeTime(int time) => InitiativeTime = time;
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
            UnitState clone = new UnitState(Id, IsHero, Position, Facing) { DisplayName = DisplayName, EnemyArchetypeId = EnemyArchetypeId, Armor = Armor, Shield = Shield, MaxShield = MaxShield, Block = Block, Speed = Speed, MainHand = MainHand, OffHand = OffHand, SkillOne = SkillOne, SkillTwo = SkillTwo };
            clone.Health = Health; clone.Mana = Mana; clone.ActionPoints = ActionPoints; clone.InitiativeTime = InitiativeTime; clone.MovementRangeThisTurn = MovementRangeThisTurn;
            foreach (KeyValuePair<StatusType, int> entry in statuses) clone.statuses[entry.Key] = entry.Value;
            foreach (KeyValuePair<StatusType, int> entry in statusStrengths) clone.statusStrengths[entry.Key] = entry.Value;
            foreach (KeyValuePair<string, int> entry in cooldowns) clone.cooldowns[entry.Key] = entry.Value;
            return clone;
        }
    }
}
