using NUnit.Framework;
using OCC.Combat.Presentation;

namespace OCC.Combat.Tests
{
    public sealed class EnemyTurnSequenceTests
    {
        [Test]
        public void Begin_HoldsFocusBeforeResolvingTheCommand()
        {
            EnemyTurnSequence sequence = new EnemyTurnSequence();

            sequence.Begin("enemy-a", CombatCommandType.Attack, 10f);

            Assert.That(sequence.Phase, Is.EqualTo(EnemyTurnSequencePhase.Focus));
            Assert.That(sequence.Advance(10f + EnemyTurnSequence.FocusSeconds - .01f), Is.EqualTo(EnemyTurnSequenceSignal.None));
            Assert.That(sequence.Advance(10f + EnemyTurnSequence.FocusSeconds), Is.EqualTo(EnemyTurnSequenceSignal.ResolveCommand));
            Assert.That(sequence.Phase, Is.EqualTo(EnemyTurnSequencePhase.ResultHold));
        }

        [Test]
        public void Advance_EmitsEachMutationSignalOnlyOnce()
        {
            EnemyTurnSequence sequence = new EnemyTurnSequence();
            sequence.Begin("enemy-a", CombatCommandType.Move, 0f);

            Assert.That(sequence.Advance(EnemyTurnSequence.FocusSeconds), Is.EqualTo(EnemyTurnSequenceSignal.ResolveCommand));
            Assert.That(sequence.Advance(EnemyTurnSequence.FocusSeconds), Is.EqualTo(EnemyTurnSequenceSignal.None));

            float resultDeadline = EnemyTurnSequence.FocusSeconds + EnemyTurnSequence.ResultHoldFor(CombatCommandType.Move);
            Assert.That(sequence.Advance(resultDeadline), Is.EqualTo(EnemyTurnSequenceSignal.EndTurn));
            Assert.That(sequence.Advance(resultDeadline), Is.EqualTo(EnemyTurnSequenceSignal.None));
            Assert.That(sequence.Phase, Is.EqualTo(EnemyTurnSequencePhase.ActorGap));

            Assert.That(sequence.Advance(resultDeadline + EnemyTurnSequence.ActorGapSeconds), Is.EqualTo(EnemyTurnSequenceSignal.ReadyForNext));
            Assert.That(sequence.Phase, Is.EqualTo(EnemyTurnSequencePhase.Idle));
        }

        [Test]
        public void ResultTiming_KeepsSkillAndAttackReadableLongerThanMovement()
        {
            float movement = EnemyTurnSequence.ResultHoldFor(CombatCommandType.Move);
            float attack = EnemyTurnSequence.ResultHoldFor(CombatCommandType.Attack);
            float skill = EnemyTurnSequence.ResultHoldFor(CombatCommandType.UseSkill);

            Assert.That(movement, Is.GreaterThanOrEqualTo(.7f));
            Assert.That(attack, Is.GreaterThanOrEqualTo(movement));
            Assert.That(skill, Is.GreaterThanOrEqualTo(attack));
        }

        [Test]
        public void Begin_RejectsASecondActorUntilThePreviousSequenceFinishes()
        {
            EnemyTurnSequence sequence = new EnemyTurnSequence();
            sequence.Begin("enemy-a", CombatCommandType.Attack, 0f);

            Assert.That(() => sequence.Begin("enemy-b", CombatCommandType.Move, 0f),
                Throws.InvalidOperationException);
            Assert.That(sequence.UnitId, Is.EqualTo("enemy-a"));
        }

        [Test]
        public void Reset_DropsThePreviousActorWithoutEmittingACommand()
        {
            EnemyTurnSequence sequence = new EnemyTurnSequence();
            sequence.Begin("enemy-a", CombatCommandType.UseSkill, 3f);

            sequence.Reset();

            Assert.That(sequence.Phase, Is.EqualTo(EnemyTurnSequencePhase.Idle));
            Assert.That(sequence.UnitId, Is.Null);
            Assert.That(sequence.Advance(99f), Is.EqualTo(EnemyTurnSequenceSignal.None));
        }
    }
}
