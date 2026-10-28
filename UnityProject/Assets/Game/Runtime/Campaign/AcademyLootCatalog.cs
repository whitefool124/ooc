using System;
using System.Collections.Generic;
using System.Linq;
using OCC.Combat.Roguelite;
using RogueEquipmentSlot = OCC.Combat.Roguelite.EquipmentSlot;

namespace OCC.Combat
{
    public enum BattlefieldChestTier { Fixed, Common, Refined, Cache }

    /// <summary>First-stage optional containers. Contents are generated once, independently of battle rewards.</summary>
    public static class AcademyLootCatalog
    {
        public const string GoldBag = "LOOT-GOLD-2";
        public const string ManuscriptPrefix = "LOOT-SPELL:";
        private static readonly Lazy<RogueContentCatalog> Content = new Lazy<RogueContentCatalog>(RogueContentCatalog.CreateAcademyV01);
        private static readonly string[] Materials = { "FORGE-LOAD", "FORGE-CIRCUIT", "SPEC-AMPLIFY", "SPEC-EFFICIENT" };
        public static bool IsMaterial(string id) => Materials.Contains(id);
        public static int Durability(BattlefieldChestTier tier) => tier == BattlefieldChestTier.Common ? 8 : tier == BattlefieldChestTier.Refined ? 12 : tier == BattlefieldChestTier.Cache ? 16 : 0;
        public static string Name(BattlefieldChestTier tier) => tier == BattlefieldChestTier.Common ? "补给箱" : tier == BattlefieldChestTier.Refined ? "精制箱" : tier == BattlefieldChestTier.Cache ? "秘藏箱" : "固定物资箱";

        public static BattlefieldChestTier PlannedTier(RogueliteMapRun run)
        {
            if (run == null || !run.IsInAcademyLayerPhase || run.HasPendingContentCombat) return BattlefieldChestTier.Fixed;
            for (int tier = 1; tier <= 3; tier++)
            {
                int category = tier;
                RogueliteMapNode selected = run.MapNodes.Where(node =>
                {
                    if (category == 3) return node.Type == RogueliteMapNodeType.Elite;
                    if (node.Type != RogueliteMapNodeType.Combat) return false;
                    string id = RogueliteAcademyLayerCatalog.Mapping(node.Id).ContentTableId;
                    bool weak = id == "N01" || id == "N02";
                    return category == 1 ? weak : !weak;
                }).OrderBy(node => Hash(run.Seed + "|chest-node|" + category, node.Id)).ThenBy(node => node.Id, StringComparer.Ordinal).FirstOrDefault();
                if (selected?.Id == run.CurrentNodeId) return (BattlefieldChestTier)tier;
            }
            return BattlefieldChestTier.Fixed;
        }

        public static GridPosition? SafePosition(CombatState state, GridPosition preferred, BattlefieldChestTier tier)
        {
            UnitState hero = state.GetUnit("hero");
            GridPosition[] offsets = { new GridPosition(1, 0), new GridPosition(-1, 0), new GridPosition(0, 1), new GridPosition(0, -1) };
            // A box beside the spawn leaves an immediate search option. Never seal a route or put it on an opening attack line.
            foreach (GridPosition candidate in new[] { preferred }.Concat(offsets.Select(d => hero.Position + d)).Distinct())
            {
                if (!state.Map.IsInside(candidate) || state.IsOccupied(candidate) || candidate.ManhattanDistance(hero.Position) != 1) continue;
                TileState tile = state.Map.GetTile(candidate);
                if ((!tile.IsLootChest && tile.BlocksMovement) || tile.HasEffectLayer || tile.IsDeviceLike || tile.IsObjective) continue;
                var reached = new HashSet<GridPosition> { hero.Position };
                var queue = new Queue<GridPosition>(); queue.Enqueue(hero.Position);
                // Flood fill uses terrain only: unit occupancy must not turn a currently occupied passage into a false dead end.
                while (queue.Count > 0)
                {
                    GridPosition current = queue.Dequeue();
                    foreach (GridPosition next in offsets.Select(d => current + d))
                        if (next != candidate && state.Map.IsInside(next) && !state.Map.GetTile(next).BlocksMovement && reached.Add(next)) queue.Enqueue(next);
                }
                if (state.Units.Values.Where(e => !e.IsHero && e.IsAlive).All(e => reached.Contains(e.Position)) &&
                    HasOpeningIntervention(state, candidate, tier)) return candidate;
            }
            return null;
        }

        private static bool HasOpeningIntervention(CombatState state, GridPosition position, BattlefieldChestTier tier)
        {
            CombatState simulation = state.Clone();
            simulation.SetLootSource(new LootSourceState("container-safety", position, Array.Empty<ItemInstance>(), tier));
            var plans = new EnemyTurnPlanBook(); var executor = new CombatCommandExecutionService();
            CombatResolver.AdvanceToNextTurn(simulation);
            for (int step = 0; step < 32 && !simulation.IsDefeat && !simulation.LootSource.IsDestroyed; step++)
            {
                UnitState unit = simulation.GetUnit(simulation.ActiveUnitId);
                if (unit == null) return false;
                if (unit.IsHero) return unit.IsAlive && unit.ActionPoints >= 1 && unit.Position.ManhattanDistance(position) == 1;
                var command = plans.GetExecutionCommand(simulation, unit, simulation.GetUnit("hero"));
                var result = executor.Execute(simulation, simulation.RogueSpells?.FireBattle, command, true);
                if (!result.Accepted) return false;
                if (simulation.ActiveUnitId == unit.Id && !simulation.IsDefeat && !simulation.IsVictory) CombatResolver.EndTurn(simulation, unit);
            }
            return false;
        }

        public static LootSourceState Create(int seed, string nodeId, GridPosition position, BattlefieldChestTier tier)
        {
            if (tier == BattlefieldChestTier.Fixed) throw new ArgumentException("Fixed containers use their authored contents.");
            string key = "academy-chest-v1:" + seed + ":" + nodeId;
            List<string> ids = new List<string>();
            string Pick(IEnumerable<string> pool, string salt) => pool.Distinct().Where(id => !ids.Contains(id))
                .OrderBy(id => Hash(key + salt, id)).ThenBy(id => id, StringComparer.Ordinal).First();
            IEnumerable<string> Equipment() => Content.Value.Equipment.Where(e => e.AllowedRarities.Contains(EquipmentRarity.Uncommon) &&
                e.SourceTypes.Contains("combat") && EquipmentSlotRules.IsActive(e.Slot) &&
                e.Slot != RogueEquipmentSlot.Ring1 && e.Slot != RogueEquipmentSlot.Ring2 && e.Slot != RogueEquipmentSlot.Necklace).Select(e => e.DefinitionId);
            IEnumerable<string> Spells(bool high) => Content.Value.Spells.Where(s => s.RewardEligible &&
                s.RewardSources.Contains("combat") && (high ? s.Rarity != SpellRarity.Common : s.Rarity == SpellRarity.Common || s.Rarity == SpellRarity.Uncommon))
                .Select(s => ManuscriptPrefix + s.DefinitionId);
            IEnumerable<string> Artifacts() => ArtifactCatalog.All.Where(a => ArtifactCatalog.IsCurrentlyUsable(a.Id) &&
                (a.ContentSources & ArtifactContentSource.Loot) != 0).Select(a => a.Id);
            if (tier == BattlefieldChestTier.Common)
            {
                string[][] categories = { new[] { GoldBag }, new[] { "F-S01" }, new[] { AcademyBattleRewardCatalog.RecoveryTool }, Materials };
                foreach (int category in Enumerable.Range(0, categories.Length).OrderBy(i => Hash(key, i.ToString())).Take(2))
                    ids.Add(Pick(categories[category], "common:" + category));
            }
            else if (tier == BattlefieldChestTier.Refined)
            {
                ids.Add(Pick(Materials, "material"));
                IEnumerable<string> pool = Equipment().Concat(Spells(false)).Concat(Artifacts()).Concat(new[] { "F-S01" });
                ids.Add(Pick(pool, "refined-1")); ids.Add(Pick(pool, "refined-2"));
            }
            else
            {
                ids.Add(Pick(Spells(true).Concat(Equipment()).Concat(ArtifactCatalog.All.Where(a =>
                    (a.ContentSources & ArtifactContentSource.Loot) != 0 && a.Rarity >= ItemRarity.Uncommon).Select(a => a.Id)), "guarantee"));
                ids.Add(Pick(Materials.Take(2), "forge"));
                ids.Add(Pick(Materials.Skip(2), "specialization"));
                ids.Add(Pick(Equipment().Concat(Artifacts()).Concat(new[] { "F-S01" }), "cache-last"));
            }
            return new LootSourceState(key, position, ids.Select((id, i) => new ItemInstance(key + ":" + i, id, i)), tier);
        }

        public static ItemDefinition FindItem(string id)
        {
            if (id == GoldBag) return new ItemDefinition(id, "金币袋", "领取获得2金币。", ItemCategory.Material, ItemRarity.Common, iconPath: "Art/FormalResourceIcons12/gold");
            if (IsMaterial(id)) return new ItemDefinition(id, AcademyBattleRewardCatalog.Resource(id).DisplayName,
                "强化材料×1", ItemCategory.Material, ItemRarity.Common, iconPath: "Art/FormalIcons32/loot");
            if (id != null && id.StartsWith(ManuscriptPrefix, StringComparison.Ordinal))
            {
                SpellDefinition spell = Content.Value.Spells.FirstOrDefault(s => s.DefinitionId == id.Substring(ManuscriptPrefix.Length));
                if (spell != null) return new ItemDefinition(id, spell.DisplayName + "抄本", "领取后掌握该术式，战斗中不能更换术式。", ItemCategory.Scroll,
                    spell.Rarity == SpellRarity.Common ? ItemRarity.Common : spell.Rarity == SpellRarity.Uncommon ? ItemRarity.Uncommon : ItemRarity.Rare,
                    iconPath: "Art/FormalItemIcons32/fire_scroll");
            }
            EquipmentDefinition equipment = Content.Value.Equipment.FirstOrDefault(e => e.DefinitionId == id);
            return equipment == null ? null : new ItemDefinition(id, equipment.DisplayName, "学院装备", ItemCategory.Armor,
                equipment.AllowedRarities.Contains(EquipmentRarity.Uncommon) ? ItemRarity.Uncommon : ItemRarity.Common, width: equipment.Width, height: equipment.Height,
                iconPath: FormalArtRegistry.EquipmentIconPath(id));
        }

        private static uint Hash(string prefix, string id)
        {
            unchecked { uint hash = 2166136261; foreach (char c in prefix + "|" + id) { hash ^= c; hash *= 16777619; } return hash; }
        }
    }
}
