namespace OCC.Combat
{
    public enum CombatOutcomePersistence
    {
        None,
        MapRun,
        ShortRun,
        Story
    }

    public readonly struct CombatOutcomeSettlement
    {
        public bool HandledNow { get; }
        public bool Victory { get; }
        public CombatOutcomePersistence Persistence { get; }
        public bool RefreshSettlement { get; }

        public CombatOutcomeSettlement(bool handledNow, bool victory,
            CombatOutcomePersistence persistence = CombatOutcomePersistence.None,
            bool refreshSettlement = false)
        {
            HandledNow = handledNow;
            Victory = victory;
            Persistence = persistence;
            RefreshSettlement = refreshSettlement;
        }
    }

    /// <summary>
    /// Makes combat settlement idempotent while adapting both supported roguelite run shapes.
    /// Presentation and persistence remain caller-owned ports.
    /// </summary>
    public sealed class CombatOutcomeSettlementCoordinator
    {
        public bool IsHandled { get; private set; }

        public CombatOutcomeSettlement Process(CombatFlowPhase phase, CombatState combat,
            RogueliteMapRun mapRun, RogueliteDeveloperRun legacyRun)
        {
            if (IsHandled) return default;

            if (phase == CombatFlowPhase.Defeat)
            {
                IsHandled = true;
                return new CombatOutcomeSettlement(true, false);
            }

            if (phase != CombatFlowPhase.Victory) return default;
            if (mapRun != null)
            {
                IsHandled = true;
                RogueliteCombatSettlement.TrySettleVictory(mapRun, combat);
                return new CombatOutcomeSettlement(true, true, CombatOutcomePersistence.MapRun, true);
            }

            if (legacyRun == null) return default;
            IsHandled = true;
            if (legacyRun.Kind == RogueliteLaunchKind.TemplateSandbox)
                return new CombatOutcomeSettlement(true, true);

            string summary = "胜利 | " + legacyRun.CurrentMission.TemplateId + " | 种子 " + legacyRun.Package.Seed;
            legacyRun.Complete(summary);
            return new CombatOutcomeSettlement(true, true,
                legacyRun.IsShortRun ? CombatOutcomePersistence.ShortRun : CombatOutcomePersistence.Story);
        }

        public void Reset() => IsHandled = false;
    }
}
