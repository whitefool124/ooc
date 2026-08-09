using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public sealed class EnemyIntentPresentation
    {
        public CombatCommand Command { get; }
        public string Signature { get; }
        public string ActionName { get; }
        public string TargetSummary { get; }
        public string ResultSummary { get; }
        public string CompactText => ActionName + " → " + TargetSummary;
        public string DetailedText => CompactText + " // " + ResultSummary;

        internal EnemyIntentPresentation(CombatCommand command, string signature, string actionName, string targetSummary, string resultSummary)
        {
            Command = command;
            Signature = signature ?? string.Empty;
            ActionName = actionName ?? string.Empty;
            TargetSummary = targetSummary ?? string.Empty;
            ResultSummary = resultSummary ?? string.Empty;
        }
    }

    public sealed class EnemyInformationPresentation
    {
        public string Name { get; }
        public string Vitals { get; }
        public string Defenses { get; }
        public string Weapon { get; }
        public string Skills { get; }
        public string Statuses { get; }
        public string FullText => string.Join("\n", Name + " // " + Vitals, Defenses, Weapon, Skills, Statuses);

        internal EnemyInformationPresentation(string name, string vitals, string defenses, string weapon, string skills, string statuses)
        {
            Name = name; Vitals = vitals; Defenses = defenses; Weapon = weapon; Skills = skills; Statuses = statuses;
        }
    }

    public sealed class CombatOutcomePresentation
    {
        public string Title { get; }
        public string Reason { get; }
        public string HeroState { get; }
        public int RemainingEnemyCount { get; }
        public string ObjectiveState { get; }
        public string Consequence { get; }
        public IReadOnlyList<string> RecentEvents { get; }
        public string DetailText => string.Join("\n", Reason, HeroState + " // 剩余敌人 " + RemainingEnemyCount, ObjectiveState, Consequence,
            RecentEvents.Count == 0 ? "最近事件：无" : "最近事件：" + string.Join(" / ", RecentEvents.Take(3)));

        internal CombatOutcomePresentation(string title, string reason, string heroState, int remainingEnemyCount,
            string objectiveState, string consequence, IReadOnlyList<string> recentEvents)
        {
            Title = title; Reason = reason; HeroState = heroState; RemainingEnemyCount = remainingEnemyCount;
            ObjectiveState = objectiveState; Consequence = consequence; RecentEvents = recentEvents ?? Array.Empty<string>();
        }
    }

    public static class CombatInformationPresenter
    {
        public static EnemyIntentPresentation BuildEnemyIntent(CombatState state, UnitState enemy, UnitState hero)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (enemy == null || hero == null) throw new ArgumentNullException(enemy == null ? nameof(enemy) : nameof(hero));
            // This is the sole intent decision: the presentation retains the exact authoritative command.
            CombatCommand command = EnemyTactics.Choose(state, enemy, hero);
            return BuildEnemyIntent(state, enemy, command);
        }

        public static EnemyIntentPresentation BuildEnemyIntent(CombatState state, UnitState enemy, CombatCommand command)
        {
            UnitState target = string.IsNullOrEmpty(command.TargetUnitId) ? null : state.GetUnit(command.TargetUnitId);
            string action;
            string result;
            switch (command.Type)
            {
                case CombatCommandType.UseSkill:
                    SkillDefinition skill = command.SlotIndex == 0 ? enemy.SkillOne : enemy.SkillTwo;
                    action = skill?.DisplayName ?? "施放技能";
                    result = skill == null ? "技能数据缺失" : SkillResult(skill);
                    break;
                case CombatCommandType.Attack:
                    action = enemy.MainHand?.DisplayName ?? "武器攻击";
                    CombatResolver.AttackPreview preview = CombatResolver.PreviewAttack(state, enemy.Id, command.TargetUnitId, false);
                    result = DamageBreakdown(preview);
                    break;
                case CombatCommandType.Move:
                    action = "移动";
                    result = "抵达 " + Cell(command.Destination) + "，朝向 " + FacingLabel(command.Facing);
                    break;
                case CombatCommandType.EndTurn:
                    action = "结束行动";
                    result = "放弃剩余行动点";
                    break;
                default:
                    action = command.Type.ToString();
                    result = "按确定性规则结算";
                    break;
            }
            string targetSummary = target != null ? target.DisplayName : command.Type == CombatCommandType.Move ? Cell(command.Destination) : "自身/战场";
            return new EnemyIntentPresentation(command, CommandSignature(command), action, targetSummary, result);
        }

        public static EnemyInformationPresentation BuildEnemyInformation(UnitState enemy)
        {
            if (enemy == null) throw new ArgumentNullException(nameof(enemy));
            string weapon = enemy.MainHand == null ? "武器：无" : "武器：" + enemy.MainHand.DisplayName + " // 伤害 " + enemy.MainHand.Damage + " // 射程 " + enemy.MainHand.Range;
            string skills = "技能：" + string.Join("；", new[] { enemy.SkillOne, enemy.SkillTwo }.Where(skill => skill != null)
                .Select(skill => skill.DisplayName + "(" + skill.ManaCost + "以太/" + skill.Range + "格/CD" + enemy.Cooldown(skill) + ")"));
            string statuses = enemy.Statuses.Count == 0 ? "状态：无" : "状态：" + string.Join("；", enemy.Statuses.OrderBy(pair => pair.Key)
                .Select(pair => StatusLabel(pair.Key) + " " + pair.Value));
            return new EnemyInformationPresentation(enemy.DisplayName,
                "生命 " + enemy.Health + "/" + enemy.MaxHealth + " // 护盾 " + enemy.Shield + "/" + enemy.MaxShield + " // 以太 " + enemy.Mana + "/" + enemy.MaxMana,
                "护甲 " + enemy.EffectiveArmor + " // 格挡 " + enemy.Block + " // 速度 " + enemy.EffectiveSpeed,
                weapon, skills, statuses);
        }

        public static CombatOutcomePresentation BuildOutcome(CombatState state, bool rogueliteMapCombat)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            UnitState hero = state.Units.Values.FirstOrDefault(unit => unit.IsHero);
            bool victory = state.IsVictory;
            string title = victory ? "战斗胜利" : "战斗失败";
            string reason = victory ? "任务目标已完成。" : hero == null || !hero.IsAlive ? "失败原因：英雄倒下。" : "失败原因：任务目标未能完成。";
            string heroState = hero == null ? "英雄状态不可用" : "英雄 生命 " + hero.Health + "/" + hero.MaxHealth + " // 护盾 " + hero.Shield + "/" + hero.MaxShield;
            int remaining = state.Units.Values.Count(unit => !unit.IsHero && unit.IsAlive);
            int complete = state.Objectives.Count(objective => objective.IsComplete(state));
            string objective = "目标 " + complete + "/" + state.Objectives.Count + " 完成";
            string consequence = victory ? "胜利状态将按当前模式结算。" : rogueliteMapCombat
                ? "战斗内生命、背包与快捷栏变化不会覆盖战前地图存档；战术重开恢复本场确定性快照。"
                : "战斗内变化不会提交；战术重开恢复本场确定性快照。";
            return new CombatOutcomePresentation(title, reason, heroState, remaining, objective, consequence, state.EventLog.Take(5).ToArray());
        }

        public static string PhaseText(CombatFlowPhase phase, CombatState state)
        {
            if (phase == CombatFlowPhase.Victory) return "结算阶段 // 战斗胜利";
            if (phase == CombatFlowPhase.Defeat) return "结算阶段 // 战斗失败";
            if (phase == CombatFlowPhase.TacticalRestart) return "重开阶段 // 正在恢复快照";
            if (phase != CombatFlowPhase.Active) return "准备阶段";
            UnitState active = state?.GetUnit(state.ActiveUnitId);
            return active?.IsHero == true ? "玩家阶段 // 等待指令" : "敌方阶段 // 正在执行真实意图";
        }

        public static string CommandSignature(CombatCommand command) => string.Join("|", command.Type, command.UnitId ?? string.Empty,
            command.TargetUnitId ?? string.Empty, command.Destination.X, command.Destination.Y, command.Facing, command.SlotIndex);

        public static string DamageBreakdown(CombatResolver.AttackPreview preview) => "基础 " + preview.BaseDamage +
            " + 朝向 " + preview.FacingModifier + " - 掩体 " + preview.CoverReduction + " - 护甲 " + preview.ArmorReduction +
            " - 格挡 " + preview.BlockReduction + " // 护盾吸收 " + preview.ShieldAbsorption + " // 生命伤害 " + preview.FinalDamage;

        public static string BuildActionResult(CombatState state, CombatCommand command, CombatEffectExecution execution)
        {
            if (state == null || execution == null) return string.Empty;
            UnitState source = state.GetUnit(command.UnitId);
            UnitState target = string.IsNullOrEmpty(command.TargetUnitId) ? null : state.GetUnit(command.TargetUnitId);
            string action = command.Type == CombatCommandType.Attack ? source?.MainHand?.DisplayName ?? "攻击" :
                command.Type == CombatCommandType.UseSkill ? ((command.SlotIndex == 0 ? source?.SkillOne : source?.SkillTwo)?.DisplayName ?? "技能") :
                command.Type == CombatCommandType.Move ? "移动" : command.Type == CombatCommandType.Interact ? "互动" : command.Type.ToString();
            List<string> changes = new List<string>();
            foreach (CombatEffectResult result in execution.Results.OrderBy(result => result.Sequence))
            {
                if (result.Kind == CombatEffectKind.AbsorbShield && result.AppliedAmount > 0) changes.Add("护盾 " + result.ValueBefore + "→" + result.ValueAfter);
                else if (result.Kind == CombatEffectKind.DamageHealth && result.AppliedAmount > 0) changes.Add("生命 " + result.ValueBefore + "→" + result.ValueAfter);
                else if (result.Kind == CombatEffectKind.RestoreHealth && result.AppliedAmount > 0) changes.Add("生命 " + result.ValueBefore + "→" + result.ValueAfter);
                else if (result.Kind == CombatEffectKind.RestoreShield && result.AppliedAmount > 0) changes.Add("护盾 " + result.ValueBefore + "→" + result.ValueAfter);
                else if (result.Kind == CombatEffectKind.ApplyStatus && result.ValueAfter > 0) changes.Add(StatusLabel(result.Status) + " " + result.ValueAfter);
                else if (result.Kind == CombatEffectKind.Move && result.Changed) changes.Add(Cell(result.PositionBefore) + "→" + Cell(result.PositionAfter));
                else if (result.Kind == CombatEffectKind.DamageObject && result.AppliedAmount > 0) changes.Add("耐久 " + result.ValueBefore + "→" + result.ValueAfter);
            }
            if (changes.Count == 0) return string.Empty;
            return (source?.DisplayName ?? command.UnitId) + " // " + action + " → " + (target?.DisplayName ?? "战场") + " // " + string.Join("；", changes);
        }

        private static string SkillResult(SkillDefinition skill)
        {
            string effects = string.Join("、", skill.Effects.Select(effect => effect.Type == SkillEffectType.Damage ? effect.Amount + "伤害" :
                effect.Type == SkillEffectType.ApplyStatus ? "施加" + StatusLabel(effect.Status) + " " + effect.Duration : effect.Type.ToString()));
            return string.IsNullOrEmpty(effects) ? "按技能规则结算" : effects;
        }

        private static string StatusLabel(StatusType status)
        {
            switch (status)
            {
                case StatusType.Burning: return "燃烧";
                case StatusType.Bound: return "束缚";
                case StatusType.Slow: return "迟缓";
                case StatusType.ArmorBreak: return "破甲";
                default: return status.ToString();
            }
        }

        private static string Cell(GridPosition position) => "(" + position.X + "," + position.Y + ")";
        private static string FacingLabel(Facing facing) => facing == Facing.North ? "北" : facing == Facing.South ? "南" : facing == Facing.East ? "东" : "西";
    }

    public static class RogueliteCombatSettlement
    {
        public static bool TrySettleVictory(RogueliteMapRun run, CombatState combat)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            if (combat == null) throw new ArgumentNullException(nameof(combat));
            if (!combat.IsVictory) return false;
            run.CaptureCombatInventory(combat);
            if (run.HasPendingContentCombat) run.CompletePendingContentCombat();
            else run.CompleteCurrentCombat();
            return true;
        }
    }
}
