using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // Routes presentation only. Shield resources deliberately have no body effect.
    public static class CombatPixelVfxPolicy
    {
        public static bool IsShieldResourceEffect(string effect) => effect == "shield_absorb" || effect == "shield_restore" || effect == "shield_break";
        public static bool IsTechnical(string effect) => effect == "ether_utility" || effect == "ether_attribute" || effect == "ether_projectile" || effect == "metal_projectile";
        public static bool IsShieldResource(CombatFeedbackKind kind) =>
            kind == CombatFeedbackKind.ShieldAbsorb || kind == CombatFeedbackKind.ShieldRestore ||
            kind == CombatFeedbackKind.ShieldConsumed || kind == CombatFeedbackKind.ShieldTransferredIn ||
            kind == CombatFeedbackKind.ShieldTransferredOut;

        public static string Feedback(CombatFeedbackKind kind)
        {
            if (IsShieldResource(kind)) return null;
            switch (kind)
            {
                case CombatFeedbackKind.Damage: return "hit";
                case CombatFeedbackKind.ArmorBreak: case CombatFeedbackKind.BreakStance: return "armor_break";
                case CombatFeedbackKind.Burning: return "burning";
                case CombatFeedbackKind.Bound: return "bound";
                case CombatFeedbackKind.Slow: return "slow";
                case CombatFeedbackKind.Healing: return "health_repair";
                case CombatFeedbackKind.ManaRestore: return "mana_restore";
                case CombatFeedbackKind.StatusCleared: return "cleanse";
                case CombatFeedbackKind.Movement: return "path";
                case CombatFeedbackKind.DestructibleDamaged: return "object_damage";
                case CombatFeedbackKind.DestructibleDestroyed: return "object_break";
                case CombatFeedbackKind.UnitDefeated: return "heavy_hit";
                case CombatFeedbackKind.Attribute: return "ether_attribute";
                case CombatFeedbackKind.UtilityResolved: return "ether_utility";
                default: throw new System.ArgumentOutOfRangeException(nameof(kind));
            }
        }

        public static string Delivery(SkillDefinition skill)
        {
            if (skill == null || skill.Effects.All(e => e.Type == SkillEffectType.RestoreShield)) return null;
            if (skill.Effects.Any(e => e.Type == SkillEffectType.Damage && e.DamageType == DamageType.Fire))
                return skill.Id == "cinder_sweep" ? "fire_spray" : skill.Id == "searing_mark" ? "fire_detonate" :
                    skill.Delivery == SkillDeliveryMethod.Area ? "fire_cross_blast" : "fire_projectile";
            // Only delivery travels; status, healing and damage remain bound to resolved feedback.
            if (skill.Delivery == SkillDeliveryMethod.Projectile)
                return skill.Effects.Any(e => e.Type == SkillEffectType.Damage && e.DamageType == DamageType.Physical) ? "metal_projectile" : "ether_projectile";
            return null;
        }

        public static bool Travels(SkillDefinition skill) => skill != null && skill.Delivery == SkillDeliveryMethod.Projectile;

        public static Color Tint(string effect)
        {
            if (effect.StartsWith("fire_") || effect == "burning") return new Color(1f, .32f, .045f);
            if (effect == "health_repair" || effect == "cleanse") return new Color(.28f, 1f, .55f);
            if (effect.StartsWith("ether_") || effect == "bound" || effect == "slow" || effect == "mana_restore" || effect == "path") return new Color(.2f, .65f, 1f);
            return new Color(1f, .7f, .24f);
        }
    }

    // One owned material per bounded playback slot, reused across its stages.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CombatPixelVfxGraphic : MaskableGraphic
    {
        private Material owned;
        private float progress, family, intensity;
        public float NormalizedAge => progress;
        public void Sample(string effect, float age, float strength, float nativePixels)
        {
            if (owned == null)
            {
                Shader shader = Resources.Load<Shader>("Shaders/CombatPixelLight");
                if (shader == null) { enabled = false; return; }
                owned = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }
            material = owned;
            color = CombatPixelVfxPolicy.Tint(effect);
            progress = age; intensity = Mathf.Clamp01(strength);
            family = effect.StartsWith("fire_") || effect == "burning" ? 1f :
                effect == "health_repair" || effect == "cleanse" || effect == "mana_restore" || effect == "ether_utility" || effect == "ether_attribute" ? 2f :
                effect == "path" || effect.EndsWith("_projectile") ? 3f : 0f;
            pixels = nativePixels; raycastTarget = false; SetVerticesDirty();
            owned.SetColor("_Tint", color);
            owned.SetFloat("_Progress", progress); owned.SetFloat("_Family", family);
            owned.SetFloat("_Intensity", intensity); owned.SetFloat("_NativePixels", pixels);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (owned != null) { if (Application.isPlaying) Destroy(owned); else DestroyImmediate(owned); }
        }
        private float pixels = 96f;
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear(); Rect r = rectTransform.rect;
            for (int i = 0; i < 4; i++)
            {
                Vector2 uv = new Vector2(i == 1 || i == 2 ? 1f : 0f, i >= 2 ? 1f : 0f);
                UIVertex v = UIVertex.simpleVert; v.color = color;
                v.position = new Vector3(Mathf.Lerp(r.xMin, r.xMax, uv.x), Mathf.Lerp(r.yMin, r.yMax, uv.y));
                v.uv0 = new Vector4(uv.x, uv.y, 0f, 0f); helper.AddVert(v);
            }
            helper.AddTriangle(0, 1, 2); helper.AddTriangle(2, 3, 0);
        }
    }
}
