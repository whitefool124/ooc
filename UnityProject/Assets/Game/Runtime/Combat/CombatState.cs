using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public sealed class CombatState
    {
        private readonly Dictionary<string, UnitState> units;
        private readonly HashSet<GridPosition> investigated = new HashSet<GridPosition>();

        public GridMap Map { get; }
        public string ActiveUnitId { get; private set; }
        public int CurrentTime { get; private set; }
        public bool IsVictory { get; private set; }
        public bool IsDefeat { get; private set; }
        public List<string> EventLog { get; } = new List<string>();
        public IReadOnlyDictionary<string, UnitState> Units => units;
        public InventoryGrid Backpack { get; private set; } = new InventoryGrid(4, 3);
        public LootContainer Loot { get; private set; }
        public ConsumableDefinition[] Quickbar { get; } = new ConsumableDefinition[8];
        public IReadOnlyList<CombatObjective> Objectives { get; private set; }

        public CombatState(GridMap map, IEnumerable<UnitState> units, IEnumerable<CombatObjective> objectives = null)
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));
            this.units = (units ?? throw new ArgumentNullException(nameof(units)))
                .ToDictionary(unit => unit.Id, StringComparer.Ordinal);

            if (this.units.Count == 0)
            {
                throw new ArgumentException("Combat requires at least one unit.", nameof(units));
            }

            foreach (UnitState unit in this.units.Values)
            {
                if (!Map.IsInside(unit.Position) || Map.IsBlocked(unit.Position))
                {
                    throw new ArgumentException($"Unit {unit.Id} has an invalid starting position.", nameof(units));
                }
            }

            if (this.units.Values.Select(unit => unit.Position).Distinct().Count() != this.units.Count)
            {
                throw new ArgumentException("Multiple units cannot occupy the same starting position.", nameof(units));
            }
            if (objectives != null) Objectives = objectives.ToList();
            else
            {
                GridPosition[] objectivePositions = Map.PositionsWith(tile => tile.IsObjective).ToArray();
                Objectives = objectivePositions.Length == 0 ? new List<CombatObjective>() : new List<CombatObjective> { new DestructionObjective(objectivePositions) };
            }
        }

        public void ConfigureObjectives(params CombatObjective[] objectives)
        { if (objectives == null || objectives.Length == 0) throw new ArgumentException("At least one objective is required.", nameof(objectives)); Objectives = objectives.ToList(); EvaluateOutcome(); }
        public bool IsInvestigated(GridPosition position) => investigated.Contains(position);
        internal void MarkInvestigated(GridPosition position) => investigated.Add(position);

        public UnitState GetUnit(string unitId) =>
            units.TryGetValue(unitId, out UnitState unit) ? unit : null;

        public bool IsOccupied(GridPosition position, string ignoredUnitId = null) =>
            units.Values.Any(unit => unit.Id != ignoredUnitId && unit.Position == position);

        public void SetLoot(LootContainer loot) => Loot = loot;
        public void ConfigureQuickbar(params ConsumableDefinition[] items)
        {
            Array.Clear(Quickbar, 0, Quickbar.Length);
            if (items == null) return;
            for (int i = 0; i < Math.Min(Quickbar.Length, items.Length); i++) Quickbar[i] = items[i];
        }
        internal void ClearQuickbarSlot(int index)
        {
            if (index >= 0 && index < Quickbar.Length) Quickbar[index] = null;
        }

        internal void SetActiveUnit(string unitId) => ActiveUnitId = unitId;
        internal void SetCurrentTime(int time) => CurrentTime = time;
        public void AddLog(string message) { EventLog.Insert(0, message); if (EventLog.Count > 8) EventLog.RemoveAt(8); }
        internal void EvaluateOutcome()
        {
            IsDefeat = !units.Values.Any(unit => unit.IsHero && unit.IsAlive);
            IsVictory = Objectives != null && Objectives.Count > 0 && Objectives.All(objective => objective.IsComplete(this));
        }
        public CombatState Clone()
        {
            CombatState clone = new CombatState(Map.Clone(), units.Values.Select(unit => unit.Clone()), Objectives.Select(objective => objective.Clone()));
            clone.ActiveUnitId = ActiveUnitId; clone.CurrentTime = CurrentTime; clone.IsVictory = IsVictory; clone.IsDefeat = IsDefeat;
            clone.Backpack = Backpack.Clone(); clone.Loot = Loot?.Clone(); Array.Copy(Quickbar, clone.Quickbar, Quickbar.Length);
            foreach (GridPosition position in investigated) clone.investigated.Add(position);
            clone.EventLog.AddRange(EventLog); return clone;
        }
    }
}
