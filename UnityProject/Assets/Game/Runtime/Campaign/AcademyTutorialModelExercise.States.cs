using System;
using System.Collections.Generic;
using System.Linq;
using OCC.Combat.Roguelite;

namespace OCC.Combat
{
    public sealed partial class AcademyTutorialModelExercise
    {
        private static readonly Dictionary<string, string[]> VariantKeys = new Dictionary<string, string[]>
        {
            ["U02"] = new[] { "slow", "agility", "bound", "break-stance" },
            ["U03"] = new[] { "status_fireground_boost", "status_fireground_vulnerable", "status_strength", "status_spell_power", "status_shield_efficiency", "status_shield_grant", "status_control", "status_range", "status_speed", "status_damage_taken", "status_move_bonus", "status_spell_cost", "status_cooldown_delta", "status_invulnerable" },
            ["U04"] = new[] { "status_next_weapon_damage", "status_next_weapon_fire", "status_pending_mana", "status_pending_ap", "status_pending_shield", "status_marked", "status_prepared", "status_counter", "passive-not-dispellable" }
            ,["R03"] = new[] { "gold", "contribution", "equipment", "artifact", "spell", "material" }
            ,["R05"] = new[] { "forge-material", "specialization-material" }
            ,["E12"] = new[] { "loose-paper", "paper-screen", "trace", "binding-mark", "smoke", "wind", "light-lane" }
            ,["E10"] = new[] { "common-container", "refined-container", "cache-container", "fixed-container" }
            ,["E03"] = new[] { "melee-preview", "launched-preview", "projected-preview" }
            ,["E13"] = new[] { "overload-device", "ward-generator", "tower-mechanism", "task-interaction" }
            ,["E08"] = new[] { "aether-crystal", "pressure-crystal" }
            ,["C14"] = new[] { "cover-light-shield", "cover-heavy-shield" }
        };
        public static IReadOnlyList<string> Variants(string id) => VariantKeys.TryGetValue(id, out string[] keys) ? keys : Array.Empty<string>();
        private static CombatState StatusFixture()
        {
            var hero = new UnitState("hero", true, new GridPosition(1, 1)) { DisplayName = "演练者" }; hero.ConfigureMana(12, 5); hero.Equip(CombatCatalog.Hammer, hero.SkillOne, hero.SkillTwo);
            var target = new UnitState("target", false, new GridPosition(2, 1)) { DisplayName = "演练靶", Armor = 0 }; target.ConfigureVitality(99);
            var combat = new CombatState(new GridMap(7, 5), new[] { hero, target }); combat.ConfigureRuleset(CombatRuleset.Roguelite); CombatResolver.BeginTurn(combat, "hero");
            hero.ClearShield(); target.ClearShield();
            combat.AttachRogueSpellRuntime(new RogueSpellCombatRuntime(combat, RogueSpellLoadout.CreateStarter().CreateCombatSnapshot()));
            return combat;
        }
        private static Model BattleBoundary()
        {
            var run = Service("layer_medical"); string meal = run.CurrentMedicalService.MealCandidateIds.First(id => id != "MEAL-GUARD"); run.ChooseFirstRunMeal(meal);
            var dto = run.ExportRogue11(); dto.CurrentNodeId = run.MapNodes.First(n => n.IsCombat).Id; run = RogueliteMapRun.FromRogue11(dto);
            var items = RogueEquipmentRuntime.FromDto(dto); items.TryAddContainerItem("teaching-retained-artifact", "G-T13"); var artifact = items.AllTacticalItems.Single(i => i.InstanceId == "teaching-retained-artifact"); artifact.Consume(); items.WriteToDto(dto);
            var battle = StatusFixture(); var hero = battle.GetUnit("hero"); run.ApplyBuild(hero); battle.AttachRogueEquipmentRuntime(RogueEquipmentRuntime.FromDto(dto)); hero.ApplyStatus(StatusType.Burning, 2, 2, "teaching"); hero.GrantShield(7);
            bool ended = false, clean = false; int retainedUses = -1;
            return new Model
            {
                Prompt = "跨战保留生命、餐食剩余战数和法宝总次数。普通战斗状态、护盾与场地不带进下一场。",
                Read = () => "燃烧 " + (hero.HasStatus(StatusType.Burning) ? "有" : "无") + "　护盾 " + hero.Shield + "\n餐食剩余战数 " + run.MealBattlesRemaining + "\n法宝剩余总次数 " + run.RogueRunState.TacticalItemInstances.Single(i => i.InstanceId == "teaching-retained-artifact").ChargesCurrent,
                Steps = new Action[] {
                    () => { run.CaptureCombatInventory(battle); run.CompleteCurrentCombat(); Save(run); ended = run.MealBattlesRemaining == 2; },
                    () => { run = Restore(run); battle = StatusFixture(); hero = battle.GetUnit("hero"); run.ApplyBuild(hero); battle.AttachRogueEquipmentRuntime(RogueEquipmentRuntime.FromDto(run.RogueRunState)); retainedUses = battle.RogueEquipment.AllTacticalItems.Single(i => i.InstanceId == "teaching-retained-artifact").ChargesCurrent; clean = !hero.HasStatus(StatusType.Burning) && hero.Shield == 0; }
                }, Labels = new[] { "结算演练战并保存结果", "进入下一场并核对保留项" },
                Verify = () => ended && clean && retainedUses == 2 && run.MealBattlesRemaining == 2,
                Fingerprint = () => Save(run) + "|" + hero.Shield + "|" + hero.Statuses.Count,
                Proofs = new[] { "status-cleared", "shield-cleared", "meal-retained", "artifact-retained" }
            };
        }
        private static Model StateVariant(string lesson, string key)
        {
            if (key.StartsWith("status_pending_") || key.StartsWith("status_next_weapon_")) return PendingState(key);
            if (key == "status_counter") return CounterState();
            if (key == "passive-not-dispellable") return PassiveProtection();
            if (key == "status_cooldown_delta") return CooldownState();
            var mapping = new Dictionary<string, StatusType>
            {
                ["slow"] = StatusType.Slow, ["agility"] = StatusType.Agility, ["bound"] = StatusType.Bound, ["break-stance"] = StatusType.BreakStance,
                ["status_fireground_boost"] = StatusType.FiregroundBoost, ["status_fireground_vulnerable"] = StatusType.FiregroundVulnerable,
                ["status_strength"] = StatusType.Strength, ["status_spell_power"] = StatusType.SpellPower,
                ["status_shield_efficiency"] = StatusType.ShieldEfficiency, ["status_shield_grant"] = StatusType.ShieldGrant,
                ["status_control"] = StatusType.Control, ["status_range"] = StatusType.Range, ["status_speed"] = StatusType.Speed,
                ["status_damage_taken"] = StatusType.DamageTaken, ["status_move_bonus"] = StatusType.MoveBonus,
                ["status_spell_cost"] = StatusType.SpellCost, ["status_invulnerable"] = StatusType.Invulnerable,
                ["status_marked"] = StatusType.Marked, ["status_prepared"] = StatusType.Prepared
            };
            StatusType type = mapping[key]; var state = StatusFixture(); var hero = state.GetUnit("hero"); var target = state.GetUnit("target");
            UnitState owner = type == StatusType.Slow || type == StatusType.FiregroundVulnerable ? target : hero;
            if (type == StatusType.Slow) CombatResolver.BeginTurn(state, "target");
            if (type == StatusType.BreakStance) hero.GrantShield(6);
            string outcome = "尚未执行"; bool changed = false;
            int strength = type == StatusType.SpellCost ? -1 : 2;
            Action apply = () => CombatEffectExecutor.Execute(state, hero.Id, CombatEffect.ApplyStatus(owner.Id, type, 2, strength));
            Action probe = () =>
            {
                switch (type)
                {
                    case StatusType.Slow: CombatResolver.BeginTurn(state, target.Id); changed = CombatMovementQuery.Budget(state, target) == 3; outcome = "移动步数 " + CombatMovementQuery.Budget(state, target); break;
                    case StatusType.Agility: CombatResolver.BeginTurn(state, hero.Id); changed = CombatMovementQuery.Budget(state, hero) == 5; outcome = "移动步数 " + CombatMovementQuery.Budget(state, hero); break;
                    case StatusType.Bound: outcome = CombatMovementQuery.PlayerTargetFailure(state, new GridPosition(1, 2)); changed = outcome.Contains("束缚"); break;
                    case StatusType.BreakStance: state.TryGrantRogueliteShield(hero.Id, "teaching", 4); changed = hero.Shield == 0; outcome = "护盾 " + hero.Shield + "；获得护盾被破势阻止"; break;
                    case StatusType.FiregroundBoost:
                    case StatusType.FiregroundVulnerable:
                        var fire = state.RogueSpells.FireBattle; fire.CreateOrRefreshFireground(target.Position, 8, 2, "teaching", hero.Id);
                        int health = target.Health; fire.ResolveEntry(target, new GridPosition(2, 2)); changed = target.Health < health; outcome = "火场造成 " + (health - target.Health) + " 点生命损失"; break;
                    case StatusType.Strength: outcome = "物理基础伤害 4 → " + CombatDebugTuning.OutgoingDamageFor(hero, 4, DamageType.Physical); changed = CombatDebugTuning.OutgoingDamageFor(hero, 4, DamageType.Physical) == 6; break;
                    case StatusType.SpellPower: outcome = "火焰基础伤害 4 → " + CombatDebugTuning.OutgoingDamageFor(hero, 4, DamageType.Fire); changed = CombatDebugTuning.OutgoingDamageFor(hero, 4, DamageType.Fire) == 6; break;
                    case StatusType.ShieldEfficiency: CombatEffectExecutor.Execute(state, hero.Id, CombatEffect.RestoreShield(hero.Id, 3, "teaching")); changed = hero.Shield == 5; outcome = "获得3点护盾后的实际护盾 " + hero.Shield; break;
                    case StatusType.ShieldGrant: CombatEffectExecutor.Execute(state, hero.Id, CombatEffect.RestoreShield(target.Id, 3, "teaching")); changed = target.Shield == 5; outcome = "给予3点护盾后的目标护盾 " + target.Shield; break;
                    case StatusType.Control: CombatEffectExecutor.Execute(state, hero.Id, CombatEffect.ApplyStatus(target.Id, StatusType.Bound, 1)); changed = target.StatusDuration(StatusType.Bound) == 4; outcome = "目标束缚生效窗口 " + CombatStatusPresentation.From(target, StatusType.Bound, state).Duration + " 回合"; break;
                    case StatusType.Range: changed = hero.EffectiveRange(3) == 5; outcome = "原射程3，实际射程 " + hero.EffectiveRange(3); break;
                    case StatusType.Speed: changed = hero.EffectiveSpeed == hero.Speed + 2; outcome = "基础速度 " + hero.Speed + "，实际速度 " + hero.EffectiveSpeed; break;
                    case StatusType.DamageTaken: int before = hero.Health; FireBattleState.ApplyRawDamage(target, hero, 4, DamageType.Physical, "teaching", state); changed = hero.Health == before - 6; outcome = "基础伤害4，实际生命损失 " + (before - hero.Health); break;
                    case StatusType.MoveBonus: int budget = CombatMovementQuery.Budget(state, hero); CombatResolver.Resolve(state, CombatCommand.Move(hero.Id, new GridPosition(1, 2))); changed = budget == 5 && !hero.HasStatus(type); outcome = "本次步数 " + budget + "；移动后标记移除"; break;
                    case StatusType.SpellCost: int cost = state.RogueSpells.EffectiveManaCost(state.RogueSpells.DefinitionAtSlot(2)); int oldMana = hero.Mana; state.RogueSpells.ExecuteSlot(2, CombatCommand.UseSkill(hero.Id, 2, hero.Id)); changed = hero.Mana == oldMana - cost && !hero.HasStatus(type); outcome = "本次魔力消耗 " + cost + "；支付后标记移除"; break;
                    case StatusType.Invulnerable:
                        int oldHealth = hero.Health; var position = hero.Position; FireBattleState.ApplyRawDamage(target, hero, 4, DamageType.Physical, "teaching", state);
                        state.ResolveForcedMove(hero, new GridPosition(1, 0), 1, target.Id); hero.ApplyStatus(StatusType.Bound, 1, 0, target.Id);
                        changed = hero.Health == oldHealth && hero.Position == position && hero.HasStatus(StatusType.Bound) && state.IsOccupied(position, target.Id);
                        outcome = "伤害与推拉无效；仍占原格，束缚仍可施加"; break;
                    case StatusType.Prepared: CombatResolver.Resolve(state, CombatCommand.Move(hero.Id, new GridPosition(1, 2))); changed = !hero.HasStatus(type); outcome = "移动后架设解除"; break;
                    case StatusType.Marked: CombatStatusLifecycle.ResolveTurnEnd(state, hero); changed = hero.StatusDuration(type) == 1; outcome = "标记按自身回合衰减，当前剩余 " + hero.StatusDuration(type); break;
                }
            };
            return new Model
            {
                Prompt = "只看这一种状态：" + CombatInformationPresenter.StatusLabel(type) + "。来源、数值和持续窗口都在状态详情中。",
                Read = () => (owner.HasStatus(type) ? CombatStatusPresentation.From(owner, type, state).DisplayName + "　" + CombatStatusPresentation.From(owner, type, state).ValueText + "\n" + CombatStatusPresentation.From(owner, type, state).Detail : "该状态当前未生效") + "\n\n" + outcome,
                Steps = new[] { apply, probe }, Labels = new[] { "施加并查看这一状态", "观察这条规则的实际作用" },
                Verify = () => changed, Fingerprint = () => outcome + "|" + owner.StatusDuration(type) + "|" + owner.StatusStrength(type), Proofs = new[] { key }
            };
        }
        private static Model StatusAndGround()
        {
            var state = StatusFixture(); var hero = state.GetUnit("hero"); var water = new GridPosition(1, 2); state.Map.SetTile(water, new TileState { IsWater = true });
            bool applied = false, removed = false;
            return new Model
            {
                Prompt = "燃烧是身上的状态，浅水是格子的效果。状态的来源、强度和解除条件分别查看。",
                Read = () => "身上燃烧 " + (hero.HasStatus(StatusType.Burning) ? "生效，强度2" : "无") + "\n目标格：" + state.Map.GetTile(water).EffectLayerName() + "\n" + (hero.HasStatus(StatusType.Burning) ? CombatStatusPresentation.From(hero, StatusType.Burning, state).Detail : "燃烧已移除或尚未施加"),
                Steps = new Action[] { () => { CombatEffectExecutor.Execute(state, hero.Id, CombatEffect.ApplyStatus(hero.Id, StatusType.Burning, 2, 2)); applied = hero.HasStatus(StatusType.Burning); }, () => { CombatResolver.Resolve(state, CombatCommand.Move(hero.Id, water)); removed = !hero.HasStatus(StatusType.Burning) && state.Map.GetTile(water).IsWater; } },
                Labels = new[] { "施加燃烧并查看来源", "进入演练浅水，观察解除" },
                Verify = () => applied && removed, Fingerprint = () => hero.Position + "|" + hero.StatusDuration(StatusType.Burning),
                Proofs = new[] { "status-source", "status-preview", "status-remove", "ground-separated" }
            };
        }
        private static Model CooldownState()
        {
            var state = StatusFixture(); var hero = state.GetUnit("hero"); hero.Equip(hero.MainHand, CombatCatalog.FrostBind, hero.SkillTwo); hero.AdjustCooldown(hero.SkillOne.Id, 2);
            return new Model
            {
                Prompt = "冷却调节是即时结算。指定术式剩余冷却改变，最低为0。", ActionLabel = "缩短指定术式冷却",
                Read = () => "示例术式剩余冷却 " + hero.Cooldown(hero.SkillOne) + "\n可使用 " + (hero.IsSkillReady(hero.SkillOne) ? "是" : "否"),
                Apply = () => hero.AdjustCooldown(hero.SkillOne.Id, -2), Verify = () => hero.Cooldown(hero.SkillOne) == 0 && hero.IsSkillReady(hero.SkillOne),
                Fingerprint = () => hero.Cooldown(hero.SkillOne).ToString(), Proofs = new[] { "status_cooldown_delta" }
            };
        }
        private static Model PendingState(string key)
        {
            var state = StatusFixture(); var hero = state.GetUnit("hero"); var target = state.GetUnit("target"); string name = "待回魔"; int beforeHealth = target.Health;
            CombatOngoingEffectKind kind = key == "status_pending_ap" ? CombatOngoingEffectKind.NextTurnActionPoints : key == "status_pending_shield" ? CombatOngoingEffectKind.NextTurnShield : CombatOngoingEffectKind.NextTurnMana;
            if (key == "status_pending_ap") name = "待行动点"; if (key == "status_pending_shield") name = "待护盾";
            bool weapon = key.StartsWith("status_next_weapon_"); if (weapon) name = key.EndsWith("fire") ? "火焰附着" : "武器蓄力";
            bool armed = false;
            return new Model
            {
                Prompt = name + "是待触发标记。先出现标记，达到声明的窗口后才结算并消耗。",
                Read = () => name + "标记 " + (state.PassiveEffects.OngoingEffectsFor(hero.Id).Count > 0 ? "待触发" : "无") + "\n魔力 " + hero.Mana + "　行动点 " + hero.ActionPoints + "　护盾 " + hero.Shield + "\n演练靶生命 " + target.Health,
                Steps = new Action[]
                {
                    () => { if (weapon) state.PassiveEffects.ArmNextWeaponDamage("teaching", hero.Id, name, "下次武器命中追加2点伤害", CombatPassiveSourceKind.ActiveSpell, "teaching", 2, key.EndsWith("fire") ? DamageType.Fire : DamageType.Physical); else state.PassiveEffects.ScheduleNextTurn("teaching", hero.Id, name, "下次自身回合开始时结算", CombatPassiveSourceKind.ActiveSpell, "teaching", kind, 2); armed = state.PassiveEffects.OngoingEffectsFor(hero.Id).Count == 1; },
                    () => { if (weapon) CombatResolver.ResolveWeaponAttack(state, hero.Id, target.Id); else CombatResolver.BeginTurn(state, hero.Id); }
                },
                Labels = new[] { "放入待触发标记", weapon ? "执行一次武器命中" : "推进到下次自己回合开始" },
                Verify = () => armed && state.PassiveEffects.OngoingEffectsFor(hero.Id).Count == 0 && (weapon ? target.Health < beforeHealth : kind == CombatOngoingEffectKind.NextTurnMana ? hero.Mana == 7 : kind == CombatOngoingEffectKind.NextTurnShield ? hero.Shield == 2 : hero.ActionPoints == CombatResolver.HeroActionPointsPerTurn + 2),
                Fingerprint = () => hero.Mana + "|" + hero.ActionPoints + "|" + hero.Shield + "|" + target.Health + "|" + state.PassiveEffects.OngoingEffectsFor(hero.Id).Count,
                Proofs = new[] { key }
            };
        }
        private static Model CounterState()
        {
            var state = StatusFixture(); var hero = state.GetUnit("hero"); var target = state.GetUnit("target"); int health = hero.Health; bool armed = false;
            return new Model
            {
                Prompt = "反击标记记录来源和一次触发次数。相邻攻击后结算；触发或到期即移除。",
                Read = () => "演练靶反击 " + (target.HasStatus(StatusType.Counter) ? "待触发，伤害4" : "无") + "\n演练者生命 " + hero.Health,
                Steps = new Action[] { () => { target.ApplyStatus(StatusType.Counter, 1, 4, target.Id); armed = target.HasStatus(StatusType.Counter); }, () => CombatResolver.ResolveWeaponAttack(state, hero.Id, target.Id) },
                Labels = new[] { "查看演练靶的反击标记", "完成一次相邻攻击，观察结算" },
                Verify = () => armed && !target.HasStatus(StatusType.Counter) && hero.Health == health - 4,
                Fingerprint = () => target.StatusDuration(StatusType.Counter) + "|" + hero.Health, Proofs = new[] { "status_counter" }
            };
        }
        private static Model PassiveProtection()
        {
            var state = StatusFixture(); var hero = state.GetUnit("hero");
            state.PassiveEffects.RegisterPassive(hero.Id, new CombatPassiveDefinition("teaching-passive", "示例被动", CombatPassiveSourceKind.Equipment, "teaching-equipment", CombatPassiveTrigger.AttributeQuery, "被动来源与普通状态分开"));
            return new Model
            {
                Prompt = "装备、天赋、被动术式和餐食有各自来源，不属于可驱散的普通状态。", ActionLabel = "清除普通状态并检查被动来源",
                Read = () => "普通状态 " + hero.Statuses.Count + "\n被动来源 " + state.PassiveEffects.PassivesFor(hero.Id).Count,
                Apply = () => { foreach (StatusType type in Enum.GetValues(typeof(StatusType))) hero.ClearStatus(type); },
                Verify = () => state.PassiveEffects.PassivesFor(hero.Id).Count == 1, Fingerprint = () => state.PassiveEffects.PassivesFor(hero.Id).Count.ToString(), Proofs = new[] { "passive-not-dispellable" }
            };
        }
    }
}
