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
                CombatCommand.Move(hero.Id, new GridPosition(1, 0), Facing.East));
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
            CombatResolver.Resolve(state, CombatCommand.Move(hero.Id, new GridPosition(0, 1), Facing.East));
            CombatResolver.BeginTurn(state, enemy.Id);
            CombatResolver.Resolve(state, CombatCommand.Move(enemy.Id, new GridPosition(3, 0), Facing.West));
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

        private static CombatState State(out UnitState hero, out UnitState enemy)
        {
            hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            enemy = new UnitState("enemy", false, new GridPosition(2, 0), Facing.West);
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
            public void NotifyMovement(GridPosition source, GridPosition target) => Movements.Add(source + ">" + target);
            public void NotifyStatusApplied(GridPosition position, StatusType status, int duration) { }
            public void NotifyDestructible(GridPosition position, TileState tile) { }
            public void NotifyFireSpell(FireSpellDefinition spell, GridPosition source,
                IReadOnlyList<GridPosition> targetCells, bool isTriggered = false)
            { FireSources.Add(source); FireTargets.Add(targetCells); FireTriggers.Add(isTriggered); }
        }
    }
}
