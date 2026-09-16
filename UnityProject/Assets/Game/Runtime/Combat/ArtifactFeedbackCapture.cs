using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    // Captures one resolved effect at a time; never infers individual changes from an aggregate Applied value.
    internal sealed class ArtifactFeedbackCapture
    {
        private readonly string sourceId;
        private readonly GridPosition sourcePosition;
        private readonly UnitState[] live;
        private readonly UnitState[] before;

        public ArtifactFeedbackCapture(UnitState source, params UnitState[] affected)
        {
            sourceId = source.Id; sourcePosition = source.Position;
            live = affected.Where(unit => unit != null).GroupBy(unit => unit.Id).Select(group => group.First()).ToArray();
            before = live.Select(unit => unit.Clone()).ToArray();
        }

        public List<CombatFeedbackEvent> Finish(ArtifactEffectKind kind)
        {
            var feedback = new List<CombatFeedbackEvent>();
            for (int i = 0; i < live.Length; i++)
            {
                UnitState old = before[i], unit = live[i];
                void Add(CombatFeedbackKind semantic, int amount, int duration = 0)
                { if (amount > 0) feedback.Add(new CombatFeedbackEvent(semantic, sourcePosition, unit.Position, amount, duration, sourceId, unit.Id)); }
                Add(CombatFeedbackKind.ShieldAbsorb,
                    kind == ArtifactEffectKind.Damage || kind == ArtifactEffectKind.BacklashIfTargetSurvives || kind == ArtifactEffectKind.ArmReaction ? old.Shield - unit.Shield : 0);
                Add(CombatFeedbackKind.ShieldConsumed, kind == ArtifactEffectKind.ConsumeShield ? old.Shield - unit.Shield : 0);
                Add(CombatFeedbackKind.ShieldRestore, unit.Shield - old.Shield);
                Add(CombatFeedbackKind.Damage, old.Health - unit.Health);
                Add(CombatFeedbackKind.Healing, unit.Health - old.Health);
                Add(CombatFeedbackKind.ManaRestore, unit.Mana - old.Mana);
                foreach (var status in unit.Statuses.OrderBy(entry => entry.Key))
                    if (status.Value > old.StatusDuration(status.Key)) Add(CombatFeedbackCatalog.ForStatus(status.Key), 1, status.Value);
                if (old.Statuses.Keys.Any(status => !unit.HasStatus(status))) Add(CombatFeedbackKind.StatusCleared, 1);
                if (old.IsAlive && !unit.IsAlive) Add(CombatFeedbackKind.UnitDefeated, 1);
                if (old.Position != unit.Position)
                    feedback.Add(new CombatFeedbackEvent(CombatFeedbackKind.Movement, old.Position, unit.Position,
                        sourceUnitId: unit.Id, targetUnitId: unit.Id));
            }
            return feedback;
        }

        public static CombatFeedbackEvent Utility(UnitState source, UnitState target, GridPosition position, string message) =>
            new CombatFeedbackEvent(CombatFeedbackKind.UtilityResolved, source.Position, position,
                sourceUnitId: source.Id, targetUnitId: target?.Id, message: message);
    }
}
