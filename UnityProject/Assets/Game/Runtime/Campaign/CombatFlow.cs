using System;

namespace OCC.Combat
{
    public enum CombatFlowPhase { DeveloperMenu, Briefing, Active, Victory, Defeat, TacticalRestart }

    public sealed class CombatFlowController
    {
        public CombatFlowPhase Phase { get; private set; } = CombatFlowPhase.DeveloperMenu;
        public MissionPreparation Preparation { get; private set; }
        public CombatState State { get; private set; }
        public CombatState RestartSnapshot { get; private set; }

        public void Configure(MissionPreparation preparation, CombatState state)
        {
            Preparation = preparation ?? throw new ArgumentNullException(nameof(preparation));
            State = state ?? throw new ArgumentNullException(nameof(state));
            RestartSnapshot = state.Clone(); Phase = CombatFlowPhase.DeveloperMenu;
        }
        public void OpenBriefing() { Require(CombatFlowPhase.DeveloperMenu); Phase = CombatFlowPhase.Briefing; }
        public void BeginCombat() { Require(CombatFlowPhase.Briefing); Phase = CombatFlowPhase.Active; }
        public void RefreshOutcome()
        {
            if (State == null) throw new InvalidOperationException("Combat flow is not configured.");
            if (Phase != CombatFlowPhase.Active) return;
            if (State.IsVictory) Phase = CombatFlowPhase.Victory; else if (State.IsDefeat) Phase = CombatFlowPhase.Defeat;
        }
        public void TacticalRestart()
        {
            if (Phase != CombatFlowPhase.Victory && Phase != CombatFlowPhase.Defeat && Phase != CombatFlowPhase.Active) throw new InvalidOperationException("Tactical restart is only available during or after combat.");
            State = RestartSnapshot.Clone(); Phase = CombatFlowPhase.TacticalRestart;
        }
        public void ResumeAfterRestart() { Require(CombatFlowPhase.TacticalRestart); Phase = CombatFlowPhase.Active; }
        public void ReturnToDeveloperMenu()
        {
            State = RestartSnapshot.Clone();
            Phase = CombatFlowPhase.DeveloperMenu;
        }
        private void Require(CombatFlowPhase phase) { if (Phase != phase) throw new InvalidOperationException($"Expected phase {phase}, current phase is {Phase}."); }
    }

    public static class CombatSceneConfigurationValidator
    {
        public static void Validate(int heroCount, int enemyCount, int objectiveCount, int lightCoverCount, int heavyCoverCount)
        {
            if (heroCount != 1) throw new InvalidOperationException("Scene requires exactly one hero marker.");
            if (enemyCount < 1) throw new InvalidOperationException("Scene requires at least one enemy marker.");
            if (objectiveCount < 1) throw new InvalidOperationException("Scene requires at least one objective marker.");
            if (lightCoverCount < 1 || heavyCoverCount < 1) throw new InvalidOperationException("Scene requires both light and heavy cover.");
        }
    }
}
