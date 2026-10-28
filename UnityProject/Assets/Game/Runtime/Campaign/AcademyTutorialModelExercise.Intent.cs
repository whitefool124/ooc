using System;
using System.Linq;
namespace OCC.Combat
{
    public sealed partial class AcademyTutorialModelExercise
    {
        private static Model PassiveSource()
        { var model = PassiveSpell(); model.Proofs = new[] { "passive-source", "passive-trigger" }; return model; }
        private static Model TemporaryGrowth()
        {
            var state = StatusFixture(); var hero = state.GetUnit("hero"); var enemy = state.GetUnit("target"); EnemyArchetypes.Get("raider").Apply(enemy);
            var growth = new AcademyEnemyGrowthRuntime(); state.AttachAcademyEnemyGrowth(growth); bool source = false, gained = false, consumed = false, cleared = false;
            string text = "本场成长的公开来源与一次性蓄备各有持续规则";
            return new Model
            {
                Prompt = "先查看来源，再观察生成、消费与战后清除。这里不介绍怎样叠加收益。",
                Read = () => text + "\n演练敌人力量增幅 " + enemy.StatusStrength(StatusType.Strength) + "\n一次性蓄备 " + state.PassiveEffects.OngoingEffectsFor(hero.Id).Count,
                Steps = new Action[] {
                    () => { var intent = growth.PresentIntent(enemy); source = intent != null; text = intent.DetailedText; },
                    () => { CombatResolver.BeginTurn(state, enemy.Id); growth.Resolve(state, enemy, CombatCommand.UseSkill(enemy.Id, AcademyEnemyGrowthRuntime.CommandSkillIndex, enemy.Id)); gained = growth.HasUsed(enemy.Id) && enemy.StatusStrength(StatusType.Strength) == 1; text = "成长已产生；同一来源本场不能重复使用"; },
                    () => { CombatResolver.BeginTurn(state, hero.Id); state.PassiveEffects.ArmNextWeaponDamage("teaching-growth", hero.Id, "演练蓄备", "下次武器命中后结算", CombatPassiveSourceKind.ActiveSpell, "teaching-growth", 2, DamageType.Physical); text = "一次性蓄备已生成，等待武器命中后消费"; },
                    () => { FireSpellEngine.ResolveWeaponAttack(state.RogueSpells.FireBattle, hero.Id, enemy.Id); consumed = state.PassiveEffects.OngoingEffectsFor(hero.Id).Count == 0; text = "蓄备触发并移除，本场成长仍保留至战斗结束"; },
                    () => { state = StatusFixture(); hero = state.GetUnit("hero"); enemy = state.GetUnit("target"); cleared = enemy.StatusStrength(StatusType.Strength) == 0 && state.PassiveEffects.OngoingEffectsFor(hero.Id).Count == 0; text = "新战斗没有继承上场的成长与蓄备"; }
                }, Labels = new[] { "查看这次成长的公开来源", "观察成长实际生成", "生成一次性演练蓄备", "触发并消费蓄备", "检查新战斗的清除结果" },
                Verify = () => source && gained && consumed && cleared, Fingerprint = () => text + "|" + enemy.StatusStrength(StatusType.Strength) + "|" + state.PassiveEffects.OngoingEffectsFor(hero.Id).Count,
                Proofs = new[] { "growth-source", "growth-gain", "growth-consume", "growth-clear" }
            };
        }
        private static Model ReactionLimit()
        { var model = CounterState(); model.Proofs = new[] { "reaction-preview", "reaction-trigger", "reaction-limit" }; return model; }
        private static Model ReactionOrder()
        {
            var hero = new UnitState("hero", true, new GridPosition(1, 1)); var helper = new UnitState("helper", true, new GridPosition(1, 2));
            var target = new UnitState("target", false, new GridPosition(2, 1)); hero.ConfigureMana(12); helper.ConfigureMana(12); target.ConfigureVitality(99);
            var state = new CombatState(new GridMap(8, 5), new[] { hero, helper, target }); state.ConfigureRuleset(CombatRuleset.Roguelite); var battle = new ArtifactBattleState(state); var marked = new GridPosition(3, 1); bool two = false;
            return new Model
            {
                Prompt = "多来源按公开顺序逐个重查触发条件。前一来源推离后，后一来源可能不再触发。",
                Read = () => "待触发来源 " + battle.Reactions.Count + "\n演练靶位置 " + target.Position + "\n生命 " + target.Health,
                Steps = new Action[] {
                    () => { CombatResolver.BeginTurn(state, hero.Id); ArtifactEngine.Execute(battle, hero.Id, ArtifactCatalog.ReactionBell, ArtifactTarget.At(marked), 2); CombatResolver.BeginTurn(state, helper.Id); ArtifactEngine.Execute(battle, helper.Id, ArtifactCatalog.ReactionBell, ArtifactTarget.At(marked), 2); hero.SetActionValue(200); helper.SetActionValue(100); two = battle.Reactions.Count == 2; },
                    () => { CombatResolver.BeginTurn(state, target.Id); CombatResolver.Resolve(state, CombatCommand.Move(target.Id, new GridPosition(5, 1))); }
                }, Labels = new[] { "查看两个来源与结算顺序", "观察逐个重查与最终站位" },
                Verify = () => two && target.Health == 87 && target.Position == new GridPosition(4, 1) && !battle.Reactions.ContainsKey(hero.Id) && battle.Reactions.ContainsKey(helper.Id),
                Fingerprint = () => target.Health + "|" + target.Position + "|" + battle.Reactions.Count,
                Proofs = new[] { "multi-preview", "reaction-order", "final-position" }
            };
        }
        private static Model BossPhases()
        {
            var hero = new UnitState("hero", true, new GridPosition(1, 1)); var core = new UnitState("core", false, new GridPosition(4, 3)); EnemyArchetypes.Get("core_overseer").Apply(core);
            var state = new CombatState(new GridMap(7, 7), new[] { hero, core }); state.ConfigureRuleset(CombatRuleset.Roguelite); var boss = new AcademyCoreBossRuntime(); state.AttachAcademyCoreBoss(boss);
            for (int kind = 1; kind <= 3; kind++) state.Map.SetTile(new GridPosition(6, kind), new TileState { IsDevice = true, IsTowerMechanism = true, MechanismKind = kind, Durability = 16 });
            bool first = false, second = false, third = false;
            return new Model
            {
                Prompt = "这里只观察阶段变化。每次停下来查看新增的公开机制，不规定击破顺序。",
                Read = () => "首领阶段 " + boss.PhaseFor(state, core) + "\n已放行机关 " + boss.ReleasedMechanismCount(state) + "\n" + boss.PresentIntent(state, core, boss.ChooseEnemyCommand(state, core, hero)).DetailedText,
                Steps = new Action[] {
                    () => first = boss.PhaseFor(state, core) == 0,
                    () => { for (int i = 0; i < 4; i++) CombatResolver.BeginTurn(state, core.Id); second = boss.PhaseFor(state, core) == 1 && boss.ReleasedMechanismCount(state) == 3; },
                    () => { core.ClearShield(); FireBattleState.ApplyRawDamage(hero, core, core.Health - Math.Max(1, core.MaxHealth * 3 / 10), DamageType.Arcane, "teaching-phase", state); third = core.IsAlive && boss.PhaseFor(state, core) == 2; }
                }, Labels = new[] { "查看隔离门槛阶段", "观察门槛完成后的新阶段", "观察生命门槛后的并链阶段" },
                Verify = () => first && second && third, Fingerprint = () => boss.PhaseFor(state, core) + "|" + core.Health + "|" + boss.ReleasedMechanismCount(state),
                Proofs = new[] { "all-boss-phases" }
            };
        }
        private static Model UpdatedIntent()
        { var model = LockedRoute(); model.Proofs = new[] { "intent-before", "intent-after" }; return model; }
        private static Model LockedRoute()
        {
            var hero = new UnitState("hero", true, new GridPosition(5, 1)); var ram = new UnitState("ram", false, new GridPosition(1, 1));
            EnemyArchetypes.Get("breach_ram").Apply(ram); var state = new CombatState(new GridMap(8, 5), new[] { hero, ram }); state.ConfigureRuleset(CombatRuleset.Roguelite);
            var runtime = new ThreeMaterialPressureRuntime(); state.AttachThreeMaterialPressure(runtime); CombatResolver.BeginTurn(state, ram.Id);
            var locked = hero.Position; var command = CombatCommand.BreachCharge(ram.Id, locked);
            EnemyIntentPresentation before = null, after = null; bool unchanged = false;
            return new Model
            {
                Prompt = "目标格已经锁定，路线仍按当前战场查询。示例只更换一个阻挡物，不教授诱导位置。",
                Read = () => (after ?? before)?.DetailedText + "\n当前冲压者位置 " + ram.Position,
                Steps = new Action[] { () => before = runtime.PresentIntent(state, ram, command), () => { state.Map.SetTile(new GridPosition(3, 1), new TileState { IsPermanentWall = true, Durability = 24 }); after = runtime.PresentIntent(state, ram, command); unchanged = command.Destination == locked && before.TargetSummary == after.TargetSummary && before.Signature != after.Signature; }, () => runtime.ResolveCharge(state, ram, locked) },
                Labels = new[] { "查看锁定格与当前路线", "更换阻挡后重新查看", "核对实际碰撞与终点" },
                Verify = () => unchanged && ram.Position == new GridPosition(2, 1) && state.Map.GetTile(new GridPosition(3, 1)).IsPermanentWall,
                Fingerprint = () => (after ?? before)?.Signature + "|" + ram.Position,
                Proofs = new[] { "locked-cell", "route-before", "route-after", "collision-result" }
            };
        }
    }
}
