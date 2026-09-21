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
        private readonly Dictionary<EquipmentSlot, string> equipped = EquipmentSlotRules.ActiveSlots.ToDictionary(value => value, value => string.Empty);
        private readonly string[] quickbar = new string[RogueRuntimeConstants.ItemQuickbarSize];
        private readonly HashSet<string> firstMoveAvailable = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> firstPaidSpellReturned = new HashSet<string>(StringComparer.Ordinal);
        private CombatState attachedCombat;

        public IReadOnlyDictionary<EquipmentSlot, string> Equipped => equipped;
        public IReadOnlyDictionary<string, RogueBackpackPlacement> Backpack => backpack;
        public int BackpackColumns => CurrentBackpackCapacity().columns;
        public int BackpackRows => CurrentBackpackCapacity().rows;
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
            RogueEquipmentInstance backpack = runtime.CreateInstance("starter-backpack", "ACA-EQ-BP01", EquipmentRarity.Common, 1, "starter");
            runtime.AddToBackpack(chest); runtime.AddToBackpack(backpack);
            runtime.Equip(chest.InstanceId, EquipmentSlot.Chest); runtime.Equip(backpack.InstanceId, EquipmentSlot.Backpack);
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
            }

            HashSet<string> equippedIds = runtime.RestoreEquippedSlots(dto.EquipmentSlotInstanceIds);
            foreach (EquipmentInstanceDto saved in dto.EquipmentInstances.OrderBy(value => value.AcquiredOrder))
            {
                if (equippedIds.Contains(saved.InstanceId)) continue;
                RogueEquipmentInstance instance = runtime.equipment[saved.InstanceId];
                if (saved.BackpackX >= 0 && saved.BackpackY >= 0) runtime.backpack[instance.InstanceId] = new RogueBackpackPlacement(saved.BackpackX, saved.BackpackY, saved.BackpackRotated);
                else if (!runtime.AddToBackpack(instance)) throw new InvalidOperationException("Saved equipment has no legal backpack position: " + instance.InstanceId);
            }
            foreach (TacticalItemInstanceDto saved in dto.TacticalItemInstances)
            {
                RogueTacticalItemInstance item = runtime.CreateTacticalItem(saved.InstanceId, saved.DefinitionId, 0, saved.SourceType); item.RestoreCharges(saved.ChargesCurrent);
                runtime.backpack[item.InstanceId] = new RogueBackpackPlacement(saved.X, saved.Y, saved.Rotated);
            }
            runtime.NormalizeBackpackPlacementsAfterLoad();
            for (int index = 0; index < runtime.quickbar.Length; index++) runtime.AssignQuickbar(index, dto.ItemQuickbarInstanceIds[index]);
            return runtime;
        }

        public void WriteToDto(RogueRunDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            dto.EquipmentInstances.Clear(); foreach (EquipmentSlot slot in equipped.Keys.ToArray()) dto.EquipmentSlotInstanceIds[slot] = equipped[slot];
            foreach (RogueEquipmentInstance instance in equipment.Values.OrderBy(value => value.AcquiredOrder))
            {
                KeyValuePair<EquipmentSlot, string> equippedPair = equipped.FirstOrDefault(value => value.Value == instance.InstanceId);
                EquipmentSlot slot = string.IsNullOrEmpty(equippedPair.Value) ? EquipmentSlot.None : equippedPair.Key;
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

        public bool TryAddEquipmentFromLoot(string instanceId, string definitionId, string sourceType)
        {
            EquipmentDefinition definition = catalog.Equipment.FirstOrDefault(value => value.DefinitionId == definitionId);
            if (definition == null || equipment.ContainsKey(instanceId) || tactical.ContainsKey(instanceId)) return false;
            RogueEquipmentInstance instance = CreateInstance(instanceId, definitionId, definition.AllowedRarities[0],
                equipment.Count + tactical.Count, sourceType);
            if (AddToBackpack(instance)) return true;
            equipment.Remove(instanceId);
            return false;
        }

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
            slot = EquipmentSlotRules.NormalizeLegacy(slot);
            if (!CanEquip(instanceId, slot, false, out RogueEquipmentInstance instance)) return false;
            if (slot == EquipmentSlot.Backpack)
            {
                (int columns, int rows) = CapacityFor(instanceId);
                if (!AllPlacementsLegal(columns, rows, instanceId)) return false;
            }
            backpack.Remove(instanceId); equipped[slot] = instanceId; NotifyLoadoutChanged(); return true;
        }

        public bool CanEquipOrReplace(string instanceId, EquipmentSlot slot)
        {
            slot = EquipmentSlotRules.NormalizeLegacy(slot);
            if (!CanEquip(instanceId, slot, true, out _)) return false;
            string previous = equipped[slot];
            if (slot != EquipmentSlot.Backpack)
                return string.IsNullOrEmpty(previous) || FindFirstFit(previous, instanceId).HasValue;
            (int columns, int rows) = CapacityFor(instanceId);
            if (!AllPlacementsLegal(columns, rows, instanceId)) return false;
            return string.IsNullOrEmpty(previous) || FindFirstFit(previous, instanceId, columns, rows).HasValue;
        }

        public bool EquipOrReplace(string instanceId, EquipmentSlot slot)
        {
            slot = EquipmentSlotRules.NormalizeLegacy(slot);
            if (!CanEquip(instanceId, slot, true, out _)) return false;
            string previous = equipped[slot];
            if (string.IsNullOrEmpty(previous)) return Equip(instanceId, slot);
            int columns = BackpackColumns;
            int rows = BackpackRows;
            if (slot == EquipmentSlot.Backpack)
            {
                (columns, rows) = CapacityFor(instanceId);
                if (!AllPlacementsLegal(columns, rows, instanceId)) return false;
            }
            RogueBackpackPlacement? previousPlacement = FindFirstFit(previous, instanceId, columns, rows);
            if (!previousPlacement.HasValue) return false;
            backpack.Remove(instanceId);
            backpack[previous] = previousPlacement.Value;
            equipped[slot] = instanceId;
            NotifyLoadoutChanged();
            return true;
        }

        private bool CanEquip(string instanceId, EquipmentSlot slot, bool allowOccupied, out RogueEquipmentInstance instance)
        {
            instance = null;
            if (!equipment.TryGetValue(instanceId, out instance) || !backpack.ContainsKey(instanceId) || !equipped.ContainsKey(slot)) return false;
            if (!allowOccupied && !string.IsNullOrEmpty(equipped[slot])) return false;
            EquipmentDefinition definition = Definition(instance);
            return EquipmentSlotRules.CanEquip(definition.Slot, slot);
        }

        public bool Unequip(EquipmentSlot slot)
        {
            slot = EquipmentSlotRules.NormalizeLegacy(slot);
            if (!equipped.ContainsKey(slot)) return false;
            string instanceId = equipped[slot];
            if (string.IsNullOrEmpty(instanceId)) return false;
            int columns = slot == EquipmentSlot.Backpack ? RogueRuntimeConstants.DefaultBackpackColumns : BackpackColumns;
            int rows = slot == EquipmentSlot.Backpack ? RogueRuntimeConstants.DefaultBackpackRows : BackpackRows;
            if (!AllPlacementsLegal(columns, rows)) return false;
            RogueBackpackPlacement? placement = FindFirstFit(instanceId, null, columns, rows);
            if (!placement.HasValue) return false;
            backpack[instanceId] = placement.Value; equipped[slot] = string.Empty; NotifyLoadoutChanged(); return true;
        }

        public bool CanUnequipToBackpack(EquipmentSlot slot, int x, int y, bool rotated)
        {
            slot = EquipmentSlotRules.NormalizeLegacy(slot);
            if (!equipped.TryGetValue(slot, out string instanceId) || string.IsNullOrEmpty(instanceId)) return false;
            Size(instanceId, rotated, out int width, out int height);
            int columns = slot == EquipmentSlot.Backpack ? RogueRuntimeConstants.DefaultBackpackColumns : BackpackColumns;
            int rows = slot == EquipmentSlot.Backpack ? RogueRuntimeConstants.DefaultBackpackRows : BackpackRows;
            return AllPlacementsLegal(columns, rows) && Fits(instanceId, x, y, rotated, width, height, null, columns, rows);
        }

        public bool UnequipToBackpack(EquipmentSlot slot, int x, int y, bool rotated)
        {
            slot = EquipmentSlotRules.NormalizeLegacy(slot);
            if (!CanUnequipToBackpack(slot, x, y, rotated)) return false;
            string instanceId = equipped[slot];
            backpack[instanceId] = new RogueBackpackPlacement(x, y, rotated);
            equipped[slot] = string.Empty;
            NotifyLoadoutChanged();
            return true;
        }

        internal void AttachToCombat(CombatState combat)
        {
            attachedCombat = combat ?? throw new ArgumentNullException(nameof(combat));
            RefreshEquipmentPassives();
        }

        private void NotifyLoadoutChanged()
        {
            if (attachedCombat != null) RefreshEquipmentPassives();
        }

        private void RefreshEquipmentPassives()
        {
            attachedCombat.PassiveEffects.RemovePassivesFromSource("hero", CombatPassiveSourceKind.Equipment);
            foreach (EquipmentSlot slot in EquipmentSlotRules.ActiveSlots)
            {
                string instanceId = equipped[slot];
                if (string.IsNullOrEmpty(instanceId)) continue;
                RogueEquipmentInstance instance = equipment[instanceId];
                EquipmentDefinition definition = Definition(instance);
                List<string> effectIds = definition.FixedEffectIds.ToList();
                effectIds.AddRange(instance.MutableAffixIds.Select(affixId =>
                    catalog.Affixes.Single(value => value.AffixId == affixId).EffectId));
                effectIds.AddRange(instance.UpgradeBranchIds.Select(UpgradeEffectId));
                foreach (string effectId in effectIds.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal))
                {
                    attachedCombat.PassiveEffects.RegisterPassive("hero", new CombatPassiveDefinition(
                        "equipment:" + instance.InstanceId + ":" + effectId,
                        definition.DisplayName,
                        CombatPassiveSourceKind.Equipment,
                        instance.InstanceId,
                        EquipmentTrigger(effectId),
                        EquipmentEffectDetail(effectId), 10,
                        EquipmentCondition(effectId), effectId, EquipmentActivationLimit(effectId),
                        EquipmentLimitScope(effectId)));
                }
            }
        }

        private static string UpgradeEffectId(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            int separator = value.IndexOf(':');
            return separator < 0 ? value : value.Substring(separator + 1);
        }

        private static CombatPassiveTrigger EquipmentTrigger(string effectId)
        {
            if (effectId.StartsWith("turn_start_", StringComparison.Ordinal) || effectId.StartsWith("low_mana_", StringComparison.Ordinal))
                return CombatPassiveTrigger.OwnTurnStart;
            if (effectId.StartsWith("first_move", StringComparison.Ordinal) || effectId.StartsWith("move_", StringComparison.Ordinal))
                return CombatPassiveTrigger.AfterActiveMove;
            if (effectId.StartsWith("first_paid_personal_spell", StringComparison.Ordinal)) return CombatPassiveTrigger.AfterPersonalSpellCostPaid;
            if (effectId.StartsWith("first_search", StringComparison.Ordinal) || effectId.StartsWith("first_task", StringComparison.Ordinal) ||
                effectId.StartsWith("first_quickbar", StringComparison.Ordinal)) return CombatPassiveTrigger.RelevantAction;
            if (effectId.Contains("spell")) return CombatPassiveTrigger.AfterPersonalSpellDamage;
            if (effectId.Contains("weapon") || effectId.Contains("attack")) return CombatPassiveTrigger.AfterWeaponHit;
            if (effectId.Contains("status")) return CombatPassiveTrigger.StatusApplied;
            return CombatPassiveTrigger.AttributeQuery;
        }

        private static string EquipmentCondition(string effectId)
        {
            if (effectId == "first_move:+1") return "first_move_each_own_turn";
            if (effectId.StartsWith("first_", StringComparison.Ordinal)) return effectId.Split(':')[0];
            if (effectId.StartsWith("low_mana_", StringComparison.Ordinal)) return "low_personal_mana";
            return "equipped";
        }

        private static int EquipmentActivationLimit(string effectId) =>
            effectId.StartsWith("first_", StringComparison.Ordinal) ? 1 : 0;

        private static CombatPassiveLimitScope EquipmentLimitScope(string effectId)
        {
            if (effectId == "first_move:+1") return CombatPassiveLimitScope.OwnTurn;
            return effectId.StartsWith("first_", StringComparison.Ordinal)
                ? CombatPassiveLimitScope.Battle
                : CombatPassiveLimitScope.None;
        }

        private static string EquipmentEffectDetail(string effectId)
        {
            string[] parts = effectId.Split(':');
            string amount = parts.Length > 1 ? parts[parts.Length - 1].TrimStart('+') : string.Empty;
            if (effectId.StartsWith("turn_start_shield:", StringComparison.Ordinal)) return "自己回合开始时获得 " + amount + " 护盾。";
            if (effectId == "first_move:+1") return "每个自己回合第一次移动的最大步数 +1。";
            if (effectId == "first_search_free") return "每场战斗第一次搜索不消耗行动点。";
            if (effectId == "first_task_interact_free") return "每场战斗第一次任务互动不消耗行动点。";
            if (effectId == "first_quickbar_swap_free") return "每场战斗第一次调整战术栏不消耗行动点。";
            if (effectId.StartsWith("weapon_range:", StringComparison.Ordinal)) return "武器射程 +" + amount + "。";
            if (effectId.StartsWith("weapon_damage:", StringComparison.Ordinal)) return "武器伤害 +" + amount + "。";
            if (effectId.StartsWith("max_mana:", StringComparison.Ordinal)) return "个人魔力上限 +" + amount + "。";
            if (effectId.StartsWith("first_paid_personal_spell_mana:", StringComparison.Ordinal))
                return "每场战斗第一次支付个人术式魔力后，返还 " + amount + " 点个人魔力。";
            if (effectId.StartsWith("low_mana_shield:", StringComparison.Ordinal))
                return "低个人魔力条件满足时获得 " + amount + " 护盾。";
            if (effectId.StartsWith("forced_move:-", StringComparison.Ordinal)) return "受到的强制位移距离减少 " + amount.TrimStart('-') + " 格。";
            return "装备效果持续生效；完整规则见装备详情。";
        }

        public void OnTurnStart(CombatState combat, string unitId)
        {
            firstMoveAvailable.Add(unitId);
            foreach (EquipmentSlot slot in EquipmentSlotRules.ActiveSlots)
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

        public int MovementBonus(string unitId) => unitId == "hero" && firstMoveAvailable.Contains(unitId) && HasEquippedEffect("first_move:+1") ? 1 : 0;
        public void AfterMove(string unitId) => firstMoveAvailable.Remove(unitId);

        public void OnPersonalSpellPaid(CombatState combat, string unitId, int manaPaid)
        {
            if (combat == null || unitId != "hero" || manaPaid < 1 || firstPaidSpellReturned.Contains(unitId) ||
                !HasEquippedEffect("first_paid_personal_spell_mana:+2")) return;
            UnitState unit = combat.GetUnit(unitId);
            if (unit == null || !unit.IsAlive) return;
            unit.RestoreMana(2);
            firstPaidSpellReturned.Add(unitId);
            combat.AddLog("苗床回流芯触发：本场首次付费个人术式返还 2 魔力。");
        }

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
            // The master plan removed random reforge/reroll. Keep the method only so old callers and
            // saves fail closed; deterministic material installation is represented by Calibrate.
            return false;
        }

        public bool Calibrate(string instanceId, string nodeId, string branchId)
        {
            if (!equipment.TryGetValue(instanceId, out RogueEquipmentInstance instance) || instance.UpgradeBranchIds.Count > 0) return false;
            UpgradeNodeDefinition node = Definition(instance).UpgradeNodes.FirstOrDefault(value => value.NodeId == nodeId);
            if (node == null || (branchId != node.BranchAEffectId && branchId != node.BranchBEffectId)) return false;
            instance.UpgradeBranchIds.Add(nodeId + ":" + branchId); NotifyLoadoutChanged(); return true;
        }

        public RogueValidationResult Validate()
        {
            RogueValidationResult result = new RogueValidationResult();
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
        private HashSet<string> RestoreEquippedSlots(IReadOnlyDictionary<EquipmentSlot, string> savedSlots)
        {
            HashSet<string> restored = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<EquipmentSlot, string> pair in savedSlots.Where(value => !string.IsNullOrEmpty(value.Value)))
            {
                EquipmentSlot slot = EquipmentSlotRules.NormalizeLegacy(pair.Key);
                if (!equipped.ContainsKey(slot) || !equipment.TryGetValue(pair.Value, out RogueEquipmentInstance instance) ||
                    !EquipmentSlotRules.CanEquip(Definition(instance).Slot, slot) || !restored.Add(pair.Value))
                    throw new InvalidOperationException("Saved equipment cannot be equipped: " + pair.Value);
                equipped[slot] = pair.Value;
            }
            return restored;
        }
        private bool HasEquippedEffect(string effectId) => equipped.Values.Where(value => !string.IsNullOrEmpty(value))
            .Select(value => equipment[value]).SelectMany(value => Definition(value).FixedEffectIds)
            .Any(value => string.Equals(value, effectId, StringComparison.Ordinal));
        private bool AddFirstFit(string instanceId)
        {
            if (backpack.ContainsKey(instanceId) || equipped.Values.Contains(instanceId)) return false;
            RogueBackpackPlacement? placement = FindFirstFit(instanceId); if (!placement.HasValue) return false;
            backpack[instanceId] = placement.Value; return true;
        }
        private RogueBackpackPlacement? FindFirstFit(string instanceId, string ignoredInstanceId = null,
            int? capacityColumns = null, int? capacityRows = null)
        {
            int columns = capacityColumns ?? BackpackColumns;
            int rows = capacityRows ?? BackpackRows;
            Size(instanceId, false, out int width, out int height);
            for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
            {
                if (Fits(instanceId, x, y, false, width, height, ignoredInstanceId, columns, rows)) return new RogueBackpackPlacement(x, y, false);
                if (width != height && Fits(instanceId, x, y, true, height, width, ignoredInstanceId, columns, rows)) return new RogueBackpackPlacement(x, y, true);
            }
            return null;
        }
        private bool Fits(string instanceId, int x, int y, bool rotated, int width, int height,
            string ignoredInstanceId = null, int? capacityColumns = null, int? capacityRows = null)
        {
            int columns = capacityColumns ?? BackpackColumns;
            int rows = capacityRows ?? BackpackRows;
            if (x < 0 || y < 0 || x + width > columns || y + height > rows) return false;
            foreach (KeyValuePair<string, RogueBackpackPlacement> pair in backpack)
            {
                if (pair.Key == ignoredInstanceId) continue;
                Size(pair.Key, pair.Value.Rotated, out int otherWidth, out int otherHeight);
                if (x < pair.Value.X + otherWidth && x + width > pair.Value.X && y < pair.Value.Y + otherHeight && y + height > pair.Value.Y) return false;
            }
            return true;
        }
        private (int columns, int rows) CurrentBackpackCapacity()
        {
            string instanceId = equipped[EquipmentSlot.Backpack];
            return string.IsNullOrEmpty(instanceId)
                ? (RogueRuntimeConstants.DefaultBackpackColumns, RogueRuntimeConstants.DefaultBackpackRows)
                : CapacityFor(instanceId);
        }
        private (int columns, int rows) CapacityFor(string instanceId)
        {
            EquipmentDefinition definition = DefinitionFor(instanceId);
            if (definition == null || definition.Slot != EquipmentSlot.Backpack)
                throw new InvalidOperationException("Backpack capacity requires a backpack equipment definition: " + instanceId);
            return (definition.BackpackColumns, definition.BackpackRows);
        }
        private bool AllPlacementsLegal(int columns, int rows, string ignoredInstanceId = null)
        {
            foreach (KeyValuePair<string, RogueBackpackPlacement> pair in backpack)
            {
                if (pair.Key == ignoredInstanceId) continue;
                Size(pair.Key, pair.Value.Rotated, out int width, out int height);
                if (pair.Value.X < 0 || pair.Value.Y < 0 || pair.Value.X + width > columns || pair.Value.Y + height > rows)
                    return false;
                foreach (KeyValuePair<string, RogueBackpackPlacement> other in backpack)
                {
                    if (other.Key == ignoredInstanceId || string.CompareOrdinal(pair.Key, other.Key) >= 0) continue;
                    Size(other.Key, other.Value.Rotated, out int otherWidth, out int otherHeight);
                    if (pair.Value.X < other.Value.X + otherWidth && pair.Value.X + width > other.Value.X &&
                        pair.Value.Y < other.Value.Y + otherHeight && pair.Value.Y + height > other.Value.Y) return false;
                }
            }
            return true;
        }
        private void NormalizeBackpackPlacementsAfterLoad()
        {
            if (AllPlacementsLegal(BackpackColumns, BackpackRows)) return;
            string[] ordered = backpack.Keys.OrderBy(InstanceAcquiredOrder).ThenBy(value => value, StringComparer.Ordinal).ToArray();
            backpack.Clear();
            foreach (string instanceId in ordered)
                if (!AddFirstFit(instanceId))
                    throw new InvalidOperationException("Saved backpack contents do not fit the current capacity: " + instanceId);
        }
        private int InstanceAcquiredOrder(string instanceId)
        {
            if (equipment.TryGetValue(instanceId, out RogueEquipmentInstance equipmentItem)) return equipmentItem.AcquiredOrder;
            return tactical.TryGetValue(instanceId, out RogueTacticalItemInstance tacticalItem) ? tacticalItem.AcquiredOrder : int.MaxValue;
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
