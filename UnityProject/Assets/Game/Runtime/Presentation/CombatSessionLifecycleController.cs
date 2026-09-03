using System;

namespace OCC.Combat.Presentation
{
    public readonly struct CombatSessionActivation
    {
        public CombatState State { get; }
        public FireBattleState FireBattle { get; }
        public CombatEffectExecution InitialTurnEffects { get; }

        public CombatSessionActivation(CombatState state, FireBattleState fireBattle,
            CombatEffectExecution initialTurnEffects)
        {
            State = state;
            FireBattle = fireBattle;
            InitialTurnEffects = initialTurnEffects;
        }
    }

    public readonly struct CombatUnitLifecycleAdvance
    {
        public bool Changed { get; }
        public string UnitId { get; }

        public CombatUnitLifecycleAdvance(bool changed, string unitId)
        {
            Changed = changed;
            UnitId = unitId;
        }
    }

    /// <summary>
    /// Coordinates production-combat activation, restart and active-unit lifecycle boundaries.
    /// Training range sessions deliberately remain outside this controller.
    /// </summary>
    public sealed class CombatSessionLifecycleController
    {
        private string activeUnitId;

        public CombatSessionActivation Begin(CombatFlowController flow, EnemyTurnCoordinator enemyTurn,
            CombatOutcomeSettlementCoordinator outcomeSettlement)
        {
            if (flow == null) throw new ArgumentNullException(nameof(flow));
            flow.BeginCombat();
            return Activate(flow.State, enemyTurn, outcomeSettlement);
        }

        public CombatSessionActivation Restart(CombatFlowController flow, EnemyTurnCoordinator enemyTurn,
            CombatOutcomeSettlementCoordinator outcomeSettlement)
        {
            if (flow == null) throw new ArgumentNullException(nameof(flow));
            flow.TacticalRestart();
            CombatSessionActivation activation = Activate(flow.State, enemyTurn, outcomeSettlement);
            flow.ResumeAfterRestart();
            return activation;
        }

        public CombatUnitLifecycleAdvance ObserveActiveUnit(string unitId)
        {
            if (string.Equals(activeUnitId, unitId, StringComparison.Ordinal)) return default;
            activeUnitId = unitId;
            return new CombatUnitLifecycleAdvance(true, unitId);
        }

        public void ResetObservation() => activeUnitId = null;

        private CombatSessionActivation Activate(CombatState state, EnemyTurnCoordinator enemyTurn,
            CombatOutcomeSettlementCoordinator outcomeSettlement)
        {
            if (state == null) throw new InvalidOperationException("Combat flow has no state.");
            if (enemyTurn == null) throw new ArgumentNullException(nameof(enemyTurn));
            if (outcomeSettlement == null) throw new ArgumentNullException(nameof(outcomeSettlement));
            enemyTurn.Reset();
            outcomeSettlement.Reset();
            ResetObservation();
            FireBattleState fireBattle = new FireBattleState(state);
            CombatEffectExecution initialTurn = CombatResolver.BeginTurn(state, "hero");
            return new CombatSessionActivation(state, fireBattle, initialTurn);
        }
    }
}
