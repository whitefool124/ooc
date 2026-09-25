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
        public EnemyIntentPresentation GetPublicIntent(CombatState state, UnitState enemy, UnitState hero)
        {
            if (state == null || enemy == null || hero == null) return null;
            CombatCommand command = GetOrCreate(state, enemy, hero);
            if (state.AcademyEnemyArea != null &&
                (state.AcademyEnemyArea.HasPending(enemy.Id) || command.Type == CombatCommandType.UseSkill &&
                    (command.SlotIndex == AcademyEnemyAreaRuntime.PrepareSkillIndex || command.SlotIndex == AcademyEnemyAreaRuntime.ResolveSkillIndex)))
                return state.AcademyEnemyArea.PresentIntent(state, enemy, command);
            if (command.Type == CombatCommandType.UseSkill && command.SlotIndex == AcademyEnemyGrowthRuntime.CommandSkillIndex &&
                state.AcademyEnemyGrowth != null)
                return state.AcademyEnemyGrowth.PresentIntent(enemy);
            EnemyIntentPresentation intent = state.RainLanternCourt != null ? state.RainLanternCourt.PresentIntent(state, enemy, command)
                : state.GreenhouseCollectionRoom != null ? state.GreenhouseCollectionRoom.PresentIntent(state, enemy, command)
                : state.ThreeMaterialPressure != null ? state.ThreeMaterialPressure.PresentIntent(state, enemy, command)
                : state.AcademyCoreBoss != null ? state.AcademyCoreBoss.PresentIntent(state, enemy, command)
                : state.AcademyFieldEnemy != null && AcademyFieldEnemyRuntime.Handles(enemy) ? state.AcademyFieldEnemy.PresentIntent(state, enemy, command)
                : state.PressureTest != null ? state.PressureTest.PresentIntent(state, enemy, command)
                : CombatInformationPresenter.BuildEnemyIntent(state, enemy, command);
            if (state.AcademyCoreBoss != null && enemy.EnemyArchetypeId == "core_overseer" &&
                command.Type == CombatCommandType.UseSkill) return intent;
            return intent?.WithMovementPreview(state, enemy, command).WithAttackPreview(state, enemy, command);
        }
        public CombatCommand GetExecutionCommand(CombatState state, UnitState enemy, UnitState hero) => GetOrCreate(state, enemy, hero);
        public void Invalidate() => commands.Clear();
        public bool HasPlanFor(string enemyId) => !string.IsNullOrEmpty(enemyId) && commands.ContainsKey(enemyId);
        private CombatCommand GetOrCreate(CombatState state, UnitState enemy, UnitState hero)
        {
            if (!commands.TryGetValue(enemy.Id, out CombatCommand command))
            {
                command = state.RainLanternCourt != null
                    ? state.RainLanternCourt.ChooseEnemyCommand(state, enemy, hero)
                    : state.GreenhouseCollectionRoom != null
                        ? state.GreenhouseCollectionRoom.ChooseEnemyCommand(state, enemy, hero)
                        : state.ThreeMaterialPressure != null
                            ? state.ThreeMaterialPressure.ChooseEnemyCommand(state, enemy, hero)
                        : state.AcademyCoreBoss != null
                            ? state.AcademyCoreBoss.ChooseEnemyCommand(state, enemy, hero)
                            : state.AcademyFieldEnemy != null && AcademyFieldEnemyRuntime.Handles(enemy)
                                ? state.AcademyFieldEnemy.ChooseEnemyCommand(state, enemy, hero)
                        : state.PressureTest != null
                            ? state.PressureTest.ChooseEnemyCommand(state, enemy, hero)
                        : EnemyTactics.Choose(state, enemy, hero);
                if (state.AcademyEnemyArea != null && state.AcademyEnemyGrowth != null)
                    command = KeepDeclaredAcademyIntent(state, enemy, hero, command);
                if (state.AcademyEnemyArea != null)
                    command = state.AcademyEnemyArea.Choose(state, enemy, hero, command);
                if (state.AcademyEnemyGrowth != null && state.AcademyEnemyArea?.HasPending(enemy.Id) != true)
                    command = state.AcademyEnemyGrowth.Choose(state, enemy, command);
                commands.Add(enemy.Id, command);
            }
            return command;
        }

        public static CombatCommand KeepDeclaredAcademyIntent(CombatState state, UnitState enemy, UnitState hero,
            CombatCommand command)
        {
            if (command.Type == CombatCommandType.Attack)
                return CombatCommand.EndTurn(enemy.Id);
            if (command.Type == CombatCommandType.Move)
            {
                bool arbalistRetreat = enemy.EnemyArchetypeId == "rune_arbalist" &&
                    command.Destination.ManhattanDistance(hero.Position) >
                        enemy.Position.ManhattanDistance(hero.Position) &&
                    (enemy.Position.ManhattanDistance(hero.Position) < EnemyAbilityCatalog.WindlassBolt.MinimumRange ||
                        !state.HasLineOfSight(enemy.Position, hero.Position));
                bool trackerFollowingTrace = enemy.EnemyArchetypeId == "elder_tracker_hound" &&
                    state.Map.IsInside(command.Destination) && state.Map.GetTile(command.Destination).HasTrace;
                bool followingDecoy = state.ArtifactBattle?.TryGetLureTarget(enemy, out _) == true;
                return arbalistRetreat || trackerFollowingTrace || followingDecoy
                    ? command : CombatCommand.EndTurn(enemy.Id);
            }
            if (command.Type == CombatCommandType.UseSkill && command.SlotIndex == 0 &&
                (enemy.EnemyArchetypeId == "elite_vanguard" || enemy.EnemyArchetypeId == "breach_ram" ||
                    enemy.EnemyArchetypeId == "signal_keeper" || enemy.EnemyArchetypeId == "wind_librarian" ||
                    enemy.EnemyArchetypeId == "legacy_storekeeper" || enemy.EnemyArchetypeId == "prototype_hand" ||
                    enemy.EnemyArchetypeId == "lantern_revealer"))
                return CombatCommand.EndTurn(enemy.Id);
            return command;
        }
    }
}
