using System;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Presentation;

namespace OCC.Combat.Tests
{
    public sealed class AcademyEnemyGrowthRuntimeTests
    {
        [Test]
        public void GrowthCatalog_CoversFifteenStageOneSkillsWithValidEffects()
        {
            Assert.That(AcademyEnemyGrowthRuntime.All.Count, Is.EqualTo(15));
            Assert.That(AcademyEnemyGrowthRuntime.All.Select(skill => skill.Id).Distinct().Count(), Is.EqualTo(15));
            Assert.That(SkillCatalogValidator.Validate(AcademyEnemyGrowthRuntime.All), Is.Empty);
        }

        [Test]
        public void OrdinaryEnemy_UsesGrowthWhenPrimaryActionCannotReach_OnlyOncePerBattle()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(4, 4));
            EnemyArchetypes.Get("raider").Apply(enemy);
            CombatState state = new CombatState(new GridMap(5, 5), new[] { hero, enemy });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachAcademyEnemyGrowth(new AcademyEnemyGrowthRuntime());
            CombatResolver.BeginTurn(state, enemy.Id);

            EnemyTurnPlanBook plans = new EnemyTurnPlanBook();
            CombatCommand command = plans.GetExecutionCommand(state, enemy, hero);
            Assert.That(command.Type, Is.EqualTo(CombatCommandType.UseSkill));
            Assert.That(command.SlotIndex, Is.EqualTo(AcademyEnemyGrowthRuntime.CommandSkillIndex));
            Assert.That(plans.GetPublicIntent(state, enemy, hero).ActionName, Is.EqualTo("侧锋蓄势"));

            CombatResolver.Resolve(state, command);
            Assert.That(enemy.StatusStrength(StatusType.Strength), Is.EqualTo(1));
            Assert.That(enemy.StatusDuration(StatusType.Strength), Is.EqualTo(int.MaxValue));
            Assert.That(state.AcademyEnemyGrowth.HasUsed(enemy.Id), Is.True);
            Assert.That(state.Clone().AcademyEnemyGrowth.HasUsed(enemy.Id), Is.True);
            Assert.That(state.AcademyEnemyGrowth.ChooseOrdinaryFallback(enemy,
                CombatCommand.Move(enemy.Id, new GridPosition(3, 4))).Type, Is.EqualTo(CombatCommandType.Move));
        }

        [TestCase("stone_snare")]
        [TestCase("lantern_revealer")]
        [TestCase("signal_keeper")]
        [TestCase("elite_vanguard")]
        [TestCase("prototype_hand")]
        [TestCase("elder_tracker_hound")]
        [TestCase("breach_ram")]
        [TestCase("wind_librarian")]
        public void SpecialistEnemy_CanChooseItsOneBattleGrowthFromSecondOwnTurn(string archetypeId)
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(4, 4));
            EnemyArchetypes.Get(archetypeId).Apply(enemy);
            CombatState state = new CombatState(new GridMap(5, 5), new[] { hero, enemy });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachAcademyEnemyGrowth(new AcademyEnemyGrowthRuntime());
            CombatCommand activeSkill = CombatCommand.Attack(enemy.Id, hero.Id);
            CombatResolver.BeginTurn(state, enemy.Id);
            Assert.That(state.AcademyEnemyGrowth.Choose(state, enemy, activeSkill).Type,
                Is.EqualTo(CombatCommandType.Attack));
            CombatResolver.BeginTurn(state, enemy.Id);
            CombatCommand growth = state.AcademyEnemyGrowth.Choose(state, enemy, activeSkill);
            Assert.That(growth.SlotIndex, Is.EqualTo(AcademyEnemyGrowthRuntime.CommandSkillIndex));
            CombatResolver.Resolve(state, growth);
            Assert.That(state.AcademyEnemyGrowth.HasUsed(enemy.Id), Is.True);
            Assert.That(state.AcademyEnemyGrowth.Choose(state, enemy, activeSkill).Type,
                Is.EqualTo(CombatCommandType.Attack));
        }

        [Test]
        public void SpecialistGrowth_DoesNotReplacePreparedAreaAction()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(4, 4));
            EnemyArchetypes.Get("signal_keeper").Apply(enemy);
            CombatState state = new CombatState(new GridMap(5, 5), new[] { hero, enemy });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachAcademyEnemyGrowth(new AcademyEnemyGrowthRuntime());
            CombatResolver.BeginTurn(state, enemy.Id);
            CombatResolver.BeginTurn(state, enemy.Id);
            CombatCommand prepare = CombatCommand.UseSkillAt(enemy.Id,
                AcademyEnemyAreaRuntime.PrepareSkillIndex, new GridPosition(3, 4), default);
            Assert.That(state.AcademyEnemyGrowth.Choose(state, enemy, prepare).SlotIndex,
                Is.EqualTo(AcademyEnemyAreaRuntime.PrepareSkillIndex));
        }

        [Test]
        public void SpecialistGrowth_IsSelectedThroughThePublicEnemyPlan()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(3, 4));
            UnitState ram = new UnitState("ram", false, new GridPosition(4, 4));
            EnemyArchetypes.Get("breach_ram").Apply(ram);
            CombatState state = new CombatState(new GridMap(5, 5), new[] { hero, ram });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachAcademyEnemyGrowth(new AcademyEnemyGrowthRuntime());
            CombatResolver.BeginTurn(state, ram.Id);
            CombatResolver.BeginTurn(state, ram.Id);
            EnemyTurnPlanBook plans = new EnemyTurnPlanBook();
            CombatCommand command = plans.GetExecutionCommand(state, ram, hero);
            Assert.That(command.SlotIndex, Is.EqualTo(AcademyEnemyGrowthRuntime.CommandSkillIndex));
            Assert.That(plans.GetPublicIntent(state, ram, hero).ActionName, Is.EqualTo("冲压蓄能"));
        }

        [Test]
        public void AcademyPlan_DoesNotFallBackToGenericAttackOrPursuit()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState raider = new UnitState("raider", false, new GridPosition(4, 4));
            EnemyArchetypes.Get("raider").Apply(raider);
            CombatState state = new CombatState(new GridMap(5, 5), new[] { hero, raider });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachAcademyEnemyGrowth(new AcademyEnemyGrowthRuntime());
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            CombatResolver.BeginTurn(state, raider.Id);
            CombatCommand growth = new EnemyTurnPlanBook().GetExecutionCommand(state, raider, hero);
            Assert.That(growth.SlotIndex, Is.EqualTo(AcademyEnemyGrowthRuntime.CommandSkillIndex));
            CombatResolver.Resolve(state, growth);
            CombatResolver.BeginTurn(state, raider.Id);
            CombatCommand next = new EnemyTurnPlanBook().GetExecutionCommand(state, raider, hero);
            Assert.That(next.Type, Is.EqualTo(CombatCommandType.EndTurn));
            Assert.That(EnemyTurnPlanBook.KeepDeclaredAcademyIntent(state, raider, hero,
                CombatCommand.Attack(raider.Id, hero.Id)).Type, Is.EqualTo(CombatCommandType.EndTurn));
        }

        [Test]
        public void AcademyPlan_PreservesOnlyNamedRetreatAndTraceMovement()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState arbalist = new UnitState("arbalist", false, new GridPosition(1, 0));
            EnemyArchetypes.Get("rune_arbalist").Apply(arbalist);
            CombatState state = new CombatState(new GridMap(6, 6), new[] { hero, arbalist });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatCommand retreat = CombatCommand.Move(arbalist.Id, new GridPosition(2, 0));
            Assert.That(EnemyTurnPlanBook.KeepDeclaredAcademyIntent(state, arbalist, hero, retreat).Type,
                Is.EqualTo(CombatCommandType.Move));
            Assert.That(EnemyTurnPlanBook.KeepDeclaredAcademyIntent(state, arbalist, hero,
                CombatCommand.Move(arbalist.Id, new GridPosition(0, 1))).Type,
                Is.EqualTo(CombatCommandType.EndTurn));

            UnitState tracker = new UnitState("tracker", false, new GridPosition(4, 4));
            EnemyArchetypes.Get("elder_tracker_hound").Apply(tracker);
            CombatState traceState = new CombatState(new GridMap(6, 6),
                new[] { new UnitState("hero", true, new GridPosition(0, 0)), tracker });
            TileState trace = traceState.Map.GetTile(new GridPosition(3, 4)).Clone();
            trace.HasTrace = true;
            traceState.Map.SetTile(new GridPosition(3, 4), trace);
            CombatCommand traceStep = CombatCommand.Move(tracker.Id, new GridPosition(3, 4));
            Assert.That(EnemyTurnPlanBook.KeepDeclaredAcademyIntent(traceState, tracker,
                traceState.GetUnit("hero"), traceStep).Type, Is.EqualTo(CombatCommandType.Move));
        }

        [TestCase("elite_vanguard")]
        [TestCase("breach_ram")]
        [TestCase("signal_keeper")]
        [TestCase("wind_librarian")]
        [TestCase("legacy_storekeeper")]
        [TestCase("prototype_hand")]
        [TestCase("lantern_revealer")]
        public void AcademyPlan_DoesNotCastDescriptiveSkillAsATargetedSpell(string archetypeId)
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0));
            EnemyArchetypes.Get(archetypeId).Apply(enemy);
            CombatState state = new CombatState(new GridMap(5, 5), new[] { hero, enemy });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatCommand command = EnemyTurnPlanBook.KeepDeclaredAcademyIntent(state, enemy, hero,
                CombatCommand.UseSkill(enemy.Id, 0, hero.Id));
            Assert.That(command.Type, Is.EqualTo(CombatCommandType.EndTurn));
        }

        [Test]
        public void SubsequentAcademyBattle_AttachesGrowthRuntimeThroughFormalSessionBuilder()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateSubsequentAcademyRun(4362);
            run.ConfirmAcademyDeparture();
            RogueliteMapNode combat = run.AvailableNodes.FirstOrDefault(node => node.IsCombat);
            Assert.That(combat, Is.Not.Null);
            run.SelectNode(combat.Id);

            CombatState state = new CombatSceneSessionBuilder().Build(run, null,
                Array.Empty<CombatSceneMarker>()).State;
            Assert.That(state.AcademyEnemyGrowth, Is.Not.Null);
        }
    }
}
