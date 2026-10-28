using UnityEngine;

namespace OCC.Combat.Presentation
{
    public enum CombatParticleFamily { Impact, HeavyImpact, Cast, Projectile, Arc, Spray, Line, Blast, Fireground, Burning, Recovery, Cleanse, Bind, Slow, Break, Debris, Movement, Attribute }

    // Presentation recipes only. No combat state, random generator or storage dependency.
    public readonly struct CombatParticleRecipe
    {
        public readonly CombatParticleFamily Family;
        public readonly Color Color;
        public readonly int Count;
        public readonly float PixelMetres, Radius, Speed, Light;
        public CombatParticleRecipe(CombatParticleFamily family, Color color, int count, float radius, float speed, float light = 0f)
        { Family = family; Color = color; Count = count; Radius = radius; Speed = speed; Light = light; PixelMetres = 1f / 32f; }

        public static bool TryGet(string effect, out CombatParticleRecipe recipe)
        {
            recipe = default;
            if (string.IsNullOrEmpty(effect) || CombatPixelVfxPolicy.IsShieldResourceEffect(effect)) return false;
            CombatParticleFamily family;
            switch (effect)
            {
                case "hit": family = CombatParticleFamily.Impact; break;
                case "heavy_hit": family = CombatParticleFamily.HeavyImpact; break;
                case "fire_cast": family = CombatParticleFamily.Cast; break;
                case "fire_projectile": case "ether_projectile": case "metal_projectile": family = CombatParticleFamily.Projectile; break;
                case "fire_melee_arc": family = CombatParticleFamily.Arc; break;
                case "fire_spray": family = CombatParticleFamily.Spray; break;
                case "fire_line": family = CombatParticleFamily.Line; break;
                case "fire_impact": case "fire_cross_blast": case "fire_detonate": case "fire_overlimit": family = CombatParticleFamily.Blast; break;
                case "fire_burning_ground": case "fire_wall": family = CombatParticleFamily.Fireground; break;
                case "burning": family = CombatParticleFamily.Burning; break;
                case "health_repair": case "mana_restore": case "fire_absorb": family = CombatParticleFamily.Recovery; break;
                case "cleanse": family = CombatParticleFamily.Cleanse; break;
                case "bound": family = CombatParticleFamily.Bind; break;
                case "slow": family = CombatParticleFamily.Slow; break;
                case "armor_break": case "fire_break_stance": family = CombatParticleFamily.Break; break;
                case "object_damage": case "object_break": family = CombatParticleFamily.Debris; break;
                case "path": family = CombatParticleFamily.Movement; break;
                case "fire_attachment": case "ether_attribute": case "ether_utility": family = CombatParticleFamily.Attribute; break;
                default: return false;
            }
            bool blast = family == CombatParticleFamily.Blast || family == CombatParticleFamily.HeavyImpact;
            bool debris = family == CombatParticleFamily.Debris || family == CombatParticleFamily.Break;
            Color color = CombatPixelVfxPolicy.Tint(effect);
            if (effect == "bound") color = new Color(.32f, .75f, 1f);
            if (effect == "slow") color = new Color(.54f, .65f, .63f);
            if (debris) color = new Color(.68f, .48f, .27f);
            recipe = new CombatParticleRecipe(family, color, blast ? 72 : debris ? 40 : 28,
                blast ? .22f : family == CombatParticleFamily.Impact ? .18f : .12f,
                blast ? 2.4f : debris ? 1.6f : family == CombatParticleFamily.Impact ? 2f : .9f,
                family == CombatParticleFamily.Blast ? 1.3f : family == CombatParticleFamily.Cast ? .5f : 0f);
            return true;
        }
    }
}
