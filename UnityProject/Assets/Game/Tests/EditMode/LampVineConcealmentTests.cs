using System;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class LampVineConcealmentTests
    {
        private static CombatState Fixture(bool hiddenHero = false)
        {
            var hero = new UnitState("hero", true, hiddenHero ? new GridPosition(3, 2) : new GridPosition(1, 1));
            hero.Equip(CombatCatalog.Rifle, CombatCatalog.Shield, CombatCatalog.FireBolt, CombatCatalog.FrostBind);
            var enemy = new UnitState("enemy", false, hiddenHero ? new GridPosition(1, 1) : new GridPosition(3, 2));
            EnemyArchetypes.Get("pyromancer").Apply(enemy);
            var state = new CombatState(new GridMap(8, 5), new[] { hero, enemy }); state.ConfigureRuleset(CombatRuleset.Roguelite); state.ConfigureLampVineSeed(912);
            state.Map.SetTile(new GridPosition(3, 1), new TileState { IsLampVine = true, Durability = 6 }); state.Map.SetTile(new GridPosition(4, 1), new TileState { IsLampVine = true, Durability = 6 });
            string id = hiddenHero ? hero.Id : enemy.Id; CombatResolver.BeginTurn(state, id);
            CombatResolver.Resolve(state, CombatCommand.Move(id, new GridPosition(3, 1)));
            if (!hiddenHero) CombatResolver.BeginTurn(state, id);
            CombatResolver.Resolve(state, CombatCommand.Move(id, new GridPosition(4, 1)));
            return state;
        }
        [Test]
        public void EntranceStaysPublicAndRealEnemyCellCannotBeInspectedOrTargeted()
        {
            var state = Fixture(); var hero = state.GetUnit("hero"); var enemy = state.GetUnit("enemy");
            Assert.That(state.LampVines.PublicCell(enemy.Id), Is.EqualTo(new GridPosition(3, 1)));
            Assert.That(CombatInformationPresenter.EnemyInspectionTargetAt(state, enemy.Position), Is.Null);
            Assert.That(new EnemyTurnPlanBook().GetPublicIntent(state, enemy, hero).Route, Is.Empty);
            CombatResolver.BeginTurn(state, hero.Id); int ap = hero.ActionPoints;
            Assert.Throws<InvalidOperationException>(() => CombatResolver.ResolveWeaponAttack(state, hero.Id, enemy.Id)); Assert.That(hero.ActionPoints, Is.EqualTo(ap));
        }
        [Test]
        public void PartialDamageDoesNotAdvanceAndCloningPreservesTheSearch()
        {
            var state = Fixture(); CombatResolver.BeginTurn(state, "hero"); var entry = new GridPosition(3, 1);
            CombatResolver.Resolve(state, CombatCommand.AttackCell("hero", entry));
            Assert.That(state.Map.GetTile(entry).Durability, Is.EqualTo(2)); Assert.That(state.LampVines.PublicCell("enemy"), Is.EqualTo(entry));
            var clone = state.Clone(); Assert.That(clone.LampVines.PublicCell("enemy"), Is.EqualTo(entry));
            CombatResolver.Resolve(state, CombatCommand.AttackCell("hero", entry)); CombatResolver.Resolve(clone, CombatCommand.AttackCell("hero", entry));
            Assert.That(clone.LampVines.PublicCell("enemy"), Is.EqualTo(state.LampVines.PublicCell("enemy")));
            Assert.That(state.LampVines.PublicCell("enemy"), Is.EqualTo(new GridPosition(4, 1)));
        }
        [Test]
        public void FindingUnitUsesOneNormalPacketForVineAndUnitAndRevealsIt()
        {
            var state = Fixture(); CombatResolver.BeginTurn(state, "hero"); var cell = new GridPosition(4, 1); var enemy = state.GetUnit("enemy"); int hp = enemy.Health;
            // Destroy the empty entrance with two ordinary attacks before searching the next public cell.
            CombatResolver.Resolve(state, CombatCommand.AttackCell("hero", new GridPosition(3, 1))); CombatResolver.Resolve(state, CombatCommand.AttackCell("hero", new GridPosition(3, 1)));
            CombatResolver.BeginTurn(state, "hero"); int mana = state.GetUnit("hero").Mana;
            CombatResolver.Resolve(state, CombatCommand.AttackCell("hero", cell));
            Assert.That(state.Map.GetTile(cell).Durability, Is.EqualTo(2)); Assert.That(enemy.Health, Is.EqualTo(hp - 4)); Assert.That(state.GetUnit("hero").Mana, Is.EqualTo(mana));
            Assert.That(state.LampVines.IsHiddenFrom(state, state.GetUnit("hero"), enemy), Is.False);
        }
        [Test]
        public void EnemySearchPlansUseKnownEntranceInsteadOfHiddenHeroPosition()
        {
            var state = Fixture(true); var plans = new EnemyTurnPlanBook(); var command = plans.GetExecutionCommand(state, state.GetUnit("enemy"), state.GetUnit("hero"));
            Assert.That(command.Type, Is.EqualTo(CombatCommandType.AttackCell)); Assert.That(command.Destination, Is.EqualTo(new GridPosition(3, 1)));
            Assert.That(plans.GetPublicIntent(state, state.GetUnit("enemy"), state.GetUnit("hero")).TargetSummary, Does.Not.Contain("(4, 1)"));
        }
        [Test]
        public void JournalReplayKeepsDamageAndTheSameSearchCandidate()
        {
            var original = Fixture(); var restored = Fixture();
            CombatResolver.BeginTurn(original, "hero"); CombatResolver.BeginTurn(restored, "hero");
            var command = CombatCommand.AttackCell("hero", new GridPosition(3, 1));
            CombatResolver.Resolve(original, command); CombatResolver.Resolve(original, command);
            CombatJournalReplayer.ReplayAfterActivation(restored, new[] { CombatJournalEntry.Accepted(command).Encode(), CombatJournalEntry.Accepted(command).Encode() });
            Assert.That(restored.LampVines.PublicCell("enemy"), Is.EqualTo(original.LampVines.PublicCell("enemy")));
            Assert.That(restored.Map.GetTile(command.Destination).Durability, Is.EqualTo(0));
            Assert.That(restored.GetUnit("hero").ActionPoints, Is.EqualTo(original.GetUnit("hero").ActionPoints));
        }
        [Test]
        public void ChargeDestroysTheKnownVineAndAdvancesOnlyAfterDamage()
        {
            var state = Fixture(true); var ram = state.GetUnit("enemy"); EnemyArchetypes.Get("breach_ram").Apply(ram);
            var runtime = new ThreeMaterialPressureRuntime();
            CombatResolver.BeginTurn(state, ram.Id); runtime.ResolveCharge(state, ram, state.LampVines.PublicCell("hero"));
            Assert.That(state.Map.GetTile(new GridPosition(3, 1)).Durability, Is.EqualTo(0));
            Assert.That(state.LampVines.PublicCell("hero"), Is.EqualTo(new GridPosition(4, 1)));
        }
        [Test]
        public void AttackCellJournalRoundTripRetainsCellAndOriginalSkillSlot()
        {
            var command = CombatCommand.AttackCell("hero", new GridPosition(3, 1), 0);
            var restored = CombatJournalEntry.Decode(CombatJournalEntry.Accepted(command).Encode()).Command;
            Assert.That(restored.Type, Is.EqualTo(command.Type)); Assert.That(restored.Destination, Is.EqualTo(command.Destination)); Assert.That(restored.SlotIndex, Is.EqualTo(0));
        }
    }
}
