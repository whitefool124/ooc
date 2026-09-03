using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using OCC.Combat.Presentation;

namespace OCC.Combat.Tests
{
    public sealed class EnemyPackTests
    {
        private static readonly string[] PackIds =
        {
            "shieldguard", "pyromancer", "raider", "elite_vanguard", "sigil_mauler",
            "barrier_mender", "tether_hound", "stone_snare", "lantern_revealer", "rune_arbalist"
        };
        private static readonly string[] EncounterIds =
        {
            "rail_patrol", "depot_wreck", "relay_raid", "signal_hub", "gatehouse",
            "transmission_tower", "elite_foundry", "core_approach", "core_finale"
        };

        [Test]
        public void FinalArchetypes_HaveStableEraCorrectContracts()
        {
            EnemyArchetype mauler = EnemyArchetypes.Get("sigil_mauler");
            EnemyArchetype mender = EnemyArchetypes.Get("barrier_mender");
            EnemyArchetype hound = EnemyArchetypes.Get("tether_hound");

            Assert.That((mauler.DisplayName, mauler.MaxHealth, mauler.Armor, mauler.Shield, mauler.Speed),
                Is.EqualTo(("承压检验偶", 14, 1, 0, 8)));
            Assert.That((mender.DisplayName, mender.MaxHealth, mender.Armor, mender.Shield, mender.Speed),
                Is.EqualTo(("护障助教", 12, 0, 4, 7)));
            Assert.That((hound.DisplayName, hound.MaxHealth, hound.Armor, hound.Shield, hound.Speed),
                Is.EqualTo(("缚环寻迹兽", 10, 0, 0, 11)));

            Assert.That((mauler.PrimarySkill.Id, mauler.PrimarySkill.Range, mauler.PrimarySkill.ManaCost, mauler.PrimarySkill.Cooldown),
                Is.EqualTo(("enemy_sundering_sigil", 1, 1, 2)));
            Assert.That((mender.PrimarySkill.Id, mender.PrimarySkill.Range, mender.PrimarySkill.ManaCost, mender.PrimarySkill.Cooldown),
                Is.EqualTo(("enemy_ward_mend", 4, 2, 2)));
            Assert.That((hound.PrimarySkill.Id, hound.PrimarySkill.Range, hound.PrimarySkill.ManaCost, hound.PrimarySkill.Cooldown),
                Is.EqualTo(("enemy_tether_pounce", 1, 1, 1)));
        }

        [Test]
        public void AcademyIdentities_KeepStableIdsAndUseNonLethalResolutionSemantics()
        {
            var expected = new Dictionary<string, (string Name, EnemyResolutionKind Kind, string Resolution)>
            {
                ["shieldguard"] = ("高年级陪练生·盾术", EnemyResolutionKind.Student, "认输并退出考核"),
                ["pyromancer"] = ("高年级陪练生·火矢", EnemyResolutionKind.Student, "认输并退出考核"),
                ["raider"] = ("高年级陪练生·侧锋", EnemyResolutionKind.Student, "认输并退出考核"),
                ["rune_arbalist"] = ("高年级陪练生·重弩", EnemyResolutionKind.Student, "认输并退出考核"),
                ["barrier_mender"] = ("护障助教", EnemyResolutionKind.Staff, "失去战斗能力并退出冲突"),
                ["stone_snare"] = ("约束助教", EnemyResolutionKind.Staff, "失去战斗能力并退出冲突"),
                ["lantern_revealer"] = ("档案巡查员", EnemyResolutionKind.Staff, "失去战斗能力并退出冲突"),
                ["elite_vanguard"] = ("刻阵教官", EnemyResolutionKind.Staff, "失去战斗能力并退出冲突"),
                ["tether_hound"] = ("缚环寻迹兽", EnemyResolutionKind.Beast, "被制服并重新约束"),
                ["sigil_mauler"] = ("承压检验偶", EnemyResolutionKind.Construct, "被摧毁")
            };

            foreach (KeyValuePair<string, (string Name, EnemyResolutionKind Kind, string Resolution)> pair in expected)
            {
                EnemyArchetype archetype = EnemyArchetypes.Get(pair.Key);
                Assert.That(archetype.DisplayName, Is.EqualTo(pair.Value.Name), pair.Key);
                Assert.That(archetype.ResolutionKind, Is.EqualTo(pair.Value.Kind), pair.Key);
                UnitState unit = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
                archetype.Apply(unit);
                string log = EnemyResolutionSemantics.DefeatLog(unit);
                Assert.That(log, Does.Contain(pair.Value.Resolution), pair.Key);
                if (pair.Value.Kind != EnemyResolutionKind.Construct)
                    Assert.That(log, Does.Not.Contain("摧毁").And.Not.Contain("击杀").And.Not.Contain("死亡"), pair.Key);

                UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
                CombatState combat = new CombatState(new GridMap(2, 1), new[] { hero, unit });
                CombatEffectExecutor.Execute(combat, hero.Id, CombatEffect.DamageHealth(unit.Id, 99));
                Assert.That(combat.EventLog.Last(), Does.Contain(pair.Value.Resolution), pair.Key + " combat log");
            }
        }

        [Test]
        public void FormalVisualLoader_RequiresEveryResolvedUnitTextureAndSixFrameAction()
        {
            var visuals = new CombatFormalVisualAssets();
            Assert.DoesNotThrow(visuals.LoadRuntime);
            foreach (string id in PackIds)
            {
                UnitState unit = new UnitState("enemy_" + id, false, new GridPosition(1, 0), Facing.West);
                EnemyArchetypes.Get(id).Apply(unit);
                Assert.That(visuals.Unit(unit), Is.Not.Null, id + " static");
                for (int frame = 0; frame < 6; frame++) Assert.That(visuals.Unit(unit, frame), Is.Not.Null, id + " frame " + frame);
            }
        }

        [Test]
        public void ExpansionPack_HasTenDistinctClosedLoopContracts()
        {
            Assert.That(PackIds.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(10));
            EnemyArchetype[] pack = PackIds.Select(EnemyArchetypes.Get).ToArray();
            Assert.That(pack.Select(enemy => enemy.ArtId).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(10));
            Assert.That(pack.Select(enemy => enemy.PrimarySkill.Id).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(10));
            Assert.That(EnemyAbilityCatalog.All.Select(skill => skill.Id).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(10));
            Assert.That(pack.All(enemy => enemy.MaxHealth >= 10 && enemy.MaxHealth <= 14), Is.True);
        }

        [Test]
        public void RetiredDraftAndModernFirearmArchetypes_AreNotRuntimeReachable()
        {
            string[] retired = { "rifleman", "sniper", "aether_sapper", "barrier_engineer", "relay_hound" };
            foreach (string id in retired)
                Assert.Throws<KeyNotFoundException>(() => EnemyArchetypes.Get(id), id);

            string[] forbidden = { "rifle", "sniper", "sapper", "engineer", "步枪", "狙击", "工兵", "爆破" };
            string enemySurface = string.Join("|", EnemyArchetypes.All.SelectMany(archetype => new[]
            {
                archetype.Id, archetype.DisplayName, archetype.Weapon.Id, archetype.Weapon.DisplayName,
                archetype.PrimarySkill?.Id ?? string.Empty, archetype.PrimarySkill?.DisplayName ?? string.Empty
            })).ToLowerInvariant();
            foreach (string term in forbidden) Assert.That(enemySurface, Does.Not.Contain(term.ToLowerInvariant()), term);
        }

        [Test]
        public void EveryActiveEncounter_ResolvesAndContainsNoRetiredModernRole()
        {
            string[] forbiddenIds = { "rifleman", "sniper", "aether_sapper", "barrier_engineer", "relay_hound" };
            foreach (string encounterId in EncounterIds)
            {
                RogueliteEncounterDefinition encounter = RogueliteEncounterCatalog.For(encounterId);
                Assert.That(encounter.EnemyArchetypeIds, Is.Not.Empty, encounterId);
                Assert.That(encounter.EnemyArchetypeIds.Intersect(forbiddenIds), Is.Empty, encounterId);
                foreach (string archetypeId in encounter.EnemyArchetypeIds)
                    Assert.DoesNotThrow(() => EnemyArchetypes.Get(archetypeId), encounterId + ":" + archetypeId);
            }
            string[] active = EncounterIds.SelectMany(id => RogueliteEncounterCatalog.For(id).EnemyArchetypeIds).Distinct(StringComparer.Ordinal).ToArray();
            Assert.That(PackIds.Except(active), Is.Empty, "Every pack enemy must be reachable through an active first-region encounter.");
        }

        [Test]
        public void SigilMauler_UsesSunderingSigilOnceThenFallsBackToWeapon()
        {
            CombatState state = CreateState("sigil_mauler", new GridPosition(1, 0), out UnitState enemy, out UnitState hero);
            CombatCommand first = EnemyTactics.Choose(state, enemy, hero);
            Assert.That(first.Type, Is.EqualTo(CombatCommandType.UseSkill));
            CombatResolver.BeginTurn(state, enemy.Id);
            CombatResolver.Resolve(state, first);
            Assert.That(hero.HasStatus(StatusType.ArmorBreak), Is.True);

            CombatCommand second = EnemyTactics.Choose(state, enemy, hero);
            Assert.That(second.Type, Is.EqualTo(CombatCommandType.Attack));
        }

        [Test]
        public void BarrierMender_SelectsLargestShieldGapWithStableIdTieBreakAndRestoresShield()
        {
            GridMap map = new GridMap(7, 3);
            UnitState hero = new UnitState("hero", true, new GridPosition(6, 1), Facing.West);
            UnitState mender = new UnitState("mender", false, new GridPosition(0, 1), Facing.East);
            UnitState allyA = new UnitState("ally_a", false, new GridPosition(1, 1), Facing.East);
            UnitState allyB = new UnitState("ally_b", false, new GridPosition(2, 1), Facing.East);
            EnemyArchetypes.Get("barrier_mender").Apply(mender);
            EnemyArchetypes.Get("warden").Apply(allyA);
            EnemyArchetypes.Get("warden").Apply(allyB);
            CombatState state = new CombatState(map, new[] { hero, mender, allyB, allyA });
            CombatEffectExecutor.Execute(state, mender.Id, CombatEffect.AbsorbShield(allyA.Id, 4), CombatEffect.AbsorbShield(allyB.Id, 4));

            CombatCommand command = EnemyTactics.Choose(state, mender, hero);
            Assert.That(command.Type, Is.EqualTo(CombatCommandType.UseSkill));
            Assert.That(command.TargetUnitId, Is.EqualTo("ally_a"));
            int before = allyA.Shield;
            CombatResolver.BeginTurn(state, mender.Id);
            CombatResolver.Resolve(state, command);
            Assert.That(allyA.Shield, Is.EqualTo(before + 4));
        }

        [Test]
        public void TetherHound_BindsOnlyWhenHeroIsNotAlreadyBound()
        {
            CombatState state = CreateState("tether_hound", new GridPosition(1, 0), out UnitState enemy, out UnitState hero);
            CombatCommand first = EnemyTactics.Choose(state, enemy, hero);
            Assert.That(first.Type, Is.EqualTo(CombatCommandType.UseSkill));
            CombatResolver.BeginTurn(state, enemy.Id);
            CombatResolver.Resolve(state, first);
            Assert.That(hero.HasStatus(StatusType.Bound), Is.True);
            Assert.That(EnemyTactics.Choose(state, enemy, hero).Type, Is.EqualTo(CombatCommandType.Attack));
        }

        [TestCase("shieldguard", StatusType.Slow, 1)]
        [TestCase("pyromancer", StatusType.Burning, 4)]
        [TestCase("raider", StatusType.Bound, 1)]
        [TestCase("elite_vanguard", StatusType.ArmorBreak, 1)]
        [TestCase("stone_snare", StatusType.Bound, 3)]
        [TestCase("lantern_revealer", StatusType.ArmorBreak, 3)]
        public void StatusSpecialists_UseSkillOnlyWhenStatusWindowIsOpen(string archetypeId, StatusType status, int distance)
        {
            CombatState state = CreateState(archetypeId, new GridPosition(distance, 0), out UnitState enemy, out UnitState hero);
            Assert.That(EnemyTactics.Choose(state, enemy, hero).Type, Is.EqualTo(CombatCommandType.UseSkill));
            hero.ApplyStatus(status, 2);
            Assert.That(EnemyTactics.Choose(state, enemy, hero).Type, Is.Not.EqualTo(CombatCommandType.UseSkill));
        }

        [Test]
        public void RuneArbalist_UsesHeavyBoltAtFiveCellsThenFallsBackDuringCooldown()
        {
            CombatState state = CreateState("rune_arbalist", new GridPosition(5, 0), out UnitState enemy, out UnitState hero);
            CombatCommand first = EnemyTactics.Choose(state, enemy, hero);
            Assert.That(first.Type, Is.EqualTo(CombatCommandType.UseSkill));
            CombatResolver.BeginTurn(state, enemy.Id);
            CombatResolver.Resolve(state, first);
            Assert.That(hero.Health, Is.LessThan(hero.MaxHealth));
            Assert.That(EnemyTactics.Choose(state, enemy, hero).Type, Is.EqualTo(CombatCommandType.Move));
        }

        [TestCase("shieldguard")]
        [TestCase("pyromancer")]
        [TestCase("raider")]
        [TestCase("elite_vanguard")]
        [TestCase("sigil_mauler")]
        [TestCase("barrier_mender")]
        [TestCase("tether_hound")]
        [TestCase("stone_snare")]
        [TestCase("lantern_revealer")]
        [TestCase("rune_arbalist")]
        public void FinalUnitArt_LoadsWithRequiredPixelImporter(string id)
        {
            Sprite sprite = Resources.Load<Sprite>(FormalArtRegistry.UnitPath(id));
            Assert.That(sprite, Is.Not.Null, id);
            Assert.That(sprite.texture.width, Is.EqualTo(64), id);
            Assert.That(sprite.texture.height, Is.EqualTo(64), id);
            TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite)) as TextureImporter;
            Assert.That(importer, Is.Not.Null, id);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), id);
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), id);
            Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), id);
            Assert.That(importer.mipmapEnabled, Is.False, id);
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(32f), id);
        }

        private static CombatState CreateState(string archetypeId, GridPosition enemyPosition, out UnitState enemy, out UnitState hero)
        {
            GridMap map = new GridMap(6, 3);
            hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            enemy = new UnitState("enemy", false, enemyPosition, Facing.West);
            EnemyArchetypes.Get(archetypeId).Apply(enemy);
            return new CombatState(map, new[] { hero, enemy });
        }
    }
}
