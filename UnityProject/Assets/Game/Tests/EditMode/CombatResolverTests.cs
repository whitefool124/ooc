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
                CombatResolver.Resolve(state, CombatCommand.Move("hero", new GridPosition(1, 0))));
            Assert.That(state.GetUnit("hero").Position, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(state.GetUnit("hero").ActionPoints, Is.EqualTo(CombatResolver.HeroActionPointsPerTurn));
        }

        [Test]
        public void HeroTurn_GrantsThreeActionPoints_AndMoveCostsOne()
        {
            CombatState state = CreateHeroState();
            CombatResolver.BeginTurn(state, "hero");

            CombatResolver.Resolve(state, CombatCommand.Move("hero", new GridPosition(0, 1)));

            UnitState hero = state.GetUnit("hero");
            Assert.That(hero.ActionPoints, Is.EqualTo(2));
        }

        [Test]
        public void EnemyMove_ConsumesTheWholeTurnWhileHeroActionsKeepTheirConfiguredCost()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(3, 0));
            CombatState state = new CombatState(new GridMap(5, 2), new[] { hero, enemy });

            CombatResolver.BeginTurn(state, enemy.Id);
            CombatResolver.Resolve(state, CombatCommand.Move(enemy.Id, new GridPosition(2, 0)));

            Assert.That(enemy.ActionPoints, Is.Zero);
            Assert.That(enemy.Position, Is.EqualTo(new GridPosition(2, 0)));
        }

        [Test]
        public void TurnStart_UsesPublishedThreeActionPointsAndHeroThreeOrTwoMovement()
        {
            CombatState state = CreateHeroState();
            UnitState hero = state.GetUnit("hero");
            CombatResolver.BeginTurn(state, hero.Id);
            Assert.That(hero.ActionPoints, Is.EqualTo(3));
            Assert.That(hero.MovementRangeThisTurn, Is.EqualTo(UnitState.HeroBaseMovementRange));

            hero.ApplyStatus(StatusType.Agility, 2, -1);
            CombatResolver.BeginTurn(state, hero.Id);
            Assert.That(hero.ActionPoints, Is.EqualTo(3));
            Assert.That(hero.MovementRangeThisTurn, Is.EqualTo(UnitState.HeroSlowedMovementRange));
        }

        [Test]
        public void HeroMove_AllowsThreeCellsAndRejectsFourCells()
        {
            CombatState withinRange = new CombatState(new GridMap(5, 2),
                new[] { new UnitState("hero", true, new GridPosition(0, 0)) });
            CombatResolver.BeginTurn(withinRange, "hero");
            Assert.DoesNotThrow(() => CombatResolver.Resolve(withinRange,
                CombatCommand.Move("hero", new GridPosition(3, 0))));

            CombatState beyondRange = new CombatState(new GridMap(5, 2),
                new[] { new UnitState("hero", true, new GridPosition(0, 0)) });
            CombatResolver.BeginTurn(beyondRange, "hero");
            Assert.Throws<InvalidOperationException>(() => CombatResolver.Resolve(beyondRange,
                CombatCommand.Move("hero", new GridPosition(4, 0))));
            Assert.That(CombatMovementQuery.PlayerTargetFailure(beyondRange, new GridPosition(4, 0)),
                Is.EqualTo("目标格超出当前步数"));
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
            Assert.That(firstHero.ActionPoints, Is.EqualTo(secondHero.ActionPoints));
        }

        [Test]
        public void PreviewAttack_ReportsLineOfSightAndDeterministicDamage()
        {
            GridMap map = new GridMap(6, 3);
            map.SetTile(new GridPosition(2, 1), new TileState { Cover = CoverType.Heavy, Durability = 5 });
            CombatState state = new CombatState(map, new[]
            {
                new UnitState("hero", true, new GridPosition(0, 1)),
                new UnitState("enemy", false, new GridPosition(4, 1))
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

            Assert.Throws<InvalidOperationException>(() => CombatResolver.Resolve(state, CombatCommand.Move("enemy", new GridPosition(4, 1))));
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
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            WeaponDefinition weapon = build == "rifle" ? CombatCatalog.Rifle : build == "hammer" ? CombatCatalog.Hammer : CombatCatalog.Wand;
            hero.Equip(weapon, CombatCatalog.Shield, CombatCatalog.FireBolt, CombatCatalog.FrostBind);

            Assert.That(hero.MainHand.Id, Is.EqualTo(build));
            Assert.That(hero.MainHand.Range, Is.GreaterThan(0));
        }

        [Test]
        public void EnemyArchetypes_IncludeFixedFirstRunElite()
        {
            Assert.That(EnemyArchetypes.All.Count, Is.EqualTo(17));
            Assert.That(EnemyArchetypes.All, Has.Exactly(7).Matches<EnemyArchetype>(archetype => archetype.IsElite));
            Assert.That(EnemyArchetypes.Get("elite_vanguard").DisplayName, Is.EqualTo("划线教官"));
            Assert.That(EnemyArchetypes.Get("breach_ram").MaxHealth, Is.EqualTo(36));
            Assert.That(EnemyArchetypes.Get("elder_tracker_hound").DisplayName, Is.EqualTo("老寻"));
            Assert.That(EnemyArchetypes.Get("signal_keeper").DisplayName, Is.EqualTo("灯台值守"));
            Assert.That(EnemyArchetypes.Get("wind_librarian").DisplayName, Is.EqualTo("小铃"));
            Assert.That(EnemyArchetypes.Get("legacy_storekeeper").DisplayName, Is.EqualTo("老库管"));
            Assert.That(EnemyArchetypes.Get("prototype_hand").DisplayName, Is.EqualTo("试制员"));
        }

        [Test]
        public void DebugAssist_DoesNotChangePublishedEnemyActionPointsOrDamage()
        {
            CombatDebugTuning.TemporaryEnemyAssistEnabled = true;
            try
            {
                UnitState hero = new UnitState("hero", true, new GridPosition(0, 0))
                {
                    Armor = 0,
                    Block = 0
                };
                UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0));
                enemy.Equip(CombatCatalog.Hammer, CombatCatalog.Shield, CombatCatalog.FireBolt, CombatCatalog.FrostBind);
                CombatState state = new CombatState(new GridMap(3, 2), new[] { hero, enemy });
                state.ConfigureRuleset(CombatRuleset.Roguelite);

                CombatResolver.BeginTurn(state, enemy.Id);
                CombatResolver.AttackPreview preview = CombatResolver.PreviewAttack(state, enemy.Id, hero.Id, false);
                CombatResolver.AttackPreview skillPreview = CombatResolver.PreviewSkillAttack(state, enemy.Id, hero.Id, CombatCatalog.FireBolt);
                int healthBefore = hero.Health;
                CombatResolver.Resolve(state, CombatCommand.Attack(enemy.Id, hero.Id));

                Assert.That(enemy.ActionPoints, Is.Zero, "敌方一次动作应耗尽本回合全部行动点");
                Assert.That(preview.BaseDamage, Is.EqualTo(6));
                Assert.That(preview.FinalDamage, Is.EqualTo(6));
                Assert.That(skillPreview.BaseDamage, Is.EqualTo(5));
                Assert.That(skillPreview.FinalDamage, Is.EqualTo(5));
                Assert.That(hero.Health, Is.EqualTo(healthBefore - 6));
            }
            finally
            {
                CombatDebugTuning.TemporaryEnemyAssistEnabled = false;
            }
        }

        [Test]
        public void OpenInventory_IsAnAuthoritativeOneActionPointCombatCommand()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(2, 0));
            CombatState state = new CombatState(new GridMap(3, 1), new[] { hero, enemy });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(state, hero.Id);

            CombatResolver.Resolve(state, CombatCommand.OpenInventory(hero.Id));

            Assert.That(hero.ActionPoints, Is.EqualTo(2));
            Assert.That(state.InventoryOpenCount, Is.EqualTo(1));
        }

        [Test]
        public void EquippedBackpack_MakesOnlyFirstInventoryOpenFreeEachBattle()
        {
            CombatState state = CreateHeroState();
            UnitState hero = state.GetUnit("hero");
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachRogueEquipmentRuntime(OCC.Combat.Roguelite.RogueEquipmentRuntime.CreateStarter(17));
            CombatResolver.BeginTurn(state, hero.Id);

            CombatResolver.Resolve(state, CombatCommand.OpenInventory(hero.Id));
            Assert.That(hero.ActionPoints, Is.EqualTo(3));
            CombatResolver.Resolve(state, CombatCommand.OpenInventory(hero.Id));
            Assert.That(hero.ActionPoints, Is.EqualTo(2));
        }

        [Test]
        public void ThreeWorkshopBuilds_RequireDifferentRangeDelayAndResourceRoutes()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));

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
            UnitState hero = new UnitState("hero", true, new GridPosition(9, 4));
            StageTwoBuilds.Apply(hero, build);
            CombatState state = new CombatState(map, new[] { hero });
            CombatResolver.BeginTurn(state, "hero");

            CombatResolver.Resolve(state, CombatCommand.Interact("hero", new GridPosition(10, 4)));
            if (state.Map.GetTile(new GridPosition(10, 4)).Durability > 0)
                CombatResolver.Resolve(state, CombatCommand.Interact("hero", new GridPosition(10, 4)));

            Assert.That(state.IsVictory, Is.True);
        }

        [Test]
        public void DestructionObjective_DoesNotDependOnFixedRelayCoordinate()
        {
            GridMap map = new GridMap(6, 4);
            GridPosition target = new GridPosition(2, 2);
            map.SetTile(target, new TileState { IsObjective = true, Durability = 3 });
            CombatState state = new CombatState(map, new[] { new UnitState("hero", true, new GridPosition(1, 2)) });
            CombatResolver.BeginTurn(state, "hero");
            CombatResolver.Resolve(state, CombatCommand.Interact("hero", target));
            Assert.That(state.IsVictory, Is.True);
        }

        [Test]
        public void Objectives_AreClonedAndCanBeInjectedPerMap()
        {
            GridMap map = new GridMap(4, 4);
            CombatState state = new CombatState(map, new[] { new UnitState("hero", true, new GridPosition(1, 1)) }, new CombatObjective[] { new CaptureObjective(new GridPosition(1, 1)) });
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
            CombatState state = new CombatState(map, new[] { new UnitState("hero", true, new GridPosition(1, 1)) }, new CombatObjective[] { new InvestigationObjective(new[] { target }) });
            CombatState snapshot = state.Clone();
            CombatResolver.BeginTurn(state, "hero");
            CombatResolver.Resolve(state, CombatCommand.Interact("hero", target));
            Assert.That(state.IsVictory, Is.True);
            CombatState restarted = snapshot.Clone();
            CombatResolver.BeginTurn(restarted, "hero");
            CombatResolver.Resolve(restarted, CombatCommand.Interact("hero", target));
            Assert.That(restarted.IsVictory, Is.EqualTo(state.IsVictory));
        }

        [Test]
        public void ForcedMoveIntoDurableObject_DamagesShieldedUnitAndObjectAndKeepsOrigin()
        {
            GridMap map = new GridMap(4, 3);
            GridPosition obstacle = new GridPosition(2, 1);
            map.SetTile(obstacle, new TileState { Cover = CoverType.Heavy, Durability = 8 });
            UnitState hero = new UnitState("hero", true, new GridPosition(1, 1));
            CombatState state = new CombatState(map, new[] { hero });

            ForcedMoveResult result = state.ResolveForcedMove(hero, new GridPosition(1, 0), 1, "test-push");

            Assert.That(result, Is.EqualTo(ForcedMoveResult.ObjectCollision));
            Assert.That(hero.Position, Is.EqualTo(new GridPosition(1, 1)));
            Assert.That(hero.Shield, Is.Zero, "The initial 2 shield absorbs the first half of the 4 collision damage.");
            Assert.That(hero.Health, Is.EqualTo(hero.MaxHealth - 2));
            Assert.That(map.GetTile(obstacle).Durability, Is.EqualTo(4));
        }

        [Test]
        public void ForcedMoveBlockedByUnit_CancelsWithoutObjectCollisionDamage()
        {
            GridMap map = new GridMap(4, 3);
            UnitState hero = new UnitState("hero", true, new GridPosition(1, 1));
            UnitState blocker = new UnitState("blocker", false, new GridPosition(2, 1));
            CombatState state = new CombatState(map, new[] { hero, blocker });

            ForcedMoveResult result = state.ResolveForcedMove(hero, new GridPosition(1, 0), 1, "test-push");

            Assert.That(result, Is.EqualTo(ForcedMoveResult.Blocked));
            Assert.That(hero.Position, Is.EqualTo(new GridPosition(1, 1)));
            Assert.That(hero.Shield, Is.EqualTo(2));
            Assert.That(hero.Health, Is.EqualTo(hero.MaxHealth));
        }

        private static CombatState CreateHeroState(params GridPosition[] blockedPositions) =>
            new CombatState(
                new GridMap(4, 4, blockedPositions),
                new[] { new UnitState("hero", true, new GridPosition(0, 0)) });

        private static CombatState CreateDuelState() => new CombatState(
            new GridMap(6, 3),
            new[]
            {
                new UnitState("hero", true, new GridPosition(0, 1)),
                new UnitState("enemy", false, new GridPosition(3, 1))
            });

        private static void ApplySequence(CombatState state)
        {
            CombatResolver.BeginTurn(state, "hero");
            CombatResolver.Resolve(state, CombatCommand.Move("hero", new GridPosition(0, 1)));
        }
    }
}
