using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public static class FireSpellRewardPool
    {
        public static IReadOnlyList<FireSpellDefinition> RollPersonalChoices(int seed, int completedCombatCount,
            RogueliteMapNodeType nodeType, IEnumerable<string> ownedIds)
            => RollPersonalChoices(seed, completedCombatCount, nodeType, ownedIds, null, false);

        public static IReadOnlyList<FireSpellDefinition> RollPersonalChoices(int seed, int completedCombatCount,
            RogueliteMapNodeType nodeType, IEnumerable<string> ownedIds, WeaponDefinition equippedWeapon)
            => RollPersonalChoices(seed, completedCombatCount, nodeType, ownedIds, equippedWeapon, true);

        private static IReadOnlyList<FireSpellDefinition> RollPersonalChoices(int seed, int completedCombatCount,
            RogueliteMapNodeType nodeType, IEnumerable<string> ownedIds, WeaponDefinition equippedWeapon, bool filterWeapon)
        {
            HashSet<string> owned = new HashSet<string>(ownedIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            FireSpellDefinition[] legal = FireSpellCatalog.All.Where(spell => !owned.Contains(spell.Id) &&
                (!filterWeapon || FireSpellCatalog.IsWeaponCompatible(spell, equippedWeapon))).ToArray();
            Random random = new Random(unchecked(seed * 486187739 + completedCombatCount * 7919 + (int)nodeType * 101));
            FireSpellRarity[] slots = SlotsFor(nodeType, random);
            var result = new List<FireSpellDefinition>(slots.Length);
            foreach (FireSpellRarity rarity in slots)
            {
                FireSpellDefinition[] candidates = legal.Where(spell => spell.Rarity == rarity && !result.Any(chosen => chosen.Id == spell.Id)).ToArray();
                if (candidates.Length == 0) candidates = legal.Where(spell => !result.Any(chosen => chosen.Id == spell.Id)).ToArray();
                FireSpellDefinition chosen = candidates.OrderBy(_ => random.Next()).ThenBy(spell => spell.Id, StringComparer.Ordinal).FirstOrDefault();
                if (chosen != null) result.Add(chosen);
            }
            return result;
        }

        private static FireSpellRarity[] SlotsFor(RogueliteMapNodeType nodeType, Random random)
        {
            if (nodeType == RogueliteMapNodeType.Finale) return new[] { FireSpellRarity.Rare };
            if (nodeType == RogueliteMapNodeType.Elite) return new[] { random.Next(100) < 35 ? FireSpellRarity.Rare : FireSpellRarity.Uncommon };
            if (nodeType == RogueliteMapNodeType.Treasure) return new[] { random.Next(100) < 60 ? FireSpellRarity.Rare : FireSpellRarity.Uncommon };
            return new[] { FireSpellRarity.Common, random.Next(100) < 35 ? FireSpellRarity.Uncommon : FireSpellRarity.Common };
        }
    }
}
