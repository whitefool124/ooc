using System.Linq;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    public static class CombatUnitMotionPolicy
    {
        public static Vector2 PreparationDirection(UnitState actor, EnemyIntentPresentation intent)
        {
            if (actor == null || intent == null) return Vector2.zero;
            GridPosition target = intent.Route.Count > 1 ? intent.Route[1] :
                intent.AffectedCells.OrderBy(p => p.ManhattanDistance(actor.Position)).FirstOrDefault();
            if (intent.Route.Count < 2 && intent.AffectedCells.Count == 0) return Vector2.zero;
            Vector2 delta = new Vector2(target.X - actor.Position.X, actor.Position.Y - target.Y);
            return delta.sqrMagnitude > .001f ? delta.normalized : Vector2.zero;
        }

        public static Vector2 Preparation(CombatCommandType type, Vector2 direction, float progress)
        {
            float amount = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress) / .65f);
            if (type == CombatCommandType.Move) return Snap(direction * (2f * amount));
            if (type == CombatCommandType.Attack) return Snap(-direction * (3f * amount));
            if (type == CombatCommandType.UseSkill) return Snap(new Vector2(-direction.x, -3f) * amount);
            return Vector2.zero;
        }

        public static Vector2 Impact(bool health, Vector2 direction, float progress)
        {
            float t = Mathf.Clamp01(progress);
            // Contact, a short held pose, then recovery. Shield recoil stays shorter and lighter.
            float strength = health ? (t < .22f ? 1f : Mathf.Pow(1f - (t - .22f) / .78f, 2f)) :
                Mathf.Pow(1f - t, 3f);
            float recoil = health ? 6f : 2f;
            return Snap(direction * strength * recoil +
                new Vector2(health ? Mathf.Sin(t * Mathf.PI * 6f) * strength * 2f : 0f, 0f));
        }

        public static Color ImpactTint(bool health, float progress, float intensity)
        {
            float t = Mathf.Clamp01(progress);
            float flash = t < .20f ? 1f : Mathf.Clamp01((1f - t) / .80f);
            return Color.Lerp(Color.white, health ? new Color(1f, .36f, .28f) :
                new Color(.72f, .76f, .80f), flash * (health ? .78f : .85f) * intensity);
        }

        public static Vector2 Stop(Vector2 direction, float progress)
            => Snap(direction * Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI) * 2f);
        private static Vector2 Snap(Vector2 v) => new Vector2(Mathf.Round(v.x), Mathf.Round(v.y));
    }
}
