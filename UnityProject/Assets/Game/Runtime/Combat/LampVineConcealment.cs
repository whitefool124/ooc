using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    // Shared visibility/search owner. A search is a normal weapon/skill attack at a public cell.
    public sealed class LampVineConcealment
    {
        private sealed class Record
        {
            public GridPosition Known, Candidate;
            public HashSet<GridPosition> Region = new HashSet<GridPosition>();
            public HashSet<GridPosition> Searched = new HashSet<GridPosition>();
            public Record Clone() => new Record { Known = Known, Candidate = Candidate, Region = new HashSet<GridPosition>(Region), Searched = new HashSet<GridPosition>(Searched) };
        }
        private readonly Dictionary<string, Record> records = new Dictionary<string, Record>(StringComparer.Ordinal);
        private readonly int seed;
        public LampVineConcealment(int seed = 0) { this.seed = seed; }
        public static bool Intact(CombatState state, GridPosition cell) => state.Map.IsInside(cell) && state.Map.GetTile(cell).IsLampVine && state.Map.GetTile(cell).Durability > 0;
        public bool IsHiddenFrom(CombatState state, UnitState observer, UnitState target)
            => state.Ruleset == CombatRuleset.Roguelite && observer != null && target != null && observer.IsHero != target.IsHero && target.IsAlive && records.ContainsKey(target.Id) && Intact(state, target.Position) && !target.HasStatus(StatusType.Revealed);
        public IReadOnlyList<GridPosition> KnownCells(CombatState state, UnitState observer)
            => records.Where(r => IsHiddenFrom(state, observer, state.GetUnit(r.Key))).Select(r => r.Value.Candidate).Distinct().ToArray();
        public GridPosition PublicCell(string unitId) => records.TryGetValue(unitId, out var record) ? record.Candidate : throw new InvalidOperationException("No concealed-unit record.");
        internal void Reveal(string unitId) { records.Remove(unitId); }
        internal void AfterMove(CombatState state, UnitState unit, IReadOnlyList<GridPosition> path)
        {
            if (state.Ruleset != CombatRuleset.Roguelite || unit == null || path == null || path.Count < 2) return;
            for (int i = 1; i < path.Count; i++)
            {
                if (!Intact(state, path[i])) { records.Remove(unit.Id); continue; }
                if (!Intact(state, path[i - 1]))
                {
                    var record = new Record { Known = path[i], Candidate = path[i] }; var queue = new Queue<GridPosition>(); queue.Enqueue(path[i]); record.Region.Add(path[i]);
                    while (queue.Count > 0) foreach (var adjacent in Neighbors(queue.Dequeue()))
                        if (Intact(state, adjacent) && record.Region.Add(adjacent)) queue.Enqueue(adjacent);
                    records[unit.Id] = record;
                }
            }
            Refresh(state);
        }
        internal void Refresh(CombatState state)
        {
            foreach (string id in records.Keys.ToArray())
            {
                var unit = state.GetUnit(id);
                if (unit == null || !unit.IsAlive || !Intact(state, unit.Position) || unit.HasStatus(StatusType.Revealed)) records.Remove(id);
            }
        }
        internal void AfterCellDamaged(CombatState state, GridPosition cell)
        {
            Refresh(state);
            if (Intact(state, cell)) return;
            foreach (var pair in records.ToArray())
            {
                var record = pair.Value;
                if (!record.Region.Contains(cell)) continue;
                record.Searched.Add(cell);
                var frontier = record.Searched.SelectMany(Neighbors).Where(p => record.Region.Contains(p) && Intact(state, p) && !record.Searched.Contains(p)).Distinct().ToArray();
                if (frontier.Length == 0) { records.Remove(pair.Key); continue; }
                record.Candidate = frontier.OrderBy(p => Stable(seed + ":" + pair.Key + ":" + record.Searched.Count + ":" + p.X + ":" + p.Y)).ThenBy(p => p.X).ThenBy(p => p.Y).First();
            }
        }
        private static uint Stable(string value) { unchecked { uint hash = 2166136261; foreach (char c in value) hash = (hash ^ c) * 16777619; return hash; } }
        private static IEnumerable<GridPosition> Neighbors(GridPosition p)
        { yield return p + new GridPosition(0, 1); yield return p + new GridPosition(1, 0); yield return p + new GridPosition(0, -1); yield return p + new GridPosition(-1, 0); }
        public string AttackFailure(CombatState state, UnitState source, GridPosition cell, int skillSlot = -1)
        {
            if (source == null || !source.IsAlive || source.ActionPoints < 1) return "行动点不足";
            if (!Intact(state, cell)) return "请选择完整灯藤格";
            SkillDefinition skill = skillSlot == 0 ? source.SkillOne : skillSlot == 1 ? source.SkillTwo : null;
            IAttackDefinition attack = AttackDefinition(source, skill);
            if (attack == null || skillSlot >= 0 && (skill == null || !source.IsSkillReady(skill))) return "动作不可用或冷却中";
            int distance = source.Position.ManhattanDistance(cell);
            if (Intact(state, source.Position) && distance > 1) return "藤内只能攻击正交相邻目标";
            if (distance < attack.MinimumRange || distance > source.EffectiveRange(attack.Range)) return "目标超出当前动作射程";
            if (distance > 1 && !state.Map.HasLineOfSight(source.Position, cell, state.CurrentTime, true)) return "攻击线被阻挡";
            if (source.Mana < attack.ManaCost) return "个人魔力不足";
            return "";
        }
        public CombatCommand PlanSearch(CombatState state, UnitState source, UnitState concealed)
        {
            var cell = PublicCell(concealed.Id);
            if (source.EnemyArchetypeId == "breach_ram" && state.ThreeMaterialPressure?.ChargeCooldown == 0) return CombatCommand.BreachCharge(source.Id, cell);
            int skillSlot = source.EnemyArchetypeId == "pyromancer" && source.SkillOne?.Damage > 0 ? 0 : -1;
            if (AttackFailure(state, source, cell, skillSlot) == "") return CombatCommand.AttackCell(source.Id, cell, skillSlot);
            if (skillSlot >= 0 && AttackFailure(state, source, cell) == "") return CombatCommand.AttackCell(source.Id, cell);
            if (source.HasStatus(StatusType.Bound)) return CombatCommand.EndTurn(source.Id);
            var candidates = Neighbors(cell).Where(p => state.Map.IsInside(p) && !state.Map.IsBlocked(p) && !state.IsOccupied(p, source.Id))
                .Select(p => state.Map.FindLowestCostPath(source.Position, p, state.Map.Width * state.Map.Height * 2,
                    c => CombatMovementQuery.EntryCost(state, source, c), c => state.IsOccupied(c, source.Id))).Where(p => p.Count > 1).OrderBy(p => p.Count).ThenBy(p => p.Last().X).ThenBy(p => p.Last().Y).ToArray();
            if (candidates.Length == 0) return CombatCommand.EndTurn(source.Id);
            int spent = 0; var end = source.Position;
            foreach (var step in candidates[0].Skip(1)) { spent += CombatMovementQuery.EntryCost(state, source, step); if (spent > CombatMovementQuery.Budget(state, source)) break; end = step; }
            return end == source.Position ? CombatCommand.EndTurn(source.Id) : CombatCommand.Move(source.Id, end);
        }
        internal CombatEffectExecution Attack(CombatState state, UnitState source, GridPosition cell, int skillSlot)
        {
            string failure = AttackFailure(state, source, cell, skillSlot); if (failure != "") throw new InvalidOperationException(failure);
            SkillDefinition skill = skillSlot == 0 ? source.SkillOne : skillSlot == 1 ? source.SkillTwo : null;
            IAttackDefinition attack = AttackDefinition(source, skill);
            // Capture occupants before destroying their cover reveals them. One packet supplies both results.
            var occupants = state.Units.Values.Where(u => u.IsAlive && u.Position == cell).ToArray();
            int raw = attack.Damage + (skill == null && source.IsHero ? state.RogueEquipment?.ForgeWeaponDamageBonus ?? 0 : 0);
            int damage = CombatDebugTuning.OutgoingDamageFor(source, raw, attack.DamageType);
            var effects = new List<CombatEffect> { CombatEffect.SpendActionPoints(1) };
            if (attack.ManaCost > 0) effects.Add(CombatEffect.SpendMana(attack.ManaCost));
            effects.Add(CombatEffect.DamageObject(cell, damage));
            foreach (var target in occupants)
            {
                var kind = attack.DamageType == DamageType.Fire ? Roguelite.DamageComponentKind.Fire : attack.DamageType == DamageType.Arcane ? Roguelite.DamageComponentKind.Aether : Roguelite.DamageComponentKind.Physical;
                var packet = new Roguelite.DamagePacket("vine-search-" + attack.Id, source.Id, target.Id, attack.Id,
                    new[] { new Roguelite.DamageComponent(kind, Math.Max(0, damage + target.StatusStrength(StatusType.DamageTaken))) });
                var resolution = Roguelite.RogueDamageResolver.Resolve(packet, target.Shield, target.Health);
                effects.Add(CombatEffect.AbsorbShield(target.Id, resolution.ShieldAbsorbed)); effects.Add(CombatEffect.DamageHealth(target.Id, resolution.HealthDamage));
                if (target.Health > resolution.HealthDamage && skill != null) foreach (var effect in skill.Effects.Where(e => e.Type == SkillEffectType.ApplyStatus))
                    effects.Add(CombatEffect.ApplyStatus(target.Id, effect.Status, effect.Duration, effect.Amount));
            }
            var execution = CombatEffectExecutor.Execute(state, source.Id, effects.ToArray());
            foreach (var target in occupants)
            {
                records.Remove(target.Id);
                if (skill == null) { state.RogueEquipment?.AfterWeaponHit(state, source.Id); state.RogueSpells?.AfterWeaponHit(source.Id, target); }
                execution = CombatEffectExecution.Combine(execution, CombatResolver.ResolveAdjacentCounter(state, target, source));
            }
            if (skill != null) source.SetCooldown(skill);
            state.AddLog(source.DisplayName + "以" + attack.DisplayName + "搜查公开灯藤格；沿用原动作费用与伤害。"); state.EvaluateOutcome(); return execution;
        }
        internal LampVineConcealment Clone()
        { var copy = new LampVineConcealment(seed); foreach (var pair in records) copy.records[pair.Key] = pair.Value.Clone(); return copy; }
        private static IAttackDefinition AttackDefinition(UnitState source, SkillDefinition skill)
            => skill == null ? source.BasicAttack : new EnemyAttackDefinition(skill.Id, skill.DisplayName, skill.DamageType, skill.Damage, skill.Range,
                initiativeDelay: skill.InitiativeDelay, manaCost: skill.ManaCost, minimumRange: skill.MinimumRange);
    }
}
