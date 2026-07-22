namespace OCC.Combat
{
    public static class StageTwoBuilds
    {
        public static readonly WeaponDefinition ArcaneWand = new WeaponDefinition("arcane_wand", "\u4ee5\u592a\u805a\u7126\u624b\u6756", DamageType.Arcane, 3, 3, manaCost: 1);
        public static readonly WeaponDefinition CalibratedRifle = new WeaponDefinition("calibrated_rifle", "\u6821\u51c6\u6b65\u67aa", DamageType.Physical, 5, 4);

        public static void Apply(UnitState hero, int build)
        {
            if (build == 0) hero.Equip(CombatCatalog.Rifle, CombatCatalog.Shield, CombatCatalog.FireBolt, CombatCatalog.FrostBind);
            else if (build == 1) hero.Equip(CombatCatalog.Hammer, CombatCatalog.Shield, CombatCatalog.FrostBind, CombatCatalog.FireBolt);
            else hero.Equip(ArcaneWand, CombatCatalog.Shield, CombatCatalog.FireBolt, CombatCatalog.FrostBind);
        }
    }
}
