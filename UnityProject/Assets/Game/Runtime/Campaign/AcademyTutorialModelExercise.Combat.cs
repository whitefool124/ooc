using System;
using System.Linq;
using OCC.Combat.Roguelite;
namespace OCC.Combat
{
    public sealed partial class AcademyTutorialModelExercise
    {
        private static Model TurnWindows()
        {
            var state = StatusFixture(); int heroBefore = state.OwnTurnCount("hero"), enemyBefore = state.OwnTurnCount("target"); bool enemyWindow = false, heroWindow = false;
            return new Model
            {
                ReadCombat = () => state,
                Prompt = "各单位有自己的回合。当前学院规则以主角回合开始作为公共环境窗口；行动顺序按公开行动条更新。",
                Read = () => "当前行动者 " + (state.ActiveUnitId == "hero" ? "你" : "演练靶") + "\n你的自身回合数 " + state.OwnTurnCount("hero") + "\n演练靶自身回合数 " + state.OwnTurnCount("target") + "\n公共环境窗口次数 " + state.OwnTurnCount("hero"),
                Steps = new Action[] { () => { CombatResolver.BeginTurn(state, "target"); enemyWindow = state.OwnTurnCount("target") == enemyBefore + 1 && state.OwnTurnCount("hero") == heroBefore; }, () => { CombatResolver.BeginTurn(state, "hero"); heroWindow = state.OwnTurnCount("hero") == heroBefore + 1; } },
                Labels = new[] { "观察另一个单位的自身窗口", "观察你的窗口与公共环境结算" },
                Verify = () => enemyWindow && heroWindow, Fingerprint = () => state.ActiveUnitId + "|" + state.OwnTurnCount("hero") + "|" + state.OwnTurnCount("target"),
                Proofs = new[] { "own-turn", "public-turn" }
            };
        }
        private static Model CooldownAndUses()
        {
            var state = StatusFixture(); var hero = state.GetUnit("hero"); var original = state.RogueSpells.DefinitionAtSlot(2);
            var catalog = RogueContentCatalog.CreateAcademyV01();
            var limited = new SpellDefinition(original.DefinitionId, original.DisplayName, original.Element, original.Role, original.Rarity, original.ActionPointCost, original.ManaCost, 1, original.Targeting, original.Range, original.LineOfSightRule, original.Rules, original.CompatibilityTags, original.RewardSources, false, original.EquivalenceGroupId, original.CatalogVersion, original.IsBasic, 1);
            var sample = new RogueContentCatalog(catalog.Spells.Select(s => s.DefinitionId == limited.DefinitionId ? limited : s), catalog.Equipment, catalog.Affixes, catalog.TacticalItems);
            state.AttachRogueSpellRuntime(new RogueSpellCombatRuntime(state, RogueSpellLoadout.CreateStarter().CreateCombatSnapshot(), contentCatalog: sample));
            var runtime = state.RogueSpells; bool cooling = false, readyButEmpty = false, fresh = false, retained = false;
            var boundary = BattleBoundary(); string result = "演练示例每战1次、冷却1个自身回合；正式术式以卡面标注为准";
            return new Model
            {
                Prompt = "冷却与每战次数分别检查。新战斗重置主动术式每战上限；法宝实例剩余总次数不补满。",
                Read = () => result + "\n示例剩余次数 " + runtime.BattleUsesRemaining(limited.DefinitionId) + "\n剩余冷却 " + runtime.CooldownRemaining(limited.DefinitionId),
                Steps = new Action[] {
                    () => { runtime.ExecuteSlot(2, CombatCommand.UseSkill(hero.Id, 2, hero.Id)); cooling = runtime.CooldownRemaining(limited.DefinitionId) > 0 && runtime.BattleUsesRemaining(limited.DefinitionId) == 0; result = "提交成功，扣除一次；冷却单独开始计时"; },
                    () => { CombatResolver.BeginTurn(state, hero.Id); CombatResolver.BeginTurn(state, hero.Id); int mana = hero.Mana, ap = hero.ActionPoints; try { runtime.ExecuteSlot(2, CombatCommand.UseSkill(hero.Id, 2, hero.Id)); } catch (InvalidOperationException) { } readyButEmpty = runtime.CooldownRemaining(limited.DefinitionId) == 0 && !runtime.IsReady(limited.DefinitionId) && hero.Mana == mana && hero.ActionPoints == ap && state.Clone().RogueSpells.BattleUsesRemaining(limited.DefinitionId) == 0; result = "冷却已经结束，但本场次数用尽，仍不能施放；读回战内状态不刷新"; },
                    () => { var next = StatusFixture(); var nextRuntime = new RogueSpellCombatRuntime(next, RogueSpellLoadout.CreateStarter().CreateCombatSnapshot(), contentCatalog: sample); fresh = nextRuntime.BattleUsesRemaining(limited.DefinitionId) == 1 && nextRuntime.IsReady(limited.DefinitionId); for (int i = 0; i < Count(boundary); i++) ApplyStep(boundary, i); retained = boundary.Verify(); result = "下一场示例术式恢复1次；法宝仍剩2次，餐食仍按战数减少"; }
                }, Labels = new[] { "施放每战一次的演练示例", "等待冷却后尝试再次施放", "进入下一场并比较两种次数" },
                Verify = () => cooling && readyButEmpty && fresh && retained,
                Fingerprint = () => result + "|" + runtime.CooldownRemaining(limited.DefinitionId) + "|" + runtime.BattleUsesRemaining(limited.DefinitionId),
                Proofs = new[] { "cooldown-spent", "cooldown-ready", "battle-uses-reset", "artifact-uses-retained" }
            };
        }
        private static Model ActionPoints()
        {
            var state = StatusFixture(); var hero = state.GetUnit("hero"); int initial = hero.ActionPoints; bool paid = false;
            return new Model
            {
                Prompt = "每次操作先看费用。行动点尚有剩余时可以继续；主动结束会清空本回合剩余行动点。",
                Read = () => "行动点 " + hero.ActionPoints,
                Steps = new Action[] { () => { CombatResolver.Resolve(state, CombatCommand.Move(hero.Id, new GridPosition(1, 2))); paid = hero.ActionPoints == initial - 1; }, () => { state.GetUnit("target").SetActionValue(1000); CombatResolver.EndTurn(state, hero); } },
                Labels = new[] { "支付一次移动费用", "主动结束这个回合" }, Verify = () => paid && hero.ActionPoints == 0,
                Fingerprint = () => hero.Position + "|" + hero.ActionPoints, Proofs = new[] { "action-paid", "end-turn" }
            };
        }
        private static Model MoveControl()
        { var model = Passage(); model.Proofs = new[] { "move-preview", "move-accepted", "illegal-unchanged" }; return model; }
        private static Model WeaponPayment()
        {
            var state = StatusFixture(); var hero = state.GetUnit("hero"); var target = state.GetUnit("target"); int ap = hero.ActionPoints, mana = hero.Mana, hp = target.Health; int expected = 0;
            return new Model
            {
                Prompt = "武器动作支付行动点，不支付个人魔力。先看当前伤害预览，再核对实际结果。",
                Read = () => "行动点 " + hero.ActionPoints + "　魔力 " + hero.Mana + "\n演练靶生命 " + target.Health,
                Steps = new Action[] { () => expected = CombatResolver.PreviewAttack(state, hero.Id, target.Id, false).FinalDamage, () => CombatResolver.ResolveWeaponAttack(state, hero.Id, target.Id) },
                Labels = new[] { "查看武器伤害与费用", "执行这次武器动作" },
                Verify = () => hero.ActionPoints == ap - 1 && hero.Mana == mana && target.Health == hp - expected,
                Fingerprint = () => hero.ActionPoints + "|" + hero.Mana + "|" + target.Health, Proofs = new[] { "weapon-preview", "weapon-accepted" }
            };
        }
        private static Model SpellPayment()
        {
            var state = StatusFixture(); var hero = state.GetUnit("hero"); int ap = hero.ActionPoints, mana = hero.Mana; bool cancelled = false;
            var definition = state.RogueSpells.DefinitionAtSlot(2); int cost = state.RogueSpells.EffectiveManaCost(definition);
            return new Model
            {
                Prompt = "个人术式同时检查行动点与魔力。取消预览不扣费，确认成功才支付。",
                Read = () => "示例术式 " + definition.DisplayName + "\n行动点费用 " + definition.ActionPointCost + "　魔力费用 " + cost + "\n当前行动点 " + hero.ActionPoints + "　魔力 " + hero.Mana,
                Steps = new Action[] { () => { var session = new UiConfirmationSession(); session.Open(new UiConfirmationRequest(UiConfirmationKind.LeaveCombat, "演练确认", "查看费用后返回", "确认施放"), () => state.RogueSpells.ExecuteSlot(2, CombatCommand.UseSkill(hero.Id, 2, hero.Id))); session.Cancel(); cancelled = hero.ActionPoints == ap && hero.Mana == mana; }, () => state.RogueSpells.ExecuteSlot(2, CombatCommand.UseSkill(hero.Id, 2, hero.Id)) },
                Labels = new[] { "预览后取消", "确认施放示例术式" },
                Verify = () => cancelled && hero.ActionPoints == ap - definition.ActionPointCost && hero.Mana == mana - cost,
                Fingerprint = () => hero.ActionPoints + "|" + hero.Mana + "|" + hero.Shield,
                Proofs = new[] { "spell-preview", "cancel-unchanged", "spell-accepted" }
            };
        }
        private static Model ManaCapacity()
        {
            var state = StatusFixture(); var hero = state.GetUnit("hero"); var run = Service("academy_gate"); int initial = hero.Mana; bool restored = false, full = false;
            return new Model
            {
                Prompt = "恢复魔力不会超过当前上限。每场战斗入场时，魔力恢复到当时的上限。",
                Read = () => "魔力 " + hero.Mana + "　上限 " + hero.MaxMana,
                Steps = new Action[] { () => { CombatEffectExecutor.Execute(state, hero.Id, CombatEffect.RestoreMana(hero.Id, 20)); restored = hero.Mana > initial && hero.Mana == hero.MaxMana; }, () => { hero.ConfigureMana(hero.MaxMana, 0); run.ApplyBuild(hero); full = hero.Mana == hero.MaxMana; } },
                Labels = new[] { "实际恢复并检查上限", "进入下一场演练战" }, Verify = () => restored && full,
                Fingerprint = () => hero.Mana + "|" + hero.MaxMana, Proofs = new[] { "mana-restored", "mana-cap", "battle-full" }
            };
        }
        private static Model RejectedAction()
        {
            var model = Passage(); model.Prompt = "目标非法时先查看具体原因。返回动作选择不会付费，再找一个合法预览。";
            model.Proofs = new[] { "illegal-reason", "cancel", "legal-preview" }; return model;
        }
        private static Model DamageLayers()
        {
            var state = StatusFixture(); var hero = state.GetUnit("hero"); var target = state.GetUnit("target"); target.GrantShield(2); int hp = target.Health; CombatResolver.AttackPreview preview = default;
            return new Model
            {
                Prompt = "按当前目标计算伤害。护盾先吸收，剩余部分才进入生命；每段分别结算。",
                Read = () => "演练靶护盾 " + target.Shield + "　生命 " + target.Health + "\n" + CombatInformationPresenter.DamageBreakdown(CombatResolver.PreviewAttack(state, hero.Id, target.Id, false)),
                Steps = new Action[] { () => preview = CombatResolver.PreviewAttack(state, hero.Id, target.Id, false), () => CombatResolver.ResolveWeaponAttack(state, hero.Id, target.Id) },
                Labels = new[] { "展开当前伤害预览", "结算并查看护盾与生命" },
                Verify = () => preview.ShieldAbsorption == 2 && target.Shield == 0 && target.Health == hp - preview.FinalDamage,
                Fingerprint = () => target.Shield + "|" + target.Health, Proofs = new[] { "damage-preview", "shield-damage", "health-damage" }
            };
        }
        private static Model ShieldRefresh() => ShieldRefresh(false);
        private static Model ShieldRefresh(bool heavy)
        {
            var state = StatusFixture(); var hero = state.GetUnit("hero"); state.Map.SetTile(heavy ? new GridPosition(1, 2) : hero.Position, new TileState { Cover = heavy ? CoverType.Heavy : CoverType.Light, Durability = heavy ? 24 : 8 }); hero.GrantShield(7);
            return new Model
            {
                ReadCombat = () => state,
                Prompt = "自身回合开始清除旧护盾；回合结束按当前位置检查掩体。" + (heavy ? "正交邻接重掩体授予4点。" : "站在轻掩体上授予2点。"),
                Read = () => "护盾 " + hero.Shield + "\n来源 " + (heavy ? "正交邻接重掩体" : "脚下轻掩体"),
                Steps = new Action[] { () => CombatResolver.BeginTurn(state, hero.Id), () => state.EndRogueliteTurn(hero) },
                Labels = new[] { "开始自身回合，清除旧护盾", "在当前掩体位置结束回合" },
                Verify = () => hero.Shield == (heavy ? 4 : 2) && state.RogueShieldEvents.Count > 0,
                Fingerprint = () => hero.Shield.ToString(), Proofs = new[] { "shield-refresh", "source-view", heavy ? "cover-heavy-shield" : "cover-light-shield" }
            };
        }
    }
}
