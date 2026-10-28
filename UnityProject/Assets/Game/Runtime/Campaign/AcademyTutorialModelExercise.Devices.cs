using System;
namespace OCC.Combat
{
    public sealed partial class AcademyTutorialModelExercise
    {
        private static Model DeviceVariant(string key)
        {
            var state = StatusFixture(); var hero = state.GetUnit("hero"); var cell = new GridPosition(1, 2); int hp = hero.Health;
            string text = "查看装置的公开作用与耐久"; bool observed = false, result = false;
            Action first, second;
            if (key == "overload-device")
            {
                state.Map.SetTile(cell, new TileState { IsOverloadDevice = true, IsDevice = true, Durability = 8 });
                first = () => { observed = state.Map.GetTile(cell).Durability == 8; text = "过载装置　耐久8\n损毁后波及正交四格，敌我一致"; };
                second = () => { CombatEffectExecutor.Execute(state, hero.Id, CombatEffect.DamageObject(cell, 8)); result = hero.Health == hp - 8 && !state.Map.GetTile(cell).IsOverloadDevice; text = "装置已引爆；相邻演练者生命减少8"; };
            }
            else if (key == "ward-generator")
            {
                state.Map.SetTile(cell, new TileState { IsWardGenerator = true, IsDevice = true, Durability = 8 });
                first = () => { state.EndRogueliteTurn(hero); observed = hero.Shield == 4; hero.GrantShield(7, "teaching-other"); FireBattleState.ApplyRawDamage(state.GetUnit("target"), hero, 2, DamageType.Physical, "teaching-shield-hit", state); text = "发生器提供4点结构护盾；另有7点普通护盾\n受到2点伤害后，当前护盾 " + hero.Shield; };
                second = () => { CombatEffectExecutor.Execute(state, hero.Id, CombatEffect.DamageObject(cell, 8)); result = hero.Shield == 7; text = "发生器损毁，其尚存的2点结构护盾移除\n其余来源的7点护盾保留"; };
            }
            else if (key == "tower-mechanism")
            {
                var boss = new AcademyCoreBossRuntime(); state.AttachAcademyCoreBoss(boss); state.Map.SetTile(cell, new TileState { IsTowerMechanism = true, IsDevice = true, MechanismKind = 1, Durability = 16 });
                first = () => { boss.ReleaseNextMechanism(state); observed = boss.ReleasedMechanismCount(state) == 1; text = "机关放行后计入维护链\n当前有效维护链 " + boss.ReleasedMechanismCount(state); };
                second = () => { CombatEffectExecutor.Execute(state, hero.Id, CombatEffect.DamageObject(cell, 16)); result = boss.ReleasedMechanismCount(state) == 0; text = "机关损毁，当前有效维护链 " + boss.ReleasedMechanismCount(state); };
            }
            else
            {
                state.ConfigureObjectives(new InvestigationObjective(new[] { cell }));
                int ap = hero.ActionPoints;
                first = () => { observed = !state.IsInvestigated(cell) && !state.Objectives[0].IsComplete(state); text = "本例目标要求互动调查；只有伤害不会代替任务互动"; };
                second = () => { CombatResolver.Resolve(state, CombatCommand.Interact(hero.Id, cell)); result = state.IsInvestigated(cell) && state.Objectives[0].IsComplete(state) && hero.ActionPoints == ap - 1; text = "调查已完成，目标记录更新；支付1行动点"; };
            }
            return new Model
            {
                ReadCombat = () => state, Prompt = "只核对这一种装置：先看规则，再互动或破坏并查看结果，不规定处理顺序。",
                Read = () => text + "\n当前位置 " + hero.Position + "　护盾 " + hero.Shield,
                Steps = new[] { first, second }, Labels = new[] { "查看这个来源的作用", key == "task-interaction" ? "完成一次任务互动" : "破坏并核对来源变化" },
                Verify = () => observed && result, Fingerprint = () => text + "|" + hero.Health + "|" + hero.Shield + "|" + state.Map.GetTile(cell).Durability,
                Proofs = new[] { key }
            };
        }
    }
}
