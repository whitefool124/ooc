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
            bool rareSlot = nodeType == RogueliteMapNodeType.Elite || nodeType == RogueliteMapNodeType.Treasure || nodeType == RogueliteMapNodeType.Finale;
            FireSpellDefinition[] legal = FireSpellCatalog.All.Where(spell => !owned.Contains(spell.Id) &&
                (rareSlot ? spell.Rarity == FireSpellRarity.Rare : spell.Rarity != FireSpellRarity.Rare) &&
                (!filterWeapon || FireSpellCatalog.IsWeaponCompatible(spell, equippedWeapon))).ToArray();
            int count = rareSlot ? 1 : 2;
            Random random = new Random(unchecked(seed * 486187739 + completedCombatCount * 7919 + (int)nodeType * 101));
            return legal.OrderBy(_ => random.Next()).ThenBy(spell => spell.Id, StringComparer.Ordinal).Take(count).ToArray();
        }
    }
}
