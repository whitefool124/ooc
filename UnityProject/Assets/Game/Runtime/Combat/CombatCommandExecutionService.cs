using System;
using System.Collections.Generic;

namespace OCC.Combat
{
    public sealed class CombatCommandExecutionResult
    {
        public bool Accepted { get; }
        public string RejectionReason { get; }
        public CombatEffectExecution Execution { get; }
        public IReadOnlyList<FireSpellExecution> MovementFireExecutions { get; }
        public IReadOnlyList<FireSpellExecution> AttackFireExecutions { get; }
        public SkillDefinition DeliveredSkill { get; }
        public GridPosition DeliverySource { get; }
        public GridPosition DeliveryTarget { get; }
        public bool HeroMoved { get; }
        public string ActionResult { get; }
        public FireBattleState FireBattle { get; }
        public IReadOnlyList<GridPosition> MovementPath { get; }
        public IReadOnlyList<CombatMechanicTriggerContext> MechanicTriggerContexts { get; }

        private CombatCommandExecutionResult(bool accepted, string rejectionReason,
            CombatEffectExecution execution, IReadOnlyList<FireSpellExecution> movementFireExecutions,
            IReadOnlyList<FireSpellExecution> attackFireExecutions, SkillDefinition deliveredSkill,
            GridPosition deliverySource, GridPosition deliveryTarget, bool heroMoved,
            string actionResult, FireBattleState fireBattle, IReadOnlyList<GridPosition> movementPath,
            IReadOnlyList<CombatMechanicTriggerContext> mechanicTriggerContexts)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            Execution = execution;
            MovementFireExecutions = movementFireExecutions ?? Array.Empty<FireSpellExecution>();
            AttackFireExecutions = attackFireExecutions ?? Array.Empty<FireSpellExecution>();
            DeliveredSkill = deliveredSkill;
            DeliverySource = deliverySource;
            DeliveryTarget = deliveryTarget;
            HeroMoved = heroMoved;
            ActionResult = actionResult;
            FireBattle = fireBattle;
            MovementPath = movementPath ?? Array.Empty<GridPosition>();
            MechanicTriggerContexts = mechanicTriggerContexts ?? Array.Empty<CombatMechanicTriggerContext>();
        }

        public static CombatCommandExecutionResult Rejected(string reason, FireBattleState fireBattle) =>
            new CombatCommandExecutionResult(false, reason, null, null, null, null,
                default, default, false, string.Empty, fireBattle, null, null);

        public static CombatCommandExecutionResult Succeeded(CombatEffectExecution execution,
            IReadOnlyList<FireSpellExecution> movementFireExecutions,
            IReadOnlyList<FireSpellExecution> attackFireExecutions, SkillDefinition deliveredSkill,
            GridPosition deliverySource, GridPosition deliveryTarget, bool heroMoved,
            string actionResult, FireBattleState fireBattle, IReadOnlyList<GridPosition> movementPath = null,
            IReadOnlyList<CombatMechanicTriggerContext> mechanicTriggerContexts = null) =>
            new CombatCommandExecutionResult(true, string.Empty, execution, movementFireExecutions,
                attackFireExecutions, deliveredSkill, deliverySource, deliveryTarget, heroMoved,
                actionResult, fireBattle, movementPath, mechanicTriggerContexts);
    }

    /// <summary>
    /// Executes one authoritative combat command and reports the presentation context created by it.
    /// It owns no Unity lifecycle or UI behavior.
    /// </summary>
    public sealed class CombatCommandExecutionService
    {
        public const string ExplicitHeroEndTurnReason = "请点击“结束回合”继续战斗。";

        public static bool CanSubmit(CombatCommand command, bool explicitHeroEndTurn) =>
            command.Type != CombatCommandType.EndTurn || command.UnitId != "hero" || explicitHeroEndTurn;

        public CombatCommandExecutionResult Execute(CombatState state, FireBattleState fireBattle,
            CombatCommand command, bool explicitHeroEndTurn = false)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (!CanSubmit(command, explicitHeroEndTurn))
                return CombatCommandExecutionResult.Rejected(ExplicitHeroEndTurnReason, fireBattle);

            try
            {
                if (state.Ruleset == CombatRuleset.Roguelite && state.RogueSpells != null) fireBattle = state.RogueSpells.FireBattle;
                UnitState commandUnit = state.GetUnit(command.UnitId);
                SkillDefinition commandSkill = command.Type == CombatCommandType.UseSkill && commandUnit != null
                    ? (command.SlotIndex == 0 ? commandUnit.SkillOne : commandUnit.SkillTwo) : null;
                SkillDefinition deliveredSkill = state.Ruleset != CombatRuleset.Roguelite ? commandSkill : null;
                GridPosition deliverySource = commandUnit?.Position ?? command.Destination;
                GridPosition movementSource = deliverySource;
                IReadOnlyList<GridPosition> movementPath = command.Type == CombatCommandType.Move && commandUnit != null
                    ? CombatMovementQuery.FindPath(state, commandUnit, command.Destination)
                    : Array.Empty<GridPosition>();
                UnitState commandTarget = string.IsNullOrWhiteSpace(command.TargetUnitId)
                    ? null : state.GetUnit(command.TargetUnitId);
                GridPosition deliveryTarget = commandTarget?.Position ??
                    (deliveredSkill != null && (deliveredSkill.TargetRule == SkillTargetRule.GridCell ||
                        deliveredSkill.TargetRule == SkillTargetRule.Destructible)
                        ? command.Destination : deliverySource);

                CombatEffectExecution execution;
                IReadOnlyList<FireSpellExecution> attackTriggers = Array.Empty<FireSpellExecution>();
                if (command.Type == CombatCommandType.Attack)
                {
                    if (fireBattle == null || fireBattle.Combat != state) fireBattle = new FireBattleState(state);
                    FireWeaponAttackResolution attack = FireSpellEngine.ResolveWeaponAttack(fireBattle,
                        command.UnitId, command.TargetUnitId);
                    execution = attack.WeaponExecution;
                    attackTriggers = attack.TriggerExecutions;
                }
                else execution = CombatResolver.Resolve(state, command);
                if (command.Type == CombatCommandType.UseSkill && commandSkill != null && commandSkill.Damage > 0 &&
                    commandTarget != null && deliverySource.ManhattanDistance(deliveryTarget) == 1 && fireBattle != null)
                    attackTriggers = FireSpellEngine.TriggerIncomingAdjacentAttack(fireBattle, commandUnit.Id, commandTarget.Id);

                IReadOnlyList<FireSpellExecution> movementTriggers = Array.Empty<FireSpellExecution>();
                if (command.Type == CombatCommandType.Move && commandUnit != null && fireBattle != null)
                {
                    fireBattle.ResolveEntry(commandUnit, movementSource);
                    List<FireSpellExecution> combined = new List<FireSpellExecution>();
                    combined.AddRange(FireSpellEngine.TriggerMarkedTargetMove(fireBattle, commandUnit.Id, movementSource));
                    combined.AddRange(FireSpellEngine.TriggerEnemyEntry(fireBattle, commandUnit.Id));
                    movementTriggers = combined;
                }
                fireBattle?.ResolveMarkedDestructions(command.Type == CombatCommandType.Attack ||
                    command.Type == CombatCommandType.UseSkill ? command.UnitId : null);

                IReadOnlyList<CombatMechanicTriggerContext> mechanicContexts = CombatMechanicTriggerContextFactory.ForCommand(
                    command, commandUnit, movementSource, movementPath, execution);
                return CombatCommandExecutionResult.Succeeded(execution, movementTriggers, attackTriggers,
                    deliveredSkill, deliverySource, deliveryTarget,
                    command.Type == CombatCommandType.Move && command.UnitId == "hero",
                    CombatInformationPresenter.BuildActionResult(state, command, execution), fireBattle, movementPath, mechanicContexts);
            }
            catch (InvalidOperationException error)
            {
                return CombatCommandExecutionResult.Rejected(error.Message, fireBattle);
            }
        }
    }
}
