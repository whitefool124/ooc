using System;
using System.Linq;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class SkillCompositionTests
    {
        [Test]
        public void ValidationPool_HasTwentySevenValidUniqueSkillsAndFourBuilds()
        {
            Assert.That(RogueliteSkillCatalog.All.Count, Is.EqualTo(27));
            Assert.That(RogueliteSkillCatalog.Builds.Count, Is.EqualTo(4));
            Assert.That(SkillCatalogValidator.Validate(RogueliteSkillCatalog.All), Is.Empty);
            Assert.That(RogueliteSkillCatalog.All.Select(skill => skill.Id).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(27));
            Assert.That(RogueliteSkillCatalog.Builds.SelectMany(build => build.SkillIds).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(27));
            Assert.That(RogueliteSkillCatalog.All, Has.All.Matches<SkillDefinition>(skill => skill.Effects.Count > 0 && skill.Range >= 0 && skill.ManaCost >= 0 && skill.Cooldown >= 0));
        }

        [Test]
        public void Validator_RejectsAreaWithoutRadiusDeterministically()
        {
            SkillDefinition invalid = new SkillDefinition("invalid_area", "无效区域", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Area, 3, 1, 1,
                CombatFeedbackKind.Damage, new[] { SkillEffectDefinition.Damage(2, DamageType.Arcane) });

            SkillValidationIssue[] issues = SkillCatalogValidator.Validate(new[] { invalid }).ToArray();

            Assert.That(issues.Select(issue => issue.Code), Is.EqualTo(new[] { "area_radius" }));
        }

        [Test]
        public void ExistingFireBoltAndFrostBind_UseTheCompositeContract()
        {
            Assert.That(CombatCatalog.FireBolt.TargetRule, Is.EqualTo(SkillTargetRule.EnemyUnit));
            Assert.That(CombatCatalog.FireBolt.Delivery, Is.EqualTo(SkillDeliveryMethod.Projectile));
            Assert.That(CombatCatalog.FireBolt.Effects.Select(effect => effect.Type), Is.EqualTo(new[] { SkillEffectType.Damage, SkillEffectType.ApplyStatus }));
            Assert.That(CombatCatalog.FrostBind.Effects.Last().Status, Is.EqualTo(StatusType.Bound));
        }

        [Test]
        public void EveryValidationSkill_CanProduceAnOrderedExecution()
        {
            foreach (SkillDefinition skill in RogueliteSkillCatalog.All)
            {
                CombatState state = CreateSkillState();
                UnitState hero = state.GetUnit("hero");
                hero.Equip(hero.MainHand, hero.OffHand, skill, CombatCatalog.FireBolt);
                CombatResolver.BeginTurn(state, hero.Id);

                CombatCommand command = CommandFor(skill);
                CombatEffectExecution execution = CombatResolver.Resolve(state, command);

                Assert.That(execution.Results.Count, Is.GreaterThanOrEqualTo(2), skill.Id);
                Assert.That(execution.Results[0].Kind, Is.EqualTo(skill.ManaCost > 0 ? CombatEffectKind.SpendMana : CombatEffectKind.SpendActionPoints), skill.Id);
                Assert.That(execution.Results.Select(result => result.Sequence), Is.EqualTo(Enumerable.Range(0, execution.Results.Count)), skill.Id);
                Assert.That(hero.Cooldown(skill), Is.EqualTo(skill.Cooldown), skill.Id);
            }
        }

        [Test]
        public void AreaDelivery_ResolvesTargetsByStableUnitIdOrder()
        {
            GridMap map = new GridMap(6, 3);
            UnitState hero = new UnitState("hero", true, new GridPosition(1, 1), Facing.East);
            UnitState enemyB = new UnitState("enemy_b", false, new GridPosition(2, 1), Facing.West);
            UnitState enemyA = new UnitState("enemy_a", false, new GridPosition(3, 1), Facing.West);
            CombatState state = new CombatState(map, new[] { hero, enemyB, enemyA });
            hero.Equip(hero.MainHand, hero.OffHand, RogueliteSkillCatalog.Get("hammer_pulse"), CombatCatalog.FireBolt);
            CombatResolver.BeginTurn(state, hero.Id);

            CombatEffectExecution execution = CombatResolver.Resolve(state, CombatCommand.UseSkill(hero.Id, 0, enemyB.Id));

            Assert.That(execution.Results.Where(result => result.Kind == CombatEffectKind.DamageHealth).Select(result => result.TargetUnitId),
                Is.EqualTo(new[] { "enemy_a", "enemy_b" }));
        }

        [Test]
        public void FourBuilds_EquipTheirDeclaredAnchorSkills()
        {
            foreach (RogueliteSkillBuild build in RogueliteSkillCatalog.Builds)
            {
                UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
                build.Apply(hero);
                Assert.That(hero.SkillOne.Id, Is.EqualTo(build.PrimarySkillId), build.Id);
                Assert.That(hero.SkillTwo.Id, Is.EqualTo(build.SecondarySkillId), build.Id);
                Assert.That(build.SkillIds, Does.Contain(hero.SkillOne.Id));
                Assert.That(build.SkillIds, Does.Contain(hero.SkillTwo.Id));
            }
        }

        [Test]
        public void FourBuilds_CanCompleteTheSameDeterministicEliminationSlice()
        {
            foreach (RogueliteSkillBuild build in RogueliteSkillCatalog.Builds)
            {
                UnitState hero = new UnitState("hero", true, new GridPosition(0, 1), Facing.East);
                UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 1), Facing.East);
                CombatState state = new CombatState(new GridMap(4, 3), new[] { hero, enemy }, new CombatObjective[] { new EliminationObjective() });
                build.Apply(hero);
                CombatResolver.BeginTurn(state, hero.Id);

                if (hero.SkillOne.TargetRule == SkillTargetRule.EnemyUnit)
                    CombatResolver.Resolve(state, CombatCommand.UseSkill(hero.Id, 0, enemy.Id));
                while (enemy.IsAlive && hero.ActionPoints > 0)
                    CombatResolver.Resolve(state, CombatCommand.Attack(hero.Id, enemy.Id));
                if (enemy.IsAlive)
                {
                    CombatResolver.BeginTurn(state, hero.Id);
                    while (enemy.IsAlive && hero.ActionPoints > 0)
                        CombatResolver.Resolve(state, CombatCommand.Attack(hero.Id, enemy.Id));
                }

                Assert.That(state.IsVictory, Is.True, build.Id);
            }
        }

        [Test]
        public void SameCompositeSkillCommand_ProducesIdenticalResults()
        {
            CombatState first = CreateSkillState();
            CombatState second = first.Clone();
            SkillDefinition skill = RogueliteSkillCatalog.Get("searing_mark");
            first.GetUnit("hero").Equip(first.GetUnit("hero").MainHand, first.GetUnit("hero").OffHand, skill, CombatCatalog.FireBolt);
            second.GetUnit("hero").Equip(second.GetUnit("hero").MainHand, second.GetUnit("hero").OffHand, skill, CombatCatalog.FireBolt);
            CombatResolver.BeginTurn(first, "hero");
            CombatResolver.BeginTurn(second, "hero");

            CombatEffectExecution firstExecution = CombatResolver.Resolve(first, CombatCommand.UseSkill("hero", 0, "enemy"));
            CombatEffectExecution secondExecution = CombatResolver.Resolve(second, CombatCommand.UseSkill("hero", 0, "enemy"));

            Assert.That(Signature(secondExecution), Is.EqualTo(Signature(firstExecution)));
            Assert.That(second.EventLog, Is.EqualTo(first.EventLog));
        }

        [Test]
        public void ContractContainsNoHitChanceOrCriticalChanceFields()
        {
            string[] propertyNames = typeof(SkillDefinition).GetProperties().Select(property => property.Name).ToArray();
            Assert.That(propertyNames, Has.None.Contains("HitChance"));
            Assert.That(propertyNames, Has.None.Contains("CriticalChance"));
            Assert.That(propertyNames, Has.None.Contains("Random"));
        }

        private static CombatCommand CommandFor(SkillDefinition skill)
        {
            if (skill.TargetRule == SkillTargetRule.Self) return CombatCommand.UseSkill("hero", 0, null);
            if (skill.TargetRule == SkillTargetRule.AllyUnit) return CombatCommand.UseSkill("hero", 0, "ally");
            if (skill.TargetRule == SkillTargetRule.GridCell) return CombatCommand.UseSkillAt("hero", 0, new GridPosition(0, 3), Facing.North);
            if (skill.TargetRule == SkillTargetRule.Destructible) return CombatCommand.UseSkillAt("hero", 0, new GridPosition(1, 0), Facing.South);
            return CombatCommand.UseSkill("hero", 0, "enemy");
        }

        private static CombatState CreateSkillState()
        {
            GridMap map = new GridMap(6, 4);
            map.SetTile(new GridPosition(1, 0), new TileState { Cover = CoverType.Light, Durability = 6 });
            return new CombatState(map, new[]
            {
                new UnitState("hero", true, new GridPosition(0, 1), Facing.East),
                new UnitState("ally", true, new GridPosition(0, 2), Facing.East),
                new UnitState("enemy", false, new GridPosition(1, 1), Facing.West),
                new UnitState("enemy_area", false, new GridPosition(2, 1), Facing.West)
            }, new CombatObjective[] { new EliminationObjective() });
        }

        private static string Signature(CombatEffectExecution execution) => string.Join("|", execution.Results.Select(result =>
            $"{result.Sequence}:{result.Kind}:{result.TargetUnitId}:{result.RequestedAmount}:{result.AppliedAmount}:{result.ValueBefore}:{result.ValueAfter}:{result.Status}:{result.StatusPhase}:{result.PositionBefore}:{result.PositionAfter}"));
    }
}
