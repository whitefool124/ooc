using System;
using System.Collections.Generic;

namespace OCC.Combat
{
    // Read-only player-facing contract. Presentation asks this service; it never reproduces combat rules.
    public sealed class CombatAvailabilityQuery
    {
        private readonly BattlefieldPresentationAdapter adapter = new BattlefieldPresentationAdapter();
        public CombatActionPreview Preview(CombatState state, string action, string targetId) => adapter.BuildPreview(state, action, targetId);
        public string CellFailure(CombatState state, string action, GridPosition position) => adapter.InvalidReasonForCell(state, action, position);
    }

    // Keeps executable enemy commands out of presentation while intent and execution share one stable plan.
    public sealed class EnemyTurnPlanBook
    {
        private readonly Dictionary<string, CombatCommand> commands = new Dictionary<string, CombatCommand>(StringComparer.Ordinal);
        public EnemyIntentPresentation GetPublicIntent(CombatState state, UnitState enemy, UnitState hero) =>
            state == null || enemy == null || hero == null ? null : CombatInformationPresenter.BuildEnemyIntent(state, enemy, GetOrCreate(state, enemy, hero));
        public CombatCommand GetExecutionCommand(CombatState state, UnitState enemy, UnitState hero) => GetOrCreate(state, enemy, hero);
        public void Invalidate() => commands.Clear();
        public bool HasPlanFor(string enemyId) => !string.IsNullOrEmpty(enemyId) && commands.ContainsKey(enemyId);
        private CombatCommand GetOrCreate(CombatState state, UnitState enemy, UnitState hero)
        {
            if (!commands.TryGetValue(enemy.Id, out CombatCommand command)) { command = EnemyTactics.Choose(state, enemy, hero); commands.Add(enemy.Id, command); }
            return command;
        }
    }
}
