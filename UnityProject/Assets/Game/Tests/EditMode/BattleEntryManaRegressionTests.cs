using System;
using NUnit.Framework;
using OCC.Combat.Presentation;
using OCC.Combat.Roguelite;

namespace OCC.Combat.Tests
{
    public sealed class BattleEntryManaRegressionTests
    {
        [TestCase(false,8)][TestCase(true,10)]
        public void ShieldSpecialization_UsesTheLockedBattleBalance(bool legacy, int expectedShield)
        {
            var hero=new UnitState("hero",true,new GridPosition(0,0));
            var enemy=new UnitState("enemy",false,new GridPosition(2,0));
            var state=new CombatState(new GridMap(3,1),new[]{hero,enemy});state.ConfigureRuleset(CombatRuleset.Roguelite);
            var runtime=new RogueSpellCombatRuntime(state,RogueSpellLoadout.CreateStarter().CreateCombatSnapshot(),
                specializedSpellId:"BASE-AETHER-SHIELD",legacyBasics:legacy);
            state.AttachRogueSpellRuntime(runtime);CombatResolver.BeginTurn(state,"hero");
            Assert.That(runtime.ExecuteSlot(2,CombatCommand.UseSkill("hero",2,"hero")).Accepted,Is.True);
            Assert.That(hero.Shield,Is.EqualTo(expectedShield));
        }
        [Test]
        public void Recovery_ClampsToTheCurrentManaCapacity()
        {
            var hero=new UnitState("hero",true,new GridPosition(0,0));hero.ConfigureMana(14,13);
            var state=new CombatState(new GridMap(2,1),new[]{hero,new UnitState("enemy",false,new GridPosition(1,0))});
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            var runtime=new RogueSpellCombatRuntime(state,RogueSpellLoadout.CreateStarter().CreateCombatSnapshot());
            state.AttachRogueSpellRuntime(runtime);CombatResolver.BeginTurn(state,"hero");
            Assert.That(runtime.ExecuteSlot(3,CombatCommand.UseSkill("hero",3,"hero")).Accepted,Is.True);
            Assert.That(hero.Mana,Is.EqualTo(14));
        }
        [Test]
        public void UnknownBalanceHeader_IsRejectedBeforeReplayingActions()
        {
            var fields=CombatJournalEntry.BalanceVersion().Encode().Split(';');
            fields[9]=Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("future-version"));
            Assert.Throws<InvalidOperationException>(()=>CombatJournalEntry.Decode(string.Join(";",fields)));
        }
        private sealed class Store:IRogueliteSaveStore
        {
            private readonly System.Collections.Generic.Dictionary<string,string> values=new System.Collections.Generic.Dictionary<string,string>();
            public bool HasKey(string k)=>values.ContainsKey(k);
            public string GetString(string k,string fallback="")=>values.TryGetValue(k,out var v)?v:fallback;
            public void SetString(string k,string v)=>values[k]=v;
            public void DeleteKey(string k)=>values.Remove(k);public void Flush(){}
        }

        [TestCase(true, 10, 0)][TestCase(false, 8, 10)]
        public void SavedBattle_ReplaysItsLockedBasicValuesAndOriginalMana(bool legacy, int shield, int mana)
        {
            var run=RogueliteMapRun.CreateSubsequentAcademyRun(303,includeOpeningGift:false);
            var initialGateway=new RogueliteSaveGateway(new Store());
            Assert.That(initialGateway.SaveMapRun(run),Is.True);
            Assert.That(initialGateway.TryLoadMapRun(out run),Is.True);
            run.RogueRunState.CurrentMana=2;run=RogueliteMapRun.FromRogue11(run.RogueRunState);
            run.ConfirmAcademyDeparture();run.SelectNode("dorm_drill");
            run.BeginCombatJournal();
            if(legacy)run.RogueRunState.CombatJournalRows.Clear(); // Earlier saves have no balance header.
            var builder=new CombatSceneSessionBuilder();
            var state=builder.Build(run,null,Array.Empty<CombatSceneMarker>()).State;
            CombatResolver.AdvanceToNextTurn(state);
            var command=CombatCommand.UseSkill("hero",2,"hero");
            var result=new CombatCommandExecutionService().Execute(state,state.RogueSpells.FireBattle,command,true);
            Assert.That(result.Accepted,Is.True,result.RejectionReason);
            run.AppendCombatJournal(CombatJournalEntry.Accepted(command));
            var gateway=new RogueliteSaveGateway(new Store());
            Assert.That(gateway.SaveMapRun(run),Is.True,gateway.LastError);
            Assert.That(gateway.TryLoadMapRun(out var restoredRun),Is.True,gateway.LastError);
            Assert.That(restoredRun.UsesLegacyCombatBalance,Is.EqualTo(legacy));
            var restored=builder.Build(restoredRun,null,Array.Empty<CombatSceneMarker>()).State;
            CombatResolver.AdvanceToNextTurn(restored);
            CombatJournalReplayer.ReplayAfterActivation(restored,restoredRun.CombatJournalRows);
            Assert.That(restored.GetUnit("hero").Shield,Is.EqualTo(state.GetUnit("hero").Shield));
            Assert.That(restored.GetUnit("hero").Shield,Is.EqualTo(shield));
            Assert.That(restored.GetUnit("hero").Mana,Is.EqualTo(mana));
            Assert.That(restored.GetUnit("hero").Mana,Is.EqualTo(state.GetUnit("hero").Mana));
            Assert.That(restored.Clone().RogueSpells.DefinitionAtSlot(2).Rules,Does.Contain("grant_shield:"+(legacy?8:6)));
            restoredRun.RestartCombatJournalPreservingBalance();
            Assert.That(restoredRun.UsesLegacyCombatBalance,Is.EqualTo(legacy));
            Assert.That(restoredRun.CombatJournalRows.Count,Is.EqualTo(legacy?0:1));
        }
        [TestCase(0)][TestCase(2)][TestCase(11)]
        public void FreshBattle_RestoresManaButDoesNotHealPersistentHealth(int previousMana)
        {
            var run=RogueliteMapRun.CreateSubsequentAcademyRun(303,includeOpeningGift:false);
            run.RogueRunState.CurrentMana=previousMana;run.RogueRunState.CurrentHealth=31;
            run.ConfirmAcademyDeparture();run.SelectNode("dorm_drill");
            var build=new CombatSceneSessionBuilder().Build(run,null,Array.Empty<CombatSceneMarker>());
            var hero=build.State.GetUnit("hero");
            Assert.That(hero.Mana,Is.EqualTo(hero.MaxMana));
            Assert.That(hero.Mana,Is.EqualTo(12));Assert.That(hero.Health,Is.EqualTo(31));
            Assert.That(run.RogueRunState.CurrentMana,Is.EqualTo(previousMana),"Building a preview must not write the persistent round");
        }
    }
}
