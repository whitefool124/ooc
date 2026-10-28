using System;
using System.Linq;

namespace OCC.Combat
{
    public sealed partial class AcademyTutorialModelExercise
    {
        private static readonly string[] EffectProofs = { "effect-preview", "effect-commit", "effect-result" };
        private static Model FollowUpMovement()
        {
            var state = StatusFixture(); var hero = state.GetUnit("hero"); var target = state.GetUnit("target"); var battle = state.RogueSpells.FireBattle;
            var spell = new FireSpellDefinition("teaching-followup", "续步规则演练", FireSpellRarity.Common, FireSpellGroup.Universal,
                FireCombatAffinity.WeaponUniversal, FireDeliveryMode.WeaponAttachment, FireWeaponRequirement.AnyWeapon,
                FireTriggerWindow.AfterNextWeaponAttack, FireConsumptionRule.OnTrigger,
                0, 0, 0, 0, 0, FireTargetKind.Self, FireSelectionShape.Single, 1, false, false,
                new[] { new FireSpellRule(FireRuleKind.MoveAfterAttack, 1, timing: FireRuleTiming.OnTrigger) });
            bool armed = false; int apBefore = hero.ActionPoints;
            return new Model
            {
                Prompt = "续步在声明的结算后额外移动；依然检查路径、阻挡和落点。示例不影响本轮。",
                Read = () => "待结算续步 " + battle.PendingEffects.Count + "\n当前位置 " + hero.Position + "\n行动点 " + hero.ActionPoints,
                Steps = new Action[] { () => { FireSpellEngine.Execute(battle, hero.Id, spell, FireSpellTarget.Unit(hero.Id)); armed = battle.PendingEffects.Count == 1; }, () => FireSpellEngine.ResolveWeaponAttack(battle, hero.Id, target.Id, new GridPosition(1, 2)) },
                Labels = new[] { "准备示例续步", "完成示例命中与额外一步" },
                Verify = () => armed && hero.Position == new GridPosition(1, 2) && hero.ActionPoints == apBefore - 1 && battle.PendingEffects.Count == 0,
                Fingerprint = () => hero.Position + "|" + hero.ActionPoints + "|" + battle.PendingEffects.Count, Proofs = EffectProofs
            };
        }
        private static Model Interception()
        {
            var state = StatusFixture(); var hero = state.GetUnit("hero"); var target = state.GetUnit("target"); var battle = new ArtifactBattleState(state);
            var marked = new GridPosition(3, 1); int health = target.Health; bool armed = false; bool stopped = false;
            return new Model
            {
                Prompt = "标记格生成时不触发。敌人入格后结算一次；推离改变站位时，未走完的路径停止。",
                Read = () => "标记格 " + marked + "\n待触发次数 " + battle.Reactions.Count + "\n演练靶生命 " + target.Health + "\n演练靶位置 " + target.Position,
                Steps = new Action[]
                {
                    () => { ArtifactEngine.Execute(battle, hero.Id, ArtifactCatalog.ReactionBell, ArtifactTarget.At(marked), 2); armed = battle.Reactions.Count == 1 && target.Health == health; },
                    () => { CombatResolver.BeginTurn(state, target.Id); CombatResolver.Resolve(state, CombatCommand.Move(target.Id, new GridPosition(5, 1))); stopped = target.Position == new GridPosition(4, 1); }
                },
                Labels = new[] { "生成演练截击点", "让演练靶经过标记格" },
                Verify = () => armed && stopped && target.Health == health - 12 && battle.Reactions.Count == 0,
                Fingerprint = () => target.Position + "|" + target.Health + "|" + battle.Reactions.Count, Proofs = EffectProofs
            };
        }
        private static Model DelayedResources()
        {
            var model = PendingState("status_pending_mana"); model.Proofs = EffectProofs; return model;
        }
        private static Model DirectCounter()
        {
            var model = CounterState(); model.Proofs = EffectProofs; return model;
        }
        private static Model PreHitShield()
        {
            var state = StatusFixture(); var hero = state.GetUnit("hero"); var target = state.GetUnit("target"); target.MoveTo(new GridPosition(4, 1));
            target.ConfigureEnemyAttack(new EnemyAttackDefinition("teaching-shot", "演练射击", DamageType.Physical, 6, 5), null, null);
            var battle = new ArtifactBattleState(state); int health = hero.Health; bool prepared = false;
            return new Model
            {
                Prompt = "格挡先在受击前获得所示护盾，再结算这次伤害。它只有一次指定触发窗口。",
                Read = () => "待触发格挡 " + battle.Reactions.Count + "\n演练者护盾 " + hero.Shield + "\n演练者生命 " + hero.Health,
                Steps = new Action[]
                {
                    () => { battle.Reactions[hero.Id] = new ArtifactReaction(ArtifactReactionTrigger.IncomingRangedDamage, 8, 0, default); prepared = hero.Shield == 0 && battle.Reactions.Count == 1; },
                    () => { CombatResolver.BeginTurn(state, target.Id); CombatResolver.ResolveWeaponAttack(state, target.Id, hero.Id); }
                },
                Labels = new[] { "放入格挡标记", "结算一次指定的演练伤害" },
                Verify = () => prepared && battle.Reactions.Count == 0 && hero.Shield == 2 && hero.Health == health,
                Fingerprint = () => hero.Shield + "|" + hero.Health + "|" + battle.Reactions.Count, Proofs = EffectProofs
            };
        }
    }
}
