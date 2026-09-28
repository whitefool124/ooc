using System;
using System.Linq;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class ApprovedFireSpellTableTests
    {
        private readonly FireSpellTrainingRangeProvider range = new FireSpellTrainingRangeProvider();

        [Test]
        public void EveryApprovedSpell_HasACommitAndADeterministicResolution()
        {
            Assert.That(FireSpellCatalog.Version, Is.EqualTo("fire-personal-spells-v0.5-three-builds"));
            Assert.That(FireSpellCatalog.All.Count, Is.EqualTo(80));
            foreach (FireSpellDefinition spell in FireSpellCatalog.All)
            {
                FireSpellTrainingRangeCase first = (FireSpellTrainingRangeCase)range.Prepare(spell.Id);
                FireSpellTrainingRangeCase second = (FireSpellTrainingRangeCase)range.Prepare(spell.Id);
                Assert.That(first.Preview().CanCommit, Is.True, spell.Id + ": " + first.Preview().Summary);
                Assert.That(first.Execute().Signature(), Is.EqualTo(second.Execute().Signature()), spell.Id);
                Assert.That(range.PrepareIllegal(spell.Id).Preview().CanCommit, Is.False, spell.Id);
            }
        }

        [TestCase("F-P-M01", StatusType.Agility, 3, "hero")]
        [TestCase("F-P-U09", StatusType.Agility, 2, "hero")]
        [TestCase("F-P-U10", StatusType.ShieldEfficiency, 4, "hero")]
        [TestCase("F-P-U27", StatusType.DamageTaken, -4, "hero")]
        [TestCase("F-P-U19", StatusType.FiregroundBoost, 4, "range_ally")]
        [TestCase("F-P-R03", StatusType.FiregroundVulnerable, 4, "range_normal")]
        public void AttributeCards_UseTheSharedStatusSystem(string id, StatusType status, int strength, string targetId)
        {
            FireSpellTrainingRangeCase prepared = (FireSpellTrainingRangeCase)range.Prepare(id);
            prepared.Execute();
            Assert.That(prepared.Combat.GetUnit(targetId).StatusStrength(status), Is.EqualTo(strength), id);
            Assert.That(prepared.Battle.PendingEffects.Any(effect => effect.Spell.Id == id), Is.False, id);
        }

        [Test]
        public void BurningTransfer_MovesTheExistingStatusToTheSelectedRecipient()
        {
            FireSpellTrainingRangeCase prepared = (FireSpellTrainingRangeCase)range.Prepare("F-P-U02");
            Assert.That(prepared.Combat.GetUnit("range_shield").HasStatus(StatusType.Burning), Is.True);
            prepared.Execute();
            Assert.That(prepared.Combat.GetUnit("range_shield").HasStatus(StatusType.Burning), Is.False);
            Assert.That(prepared.Combat.GetUnit("range_normal").StatusStrength(StatusType.Burning), Is.EqualTo(8));
        }

        [Test]
        public void FiregroundMovement_ChangesTheChosenCellAndPreservesItsDuration()
        {
            FireSpellTrainingRangeCase moved = (FireSpellTrainingRangeCase)range.Prepare("F-P-R12");
            var original = new GridPosition(3, 2);
            var destination = moved.RecommendedCell;
            int remaining = moved.Battle.Firegrounds[original].RemainingTurns;
            moved.Execute();
            Assert.That(moved.Battle.HasFireground(original), Is.False);
            Assert.That(moved.Battle.Firegrounds[destination].RemainingTurns, Is.EqualTo(remaining));

            FireSpellTrainingRangeCase expanded = (FireSpellTrainingRangeCase)range.Prepare("F-P-R13");
            expanded.Execute();
            Assert.That(expanded.Battle.HasFireground(original), Is.True);
            Assert.That(expanded.Battle.HasFireground(expanded.RecommendedCell), Is.True);
        }

        [Test]
        public void ChargeTrace_CanBeReadForFireDamageAndRetreat()
        {
            FireSpellTrainingRangeCase fire = (FireSpellTrainingRangeCase)range.Prepare("F-P-U20");
            int health = fire.Combat.GetUnit("range_normal").Health;
            fire.Execute();
            Assert.That(fire.Combat.GetUnit("range_normal").Health, Is.LessThan(health));

            FireSpellTrainingRangeCase retreat = (FireSpellTrainingRangeCase)range.Prepare("F-P-M24");
            Assert.That(retreat.Combat.GetUnit("hero").Position, Is.EqualTo(new GridPosition(3, 2)));
            retreat.Execute();
            Assert.That(retreat.Combat.GetUnit("hero").Position, Is.EqualTo(new GridPosition(3, 4)));
        }

        [Test]
        public void BreachMove_UsesAnObjectDestroyedByTheCasterThisTurn()
        {
            FireSpellTrainingRangeCase prepared = (FireSpellTrainingRangeCase)range.Prepare("F-P-U26");
            Assert.That(prepared.Battle.DestroyedObjectsThisHeroTurn.Any(record =>
                record.Cell == prepared.RecommendedCell && record.DestroyerUnitId == "hero"), Is.True);
            prepared.Execute();
            Assert.That(prepared.Combat.GetUnit("hero").Position, Is.EqualTo(prepared.RecommendedCell));
        }

        [Test]
        public void TimelineCards_ChangeActionValueByTheDeclaredAmount()
        {
            FireSpellTrainingRangeCase delay = (FireSpellTrainingRangeCase)range.Prepare("F-P-R09");
            int enemyValue = delay.Combat.GetUnit("range_normal").ActionValue;
            delay.Execute();
            Assert.That(delay.Combat.GetUnit("range_normal").ActionValue, Is.EqualTo(Math.Max(0, enemyValue - 8)));

            FireSpellTrainingRangeCase advance = (FireSpellTrainingRangeCase)range.Prepare("F-P-U16");
            int heroValue = advance.Combat.GetUnit("hero").ActionValue;
            advance.Execute();
            Assert.That(advance.Combat.GetUnit("hero").ActionValue, Is.EqualTo(Math.Min(199, heroValue + 8)));
        }
    }
}
