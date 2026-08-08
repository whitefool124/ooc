using System.Collections.Generic;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class StructuralSafetyTests
    {
        [Test]
        public void EnemyArchetypeLookup_DoesNotSilentlySubstituteAnotherArchetype()
        {
            Assert.That(EnemyArchetypes.Get("sigil_mauler").Id, Is.EqualTo("sigil_mauler"));
            Assert.Throws<KeyNotFoundException>(() => EnemyArchetypes.Get("missing_archetype"));
        }

        [Test]
        public void EnemyTactics_UsesCapabilityRatherThanSceneInstanceId()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(3, 0), Facing.West);
            UnitState caster = new UnitState("enemy_7", false, new GridPosition(0, 0), Facing.East);
            caster.Equip(CombatCatalog.Rifle, null, CombatCatalog.FireBolt, null);
            caster.ConfigureMana(20);

            CombatCommand command = EnemyTactics.Choose(caster, hero);

            Assert.That(command.Type, Is.EqualTo(CombatCommandType.UseSkill));
            Assert.That(command.UnitId, Is.EqualTo("enemy_7"));
        }
    }
}
