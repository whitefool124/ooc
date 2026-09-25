using System.Linq;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class AcademyEnemyAreaRuntimeTests
    {
        [Test]
        public void TargetCrossSkills_AreValidAndRequireACompletedHeroTurnBeforeDamage()
        {
            Assert.That(AcademyEnemyAreaRuntime.All.Select(skill => skill.Id),
                Is.EquivalentTo(new[] { "SK-AOE-RAIDER", "SK-AOE-SHIELDGUARD", "SK-AOE-PYROMANCER",
                    "SK-AOE-ARBALEST", "SK-AOE-HOUND", "SK-AOE-DUMMY", "SK-AOE-MENDER", "SK-AOE-SNARE",
                    "SK-AOE-REVEALER", "SK-AOE-SIGNAL", "SK-AOE-VANGUARD", "SK-AOE-PROTOTYPE",
                    "SK-AOE-WIND", "SK-AOE-ELDER" }));
            Assert.That(SkillCatalogValidator.Validate(AcademyEnemyAreaRuntime.All), Is.Empty);

            UnitState hero = new UnitState("hero", true, new GridPosition(2, 2));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(2, 0));
            EnemyArchetypes.Get("pyromancer").Apply(enemy);
            CombatState state = new CombatState(new GridMap(5, 5), new[] { hero, enemy });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            CombatResolver.BeginTurn(state, enemy.Id);

            CombatCommand prepare = state.AcademyEnemyArea.Choose(state, enemy, hero, CombatCommand.EndTurn(enemy.Id));
            Assert.That(prepare.SlotIndex, Is.EqualTo(AcademyEnemyAreaRuntime.PrepareSkillIndex));
            Assert.That(state.AcademyEnemyArea.PresentIntent(state, enemy, prepare).AffectedCells.Count, Is.EqualTo(5));
            int healthBefore = hero.Health;
            CombatResolver.Resolve(state, prepare);
            Assert.That(hero.Health, Is.EqualTo(healthBefore));
            Assert.That(state.AcademyEnemyArea.HasPending(enemy.Id), Is.True);
            Assert.That(state.Clone().AcademyEnemyArea.HasPending(enemy.Id), Is.True);
            Assert.That(state.AcademyEnemyArea.Choose(state, enemy, hero, CombatCommand.EndTurn(enemy.Id)).Type,
                Is.EqualTo(CombatCommandType.EndTurn));

            UnitState restoredHero = new UnitState("hero", true, new GridPosition(2, 2));
            UnitState restoredEnemy = new UnitState("enemy", false, new GridPosition(2, 0));
            EnemyArchetypes.Get("pyromancer").Apply(restoredEnemy);
            CombatState restored = new CombatState(new GridMap(5, 5), new[] { restoredHero, restoredEnemy });
            restored.ConfigureRuleset(CombatRuleset.Roguelite);
            restored.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            CombatResolver.BeginTurn(restored, restoredEnemy.Id);
            CombatJournalReplayer.ReplayAfterActivation(restored,
                new[] { CombatJournalEntry.Accepted(prepare).Encode() });
            Assert.That(restored.AcademyEnemyArea.HasPending(restoredEnemy.Id), Is.True);
            Assert.That(restoredHero.Health, Is.EqualTo(healthBefore));

            CombatResolver.BeginTurn(state, hero.Id);
            CombatResolver.EndTurn(state, hero);
            Assert.That(state.AcademyEnemyArea.CompletedHeroTurns, Is.EqualTo(1));
            CombatResolver.BeginTurn(state, enemy.Id);
            CombatCommand resolve = state.AcademyEnemyArea.Choose(state, enemy, hero, CombatCommand.EndTurn(enemy.Id));
            Assert.That(resolve.SlotIndex, Is.EqualTo(AcademyEnemyAreaRuntime.ResolveSkillIndex));
            CombatResolver.Resolve(state, resolve);
            Assert.That(hero.Health, Is.LessThan(healthBefore));
            Assert.That(hero.StatusStrength(StatusType.Burning), Is.EqualTo(2));
            Assert.That(state.AcademyEnemyArea.HasPending(enemy.Id), Is.False);
            Assert.That(enemy.Cooldown(AcademyEnemyAreaRuntime.For(enemy.EnemyArchetypeId)), Is.EqualTo(2));
        }

        [Test]
        public void SignalArea_PreviewsSixCellsAndSettlesOnlyAfterAFullHeroTurn()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(5, 3));
            UnitState keeper = new UnitState("keeper", false, new GridPosition(1, 3));
            EnemyArchetypes.Get("signal_keeper").Apply(keeper);
            CombatState state = new CombatState(new GridMap(9, 7), new[] { hero, keeper });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachAcademyFieldEnemy(new AcademyFieldEnemyRuntime());
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            CombatResolver.BeginTurn(state, keeper.Id);

            CombatCommand prepare = state.AcademyEnemyArea.Choose(state, keeper, hero, CombatCommand.EndTurn(keeper.Id));
            Assert.That(prepare.SlotIndex, Is.EqualTo(AcademyEnemyAreaRuntime.PrepareSkillIndex));
            EnemyIntentPresentation preview = state.AcademyEnemyArea.PresentIntent(state, keeper, prepare);
            Assert.That(preview.AttackRange.Count, Is.EqualTo(6));
            Assert.That(preview.AffectedCells, Does.Contain(hero.Position));
            int healthBefore = hero.Health;
            CombatResolver.Resolve(state, prepare);
            Assert.That(hero.Health, Is.EqualTo(healthBefore));
            Assert.That(state.Environment.LightLanes, Is.Empty);
            Assert.That(state.Clone().AcademyEnemyArea.HasPending(keeper.Id), Is.True);

            CombatResolver.BeginTurn(state, hero.Id);
            CombatResolver.EndTurn(state, hero);
            CombatResolver.BeginTurn(state, keeper.Id);
            CombatCommand resolve = state.AcademyEnemyArea.Choose(state, keeper, hero, CombatCommand.EndTurn(keeper.Id));
            Assert.That(resolve.SlotIndex, Is.EqualTo(AcademyEnemyAreaRuntime.ResolveSkillIndex));
            CombatResolver.Resolve(state, resolve);
            Assert.That(hero.Health, Is.EqualTo(healthBefore));
            Assert.That(state.Environment.LightLanes.Single().Direction, Is.EqualTo(FieldWindState.East));
            CombatResolver.EndTurn(state, keeper);
            Assert.That(hero.Health, Is.LessThan(healthBefore));
            Assert.That(keeper.Cooldown(AcademyEnemyAreaRuntime.For("signal_keeper")), Is.EqualTo(2));
        }

        [Test]
        public void VanguardArea_BuildsAtMostThreeTemporaryWallsAfterTheTelegraph()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(5, 3));
            UnitState vanguard = new UnitState("vanguard", false, new GridPosition(1, 3));
            EnemyArchetypes.Get("elite_vanguard").Apply(vanguard);
            CombatState state = new CombatState(new GridMap(9, 7), new[] { hero, vanguard });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachAcademyFieldEnemy(new AcademyFieldEnemyRuntime());
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            CombatResolver.BeginTurn(state, vanguard.Id);
            Assert.That(state.Map.GetTile(new GridPosition(2, 3)).Cover, Is.EqualTo(CoverType.None),
                "回合开始不应自动夯墙。");

            CombatCommand prepare = state.AcademyEnemyArea.Choose(state, vanguard, hero, CombatCommand.EndTurn(vanguard.Id));
            Assert.That(prepare.SlotIndex, Is.EqualTo(AcademyEnemyAreaRuntime.PrepareSkillIndex));
            EnemyIntentPresentation intent = state.AcademyEnemyArea.PresentIntent(state, vanguard, prepare);
            Assert.That(intent.AttackRange, Is.EquivalentTo(new[]
                { new GridPosition(2, 3), new GridPosition(3, 3), new GridPosition(4, 3) }));
            Assert.That(intent.AffectedCells.Count, Is.EqualTo(3));
            CombatResolver.Resolve(state, prepare);
            Assert.That(state.Map.GetTile(new GridPosition(2, 3)).Cover, Is.EqualTo(CoverType.None));
            CombatResolver.BeginTurn(state, hero.Id);
            CombatResolver.EndTurn(state, hero);
            CombatResolver.BeginTurn(state, vanguard.Id);
            CombatCommand resolve = state.AcademyEnemyArea.Choose(state, vanguard, hero, CombatCommand.EndTurn(vanguard.Id));
            Assert.That(resolve.SlotIndex, Is.EqualTo(AcademyEnemyAreaRuntime.ResolveSkillIndex));
            CombatResolver.Resolve(state, resolve);
            foreach (int x in new[] { 2, 3, 4 })
            {
                TileState wall = state.Map.GetTile(new GridPosition(x, 3));
                Assert.That(wall.Cover, Is.EqualTo(CoverType.Heavy));
                Assert.That(wall.Durability, Is.EqualTo(TileState.TemporaryHeavyCoverDurability));
                Assert.That(wall.StructureOwnerUnitId, Is.EqualTo(vanguard.Id));
            }
            Assert.That(hero.Health, Is.EqualTo(hero.MaxHealth), "夯墙不造成伤害。");
            Assert.That(vanguard.Cooldown(AcademyEnemyAreaRuntime.For("elite_vanguard")), Is.EqualTo(2));
        }

        [Test]
        public void PrototypeArea_TelegraphsAndDetonatesTwoAdjacentDevicesAcrossBothSides()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(3, 4));
            UnitState hand = new UnitState("hand", false, new GridPosition(1, 1));
            UnitState ally = new UnitState("ally", false, new GridPosition(4, 4));
            EnemyArchetypes.Get("prototype_hand").Apply(hand);
            GridMap map = new GridMap(9, 7);
            foreach (GridPosition cell in new[] { new GridPosition(3, 3), new GridPosition(4, 3) })
                map.SetTile(cell, new TileState
                { IsDevice = true, IsOverloadDevice = true, Durability = TileState.PrototypeDurability });
            CombatState state = new CombatState(map, new[] { hero, hand, ally });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachAcademyFieldEnemy(new AcademyFieldEnemyRuntime());
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            CombatResolver.BeginTurn(state, hand.Id);
            Assert.That(map.PositionsWith(tile => tile.IsDeviceLike).Count(), Is.EqualTo(2),
                "回合开始不应自动布放或引爆。");
            CombatCommand prepare = state.AcademyEnemyArea.Choose(state, hand, hero, CombatCommand.EndTurn(hand.Id));
            Assert.That(prepare.SlotIndex, Is.EqualTo(AcademyEnemyAreaRuntime.PrepareSkillIndex));
            EnemyIntentPresentation intent = state.AcademyEnemyArea.PresentIntent(state, hand, prepare);
            Assert.That(intent.AffectedCells, Does.Contain(hero.Position));
            Assert.That(intent.AffectedCells, Does.Contain(ally.Position));
            int heroHealth = hero.Health, allyHealth = ally.Health;
            CombatResolver.Resolve(state, prepare);
            Assert.That(hero.Health, Is.EqualTo(heroHealth));
            Assert.That(state.Clone().AcademyEnemyArea.HasPending(hand.Id), Is.True);
            CombatResolver.BeginTurn(state, hero.Id);
            CombatResolver.EndTurn(state, hero);
            CombatResolver.BeginTurn(state, hand.Id);
            CombatCommand resolve = state.AcademyEnemyArea.Choose(state, hand, hero, CombatCommand.EndTurn(hand.Id));
            Assert.That(resolve.SlotIndex, Is.EqualTo(AcademyEnemyAreaRuntime.ResolveSkillIndex));
            CombatResolver.Resolve(state, resolve);
            Assert.That(hero.Health, Is.LessThan(heroHealth));
            Assert.That(ally.Health, Is.LessThan(allyHealth));
            Assert.That(map.GetTile(new GridPosition(3, 3)).IsOverloadDevice, Is.False);
            Assert.That(map.GetTile(new GridPosition(4, 3)).IsOverloadDevice, Is.False);
            Assert.That(hand.Cooldown(AcademyEnemyAreaRuntime.For("prototype_hand")), Is.EqualTo(2));
        }

        [Test]
        public void WindArea_FollowsFrozenPublicWindAndLeavesFireOnlyAtTheEnd()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(4, 3));
            UnitState librarian = new UnitState("librarian", false, new GridPosition(1, 1));
            UnitState ally = new UnitState("ally", false, new GridPosition(5, 3));
            EnemyArchetypes.Get("wind_librarian").Apply(librarian);
            CombatState state = new CombatState(new GridMap(9, 7), new[] { hero, librarian, ally });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachAcademyFieldEnemy(new AcademyFieldEnemyRuntime());
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            state.AttachRogueSpellRuntime(new OCC.Combat.Roguelite.RogueSpellCombatRuntime(state,
                OCC.Combat.Roguelite.RogueSpellLoadout.Restore(new[] { "BASE-FIRE-MELEE" },
                    new[] { "BASE-FIRE-MELEE", "", "", "", "", "", "", "" }, true)));
            state.RogueSpells.FireBattle.CreateOrRefreshFireground(new GridPosition(2, 3), 3, 2, "test-fire");
            Assert.That(state.Environment.Wind.TryChange(FieldWindState.East, 1), Is.True);
            CombatResolver.BeginTurn(state, librarian.Id);
            CombatCommand prepare = state.AcademyEnemyArea.Choose(state, librarian, hero,
                CombatCommand.EndTurn(librarian.Id));
            Assert.That(prepare.SlotIndex, Is.EqualTo(AcademyEnemyAreaRuntime.PrepareSkillIndex));
            EnemyIntentPresentation intent = state.AcademyEnemyArea.PresentIntent(state, librarian, prepare);
            Assert.That(intent.AttackRange, Is.EquivalentTo(new[]
                { new GridPosition(3, 3), hero.Position, ally.Position }));
            int heroHealth = hero.Health, allyHealth = ally.Health;
            CombatResolver.Resolve(state, prepare);
            Assert.That(hero.Health, Is.EqualTo(heroHealth));
            CombatResolver.BeginTurn(state, hero.Id);
            CombatResolver.EndTurn(state, hero);
            CombatResolver.BeginTurn(state, librarian.Id);
            CombatCommand resolve = state.AcademyEnemyArea.Choose(state, librarian, hero,
                CombatCommand.EndTurn(librarian.Id));
            Assert.That(resolve.SlotIndex, Is.EqualTo(AcademyEnemyAreaRuntime.ResolveSkillIndex));
            CombatResolver.Resolve(state, resolve);
            Assert.That(hero.Health, Is.LessThan(heroHealth));
            Assert.That(ally.Health, Is.LessThan(allyHealth));
            Assert.That(state.RogueSpells.FireBattle.HasFireground(new GridPosition(2, 3)), Is.False);
            Assert.That(state.RogueSpells.FireBattle.HasFireground(new GridPosition(3, 3)), Is.False);
            Assert.That(state.RogueSpells.FireBattle.HasFireground(new GridPosition(4, 3)), Is.False);
            Assert.That(state.RogueSpells.FireBattle.HasFireground(new GridPosition(5, 3)), Is.True);
            Assert.That(librarian.Cooldown(AcademyEnemyAreaRuntime.For("wind_librarian")), Is.EqualTo(2));
        }

        [TestCase("tether_hound", StatusType.Bound)]
        [TestCase("sigil_mauler", StatusType.BreakStance)]
        [TestCase("elder_tracker_hound", StatusType.Marked)]
        public void SelfCenteredSkills_UseConfiguredAreaAndResolveAfterHeroTurn(string archetypeId, StatusType expectedStatus)
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(2, 3));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(2, 2));
            EnemyArchetypes.Get(archetypeId).Apply(enemy);
            CombatState state = new CombatState(new GridMap(5, 5), new[] { hero, enemy });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            CombatResolver.BeginTurn(state, enemy.Id);

            CombatCommand prepare = state.AcademyEnemyArea.Choose(state, enemy, hero, CombatCommand.EndTurn(enemy.Id));
            Assert.That(prepare.SlotIndex, Is.EqualTo(AcademyEnemyAreaRuntime.PrepareSkillIndex));
            EnemyIntentPresentation intent = state.AcademyEnemyArea.PresentIntent(state, enemy, prepare);
            Assert.That(intent.AffectedCells, Does.Contain(hero.Position));
            Assert.That(intent.AffectedCells.Contains(enemy.Position), Is.EqualTo(archetypeId == "elder_tracker_hound"));
            CombatResolver.Resolve(state, prepare);
            Assert.That(hero.HasStatus(expectedStatus), Is.False);

            CombatResolver.BeginTurn(state, hero.Id);
            CombatResolver.EndTurn(state, hero);
            CombatResolver.BeginTurn(state, enemy.Id);
            CombatCommand resolve = state.AcademyEnemyArea.Choose(state, enemy, hero, CombatCommand.EndTurn(enemy.Id));
            Assert.That(resolve.SlotIndex, Is.EqualTo(AcademyEnemyAreaRuntime.ResolveSkillIndex));
            CombatResolver.Resolve(state, resolve);
            Assert.That(hero.HasStatus(expectedStatus), Is.True);
            Assert.That(enemy.HasStatus(expectedStatus), Is.EqualTo(archetypeId == "elder_tracker_hound"));
        }

        [TestCase("raider")]
        [TestCase("shieldguard")]
        public void FrontFanSkills_PreviewThreeCellsAndApplyCenterOnlyBind(string archetypeId)
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(3, 3));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(2, 2));
            UnitState centerTarget = new UnitState("center", false, new GridPosition(3, 2));
            EnemyArchetypes.Get(archetypeId).Apply(enemy);
            CombatState state = new CombatState(new GridMap(6, 6), new[] { hero, enemy, centerTarget });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            CombatResolver.BeginTurn(state, enemy.Id);

            CombatCommand prepare = state.AcademyEnemyArea.Choose(state, enemy, hero, CombatCommand.EndTurn(enemy.Id));
            Assert.That(prepare.SlotIndex, Is.EqualTo(AcademyEnemyAreaRuntime.PrepareSkillIndex));
            EnemyIntentPresentation intent = state.AcademyEnemyArea.PresentIntent(state, enemy, prepare);
            Assert.That(intent.AffectedCells, Is.EquivalentTo(new[] { new GridPosition(3, 1), centerTarget.Position, hero.Position }));
            CombatResolver.Resolve(state, prepare);
            CombatResolver.BeginTurn(state, hero.Id);
            CombatResolver.EndTurn(state, hero);
            CombatResolver.BeginTurn(state, enemy.Id);
            int heroHealth = hero.Health;
            int centerHealth = centerTarget.Health;
            CombatResolver.Resolve(state, state.AcademyEnemyArea.Choose(state, enemy, hero, CombatCommand.EndTurn(enemy.Id)));
            Assert.That(hero.Health, Is.LessThan(heroHealth));
            Assert.That(centerTarget.Health, Is.LessThan(centerHealth));
            Assert.That(hero.HasStatus(StatusType.Bound), Is.False);
            Assert.That(centerTarget.HasStatus(StatusType.Bound), Is.EqualTo(archetypeId == "raider"));
            if (archetypeId == "shieldguard")
            {
                Assert.That(hero.HasStatus(StatusType.Slow), Is.True);
                Assert.That(centerTarget.HasStatus(StatusType.Slow), Is.True);
            }
        }

        [Test]
        public void MenderArea_OnlyGrantsShieldToAlliesAlongTheSameStructure()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(6, 5));
            UnitState mender = new UnitState("mender", false, new GridPosition(2, 1));
            UnitState center = new UnitState("center", false, new GridPosition(3, 1));
            UnitState wing = new UnitState("wing", false, new GridPosition(4, 1));
            UnitState outside = new UnitState("outside", false, new GridPosition(5, 1));
            EnemyArchetypes.Get("barrier_mender").Apply(mender);
            GridMap map = new GridMap(7, 6);
            CombatState state = new CombatState(map, new[] { hero, mender, center, wing, outside });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            state.AttachAcademyFieldEnemy(new AcademyFieldEnemyRuntime());
            CombatResolver.BeginTurn(state, mender.Id);
            Assert.That(mender.Shield, Is.Zero, "离墙不会在范围意图回合额外触发借墙护盾。");
            Assert.That(state.AcademyEnemyArea.Choose(state, mender, hero, CombatCommand.EndTurn(mender.Id)).Type,
                Is.EqualTo(CombatCommandType.EndTurn));

            foreach (int x in new[] { 2, 3, 4 })
                map.SetTile(new GridPosition(x, 2), new TileState { Cover = CoverType.Heavy, Durability = TileState.HeavyDurability });
            CombatCommand prepare = state.AcademyEnemyArea.Choose(state, mender, hero, CombatCommand.EndTurn(mender.Id));
            Assert.That(prepare.SlotIndex, Is.EqualTo(AcademyEnemyAreaRuntime.PrepareSkillIndex));
            EnemyIntentPresentation intent = state.AcademyEnemyArea.PresentIntent(state, mender, prepare);
            Assert.That(intent.AffectedCells, Is.EquivalentTo(new[] { center.Position, wing.Position }));
            Assert.That(intent.ResultSummary, Does.Contain("护盾"));
            CombatResolver.Resolve(state, prepare);
            CombatResolver.BeginTurn(state, hero.Id);
            CombatResolver.EndTurn(state, hero);
            CombatResolver.BeginTurn(state, mender.Id);
            int centerShield = center.Shield;
            int wingShield = wing.Shield;
            int outsideShield = outside.Shield;
            CombatResolver.Resolve(state, state.AcademyEnemyArea.Choose(state, mender, hero, CombatCommand.EndTurn(mender.Id)));
            Assert.That(center.Shield, Is.EqualTo(centerShield + 2));
            Assert.That(wing.Shield, Is.EqualTo(wingShield + 2));
            Assert.That(outside.Shield, Is.EqualTo(outsideShield));
            Assert.That(hero.Shield, Is.Zero);
        }

        [Test]
        public void MenderDirectWard_RequiresTheSameConnectedStructure()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(6, 5));
            UnitState mender = new UnitState("mender", false, new GridPosition(2, 1));
            UnitState ally = new UnitState("ally", false, new GridPosition(3, 1));
            EnemyArchetypes.Get("barrier_mender").Apply(mender);
            GridMap map = new GridMap(7, 6);
            CombatState state = new CombatState(map, new[] { hero, mender, ally });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(state, mender.Id);
            CombatCommand ward = CombatCommand.UseSkill(mender.Id, 0, ally.Id);
            Assert.That(EnemyTactics.Choose(state, mender, hero).Type, Is.Not.EqualTo(CombatCommandType.UseSkill));
            Assert.Throws<System.InvalidOperationException>(() => CombatResolver.Resolve(state, ward));

            map.SetTile(new GridPosition(2, 2), new TileState { Cover = CoverType.Heavy, Durability = TileState.HeavyDurability });
            map.SetTile(new GridPosition(3, 2), new TileState { Cover = CoverType.Heavy, Durability = TileState.HeavyDurability });
            Assert.That(EnemyTactics.Choose(state, mender, hero).Type, Is.EqualTo(CombatCommandType.UseSkill));
            int shieldBefore = ally.Shield;
            CombatResolver.Resolve(state, ward);
            Assert.That(ally.Shield, Is.EqualTo(shieldBefore + 4));
        }

        [Test]
        public void MenderBorrow_TriggersOnCoverDestructionTwicePerBattleAndSurvivesClone()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 4));
            UnitState mender = new UnitState("mender", false, new GridPosition(2, 1));
            EnemyArchetypes.Get("barrier_mender").Apply(mender);
            GridMap map = new GridMap(6, 5);
            GridPosition[] covers = { new GridPosition(2, 2), new GridPosition(3, 2), new GridPosition(4, 2) };
            foreach (GridPosition cover in covers)
                map.SetTile(cover, new TileState { Cover = CoverType.Light, Durability = 4 });
            CombatState state = new CombatState(map, new[] { hero, mender });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachAcademyFieldEnemy(new AcademyFieldEnemyRuntime());
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());

            CombatEffectExecutor.Execute(state, hero.Id, CombatEffect.DamageObject(covers[0], 2));
            Assert.That(mender.Shield, Is.Zero);
            CombatEffectExecutor.Execute(state, hero.Id, CombatEffect.DamageObject(covers[0], 2));
            Assert.That(mender.Shield, Is.EqualTo(4));

            CombatState clone = state.Clone();
            CombatEffectExecutor.Execute(clone, hero.Id, CombatEffect.DamageObject(covers[1], 4));
            Assert.That(clone.GetUnit(mender.Id).Shield, Is.EqualTo(8));
            CombatEffectExecutor.Execute(clone, hero.Id, CombatEffect.DamageObject(covers[2], 4));
            Assert.That(clone.GetUnit(mender.Id).Shield, Is.EqualTo(8));
            Assert.That(mender.Shield, Is.EqualTo(4));
        }

        [Test]
        public void SnareLine_TelegraphsThreeCellsAndPlacesMarksOnlyOnEmptyCellsForTwoHeroTurns()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(4, 1));
            UnitState snare = new UnitState("snare", false, new GridPosition(1, 1));
            EnemyArchetypes.Get("stone_snare").Apply(snare);
            CombatState state = new CombatState(new GridMap(6, 4), new[] { hero, snare });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachAcademyFieldEnemy(new AcademyFieldEnemyRuntime());
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            CombatResolver.BeginTurn(state, snare.Id);
            Assert.That(state.Map.PositionsWith(tile => tile.IsBindingMark), Is.Empty,
                "刻印不应在学院敌人回合开始自动叠加到范围意图上。");

            CombatCommand prepare = state.AcademyEnemyArea.Choose(state, snare, hero, CombatCommand.EndTurn(snare.Id));
            Assert.That(prepare.SlotIndex, Is.EqualTo(AcademyEnemyAreaRuntime.PrepareSkillIndex));
            EnemyIntentPresentation intent = state.AcademyEnemyArea.PresentIntent(state, snare, prepare);
            Assert.That(intent.AttackRange, Is.EquivalentTo(new[] { new GridPosition(2, 1), new GridPosition(3, 1), hero.Position }));
            Assert.That(intent.AffectedCells, Is.EquivalentTo(new[] { new GridPosition(2, 1), new GridPosition(3, 1) }));
            CombatResolver.Resolve(state, prepare);
            CombatResolver.BeginTurn(state, hero.Id);
            CombatResolver.EndTurn(state, hero);
            CombatResolver.BeginTurn(state, snare.Id);
            CombatCommand resolve = state.AcademyEnemyArea.Choose(state, snare, hero, CombatCommand.EndTurn(snare.Id));
            Assert.That(resolve.SlotIndex, Is.EqualTo(AcademyEnemyAreaRuntime.ResolveSkillIndex));
            CombatResolver.Resolve(state, resolve);
            Assert.That(state.Map.GetTile(new GridPosition(2, 1)).IsBindingMark, Is.True);
            Assert.That(state.Map.GetTile(new GridPosition(3, 1)).IsBindingMark, Is.True);
            Assert.That(state.Map.GetTile(hero.Position).IsBindingMark, Is.False);
            Assert.That(state.Clone().Map.GetTile(new GridPosition(2, 1)).IsBindingMark, Is.True);

            CombatResolver.BeginTurn(state, hero.Id);
            CombatResolver.EndTurn(state, hero);
            Assert.That(state.Map.GetTile(new GridPosition(2, 1)).IsBindingMark, Is.True);
            CombatResolver.BeginTurn(state, hero.Id);
            CombatResolver.EndTurn(state, hero);
            Assert.That(state.Map.GetTile(new GridPosition(2, 1)).IsBindingMark, Is.False);
        }

        [Test]
        public void SnareDirectMark_IsChosenAsOneActionWhenSingleTargetAttackIsOutOfRange()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(5, 5));
            UnitState snare = new UnitState("snare", false, new GridPosition(1, 1));
            EnemyArchetypes.Get("stone_snare").Apply(snare);
            CombatState state = new CombatState(new GridMap(6, 6), new[] { hero, snare });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachAcademyFieldEnemy(new AcademyFieldEnemyRuntime());
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            CombatResolver.BeginTurn(state, snare.Id);

            EnemyTurnPlanBook plans = new EnemyTurnPlanBook();
            CombatCommand command = plans.GetExecutionCommand(state, snare, hero);
            Assert.That(command.SlotIndex, Is.EqualTo(AcademyFieldEnemyRuntime.BindingMarkSkillIndex));
            Assert.That(plans.GetPublicIntent(state, snare, hero).ActionName, Is.EqualTo("刻印"));
            CombatResolver.Resolve(state, command);
            Assert.That(state.Map.GetTile(command.Destination).IsBindingMark, Is.True);
            Assert.That(snare.ActionPoints, Is.Zero);
        }

        [Test]
        public void SnareLine_StopsActualPlacementAtHeavyCover()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(4, 1));
            UnitState snare = new UnitState("snare", false, new GridPosition(1, 1));
            EnemyArchetypes.Get("stone_snare").Apply(snare);
            GridMap map = new GridMap(6, 4);
            map.SetTile(new GridPosition(3, 1), new TileState { Cover = CoverType.Heavy, Durability = 12 });
            CombatState state = new CombatState(map, new[] { hero, snare });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            CombatResolver.BeginTurn(state, snare.Id);
            CombatCommand prepare = state.AcademyEnemyArea.Choose(state, snare, hero, CombatCommand.EndTurn(snare.Id));
            Assert.That(prepare.SlotIndex, Is.EqualTo(AcademyEnemyAreaRuntime.PrepareSkillIndex));
            Assert.That(state.AcademyEnemyArea.PresentIntent(state, snare, prepare).AffectedCells,
                Is.EqualTo(new[] { new GridPosition(2, 1) }));
        }

        [Test]
        public void LanternSweep_UsesOnePublicActionAndClearsShieldBeforeArcaneDamage()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(5, 1));
            UnitState lantern = new UnitState("lantern", false, new GridPosition(1, 1));
            EnemyArchetypes.Get("lantern_revealer").Apply(lantern);
            CombatState state = new CombatState(new GridMap(6, 4), new[] { hero, lantern });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachAcademyFieldEnemy(new AcademyFieldEnemyRuntime());
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            CombatEffectExecutor.Execute(state, hero.Id, CombatEffect.RestoreShield(hero.Id, 4, "test-shield"));
            int healthBefore = hero.Health;
            CombatResolver.BeginTurn(state, lantern.Id);
            Assert.That(hero.Shield, Is.EqualTo(4));
            Assert.That(hero.Health, Is.EqualTo(healthBefore));

            EnemyTurnPlanBook plans = new EnemyTurnPlanBook();
            CombatCommand command = plans.GetExecutionCommand(state, lantern, hero);
            Assert.That(command.SlotIndex, Is.EqualTo(AcademyFieldEnemyRuntime.LanternSweepSkillIndex));
            EnemyIntentPresentation intent = plans.GetPublicIntent(state, lantern, hero);
            Assert.That(intent.ActionName, Is.EqualTo("转灯"));
            Assert.That(intent.AffectedCells, Does.Contain(hero.Position));
            CombatResolver.Resolve(state, command);
            Assert.That(hero.Shield, Is.Zero);
            Assert.That(hero.Health, Is.LessThan(healthBefore));
            Assert.That(hero.HasStatus(StatusType.Marked), Is.True);
            Assert.That(lantern.ActionPoints, Is.Zero);
        }

        [Test]
        public void LanternSweep_SmokeCutsPreviewDamageAndPersistentLightAtTheSameCell()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(5, 1));
            UnitState lantern = new UnitState("lantern", false, new GridPosition(1, 1));
            EnemyArchetypes.Get("lantern_revealer").Apply(lantern);
            CombatState state = new CombatState(new GridMap(6, 4), new[] { hero, lantern });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachAcademyFieldEnemy(new AcademyFieldEnemyRuntime());
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            TileState smoke = state.Map.GetTile(new GridPosition(3, 1)).Clone();
            smoke.SmokeExpiresAt = 99;
            state.Map.SetTile(new GridPosition(3, 1), smoke);
            CombatEffectExecutor.Execute(state, hero.Id, CombatEffect.RestoreShield(hero.Id, 4, "test-shield"));
            CombatResolver.BeginTurn(state, lantern.Id);

            var plans = new EnemyTurnPlanBook();
            CombatCommand command = plans.GetExecutionCommand(state, lantern, hero);
            EnemyIntentPresentation intent = plans.GetPublicIntent(state, lantern, hero);
            Assert.That(command.SlotIndex, Is.EqualTo(AcademyFieldEnemyRuntime.LanternSweepSkillIndex));
            Assert.That(intent.AffectedCells, Does.Contain(new GridPosition(2, 1)));
            Assert.That(intent.AffectedCells.Contains(new GridPosition(3, 1)), Is.False);
            Assert.That(intent.AffectedCells.Contains(hero.Position), Is.False);
            int healthBefore = hero.Health;
            CombatResolver.Resolve(state, command);
            Assert.That(hero.Shield, Is.EqualTo(4));
            Assert.That(hero.Health, Is.EqualTo(healthBefore));
            Assert.That(hero.HasStatus(StatusType.Marked), Is.False);
            FieldLightLaneState lane = state.Environment.LightLanes.Single();
            Assert.That(state.Environment.LitCells(state.Map, lane, state.CurrentTime),
                Is.EqualTo(new[] { new GridPosition(2, 1) }));
            Assert.That(state.Environment.LitCells(state.Map, lane, 99).Count, Is.EqualTo(4),
                "烟尘失效后光带可照亮完整路径");
        }

        [Test]
        public void DoubleLanternSweep_FreezesBothParallelLanesAcrossHeroTurn()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(3, 2));
            UnitState lantern = new UnitState("lantern", false, new GridPosition(1, 1));
            UnitState centerLane = new UnitState("center", false, new GridPosition(3, 1));
            EnemyArchetypes.Get("lantern_revealer").Apply(lantern);
            CombatState state = new CombatState(new GridMap(6, 5), new[] { hero, lantern, centerLane });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachAcademyFieldEnemy(new AcademyFieldEnemyRuntime());
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            CombatEffectExecutor.Execute(state, hero.Id, CombatEffect.RestoreShield(hero.Id, 4, "test-shield"));
            CombatResolver.BeginTurn(state, lantern.Id);

            CombatCommand prepare = state.AcademyEnemyArea.Choose(state, lantern, hero, CombatCommand.EndTurn(lantern.Id));
            Assert.That(prepare.SlotIndex, Is.EqualTo(AcademyEnemyAreaRuntime.PrepareSkillIndex));
            EnemyIntentPresentation intent = state.AcademyEnemyArea.PresentIntent(state, lantern, prepare);
            Assert.That(intent.AttackRange.Count, Is.EqualTo(6));
            Assert.That(intent.AffectedCells, Does.Contain(hero.Position));
            Assert.That(intent.AffectedCells, Does.Contain(centerLane.Position));
            int healthBefore = hero.Health;
            CombatResolver.Resolve(state, prepare);
            Assert.That(hero.Shield, Is.EqualTo(4));
            CombatState saved = state.Clone();
            Assert.That(saved.AcademyEnemyArea.PresentIntent(saved, saved.GetUnit(lantern.Id), prepare).AffectedCells,
                Is.EquivalentTo(intent.AffectedCells));
            CombatResolver.BeginTurn(state, hero.Id);
            CombatResolver.EndTurn(state, hero);
            CombatResolver.BeginTurn(state, lantern.Id);
            CombatCommand resolve = state.AcademyEnemyArea.Choose(state, lantern, hero, CombatCommand.EndTurn(lantern.Id));
            Assert.That(resolve.SlotIndex, Is.EqualTo(AcademyEnemyAreaRuntime.ResolveSkillIndex));
            CombatResolver.Resolve(state, resolve);
            Assert.That(hero.Shield, Is.Zero);
            Assert.That(hero.Health, Is.LessThan(healthBefore));
            Assert.That(hero.HasStatus(StatusType.Marked), Is.True);
            Assert.That(centerLane.HasStatus(StatusType.Marked), Is.True);
            Assert.That(state.Environment.LightLanes.Count, Is.EqualTo(2));
        }
    }
}
