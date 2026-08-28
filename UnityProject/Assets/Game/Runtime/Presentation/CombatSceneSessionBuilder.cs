using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OCC.Combat.Roguelite;

namespace OCC.Combat.Presentation
{
    public sealed class CombatSceneSessionBuild
    {
        public CombatState State { get; }
        public MissionPreparation Preparation { get; }
        public FirstRegionLevelDefinition Level { get; }

        public CombatSceneSessionBuild(CombatState state, MissionPreparation preparation,
            FirstRegionLevelDefinition level)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Preparation = preparation ?? throw new ArgumentNullException(nameof(preparation));
            Level = level;
        }
    }

    /// <summary>
    /// Builds one production combat state from either a formal level definition or the legacy scene markers.
    /// </summary>
    public sealed class CombatSceneSessionBuilder
    {
        public CombatSceneSessionBuild Build(RogueliteMapRun mapRun, RogueliteDeveloperRun developerRun,
            IEnumerable<CombatSceneMarker> sceneMarkers, MissionPreparation fallbackPreparation = null)
        {
            string encounterId = mapRun != null
                ? (mapRun.HasPendingContentCombat ? mapRun.PendingContentCombatMissionId : mapRun.CurrentNodeId)
                : developerRun?.CurrentMission.Id;
            RogueliteEncounterDefinition encounter = string.IsNullOrEmpty(encounterId)
                ? null
                : RogueliteEncounterCatalog.For(mapRun, encounterId);
            string levelId = encounter?.LevelId ?? encounterId;
            FirstRegionLevelDefinition currentLevel;
            CombatState state;
            GridMap map;
            if (FirstRegionLevelCatalog.TryFor(levelId, out FirstRegionLevelDefinition level))
            {
                FirstRegionLevelDefinition resolvedLevel = encounter == null ? level : BindEncounterToLevel(level, encounter);
                FirstRegionLevelBuild build = FirstRegionLevelBuilder.Build(resolvedLevel, "core_overseer");
                currentLevel = build.Definition;
                state = build.State;
                map = state.Map;
            }
            else
            {
                if (mapRun != null && encounter != null)
                    throw new InvalidOperationException("Encounter package " + encounter.VariantKey + " references unknown level " + levelId + ".");
                currentLevel = null;
                map = new GridMap(12, 9);
                CombatSceneMarker[] markers = (sceneMarkers ?? Array.Empty<CombatSceneMarker>()).ToArray();
                foreach (CombatSceneMarker marker in markers)
                {
                    GridPosition position = ScenePosition(marker);
                    if (marker.MarkerType == CombatSceneMarkerType.LightCover) map.SetTile(position, new TileState { Cover = CoverType.Light, Durability = 4 });
                    if (marker.MarkerType == CombatSceneMarkerType.HeavyCover) map.SetTile(position, new TileState { Cover = CoverType.Heavy, Durability = 7 });
                    if (marker.MarkerType == CombatSceneMarkerType.Objective) map.SetTile(position, new TileState { IsObjective = true, IsDevice = true, Durability = 6 });
                }
                List<UnitState> units = new List<UnitState>();
                int enemyIndex = 0;
                foreach (CombatSceneMarker marker in markers.Where(value => value.MarkerType == CombatSceneMarkerType.Unit)
                    .OrderBy(value => value.name, StringComparer.Ordinal))
                {
                    bool hero = marker.name.Contains("主角");
                    if (!hero && encounter != null && enemyIndex >= encounter.EnemyArchetypeIds.Count) continue;
                    UnitState unit = new UnitState(hero ? "hero" : "enemy_" + enemyIndex, hero,
                        ScenePosition(marker), hero ? Facing.East : Facing.West);
                    if (hero)
                    {
                        unit.DisplayName = "阿斯特拉";
                        unit.Speed = 11;
                        unit.Equip(CombatCatalog.Hammer, CombatCatalog.Shield, CombatCatalog.FireBolt, CombatCatalog.FrostBind);
                    }
                    else
                    {
                        string archetypeId = encounter == null
                            ? EnemyArchetypes.All[Math.Min(enemyIndex, EnemyArchetypes.All.Count - 1)].Id
                            : encounter.EnemyArchetypeIds[Math.Min(enemyIndex, encounter.EnemyArchetypeIds.Count - 1)];
                        EnemyArchetypes.Get(archetypeId).Apply(unit);
                        enemyIndex++;
                    }
                    units.Add(unit);
                }
                if (units.Count == 0 || !units.Any(unit => unit.IsHero)) return null;
                state = new CombatState(map, units);
            }

            MissionPreparation preparation = mapRun == null && developerRun == null
                ? (fallbackPreparation ?? throw new ArgumentNullException(nameof(fallbackPreparation))).Clone()
                : PrepareMission(state, map, currentLevel, encounter, mapRun, developerRun);
            if (mapRun != null)
            {
                state.ConfigureRuleset(CombatRuleset.Roguelite);
                RogueAcademyContentService academyContent = new RogueAcademyContentService();
                foreach (UnitState enemy in state.Units.Values.Where(unit => !unit.IsHero)) academyContent.ApplyEnemyBaseline(state, enemy);
                string[] mastered = new[] { "BASE-FIRE-MELEE", "BASE-FIRE-RANGED", "BASE-AETHER-SHIELD", "BASE-MANA-RECOVER" }
                    .Concat(mapRun.OwnedFireSpellIds).Distinct(StringComparer.Ordinal).ToArray();
                RogueSpellLoadout loadout = RogueSpellLoadout.Restore(mastered, mapRun.RogueEquippedSpellIds, true);
                state.AttachRogueSpellRuntime(new RogueSpellCombatRuntime(state, loadout));
                state.AttachRogueEquipmentRuntime(mapRun.RogueRunState == null ? RogueEquipmentRuntime.CreateStarter(mapRun.Seed) : RogueEquipmentRuntime.FromDto(mapRun.RogueRunState));
            }
            if (mapRun == null || !mapRun.UsesRogue11) ConfigureCombatInventory(state, mapRun, developerRun);
            ApplyShortRunChoices(state, developerRun);
            mapRun?.ApplyBuild(state.GetUnit("hero"));
            ConfigureLoot(state, mapRun);
            return new CombatSceneSessionBuild(state, preparation, currentLevel);
        }

        public static FirstRegionLevelDefinition BindEncounterToLevel(FirstRegionLevelDefinition level, RogueliteEncounterDefinition encounter)
        {
            IReadOnlyList<GridPosition> spawnPositions = encounter.Layout?.EnemySpawns ?? level.EnemyPlacements.Select(value => value.Position).ToArray();
            if (encounter.EnemyArchetypeIds.Count > spawnPositions.Count)
                throw new InvalidOperationException("Encounter " + encounter.VariantKey + " has more enemies than registered spawn positions in " + level.Id + ".");
            LevelEnemyPlacement[] enemies = encounter.EnemyArchetypeIds.Select((archetypeId, index) =>
            {
                GridPosition spawn = spawnPositions[index];
                Facing facing = encounter.Layout == null ? level.EnemyPlacements[index].Facing : Facing.West;
                return new LevelEnemyPlacement(archetypeId, spawn.X, spawn.Y, facing);
            }).ToArray();
            RogueliteEncounterLayout layout = encounter.Layout;
            GridPosition heroSpawn = layout?.HeroSpawn ?? level.HeroSpawn;
            IReadOnlyList<LevelTerrainPlacement> terrain = layout?.Terrain ?? level.Terrain;
            CombatObjectiveType objectiveType = layout == null ? level.ObjectiveType : CombatObjectiveType.Elimination;
            IReadOnlyList<GridPosition> routeAnchors = layout == null ? level.SpaceContract.RouteAnchors :
                new[] { new GridPosition(Math.Max(0, heroSpawn.X - 2), heroSpawn.Y), new GridPosition(Math.Min(layout.Width - 1, heroSpawn.X + 2), heroSpawn.Y) };
            string objectiveSummary = string.IsNullOrEmpty(encounter.ObjectiveSummary) ? level.ObjectiveSummary : encounter.ObjectiveSummary;
            return new FirstRegionLevelDefinition(level.Id, level.DisplayName, objectiveSummary, objectiveType,
                level.Tier, heroSpawn, level.FloorTheme, encounter.IsElite, encounter.IsBoss,
                level.PrerequisiteLevelIds, enemies, terrain,
                new LevelSpaceContract(encounter.SpatialGrammar, routeAnchors,
                    encounter.PublicRisk, encounter.SpawnRelationship), level.Width, level.Height, level.BlockedPositions);
        }

        private static MissionPreparation PrepareMission(CombatState state, GridMap map,
            FirstRegionLevelDefinition currentLevel, RogueliteEncounterDefinition encounter,
            RogueliteMapRun mapRun, RogueliteDeveloperRun developerRun)
        {
            RogueliteMissionDefinition mission = mapRun != null
                ? RogueliteDeveloperCatalog.FindMission(encounter?.LevelId ?? (mapRun.HasPendingContentCombat
                    ? mapRun.PendingContentCombatMissionId
                    : mapRun.CurrentNodeId))
                : developerRun.CurrentMission;
            string enemySummary = currentLevel == null
                ? (mapRun != null ? DescribeEncounter(encounter, mission.EnemySummary) : mission.EnemySummary)
                : DescribeEncounter(encounter, mapRun != null
                    ? currentLevel.EnemySummary(mapRun.RegionBossId)
                    : currentLevel.EnemySummary());
            MissionPreparation preparation = new MissionPreparation().Configure(mission.Id,
                currentLevel?.ObjectiveSummary ?? mission.ObjectiveSummary, enemySummary);
            if (currentLevel == null)
            {
                if (mission.ObjectiveType == CombatObjectiveType.Elimination)
                    state.ConfigureObjectives(new EliminationObjective(mission.Id + "_objective"));
                else
                    state.ConfigureObjectives(new DestructionObjective(
                        map.PositionsWith(tile => tile.IsObjective), mission.Id + "_objective"));
            }
            return preparation;
        }

        private static string DescribeEncounter(RogueliteEncounterDefinition encounter, string fallback)
        {
            if (encounter == null) return fallback;
            string summary = string.Join("、", encounter.EnemyArchetypeIds
                .Select(id => EnemyArchetypes.Get(id).DisplayName));
            return (encounter.IsBoss ? "区域首领：" : encounter.IsElite ? "精英编成：" : "区域编成：") + summary;
        }

        private static void ConfigureCombatInventory(CombatState state, RogueliteMapRun mapRun,
            RogueliteDeveloperRun developerRun)
        {
            if (mapRun != null)
            {
                state.ConfigureItemInventory(mapRun.Inventory, mapRun.ItemQuickbar);
                return;
            }
            InventoryContainerState inventory = new InventoryContainerState();
            List<string> slots = new List<string>();
            AddExplicitCombatItem(inventory, slots, "combat-medkit", "medkit", 0);
            AddExplicitCombatItem(inventory, slots, "combat-shield-cell", "shield_cell", 1);
            if (developerRun?.IsShortRun == true &&
                developerRun.ShortRun.Phase == ShortRoguelitePhase.SecondCombat &&
                developerRun.ShortRun.SalvageChoiceId == "shield_cell")
                AddExplicitCombatItem(inventory, slots, "combat-shield-cell-salvage", "shield_cell", 2);
            state.ConfigureItemInventory(inventory, slots);
        }

        private static void AddExplicitCombatItem(InventoryContainerState inventory, IList<string> slots,
            string instanceId, string definitionId, int acquisitionOrder)
        {
            InventoryResult result = inventory.AddFirstFit(
                new ItemInstance(instanceId, definitionId, acquisitionOrder));
            if (!result.Success)
                throw new InvalidOperationException("Unable to configure explicit combat inventory: " + result.Error);
            slots.Add(instanceId);
        }

        private static void ApplyShortRunChoices(CombatState state, RogueliteDeveloperRun developerRun)
        {
            if (developerRun?.IsShortRun != true ||
                developerRun.ShortRun.Phase != ShortRoguelitePhase.SecondCombat) return;
            UnitState hero = state.GetUnit("hero");
            if (developerRun.ShortRun.EventChoiceId == "field_repair") hero.Armor += 1;
            if (developerRun.ShortRun.UpgradeChoiceId == "calibrated_rifle")
                hero.Equip(StageTwoBuilds.CalibratedRifle, CombatCatalog.Shield,
                    CombatCatalog.FireBolt, CombatCatalog.FrostBind);
        }

        private static void ConfigureLoot(CombatState state, RogueliteMapRun mapRun)
        {
            state.SetLoot(new LootContainer(new GridPosition(2, 0),
                new InventoryItem("aether_core", "以太核心", 2, 1)));
            string lootKey = mapRun == null ? "relay-crate" : mapRun.CurrentNodeId + "-relay-crate";
            ArtifactDefinition lootArtifact = ArtifactRewardPool.RollLoot(mapRun?.Seed ?? 0, lootKey);
            state.SetLootSource(new LootSourceState(lootKey, new GridPosition(2, 0), new[]
            {
                new ItemInstance(lootKey + "-medkit", "medkit", 0),
                new ItemInstance(lootKey + "-scroll-F-S01", "F-S01", 1),
                new ItemInstance(lootKey + "-artifact-" + lootArtifact.Id, lootArtifact.Id, 2)
            }));
            mapRun?.RestoreLootProgress(state.LootSource);
        }

        private static GridPosition ScenePosition(CombatSceneMarker marker) =>
            new GridPosition(Mathf.RoundToInt(marker.transform.position.x),
                Mathf.RoundToInt(marker.transform.position.y));
    }
}
