using System;
using System.Linq;

namespace OCC.Combat.Presentation
{
    /// <summary>
    /// Owns the transient player action, inspected target and keyboard cursor as one state boundary.
    /// </summary>
    public sealed class CombatSelectionController
    {
        private readonly CombatTargetNavigationState navigation = new CombatTargetNavigationState();

        public string Action { get; private set; } = "移动";
        public string TargetId { get; private set; }
        public bool HasPreviewPosition { get; private set; }
        public GridPosition PreviewPosition { get; private set; }
        public bool IsKeyboardTargeting => navigation.Active;
        public GridPosition KeyboardPosition => navigation.Position;

        /// <summary>
        /// Transient right-click menu preview: which action the menu is offering at which anchor cell.
        /// An empty action marks only the anchor, so opening the menu never repaints the board with the
        /// committed action's range.
        /// </summary>
        public bool HasMenuPreview { get; private set; }
        public string MenuPreviewAction { get; private set; } = string.Empty;
        public GridPosition MenuPreviewPosition { get; private set; }

        public void SetMenuPreview(string action, GridPosition position)
        {
            HasMenuPreview = true;
            MenuPreviewAction = action ?? string.Empty;
            MenuPreviewPosition = position;
        }

        public void ClearMenuPreview()
        {
            HasMenuPreview = false;
            MenuPreviewAction = string.Empty;
        }

        public void SelectAction(string action)
        {
            navigation.End();
            Action = string.IsNullOrEmpty(action) ? "移动" : action;
            TargetId = null;
            HasPreviewPosition = false;
            ClearMenuPreview();
        }

        public void Reset(string action = "移动")
        {
            navigation.End();
            Action = action;
            TargetId = null;
            HasPreviewPosition = false;
            ClearMenuPreview();
        }

        public bool SetTarget(CombatState state, string unitId) =>
            SetKnownTarget(state != null && state.GetUnit(unitId) != null ? unitId : null);

        public bool SetKnownTarget(string unitId)
        {
            if (string.Equals(TargetId, unitId, StringComparison.Ordinal)) return false;
            TargetId = unitId;
            return true;
        }

        public bool ClearTarget() => SetKnownTarget(null);

        public void SetPreviewPosition(GridPosition position)
        {
            PreviewPosition = position;
            HasPreviewPosition = true;
        }

        public void ClearPreviewPosition() => HasPreviewPosition = false;

        public bool BeginKeyboardTargeting(CombatState state)
        {
            UnitState hero = state?.GetUnit("hero");
            if (hero == null || !hero.IsAlive || state.ActiveUnitId != hero.Id) return false;
            UnitState selected = string.IsNullOrEmpty(TargetId) ? null : state.GetUnit(TargetId);
            navigation.Begin(selected?.Position ?? hero.Position, state.Map.Width, state.Map.Height);
            SetPreviewPosition(navigation.Position);
            return true;
        }

        public bool MoveKeyboardTarget(CombatState state, int deltaX, int deltaY)
        {
            if (state == null || !navigation.Active) return false;
            navigation.Move(deltaX, deltaY, state.Map.Width, state.Map.Height);
            UnitState unit = state.Units.Values.FirstOrDefault(candidate =>
                candidate.IsAlive && candidate.Position == navigation.Position);
            TargetId = unit != null && !unit.IsHero ? unit.Id : null;
            SetPreviewPosition(navigation.Position);
            return true;
        }

        public bool TryCommitKeyboardTarget(out GridPosition position)
        {
            position = navigation.Position;
            if (!navigation.Active) return false;
            navigation.End();
            ClearPreviewPosition();
            return true;
        }

        public bool CancelKeyboardTargeting()
        {
            if (!navigation.Active) return false;
            navigation.End();
            TargetId = null;
            ClearPreviewPosition();
            return true;
        }

        public void EndKeyboardTargeting() { navigation.End(); ClearPreviewPosition(); }
    }
}
