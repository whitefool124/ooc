using System;
using System.Collections.Generic;

namespace OCC.Combat
{
    public sealed class UnitState
    {
        private readonly Dictionary<StatusType, int> statuses = new Dictionary<StatusType, int>();
        private readonly Dictionary<string, int> cooldowns = new Dictionary<string, int>(StringComparer.Ordinal);

        public string Id { get; }
        public bool IsHero { get; }
        public GridPosition Position { get; private set; }
        public Facing Facing { get; private set; }
        public int ActionPoints { get; private set; }
        public string DisplayName { get; set; }
        public int Health { get; private set; } = 12;
        public int MaxHealth { get; }
        public int Mana { get; private set; }
        public int MaxMana { get; }
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
        public int EffectiveArmor => Math.Max(0, Armor - (HasStatus(StatusType.ArmorBreak) ? 2 : 0));
        public int EffectiveSpeed => Math.Max(1, Speed - (HasStatus(StatusType.Slow) ? 3 : 0));

        public UnitState(string id, bool isHero, GridPosition position, Facing facing)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A unit id is required.", nameof(id));
            Id = id; IsHero = isHero; Position = position; Facing = facing; DisplayName = id;
            MaxHealth = isHero ? 18 : 12; Health = MaxHealth; MaxMana = isHero ? 6 : 4; Mana = MaxMana;
        }

        public bool HasStatus(StatusType type) => statuses.TryGetValue(type, out int turns) && turns > 0;
        public int StatusDuration(StatusType type) => statuses.TryGetValue(type, out int turns) ? turns : 0;
        public bool IsSkillReady(SkillDefinition skill) => skill != null && (!cooldowns.TryGetValue(skill.Id, out int turns) || turns <= 0);
        public int Cooldown(SkillDefinition skill) => skill != null && cooldowns.TryGetValue(skill.Id, out int turns) ? turns : 0;
        public void Equip(WeaponDefinition mainHand, WeaponDefinition offHand, SkillDefinition skillOne, SkillDefinition skillTwo)
        {
            MainHand = mainHand ?? MainHand; OffHand = offHand ?? OffHand; SkillOne = skillOne ?? SkillOne; SkillTwo = skillTwo ?? SkillTwo;
        }

        internal void BeginTurn(int actionPoints) => ActionPoints = actionPoints;
        internal void MoveTo(GridPosition destination, Facing facing) { Position = destination; Facing = facing; }
        internal void TurnInPlace(Facing facing) => Facing = facing;
        internal void SpendActionPoint(int amount) { if (amount < 0 || amount > ActionPoints) throw new InvalidOperationException("Unit does not have enough action points."); ActionPoints -= amount; }
        internal void TakeDamage(int amount) => Health = Math.Max(0, Health - amount);
        internal int AbsorbShield(int amount) { int absorbed = Math.Min(Shield, amount); Shield -= absorbed; return absorbed; }
        internal void Heal(int amount) => Health = Math.Min(MaxHealth, Health + amount);
        internal void RestoreShield(int amount) => Shield = Math.Min(MaxShield, Shield + amount);
        internal void SpendMana(int amount) { if (amount < 0 || amount > Mana) throw new InvalidOperationException("\u4ee5\u592a\u4e0d\u8db3\u3002"); Mana -= amount; }
        internal void RestoreMana(int amount) => Mana = Math.Min(MaxMana, Mana + amount);
        internal void SetInitiativeTime(int time) => InitiativeTime = time;
        public void ApplyStatus(StatusType type, int duration) { if (duration > 0) statuses[type] = Math.Max(StatusDuration(type), duration); }
        internal void ClearStatus(StatusType type) => statuses.Remove(type);
        internal void SetCooldown(SkillDefinition skill) { if (skill != null && skill.Cooldown > 0) cooldowns[skill.Id] = skill.Cooldown; }
        internal void TickTurnEffects()
        {
            if (HasStatus(StatusType.Burning)) TakeDamage(2);
            Tick(statuses); Tick(cooldowns);
        }
        private static void Tick(Dictionary<StatusType, int> values)
        {
            List<StatusType> remove = new List<StatusType>();
            List<StatusType> keys = new List<StatusType>(values.Keys);
            foreach (StatusType key in keys) { int next = values[key] - 1; if (next <= 0) remove.Add(key); else values[key] = next; }
            foreach (StatusType key in remove) values.Remove(key);
        }
        private static void Tick(Dictionary<string, int> values)
        {
            List<string> remove = new List<string>();
            List<string> keys = new List<string>(values.Keys);
            foreach (string key in keys) { int next = values[key] - 1; if (next <= 0) remove.Add(key); else values[key] = next; }
            foreach (string key in remove) values.Remove(key);
        }
        internal UnitState Clone()
        {
            UnitState clone = new UnitState(Id, IsHero, Position, Facing) { DisplayName = DisplayName, Armor = Armor, Shield = Shield, MaxShield = MaxShield, Block = Block, Speed = Speed, MainHand = MainHand, OffHand = OffHand, SkillOne = SkillOne, SkillTwo = SkillTwo };
            clone.Health = Health; clone.Mana = Mana; clone.ActionPoints = ActionPoints; clone.InitiativeTime = InitiativeTime;
            foreach (KeyValuePair<StatusType, int> entry in statuses) clone.statuses[entry.Key] = entry.Value;
            foreach (KeyValuePair<string, int> entry in cooldowns) clone.cooldowns[entry.Key] = entry.Value;
            return clone;
        }
    }
}
