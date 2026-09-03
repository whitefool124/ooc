using System.Linq;
using NUnit.Framework;
using OCC.Combat.Roguelite;

namespace OCC.Combat.Tests
{
    public sealed class RogueDamageAndTurnLifecycleTests
    {
        [Test]
        public void M1Resolver_MergesComponentsUsesHighestPerCategoryMultipliesCategoriesAndRoundsUp()
        {
            DamagePacket packet = new DamagePacket("p1", "hero", "enemy", "composite_hit",
                new[] { new DamageComponent(DamageComponentKind.Physical, 7), new DamageComponent(DamageComponentKind.Fire, 4) },
                new[] { DamageTag.Melee },
                new[]
                {
                    new PercentageReductionEffect("stance-low", ReductionCategory.Stance, 10),
                    new PercentageReductionEffect("stance-high", ReductionCategory.Stance, 20),
                    new PercentageReductionEffect("reaction", ReductionCategory.Reaction, 25)
                });

            DamageResolution result = RogueDamageResolver.Resolve(packet, 5, 18);

            Assert.That(result.RawTotal, Is.EqualTo(11));
            Assert.That(result.ReductionRate, Is.EqualTo(40));
            Assert.That(result.AfterReduction, Is.EqualTo(7));
            Assert.That(result.ShieldAbsorbed, Is.EqualTo(5));
            Assert.That(result.HealthDamage, Is.EqualTo(2));
        }

        [Test]
        public void M1Resolver_CapsReductionAtFiftyAndHasNoMinimumDamageArmorBlockOrPierce()
        {
            DamagePacket packet = new DamagePacket("p2", "hero", "enemy", "approved_exception",
                new[] { new DamageComponent(DamageComponentKind.Fire, 1) }, reductionEffects: new[]
                {
                    new PercentageReductionEffect("stance", ReductionCategory.Stance, 80),
                    new PercentageReductionEffect("reaction", ReductionCategory.Reaction, 80)
                });
            DamageResolution result = RogueDamageResolver.Resolve(packet, 0, 1);
            Assert.That(result.ReductionRate, Is.EqualTo(50));
            Assert.That(result.AfterReduction, Is.EqualTo(1));
            Assert.That(result.HealthDamage, Is.EqualTo(1));
            Assert.That(result.TargetDefeated, Is.True);
        }

        [Test]
        public void M1Combatant_MultiSegmentResolvesOnePacketAtATimeAndStopsAfterDeath()
        {
            RogueCombatantState target = new RogueCombatantState("enemy", 10, 3);
            DamagePacket first = Packet("s1", 5, 0, 2);
            DamagePacket second = Packet("s2", 9, 1, 2);

            DamageResolution a = target.Apply(first);
            DamageResolution b = target.Apply(second);

            Assert.That(a.ShieldAbsorbed, Is.EqualTo(3));
            Assert.That(target.CurrentHealth, Is.Zero);
            Assert.That(b.TargetDefeated, Is.True);
            Assert.Throws<System.InvalidOperationException>(() => target.Apply(Packet("late", 1, 0, 1)));
        }

        [Test]
        public void M1TurnLifecycle_ClearsShieldThenGrantsStableSourcesOnce()
        {
            RogueCombatantState target = new RogueCombatantState("hero", 18, 9);
            target.BeginOwnTurn(3, new[] { new ShieldGrant("chest", 4), new ShieldGrant("cover-heavy", 4) });
            Assert.That(target.CurrentShield, Is.EqualTo(8));
            Assert.That(target.TryGrantShield("chest", 4, 3), Is.False);
            Assert.That(target.ShieldEvents.Count(value => value.EventKind == ShieldEventKind.ClearedAtTurnStart), Is.EqualTo(1));
        }

        [Test]
        public void M1BreakStance_ClearsShieldBlocksEveryGrantAndExpiresAfterOwnersTurn()
        {
            RogueCombatantState target = new RogueCombatantState("enemy", 12, 6);
            target.ApplyBreakStance(7);
            Assert.That(target.CurrentShield, Is.Zero);
            target.BeginOwnTurn(7, new[] { new ShieldGrant("chest", 4), new ShieldGrant("cover-light", 2) });
            Assert.That(target.CurrentShield, Is.Zero);
            Assert.That(target.ShieldEvents.Count(value => value.EventKind == ShieldEventKind.PreventedByBreakStance), Is.EqualTo(2));
            target.EndOwnTurn(7);
            Assert.That(target.BreakStance.IsActive, Is.False);
            Assert.That(target.TryGrantShield("spell", 6, 8), Is.True);
        }

        [Test]
        public void M1FormalRogueliteRuleset_IgnoresLegacyArmorBlockCoverAndPierce()
        {
            GridMap map = new GridMap(3, 1);
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West) { Armor = 99, Block = 99 };
            map.SetTile(enemy.Position, new TileState { Cover = CoverType.Light, Durability = 99 });
            CombatState state = new CombatState(map, new[] { hero, enemy });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(state, "hero");

            CombatResolver.AttackPreview preview = CombatResolver.PreviewAttack(state, "hero", "enemy", false);

            Assert.That(preview.CoverReduction, Is.Zero);
            Assert.That(preview.ArmorReduction, Is.Zero);
            Assert.That(preview.BlockReduction, Is.Zero);
            Assert.That(preview.ShieldAbsorption, Is.Zero);
            Assert.That(preview.FinalDamage, Is.EqualTo(hero.MainHand.Damage));
        }

        [Test]
        public void M1FormalCover_GrantsHighestConditionalShieldOnceAndNeverReducesDamage()
        {
            GridMap map = new GridMap(3, 2);
            UnitState hero = new UnitState("hero", true, new GridPosition(1, 0), Facing.North);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(2, 0), Facing.West);
            map.SetTile(hero.Position, new TileState { Cover = CoverType.Light, Durability = 10 });
            map.SetTile(new GridPosition(1, 1), new TileState { Cover = CoverType.Heavy, Durability = 10 });
            CombatState state = new CombatState(map, new[] { hero, enemy });
            state.ConfigureRuleset(CombatRuleset.Roguelite);

            CombatResolver.BeginTurn(state, "hero");

            Assert.That(hero.Shield, Is.EqualTo(4));
            Assert.That(state.TryGrantRogueliteShield("hero", "cover-heavy", 4), Is.False);
        }

        [Test]
        public void M1FormalBreakStance_ClearsAndBlocksShieldUntilOwnersTurnEnds()
        {
            GridMap map = new GridMap(2, 1);
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
            CombatState state = new CombatState(map, new[] { hero, enemy });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.ApplyRogueliteBreakStance("hero");
            CombatResolver.BeginTurn(state, "hero");

            Assert.That(state.TryGrantRogueliteShield("hero", "spell", 6), Is.False);
            CombatResolver.EndTurn(state, hero);
            Assert.That(hero.HasStatus(StatusType.BreakStance), Is.False);
        }

        [Test]
        public void UiM3FormalShieldEvents_ReportGrantAbsorbWastePreventionAndTurnClear()
        {
            GridMap map = new GridMap(2, 1);
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
            CombatState state = new CombatState(map, new[] { hero, enemy });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(state, hero.Id);
            Assert.That(state.TryGrantRogueliteShield(hero.Id, "spell", 6), Is.True);
            CombatEffectExecutor.Execute(state, enemy.Id, CombatEffect.AbsorbShield(hero.Id, 2));
            state.ApplyRogueliteBreakStance(hero.Id);
            Assert.That(state.TryGrantRogueliteShield(hero.Id, "cover-light", 2), Is.False);

            Assert.That(state.RogueShieldEvents.Any(value => value.EventKind == ShieldEventKind.Granted && value.SourceId == "spell"), Is.True);
            Assert.That(state.RogueShieldEvents.Any(value => value.EventKind == ShieldEventKind.Absorbed && value.Amount == 2), Is.True);
            Assert.That(state.RogueShieldEvents.Any(value => value.EventKind == ShieldEventKind.Wasted && value.Amount == 4), Is.True);
            Assert.That(state.RogueShieldEvents.Any(value => value.EventKind == ShieldEventKind.PreventedByBreakStance), Is.True);

            CombatState cloned = state.Clone();
            Assert.That(cloned.RogueShieldEvents.Count, Is.EqualTo(state.RogueShieldEvents.Count));
        }

        private static DamagePacket Packet(string id, int amount, int index, int count)
            => new DamagePacket(id, "hero", "enemy", "multi", new[] { new DamageComponent(DamageComponentKind.Physical, amount) },
                new[] { DamageTag.Segment }, segmentIndex: index, segmentCount: count);
    }
}
