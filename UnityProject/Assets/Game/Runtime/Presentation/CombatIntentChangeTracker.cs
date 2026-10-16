using System.Collections.Generic;
using System.Text;

namespace OCC.Combat.Presentation
{
    /// <summary>Tracks changes to published plans, never requests or changes an AI command.</summary>
    public sealed class CombatIntentChangeTracker
    {
        private readonly Dictionary<string, string> signatures = new Dictionary<string, string>();
        private readonly Dictionary<string, float> changedAt = new Dictionary<string, float>();
        public const float Duration = .85f;

        public bool Observe(string unitId, EnemyIntentPresentation intent, float now)
        {
            if (intent == null || string.IsNullOrEmpty(unitId)) return false;
            string next = Fingerprint(intent);
            bool changed = signatures.TryGetValue(unitId, out string previous) && previous != next;
            signatures[unitId] = next;
            if (changed) changedAt[unitId] = now;
            return changed;
        }

        public float Age(string unitId, float now) => unitId != null && changedAt.TryGetValue(unitId, out float at)
            ? now - at : Duration;
        public void Clear() { signatures.Clear(); changedAt.Clear(); }

        private static string Fingerprint(EnemyIntentPresentation intent)
        {
            var value = new StringBuilder(intent.Signature).Append('|').Append(intent.IconId)
                .Append('|').Append(intent.ActionName).Append('|').Append(intent.ResultSummary)
                .Append('|').Append(intent.TargetSummary).Append('|').Append(intent.ExpectedDamage)
                .Append('|').Append(intent.HasDestination).Append('|').Append(intent.Destination);
            foreach (var p in intent.Route) value.Append("r").Append(p.X).Append(',').Append(p.Y);
            foreach (var p in intent.AffectedCells) value.Append("a").Append(p.X).Append(',').Append(p.Y);
            foreach (var p in intent.AttackRange) value.Append("e").Append(p.X).Append(',').Append(p.Y);
            return value.ToString();
        }
    }
}
