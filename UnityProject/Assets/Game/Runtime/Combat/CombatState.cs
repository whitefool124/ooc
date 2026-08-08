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
        public InventoryGrid Backpack { get; private set; } = new InventoryGrid(InventoryContainerState.BaseWidth, InventoryContainerState.BaseHeight);
        public InventoryContainerState ItemInventory { get; private set; } = new InventoryContainerState();
        public LootContainer Loot { get; private set; }
        public LootSourceState LootSource { get; private set; }
        public ConsumableDefinition[] Quickbar { get; } = new ConsumableDefinition[8];
        public string[] ItemQuickbar { get; } = new string[8];
        public IReadOnlyList<CombatObjective> Objectives { get; private set; }
        internal ArtifactBattleState ArtifactBattle { get; private set; }

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
            ItemInventory.AddFirstFit(new ItemInstance("combat-medkit", "medkit", 0));
            ItemInventory.AddFirstFit(new ItemInstance("combat-shield-cell", "shield_cell", 1));
            ItemQuickbar[0] = "combat-medkit"; ItemQuickbar[1] = "combat-shield-cell";
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
        internal void AttachArtifactBattle(ArtifactBattleState battle) => ArtifactBattle = battle;
        public void SetLootSource(LootSourceState loot) => LootSource = loot;
        public void ConfigureItemInventory(InventoryContainerState inventory, IEnumerable<string> quickbarIds)
        {
            ItemInventory = (inventory ?? throw new ArgumentNullException(nameof(inventory))).Clone(); Array.Clear(ItemQuickbar, 0, ItemQuickbar.Length);
            if (quickbarIds == null) return; int index = 0; foreach (string id in quickbarIds.Take(ItemQuickbar.Length)) { if (ItemInventory.Get(id) != null) ItemQuickbar[index] = id; index++; }
        }
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
        public InventoryResult EquipItemQuickbar(string instanceId, int index)
        {
            if (index < 0 || index >= ItemQuickbar.Length) return new InventoryResult(InventoryError.OutOfBounds, instanceId);
            ItemInstance item = ItemInventory.Get(instanceId); if (item == null) return new InventoryResult(InventoryError.MissingInstance, instanceId);
            ItemDefinition definition = ItemCatalog.Get(item.DefinitionId); if (!definition.CanQuickEquip) return new InventoryResult(InventoryError.Restricted, instanceId);
            if ((definition.Category == ItemCategory.Scroll || definition.Category == ItemCategory.Artifact) && ItemQuickbar.Where(id => !string.IsNullOrEmpty(id)).Select(id => ItemInventory.Get(id)).Where(value => value != null).Count(value =>
            {
                ItemCategory category = ItemCatalog.Get(value.DefinitionId).Category; return category == ItemCategory.Scroll || category == ItemCategory.Artifact;
            }) >= 4 && ItemQuickbar[index] != instanceId) return new InventoryResult(InventoryError.QuickbarFull, instanceId);
            for (int i = 0; i < ItemQuickbar.Length; i++) if (ItemQuickbar[i] == instanceId) ItemQuickbar[i] = null;
            ItemQuickbar[index] = instanceId; return InventoryResult.Ok(instanceId, index, 0);
        }
        public bool ConsumeInventoryItem(string instanceId, int amount = 1)
        {
            ItemInstance item = ItemInventory.Get(instanceId); if (item == null || !item.TryConsume(amount)) return false;
            if (!item.IsDepleted) return true;
            ItemInventory.Remove(instanceId); for (int i = 0; i < ItemQuickbar.Length; i++) if (ItemQuickbar[i] == instanceId) ItemQuickbar[i] = null; return true;
        }

        internal void SetActiveUnit(string unitId) => ActiveUnitId = unitId;
        internal void SetCurrentTime(int time) => CurrentTime = time;
        public void AddLog(string message) { EventLog.Insert(0, message); if (EventLog.Count > 8) EventLog.RemoveAt(8); }
        internal void EvaluateOutcome()
        {
            IsDefeat = !units.Values.Any(unit => unit.IsHero && unit.IsAlive);
            IsVictory = Objectives != null && Objectives.Count > 0 && Objectives.All(objective => objective.IsComplete(this));
        }
        public void ResolveDebugOutcome(bool victory)
        {
            foreach (UnitState unit in units.Values)
            {
                if (victory && !unit.IsHero) unit.TakeDamage(int.MaxValue);
                if (!victory && unit.IsHero) unit.TakeDamage(int.MaxValue);
            }
            if (victory)
            {
                foreach (DestructionObjective objective in Objectives.OfType<DestructionObjective>())
                    foreach (GridPosition position in objective.Positions) Map.GetTile(position).Durability = 0;
            }
            EvaluateOutcome();
        }
        public CombatState Clone()
        {
            CombatState clone = new CombatState(Map.Clone(), units.Values.Select(unit => unit.Clone()), Objectives.Select(objective => objective.Clone()));
            clone.ActiveUnitId = ActiveUnitId; clone.CurrentTime = CurrentTime; clone.IsVictory = IsVictory; clone.IsDefeat = IsDefeat;
            clone.Backpack = Backpack.Clone(); clone.ItemInventory = ItemInventory.Clone(); clone.Loot = Loot?.Clone(); clone.LootSource = LootSource?.Clone(); Array.Copy(Quickbar, clone.Quickbar, Quickbar.Length); Array.Copy(ItemQuickbar, clone.ItemQuickbar, ItemQuickbar.Length);
            foreach (GridPosition position in investigated) clone.investigated.Add(position);
            clone.EventLog.AddRange(EventLog); return clone;
        }
    }
}
