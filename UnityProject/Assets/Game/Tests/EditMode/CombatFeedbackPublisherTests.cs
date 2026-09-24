using System.Collections.Generic;
using NUnit.Framework;
using OCC.Combat.Presentation;

namespace OCC.Combat.Tests
{
    public sealed class CombatFeedbackPublisherTests
    {
        [Test]
        public void PublishCombatEffects_MapsMovementAndResolvedDamageInOrder()
        {
            CombatState state = State(out UnitState hero, out UnitState enemy);
            CombatResolver.BeginTurn(state, hero.Id);
            RecordingSink sink = new RecordingSink();
            CombatFeedbackPublisher publisher = new CombatFeedbackPublisher();

            CombatEffectExecution movement = CombatResolver.Resolve(state,
                CombatCommand.Move(hero.Id, new GridPosition(1, 0)));
            publisher.PublishCombatEffects(state, sink, movement);
            CombatEffectExecution attack = CombatResolver.Resolve(state,
                CombatCommand.Attack(hero.Id, enemy.Id));
            publisher.PublishCombatEffects(state, sink, attack);

            Assert.That(sink.Movements, Has.Count.EqualTo(1));
            Assert.That(sink.Movements[0], Is.EqualTo("(0, 0)>(1, 0)"));
            Assert.That(sink.Events, Is.Not.Empty);
            Assert.That(sink.Events[0].Kind, Is.EqualTo(CombatFeedbackKind.ShieldAbsorb));
        }

        [Test]
        public void PublishFireExecutions_UsesSourceSnapshotAfterUnitsMoveAndPreservesLogText()
        {
            CombatState state = State(out UnitState hero, out UnitState enemy);
            GridPosition sourceAtResolution = hero.Position, targetAtResolution = enemy.Position;
            FireSpellDefinition spell = FireSpellCatalog.Get("F-P-U01");
            FireSpellPreview preview = new FireSpellPreview(spell, new string[0],
                new[] { enemy.Position }, new[] { enemy.Id }, new GridPosition[0], false, false);
            FireSpellExecution execution = new FireSpellExecution(preview, new[]
            {
                new FireSpellResultStep(0, spell.Id, FireRuleKind.Damage, enemy.Id,
                    enemy.Position, 2, 2, "test")
            }, hero.Id, sourceAtResolution, true);
            CombatResolver.BeginTurn(state, hero.Id);
            CombatResolver.Resolve(state, CombatCommand.Move(hero.Id, new GridPosition(0, 1)));
            CombatResolver.BeginTurn(state, enemy.Id);
            CombatResolver.Resolve(state, CombatCommand.Move(enemy.Id, new GridPosition(3, 0)));
            int health = hero.Health, mana = hero.Mana, ap = hero.ActionPoints;
            RecordingSink sink = new RecordingSink();
            List<string> logs = new List<string>();

            new CombatFeedbackPublisher().PublishFireExecutions(state, sink,
                new[] { execution }, logs.Add);

            Assert.That(sink.FireSources, Is.EqualTo(new[] { sourceAtResolution }));
            Assert.That(sink.FireTargets[0], Is.EqualTo(new[] { targetAtResolution }));
            Assert.That(sink.FireTriggers, Is.EqualTo(new[] { true }));
            Assert.That(hero.Position, Is.EqualTo(new GridPosition(0, 1)));
            Assert.That(enemy.Position, Is.EqualTo(new GridPosition(3, 0)));
            Assert.That(new[] { hero.Health, hero.Mana, hero.ActionPoints }, Is.EqualTo(new[] { health, mana, ap }));
            Assert.That(logs, Is.EqualTo(new[] { spell.DisplayName + "：产生 1 项结果" }));
        }

        [Test]
        public void AttributeStack_EmitsUpdatedSignedValueEvenWhenDurationStaysTheSame()
        {
            CombatState state = State(out UnitState hero, out UnitState enemy);
            RecordingSink sink = new RecordingSink();
            CombatEffectExecution first = CombatEffectExecutor.Execute(state, hero.Id,
                CombatEffect.ApplyStatus(enemy.Id, StatusType.Agility, 2, -1));
            var publisher = new CombatFeedbackPublisher();
            publisher.PublishCombatEffects(state, sink, first);
            CombatEffectExecution second = CombatEffectExecutor.Execute(state, hero.Id,
                CombatEffect.ApplyStatus(enemy.Id, StatusType.Agility, 2, -1));

            publisher.PublishCombatEffects(state, sink, second);

            Assert.That(sink.Events, Has.Count.EqualTo(2));
            Assert.That(sink.Events[0].FloatingText, Is.EqualTo("敏捷-1"));
            Assert.That(sink.Events[1].Kind, Is.EqualTo(CombatFeedbackKind.Attribute));
            Assert.That(sink.Events[1].FloatingText, Is.EqualTo("敏捷-2"));
        }

        private static CombatState State(out UnitState hero, out UnitState enemy)
        {
            hero = new UnitState("hero", true, new GridPosition(0, 0));
            enemy = new UnitState("enemy", false, new GridPosition(2, 0));
            return new CombatState(new GridMap(4, 2), new[] { hero, enemy },
                new CombatObjective[] { new EliminationObjective() });
        }

        private sealed class RecordingSink : IResolvedCombatFeedbackSink
        {
            public List<CombatFeedbackEvent> Events { get; } = new List<CombatFeedbackEvent>();
            public List<string> Movements { get; } = new List<string>();
            public List<GridPosition> FireSources { get; } = new List<GridPosition>();
            public List<IReadOnlyList<GridPosition>> FireTargets { get; } = new List<IReadOnlyList<GridPosition>>();
            public List<bool> FireTriggers { get; } = new List<bool>();
            public void Publish(CombatFeedbackEvent feedback) => Events.Add(feedback);
            public void NotifyMovement(string unitId, GridPosition source, GridPosition target, IReadOnlyList<GridPosition> path) => Movements.Add(source + ">" + target);
            public void NotifyStatusApplied(GridPosition position, StatusType status, int duration) { }
            public void NotifyDestructible(GridPosition position, TileState tile) { }
            public void NotifyFireSpell(FireSpellExecution execution)
            { FireSources.Add(execution.SourcePosition); FireTargets.Add(execution.Preview.Cells); FireTriggers.Add(execution.IsTriggered); }
        }
    }
}
