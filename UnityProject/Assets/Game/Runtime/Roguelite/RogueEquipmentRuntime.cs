using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat.Roguelite
{
    public sealed class RogueEquipmentInstance
    {
        public string InstanceId { get; }
        public string DefinitionId { get; }
        public EquipmentRarity Rarity { get; }
        public int PowerBand { get; }
        public List<string> MutableAffixIds { get; } = new List<string>();
        public List<string> UpgradeBranchIds { get; } = new List<string>();
        public int ReforgeCount { get; internal set; }
        public string SourceStage { get; }
        public string SourceType { get; }
        public int AcquiredOrder { get; }

        public RogueEquipmentInstance(string instanceId, string definitionId, EquipmentRarity rarity, int powerBand,
            string sourceStage, string sourceType, int acquiredOrder)
        { InstanceId = instanceId; DefinitionId = definitionId; Rarity = rarity; PowerBand = powerBand; SourceStage = sourceStage; SourceType = sourceType; AcquiredOrder = acquiredOrder; }
    }

    public sealed class RogueTacticalItemInstance
    {
        public string InstanceId { get; }
        public string DefinitionId { get; }
        public int ChargesCurrent { get; private set; }
        public int ChargesMaximum { get; }
        public int AcquiredOrder { get; }
        public string SourceType { get; }
        public RogueTacticalItemInstance(string instanceId, string definitionId, int charges, int acquiredOrder, string sourceType)
        { InstanceId = instanceId; DefinitionId = definitionId; ChargesCurrent = charges; ChargesMaximum = charges; AcquiredOrder = acquiredOrder; SourceType = sourceType; }
        public bool Consume() { if (ChargesCurrent <= 0) return false; ChargesCurrent--; return true; }
        internal void RestoreCharges(int current) { ChargesCurrent = Math.Max(0, Math.Min(ChargesMaximum, current)); }
    }

    public readonly struct RogueBackpackPlacement
    {
        public int X { get; }
        public int Y { get; }
        public bool Rotated { get; }
        public RogueBackpackPlacement(int x, int y, bool rotated) { X = x; Y = y; Rotated = rotated; }
    }

    public sealed class RogueEquipmentRuntime
    {
        private readonly int seed;
        private readonly RogueContentCatalog catalog = RogueContentCatalog.CreateAcademyV01();
        private readonly Dictionary<string, RogueEquipmentInstance> equipment = new Dictionary<string, RogueEquipmentInstance>(StringComparer.Ordinal);
        private readonly Dictionary<string, RogueTacticalItemInstance> tactical = new Dictionary<string, RogueTacticalItemInstance>(StringComparer.Ordinal);
        private readonly Dictionary<string, RogueBackpackPlacement> backpack = new Dictionary<string, RogueBackpackPlacement>(StringComparer.Ordinal);
        private readonly Dictionary<EquipmentSlot, string> equipped = Enum.GetValues(typeof(EquipmentSlot)).Cast<EquipmentSlot>().ToDictionary(value => value, value => string.Empty);
        private readonly string[] quickbar = new string[RogueRuntimeConstants.ItemQuickbarSize];
        private readonly HashSet<string> facingLockedUnits = new HashSet<string>(StringComparer.Ordinal);

        public IReadOnlyDictionary<EquipmentSlot, string> Equipped => equipped;
        public IReadOnlyDictionary<string, RogueBackpackPlacement> Backpack => backpack;
        public string[] ItemQuickbarInstanceIds => (string[])quickbar.Clone();
        public IReadOnlyList<RogueEquipmentInstance> AllInstances => equipment.Values.OrderBy(value => value.AcquiredOrder).ToArray();
        public IReadOnlyList<RogueTacticalItemInstance> AllTacticalItems => tactical.Values.OrderBy(value => value.AcquiredOrder).ToArray();
        public RogueEquipmentInstance EquipmentItem(string instanceId) => string.IsNullOrEmpty(instanceId) || !equipment.TryGetValue(instanceId, out RogueEquipmentInstance value) ? null : value;
        public RogueTacticalItemInstance TacticalItem(string instanceId) => string.IsNullOrEmpty(instanceId) || !tactical.TryGetValue(instanceId, out RogueTacticalItemInstance value) ? null : value;
        public EquipmentDefinition DefinitionFor(string instanceId) => equipment.TryGetValue(instanceId, out RogueEquipmentInstance value) ? Definition(value) : null;
        public TacticalItemDefinition TacticalDefinitionFor(string instanceId) => tactical.TryGetValue(instanceId, out RogueTacticalItemInstance value) ? catalog.TacticalItems.Single(item => item.DefinitionId == value.DefinitionId) : null;

        public RogueEquipmentRuntime(int seed)
        {
            this.seed = seed;
            for (int index = 0; index < quickbar.Length; index++) quickbar[index] = string.Empty;
        }

        public static RogueEquipmentRuntime CreateStarter(int seed)
        {
            RogueEquipmentRuntime runtime = new RogueEquipmentRuntime(seed);
            RogueEquipmentInstance chest = runtime.CreateInstance("starter-chest", "ACA-EQ-CH01", EquipmentRarity.Common, 0, "starter");
            RogueEquipmentInstance shield = runtime.CreateInstance("starter-shield", "ACA-EQ-OH01", EquipmentRarity.Common, 1, "starter");
            runtime.AddToBackpack(chest); runtime.AddToBackpack(shield);
            runtime.Equip(chest.InstanceId, EquipmentSlot.Chest); runtime.Equip(shield.InstanceId, EquipmentSlot.OffHand);
            return runtime;
        }

        public static RogueEquipmentRuntime FromDto(RogueRunDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.EquipmentInstances.Count == 0 && dto.TacticalItemInstances.Count == 0) return CreateStarter(dto.Seed);
            RogueEquipmentRuntime runtime = new RogueEquipmentRuntime(dto.Seed);
            foreach (EquipmentInstanceDto saved in dto.EquipmentInstances.OrderBy(value => value.AcquiredOrder))
            {
                RogueEquipmentInstance instance = runtime.CreateInstance(saved.InstanceId, saved.DefinitionId, saved.Rarity, saved.AcquiredOrder, saved.SourceType);
                instance.MutableAffixIds.AddRange(saved.MutableAffixIds); instance.UpgradeBranchIds.AddRange(saved.UpgradeBranchIds); instance.ReforgeCount = saved.ReforgeCount;
                if (saved.BackpackX >= 0 && saved.BackpackY >= 0) runtime.backpack[instance.InstanceId] = new RogueBackpackPlacement(saved.BackpackX, saved.BackpackY, saved.BackpackRotated);
                else if (!runtime.AddToBackpack(instance)) throw new InvalidOperationException("Saved equipment has no legal backpack position: " + instance.InstanceId);
            }
            foreach (KeyValuePair<EquipmentSlot, string> slot in dto.EquipmentSlotInstanceIds.Where(value => !string.IsNullOrEmpty(value.Value))) runtime.Equip(slot.Value, slot.Key);
            foreach (TacticalItemInstanceDto saved in dto.TacticalItemInstances)
            {
                RogueTacticalItemInstance item = runtime.CreateTacticalItem(saved.InstanceId, saved.DefinitionId, 0, saved.SourceType); item.RestoreCharges(saved.ChargesCurrent);
                runtime.backpack[item.InstanceId] = new RogueBackpackPlacement(saved.X, saved.Y, saved.Rotated);
            }
            for (int index = 0; index < runtime.quickbar.Length; index++) runtime.AssignQuickbar(index, dto.ItemQuickbarInstanceIds[index]);
            return runtime;
        }

        public void WriteToDto(RogueRunDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            dto.EquipmentInstances.Clear(); foreach (EquipmentSlot slot in equipped.Keys.ToArray()) dto.EquipmentSlotInstanceIds[slot] = equipped[slot];
            foreach (RogueEquipmentInstance instance in equipment.Values.OrderBy(value => value.AcquiredOrder))
            {
                EquipmentSlot slot = equipped.FirstOrDefault(value => value.Value == instance.InstanceId).Key;
                EquipmentInstanceDto saved = new EquipmentInstanceDto(instance.InstanceId, instance.DefinitionId, slot, instance.Rarity, instance.PowerBand)
                { ReforgeCount = instance.ReforgeCount, SourceStage = instance.SourceStage, SourceType = instance.SourceType, AcquiredOrder = instance.AcquiredOrder };
                if (backpack.TryGetValue(instance.InstanceId, out RogueBackpackPlacement placement)) { saved.BackpackX = placement.X; saved.BackpackY = placement.Y; saved.BackpackRotated = placement.Rotated; }
                saved.MutableAffixIds.AddRange(instance.MutableAffixIds); saved.UpgradeBranchIds.AddRange(instance.UpgradeBranchIds); dto.EquipmentInstances.Add(saved);
            }
            dto.TacticalItemInstances.Clear();
            foreach (RogueTacticalItemInstance item in tactical.Values.OrderBy(value => value.AcquiredOrder))
            {
                RogueBackpackPlacement placement = backpack.TryGetValue(item.InstanceId, out RogueBackpackPlacement found) ? found : default;
                dto.TacticalItemInstances.Add(new TacticalItemInstanceDto { InstanceId = item.InstanceId, DefinitionId = item.DefinitionId, X = placement.X, Y = placement.Y,
                    Rotated = placement.Rotated, ChargesCurrent = item.ChargesCurrent, ChargesMaximum = item.ChargesMaximum, SourceStage = "academy", SourceType = item.SourceType });
            }
            Array.Copy(quickbar, dto.ItemQuickbarInstanceIds, quickbar.Length);
        }

        public RogueEquipmentInstance CreateInstance(string instanceId, string definitionId, EquipmentRarity rarity, int acquiredOrder, string sourceType)
        {
            if (equipment.ContainsKey(instanceId) || tactical.ContainsKey(instanceId)) throw new InvalidOperationException("Duplicate instance id.");
            catalog.Equipment.Single(value => value.DefinitionId == definitionId);
            RogueEquipmentInstance value = new RogueEquipmentInstance(instanceId, definitionId, rarity, 0, "academy", sourceType, acquiredOrder);
            equipment.Add(instanceId, value); return value;
        }

        public RogueTacticalItemInstance CreateTacticalItem(string instanceId, string definitionId, int acquiredOrder, string sourceType)
        {
            TacticalItemDefinition definition = catalog.TacticalItems.Single(value => value.DefinitionId == definitionId);
            RogueTacticalItemInstance value = new RogueTacticalItemInstance(instanceId, definitionId, definition.MaximumCharges, acquiredOrder, sourceType);
            tactical.Add(instanceId, value); return value;
        }

        public bool AddToBackpack(RogueEquipmentInstance instance) => instance != null && equipment.ContainsKey(instance.InstanceId) && AddFirstFit(instance.InstanceId);
        public bool AddTacticalToBackpack(RogueTacticalItemInstance instance) => instance != null && tactical.ContainsKey(instance.InstanceId) && AddFirstFit(instance.InstanceId);

        public bool MoveBackpack(string instanceId, int x, int y, bool rotated)
        {
            if (!CanMoveBackpack(instanceId, x, y, rotated)) return false;
            backpack[instanceId] = new RogueBackpackPlacement(x, y, rotated); return true;
        }

        public bool CanMoveBackpack(string instanceId, int x, int y, bool rotated)
        {
            if (!backpack.ContainsKey(instanceId)) return false;
            Size(instanceId, rotated, out int width, out int height);
            return Fits(instanceId, x, y, rotated, width, height, instanceId);
        }

        public bool RotateBackpack(string instanceId)
        {
            if (!backpack.TryGetValue(instanceId, out RogueBackpackPlacement placement)) return false;
            return MoveBackpack(instanceId, placement.X, placement.Y, !placement.Rotated);
        }

        public bool Equip(string instanceId, EquipmentSlot slot)
        {
            if (!CanEquip(instanceId, slot, false, out RogueEquipmentInstance instance)) return false;
            backpack.Remove(instanceId); equipped[slot] = instanceId; return true;
        }

        public bool CanEquipOrReplace(string instanceId, EquipmentSlot slot)
        {
            if (!CanEquip(instanceId, slot, true, out _)) return false;
            string previous = equipped[slot];
            return string.IsNullOrEmpty(previous) || FindFirstFit(previous, instanceId).HasValue;
        }

        public bool EquipOrReplace(string instanceId, EquipmentSlot slot)
        {
            if (!CanEquip(instanceId, slot, true, out _)) return false;
            string previous = equipped[slot];
            if (string.IsNullOrEmpty(previous)) return Equip(instanceId, slot);
            RogueBackpackPlacement? previousPlacement = FindFirstFit(previous, instanceId);
            if (!previousPlacement.HasValue) return false;
            backpack.Remove(instanceId);
            backpack[previous] = previousPlacement.Value;
            equipped[slot] = instanceId;
            return true;
        }

        private bool CanEquip(string instanceId, EquipmentSlot slot, bool allowOccupied, out RogueEquipmentInstance instance)
        {
            instance = null;
            if (!equipment.TryGetValue(instanceId, out instance) || !backpack.ContainsKey(instanceId) || !equipped.ContainsKey(slot)) return false;
            if (!allowOccupied && !string.IsNullOrEmpty(equipped[slot])) return false;
            EquipmentDefinition definition = Definition(instance);
            bool accessory = definition.Slot == EquipmentSlot.Accessory1 && (slot == EquipmentSlot.Accessory1 || slot == EquipmentSlot.Accessory2);
            if (definition.Slot != slot && !accessory) return false;
            if (slot == EquipmentSlot.OffHand && IsTwoHandedMainEquipped()) return false;
            if (slot == EquipmentSlot.MainHand && definition.Handedness == EquipmentHandedness.TwoHanded && !string.IsNullOrEmpty(equipped[EquipmentSlot.OffHand])) return false;
            return true;
        }

        public bool Unequip(EquipmentSlot slot)
        {
            string instanceId = equipped[slot];
            if (string.IsNullOrEmpty(instanceId)) return false;
            RogueBackpackPlacement? placement = FindFirstFit(instanceId);
            if (!placement.HasValue) return false;
            backpack[instanceId] = placement.Value; equipped[slot] = string.Empty; return true;
        }

        public bool CanUnequipToBackpack(EquipmentSlot slot, int x, int y, bool rotated)
        {
            if (!equipped.TryGetValue(slot, out string instanceId) || string.IsNullOrEmpty(instanceId)) return false;
            Size(instanceId, rotated, out int width, out int height);
            return Fits(instanceId, x, y, rotated, width, height);
        }

        public bool UnequipToBackpack(EquipmentSlot slot, int x, int y, bool rotated)
        {
            if (!CanUnequipToBackpack(slot, x, y, rotated)) return false;
            string instanceId = equipped[slot];
            backpack[instanceId] = new RogueBackpackPlacement(x, y, rotated);
            equipped[slot] = string.Empty;
            return true;
        }

        public void OnTurnStart(CombatState combat, string unitId)
        {
            facingLockedUnits.Remove(unitId);
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)).Cast<EquipmentSlot>())
            {
                string instanceId = equipped[slot];
                if (string.IsNullOrEmpty(instanceId)) continue;
                RogueEquipmentInstance instance = equipment[instanceId]; EquipmentDefinition definition = Definition(instance);
                if (definition.TurnStartShield > 0) combat.TryGrantRogueliteShield(unitId, instance.InstanceId + ":fixed", definition.TurnStartShield);
                foreach (string affixId in instance.MutableAffixIds)
                {
                    AffixDefinition affix = catalog.Affixes.Single(value => value.AffixId == affixId);
                    if (affix.EffectId == "turn_start_shield:2") combat.TryGrantRogueliteShield(unitId, instance.InstanceId + ":" + affixId, 2);
                    if (affix.EffectId == "turn_start_shield:4") combat.TryGrantRogueliteShield(unitId, instance.InstanceId + ":" + affixId, 4);
                }
            }
        }

        public bool UseEquippedShield(CombatState combat, string unitId, Facing facing)
        {
            string instanceId = equipped[EquipmentSlot.OffHand];
            if (string.IsNullOrEmpty(instanceId)) return false;
            RogueEquipmentInstance instance = equipment[instanceId];
            if (instance.DefinitionId != "ACA-EQ-OH01" && instance.DefinitionId != "ACA-EQ-OH02") return false;
            UnitState unit = combat.GetUnit(unitId);
            if (unit == null || unit.ActionPoints < 1) return false;
            CombatEffectExecutor.Execute(combat, unitId, CombatEffect.SpendActionPoints(1));
            unit.TurnInPlace(facing);
            int shield = instance.Rarity == EquipmentRarity.Legendary ? 8 : instance.Rarity == EquipmentRarity.Rare ? 6 : 4;
            combat.TryGrantRogueliteShield(unitId, instance.InstanceId + ":raise", shield);
            facingLockedUnits.Add(unitId); return true;
        }

        public bool IsFacingLocked(string unitId) => facingLockedUnits.Contains(unitId);

        public bool AssignQuickbar(int slot, string instanceId)
        {
            if (slot < 0 || slot >= quickbar.Length) return false;
            if (string.IsNullOrEmpty(instanceId)) { quickbar[slot] = string.Empty; return true; }
            if (!tactical.ContainsKey(instanceId) || !backpack.ContainsKey(instanceId)) return false;
            for (int index = 0; index < quickbar.Length; index++) if (quickbar[index] == instanceId) quickbar[index] = string.Empty;
            quickbar[slot] = instanceId; return true;
        }

        public bool TryReforge(string instanceId, ref int gold)
        {
            if (!equipment.TryGetValue(instanceId, out RogueEquipmentInstance instance) || (instance.Rarity != EquipmentRarity.Rare && instance.Rarity != EquipmentRarity.Legendary)) return false;
            int cost = (instance.Rarity == EquipmentRarity.Rare ? 6 : 9) + instance.ReforgeCount * 2;
            if (gold < cost) return false;
            EquipmentDefinition definition = Definition(instance);
            bool fixedShield = definition.TurnStartShield > 0 || definition.FixedEffectIds.Any(value => value.Contains("shield"));
            AffixDefinition[] candidates = catalog.Affixes.Where(value => value.LegalSlots.Contains(definition.Slot) &&
                (!value.ExactRarity.HasValue || value.ExactRarity.Value == instance.Rarity) && value.MinimumRarity <= instance.Rarity &&
                (!fixedShield || value.MutualExclusionGroup != "equipment_round_shield")).OrderBy(value => StableKey(instance.InstanceId, instance.ReforgeCount, value.AffixId)).ToArray();
            int count = instance.Rarity == EquipmentRarity.Rare ? 2 : 3;
            string[] selected = candidates.GroupBy(value => value.MutualExclusionGroup, StringComparer.Ordinal).Select(group => group.First()).Take(count).Select(value => value.AffixId).ToArray();
            if (selected.Length < count) return false;
            gold -= cost; instance.MutableAffixIds.Clear(); instance.MutableAffixIds.AddRange(selected); instance.ReforgeCount++; return true;
        }

        public bool Calibrate(string instanceId, string nodeId, string branchId)
        {
            if (!equipment.TryGetValue(instanceId, out RogueEquipmentInstance instance)) return false;
            UpgradeNodeDefinition node = Definition(instance).UpgradeNodes.FirstOrDefault(value => value.NodeId == nodeId);
            if (node == null || (branchId != node.BranchAEffectId && branchId != node.BranchBEffectId)) return false;
            instance.UpgradeBranchIds.RemoveAll(value => value.StartsWith(nodeId + ":", StringComparison.Ordinal));
            instance.UpgradeBranchIds.Add(nodeId + ":" + branchId); return true;
        }

        public RogueValidationResult Validate()
        {
            RogueValidationResult result = new RogueValidationResult();
            if (IsTwoHandedMainEquipped() && !string.IsNullOrEmpty(equipped[EquipmentSlot.OffHand])) result.Add("Two-handed main hand conflicts with offhand.");
            foreach (RogueEquipmentInstance instance in equipment.Values)
            {
                EquipmentDefinition definition = Definition(instance);
                string[] groups = instance.MutableAffixIds.Select(id => catalog.Affixes.Single(value => value.AffixId == id).MutualExclusionGroup).ToArray();
                if (groups.Distinct(StringComparer.Ordinal).Count() != groups.Length) result.Add("Duplicate affix mutual-exclusion group: " + instance.InstanceId);
                bool fixedShield = definition.TurnStartShield > 0 || definition.FixedEffectIds.Any(value => value.Contains("shield"));
                if (fixedShield && groups.Contains("equipment_round_shield")) result.Add("Fixed and random shield cannot coexist on one item: " + instance.InstanceId);
            }
            return result;
        }

        private EquipmentDefinition Definition(RogueEquipmentInstance instance) => catalog.Equipment.Single(value => value.DefinitionId == instance.DefinitionId);
        private bool IsTwoHandedMainEquipped() => !string.IsNullOrEmpty(equipped[EquipmentSlot.MainHand]) && Definition(equipment[equipped[EquipmentSlot.MainHand]]).Handedness == EquipmentHandedness.TwoHanded;
        private bool AddFirstFit(string instanceId)
        {
            if (backpack.ContainsKey(instanceId) || equipped.Values.Contains(instanceId)) return false;
            RogueBackpackPlacement? placement = FindFirstFit(instanceId); if (!placement.HasValue) return false;
            backpack[instanceId] = placement.Value; return true;
        }
        private RogueBackpackPlacement? FindFirstFit(string instanceId, string ignoredInstanceId = null)
        {
            Size(instanceId, false, out int width, out int height);
            for (int y = 0; y < RogueRuntimeConstants.BackpackHeight; y++)
            for (int x = 0; x < RogueRuntimeConstants.BackpackWidth; x++)
            {
                if (Fits(instanceId, x, y, false, width, height, ignoredInstanceId)) return new RogueBackpackPlacement(x, y, false);
                if (width != height && Fits(instanceId, x, y, true, height, width, ignoredInstanceId)) return new RogueBackpackPlacement(x, y, true);
            }
            return null;
        }
        private bool Fits(string instanceId, int x, int y, bool rotated, int width, int height, string ignoredInstanceId = null)
        {
            if (x < 0 || y < 0 || x + width > RogueRuntimeConstants.BackpackWidth || y + height > RogueRuntimeConstants.BackpackHeight) return false;
            foreach (KeyValuePair<string, RogueBackpackPlacement> pair in backpack)
            {
                if (pair.Key == ignoredInstanceId) continue;
                Size(pair.Key, pair.Value.Rotated, out int otherWidth, out int otherHeight);
                if (x < pair.Value.X + otherWidth && x + width > pair.Value.X && y < pair.Value.Y + otherHeight && y + height > pair.Value.Y) return false;
            }
            return true;
        }
        private void Size(string instanceId, bool rotated, out int width, out int height)
        {
            if (equipment.TryGetValue(instanceId, out RogueEquipmentInstance item)) { EquipmentDefinition definition = Definition(item); width = rotated ? definition.Height : definition.Width; height = rotated ? definition.Width : definition.Height; return; }
            TacticalItemDefinition tacticalDefinition = catalog.TacticalItems.Single(value => value.DefinitionId == tactical[instanceId].DefinitionId);
            width = rotated ? tacticalDefinition.Height : tacticalDefinition.Width; height = rotated ? tacticalDefinition.Width : tacticalDefinition.Height;
        }
        private int StableKey(string instanceId, int count, string affixId)
        {
            unchecked { uint hash = 2166136261; foreach (char c in seed + "|" + instanceId + "|" + count + "|" + affixId) { hash ^= c; hash *= 16777619; } return (int)hash; }
        }
    }
}
