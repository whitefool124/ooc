using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using OCC.Combat.Presentation;
using OCC.Combat.Roguelite;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Tests
{
    public sealed class CombatSpellCostRegressionTests
    {
        [TestCase(1, true)]
        [TestCase(0, false)]
        public void RogueMeleeButton_UsesItsOwnCostInsteadOfTheLegacyFireBolt(int mana, bool available)
        {
            var hero=new UnitState("hero",true,new GridPosition(0,0));
            hero.Equip(CombatCatalog.Hammer,CombatCatalog.Shield,CombatCatalog.FireBolt,CombatCatalog.FrostBind);
            hero.ConfigureMana(12,mana);
            var state=new CombatState(new GridMap(3,2),new[]{hero,new UnitState("enemy",false,new GridPosition(1,0))});
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachRogueSpellRuntime(new RogueSpellCombatRuntime(state,RogueSpellLoadout.CreateStarter().CreateCombatSnapshot()));
            CombatResolver.BeginTurn(state,hero.Id);
            var root=new GameObject("Cost regression");
            try
            {
                var host=root.AddComponent<CombatPrototypeBootstrap>();
                var hud=root.AddComponent<FormalCombatHud>();
                const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
                typeof(CombatPrototypeBootstrap).GetField("state",flags).SetValue(host,state);
                typeof(FormalCombatHud).GetField("bootstrap",flags).SetValue(hud,host);
                var go=new GameObject("技能1",typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(root.transform);
                var button=go.GetComponent<Button>();button.targetGraphic=go.GetComponent<Image>();
                go.AddComponent<UiButtonFeedback>().Configure(button,Color.white,Color.white,Color.white,Color.white,Color.gray,()=>UiMotionProfile.FromIntensity(0),null);
                var buttons=(Dictionary<string,Button>)typeof(FormalCombatHud).GetField("actionButtons",flags).GetValue(hud);buttons.Add("技能1",button);
                typeof(FormalCombatHud).GetMethod("RefreshAvailability",flags).Invoke(hud,new object[]{state,hero});
                Assert.That(button.interactable,Is.EqualTo(available));
                if(available)Assert.That(new CombatCommandExecutionService().Execute(state,state.RogueSpells.FireBattle,CombatCommand.UseSkill(hero.Id,0,"enemy")).Accepted,Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
