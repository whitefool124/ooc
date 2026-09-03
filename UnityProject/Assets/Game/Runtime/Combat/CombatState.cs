using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public enum CombatRuleset { LegacyStory, Roguelite }

    public sealed class CombatState
    {
        private readonly Dictionary<string, UnitState> units;
        private readonly HashSet<GridPosition> investigated = new HashSet<GridPosition>();
        private readonly Dictionary<string, int> rogueTurnSequences = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> rogueShieldSourceTurns = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<string> rogueBreakStanceSeenThisTurn = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<Roguelite.ShieldSourceRecord> rogueShieldEvents = new List<Roguelite.ShieldSourceRecord>();

        public GridMap Map { get; }
        public string ActiveUnitId { get; private set; }
        public int CurrentTime { get; private set; }
        public bool IsVictory { get; private set; }
        public bool IsDefeat { get; private set; }
        public CombatRuleset Ruleset { get; private set; } = CombatRuleset.LegacyStory;
        public List<string> EventLog { get; } = new List<string>();
        public IReadOnlyDictionary<string, UnitState> Units => units;
        public InventoryGrid Backpack { get; private set; } = new InventoryGrid(InventoryContainerState.BaseWidth, InventoryContainerState.BaseHeight);
        public InventoryContainerState ItemInventory { get; private set; } = new InventoryContainerState();
        public LootContainer Loot { get; private set; }
        public LootSourceState LootSource { get; private set; }
        public string[] ItemQuickbar { get; } = new string[8];
        public IReadOnlyList<CombatObjective> Objectives { get; private set; }
        internal ArtifactBattleState ArtifactBattle { get; private set; }
        public Roguelite.RogueSpellCombatRuntime RogueSpells { get; private set; }
        public Roguelite.RogueEquipmentRuntime RogueEquipment { get; private set; }
        public IReadOnlyList<Roguelite.ShieldSourceRecord> RogueShieldEvents => rogueShieldEvents;

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
        public void ConfigureRuleset(CombatRuleset ruleset)
        {
            Ruleset = ruleset;
            if (ruleset == CombatRuleset.Roguelite)
                foreach (UnitState unit in units.Values) unit.ClearShield();
        }
        internal void BeginRogueliteTurn(UnitState unit)
        {
            if (Ruleset != CombatRuleset.Roguelite || unit == null) return;
            if (unit.Shield > 0)
                AddRogueShieldEvent(new Roguelite.ShieldSourceRecord(unit.Id + ":turn_start", unit.Shield,
                    Roguelite.ShieldEventKind.ClearedAtTurnStart, RogueTurn(unit.Id) + 1));
            unit.ClearShield();
            int turn = rogueTurnSequences.TryGetValue(unit.Id, out int current) ? current + 1 : 1;
            rogueTurnSequences[unit.Id] = turn;
            if (unit.HasStatus(StatusType.BreakStance)) rogueBreakStanceSeenThisTurn.Add(unit.Id);

            RogueEquipment?.OnTurnStart(this, unit.Id);

            int coverShield = 0;
            TileState standing = Map.GetTile(unit.Position);
            if (standing.Cover == CoverType.Light && !standing.IsDestroyed) coverShield = 2;
            GridPosition front = unit.Position + FacingOffset(unit.Facing);
            if (Map.IsInside(front))
            {
                TileState frontTile = Map.GetTile(front);
                if (frontTile.Cover == CoverType.Heavy && !frontTile.IsDestroyed) coverShield = Math.Max(coverShield, 4);
            }
            if (coverShield > 0) TryGrantRogueliteShield(unit.Id, coverShield == 4 ? "cover-heavy" : "cover-light", coverShield);
        }
        internal void EndRogueliteTurn(UnitState unit)
        {
            if (Ruleset != CombatRuleset.Roguelite || unit == null || !rogueBreakStanceSeenThisTurn.Remove(unit.Id)) return;
            unit.ClearStatus(StatusType.BreakStance);
        }
        public bool TryGrantRogueliteShield(string unitId, string sourceId, int amount)
        {
            if (Ruleset != CombatRuleset.Roguelite || amount <= 0 || string.IsNullOrWhiteSpace(sourceId)) return false;
            UnitState unit = GetUnit(unitId);
            if (unit == null || !unit.IsAlive) return false;
            if (unit.HasStatus(StatusType.BreakStance))
            {
                AddRogueShieldEvent(new Roguelite.ShieldSourceRecord(sourceId, amount,
                    Roguelite.ShieldEventKind.PreventedByBreakStance, RogueTurn(unit.Id)));
                AddLog(unit.DisplayName + "的" + sourceId + "护盾被破势阻止。");
                return false;
            }
            int turn = rogueTurnSequences.TryGetValue(unit.Id, out int current) ? current : 0;
            string key = unit.Id + "|" + sourceId;
            if (rogueShieldSourceTurns.TryGetValue(key, out int claimed) && claimed == turn) return false;
            rogueShieldSourceTurns[key] = turn; unit.GrantShield(amount);
            AddRogueShieldEvent(new Roguelite.ShieldSourceRecord(sourceId, amount,
                Roguelite.ShieldEventKind.Granted, turn));
            AddLog(unit.DisplayName + "从" + sourceId + "获得 " + amount + " 护盾。");
            return true;
        }
        internal void RecordRogueliteShieldAbsorption(string unitId, string sourceId, int amount)
        {
            if (Ruleset != CombatRuleset.Roguelite || amount <= 0) return;
            AddRogueShieldEvent(new Roguelite.ShieldSourceRecord(
                string.IsNullOrWhiteSpace(sourceId) ? "damage" : sourceId, amount,
                Roguelite.ShieldEventKind.Absorbed, RogueTurn(unitId)));
        }
        public void ApplyRogueliteBreakStance(string unitId)
        {
            if (Ruleset != CombatRuleset.Roguelite) throw new InvalidOperationException("Break stance is only valid in roguelite combat.");
            UnitState unit = GetUnit(unitId) ?? throw new InvalidOperationException("Unit does not exist.");
            if (unit.Shield > 0)
                AddRogueShieldEvent(new Roguelite.ShieldSourceRecord("break_stance", unit.Shield,
                    Roguelite.ShieldEventKind.Wasted, RogueTurn(unit.Id)));
            unit.ClearShield(); unit.ApplyStatus(StatusType.BreakStance, 1); rogueBreakStanceSeenThisTurn.Remove(unit.Id);
            AddLog(unit.DisplayName + "进入破势：当前护盾清除，至下一次自己回合结束前无法获得护盾。");
        }
        private int RogueTurn(string unitId) => rogueTurnSequences.TryGetValue(unitId, out int turn) ? turn : 0;
        private void AddRogueShieldEvent(Roguelite.ShieldSourceRecord record)
        {
            rogueShieldEvents.Insert(0, record);
            if (rogueShieldEvents.Count > 16) rogueShieldEvents.RemoveAt(rogueShieldEvents.Count - 1);
        }
        private static GridPosition FacingOffset(Facing facing)
        {
            if (facing == Facing.North) return new GridPosition(0, 1);
            if (facing == Facing.South) return new GridPosition(0, -1);
            if (facing == Facing.West) return new GridPosition(-1, 0);
            return new GridPosition(1, 0);
        }
        internal void MarkInvestigated(GridPosition position) => investigated.Add(position);

        public UnitState GetUnit(string unitId) =>
            string.IsNullOrEmpty(unitId) ? null : units.TryGetValue(unitId, out UnitState unit) ? unit : null;

        public bool IsOccupied(GridPosition position, string ignoredUnitId = null) =>
            units.Values.Any(unit => unit.Id != ignoredUnitId && unit.Position == position);

        public void SetLoot(LootContainer loot) => Loot = loot;
        internal void AttachArtifactBattle(ArtifactBattleState battle) => ArtifactBattle = battle;
        public void AttachRogueSpellRuntime(Roguelite.RogueSpellCombatRuntime runtime)
        {
            if (runtime == null || runtime.Combat != this) throw new ArgumentException("Rogue spell runtime must belong to this combat.", nameof(runtime));
            RogueSpells = runtime;
        }
        public void AttachRogueEquipmentRuntime(Roguelite.RogueEquipmentRuntime runtime)
        { RogueEquipment = runtime ?? throw new ArgumentNullException(nameof(runtime)); }
        public void SetLootSource(LootSourceState loot) => LootSource = loot;
        public void ConfigureItemInventory(InventoryContainerState inventory, IEnumerable<string> quickbarIds)
        {
            ItemInventory = (inventory ?? throw new ArgumentNullException(nameof(inventory))).Clone(); Array.Clear(ItemQuickbar, 0, ItemQuickbar.Length);
            if (quickbarIds == null) return; int index = 0; foreach (string id in quickbarIds.Take(ItemQuickbar.Length)) { if (ItemInventory.Get(id) != null) ItemQuickbar[index] = id; index++; }
        }
        public InventoryResult EquipItemQuickbar(string instanceId, int index)
        {
            if (index < 0 || index >= ItemQuickbar.Length) return new InventoryResult(InventoryError.OutOfBounds, instanceId);
            ItemInstance item = ItemInventory.Get(instanceId); if (item == null) return new InventoryResult(InventoryError.MissingInstance, instanceId);
            ItemDefinition definition = ItemCatalog.Get(item.DefinitionId); if (!definition.CanQuickEquip) return new InventoryResult(InventoryError.Restricted, instanceId);
            string replacedId = ItemQuickbar[index];
            if ((definition.Category == ItemCategory.Scroll || definition.Category == ItemCategory.Artifact) && ItemQuickbar.Where(id => !string.IsNullOrEmpty(id) && id != replacedId && id != instanceId).Select(id => ItemInventory.Get(id)).Where(value => value != null).Count(value =>
            {
                ItemCategory category = ItemCatalog.Get(value.DefinitionId).Category; return category == ItemCategory.Scroll || category == ItemCategory.Artifact;
            }) >= 4) return new InventoryResult(InventoryError.QuickbarFull, instanceId);
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
            clone.ActiveUnitId = ActiveUnitId; clone.CurrentTime = CurrentTime; clone.IsVictory = IsVictory; clone.IsDefeat = IsDefeat; clone.Ruleset = Ruleset;
            clone.Backpack = Backpack.Clone(); clone.ItemInventory = ItemInventory.Clone(); clone.Loot = Loot?.Clone(); clone.LootSource = LootSource?.Clone(); Array.Copy(ItemQuickbar, clone.ItemQuickbar, ItemQuickbar.Length);
            foreach (GridPosition position in investigated) clone.investigated.Add(position);
            foreach (KeyValuePair<string, int> pair in rogueTurnSequences) clone.rogueTurnSequences[pair.Key] = pair.Value;
            foreach (KeyValuePair<string, int> pair in rogueShieldSourceTurns) clone.rogueShieldSourceTurns[pair.Key] = pair.Value;
            foreach (string unitId in rogueBreakStanceSeenThisTurn) clone.rogueBreakStanceSeenThisTurn.Add(unitId);
            clone.rogueShieldEvents.AddRange(rogueShieldEvents);
            clone.EventLog.AddRange(EventLog); return clone;
        }
    }
}
