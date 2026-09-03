using System;
using System.Linq;

namespace OCC.Combat.Roguelite
{
    public static class RogueDamageResolver
    {
        public static DamageResolution Resolve(DamagePacket packet, int currentShield, int currentHealth)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            if (currentShield < 0 || currentHealth < 0) throw new ArgumentOutOfRangeException(nameof(currentShield));
            int raw = packet.Components.Sum(value => value.RawAmount);
            PercentageReductionEffect[] applicable = packet.ReductionEffects.Where(value =>
                value.AppliesToTags.Count == 0 || value.AppliesToTags.Any(packet.Tags.Contains)).ToArray();
            double remaining = 1d;
            foreach (IGrouping<ReductionCategory, PercentageReductionEffect> category in applicable.GroupBy(value => value.Category))
                remaining *= 1d - category.Max(value => value.Percent) / 100d;
            int reductionRate = Math.Min(RogueRuntimeConstants.MaximumPercentageReduction,
                Math.Max(0, (int)Math.Round((1d - remaining) * 100d, MidpointRounding.AwayFromZero)));
            double cappedRemaining = 1d - reductionRate / 100d;
            int afterReduction = raw == 0 ? 0 : (int)Math.Ceiling(raw * cappedRemaining);
            int shieldAbsorbed = Math.Min(currentShield, afterReduction);
            int healthDamage = Math.Min(currentHealth, afterReduction - shieldAbsorbed);
            return new DamageResolution(raw, reductionRate, raw - afterReduction, afterReduction, currentShield,
                shieldAbsorbed, currentHealth, healthDamage, currentHealth - healthDamage <= 0);
        }
    }
}
