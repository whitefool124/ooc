using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class RainLanternCourtTests
    {
        [Test]
        public void FrozenLayout_UsesExactCoordinatesAndCombatants()
        {
            FirstRegionLevelDefinition level = FirstRegionLevelCatalog.For(RainLanternCourtRuntime.LevelId);
            Assert.That(level.Width, Is.EqualTo(6));
            Assert.That(level.Height, Is.EqualTo(5));
            Assert.That(level.Width * level.Height, Is.EqualTo(30), "首场教学应使用 25–36 格的默认紧凑战场。");
            Assert.That(level.HeroSpawn, Is.EqualTo(new GridPosition(1, 4)));
            Assert.That(level.EnemyPlacements.Select(value => value.ArchetypeId),
                Is.EqualTo(new[] { "tether_hound", "pyromancer" }));
            Assert.That(level.EnemyPlacements.Select(value => value.Position),
                Is.EqualTo(new[] { new GridPosition(4, 3), new GridPosition(5, 0) }));
            Assert.That(level.Terrain.Count(value => value.Kind == LevelTerrainKind.Water), Is.EqualTo(3));
            Assert.That(level.Terrain.Count(value => value.Kind == LevelTerrainKind.LampVine), Is.EqualTo(6));
            Assert.That(level.Terrain.Count(value => value.Kind == LevelTerrainKind.LightCover), Is.EqualTo(3));
            Assert.That(level.Terrain.Count(value => value.Kind == LevelTerrainKind.PermanentWall), Is.EqualTo(10));
            Assert.That(FirstRegionLevelCatalog.Validate(), Is.Empty);
        }

        [Test]
        public void FormalTerrainArt_UsesDedicatedSquareWaterAndHollowVineResources()
        {
            Assert.That(FormalArtRegistry.EnvironmentPath("rain_court_water"),
                Is.EqualTo("Art/FormalFirstBattle32/rain_court_water"));
            Assert.That(FormalArtRegistry.EnvironmentPath("lamp_vine"),
                Is.EqualTo("Art/FormalFirstBattle32/lamp_vine"));
            Texture2D water = Resources.Load<Texture2D>(FormalArtRegistry.EnvironmentPath("rain_court_water"));
            Texture2D vine = Resources.Load<Texture2D>(FormalArtRegistry.EnvironmentPath("lamp_vine"));
            Assert.That(water, Is.Not.Null);
            Assert.That(vine, Is.Not.Null);
            Assert.That(new[] { water.width, water.height }, Is.EqualTo(new[] { 32, 32 }));
            Assert.That(new[] { vine.width, vine.height }, Is.EqualTo(new[] { 32, 32 }));
        }

        [Test]
        public void Build_AttachesFormalRulesAndExactEnemyVitals()
        {
            CombatState state = FirstRegionLevelBuilder.Build(RainLanternCourtRuntime.LevelId).State;
            Assert.That(state.RainLanternCourt, Is.Not.Null);
            Assert.That(state.Units.Values.Single(value => value.EnemyArchetypeId == "tether_hound").MaxHealth, Is.EqualTo(12));
            Assert.That(state.Units.Values.Single(value => value.EnemyArchetypeId == "pyromancer").MaxHealth, Is.EqualTo(16));
            Assert.That(state.Map.GetTile(new GridPosition(2, 3)).IsWater, Is.True);
            Assert.That(state.Map.GetTile(new GridPosition(3, 0)).IsLampVine, Is.True);
            Assert.That(state.Map.HasLineOfSight(new GridPosition(3, 2), new GridPosition(2, 2)), Is.True);
            Assert.That(state.Map.HasLineOfSight(new GridPosition(3, 2), new GridPosition(1, 2)), Is.False);
        }

        [Test]
        public void FirstMoveThroughWater_CostsThreeExtinguishesAndTriggersOriginTalent()
        {
            CombatState state = FirstRegionLevelBuilder.Build(RainLanternCourtRuntime.LevelId).State;
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            UnitState hero = state.GetUnit("hero");
            hero.ApplyStatus(StatusType.Burning, 2);
            CombatResolver.BeginTurn(state, hero.Id);

            CombatResolver.Resolve(state, CombatCommand.Move(hero.Id, new GridPosition(2, 3)));

            Assert.That(hero.Position, Is.EqualTo(new GridPosition(2, 3)));
            Assert.That(hero.HasStatus(StatusType.Burning), Is.False);
            Assert.That(hero.Shield, Is.EqualTo(2));
            Assert.That(state.EventLog.Any(value => value.Contains("就地接线")), Is.True);
        }

        [Test]
        public void BorrowedCover_IsSelfOnlyNonStackingAndBoostsExactlyNextMove()
        {
            CombatState state = FirstRegionLevelBuilder.Build(RainLanternCourtRuntime.LevelId).State;
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            UnitState hero = state.GetUnit("hero");
            CombatResolver.BeginTurn(state, hero.Id);

            state.RainLanternCourt.CastBorrowedCover(state, hero);

            Assert.That(hero.Shield, Is.EqualTo(4));
            Assert.That(state.RainLanternCourt.MovementBudget(hero), Is.EqualTo(hero.MovementRangeThisTurn + 2));
            Assert.Throws<System.InvalidOperationException>(() => state.RainLanternCourt.CastBorrowedCover(state, hero));
            CombatResolver.Resolve(state, CombatCommand.Move(hero.Id, new GridPosition(1, 3)));
            Assert.That(state.RainLanternCourt.MovementBudget(hero), Is.EqualTo(hero.MovementRangeThisTurn));
        }

        [Test]
        public void Pyromancer_BurnsConfiguredFirstVineWithoutUnitDamage()
        {
            CombatState state = FirstRegionLevelBuilder.Build(RainLanternCourtRuntime.LevelId).State;
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            UnitState pyro = state.Units.Values.Single(value => value.EnemyArchetypeId == "pyromancer");
            UnitState hero = state.GetUnit("hero");
            CombatResolver.BeginTurn(state, pyro.Id);
            int health = hero.Health;
            CombatCommand command = state.RainLanternCourt.ChooseEnemyCommand(state, pyro, hero);

            Assert.That(command.Type, Is.EqualTo(CombatCommandType.Interact));
            Assert.That(command.Destination, Is.EqualTo(new GridPosition(4, 1)));
            CombatResolver.Resolve(state, command);
            Assert.That(state.Map.GetTile(command.Destination).IsLampVine, Is.False);
            Assert.That(state.Map.GetTile(command.Destination).IsScorched, Is.True);
            Assert.That(hero.Health, Is.EqualTo(health));
            CombatCommand followUp = state.RainLanternCourt.ChooseEnemyCommand(state, pyro, hero);
            Assert.That(followUp.Type, Is.EqualTo(CombatCommandType.Move));
            Assert.That(followUp.Destination, Is.EqualTo(new GridPosition(5, 2)));
        }

        [Test]
        public void FirstRunB1_IsUnlockedAndB2HasAFormalButStillLockedPackage()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateFirstRunV1(17);
            run.AcknowledgeFirstRunOrigin();
            Assert.That(RogueliteEncounterCatalog.For(run, "B1").VariantKey,
                Is.EqualTo(RainLanternCourtRuntime.EncounterId));
            Assert.That(RogueliteEncounterCatalog.For(run, "B2").VariantKey,
                Is.EqualTo(RogueliteEncounterCatalog.SecondBattleGreenhouseCollectionRoom.VariantKey));
            Assert.That(run.IsNodeAvailable("B2"), Is.False);
            Assert.That(run.RogueEquippedSpellIds[4], Is.EqualTo(RainLanternCourtRuntime.OriginSpellId));
        }

        [Test]
        public void ThreeFrozenRoutes_HaveDeterministicLegalMovementSegments()
        {
            CombatState state = FirstRegionLevelBuilder.Build(RainLanternCourtRuntime.LevelId).State;
            UnitState hero = state.GetUnit("hero");
            Assert.That(RainLanternCourtRuntime.VerificationRoutes.Keys, Is.EquivalentTo(new[]
            {
                "FIRST-B1-ROUTE-VINE", "FIRST-B1-ROUTE-WATER", "FIRST-B1-ROUTE-COVER"
            }));
            foreach (var route in RainLanternCourtRuntime.VerificationRoutes)
            {
                Assert.That(route.Value.First(), Is.EqualTo(new GridPosition(1, 4)), route.Key);
                for (int index = 1; index < route.Value.Count; index++)
                {
                    var path = state.Map.FindLowestCostPath(route.Value[index - 1], route.Value[index], UnitState.HeroBaseMovementRange,
                        position => state.RainLanternCourt.EntryCost(hero, position));
                    Assert.That(path, Is.Not.Empty, route.Key + " segment " + index);
                }
            }
        }

        [Test]
        public void Hound_LostSightSearchPausesOneTurnAndPublicIntentHidesInternalPath()
        {
            GridMap map = new GridMap(6, 4);
            map.SetTile(new GridPosition(2, 1), new TileState { IsLampVine = true, Durability = 1 });
            map.SetTile(new GridPosition(3, 1), new TileState { IsLampVine = true, Durability = 1 });
            UnitState hero = new UnitState("hero", true, new GridPosition(1, 1));
            UnitState hound = new UnitState("hound", false, new GridPosition(4, 1));
            EnemyArchetypes.Get("tether_hound").Apply(hound);
            CombatState state = new CombatState(map, new[] { hero, hound }, new[] { new EliminationObjective("test") });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachRainLanternCourt(new RainLanternCourtRuntime());
            CombatResolver.BeginTurn(state, hero.Id);
            CombatResolver.Resolve(state, CombatCommand.Move(hero.Id, new GridPosition(2, 1)));

            CombatResolver.BeginTurn(state, hound.Id);
            CombatCommand approach = state.RainLanternCourt.ChooseEnemyCommand(state, hound, hero);
            Assert.That(approach.Type, Is.EqualTo(CombatCommandType.Move));
            Assert.That(approach.Destination, Is.EqualTo(new GridPosition(1, 1)));
            CombatResolver.Resolve(state, approach);
            CombatResolver.BeginTurn(state, hero.Id);
            CombatResolver.Resolve(state, CombatCommand.Move(hero.Id, new GridPosition(3, 1)));

            CombatResolver.BeginTurn(state, hound.Id);
            CombatCommand sniff = state.RainLanternCourt.ChooseEnemyCommand(state, hound, hero);
            EnemyIntentPresentation intent = state.RainLanternCourt.PresentIntent(state, hound, sniff);
            Assert.That(sniff.Type, Is.EqualTo(CombatCommandType.EndTurn));
            Assert.That(intent.ActionName, Is.EqualTo("停留嗅探"));
            Assert.That(intent.DetailedText, Does.Not.Contain("最后目击"));
            Assert.That(intent.HasDestination, Is.False);
        }

        [Test]
        public void Hound_DistantVisibleHeroAdvancesByOneMovementBudgetInsteadOfWaiting()
        {
            CombatState state = FirstRegionLevelBuilder.Build(RainLanternCourtRuntime.LevelId).State;
            UnitState hound = state.Units.Values.Single(value => value.EnemyArchetypeId == "tether_hound");
            UnitState hero = state.GetUnit("hero");
            CombatResolver.BeginTurn(state, hound.Id);

            CombatCommand command = state.RainLanternCourt.ChooseEnemyCommand(state, hound, hero);

            Assert.That(command.Type, Is.EqualTo(CombatCommandType.Move));
            IReadOnlyList<GridPosition> path = state.RainLanternCourt.FindPath(state, hound, command.Destination);
            Assert.That(path.Count, Is.GreaterThan(1));
            Assert.That(path.Skip(1).Sum(position => state.RainLanternCourt.EntryCost(hound, position)),
                Is.LessThanOrEqualTo(state.RainLanternCourt.MovementBudget(hound)));
        }
    }
}
