using System;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class CombatResolverTests
    {
        [Test]
        public void Move_RejectsBlockedDestination()
        {
            CombatState state = CreateHeroState(new GridPosition(1, 0));
            CombatResolver.BeginTurn(state, "hero");

            Assert.Throws<InvalidOperationException>(() =>
                CombatResolver.Resolve(state, CombatCommand.Move("hero", new GridPosition(1, 0), Facing.East)));
            Assert.That(state.GetUnit("hero").Position, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(state.GetUnit("hero").ActionPoints, Is.EqualTo(CombatResolver.HeroActionPointsPerTurn));
        }

        [Test]
        public void HeroTurn_GrantsThreeActionPoints_AndMoveAllowsFreeFacing()
        {
            CombatState state = CreateHeroState();
            CombatResolver.BeginTurn(state, "hero");

            CombatResolver.Resolve(state, CombatCommand.Move("hero", new GridPosition(0, 1), Facing.North));

            UnitState hero = state.GetUnit("hero");
            Assert.That(hero.ActionPoints, Is.EqualTo(2));
            Assert.That(hero.Facing, Is.EqualTo(Facing.North));
        }

        [Test]
        public void SameInitialStateAndCommands_ProduceSameResult()
        {
            CombatState firstState = CreateHeroState();
            CombatState secondState = CreateHeroState();

            ApplySequence(firstState);
            ApplySequence(secondState);

            UnitState firstHero = firstState.GetUnit("hero");
            UnitState secondHero = secondState.GetUnit("hero");
            Assert.That(firstHero.Position, Is.EqualTo(secondHero.Position));
            Assert.That(firstHero.Facing, Is.EqualTo(secondHero.Facing));
            Assert.That(firstHero.ActionPoints, Is.EqualTo(secondHero.ActionPoints));
        }

        [Test]
        public void PreviewAttack_ReportsLineOfSightAndDeterministicDamage()
        {
            GridMap map = new GridMap(6, 3);
            map.SetTile(new GridPosition(2, 1), new TileState { Cover = CoverType.Heavy, Durability = 5 });
            CombatState state = new CombatState(map, new[]
            {
                new UnitState("hero", true, new GridPosition(0, 1), Facing.East),
                new UnitState("enemy", false, new GridPosition(4, 1), Facing.West)
            });

            CombatResolver.AttackPreview preview = CombatResolver.PreviewAttack(state, "hero", "enemy", false);

            Assert.That(preview.HasLineOfSight, Is.False);
            Assert.That(preview.FinalDamage, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void FireBolt_AppliesBurningAndCooldown_Deterministically()
        {
            CombatState state = CreateDuelState();
            CombatResolver.BeginTurn(state, "hero");

            CombatResolver.Resolve(state, CombatCommand.UseSkill("hero", 0, "enemy"));

            UnitState hero = state.GetUnit("hero");
            UnitState enemy = state.GetUnit("enemy");
            Assert.That(enemy.HasStatus(StatusType.Burning), Is.True);
            Assert.That(enemy.StatusDuration(StatusType.Burning), Is.EqualTo(2));
            Assert.That(hero.Cooldown(CombatCatalog.FireBolt), Is.EqualTo(1));
            Assert.That(hero.Mana, Is.EqualTo(4));
        }

        [Test]
        public void BoundUnit_CannotMove()
        {
            CombatState state = CreateDuelState();
            UnitState enemy = state.GetUnit("enemy");
            enemy.ApplyStatus(StatusType.Bound, 2);
            CombatResolver.BeginTurn(state, "enemy");

            Assert.Throws<InvalidOperationException>(() => CombatResolver.Resolve(state, CombatCommand.Move("enemy", new GridPosition(4, 1), Facing.East)));
        }

        [Test]
        public void Loot_CostsOneActionPointAndPlacesItemInGridBackpack()
        {
            CombatState state = CreateHeroState();
            state.SetLoot(new LootContainer(new GridPosition(1, 0), new InventoryItem("core", "以太核心", 2, 1)));
            CombatResolver.BeginTurn(state, "hero");

            CombatResolver.Resolve(state, CombatCommand.Loot("hero"));

            Assert.That(state.GetUnit("hero").ActionPoints, Is.EqualTo(2));
            Assert.That(state.Loot.IsLooted, Is.True);
            Assert.That(state.Backpack.Items.Count, Is.EqualTo(1));
        }

        [Test]
        public void FullBackpack_RejectsLootWithoutSpendingActionPoint()
        {
            CombatState state = CreateHeroState();
            for (int i = 0; i < 60; i++) Assert.That(state.Backpack.TryAdd(new InventoryItem("fill" + i, "填充物")), Is.True);
            state.SetLoot(new LootContainer(new GridPosition(1, 0), new InventoryItem("core", "以太核心")));
            CombatResolver.BeginTurn(state, "hero");

            Assert.Throws<InvalidOperationException>(() => CombatResolver.Resolve(state, CombatCommand.Loot("hero")));

            Assert.That(state.GetUnit("hero").ActionPoints, Is.EqualTo(3));
            Assert.That(state.Loot.IsLooted, Is.False);
        }

        [TestCase("rifle")]
        [TestCase("hammer")]
        [TestCase("wand")]
        public void ThreeBuilds_EquipDistinctMainHandRoutes(string build)
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            WeaponDefinition weapon = build == "rifle" ? CombatCatalog.Rifle : build == "hammer" ? CombatCatalog.Hammer : CombatCatalog.Wand;
            hero.Equip(weapon, CombatCatalog.Shield, CombatCatalog.FireBolt, CombatCatalog.FrostBind);

            Assert.That(hero.MainHand.Id, Is.EqualTo(build));
            Assert.That(hero.MainHand.Range, Is.GreaterThan(0));
        }

        [Test]
        public void EnemyArchetypes_ProvideTwelveBaseTypesAndThreeEliteOrBossVariants()
        {
            Assert.That(EnemyArchetypes.All.Count, Is.EqualTo(15));
            Assert.That(EnemyArchetypes.All, Has.Exactly(3).Matches<EnemyArchetype>(archetype => archetype.IsElite));
            Assert.That(EnemyArchetypes.Get("elite_vanguard").DisplayName, Is.EqualTo("刻阵教官"));
        }

        [Test]
        public void ThreeWorkshopBuilds_RequireDifferentRangeDelayAndResourceRoutes()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);

            StageTwoBuilds.Apply(hero, 0);
            Assert.That(hero.MainHand.Range, Is.EqualTo(4));
            Assert.That(hero.MainHand.InitiativeDelay, Is.EqualTo(0));
            Assert.That(hero.MainHand.ManaCost, Is.EqualTo(0));

            StageTwoBuilds.Apply(hero, 1);
            Assert.That(hero.MainHand.Range, Is.EqualTo(1));
            Assert.That(hero.MainHand.InitiativeDelay, Is.GreaterThan(0));

            StageTwoBuilds.Apply(hero, 2);
            Assert.That(hero.MainHand.Range, Is.EqualTo(3));
            Assert.That(hero.MainHand.ManaCost, Is.EqualTo(1));
        }

        [Test]
        public void ArcaneWorkshopBuild_AttackConsumesAether()
        {
            CombatState state = CreateDuelState();
            UnitState hero = state.GetUnit("hero");
            StageTwoBuilds.Apply(hero, 2);
            CombatResolver.BeginTurn(state, "hero");

            CombatResolver.Resolve(state, CombatCommand.Attack("hero", "enemy"));

            Assert.That(hero.Mana, Is.EqualTo(hero.MaxMana - 1));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void AllThreeBuilds_CanCompleteTheSameRelayMission(int build)
        {
            GridMap map = new GridMap(12, 9);
            map.SetTile(new GridPosition(10, 4), new TileState { IsObjective = true, Durability = 6 });
            UnitState hero = new UnitState("hero", true, new GridPosition(9, 4), Facing.East);
            StageTwoBuilds.Apply(hero, build);
            CombatState state = new CombatState(map, new[] { hero });
            CombatResolver.BeginTurn(state, "hero");

            CombatResolver.Resolve(state, CombatCommand.Interact("hero", new GridPosition(10, 4)));
            CombatResolver.Resolve(state, CombatCommand.Interact("hero", new GridPosition(10, 4)));

            Assert.That(state.IsVictory, Is.True);
        }

        [Test]
        public void DestructionObjective_DoesNotDependOnFixedRelayCoordinate()
        {
            GridMap map = new GridMap(6, 4);
            GridPosition target = new GridPosition(2, 2);
            map.SetTile(target, new TileState { IsObjective = true, Durability = 3 });
            CombatState state = new CombatState(map, new[] { new UnitState("hero", true, new GridPosition(1, 2), Facing.East) });
            CombatResolver.BeginTurn(state, "hero");
            CombatResolver.Resolve(state, CombatCommand.Interact("hero", target));
            Assert.That(state.IsVictory, Is.True);
        }

        [Test]
        public void Objectives_AreClonedAndCanBeInjectedPerMap()
        {
            GridMap map = new GridMap(4, 4);
            CombatState state = new CombatState(map, new[] { new UnitState("hero", true, new GridPosition(1, 1), Facing.East) }, new CombatObjective[] { new CaptureObjective(new GridPosition(1, 1)) });
            Assert.That(state.IsVictory, Is.False);
            CombatResolver.BeginTurn(state, "hero");
            state.ConfigureObjectives(new CaptureObjective(new GridPosition(1, 1)));
            Assert.That(state.IsVictory, Is.True);
            CombatState clone = state.Clone();
            Assert.That(clone.IsVictory, Is.True);
            Assert.That(clone.Objectives[0], Is.Not.SameAs(state.Objectives[0]));
        }

        [Test]
        public void InvestigationObjective_IsDeterministicAcrossTacticalRestart()
        {
            GridMap map = new GridMap(4, 4);
            GridPosition target = new GridPosition(1, 2);
            CombatState state = new CombatState(map, new[] { new UnitState("hero", true, new GridPosition(1, 1), Facing.North) }, new CombatObjective[] { new InvestigationObjective(new[] { target }) });
            CombatState snapshot = state.Clone();
            CombatResolver.BeginTurn(state, "hero");
            CombatResolver.Resolve(state, CombatCommand.Interact("hero", target));
            Assert.That(state.IsVictory, Is.True);
            CombatState restarted = snapshot.Clone();
            CombatResolver.BeginTurn(restarted, "hero");
            CombatResolver.Resolve(restarted, CombatCommand.Interact("hero", target));
            Assert.That(restarted.IsVictory, Is.EqualTo(state.IsVictory));
        }

        private static CombatState CreateHeroState(params GridPosition[] blockedPositions) =>
            new CombatState(
                new GridMap(4, 4, blockedPositions),
                new[] { new UnitState("hero", true, new GridPosition(0, 0), Facing.East) });

        private static CombatState CreateDuelState() => new CombatState(
            new GridMap(6, 3),
            new[]
            {
                new UnitState("hero", true, new GridPosition(0, 1), Facing.East),
                new UnitState("enemy", false, new GridPosition(3, 1), Facing.West)
            });

        private static void ApplySequence(CombatState state)
        {
            CombatResolver.BeginTurn(state, "hero");
            CombatResolver.Resolve(state, CombatCommand.Move("hero", new GridPosition(0, 1), Facing.North));
            CombatResolver.Resolve(state, CombatCommand.TurnInPlace("hero", Facing.East));
        }
    }
}
