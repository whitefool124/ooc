using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat.Roguelite
{
    public sealed class RogueSpellSlotPresentation
    {
        public int Slot { get; }
        public string DefinitionId { get; }
        public string DisplayName { get; }
        public int ActionPointCost { get; }
        public int ManaCost { get; }
        public int CooldownRemaining { get; }
        public string CompactSlotLabel => (Slot + 1).ToString();
        public RogueSpellSlotPresentation(int slot, SpellDefinition definition, int cooldown)
        { Slot = slot; DefinitionId = definition?.DefinitionId ?? string.Empty; DisplayName = definition?.DisplayName ?? "空"; ActionPointCost = definition?.ActionPointCost ?? 0; ManaCost = definition?.ManaCost ?? 0; CooldownRemaining = cooldown; }
    }

    public sealed class RogueQuickbarPresentation
    {
        public int Slot { get; }
        public string InstanceId { get; }
        public string DefinitionId { get; }
        public string DisplayName { get; }
        public int ChargesCurrent { get; }
        public int ChargesMaximum { get; }
        public RogueQuickbarPresentation(int slot, RogueTacticalItemInstance item, TacticalItemDefinition definition = null)
        { Slot = slot; InstanceId = item?.InstanceId ?? string.Empty; DefinitionId = item?.DefinitionId ?? string.Empty; DisplayName = definition?.DisplayName ?? (item == null ? "空" : item.DefinitionId); ChargesCurrent = item?.ChargesCurrent ?? 0; ChargesMaximum = definition?.MaximumCharges ?? 0; }
    }

    public sealed class RogueCombatHudPresentation
    {
        public int Health { get; }
        public int Mana { get; }
        public int Shield { get; }
        public int Gold { get; }
        public int StageContribution { get; }
        public int ActionPoints { get; }
        public int MaximumHealth { get; }
        public int MaximumMana { get; }
        public bool ShieldIsUncapped => true;
        public bool BreakStanceActive { get; }
        public IReadOnlyList<RogueSpellSlotPresentation> SpellSlots { get; }
        public IReadOnlyList<RogueQuickbarPresentation> Quickbar { get; }

        public RogueCombatHudPresentation(CombatState combat, RogueRunDto run = null)
        {
            if (combat == null || combat.Ruleset != CombatRuleset.Roguelite) throw new ArgumentException("Roguelite combat required.", nameof(combat));
            UnitState hero = combat.GetUnit("hero"); Health = hero?.Health ?? 0; Mana = hero?.Mana ?? 0; Shield = hero?.Shield ?? 0;
            ActionPoints = hero?.ActionPoints ?? 0; MaximumHealth = hero?.MaxHealth ?? 0; MaximumMana = hero?.MaxMana ?? RogueRuntimeConstants.MaximumPersonalMana;
            BreakStanceActive = hero?.HasStatus(StatusType.BreakStance) ?? false;
            Gold = run?.Gold ?? 0; StageContribution = run?.StageContribution ?? 0;
            SpellSlots = Enumerable.Range(0, RogueRuntimeConstants.SpellSlotCount).Select(index =>
            {
                SpellDefinition definition = combat.RogueSpells?.DefinitionAtSlot(index);
                return new RogueSpellSlotPresentation(index, definition, definition == null ? 0 : combat.RogueSpells.CooldownRemaining(definition.DefinitionId));
            }).ToArray();
            string[] quickbar = combat.RogueEquipment?.ItemQuickbarInstanceIds ?? new string[RogueRuntimeConstants.ItemQuickbarSize];
            RogueContentCatalog catalog = RogueContentCatalog.CreateAcademyV01();
            Quickbar = Enumerable.Range(0, RogueRuntimeConstants.ItemQuickbarSize).Select(index =>
            {
                RogueTacticalItemInstance item = combat.RogueEquipment?.TacticalItem(quickbar[index]);
                TacticalItemDefinition definition = item == null ? null : catalog.TacticalItems.FirstOrDefault(value => value.DefinitionId == item.DefinitionId);
                return new RogueQuickbarPresentation(index, item, definition);
            }).ToArray();
        }
    }

    public sealed class RogueMapStatusPresentation
    {
        public int Health { get; }
        public int MaximumHealth => 18;
        public int Mana { get; }
        public int MaximumMana => RogueRuntimeConstants.MaximumPersonalMana;
        public int Gold { get; }
        public int StageContribution { get; }
        public int StageTime { get; }
        public int ConsolidationTime => AcademyMapTuning.ConsolidationProgress;
        public int WarningTime => AcademyMapTuning.TransitionWarningProgress;
        public int TransitionTime => AcademyMapTuning.TransitionProgress;
        public int ExploredNodes { get; }
        public int RequiredExploredNodes => AcademyMapTuning.BossMinimumProgress;
        public int CorePermits { get; }
        public int RequiredCorePermits => AcademyMapTuning.CorePermitRequirement;
        public bool EarlyFinaleReady { get; }
        public bool ForcedFinaleReady { get; }
        public string PhaseLabel { get; }

        public RogueMapStatusPresentation(RogueliteMapRun run)
        {
            if (run == null || !run.UsesRogue11) throw new ArgumentException("rogue11 map run required", nameof(run));
            Health = run.CurrentHealth; Mana = run.CurrentMana; Gold = run.Gold; StageContribution = run.StageContribution; StageTime = run.StageTime;
            ExploredNodes = run.AcademyProgress; CorePermits = run.CorePermits;
            EarlyFinaleReady = ExploredNodes >= RequiredExploredNodes && CorePermits >= RequiredCorePermits;
            ForcedFinaleReady = StageTime >= TransitionTime;
            PhaseLabel = ForcedFinaleReady ? "终考已经开始" : StageTime >= WarningTime ? "终考已经很近" : StageTime >= ConsolidationTime ? "学期将尽" : "日程还宽裕";
        }
    }

    public sealed class RogueNodePreviewPresentation
    {
        public string NodeId { get; }
        public string RiskLabel { get; }
        public string RewardLabel { get; }
        public string FailureConsequence { get; }
        public string EncounterLabel { get; }
        public string EnemySummary { get; }
        public string SpatialRisk { get; }
        public int TimeCost { get; }
        public int ProjectedStageTime { get; }
        public int ExpectedHealthRecovery { get; }
        public int ExpectedManaRecovery { get; }
        public bool IsZeroTime => TimeCost == 0;
        public bool CrossesConsolidation { get; }
        public bool CrossesWarning { get; }
        public bool CrossesTransition { get; }

        public RogueNodePreviewPresentation(RogueliteMapRun run, RogueliteMapNode node)
        {
            if (run == null || !run.UsesRogue11) throw new ArgumentException("rogue11 map run required", nameof(run));
            if (node == null) throw new ArgumentNullException(nameof(node));
            NodeId = node.Id; TimeCost = AcademyMapTuning.TimeCost(node.Type); ProjectedStageTime = run.StageTime + TimeCost;
            ExpectedHealthRecovery = Math.Min(18 - run.CurrentHealth, TimeCost * 4);
            ExpectedManaRecovery = Math.Min(RogueRuntimeConstants.MaximumPersonalMana - run.CurrentMana, TimeCost);
            CrossesConsolidation = Crosses(run.StageTime, ProjectedStageTime, AcademyMapTuning.ConsolidationProgress);
            CrossesWarning = Crosses(run.StageTime, ProjectedStageTime, AcademyMapTuning.TransitionWarningProgress);
            CrossesTransition = Crosses(run.StageTime, ProjectedStageTime, AcademyMapTuning.TransitionProgress);
            RogueliteEncounterDefinition encounter = node.IsCombat ? RogueliteEncounterCatalog.For(run, node.Id) : null;
            EncounterLabel = encounter == null ? string.Empty : encounter.Tier == RogueliteEncounterTier.Weak ? "轻松" : encounter.Tier == RogueliteEncounterTier.Strong ? "棘手" : encounter.Tier == RogueliteEncounterTier.Elite ? "危险" : "终考";
            EnemySummary = encounter == null ? string.Empty : string.Join("、", encounter.EnemyArchetypeIds.Select(id => EnemyArchetypes.Get(id).DisplayName));
            SpatialRisk = encounter == null ? string.Empty : encounter.SpawnRelationship;
            RiskLabel = encounter?.PublicRisk ?? (node.Type == RogueliteMapNodeType.Finale ? "终考" : node.Type == RogueliteMapNodeType.Elite ? "危险" :
                node.Type == RogueliteMapNodeType.Combat ? "棘手" : node.Type == RogueliteMapNodeType.Event ? "先听听看" : "可以放心前往");
            RewardLabel = encounter?.RewardTier ?? (node.Type == RogueliteMapNodeType.Finale ? "终考奖励" : node.Type == RogueliteMapNodeType.Elite ? "稀有奖励" :
                node.Type == RogueliteMapNodeType.Combat ? "金币、学院贡献和一件奖励" : node.GrantedAccessCards > 0 ? "核心许可" : "这里能找到的东西");
            FailureConsequence = node.IsCombat ? "输了也会有人把你带回学院，但花掉的时间、生命和道具不会返还。你只能拿到一半金币与学院贡献，也不能挑选奖励。" :
                node.Type == RogueliteMapNodeType.Event ? "做出选择后就不能反悔；如果要动手，输了也会损失时间、生命和用掉的道具。" : "这里没有战斗，也不会花时间。";
        }

        private static bool Crosses(int before, int after, int threshold) => before < threshold && after >= threshold;
    }

    public sealed class RogueBuildSummaryPresentation
    {
        public int EquippedSpellCount { get; }
        public int EquippedEquipmentCount { get; }
        public int TacticalItemCount { get; }
        public int TacticalChargesRemaining { get; }

        public RogueBuildSummaryPresentation(RogueliteMapRun run)
        {
            if (run == null || !run.UsesRogue11) throw new ArgumentException("rogue11 map run required", nameof(run));
            RogueRunDto dto = run.RogueRunState;
            EquippedSpellCount = dto.EquippedSpellIds.Count(value => !string.IsNullOrEmpty(value));
            EquippedEquipmentCount = dto.EquipmentSlotInstanceIds.Count(value => !string.IsNullOrEmpty(value.Value));
            HashSet<string> quickbar = new HashSet<string>(dto.ItemQuickbarInstanceIds.Where(value => !string.IsNullOrEmpty(value)), StringComparer.Ordinal);
            TacticalItemCount = quickbar.Count;
            TacticalChargesRemaining = dto.TacticalItemInstances.Where(value => quickbar.Contains(value.InstanceId)).Sum(value => value.ChargesCurrent);
        }
    }

    public sealed class RogueDamageSegmentPresentation
    {
        public int SegmentIndex { get; }
        public DamageResolution Resolution { get; }
        public RogueDamageSegmentPresentation(int index, DamageResolution resolution) { SegmentIndex = index; Resolution = resolution; }
    }

    public static class RogueDamagePreviewPresentation
    {
        public static IReadOnlyList<RogueDamageSegmentPresentation> Build(IEnumerable<DamagePacket> packets, int shield, int health)
        {
            List<RogueDamageSegmentPresentation> rows = new List<RogueDamageSegmentPresentation>();
            foreach (DamagePacket packet in (packets ?? Array.Empty<DamagePacket>()).OrderBy(value => value.SegmentIndex))
            {
                DamageResolution result = RogueDamageResolver.Resolve(packet, shield, health); rows.Add(new RogueDamageSegmentPresentation(packet.SegmentIndex, result));
                shield -= result.ShieldAbsorbed; health -= result.HealthDamage;
            }
            return rows;
        }
    }

    public static class RogueShieldLogPresentation
    {
        public static string Format(ShieldSourceRecord record)
        {
            if (record == null) return string.Empty;
            string action = record.EventKind == ShieldEventKind.Granted ? "获得" :
                record.EventKind == ShieldEventKind.PreventedByBreakStance ? "破势阻止" :
                record.EventKind == ShieldEventKind.Absorbed ? "吸收" :
                record.EventKind == ShieldEventKind.ClearedAtTurnStart ? "回合开始清空" : "破势浪费";
            return action + " " + record.Amount + " 护盾" +
                (record.TriggerTurn > 0 ? " · 第" + record.TriggerTurn + "回合" : string.Empty);
        }
    }

    public sealed class RogueEquipmentCardPresentation
    {
        public string InstanceId { get; }
        public EquipmentSlot Slot { get; }
        public EquipmentHandedness Handedness { get; }
        public EquipmentRarity Rarity { get; }
        public IReadOnlyList<string> AffixIds { get; }
        public IReadOnlyList<string> UpgradeBranchIds { get; }
        public int Weight { get; }
        public int AetherLoad { get; }
        public IReadOnlyList<string> ShieldSourceIds { get; }

        public RogueEquipmentCardPresentation(RogueEquipmentInstance instance, EquipmentDefinition definition)
        {
            if (instance == null || definition == null) throw new ArgumentNullException(instance == null ? nameof(instance) : nameof(definition));
            InstanceId = instance.InstanceId; Slot = definition.Slot; Handedness = definition.Handedness; Rarity = instance.Rarity;
            AffixIds = instance.MutableAffixIds.ToArray(); UpgradeBranchIds = instance.UpgradeBranchIds.ToArray();
            Weight = definition.BaseWeight; AetherLoad = definition.BaseAetherLoad;
            ShieldSourceIds = definition.TurnStartShield > 0 ? new[] { instance.InstanceId + ":turn_start_shield" } : Array.Empty<string>();
        }
    }

    public sealed class RogueInventoryItemPresentation
    {
        public string InstanceId { get; }
        public string DefinitionId { get; }
        public string DisplayName { get; }
        public bool IsEquipment { get; }
        public EquipmentSlot? Slot { get; }
        public EquipmentRarity? Rarity { get; }
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
        public bool Rotated { get; }
        public int ChargesCurrent { get; }
        public int ChargesMaximum { get; }
        public int QuickbarSlot { get; }
        public string CompactBadge => IsEquipment ? Rarity.ToString() : ChargesCurrent + "/" + ChargesMaximum;

        public RogueInventoryItemPresentation(RogueEquipmentRuntime runtime, string instanceId)
        {
            if (runtime == null || string.IsNullOrWhiteSpace(instanceId) || !runtime.Backpack.TryGetValue(instanceId, out RogueBackpackPlacement placement))
                throw new ArgumentException("Backpack item is required.", nameof(instanceId));
            InstanceId = instanceId; X = placement.X; Y = placement.Y; Rotated = placement.Rotated;
            RogueEquipmentInstance equipment = runtime.EquipmentItem(instanceId);
            RogueTacticalItemInstance tactical = runtime.TacticalItem(instanceId);
            IsEquipment = equipment != null;
            if (equipment != null)
            {
                EquipmentDefinition definition = runtime.DefinitionFor(instanceId);
                DefinitionId = equipment.DefinitionId; DisplayName = definition.DisplayName; Slot = definition.Slot; Rarity = equipment.Rarity;
                Width = placement.Rotated ? definition.Height : definition.Width; Height = placement.Rotated ? definition.Width : definition.Height;
            }
            else if (tactical != null)
            {
                TacticalItemDefinition definition = runtime.TacticalDefinitionFor(instanceId);
                DefinitionId = tactical.DefinitionId; DisplayName = definition.DisplayName; Width = placement.Rotated ? definition.Height : definition.Width;
                Height = placement.Rotated ? definition.Width : definition.Height; ChargesCurrent = tactical.ChargesCurrent; ChargesMaximum = tactical.ChargesMaximum;
            }
            else throw new ArgumentException("Unknown backpack item.", nameof(instanceId));
            QuickbarSlot = Array.IndexOf(runtime.ItemQuickbarInstanceIds, instanceId);
        }
    }

    public static class RogueInventoryPresentation
    {
        public static bool ShouldDrawSourceItem(string draggingInstanceId, string itemInstanceId)
            => string.IsNullOrEmpty(draggingInstanceId) || !string.Equals(draggingInstanceId, itemInstanceId, StringComparison.Ordinal);

        public static IReadOnlyList<RogueInventoryItemPresentation> Build(RogueEquipmentRuntime runtime)
        {
            if (runtime == null) return Array.Empty<RogueInventoryItemPresentation>();
            return runtime.Backpack.Keys.Select(id => new RogueInventoryItemPresentation(runtime, id))
                .OrderBy(value => value.Y).ThenBy(value => value.X).ThenBy(value => value.InstanceId, StringComparer.Ordinal).ToArray();
        }
    }

    public readonly struct RogueLoadoutGridPoint : IEquatable<RogueLoadoutGridPoint>
    {
        public int X { get; }
        public int Y { get; }
        public RogueLoadoutGridPoint(int x, int y) { X = x; Y = y; }
        public bool Equals(RogueLoadoutGridPoint other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is RogueLoadoutGridPoint other && Equals(other);
        public override int GetHashCode() => (X * 397) ^ Y;
    }

    public static class RogueLoadoutDragPresentation
    {
        public static RogueLoadoutGridPoint AnchorForLocalPointer(float localX, float localY, float cellSize, int grabX, int grabY)
        {
            if (cellSize <= 0f) throw new ArgumentOutOfRangeException(nameof(cellSize));
            int pointerX = (int)Math.Floor(localX / cellSize);
            int pointerY = (int)Math.Floor(-localY / cellSize);
            return new RogueLoadoutGridPoint(pointerX - grabX, pointerY - grabY);
        }

        public static RogueLoadoutGridPoint Footprint(int baseWidth, int baseHeight, bool rotated)
            => rotated ? new RogueLoadoutGridPoint(baseHeight, baseWidth) : new RogueLoadoutGridPoint(baseWidth, baseHeight);
    }
}
