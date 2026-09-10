using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public enum CombatMechanicTriggerWindow
    {
        Attack,
        Proximity,
        Enter,
        PassThrough,
        Interact,
        Damaged,
        Destroyed
    }

    public sealed class CombatMechanicHoverModel
    {
        public string Title { get; }
        public string Summary { get; }
        public IReadOnlyList<string> RuleLines { get; }
        public CombatMechanicHoverModel(string title, string summary, IEnumerable<string> ruleLines)
        {
            Title = title ?? string.Empty;
            Summary = summary ?? string.Empty;
            RuleLines = (ruleLines ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        }
    }

    /// <summary>Phase-A data boundary. Concrete A/B/C rules and placements remain content-owned placeholders.</summary>
    public sealed class CombatSpecialTerrainDefinition
    {
        public string DefinitionId { get; }
        public string MechanicSlotId { get; }
        public IReadOnlyList<GridPosition> Positions { get; }
        public IReadOnlyList<CombatMechanicTriggerWindow> TriggerWindows { get; }
        public CombatMechanicHoverModel Hover { get; }
        public bool AffectsBothSides { get; }

        public CombatSpecialTerrainDefinition(string definitionId, string mechanicSlotId,
            IEnumerable<GridPosition> positions, IEnumerable<CombatMechanicTriggerWindow> triggerWindows,
            CombatMechanicHoverModel hover, bool affectsBothSides = true)
        {
            if (string.IsNullOrWhiteSpace(definitionId) || string.IsNullOrWhiteSpace(mechanicSlotId))
                throw new ArgumentException("Special terrain identity is required.");
            DefinitionId = definitionId;
            MechanicSlotId = mechanicSlotId;
            Positions = (positions ?? Array.Empty<GridPosition>()).Distinct().ToArray();
            TriggerWindows = (triggerWindows ?? Array.Empty<CombatMechanicTriggerWindow>()).Distinct().ToArray();
            Hover = hover ?? throw new ArgumentNullException(nameof(hover));
            AffectsBothSides = affectsBothSides;
        }
    }

    public sealed class CombatMechanicTriggerContext
    {
        public CombatMechanicTriggerWindow Window { get; }
        public CombatCommand Command { get; }
        public string ActorUnitId { get; }
        public bool ActorIsHero { get; }
        public GridPosition Source { get; }
        public GridPosition Position { get; }
        public IReadOnlyList<GridPosition> MovementPath { get; }
        public CombatEffectExecution Execution { get; }

        public CombatMechanicTriggerContext(CombatMechanicTriggerWindow window, CombatCommand command,
            string actorUnitId, bool actorIsHero, GridPosition source, GridPosition position,
            IEnumerable<GridPosition> movementPath = null, CombatEffectExecution execution = null)
        {
            Window = window; Command = command; ActorUnitId = actorUnitId ?? string.Empty; ActorIsHero = actorIsHero;
            Source = source; Position = position; MovementPath = (movementPath ?? Array.Empty<GridPosition>()).ToArray();
            Execution = execution ?? CombatEffectExecution.Empty;
        }
    }

    public interface ICombatSpecialTerrainRule
    {
        string DefinitionId { get; }
        bool Matches(CombatSpecialTerrainDefinition definition, CombatMechanicTriggerContext context);
        CombatEffectExecution Resolve(CombatState state, CombatSpecialTerrainDefinition definition, CombatMechanicTriggerContext context);
    }

    public sealed class CombatSpecialTerrainRuntime
    {
        private readonly IReadOnlyList<CombatSpecialTerrainDefinition> definitions;
        private readonly IReadOnlyDictionary<string, ICombatSpecialTerrainRule> rules;
        public CombatSpecialTerrainRuntime(IEnumerable<CombatSpecialTerrainDefinition> definitions, IEnumerable<ICombatSpecialTerrainRule> rules)
        {
            this.definitions = (definitions ?? Array.Empty<CombatSpecialTerrainDefinition>()).ToArray();
            this.rules = (rules ?? Array.Empty<ICombatSpecialTerrainRule>()).ToDictionary(value => value.DefinitionId, StringComparer.Ordinal);
        }

        public IReadOnlyList<CombatMechanicHoverModel> HoverAt(GridPosition position) => definitions
            .Where(value => value.Positions.Contains(position)).Select(value => value.Hover).ToArray();

        public IReadOnlyList<CombatEffectExecution> Dispatch(CombatState state, CombatMechanicTriggerContext context)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (context == null) throw new ArgumentNullException(nameof(context));
            List<CombatEffectExecution> results = new List<CombatEffectExecution>();
            foreach (CombatSpecialTerrainDefinition definition in definitions.Where(value => value.TriggerWindows.Contains(context.Window)))
            {
                if (!definition.AffectsBothSides) throw new InvalidOperationException("First-run special terrain must use identical rules for both sides.");
                if (rules.TryGetValue(definition.DefinitionId, out ICombatSpecialTerrainRule rule) && rule.Matches(definition, context))
                    results.Add(rule.Resolve(state, definition, context) ?? CombatEffectExecution.Empty);
            }
            return results;
        }
    }

    public static class CombatMechanicTriggerContextFactory
    {
        public static IReadOnlyList<CombatMechanicTriggerContext> ForCommand(CombatCommand command, UnitState actor,
            GridPosition source, IReadOnlyList<GridPosition> movementPath, CombatEffectExecution execution)
        {
            if (actor == null) return Array.Empty<CombatMechanicTriggerContext>();
            List<CombatMechanicTriggerContext> contexts = new List<CombatMechanicTriggerContext>();
            if (command.Type == CombatCommandType.Attack)
                contexts.Add(New(CombatMechanicTriggerWindow.Attack, command, actor, source, actor.Position, movementPath, execution));
            if (command.Type == CombatCommandType.Interact)
                contexts.Add(New(CombatMechanicTriggerWindow.Interact, command, actor, source, command.Destination, movementPath, execution));
            if (command.Type == CombatCommandType.Move && movementPath != null && movementPath.Count > 1)
            {
                for (int index = 1; index < movementPath.Count; index++)
                {
                    GridPosition step = movementPath[index];
                    contexts.Add(New(CombatMechanicTriggerWindow.Proximity, command, actor, source, step, movementPath, execution));
                    contexts.Add(New(index == movementPath.Count - 1 ? CombatMechanicTriggerWindow.Enter : CombatMechanicTriggerWindow.PassThrough,
                        command, actor, source, step, movementPath, execution));
                }
            }
            if (execution != null && execution.Results.Any(value => value.Kind == CombatEffectKind.DamageHealth && value.AppliedAmount > 0))
                contexts.Add(New(CombatMechanicTriggerWindow.Damaged, command, actor, source, actor.Position, movementPath, execution));
            if (execution != null && execution.Results.Any(value => value.Kind == CombatEffectKind.DamageObject && value.AppliedAmount > 0))
                contexts.Add(New(CombatMechanicTriggerWindow.Destroyed, command, actor, source, command.Destination, movementPath, execution));
            return contexts;
        }

        private static CombatMechanicTriggerContext New(CombatMechanicTriggerWindow window, CombatCommand command,
            UnitState actor, GridPosition source, GridPosition position, IReadOnlyList<GridPosition> path, CombatEffectExecution execution) =>
            new CombatMechanicTriggerContext(window, command, actor.Id, actor.IsHero, source, position, path, execution);
    }
}
