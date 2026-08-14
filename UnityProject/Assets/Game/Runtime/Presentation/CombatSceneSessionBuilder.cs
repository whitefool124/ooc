using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
                : RogueliteEncounterCatalog.For(encounterId, mapRun?.RegionBossId);
            FirstRegionLevelDefinition currentLevel;
            CombatState state;
            GridMap map;
            if (FirstRegionLevelCatalog.TryFor(encounterId, out FirstRegionLevelDefinition level))
            {
                FirstRegionLevelBuild build = FirstRegionLevelBuilder.Build(level, mapRun?.RegionBossId);
                currentLevel = build.Definition;
                state = build.State;
                map = state.Map;
            }
            else
            {
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
            ConfigureCombatInventory(state, mapRun, developerRun);
            ApplyShortRunChoices(state, developerRun);
            mapRun?.ApplyBuild(state.GetUnit("hero"));
            ConfigureLoot(state, mapRun);
            return new CombatSceneSessionBuild(state, preparation, currentLevel);
        }

        private static MissionPreparation PrepareMission(CombatState state, GridMap map,
            FirstRegionLevelDefinition currentLevel, RogueliteEncounterDefinition encounter,
            RogueliteMapRun mapRun, RogueliteDeveloperRun developerRun)
        {
            RogueliteMissionDefinition mission = mapRun != null
                ? RogueliteDeveloperCatalog.FindMission(mapRun.HasPendingContentCombat
                    ? mapRun.PendingContentCombatMissionId
                    : mapRun.CurrentNodeId)
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
