using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat.Roguelite
{
    public static class RogueRuntimeConstants
    {
        public const string SaveVersion = "rogue11";
        public const int SpellSlotCount = 8;
        public const int ItemQuickbarSize = 4;
        public const int MaximumPersonalMana = 12;
        public const int MaximumPercentageReduction = 50;
    }

    public enum DamageComponentKind { Physical, Fire, Aether, Environment, TrueHealthLoss }
    public enum DamageTag { Melee, Ranged, Explosion, Ground, Object, Segment }
    public enum ReductionCategory { Stance, Reaction, EnvironmentException }
    public enum ShieldEventKind { Granted, PreventedByBreakStance, Absorbed, ClearedAtTurnStart, Wasted }
    public enum EquipmentSlot { MainHand, OffHand, Head, Chest, Hands, Legs, Backpack, AetherCore, Conduit, Accessory1, Accessory2 }
    public enum EquipmentHandedness { None, OneHanded, TwoHanded, OffHand }
    public enum EquipmentRarity { Common, Uncommon, Rare, Legendary }
    public enum SpellRarity { Basic, Common, Uncommon, Rare }

    public sealed class DamageComponent
    {
        public DamageComponentKind Kind { get; }
        public int RawAmount { get; }
        public DamageComponent(DamageComponentKind kind, int rawAmount)
        {
            if (rawAmount < 0) throw new ArgumentOutOfRangeException(nameof(rawAmount));
            Kind = kind; RawAmount = rawAmount;
        }
    }

    public sealed class PercentageReductionEffect
    {
        public string SourceId { get; }
        public ReductionCategory Category { get; }
        public int Percent { get; }
        public IReadOnlyList<DamageTag> AppliesToTags { get; }
        public PercentageReductionEffect(string sourceId, ReductionCategory category, int percent, params DamageTag[] appliesToTags)
        {
            if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("Reduction source id is required.", nameof(sourceId));
            if (percent < 0 || percent > 100) throw new ArgumentOutOfRangeException(nameof(percent));
            SourceId = sourceId; Category = category; Percent = percent;
            AppliesToTags = appliesToTags ?? Array.Empty<DamageTag>();
        }
    }

    public sealed class DamagePacket
    {
        public string PacketId { get; }
        public string SourceUnitId { get; }
        public string TargetUnitId { get; }
        public string SourceEffectId { get; }
        public IReadOnlyList<DamageComponent> Components { get; }
        public IReadOnlyList<DamageTag> Tags { get; }
        public IReadOnlyList<PercentageReductionEffect> ReductionEffects { get; }
        public int SegmentIndex { get; }
        public int SegmentCount { get; }

        public DamagePacket(string packetId, string sourceUnitId, string targetUnitId, string sourceEffectId,
            IEnumerable<DamageComponent> components, IEnumerable<DamageTag> tags = null,
            IEnumerable<PercentageReductionEffect> reductionEffects = null, int segmentIndex = 0, int segmentCount = 1)
        {
            if (string.IsNullOrWhiteSpace(packetId) || string.IsNullOrWhiteSpace(targetUnitId) || string.IsNullOrWhiteSpace(sourceEffectId))
                throw new ArgumentException("Damage packet identity is incomplete.");
            if (segmentCount < 1 || segmentIndex < 0 || segmentIndex >= segmentCount) throw new ArgumentOutOfRangeException(nameof(segmentIndex));
            PacketId = packetId; SourceUnitId = sourceUnitId ?? string.Empty; TargetUnitId = targetUnitId; SourceEffectId = sourceEffectId;
            Components = (components ?? throw new ArgumentNullException(nameof(components))).ToArray();
            if (Components.Count == 0) throw new ArgumentException("Damage packet requires at least one component.", nameof(components));
            Tags = (tags ?? Array.Empty<DamageTag>()).Distinct().ToArray();
            ReductionEffects = (reductionEffects ?? Array.Empty<PercentageReductionEffect>()).ToArray();
            SegmentIndex = segmentIndex; SegmentCount = segmentCount;
        }
    }

    public sealed class DamageResolution
    {
        public int RawTotal { get; }
        public int ReductionRate { get; }
        public int PercentageReduced { get; }
        public int AfterReduction { get; }
        public int ShieldBefore { get; }
        public int ShieldAbsorbed { get; }
        public int HealthBefore { get; }
        public int HealthDamage { get; }
        public bool TargetDefeated { get; }
        public DamageResolution(int rawTotal, int reductionRate, int percentageReduced, int afterReduction, int shieldBefore,
            int shieldAbsorbed, int healthBefore, int healthDamage, bool targetDefeated)
        {
            RawTotal = rawTotal; ReductionRate = reductionRate; PercentageReduced = percentageReduced; AfterReduction = afterReduction;
            ShieldBefore = shieldBefore; ShieldAbsorbed = shieldAbsorbed; HealthBefore = healthBefore; HealthDamage = healthDamage;
            TargetDefeated = targetDefeated;
        }
    }

    public sealed class ShieldSourceRecord
    {
        public string SourceId { get; }
        public int Amount { get; }
        public ShieldEventKind EventKind { get; }
        public int TriggerTurn { get; }
        public ShieldSourceRecord(string sourceId, int amount, ShieldEventKind eventKind, int triggerTurn = 0)
        { SourceId = sourceId ?? throw new ArgumentNullException(nameof(sourceId)); Amount = amount; EventKind = eventKind; TriggerTurn = triggerTurn; }
    }

    public sealed class BreakStanceState
    {
        public string TargetUnitId { get; }
        public int ExpiresAfterTurnSequence { get; private set; }
        public bool IsActive { get; private set; } = true;
        public BreakStanceState(string targetUnitId, int expiresAfterTurnSequence)
        { TargetUnitId = targetUnitId ?? throw new ArgumentNullException(nameof(targetUnitId)); ExpiresAfterTurnSequence = expiresAfterTurnSequence; }
        public void Refresh(int expiresAfterTurnSequence) { ExpiresAfterTurnSequence = Math.Max(ExpiresAfterTurnSequence, expiresAfterTurnSequence); IsActive = true; }
        public void Clear() { IsActive = false; }
    }

    public sealed class SpellDefinition
    {
        public string DefinitionId { get; }
        public string DisplayName { get; }
        public string Element { get; }
        public string Role { get; }
        public SpellRarity Rarity { get; }
        public int ActionPointCost { get; }
        public int ManaCost { get; }
        public int CooldownOwnTurns { get; }
        public string Targeting { get; }
        public int Range { get; }
        public string LineOfSightRule { get; }
        public IReadOnlyList<string> Rules { get; }
        public IReadOnlyList<string> CompatibilityTags { get; }
        public IReadOnlyList<string> RewardSources { get; }
        public bool RewardEligible { get; }
        public string EquivalenceGroupId { get; }
        public string CatalogVersion { get; }
        public bool IsBasic { get; }

        public SpellDefinition(string definitionId, string displayName, string element, string role, SpellRarity rarity,
            int actionPointCost, int manaCost, int cooldownOwnTurns, string targeting, int range, string lineOfSightRule,
            IEnumerable<string> rules, IEnumerable<string> compatibilityTags, IEnumerable<string> rewardSources,
            bool rewardEligible, string equivalenceGroupId, string catalogVersion, bool isBasic = false)
        {
            DefinitionId = definitionId; DisplayName = displayName; Element = element; Role = role; Rarity = rarity;
            ActionPointCost = actionPointCost; ManaCost = manaCost; CooldownOwnTurns = cooldownOwnTurns;
            Targeting = targeting; Range = range; LineOfSightRule = lineOfSightRule;
            Rules = (rules ?? Array.Empty<string>()).ToArray(); CompatibilityTags = (compatibilityTags ?? Array.Empty<string>()).ToArray();
            RewardSources = (rewardSources ?? Array.Empty<string>()).ToArray(); RewardEligible = rewardEligible;
            EquivalenceGroupId = equivalenceGroupId ?? string.Empty; CatalogVersion = catalogVersion; IsBasic = isBasic;
        }
    }

    public sealed class UpgradeNodeDefinition
    {
        public string NodeId { get; }
        public string BranchAEffectId { get; }
        public string BranchBEffectId { get; }
        public UpgradeNodeDefinition(string nodeId, string branchAEffectId, string branchBEffectId)
        { NodeId = nodeId; BranchAEffectId = branchAEffectId; BranchBEffectId = branchBEffectId; }
    }

    public sealed class EquipmentDefinition
    {
        public string DefinitionId { get; }
        public string DisplayName { get; }
        public EquipmentSlot Slot { get; }
        public EquipmentHandedness Handedness { get; }
        public IReadOnlyList<EquipmentRarity> AllowedRarities { get; }
        public int Width { get; }
        public int Height { get; }
        public bool Rotatable { get; }
        public int BaseWeight { get; }
        public int BaseAetherLoad { get; }
        public IReadOnlyList<string> BaseActionIds { get; }
        public IReadOnlyList<string> FixedEffectIds { get; }
        public IReadOnlyList<string> AffixPoolIds { get; }
        public IReadOnlyList<UpgradeNodeDefinition> UpgradeNodes { get; }
        public string SourceStage { get; }
        public IReadOnlyList<string> SourceTypes { get; }
        public string UniqueGroupId { get; }
        public string ContentVersion { get; }
        public int TurnStartShield { get; }
        public bool HasDurability { get; }
        public int Armor { get; }
        public int BlockChance { get; }

        public EquipmentDefinition(string definitionId, string displayName, EquipmentSlot slot, EquipmentHandedness handedness,
            EquipmentRarity rarity, int width, int height, int baseWeight, int baseAetherLoad, int turnStartShield,
            IEnumerable<string> baseActionIds, IEnumerable<string> fixedEffectIds, IEnumerable<string> affixPoolIds = null,
            IEnumerable<UpgradeNodeDefinition> upgradeNodes = null, IEnumerable<string> sourceTypes = null, string uniqueGroupId = "")
        {
            DefinitionId = definitionId; DisplayName = displayName; Slot = slot; Handedness = handedness;
            AllowedRarities = new[] { rarity }; Width = width; Height = height; Rotatable = true;
            BaseWeight = baseWeight; BaseAetherLoad = baseAetherLoad; TurnStartShield = turnStartShield;
            BaseActionIds = (baseActionIds ?? Array.Empty<string>()).ToArray(); FixedEffectIds = (fixedEffectIds ?? Array.Empty<string>()).ToArray();
            AffixPoolIds = (affixPoolIds ?? Array.Empty<string>()).ToArray(); UpgradeNodes = (upgradeNodes ?? Array.Empty<UpgradeNodeDefinition>()).ToArray();
            SourceStage = "academy"; SourceTypes = (sourceTypes ?? new[] { "reward" }).ToArray(); UniqueGroupId = uniqueGroupId ?? string.Empty;
            ContentVersion = "academy-equipment-v0.1"; HasDurability = false; Armor = 0; BlockChance = 0;
        }
    }

    public sealed class AffixDefinition
    {
        public string AffixId { get; }
        public string DisplayName { get; }
        public IReadOnlyList<EquipmentSlot> LegalSlots { get; }
        public EquipmentRarity MinimumRarity { get; }
        public EquipmentRarity? ExactRarity { get; }
        public string EffectId { get; }
        public string MutualExclusionGroup { get; }
        public AffixDefinition(string affixId, string displayName, IEnumerable<EquipmentSlot> legalSlots,
            EquipmentRarity minimumRarity, string effectId, string mutualExclusionGroup, EquipmentRarity? exactRarity = null)
        { AffixId = affixId; DisplayName = displayName; LegalSlots = legalSlots.ToArray(); MinimumRarity = minimumRarity; EffectId = effectId; MutualExclusionGroup = mutualExclusionGroup; ExactRarity = exactRarity; }
    }

    public sealed class TacticalItemDefinition
    {
        public string DefinitionId { get; }
        public string DisplayName { get; }
        public int Width { get; }
        public int Height { get; }
        public int MaximumCharges { get; }
        public int ActionPointCost { get; }
        public TacticalItemDefinition(string definitionId, string displayName, int width, int height, int maximumCharges, int actionPointCost)
        { DefinitionId = definitionId; DisplayName = displayName; Width = width; Height = height; MaximumCharges = maximumCharges; ActionPointCost = actionPointCost; }
    }
}
