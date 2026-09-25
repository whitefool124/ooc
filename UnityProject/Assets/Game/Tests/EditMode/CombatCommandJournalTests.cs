using System;
using System.Collections.Generic;
using NUnit.Framework;
using OCC.Combat.Roguelite;
using OCC.Combat.Presentation;

namespace OCC.Combat.Tests
{
    public sealed class CombatCommandJournalTests
    {
        [Test]
        public void CommandEntry_RoundTripsAllTargetFields()
        {
            CombatCommand original = CombatCommand.UseSkillAt("hero", 5,
                new GridPosition(4, 6), CardinalDirection.West);
            CombatJournalEntry decoded = CombatJournalEntry.Decode(CombatJournalEntry.Accepted(original).Encode());

            Assert.That(decoded.Kind, Is.EqualTo(CombatJournalKind.Command));
            Assert.That(decoded.Command.Type, Is.EqualTo(original.Type));
            Assert.That(decoded.Command.UnitId, Is.EqualTo(original.UnitId));
            Assert.That(decoded.Command.Destination, Is.EqualTo(original.Destination));
            Assert.That(decoded.Command.AimDirection, Is.EqualTo(original.AimDirection));
            Assert.That(decoded.Command.SlotIndex, Is.EqualTo(original.SlotIndex));
        }

        [Test]
        public void ActiveCombatJournal_RoundTripsThroughTheVerifiedRunFormat()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateFirstRunV1(7641);
            run.AcknowledgeFirstRunOrigin();
            run.SelectNode("B1");
            run.BeginCombatJournal();
            run.AppendCombatJournal(CombatJournalEntry.Accepted(CombatCommand.Move("hero", new GridPosition(2, 3))));
            run.AppendCombatJournal(CombatJournalEntry.EnemyTurnEnded("enemy_0"));

            RogueRunDto saved = Rogue11Serializer.Deserialize(Rogue11Serializer.Serialize(run.RogueRunState));
            RogueliteMapRun restored = RogueliteMapRun.FromRogue11(saved);

            Assert.That(restored.HasActiveCombat, Is.True);
            Assert.That(restored.CombatJournalRows.Count, Is.EqualTo(2));
            Assert.That(CombatJournalEntry.Decode(restored.CombatJournalRows[1]).Kind,
                Is.EqualTo(CombatJournalKind.EndEnemyTurn));
            Assert.That(RogueliteMapRunValidator.Validate(restored).IsValid, Is.True,
                RogueliteMapRunValidator.Validate(restored).Summary);
        }

        [Test]
        public void CorruptJournal_IsRejectedWithoutAcceptingAChangedCommand()
        {
            RogueRunDto dto = RogueRunDto.CreateNew("journal-test", 7642);
            dto.ActiveCombatNodeId = "B1";
            dto.CombatJournalRows.Add("unsupported;row");

            Assert.Throws<InvalidOperationException>(() => Rogue11Serializer.Serialize(dto));
        }

        [Test]
        public void ReplayAfterActivation_RebuildsTheSameRogueliteTurnState()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateFirstRunV1(7643);
            run.AcknowledgeFirstRunOrigin();
            run.SelectNode("B1");
            CombatSceneSessionBuilder builder = new CombatSceneSessionBuilder();
            CombatState original = builder.Build(run, null, Array.Empty<CombatSceneMarker>()).State;
            CombatResolver.AdvanceToNextTurn(original);
            CombatCommand command = CombatCommand.EndTurn("hero");
            CombatCommandExecutionResult execution = new CombatCommandExecutionService().Execute(
                original, new FireBattleState(original), command, true);
            Assert.That(execution.Accepted, Is.True);

            CombatState restored = builder.Build(run, null, Array.Empty<CombatSceneMarker>()).State;
            CombatResolver.AdvanceToNextTurn(restored);
            CombatJournalReplayer.ReplayAfterActivation(restored,
                new[] { CombatJournalEntry.Accepted(command).Encode() });

            Assert.That(restored.ActiveUnitId, Is.EqualTo(original.ActiveUnitId));
            Assert.That(restored.TurnSequence, Is.EqualTo(original.TurnSequence));
            Assert.That(restored.GetUnit("hero").Health, Is.EqualTo(original.GetUnit("hero").Health));
            Assert.That(restored.GetUnit("hero").ActionPoints, Is.EqualTo(original.GetUnit("hero").ActionPoints));
            Assert.That(restored.RogueSpells, Is.Not.Null);
        }

        [Test]
        public void ReplayAfterActivation_RestoresPresentationAndEquipmentBeforeTheNextCommand()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateFirstRunV1(7644);
            run.AcknowledgeFirstRunOrigin();
            run.SelectNode("B1");
            CombatSceneSessionBuilder builder = new CombatSceneSessionBuilder();
            CombatState original = builder.Build(run, null, Array.Empty<CombatSceneMarker>()).State;
            CombatResolver.AdvanceToNextTurn(original);
            var liveFire = new FireBattleState(original);
            liveFire.BeginUnitTurn("hero");
            new ArtifactBattleState(original).BeginUnitTurn("hero");
            Assert.That(original.RogueEquipment.AssignQuickbar(0, null), Is.True);
            CombatCommand command = CombatCommand.EndTurn("hero");
            Assert.That(new CombatCommandExecutionService().Execute(original, liveFire, command, true).Accepted, Is.True);

            CombatState restored = builder.Build(run, null, Array.Empty<CombatSceneMarker>()).State;
            CombatResolver.AdvanceToNextTurn(restored);
            CombatJournalReplayResult replay = CombatJournalReplayer.ReplayAfterActivation(restored, new[]
            {
                CombatJournalEntry.PresentationTurnStarted("hero").Encode(),
                CombatJournalEntry.EquipmentChanged("quickbar", null, 0).Encode(),
                CombatJournalEntry.Accepted(command).Encode()
            });

            Assert.That(replay.ArtifactBattle, Is.Not.Null);
            Assert.That(restored.ActiveUnitId, Is.EqualTo(original.ActiveUnitId));
            Assert.That(restored.RogueEquipment.ItemQuickbarInstanceIds[0], Is.EqualTo(original.RogueEquipment.ItemQuickbarInstanceIds[0]));
        }

        [Test]
        public void ReplayAfterActivation_RestoresAnEnemyAndHeroTurnExchange()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateFirstRunV1(7647);
            run.AcknowledgeFirstRunOrigin();
            run.SelectNode("B1");
            CombatSceneSessionBuilder builder = new CombatSceneSessionBuilder();
            CombatState live = builder.Build(run, null, Array.Empty<CombatSceneMarker>()).State;
            CombatResolver.AdvanceToNextTurn(live);
            FireBattleState liveFire = new FireBattleState(live);
            ArtifactBattleState liveArtifacts = new ArtifactBattleState(live);
            CombatCommandExecutionService executor = new CombatCommandExecutionService();
            List<string> journal = new List<string>();

            liveFire.BeginUnitTurn("hero");
            liveArtifacts.BeginUnitTurn("hero");
            journal.Add(CombatJournalEntry.PresentationTurnStarted("hero").Encode());
            CombatCommand heroEnd = CombatCommand.EndTurn("hero");
            CombatCommandExecutionResult heroResult = executor.Execute(live, liveFire, heroEnd, true);
            Assert.That(heroResult.Accepted, Is.True);
            liveFire = heroResult.FireBattle;
            journal.Add(CombatJournalEntry.Accepted(heroEnd).Encode());

            int enemyTurns = 0;
            while (!live.IsVictory && !live.IsDefeat && live.ActiveUnitId != "hero" && enemyTurns < 10)
            {
                UnitState enemy = live.GetUnit(live.ActiveUnitId);
                Assert.That(enemy, Is.Not.Null);
                liveFire.BeginUnitTurn(enemy.Id);
                liveArtifacts.BeginUnitTurn(enemy.Id);
                journal.Add(CombatJournalEntry.PresentationTurnStarted(enemy.Id).Encode());
                CombatCommand planned = new EnemyTurnPlanBook().GetExecutionCommand(live, enemy, live.GetUnit("hero"));
                if (planned.Type != CombatCommandType.EndTurn)
                {
                    CombatCommandExecutionResult enemyResult = executor.Execute(live, liveFire, planned, true);
                    Assert.That(enemyResult.Accepted, Is.True, planned.Type.ToString());
                    liveFire = enemyResult.FireBattle;
                    journal.Add(CombatJournalEntry.Accepted(planned).Encode());
                }
                if (!live.IsVictory && !live.IsDefeat && live.ActiveUnitId == enemy.Id)
                {
                    CombatResolver.EndTurn(live, enemy);
                    journal.Add(CombatJournalEntry.EnemyTurnEnded(enemy.Id).Encode());
                }
                enemyTurns++;
            }
            Assert.That(enemyTurns, Is.GreaterThan(0));
            Assert.That(live.ActiveUnitId, Is.EqualTo("hero"));

            run.BeginCombatJournal();
            foreach (string row in journal) run.AppendCombatJournal(CombatJournalEntry.Decode(row));
            RogueliteMapRun reloadedRun = RogueliteMapRun.FromRogue11(
                Rogue11Serializer.Deserialize(Rogue11Serializer.Serialize(run.RogueRunState)));
            Assert.That(RogueliteMapRunValidator.Validate(reloadedRun).IsValid, Is.True);
            CombatState restored = builder.Build(reloadedRun, null, Array.Empty<CombatSceneMarker>()).State;
            CombatResolver.AdvanceToNextTurn(restored);
            CombatJournalReplayer.ReplayAfterActivation(restored, reloadedRun.CombatJournalRows);

            Assert.That(restored.ActiveUnitId, Is.EqualTo(live.ActiveUnitId));
            Assert.That(restored.TurnSequence, Is.EqualTo(live.TurnSequence));
            foreach (UnitState original in live.Units.Values)
            {
                UnitState replayed = restored.GetUnit(original.Id);
                Assert.That(replayed.Position, Is.EqualTo(original.Position), original.Id);
                Assert.That(replayed.Health, Is.EqualTo(original.Health), original.Id);
                Assert.That(replayed.ActionPoints, Is.EqualTo(original.ActionPoints), original.Id);
            }
        }

        [Test]
        public void ArtifactEntry_RoundTripsTargetAndCharges()
        {
            ArtifactTarget target = ArtifactTarget.Pair("enemy_0", "hero", new GridPosition(3, 4));
            CombatJournalEntry entry = CombatJournalEntry.ArtifactUsed("G-T01", "tool-1", target, 2);
            CombatJournalEntry decoded = CombatJournalEntry.Decode(entry.Encode());
            Assert.That(decoded.Kind, Is.EqualTo(CombatJournalKind.Artifact));
            Assert.That(decoded.Payload, Is.EqualTo(entry.Payload));
        }

        [Test]
        public void ReplayAfterActivation_RestoresTacticalArtifactEffectAndCharge()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateFirstRunV1(7645);
            run.AcknowledgeFirstRunOrigin();
            run.SelectNode("B1");
            RogueEquipmentRuntime equipment = RogueEquipmentRuntime.FromDto(run.RogueRunState);
            RogueTacticalItemInstance tool = equipment.CreateTacticalItem("journal-tool", "G-T01", 20, "test");
            Assert.That(equipment.AddTacticalToBackpack(tool), Is.True);
            equipment.WriteToDto(run.RogueRunState);

            CombatSceneSessionBuilder builder = new CombatSceneSessionBuilder();
            CombatState original = builder.Build(run, null, Array.Empty<CombatSceneMarker>()).State;
            CombatResolver.AdvanceToNextTurn(original);
            new FireBattleState(original).BeginUnitTurn("hero");
            ArtifactBattleState liveArtifact = new ArtifactBattleState(original);
            liveArtifact.BeginUnitTurn("hero");
            ArtifactTarget target = ArtifactTarget.Unit("hero", original.GetUnit("hero").Position);
            int uses = original.RogueEquipment.TacticalItem(tool.InstanceId).ChargesCurrent;
            ArtifactEngine.Execute(liveArtifact, "hero", ArtifactCatalog.Get("G-T01"), target, uses);
            Assert.That(original.RogueEquipment.TacticalItem(tool.InstanceId).Consume(), Is.True);

            CombatState restored = builder.Build(run, null, Array.Empty<CombatSceneMarker>()).State;
            CombatResolver.AdvanceToNextTurn(restored);
            CombatJournalReplayer.ReplayAfterActivation(restored, new[]
            {
                CombatJournalEntry.PresentationTurnStarted("hero").Encode(),
                CombatJournalEntry.ArtifactUsed("G-T01", tool.InstanceId, target, uses).Encode()
            });

            Assert.That(restored.GetUnit("hero").Shield, Is.EqualTo(original.GetUnit("hero").Shield));
            Assert.That(restored.RogueEquipment.TacticalItem(tool.InstanceId).ChargesCurrent,
                Is.EqualTo(original.RogueEquipment.TacticalItem(tool.InstanceId).ChargesCurrent));
        }
    }
}
