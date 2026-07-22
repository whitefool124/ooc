using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public enum CombatObjectiveType { Elimination, Destruction, Rescue, Capture, Extraction, Investigation }

    public abstract class CombatObjective
    {
        public string Id { get; }
        public CombatObjectiveType Type { get; }
        protected CombatObjective(string id, CombatObjectiveType type) { Id = string.IsNullOrEmpty(id) ? type.ToString() : id; Type = type; }
        public abstract bool IsComplete(CombatState state);
        public abstract CombatObjective Clone();
    }

    public sealed class EliminationObjective : CombatObjective
    {
        public EliminationObjective(string id = "elimination") : base(id, CombatObjectiveType.Elimination) { }
        public override bool IsComplete(CombatState state) => state.Units.Values.All(unit => unit.IsHero || !unit.IsAlive);
        public override CombatObjective Clone() => new EliminationObjective(Id);
    }

    public sealed class DestructionObjective : CombatObjective
    {
        public IReadOnlyList<GridPosition> Positions { get; }
        public DestructionObjective(IEnumerable<GridPosition> positions, string id = "destruction") : base(id, CombatObjectiveType.Destruction)
        { Positions = (positions ?? throw new ArgumentNullException(nameof(positions))).Distinct().ToArray(); if (Positions.Count == 0) throw new ArgumentException("At least one destruction position is required.", nameof(positions)); }
        public override bool IsComplete(CombatState state) => Positions.All(position => state.Map.GetTile(position).IsDestroyed);
        public override CombatObjective Clone() => new DestructionObjective(Positions, Id);
    }

    public sealed class RescueObjective : CombatObjective
    {
        public IReadOnlyList<string> UnitIds { get; }
        public RescueObjective(IEnumerable<string> unitIds, string id = "rescue") : base(id, CombatObjectiveType.Rescue)
        { UnitIds = (unitIds ?? throw new ArgumentNullException(nameof(unitIds))).Distinct(StringComparer.Ordinal).ToArray(); if (UnitIds.Count == 0) throw new ArgumentException("At least one rescue unit is required.", nameof(unitIds)); }
        public override bool IsComplete(CombatState state) => UnitIds.All(id => state.GetUnit(id)?.IsAlive == true);
        public override CombatObjective Clone() => new RescueObjective(UnitIds, Id);
    }

    public sealed class CaptureObjective : CombatObjective
    {
        public IReadOnlyList<GridPosition> Positions { get; }
        public string UnitId { get; }
        public CaptureObjective(GridPosition position, string unitId = null, string id = "capture") : base(id, CombatObjectiveType.Capture) { Positions = new[] { position }; UnitId = unitId; }
        public override bool IsComplete(CombatState state) => state.Units.Values.Any(unit => (UnitId == null || unit.Id == UnitId) && unit.IsHero && Positions.Contains(unit.Position));
        public override CombatObjective Clone() => new CaptureObjective(Positions[0], UnitId, Id);
    }

    public sealed class ExtractionObjective : CombatObjective
    {
        public IReadOnlyList<GridPosition> Positions { get; }
        public string UnitId { get; }
        public ExtractionObjective(GridPosition position, string unitId = null, string id = "extraction") : base(id, CombatObjectiveType.Extraction) { Positions = new[] { position }; UnitId = unitId; }
        public override bool IsComplete(CombatState state) => state.Units.Values.Any(unit => (UnitId == null || unit.Id == UnitId) && unit.IsHero && Positions.Contains(unit.Position));
        public override CombatObjective Clone() => new ExtractionObjective(Positions[0], UnitId, Id);
    }

    public sealed class InvestigationObjective : CombatObjective
    {
        public IReadOnlyList<GridPosition> Positions { get; }
        public InvestigationObjective(IEnumerable<GridPosition> positions, string id = "investigation") : base(id, CombatObjectiveType.Investigation) { Positions = (positions ?? throw new ArgumentNullException(nameof(positions))).Distinct().ToArray(); if (Positions.Count == 0) throw new ArgumentException("At least one investigation position is required.", nameof(positions)); }
        public override bool IsComplete(CombatState state) => Positions.All(state.IsInvestigated);
        public override CombatObjective Clone() => new InvestigationObjective(Positions, Id);
    }
}
