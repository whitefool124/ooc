using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat.Presentation
{
    public interface IResolvedCombatFeedbackSink
    {
        void Publish(CombatFeedbackEvent feedback);
        void NotifyMovement(GridPosition source, GridPosition target);
        void NotifyStatusApplied(GridPosition position, StatusType status, int duration);
        void NotifyDestructible(GridPosition position, TileState tile);
        void NotifyFireSpell(FireSpellDefinition spell, GridPosition source,
            IReadOnlyList<GridPosition> targetCells);
    }

    /// <summary>
    /// Maps resolved domain results to presentation and log ports without mutating combat rules.
    /// </summary>
    public sealed class CombatFeedbackPublisher
    {
        public void PublishCombatEffects(CombatState state, IResolvedCombatFeedbackSink sink,
            CombatEffectExecution execution)
        {
            if (state == null || sink == null || execution == null) return;
            foreach (CombatEffectResult result in execution.Results)
            {
                UnitState source = state.GetUnit(result.SourceUnitId);
                GridPosition sourcePosition = source == null ? result.PositionBefore : source.Position;
                if (result.Kind == CombatEffectKind.Move && result.Changed)
                    sink.NotifyMovement(result.PositionBefore, result.PositionAfter);
                else if (result.Kind == CombatEffectKind.AbsorbShield && result.AppliedAmount > 0)
                    sink.Publish(new CombatFeedbackEvent(CombatFeedbackKind.ShieldAbsorb,
                        sourcePosition, result.PositionAfter, result.AppliedAmount));
                else if (result.Kind == CombatEffectKind.DamageHealth && result.AppliedAmount > 0)
                {
                    sink.Publish(new CombatFeedbackEvent(CombatFeedbackKind.Damage,
                        sourcePosition, result.PositionAfter, result.AppliedAmount));
                    if (result.ValueBefore > 0 && result.ValueAfter == 0)
                        sink.Publish(new CombatFeedbackEvent(CombatFeedbackKind.UnitDefeated,
                            sourcePosition, result.PositionAfter));
                }
                else if (result.Kind == CombatEffectKind.RestoreHealth && result.AppliedAmount > 0)
                    sink.Publish(new CombatFeedbackEvent(CombatFeedbackKind.Healing,
                        result.PositionAfter, result.AppliedAmount));
                else if (result.Kind == CombatEffectKind.RestoreShield && result.AppliedAmount > 0)
                    sink.Publish(new CombatFeedbackEvent(CombatFeedbackKind.ShieldRestore,
                        result.PositionAfter, result.AppliedAmount));
                else if (result.Kind == CombatEffectKind.RestoreMana && result.AppliedAmount > 0)
                    sink.Publish(new CombatFeedbackEvent(CombatFeedbackKind.ManaRestore,
                        result.PositionAfter, result.AppliedAmount));
                else if (result.Kind == CombatEffectKind.ApplyStatus && result.AppliedAmount > 0)
                    sink.NotifyStatusApplied(result.PositionAfter, result.Status, result.ValueAfter);
                else if (result.Kind == CombatEffectKind.ClearStatus && result.AppliedAmount > 0)
                    sink.Publish(new CombatFeedbackEvent(CombatFeedbackKind.StatusCleared, result.PositionAfter));
                else if (result.Kind == CombatEffectKind.DamageObject && result.AppliedAmount > 0)
                    sink.NotifyDestructible(result.PositionAfter, state.Map.GetTile(result.PositionAfter));
            }
        }

        public void PublishFireExecutions(CombatState state, IResolvedCombatFeedbackSink sink,
            IEnumerable<FireSpellExecution> executions, Action<string> addLog)
        {
            if (state == null || executions == null) return;
            foreach (FireSpellExecution execution in executions)
            {
                if (execution == null) continue;
                FireSpellDefinition spell = execution.Preview?.Spell;
                FireSpellResultStep firstStep = execution.Steps.FirstOrDefault();
                UnitState stepTarget = string.IsNullOrEmpty(firstStep.TargetId)
                    ? null : state.GetUnit(firstStep.TargetId);
                if (spell != null)
                    sink?.NotifyFireSpell(spell, stepTarget?.Position ??
                        execution.Preview.Cells.FirstOrDefault(), execution.Preview.Cells);
                addLog?.Invoke((spell?.DisplayName ?? "火术触发") + "：产生 " +
                    execution.Steps.Count + " 项结果");
            }
        }
    }
}
