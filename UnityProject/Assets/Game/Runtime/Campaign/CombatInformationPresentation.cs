using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public sealed class EnemyIntentPresentation
    {
        public string Signature { get; }
        public string ActionName { get; }
        public string TargetSummary { get; }
        public string ResultSummary { get; }
        public string IconId { get; }
        public bool HasDestination { get; }
        public GridPosition Destination { get; }
        public int ExpectedDamage { get; }
        public string CompactText => ActionName + " → " + TargetSummary;
        public string DetailedText => CompactText + "\n" + ResultSummary;

        internal EnemyIntentPresentation(string signature, string actionName, string targetSummary, string resultSummary,
            string iconId, bool hasDestination, GridPosition destination, int expectedDamage)
        {
            Signature = signature ?? string.Empty;
            ActionName = actionName ?? string.Empty;
            TargetSummary = targetSummary ?? string.Empty;
            ResultSummary = resultSummary ?? string.Empty;
            IconId = string.IsNullOrWhiteSpace(iconId) ? "defend" : iconId;
            HasDestination = hasDestination;
            Destination = destination;
            ExpectedDamage = Math.Max(0, expectedDamage);
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
        public string FullText => string.Join("\n", Name, Vitals, Defenses, Weapon, Skills, Statuses);

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
        public string CompactDetailText => string.Join("\n", Reason, HeroState, "剩余敌人 " + RemainingEnemyCount, ObjectiveState, Consequence);
        public string RecentEventsText => RecentEvents.Count == 0 ? "最近事件：无" : string.Join("\n", RecentEvents.Take(5));
        public string DetailText => string.Join("\n", Reason, HeroState, "剩余敌人 " + RemainingEnemyCount, ObjectiveState, Consequence,
            RecentEvents.Count == 0 ? "最近事件：无" : "最近事件：\n" + string.Join("\n", RecentEvents.Take(3)));

        internal CombatOutcomePresentation(string title, string reason, string heroState, int remainingEnemyCount,
            string objectiveState, string consequence, IReadOnlyList<string> recentEvents)
        {
            Title = title; Reason = reason; HeroState = heroState; RemainingEnemyCount = remainingEnemyCount;
            ObjectiveState = objectiveState; Consequence = consequence; RecentEvents = recentEvents ?? Array.Empty<string>();
        }
    }

    public enum CombatCancelResolution
    {
        ClearTarget,
        ResetAction,
        RequestLeave
    }

    public static class CombatSelectionNavigation
    {
        public static CombatCancelResolution ResolveCancel(string selectedAction, string selectedTargetId, bool hasArmedItem)
        {
            if (!string.IsNullOrEmpty(selectedTargetId)) return CombatCancelResolution.ClearTarget;
            if (hasArmedItem || !string.Equals(selectedAction, "移动", StringComparison.Ordinal)) return CombatCancelResolution.ResetAction;
            return CombatCancelResolution.RequestLeave;
        }
    }

    public sealed class CombatTargetNavigationState
    {
        public bool Active { get; private set; }
        public GridPosition Position { get; private set; }

        public void Begin(GridPosition position, int width, int height)
        {
            Active = true;
            Position = Clamp(position, width, height);
        }

        public void Move(int deltaX, int deltaY, int width, int height)
        {
            if (!Active) return;
            Position = Clamp(new GridPosition(Position.X + deltaX, Position.Y + deltaY), width, height);
        }

        public void End()
        {
            Active = false;
        }

        private static GridPosition Clamp(GridPosition position, int width, int height)
        {
            return new GridPosition(Math.Max(0, Math.Min(Math.Max(0, width - 1), position.X)),
                Math.Max(0, Math.Min(Math.Max(0, height - 1), position.Y)));
        }
    }

    public static class CombatInformationPresenter
    {
        public static EnemyIntentPresentation BuildEnemyIntent(CombatState state, UnitState enemy, CombatCommand command)
        {
            UnitState target = string.IsNullOrEmpty(command.TargetUnitId) ? null : state.GetUnit(command.TargetUnitId);
            string action;
            string result;
            string iconId;
            bool hasDestination = false;
            GridPosition destination = default;
            int expectedDamage = 0;
            switch (command.Type)
            {
                case CombatCommandType.UseSkill:
                    SkillDefinition skill = command.SlotIndex == 0 ? enemy.SkillOne : enemy.SkillTwo;
                    action = skill?.DisplayName ?? "施放技能";
                    iconId = "cast";
                    int skillPreHitShield = skill != null && skill.Damage > 0 && target != null
                        ? PendingPreHitShield(state, enemy, target) : 0;
                    CombatState skillPreviewState = PreviewStateWithPreHitShield(state, target, skillPreHitShield);
                    CombatResolver.AttackPreview? skillDamage = skill != null && skill.Damage > 0 && target != null
                        ? CombatResolver.PreviewSkillAttack(skillPreviewState, enemy.Id, target.Id, skill)
                        : (CombatResolver.AttackPreview?)null;
                    expectedDamage = skillDamage.HasValue ? IncomingDamage(skillDamage.Value) : 0;
                    result = skill == null ? "技能数据缺失" : SkillResult(skill, skillDamage);
                    if (skillPreHitShield > 0 && skillDamage.HasValue)
                        result = PreHitShieldSummary(target.Shield, skillPreHitShield, skillDamage.Value.ShieldAbsorption, result);
                    result += AdjacentCounterSummary(state, enemy, target);
                    break;
                case CombatCommandType.Attack:
                    action = enemy.MainHand?.DisplayName ?? "武器攻击";
                    int attackPreHitShield = target == null ? 0 : PendingPreHitShield(state, enemy, target);
                    CombatState attackPreviewState = PreviewStateWithPreHitShield(state, target, attackPreHitShield);
                    CombatResolver.AttackPreview preview = CombatResolver.PreviewAttack(attackPreviewState, enemy.Id, command.TargetUnitId, false);
                    expectedDamage = IncomingDamage(preview);
                    result = IncomingDamageSummary(preview);
                    if (attackPreHitShield > 0)
                        result = PreHitShieldSummary(target.Shield, attackPreHitShield, preview.ShieldAbsorption, result);
                    result += AdjacentCounterSummary(state, enemy, target);
                    iconId = "attack";
                    break;
                case CombatCommandType.Move:
                    action = "移动";
                    if (state.ArtifactBattle?.TryGetLureTarget(enemy, out GridPosition lure) == true &&
                        command.Destination.ManhattanDistance(lure) < enemy.Position.ManhattanDistance(lure))
                    {
                        action = "受诱导接近";
                        result = "向诱导灯 " + Cell(lure) + " 接近，抵达 " + Cell(command.Destination);
                    }
                    else if (enemy.EnemyArchetypeId == "rune_arbalist" && enemy.MainHand != null &&
                        enemy.Position.ManhattanDistance(state.GetUnit("hero").Position) < enemy.MainHand.MinimumRange &&
                        command.Destination.ManhattanDistance(state.GetUnit("hero").Position) >= enemy.MainHand.MinimumRange)
                    {
                        action = "重弩退距";
                        result = "撤到 " + Cell(command.Destination) + "；相邻格是公开近身死区，无法发射绞盘重弩或重矢";
                    }
                    else result = "抵达 " + Cell(command.Destination);
                    iconId = "move";
                    hasDestination = true;
                    destination = command.Destination;
                    break;
                case CombatCommandType.EndTurn:
                    action = enemy.HasStatus(StatusType.Bound) ? "受缚驻留" : "结束行动";
                    result = enemy.HasStatus(StatusType.Bound)
                        ? "当前无法移动；目标若进入现有攻击范围仍可攻击"
                        : "放弃剩余行动点";
                    iconId = "defend";
                    break;
                case CombatCommandType.Interact:
                    TileState interacted = state.Map.GetTile(command.Destination);
                    action = interacted.IsDecoy ? "破坏诱导灯" : "破坏物件";
                    result = interacted.IsDecoy
                        ? "对 " + Cell(command.Destination) + " 的诱导灯造成 " + (enemy.MainHand?.Damage ?? 0) + " 耐久伤害；当前耐久 " + interacted.Durability
                        : "影响 " + Cell(command.Destination);
                    iconId = "interact_destroy";
                    break;
                default:
                    action = "行动";
                    result = "行动时会显示结果";
                    iconId = "defend";
                    break;
            }
            string targetSummary = target != null ? target.DisplayName : command.Type == CombatCommandType.Move ? Cell(command.Destination) :
                command.Type == CombatCommandType.Interact && state.Map.GetTile(command.Destination).IsDecoy ? "诱导灯 " + Cell(command.Destination) : "自身或战场";
            return new EnemyIntentPresentation(CommandSignature(command), action, targetSummary, result,
                iconId, hasDestination, destination, expectedDamage);
        }

        private static int PendingPreHitShield(CombatState state, UnitState attacker, UnitState target)
        {
            return state?.RogueSpells == null || attacker == null || target == null ? 0 :
                FireSpellEngine.PendingPreHitShield(state.RogueSpells.FireBattle, target.Id, attacker.Id);
        }

        private static CombatState PreviewStateWithPreHitShield(CombatState state, UnitState target, int amount)
        {
            if (state == null || target == null || amount <= 0) return state;
            CombatState clone = state.Clone();
            clone.TryGrantRogueliteShield(target.Id, "preview:F-P-M11", amount);
            return clone;
        }

        private static string PreHitShieldSummary(int currentShield, int amount, int absorbed, string damageSummary)
        {
            int raised = currentShield + amount;
            int remaining = Math.Max(0, raised - Math.Max(0, absorbed));
            return "热障架势先获得 " + amount + " 点护盾（" + currentShield + "→" + raised + "）；" +
                damageSummary + "；结算后护盾 " + remaining;
        }

        private static string AdjacentCounterSummary(CombatState state, UnitState attacker, UnitState target)
        {
            if (state?.RogueSpells == null || attacker == null || target == null ||
                attacker.Position.ManhattanDistance(target.Position) != 1) return string.Empty;
            bool armed = state.RogueSpells.FireBattle.PendingEffects.Any(effect =>
                effect.SourceUnitId == target.Id && effect.Spell.Id == "F-P-M13" &&
                effect.Spell.TriggerWindow == FireTriggerWindow.FirstAdjacentAttack);
            if (!armed) return string.Empty;

            FireBattleState previewBattle = state.RogueSpells.FireBattle.Clone();
            UnitState attackerBefore = previewBattle.Combat.GetUnit(attacker.Id);
            int shieldBefore = attackerBefore.Shield;
            int healthBefore = attackerBefore.Health;
            FireSpellEngine.TriggerIncomingAdjacentAttack(previewBattle, attacker.Id, target.Id);
            UnitState attackerAfter = previewBattle.Combat.GetUnit(attacker.Id);
            int shieldLoss = Math.Max(0, shieldBefore - attackerAfter.Shield);
            int healthLoss = Math.Max(0, healthBefore - attackerAfter.Health);
            string result = "；公开反应：炉心反击将使攻击者护盾 -" + shieldLoss + "、生命 -" + healthLoss;
            return !attackerAfter.IsAlive ? result + "，并击倒攻击者" : result;
        }

        public static EnemyInformationPresentation BuildEnemyInformation(UnitState enemy, bool roguelite = false)
        {
            if (enemy == null) throw new ArgumentNullException(nameof(enemy));
            string weapon = enemy.MainHand == null ? "武器：无" : "武器：" + enemy.MainHand.DisplayName + "\n伤害 " + enemy.MainHand.Damage + "　射程 " + RangeText(enemy.MainHand.MinimumRange, enemy.MainHand.Range);
            string skills = "战法：" + string.Join("；", new[] { enemy.SkillOne, enemy.SkillTwo }.Where(skill => skill != null)
                .Select(skill => skill.DisplayName + "——" + SkillResult(skill) + (enemy.Cooldown(skill) > 0 ? "；还需等待 " + enemy.Cooldown(skill) + " 回合" : string.Empty)));
            string statuses = enemy.Statuses.Count == 0 ? "状态：无" : "状态：" + string.Join("；", enemy.Statuses.OrderBy(pair => pair.Key)
                .Select(pair => StatusLabel(pair.Key) + " " + pair.Value));
            return new EnemyInformationPresentation(enemy.DisplayName,
                "生命当前 " + enemy.Health + "　上限 " + enemy.MaxHealth + "\n护盾当前 " + enemy.Shield + "　上限 " + enemy.MaxShield,
                roguelite ? "普通盾 " + enemy.Shield + "　速度 " + enemy.EffectiveSpeed : "护甲 " + enemy.EffectiveArmor + "　格挡 " + enemy.Block + "　速度 " + enemy.EffectiveSpeed,
                weapon, skills, statuses);
        }

        public static CombatOutcomePresentation BuildOutcome(CombatState state, bool rogueliteMapCombat)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            UnitState hero = state.Units.Values.FirstOrDefault(unit => unit.IsHero);
            bool victory = state.IsVictory;
            string title = victory ? "战斗胜利" : "战斗失败";
            string reason = victory ? "任务目标已完成。" : hero == null || !hero.IsAlive ? "失败原因：英雄倒下。" : "失败原因：任务目标未能完成。";
            string heroState = hero == null ? "英雄状态不可用" : "英雄生命 当前 " + hero.Health + "　上限 " + hero.MaxHealth + "\n英雄护盾 当前 " + hero.Shield + "　上限 " + hero.MaxShield;
            int remaining = state.Units.Values.Count(unit => !unit.IsHero && unit.IsAlive);
            int complete = state.Objectives.Count(objective => objective.IsComplete(state));
            string objective = "目标已完成 " + complete + "　总计 " + state.Objectives.Count;
            string consequence = victory ? "回到地图后，就能带走本场收获。" : rogueliteMapCombat
                ? "可以回到地图，或者从头再挑战一次。"
                : "离开后这场战斗不会留下任何收获，也可以从头再挑战。";
            string efficiency = state.PressureTest?.EfficiencySummary(state) ?? string.Empty;
            if (!string.IsNullOrEmpty(efficiency)) consequence += "\n" + efficiency;
            return new CombatOutcomePresentation(title, reason, heroState, remaining, objective, consequence, state.EventLog.Take(5).ToArray());
        }

        public static string BuildCompactTargetSummary(CombatActionPreview preview, UnitState target, EnemyIntentPresentation intent)
        {
            if (preview == null) return "等待战斗状态";
            string availability = preview.CanSubmit ? "可以行动" : preview.FailureReason;
            if (target == null)
                return "行动  " + preview.Action + "\n" + preview.TargetRule + "\n" + availability + "\n悬停查看说明";

            string result = string.IsNullOrWhiteSpace(preview.ExpectedResult) ? "请选择目标" : preview.ExpectedResult;
            string intentText = intent == null ? "尚未显露" : intent.CompactText;
            return target.DisplayName + "\n生命当前 " + target.Health + "　上限 " + target.MaxHealth + "\n护盾当前 " + target.Shield + "　上限 " + target.MaxShield +
                "\n行动  " + preview.Action + "\n" + availability + "\n预计  " + result + "\n敌人打算  " + intentText;
        }

        public static string BuildHudDecisionSummary(CombatActionPreview preview, UnitState target, bool keyboardTargeting)
        {
            if (preview == null) return "等待战斗状态";
            string availability = preview.CanSubmit ? "可执行" : "不可执行，" + preview.FailureReason;
            string targetText = target != null ? target.DisplayName : preview.TargetRule.Length <= 20 ? preview.TargetRule : "可选格 " + preview.ValidCellCount;
            string expected = Compact(preview.ExpectedResult, 18);
            string summary = preview.Action + "　" + availability + "\n" +
                (keyboardTargeting ? "选点中　" : string.Empty) + "目标　" + targetText + (string.IsNullOrEmpty(expected) ? string.Empty : "　预计　" + expected);
            return summary;
        }

        private static string Compact(string value, int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return value.Length <= maximumLength ? value : value.Substring(0, maximumLength - 1) + "…";
        }

        public static string BuildActionDetails(CombatActionPreview preview)
        {
            if (preview == null) return "战斗状态尚未就绪。";
            List<string> lines = new List<string>
            {
                preview.CanSubmit ? "● 可用" : "● 不可用，" + preview.FailureReason,
                "消耗　" + (string.IsNullOrWhiteSpace(preview.Cost) ? "无" : preview.Cost),
                "目标　" + preview.TargetRule,
                "效果　" + preview.ExpectedResult
            };
            if (!string.IsNullOrWhiteSpace(preview.DamageBreakdown)) lines.Add("伤害：" + preview.DamageBreakdown);
            if (!string.IsNullOrWhiteSpace(preview.StatusResults)) lines.Add("状态变化：" + preview.StatusResults);
            if (preview.AffectedCellCount > 1) lines.Add("范围　" + preview.AffectedCellCount + " 格" + (preview.FriendlyFireRisk ? "，可能波及友军" : string.Empty));
            else if (preview.FriendlyFireRisk) lines.Add("风险　可能波及友军");
            return string.Join("\n", lines);
        }

        public static string BuildTargetDetails(CombatActionPreview preview, UnitState target, EnemyIntentPresentation intent, bool roguelite = false)
        {
            List<string> sections = new List<string> { BuildActionDetails(preview) };
            if (target != null)
            {
                EnemyInformationPresentation profile = BuildEnemyInformation(target, roguelite);
                sections.Add("敌人资料\n" + profile.Name + "\n" + profile.Vitals + "\n" + profile.Weapon);
                if (intent != null) sections.Add("敌人打算\n" + intent.DetailedText);
            }
            return string.Join("\n\n", sections);
        }

        public static string EnemyInspectionTargetAt(CombatState state, GridPosition position)
        {
            if (state == null) return null;
            UnitState enemy = state.Units.Values.FirstOrDefault(unit => unit.IsAlive && !unit.IsHero && unit.Position == position);
            return enemy?.Id;
        }

        public static string BuildEnemyHoverDetails(CombatState state, UnitState enemy, EnemyIntentPresentation intent)
        {
            if (state == null || enemy == null || enemy.IsHero) return string.Empty;
            EnemyInformationPresentation profile = BuildEnemyInformation(enemy, state.Ruleset == CombatRuleset.Roguelite);
            return string.Join("\n", profile.Defenses, profile.Weapon, "特点：" + EnemyRoleSummary(enemy.EnemyArchetypeId),
                profile.Skills, profile.Statuses, "当前意图：" + (intent?.DetailedText ?? "尚未显露"));
        }

        private static string EnemyRoleSummary(string archetypeId)
        {
            switch (archetypeId)
            {
                case "shieldguard": return "盾术陪练生每次自己回合开始整盾获得 2 护盾，并会用铭盾冲撞使目标迟缓；破势可阻止整盾。";
                case "pyromancer": return "火矢陪练生从远处施放受安全刻印约束的训练火矢。";
                case "raider": return "侧锋陪练生行动迅速，贴近后用限位钩刃限制移动。";
                case "elite_vanguard": return "刻阵教官主持高阶考核，重击会清除护盾并造成破势。";
                case "sigil_mauler": return "承压检验偶必须邻接，锤印会清除护盾并造成破势。";
                case "barrier_mender": return "护障助教优先为护盾受损最严重的友军续接护障。";
                case "tether_hound": return "缚环寻迹兽移动迅速，扑咬会束缚目标。";
                case "stone_snare": return "约束助教可从远处束缚目标，限制持续时间较长。";
                case "lantern_revealer": return "档案巡查员用显影灯清除护盾并造成破势。";
                case "rune_arbalist": return "重弩陪练生移动缓慢，绞盘重弩与重矢有效距离为 2–4／2–5 格；相邻格是公开死区，会先退距再射击。";
                default: return "会根据距离选择攻击、施术或靠近目标。";
            }
        }

        public static string BuildHeroDetails(UnitState hero)
        {
            if (hero == null) return "英雄状态尚未就绪。";
            string weapon = hero.MainHand == null ? "主手：无" : "主手：" + hero.MainHand.DisplayName + "\n伤害 " + hero.MainHand.Damage + "　射程 " + RangeText(hero.MainHand.MinimumRange, hero.MainHand.Range);
            string statuses = hero.Statuses.Count == 0 ? "状态：正常" : "状态：" + string.Join("；", hero.Statuses.OrderBy(pair => pair.Key).Select(pair => StatusLabel(pair.Key) + " " + pair.Value));
            return string.Join("\n",
                "生命当前 " + hero.Health + "　上限 " + hero.MaxHealth + "\n护盾当前 " + hero.Shield + "　上限 " + hero.MaxShield + "\n以太当前 " + hero.Mana + "　上限 " + hero.MaxMana,
                "行动点 " + hero.ActionPoints + "　护甲 " + hero.EffectiveArmor + "　格挡 " + hero.Block + "　速度 " + hero.EffectiveSpeed,
                weapon, statuses);
        }

        private static string RangeText(int minimum, int maximum) => minimum > 0 ? minimum + "–" + maximum + " 格（近身死区）" : maximum + " 格";

        public static string BuildItemDetails(ItemDefinition definition, ItemInstance item, int slot)
        {
            if (definition == null || item == null) return "快捷栏 " + (slot + 1) + "　空槽";
            string uses = definition.MaximumUses > 0 ? "剩余 " + item.RemainingUses + "　总计 " + definition.MaximumUses : "无限制";
            return string.Join("\n",
                definition.Description,
                "类别：" + ItemCategoryLabel(definition.Category) + "　稀有度：" + ItemRarityLabel(definition.Rarity),
                "占格 " + definition.Width + "×" + definition.Height + "　重量 " + definition.Weight,
                "快捷栏 " + (slot + 1) + "\n次数 " + uses);
        }

        public static string PhaseText(CombatFlowPhase phase, CombatState state)
        {
            if (phase == CombatFlowPhase.Victory) return "战斗胜利";
            if (phase == CombatFlowPhase.Defeat) return "战斗失败";
            if (phase == CombatFlowPhase.TacticalRestart) return "正在重新部署";
            if (phase != CombatFlowPhase.Active) return "战斗即将开始";
            UnitState active = state?.GetUnit(state.ActiveUnitId);
            return active?.IsHero == true ? "你的行动\n选择指令" : "敌方行动";
        }

        public static string CommandSignature(CombatCommand command) => string.Join("|", command.Type, command.UnitId ?? string.Empty,
            command.TargetUnitId ?? string.Empty, command.Destination.X, command.Destination.Y, command.AimDirection, command.SlotIndex);

        public static string DamageBreakdown(CombatResolver.AttackPreview preview) => "基础 " + preview.BaseDamage +
            " - 掩体 " + preview.CoverReduction + " - 护甲 " + preview.ArmorReduction +
            " - 格挡 " + preview.BlockReduction + "　护盾吸收 " + preview.ShieldAbsorption + "　生命伤害 " + preview.FinalDamage;

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
            return (source?.DisplayName ?? command.UnitId) + "　" + action + " → " + (target?.DisplayName ?? "战场") + "\n" + string.Join("；", changes);
        }

        private static string SkillResult(SkillDefinition skill, CombatResolver.AttackPreview? damagePreview = null)
        {
            string effects = string.Join("、", skill.Effects.Select(effect =>
            {
                switch (effect.Type)
                {
                    case SkillEffectType.Damage:
                        return damagePreview.HasValue ? IncomingDamageSummary(damagePreview.Value) : "造成 " + effect.Amount + " 点伤害";
                    case SkillEffectType.RestoreHealth: return "恢复 " + effect.Amount + " 点生命";
                    case SkillEffectType.RestoreShield: return "恢复 " + effect.Amount + " 点护盾";
                    case SkillEffectType.RestoreMana: return "恢复 " + effect.Amount + " 点以太";
                    case SkillEffectType.ApplyStatus: return "施加" + StatusLabel(effect.Status) + " " + effect.Duration + " 回合";
                    case SkillEffectType.ClearStatus: return "清除" + StatusLabel(effect.Status);
                    case SkillEffectType.MoveSource: return "移动 " + effect.Amount + " 格";
                    case SkillEffectType.DamageObject: return "对物件造成 " + effect.Amount + " 点耐久伤害";
                    default: return "产生战斗效果";
                }
            }));
            return string.IsNullOrEmpty(effects) ? "选好目标后施放" : effects;
        }

        public static string BuildRogueliteHeroDetails(UnitState hero) => BuildRogueliteHeroDetails(null, hero);

        public static string BuildRogueliteHeroDetails(CombatState state, UnitState hero)
        {
            if (hero == null) return "英雄状态尚未就绪。";
            string statuses = hero.Statuses.Count == 0 ? "状态：正常" : "状态：" + string.Join("；", hero.Statuses.OrderBy(pair => pair.Key).Select(pair => StatusLabel(pair.Key) + " " + pair.Value));
            List<string> lines = new List<string>
            {
                "生命当前 " + hero.Health + "　上限 " + hero.MaxHealth + "\n普通盾 " + hero.Shield + "　个人魔力当前 " + hero.Mana + "　上限 " + hero.MaxMana,
                "行动点 " + hero.ActionPoints + "　速度 " + hero.EffectiveSpeed,
                statuses
            };
            if (state != null)
            {
                IReadOnlyList<CombatStatusBarEntry> entries = state.PassiveEffects.StatusBarEntriesFor(hero.Id);
                CombatStatusBarEntry[] ongoing = entries.Where(value => value.Kind == CombatStatusBarEntryKind.OngoingEffect).ToArray();
                CombatStatusBarEntry[] passives = entries.Where(value => value.Kind == CombatStatusBarEntryKind.Passive).ToArray();
                if (ongoing.Length > 0)
                    lines.Add("待触发：" + string.Join("；", ongoing.Select(value => value.DisplayName + "（" + value.TimingText + "）—" + value.Detail)));
                if (passives.Length > 0)
                    lines.Add("被动来源：" + string.Join("；", passives.Select(value => value.DisplayName + "（" + value.TimingText + "）—" + value.Detail)));
            }
            return string.Join("\n", lines);
        }

        private static int IncomingDamage(CombatResolver.AttackPreview preview) =>
            Math.Max(0, preview.ShieldAbsorption + preview.FinalDamage);

        private static string IncomingDamageSummary(CombatResolver.AttackPreview preview)
        {
            int total = IncomingDamage(preview);
            if (total <= 0) return "预计不会造成伤害";
            if (preview.ShieldAbsorption > 0 && preview.FinalDamage > 0)
                return "预计造成 " + total + " 点伤害（护盾 " + preview.ShieldAbsorption + "，生命 " + preview.FinalDamage + "）";
            if (preview.ShieldAbsorption > 0) return "预计造成 " + total + " 点护盾伤害";
            return "预计造成 " + total + " 点生命伤害";
        }

        private static string StatusLabel(StatusType status)
        {
            switch (status)
            {
                case StatusType.Burning: return "燃烧";
                case StatusType.Bound: return "束缚";
                case StatusType.Slow: return "迟缓";
                case StatusType.ArmorBreak: return "破甲";
                case StatusType.FiregroundBoost: return "火势";
                case StatusType.FiregroundVulnerable: return "助燃";
                default: return status.ToString();
            }
        }

        private static string ItemCategoryLabel(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Weapon: return "武器";
                case ItemCategory.Armor: return "护具";
                case ItemCategory.Consumable: return "消耗品";
                case ItemCategory.Scroll: return "卷轴";
                case ItemCategory.Artifact: return "法宝";
                case ItemCategory.Material: return "材料";
                case ItemCategory.Quest: return "任务物品";
                case ItemCategory.Container: return "容器";
                default: return category.ToString();
            }
        }

        private static string ItemRarityLabel(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return "普通";
                case ItemRarity.Uncommon: return "优良";
                case ItemRarity.Rare: return "稀有";
                case ItemRarity.Exceptional: return "卓越";
                default: return rarity.ToString();
            }
        }

        private static string Cell(GridPosition position) => "(" + position.X + "," + position.Y + ")";
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
