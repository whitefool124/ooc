using System;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Roguelite;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class AcademyMealCompletionTests
    {
        private static RogueliteMapRun Offer(string id)
        {
            for(int seed=1;seed<128;seed++)
            {
                var run=RogueliteMapRun.CreateSubsequentAcademyRun(seed);run.ConfirmAcademyDeparture();run.PreviewUiCaptureNode("layer_medical");
                if(!run.CurrentMedicalService.MealCandidateIds.Contains(id))continue;
                run.RogueRunState.Gold=50;run.RogueRunState.StageContribution=20;run.RogueRunState.AcademyFoodCount=20;
                return run;
            }
            throw new Exception("Meal never offered: "+id);
        }
        private static CombatState Build(RogueliteMapRun run)
        {
            var hero=new UnitState("hero",true,new GridPosition(0,0));
            var state=new CombatState(new GridMap(3,3),new[]{hero});
            state.AttachRogueEquipmentRuntime(RogueEquipmentRuntime.FromDto(run.RogueRunState));run.ApplyBuild(hero);return state;
        }
        [TestCase("MEAL-POWER")][TestCase("MEAL-AETHER")][TestCase("MEAL-GUARD")][TestCase("MEAL-MOVE")]
        [TestCase("MEAL-SPELL")][TestCase("MEAL-VITAL")][TestCase("MEAL-HUNTER")][TestCase("MEAL-CLEAR")]
        public void LegalOffer_PaysExactCurrency_PersistsAndCannotStack(string id)
        {
            var run=Offer(id);var definition=AcademyMealCatalog.Get(id);int gold=run.Gold,contribution=run.StageContribution,food=run.AcademyFoodCount;
            Assert.That(Resources.Load<Sprite>(definition.IconPath),Is.Not.Null);
            run.ChooseFirstRunMeal(id);
            Assert.That(run.Gold,Is.EqualTo(gold-(definition.Currency=="gold"?definition.Cost:0)));
            Assert.That(run.StageContribution,Is.EqualTo(contribution-(definition.Currency=="contribution"?definition.Cost:0)));
            Assert.That(run.AcademyFoodCount,Is.EqualTo(food-(definition.Currency=="food"?definition.Cost:0)));
            Assert.That(run.ActiveMealId,Is.EqualTo(id));Assert.That(run.MealBattlesRemaining,Is.EqualTo(3));
            var saved=Rogue11Serializer.Deserialize(Rogue11Serializer.Serialize(run.RogueRunState));
            Assert.That(saved.ActiveMealId,Is.EqualTo(id));Assert.That(saved.MealBattlesRemaining,Is.EqualTo(3));
            Assert.Throws<InvalidOperationException>(()=>run.ChooseFirstRunMeal(id));
        }
        [Test] public void SeededOptions_AreThreeDistinctAndCoverAllEight_AndLegacyRowsStillRead()
        {
            var seen=Enumerable.Range(1,40).SelectMany(seed=>AcademyMealCatalog.Candidates(seed,"layer_medical")).Distinct().ToArray();Assert.That(seen.Length,Is.EqualTo(8));
            Assert.That(AcademyMealCatalog.Candidates(42,"layer_medical"),Is.EqualTo(AcademyMealCatalog.Candidates(42,"layer_medical")));
            var run=Offer("MEAL-POWER");Assert.That(run.CurrentMedicalService.MealCandidateIds.Distinct().Count(),Is.EqualTo(3));
            var snapshot=Rogue11Serializer.Serialize(run.RogueRunState);var legacy=string.Join("|",snapshot.Split('|').Take(56));
            Assert.That(Rogue11Serializer.Deserialize(legacy).MealBattlesRemaining,Is.Zero);
            RogueliteMapRun.ValidateLayerServiceRows(new[]{"layer_medical~M~0~0~0~"});
        }
        [Test] public void Meals_ApplyRealBattleBonusesAndOneShotLimits()
        {
            CombatState Get(string id){var r=Offer(id);r.RogueRunState.CurrentHealth=30;r.ChooseFirstRunMeal(id);return Build(r);}
            var power=Get("MEAL-POWER");Assert.That(power.RogueEquipment.ForgeWeaponDamageBonus,Is.EqualTo(2));
            var mana=Get("MEAL-AETHER").GetUnit("hero");Assert.That(mana.MaxMana,Is.EqualTo(14));Assert.That(mana.Mana,Is.EqualTo(14));
            Assert.That(Get("MEAL-GUARD").GetUnit("hero").Shield,Is.EqualTo(6));
            var bread=Get("MEAL-VITAL").GetUnit("hero");Assert.That(bread.MaxHealth,Is.EqualTo(UnitState.HeroBaseHealth+4));Assert.That(bread.Health,Is.EqualTo(34));
            var movement=Get("MEAL-MOVE");Assert.That(movement.RogueEquipment.MovementBonus("hero"),Is.EqualTo(1));
            movement.RogueEquipment.AfterMove("hero");Assert.That(movement.RogueEquipment.MovementBonus("hero"),Is.Zero);
            movement.RogueEquipment.OnTurnStart(movement,"hero");Assert.That(movement.RogueEquipment.MovementBonus("hero"),Is.EqualTo(1));
            var hunt=Get("MEAL-HUNTER");Assert.That(hunt.RogueEquipment.ForgeWeaponDamageBonus,Is.EqualTo(4));
            hunt.RogueEquipment.AfterWeaponHit(hunt,"hero");Assert.That(hunt.RogueEquipment.ForgeWeaponDamageBonus,Is.Zero);
            var tea=Get("MEAL-SPELL");Assert.That(tea.RogueEquipment.MealSpellDiscountAvailable,Is.True);
            tea.RogueEquipment.ConsumeMealSpellDiscount();Assert.That(tea.RogueEquipment.MealSpellDiscountAvailable,Is.False);
            var clear=Get("MEAL-CLEAR").GetUnit("hero");clear.ApplyStatus(StatusType.Slow,3);Assert.That(clear.StatusDuration(StatusType.Slow),Is.EqualTo(2));
            clear.ApplyStatus(StatusType.Burning,3);Assert.That(clear.StatusDuration(StatusType.Burning),Is.EqualTo(3));
        }
        [TestCase("MEAL-POWER",2)][TestCase("MEAL-HUNTER",4)]
        public void WeaponMeals_ApplyToWeaponSpellPackets_AndConsumeOnlyTheFirstHitBonus(string id,int bonus)
        {
            var run=Offer(id);run.ChooseFirstRunMeal(id);
            var hero=new UnitState("hero",true,new GridPosition(0,0));hero.Equip(CombatCatalog.Hammer,hero.OffHand,hero.SkillOne,hero.SkillTwo);
            var enemy=new UnitState("enemy",false,new GridPosition(1,0));enemy.ConfigureVitality(100);enemy.ApplyStatus(StatusType.Burning,2);
            var state=new CombatState(new GridMap(3,3),new[]{hero,enemy});state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachRogueEquipmentRuntime(RogueEquipmentRuntime.FromDto(run.RogueRunState));run.ApplyBuild(hero);CombatResolver.BeginTurn(state,"hero");
            var spell=FireSpellCatalog.Get("F-P-M16");var battle=new FireBattleState(state);
            var result=FireSpellEngine.Execute(battle,"hero",spell,FireSpellTarget.Unit("enemy"));
            Assert.That(result.Preview.Failures,Is.Empty);
            Assert.That(result.Steps.First(step=>step.Kind==FireRuleKind.WeaponDamage).Requested,Is.EqualTo(spell.Rules.First(rule=>rule.Kind==FireRuleKind.WeaponDamage).Amount+bonus));
            Assert.That(state.RogueEquipment.MealWeaponDamageBonus,Is.EqualTo(id=="MEAL-HUNTER"?0:2));
        }

        [Test] public void Tea_DiscountsTheActualFirstCast_ThenRestoresItsNormalCost()
        {
            var run=Offer("MEAL-SPELL");run.ChooseFirstRunMeal("MEAL-SPELL");var state=Build(run);state.ConfigureRuleset(CombatRuleset.Roguelite);
            var runtime=new RogueSpellCombatRuntime(state,RogueSpellLoadout.CreateStarter().CreateCombatSnapshot());state.AttachRogueSpellRuntime(runtime);
            CombatResolver.BeginTurn(state,"hero");var definition=runtime.DefinitionAtSlot(2);var hero=state.GetUnit("hero");int before=hero.Mana;
            Assert.That(runtime.EffectiveManaCost(definition),Is.EqualTo(Math.Max(0,definition.ManaCost-1)));
            Assert.That(runtime.ExecuteSlot(2,CombatCommand.UseSkill("hero",2,"hero")).Accepted,Is.True);
            Assert.That(hero.Mana,Is.EqualTo(before-Math.Max(0,definition.ManaCost-1)));
            Assert.That(runtime.EffectiveManaCost(definition),Is.EqualTo(definition.ManaCost));
        }

        [Test] public void ThreeCompletedBattles_ExpireTheMeal_AndNodeRevisitKeepsOffers()
        {
            var run=Offer("MEAL-POWER");var offers=run.CurrentMedicalService.MealCandidateIds.ToArray();run.ChooseFirstRunMeal("MEAL-POWER");
            run.PreviewUiCaptureNode("layer_workshop");
            Assert.That(run.RogueRunState.LayerServiceRows.Single(row=>row.StartsWith("layer_medical~")),Does.EndWith(string.Join(",",offers)));
            var restored=RogueliteMapRun.FromRogue11(Rogue11Serializer.Deserialize(Rogue11Serializer.Serialize(run.RogueRunState)));
            restored.PreviewUiCaptureNode("layer_medical");Assert.That(restored.CurrentMedicalService.MealCandidateIds,Is.EqualTo(offers));
            foreach(string node in new[]{"A01","A02","A03"})
            {
                var combatNode=run.MapNodes.First(n=>n.IsCombat&&!run.CompletedNodes.Contains(n.Id)&&n.Type!=RogueliteMapNodeType.Finale);
                run.PreviewUiCaptureNode(combatNode.Id);run.CompleteCurrentCombat();
            }
            Assert.That(run.MealBattlesRemaining,Is.Zero);Assert.That(run.ActiveMealId,Is.Empty);
        }
    }
}
