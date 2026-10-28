using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Presentation;
using OCC.Combat.Roguelite;

namespace OCC.Combat.Tests
{
    public sealed class LampkeeperGiftTests
    {
        public static RogueliteMapRun WithGift(string id)
        {
            for (int seed = 1; seed < 300; seed++)
            {
                var run = RogueliteMapRun.CreateSubsequentAcademyRun(seed);
                if (run.OpeningGift.CandidateIds.Contains(id)) return run;
            }
            throw new InvalidOperationException("No seed for gift " + id);
        }
        [Test]
        public void CandidatesAreSeededUniqueAndBlockDepartureUntilResolved()
        {
            var a = RogueliteMapRun.CreateSubsequentAcademyRun(303);
            var b = RogueliteMapRun.CreateSubsequentAcademyRun(303);
            Assert.That(a.OpeningGift.CandidateIds, Is.EqualTo(b.OpeningGift.CandidateIds));
            Assert.That(a.OpeningGift.CandidateIds.Distinct().Count(), Is.EqualTo(3));
            Assert.Throws<InvalidOperationException>(() => a.ConfirmAcademyDeparture());
            Assert.That(a.IsNodeAvailable("dorm_drill"), Is.False);
            var restored = RoundTrip(a);
            Assert.That(restored.OpeningGift.CandidateIds, Is.EqualTo(a.OpeningGift.CandidateIds));
            Assert.That(restored.OpeningGiftPending, Is.True);
            Assert.That(restored.StageTime, Is.Zero);
            Assert.That(restored.CompletedNodes, Is.Empty);
        }
        [Test]
        public void PreGiftArchiveAndFixedExperienceNeverReceiveRetroactiveGifts()
        {
            var run = RogueliteMapRun.CreateSubsequentAcademyRun(303, includeOpeningGift: false);
            string legacy = Rogue11Serializer.Serialize(run.RogueRunState);
            legacy = string.Join("|", legacy.Split('|').Take(58));
            var old = RogueliteMapRun.FromRogue11(Rogue11Serializer.Deserialize(legacy));
            Assert.That(old.OpeningGift, Is.Null);
            old.ConfirmAcademyDeparture();
            Assert.That(RoundTrip(RogueliteMapRun.CreateFirstRunV1(303)).OpeningGift, Is.Null);
        }
        [TestCase("LK-01")][TestCase("LK-02")][TestCase("LK-03")][TestCase("LK-04")][TestCase("LK-06")]
        public void OrdinaryGiftIsSingleClaimAndSurvivesRoundTrip(string id)
        {
            var run = WithGift(id); int gold = run.RogueRunState.Gold;
            run.ChooseOpeningGift(id);
            Assert.That(run.OpeningGift.Resolved, Is.True);
            Assert.That(run.RogueRunState.Gold, Is.EqualTo(gold + (id == "LK-06" ? 12 : 0)));
            Assert.Throws<InvalidOperationException>(() => run.ChooseOpeningGift(id));
            var restored = RoundTrip(run);
            Assert.That(restored.OpeningGift.SelectedId, Is.EqualTo(id));
            restored.ConfirmAcademyDeparture();
            Assert.That(restored.RogueRunState.MasteredSpellIds.Count, Is.EqualTo(4));
            Assert.That(restored.RogueRunState.EquipmentInstances.Count, Is.EqualTo(2));
        }
        [Test]
        public void BlankAnnotationRestoresInnerChoiceLearnsWithoutAutoEquipping()
        {
            var run = WithGift("LK-05");
            Assert.That(run.OpeningGift.SpellCandidateIds.All(id => FireSpellCatalog.IsWeaponCompatible(FireSpellCatalog.Get(id), CombatCatalog.Hammer)), Is.True);
            run.ChooseOpeningGift("LK-05");
            var restored = RoundTrip(run);
            Assert.That(restored.OpeningGiftPending, Is.True);
            Assert.That(restored.OpeningGift.SpellCandidateIds, Is.EqualTo(run.OpeningGift.SpellCandidateIds));
            Assert.Throws<InvalidOperationException>(() => restored.ConfirmAcademyDeparture());
            string spell = restored.OpeningGift.SpellCandidateIds[0];
            restored.ChooseOpeningGiftSpell(spell);
            restored = RoundTrip(restored);
            Assert.That(restored.RogueRunState.MasteredSpellIds, Does.Contain(spell));
            Assert.That(restored.RogueRunState.EquippedSpellIds, Does.Not.Contain(spell));
            Assert.That(restored.OpeningGift.SelectedSpellId, Is.EqualTo(spell));
            Assert.That(restored.AssignRogueSpell(spell,4), Is.True);
            restored = RoundTrip(restored);
            Assert.That(restored.RogueRunState.EquippedSpellIds[4], Is.EqualTo(spell));
            Assert.Throws<InvalidOperationException>(() => restored.ChooseOpeningGiftSpell(spell));
        }
        [Test]
        public void FailedGiftSaveRetriesWithoutPayingTwiceOrOverwritingOriginal()
        {
            var store = new Store(); var gateway = new RogueliteSaveGateway(store);
            var saves = new RogueliteMapSaveCoordinator(gateway);
            var run = WithGift("LK-06"); Assert.That(saves.Save(run), Is.True);
            string original = store.Data[RogueliteSaveGateway.MapRunKey];
            run.ChooseOpeningGift("LK-06"); store.Fail = true;
            Assert.That(saves.Save(run), Is.False);
            Assert.That(store.Data[RogueliteSaveGateway.MapRunKey], Is.EqualTo(original));
            Assert.Throws<InvalidOperationException>(() => run.ChooseOpeningGift("LK-06"));
            store.Fail = false; Assert.That(saves.Save(run), Is.True);
            var loaded = saves.TryStart(true, FireRogueliteStarterCatalog.Universal, 0).Run;
            Assert.That(loaded.RogueRunState.Gold, Is.EqualTo(20));
            Assert.That(loaded.OpeningGift.Resolved, Is.True);
        }
        [Test]
        public void InvalidGiftVersionOrSelectionIsRejected()
        {
            Assert.Throws<InvalidOperationException>(() => LampkeeperGiftState.Deserialize("2~LK-01,LK-02,LK-03~~~"));
            Assert.Throws<InvalidOperationException>(() => LampkeeperGiftState.Deserialize("1~LK-01,LK-02,LK-03~~LK-06~"));
        }
        [Test]
        public void WalkingGiftAccumulatesActualStepsOncePerOwnTurnAndClonesIndependently()
        {
            var state = Battle("LK-02");
            var move = new CombatCommandExecutionService();
            Assert.That(move.Execute(state, state.RogueSpells.FireBattle, CombatCommand.Move("hero", new GridPosition(3,2))).Accepted, Is.True);
            Assert.That(state.GetUnit("hero").Shield, Is.Zero);
            var preview = state.Clone();
            Assert.That(move.Execute(preview, preview.RogueSpells.FireBattle, CombatCommand.Move("hero", new GridPosition(5,2))).Accepted, Is.True);
            Assert.That(preview.GetUnit("hero").Shield, Is.EqualTo(3));
            Assert.That(state.GetUnit("hero").Shield, Is.Zero);
            Assert.That(move.Execute(state, state.RogueSpells.FireBattle, CombatCommand.Move("hero", new GridPosition(5,2))).Accepted, Is.True);
            state.Lampkeeper.AfterActiveMove(state,"hero",3);
            Assert.That(state.GetUnit("hero").Shield, Is.EqualTo(3));
            CombatResolver.EndTurn(state,state.GetUnit("hero")); CombatResolver.BeginTurn(state,"hero");
            state.Lampkeeper.AfterActiveMove(state,"hero",3);
            Assert.That(state.GetUnit("hero").Shield, Is.EqualTo(3));
        }
        [Test]
        public void WardStartsWithSixRetainsRemainderOnFirstTurnAndNeverReawardsOnLaterTurns()
        {
            var state = Battle("LK-04", begin:false);
            Assert.That(state.GetUnit("hero").Shield, Is.EqualTo(6));
            Assert.That(state.Clone().GetUnit("hero").Shield, Is.EqualTo(6));
            CombatResolver.BeginTurn(state,"hero");
            Assert.That(state.GetUnit("hero").Shield, Is.EqualTo(6));
            CombatResolver.EndTurn(state,state.GetUnit("hero")); CombatResolver.BeginTurn(state,"hero");
            Assert.That(state.GetUnit("hero").Shield, Is.Zero);
            Assert.That(state.Lampkeeper.TotalShieldGained, Is.EqualTo(6));
        }
        [Test]
        public void WallGiftOnlyRewardsDirectDestructionOncePerPublicRoundAndRespectsManaCap()
        {
            var state = Battle("LK-03");
            CombatEffectExecutor.Execute(state,"hero",CombatEffect.SpendMana(5));
            state.Map.SetTile(new GridPosition(3,2),new TileState{Cover=CoverType.Light,Durability=2});
            CombatEffectExecutor.Execute(state,"hero",CombatEffect.DamageObject(new GridPosition(3,2),2));
            Assert.That(state.GetUnit("hero").Mana, Is.EqualTo(9));
            state.Map.SetTile(new GridPosition(4,2),new TileState{Cover=CoverType.Light,Durability=2});
            CombatEffectExecutor.Execute(state,"hero",CombatEffect.DamageObject(new GridPosition(4,2),2));
            Assert.That(state.GetUnit("hero").Mana, Is.EqualTo(9));
            state.Lampkeeper.AfterDirectObjectDamage(state,"enemy",1,0);
            Assert.That(state.GetUnit("hero").Mana, Is.EqualTo(9));
        }
        [TestCase(false)][TestCase(true)]
        public void WickUsesRealPersonalSpellNewFiregroundAndNotRefresh(bool existing)
        {
            var state = Battle("LK-01");
            var loadout = RogueSpellLoadout.CreateStarter(); loadout.Learn("F-P-M03"); loadout.Equip(4,"F-P-M03");
            state.AttachRogueSpellRuntime(new RogueSpellCombatRuntime(state,loadout.CreateCombatSnapshot()));
            if(existing)state.RogueSpells.FireBattle.CreateOrRefreshFireground(new GridPosition(2,2),8,5,"F-P-M03","hero");
            int cost = state.RogueSpells.EffectiveManaCost(state.RogueSpells.DefinitionAtSlot(4),"hero");
            var result = new CombatCommandExecutionService().Execute(state,state.RogueSpells.FireBattle,
                CombatCommand.Restore(CombatCommandType.UseSkill,"hero",new GridPosition(5,2),CardinalDirection.East,string.Empty,4));
            Assert.That(result.Accepted, Is.True,result.RejectionReason);
            Assert.That(state.Lampkeeper.TotalManaGained, Is.EqualTo(existing?0:1));
            Assert.That(state.GetUnit("hero").Mana, Is.EqualTo(12-cost+(existing?0:1)));
        }
        [Test]
        public void MovementGiftCountsPersonalDashButNotSwapOrForcedMovement()
        {
            var state=Battle("LK-02");var loadout=RogueSpellLoadout.CreateStarter();loadout.Learn("F-P-M03");loadout.Equip(4,"F-P-M03");
            state.AttachRogueSpellRuntime(new RogueSpellCombatRuntime(state,loadout.CreateCombatSnapshot()));
            var result=new CombatCommandExecutionService().Execute(state,state.RogueSpells.FireBattle,
                CombatCommand.Restore(CombatCommandType.UseSkill,"hero",new GridPosition(5,2),CardinalDirection.East,string.Empty,4));
            Assert.That(result.Accepted,Is.True,result.RejectionReason);
            Assert.That(state.Lampkeeper.TotalShieldGained,Is.EqualTo(3));
        }
        public static CombatState Battle(string gift,bool begin=true)
        {
            var hero=new UnitState("hero",true,new GridPosition(2,2));
            hero.ConfigureMana(12,12);
            hero.Equip(CombatCatalog.Hammer,CombatCatalog.Shield,CombatCatalog.FireBolt,CombatCatalog.FrostBind);
            var state=new CombatState(new GridMap(12,5),new[]{hero,new UnitState("enemy",false,new GridPosition(10,2))},new CombatObjective[]{new EliminationObjective()});
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachRogueSpellRuntime(new RogueSpellCombatRuntime(state,RogueSpellLoadout.CreateStarter().CreateCombatSnapshot()));
            state.AttachLampkeeper(new LampkeeperBattleRuntime(gift));
            if(begin)CombatResolver.BeginTurn(state,"hero");return state;
        }
        private static RogueliteMapRun RoundTrip(RogueliteMapRun run)
        {
            var gateway=new RogueliteSaveGateway(new Store());
            Assert.That(gateway.SaveMapRun(run),Is.True,gateway.LastError);
            Assert.That(gateway.TryLoadMapRun(out var restored),Is.True,gateway.LastError);return restored;
        }
        private sealed class Store:IRogueliteSaveStore
        {
            public readonly Dictionary<string,string> Data=new Dictionary<string,string>();public bool Fail;
            public bool HasKey(string key)=>Data.ContainsKey(key);
            public string GetString(string key,string fallback="")=>Data.TryGetValue(key,out var value)?value:fallback;
            public void SetString(string key,string value){if(Fail)throw new InvalidOperationException("write failed");Data[key]=value;}
            public void DeleteKey(string key)=>Data.Remove(key);public void Flush(){}
        }
    }
}
