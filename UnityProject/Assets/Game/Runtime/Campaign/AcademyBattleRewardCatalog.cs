using System;
using System.Collections.Generic;
using System.Linq;
using OCC.Combat.Roguelite;

namespace OCC.Combat
{
    public sealed class AcademyBattleRewardPackage
    {
        public IReadOnlyList<string> MainIds { get; }
        public IReadOnlyList<string> FollowupIds { get; }
        public IReadOnlyList<string> FixedMaterialIds { get; }

        public AcademyBattleRewardPackage(IEnumerable<string> mainIds, IEnumerable<string> followupIds,
            IEnumerable<string> fixedMaterialIds)
        {
            MainIds = (mainIds ?? Array.Empty<string>()).ToArray();
            FollowupIds = (followupIds ?? Array.Empty<string>()).ToArray();
            FixedMaterialIds = (fixedMaterialIds ?? Array.Empty<string>()).ToArray();
        }
    }

    public static class AcademyBattleRewardCatalog
    {
        public const string ForgeLoad = "FORGE-LOAD";
        public const string ForgeCircuit = "FORGE-CIRCUIT";
        public const string SpecAmplify = "SPEC-AMPLIFY";
        public const string SpecEfficient = "SPEC-EFFICIENT";
        public const string ForgePair = "REWARD-FORGE-PAIR";
        public const string SpecPair = "REWARD-SPEC-PAIR";
        public const string MixedPair = "REWARD-MIXED-PAIR";
        public const string ScrollPack = "REWARD-SCROLL-PACK";
        public const string RecoveryTool = "G-T06";

        private static readonly HashSet<string> ForgeNodes = new HashSet<string>(new[]
        { "N07", "N08", "N12", "N14", "N15" }, StringComparer.Ordinal);

        public static AcademyBattleRewardPackage Roll(int seed, string nodeId, IEnumerable<string> ownedIds)
        {
            RogueliteAcademyLayerCatalog.AcademyNodeContentMapping mapping = RogueliteAcademyLayerCatalog.Mapping(nodeId);
            string contentId = mapping.ContentTableId;
            RogueliteMapNode node = RogueliteAcademyLayerCatalog.TryResolveLayerNode(nodeId, out RogueliteMapNode resolved)
                ? resolved : throw new InvalidOperationException("Unknown academy reward node: " + nodeId);
            if (!node.IsCombat) throw new InvalidOperationException("Academy rewards require a battle node.");
            string source = node.Type == RogueliteMapNodeType.Finale ? "boss" : node.Type == RogueliteMapNodeType.Elite ? "elite" : "combat";
            bool high = node.Type != RogueliteMapNodeType.Combat || !string.Equals(contentId, "N01", StringComparison.Ordinal) &&
                !string.Equals(contentId, "N02", StringComparison.Ordinal);
            SpellRarity spellRarity = node.Type == RogueliteMapNodeType.Finale ? SpellRarity.Rare :
                node.Type == RogueliteMapNodeType.Elite ? (StableKey(seed, nodeId + "|elite-rarity") % 100 < 35 ? SpellRarity.Rare : SpellRarity.Uncommon) :
                high ? SpellRarity.Uncommon : SpellRarity.Common;
            EquipmentRarity equipmentRarity = node.Type == RogueliteMapNodeType.Finale ? EquipmentRarity.Rare :
                node.Type == RogueliteMapNodeType.Elite || high ? EquipmentRarity.Uncommon : EquipmentRarity.Common;
            int mainSeed = StableKey(seed, nodeId + "|main");
            List<string> main = BuildPair(mainSeed, source, spellRarity, equipmentRarity, ownedIds);
            IEnumerable<string> excludedArtifacts = (ownedIds ?? Array.Empty<string>())
                .Concat(node.Type == RogueliteMapNodeType.Elite ? new[] { RecoveryTool } : Array.Empty<string>());
            ArtifactDefinition artifact = ArtifactRewardPool.Roll(seed, StableKey(seed, nodeId + "|artifact"), node.Type, excludedArtifacts);
            string mobility = node.Type == RogueliteMapNodeType.Combat && StableKey(seed, nodeId + "|mobility") % 3 != 0
                ? StableKey(seed, nodeId + "|mobility") % 3 == 1 ? ScrollPack :
                    ForgeNodes.Contains(contentId) ? ForgePair : SpecPair
                : artifact.Id;
            main.Add(mobility);

            string[] fixedMaterials = node.Type == RogueliteMapNodeType.Elite
                ? new[] { contentId == "E02" ? SpecAmplify : contentId == "E03" ? ForgeCircuit : ForgeLoad }
                : Array.Empty<string>();
            int dropChance = node.Type == RogueliteMapNodeType.Elite ? 100 :
                node.Type == RogueliteMapNodeType.Finale ? 0 : high ? 40 : 25;
            string[] followup = StableKey(seed, nodeId + "|drop-check") % 100 < dropChance
                ? new[] { RollExtraDrop(seed, nodeId, node.Type, source, equipmentRarity,
                    (ownedIds ?? Array.Empty<string>()).Concat(main).Concat(fixedMaterials)) }
                : Array.Empty<string>();
            if (main.Count != 3 || followup.Length > 1)
                throw new InvalidOperationException("Academy reward pool is incomplete for " + nodeId + ".");
            return new AcademyBattleRewardPackage(main, followup, fixedMaterials);
        }

        private static string RollExtraDrop(int seed, string nodeId, RogueliteMapNodeType nodeType,
            string source, EquipmentRarity equipmentRarity, IEnumerable<string> excludedIds)
        {
            string[] excluded = excludedIds.ToArray();
            int category = StableKey(seed, nodeId + "|drop-category") % 3;
            if (category == 0)
            {
                string equipment = BuildPair(StableKey(seed, nodeId + "|drop-equipment"), source,
                    SpellRarity.Common, equipmentRarity, excluded).Skip(1).FirstOrDefault();
                if (!string.IsNullOrEmpty(equipment) && !excluded.Contains(equipment)) return equipment;
            }
            if (category == 1)
            {
                ArtifactDefinition artifact = ArtifactRewardPool.Roll(seed,
                    StableKey(seed, nodeId + "|drop-artifact"), nodeType, excluded);
                if (artifact != null && !excluded.Contains(artifact.Id)) return artifact.Id;
            }
            string[] materials = { ForgeLoad, ForgeCircuit, SpecAmplify, SpecEfficient };
            materials = materials.Where(id => !excluded.Contains(id)).ToArray();
            if (materials.Length == 0) materials = new[] { ForgeLoad, ForgeCircuit, SpecAmplify, SpecEfficient };
            return materials[StableKey(seed, nodeId + "|drop-material") % materials.Length];
        }

        public static RogueliteReward Resource(string id)
        {
            switch (id)
            {
                case ForgeLoad: return new RogueliteReward(id, "承力合金", id, 1, "装备锻造材料");
                case ForgeCircuit: return new RogueliteReward(id, "导能晶片", id, 1, "装备锻造材料");
                case SpecAmplify: return new RogueliteReward(id, "增幅刻墨", id, 1, "术式专精材料");
                case SpecEfficient: return new RogueliteReward(id, "节流刻墨", id, 1, "术式专精材料");
                case ForgePair: return new RogueliteReward(id, "锻造材料两件", id, 2, "承力合金×1＋导能晶片×1");
                case SpecPair: return new RogueliteReward(id, "专精材料两件", id, 2, "增幅刻墨×1＋节流刻墨×1");
                case MixedPair: return new RogueliteReward(id, "强化材料包", id, 2, "承力合金×1＋增幅刻墨×1");
                case ScrollPack: return new RogueliteReward(id, "火线卷轴", id, 1, "火线卷轴×1");
                default: return null;
            }
        }

        private static List<string> BuildPair(int seed, string source, SpellRarity spellRarity,
            EquipmentRarity equipmentRarity, IEnumerable<string> ownedIds)
        {
            RogueAcademyContentService service = new RogueAcademyContentService();
            // The spell pool has an elite source, while the equipment catalog uses combat/boss.
            string equipmentSource = source == "elite" ? "combat" : source;
            string spell = service.Roll(seed, source, spellRarity, equipmentRarity, 1, 0, ownedIds)
                .Select(value => value.DefinitionId).FirstOrDefault();
            string equipment = service.Roll(seed + 1, equipmentSource, spellRarity, equipmentRarity, 0, 1, ownedIds)
                .Select(value => value.DefinitionId).FirstOrDefault();
            if (spell == null) spell = service.Roll(seed, source, SpellRarity.Common, equipmentRarity, 1, 0, null)
                .Select(value => value.DefinitionId).FirstOrDefault();
            if (equipment == null) equipment = service.Roll(seed + 1, equipmentSource, spellRarity,
                EquipmentRarity.Common, 0, 1, null).Select(value => value.DefinitionId).FirstOrDefault();
            return new[] { spell, equipment }.Where(value => value != null).ToList();
        }

        private static int StableKey(int seed, string value)
        {
            unchecked
            {
                uint hash = 2166136261u ^ (uint)seed;
                foreach (char c in value ?? string.Empty) { hash ^= c; hash *= 16777619u; }
                return (int)(hash & 0x7fffffff);
            }
        }
    }
}
