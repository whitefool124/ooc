using System;

namespace OCC.Combat.Presentation
{
    public enum EnemyTurnSequencePhase
    {
        Idle,
        Focus,
        ResultHold,
        ActorGap
    }

    public enum EnemyTurnSequenceSignal
    {
        None,
        ResolveCommand,
        EndTurn,
        ReadyForNext
    }

    /// <summary>
    /// Pure presentation clock for enemy turns. It never reads or mutates combat state and never
    /// chooses an AI command; the combat coordinator consumes its one-shot signals.
    /// </summary>
    public sealed class EnemyTurnSequence
    {
        public const float FocusSeconds = .65f;
        public const float ActorGapSeconds = .30f;
        private const float MoveResultSeconds = .70f;
        private const float AttackResultSeconds = .90f;
        private const float SkillResultSeconds = 1.00f;
        private const float DefaultResultSeconds = .75f;

        public EnemyTurnSequencePhase Phase { get; private set; } = EnemyTurnSequencePhase.Idle;
        public string UnitId { get; private set; }
        public CombatCommandType CommandType { get; private set; }
        public float Deadline { get; private set; }
        public bool IsRunning => Phase != EnemyTurnSequencePhase.Idle;

        public void Begin(string unitId, CombatCommandType commandType, float now)
        {
            if (string.IsNullOrWhiteSpace(unitId)) throw new ArgumentException("An enemy unit is required.", nameof(unitId));
            if (IsRunning) throw new InvalidOperationException("The previous enemy action is still being presented.");

            UnitId = unitId;
            CommandType = commandType;
            Phase = EnemyTurnSequencePhase.Focus;
            Deadline = now + FocusSeconds;
        }

        public EnemyTurnSequenceSignal Advance(float now)
        {
            if (!IsRunning || now < Deadline) return EnemyTurnSequenceSignal.None;

            switch (Phase)
            {
                case EnemyTurnSequencePhase.Focus:
                    Phase = EnemyTurnSequencePhase.ResultHold;
                    Deadline = now + ResultHoldFor(CommandType);
                    return EnemyTurnSequenceSignal.ResolveCommand;
                case EnemyTurnSequencePhase.ResultHold:
                    Phase = EnemyTurnSequencePhase.ActorGap;
                    Deadline = now + ActorGapSeconds;
                    return EnemyTurnSequenceSignal.EndTurn;
                case EnemyTurnSequencePhase.ActorGap:
                    Reset();
                    return EnemyTurnSequenceSignal.ReadyForNext;
                default:
                    return EnemyTurnSequenceSignal.None;
            }
        }

        public void Reset()
        {
            Phase = EnemyTurnSequencePhase.Idle;
            UnitId = null;
            CommandType = default;
            Deadline = 0f;
        }

        public static float ResultHoldFor(CombatCommandType commandType)
        {
            switch (commandType)
            {
                case CombatCommandType.Move: return MoveResultSeconds;
                case CombatCommandType.Attack: return AttackResultSeconds;
                case CombatCommandType.UseSkill: return SkillResultSeconds;
                default: return DefaultResultSeconds;
            }
        }
    }
}
