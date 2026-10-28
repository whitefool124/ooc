using System;

namespace OCC.Combat.Presentation
{
    public enum EnemyTurnAdvanceKind
    {
        None,
        BeginAction,
        ResolveCommand,
        EndAction,
        ReadyForNext,
        InvalidActor,
        ActorChanged
    }

    public readonly struct EnemyTurnAdvance
    {
        public EnemyTurnAdvanceKind Kind { get; }
        public string UnitId { get; }
        public CombatCommandType CommandType { get; }
        public CombatCommand? Command { get; }

        public EnemyTurnAdvance(EnemyTurnAdvanceKind kind, string unitId = null,
            CombatCommandType commandType = default, CombatCommand? command = null)
        {
            Kind = kind;
            UnitId = unitId;
            CommandType = commandType;
            Command = command;
        }
    }

    /// <summary>
    /// Owns the transient lifecycle of one presented enemy action. Combat rules and state mutation
    /// remain with the session host; this coordinator only selects when those operations are due.
    /// </summary>
    public sealed class EnemyTurnCoordinator
    {
        private readonly EnemyTurnSequence sequence = new EnemyTurnSequence();
        private CombatCommand? pendingCommand;

        public EnemyTurnSequencePhase Phase => sequence.Phase;
        public string UnitId => sequence.UnitId;
        public bool IsRunning => sequence.IsRunning;

        public EnemyTurnAdvance Advance(UnitState enemy, float now, Func<UnitState, CombatCommand> commandFactory)
        {
            if (sequence.Phase == EnemyTurnSequencePhase.ActorGap)
            {
                if (sequence.Advance(now) != EnemyTurnSequenceSignal.ReadyForNext)
                    return new EnemyTurnAdvance(EnemyTurnAdvanceKind.None, sequence.UnitId, sequence.CommandType);
                pendingCommand = null;
                return new EnemyTurnAdvance(EnemyTurnAdvanceKind.ReadyForNext);
            }

            if (enemy == null || !enemy.IsAlive)
            {
                Reset();
                return new EnemyTurnAdvance(EnemyTurnAdvanceKind.InvalidActor, enemy?.Id);
            }

            if (!sequence.IsRunning)
            {
                if (enemy.ActionPoints > 0 && commandFactory == null)
                    throw new ArgumentNullException(nameof(commandFactory));
                pendingCommand = enemy.ActionPoints > 0 ? commandFactory(enemy) : (CombatCommand?)null;
                CombatCommandType commandType = pendingCommand?.Type ?? CombatCommandType.EndTurn;
                sequence.Begin(enemy.Id, commandType, now);
                return new EnemyTurnAdvance(EnemyTurnAdvanceKind.BeginAction, enemy.Id, commandType);
            }

            if (!string.Equals(sequence.UnitId, enemy.Id, StringComparison.Ordinal))
            {
                string previousUnitId = sequence.UnitId;
                Reset();
                return new EnemyTurnAdvance(EnemyTurnAdvanceKind.ActorChanged, previousUnitId);
            }

            EnemyTurnSequenceSignal signal = sequence.Advance(now);
            if (signal == EnemyTurnSequenceSignal.ResolveCommand)
                return new EnemyTurnAdvance(EnemyTurnAdvanceKind.ResolveCommand, enemy.Id,
                    sequence.CommandType, pendingCommand);
            if (signal == EnemyTurnSequenceSignal.EndTurn)
            {
                pendingCommand = null;
                return new EnemyTurnAdvance(EnemyTurnAdvanceKind.EndAction, enemy.Id, sequence.CommandType);
            }
            return new EnemyTurnAdvance(EnemyTurnAdvanceKind.None, enemy.Id, sequence.CommandType);
        }

        public void Reset()
        {
            pendingCommand = null;
            sequence.Reset();
        }
    }
}
