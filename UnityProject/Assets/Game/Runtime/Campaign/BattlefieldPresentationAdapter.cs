using System;
using System.Linq;

namespace OCC.Combat
{
    public sealed class CombatActionPreview
    {
        public string Action { get; }
        public string TargetRule { get; }
        public string Cost { get; }
        public string ExpectedResult { get; }
        public int ValidCellCount { get; }
        public string FailureReason { get; }
        public string TargetBefore { get; }
        public string TargetAfter { get; }
        public string DamageBreakdown { get; }
        public string StatusResults { get; }
        public int AffectedCellCount { get; }
        public bool FriendlyFireRisk { get; }
        public bool CanSubmit => string.IsNullOrEmpty(FailureReason);

        public CombatActionPreview(string action, string targetRule, string cost, string expectedResult, int validCellCount, string failureReason,
            string targetBefore = "", string targetAfter = "", string damageBreakdown = "", string statusResults = "", int affectedCellCount = 0, bool friendlyFireRisk = false)
        {
            Action = action ?? string.Empty;
            TargetRule = targetRule ?? string.Empty;
            Cost = cost ?? string.Empty;
            ExpectedResult = expectedResult ?? string.Empty;
            ValidCellCount = validCellCount;
            FailureReason = failureReason ?? string.Empty;
            TargetBefore = targetBefore ?? string.Empty;
            TargetAfter = targetAfter ?? string.Empty;
            DamageBreakdown = damageBreakdown ?? string.Empty;
            StatusResults = statusResults ?? string.Empty;
            AffectedCellCount = Math.Max(0, affectedCellCount);
            FriendlyFireRisk = friendlyFireRisk;
        }
    }

    public readonly struct BattlefieldRect
    {
        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }
        public float XMax => X + Width;
        public float YMax => Y + Height;

        public BattlefieldRect(float x, float y, float width, float height)
        {
            X = x; Y = y; Width = width; Height = height;
        }

        public bool Contains(float x, float y) => x >= X && x < XMax && y >= Y && y < YMax;
    }

    public sealed class BattlefieldPresentationAdapter
    {
        public const float CellSize = 78f;
        public const float BattlefieldWidth = 1440f;
        public const float BoardTop = 112f;
        public const int DefaultWidth = 12;
        public const int DefaultHeight = 9;

        public BattlefieldRect BoardRect(int width = DefaultWidth, int height = DefaultHeight)
        {
            float left = (BattlefieldWidth - width * CellSize) * .5f;
            return new BattlefieldRect(left, BoardTop, width * CellSize, height * CellSize);
        }

        public BattlefieldRect CellRect(BattlefieldRect board, int mapHeight, GridPosition position)
        {
            return new BattlefieldRect(board.X + position.X * CellSize, board.Y + (mapHeight - 1 - position.Y) * CellSize, CellSize - 2f, CellSize - 2f);
        }

        public bool TryResolveCell(BattlefieldRect board, int mapWidth, int mapHeight, float pointX, float pointY, out GridPosition position)
        {
            position = default;
            if (!board.Contains(pointX, pointY)) return false;
            int x = (int)Math.Floor((pointX - board.X) / CellSize);
            int visualY = (int)Math.Floor((pointY - board.Y) / CellSize);
            int y = mapHeight - 1 - visualY;
            if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight) return false;
            GridPosition candidate = new GridPosition(x, y);
            if (!CellRect(board, mapHeight, candidate).Contains(pointX, pointY)) return false;
            position = candidate;
            return true;
        }

        public bool IsInSelectedRange(CombatState state, string action, GridPosition position)
        {
            if (state == null || state.ActiveUnitId != "hero") return false;
            UnitState hero = state.GetUnit("hero");
            int distance = Distance(hero.Position, position);
            if (action == "移动") return IsInMoveRange(state, position);
            if (action == "攻击") return IsInAttackRange(state, position);
            if (action == "技能1") return IsSkillTargetInRange(state, hero.SkillOne, position);
            if (action == "技能2") return IsSkillTargetInRange(state, hero.SkillTwo, position);
            if (action == "搜刮") return state.Loot != null && !state.Loot.IsLooted && position == state.Loot.Position && distance == 1;
            if (action == "互动") return distance == 1 && HasInteractionTarget(state, position);
            return false;
        }

        public CombatActionPreview BuildPreview(CombatState state, string action, string selectedTargetId)
        {
            if (state == null) return new CombatActionPreview(action, "等待战斗状态", "--", "--", 0, "战斗状态尚未就绪");
            UnitState hero = state.GetUnit("hero");
            string globalFailure = state.IsVictory || state.IsDefeat ? "战斗已经结束" : state.ActiveUnitId != "hero" ? "等待敌方行动结束" : hero.ActionPoints < CombatResolver.BasicActionPointCost ? "行动点不足" : string.Empty;
            string targetRule = TargetRule(action, hero);
            string cost = Cost(action, hero);
            string expected = ExpectedResult(state, action, hero, selectedTargetId);
            int validCells = CountValidCells(state, action);
            string failure = globalFailure;

            if (string.IsNullOrEmpty(failure) && action == "移动" && hero.HasStatus(StatusType.Bound)) failure = "束缚状态下无法移动";
            if (string.IsNullOrEmpty(failure) && (action == "技能1" || action == "技能2"))
            {
                SkillDefinition skill = action == "技能1" ? hero.SkillOne : hero.SkillTwo;
                if (skill == null) failure = "未装备该技能";
                else if (hero.Cooldown(skill) > 0) failure = skill.DisplayName + "冷却 " + hero.Cooldown(skill) + " 回合";
                else if (hero.Mana < skill.ManaCost) failure = "以太不足：需要 " + skill.ManaCost;
                else if (RequiresUnitTarget(skill) && string.IsNullOrEmpty(selectedTargetId)) failure = "请选择有效目标";
            }
            if (string.IsNullOrEmpty(failure) && action == "攻击" && string.IsNullOrEmpty(selectedTargetId)) failure = "请选择射程与视线内的敌人";
            if (string.IsNullOrEmpty(failure) && action == "攻击" && !string.IsNullOrEmpty(selectedTargetId))
            {
                UnitState target = state.GetUnit(selectedTargetId);
                if (target == null || !target.IsAlive || target.IsHero) failure = "锁定目标不可用";
                else if (Distance(hero.Position, target.Position) > hero.MainHand.Range) failure = "目标超出武器射程";
                else if (!state.Map.HasLineOfSight(hero.Position, target.Position)) failure = "重掩体阻挡了射线";
            }
            if (string.IsNullOrEmpty(failure) && (action == "技能1" || action == "技能2") && !string.IsNullOrEmpty(selectedTargetId))
            {
                SkillDefinition skill = action == "技能1" ? hero.SkillOne : hero.SkillTwo;
                UnitState target = state.GetUnit(selectedTargetId);
                if (skill != null && RequiresUnitTarget(skill) && (target == null || !IsSkillTargetInRange(state, skill, target.Position))) failure = "技能目标不符合范围、关系或视线规则";
            }
            if (string.IsNullOrEmpty(failure) && action == "搜刮")
            {
                if (state.Loot == null || state.Loot.IsLooted) failure = "现场没有可搜刮战利品";
                else if (!state.Backpack.CanAdd(state.Loot.Item)) failure = "背包已满，需要先调整物品";
                else if (Distance(hero.Position, state.Loot.Position) != 1) failure = "战利品不在相邻格";
            }
            if (string.IsNullOrEmpty(failure) && validCells == 0 && action != "结束行动") failure = "当前没有有效目标格";
            string before = string.Empty, after = string.Empty, breakdown = string.Empty, statuses = string.Empty;
            int affected = 0;
            UnitState exactTarget = string.IsNullOrEmpty(selectedTargetId) ? null : state.GetUnit(selectedTargetId);
            if (exactTarget != null && action == "攻击")
            {
                CombatResolver.AttackPreview damage = CombatResolver.PreviewAttack(state, hero.Id, exactTarget.Id, false);
                before = "生命 " + exactTarget.Health + " // 护盾 " + exactTarget.Shield;
                after = "生命 " + Math.Max(0, exactTarget.Health - damage.FinalDamage) + " // 护盾 " + Math.Max(0, exactTarget.Shield - damage.ShieldAbsorption);
                breakdown = CombatInformationPresenter.DamageBreakdown(damage);
                affected = 1;
            }
            else if (exactTarget != null && (action == "技能1" || action == "技能2"))
            {
                SkillDefinition skill = action == "技能1" ? hero.SkillOne : hero.SkillTwo;
                before = "生命 " + exactTarget.Health + " // 护盾 " + exactTarget.Shield;
                if (skill != null && skill.Damage > 0)
                {
                    CombatResolver.AttackPreview damage = CombatResolver.PreviewSkillAttack(state, hero.Id, exactTarget.Id, skill);
                    after = "生命 " + Math.Max(0, exactTarget.Health - damage.FinalDamage) + " // 护盾 " + Math.Max(0, exactTarget.Shield - damage.ShieldAbsorption);
                    breakdown = CombatInformationPresenter.DamageBreakdown(damage);
                }
                statuses = skill == null ? string.Empty : string.Join("、", skill.Effects.Where(effect => effect.Type == SkillEffectType.ApplyStatus).Select(EffectLabel));
                affected = 1;
            }
            return new CombatActionPreview(action, targetRule, cost, expected, validCells, failure, before, after, breakdown, statuses, affected, false);
        }

        public string InvalidReasonForCell(CombatState state, string action, GridPosition position)
        {
            CombatActionPreview preview = BuildPreview(state, action, null);
            string global = state == null || state.IsVictory || state.IsDefeat || state.ActiveUnitId != "hero" || state.GetUnit("hero").ActionPoints < CombatResolver.BasicActionPointCost ? preview.FailureReason : string.Empty;
            if (!string.IsNullOrEmpty(global)) return global;
            if (!state.Map.IsInside(position)) return "目标格超出地图范围";
            UnitState hero = state.GetUnit("hero");
            int distance = Distance(hero.Position, position);
            UnitState target = state.Units.Values.FirstOrDefault(unit => unit.IsAlive && unit.Position == position);
            if (action == "移动")
            {
                if (hero.HasStatus(StatusType.Bound)) return "束缚状态下无法移动";
                if (distance == 0) return "主角已在该格";
                if (distance > hero.MovementRangeThisTurn) return "目标格超出移动范围";
                if (state.Map.IsBlocked(position)) return "目标格被阻挡";
                if (state.IsOccupied(position, hero.Id)) return "目标格已被单位占据";
            }
            else if (action == "攻击")
            {
                if (target == null || target.IsHero) return "当前格没有可攻击目标";
                if (distance > hero.MainHand.Range) return "目标超出武器射程";
                if (!state.Map.HasLineOfSight(hero.Position, position)) return "重掩体阻挡了射线";
            }
            else if (action == "技能1" || action == "技能2")
            {
                SkillDefinition skill = action == "技能1" ? hero.SkillOne : hero.SkillTwo;
                if (skill == null) return "未装备该技能";
                if (hero.Cooldown(skill) > 0) return skill.DisplayName + "冷却 " + hero.Cooldown(skill) + " 回合";
                if (hero.Mana < skill.ManaCost) return "以太不足：需要 " + skill.ManaCost;
                if (!IsSkillTargetInRange(state, skill, position)) return SkillInvalidReason(state, hero, skill, position, target);
            }
            else if (action == "搜刮")
            {
                if (state.Loot == null || state.Loot.IsLooted) return "现场没有可搜刮战利品";
                if (position != state.Loot.Position) return "请选择战利品所在格";
                if (distance != 1) return "只能搜刮相邻格的战利品";
                if (!state.Backpack.CanAdd(state.Loot.Item)) return "背包已满，需要先调整物品";
            }
            else if (action == "互动")
            {
                if (distance != 1) return "只能与相邻格互动";
                if (!HasInteractionTarget(state, position)) return "该格没有可互动目标";
            }
            return string.Empty;
        }

        private int CountValidCells(CombatState state, string action)
        {
            if (action == "结束行动") return 1;
            int count = 0;
            for (int y = 0; y < state.Map.Height; y++)
                for (int x = 0; x < state.Map.Width; x++)
                    if (IsInSelectedRange(state, action, new GridPosition(x, y))) count++;
            return count;
        }

        private static string TargetRule(string action, UnitState hero)
        {
            if (action == "移动") return "选择 3 格内可通行空格";
            if (action == "攻击") return "选择 " + hero.MainHand.Range + " 格内可见敌人";
            if (action == "技能1" || action == "技能2")
            {
                SkillDefinition skill = action == "技能1" ? hero.SkillOne : hero.SkillTwo;
                return skill == null ? "未装备" : SkillTargetRuleLabel(skill) + " / " + skill.Range + " 格";
            }
            if (action == "搜刮") return "选择相邻战利品格";
            if (action == "互动") return "选择相邻目标/调查格";
            return "立即结束当前单位行动";
        }

        private static string Cost(string action, UnitState hero)
        {
            if (action == "结束行动") return "剩余 AP 全部放弃";
            if (action == "技能1" || action == "技能2")
            {
                SkillDefinition skill = action == "技能1" ? hero.SkillOne : hero.SkillTwo;
                return "1 AP" + (skill != null && skill.ManaCost > 0 ? " + " + skill.ManaCost + " 以太" : string.Empty);
            }
            return "1 AP";
        }

        private static string ExpectedResult(CombatState state, string action, UnitState hero, string selectedTargetId)
        {
            UnitState target = string.IsNullOrEmpty(selectedTargetId) ? null : state.GetUnit(selectedTargetId);
            if (action == "移动") return "移动并朝向目标格；无随机判定";
            if (action == "攻击") return target == null ? "命中后按护盾→护甲→格挡确定性结算" : DamageSummary(CombatResolver.PreviewAttack(state, hero.Id, target.Id, false));
            if (action == "技能1" || action == "技能2")
            {
                SkillDefinition skill = action == "技能1" ? hero.SkillOne : hero.SkillTwo;
                if (skill == null) return "无效果";
                string effects = string.Join("、", skill.Effects.Select(EffectLabel));
                if (target != null && skill.Damage > 0) effects = DamageSummary(CombatResolver.PreviewSkillAttack(state, hero.Id, target.Id, skill)) + (string.IsNullOrEmpty(effects) ? string.Empty : "；" + effects);
                return string.IsNullOrEmpty(effects) ? "效果将确定性结算" : effects;
            }
            if (action == "搜刮") return state.Loot == null || state.Loot.IsLooted ? "无战利品" : "获得 " + state.Loot.Item.DisplayName + "；无随机判定";
            if (action == "互动") return "调查或对物件造成 3 点耐久伤害";
            return "推进至下一行动单位";
        }

        private static string DamageSummary(CombatResolver.AttackPreview preview) => "预计生命 -" + preview.FinalDamage + " / 护盾 -" + preview.ShieldAbsorption + " / 减伤 " + (preview.CoverReduction + preview.ArmorReduction + preview.BlockReduction);
        private static string EffectLabel(SkillEffectDefinition effect) => effect.Type == SkillEffectType.Damage ? effect.Amount + " 基础伤害" : effect.Type == SkillEffectType.RestoreHealth ? "生命 +" + effect.Amount : effect.Type == SkillEffectType.RestoreShield ? "护盾 +" + effect.Amount : effect.Type == SkillEffectType.RestoreMana ? "以太 +" + effect.Amount : effect.Type == SkillEffectType.ApplyStatus ? "施加 " + effect.Status + " " + effect.Duration : effect.Type == SkillEffectType.ClearStatus ? "清除 " + effect.Status : effect.Type == SkillEffectType.DamageObject ? "物件耐久 -" + effect.Amount : "位移";
        private static string SkillTargetRuleLabel(SkillDefinition skill) => skill.TargetRule == SkillTargetRule.Self ? "自身" : skill.TargetRule == SkillTargetRule.EnemyUnit ? "敌方单位" : skill.TargetRule == SkillTargetRule.AllyUnit ? "友方单位" : skill.TargetRule == SkillTargetRule.AnyUnit ? "任意单位" : skill.TargetRule == SkillTargetRule.Destructible ? "可破坏物" : "空地格";
        private static bool RequiresUnitTarget(SkillDefinition skill) => skill.TargetRule == SkillTargetRule.EnemyUnit || skill.TargetRule == SkillTargetRule.AllyUnit || skill.TargetRule == SkillTargetRule.AnyUnit;

        private static bool HasInteractionTarget(CombatState state, GridPosition position)
        {
            TileState tile = state.Map.GetTile(position);
            return tile.IsObjective || tile.Cover != CoverType.None || state.Objectives.OfType<InvestigationObjective>().Any(objective => objective.Positions.Contains(position));
        }

        private static string SkillInvalidReason(CombatState state, UnitState hero, SkillDefinition skill, GridPosition position, UnitState target)
        {
            int distance = Distance(hero.Position, position);
            if (distance > skill.Range) return "目标超出技能射程";
            if (skill.TargetRule == SkillTargetRule.GridCell && state.Map.IsBlocked(position)) return "目标格被阻挡";
            if (skill.TargetRule == SkillTargetRule.GridCell && state.IsOccupied(position, hero.Id)) return "目标格已被占据";
            if (skill.TargetRule == SkillTargetRule.Destructible) return "目标格没有可破坏物件";
            if (RequiresUnitTarget(skill) && target == null) return "当前格没有技能目标";
            if (skill.Range > 1 && !skill.HasModifier(SkillModifierType.IgnoreLineOfSight) && !state.Map.HasLineOfSight(hero.Position, position)) return "重掩体阻挡了技能投递";
            return "技能目标不符合规则";
        }

        public bool IsSkillTargetInRange(CombatState state, SkillDefinition skill, GridPosition position)
        {
            if (state == null || skill == null || !state.Map.IsInside(position)) return false;
            UnitState hero = state.GetUnit("hero");
            int distance = Distance(hero.Position, position);
            if (skill.TargetRule == SkillTargetRule.Self) return position == hero.Position;
            if (distance > skill.Range) return false;
            if (skill.TargetRule == SkillTargetRule.GridCell) return distance > 0 && !state.Map.IsBlocked(position) && !state.IsOccupied(position, hero.Id);
            if (skill.TargetRule == SkillTargetRule.Destructible)
            {
                TileState tile = state.Map.GetTile(position);
                return tile.Cover != CoverType.None || tile.IsObjective;
            }
            UnitState target = state.Units.Values.FirstOrDefault(unit => unit.IsAlive && unit.Position == position);
            if (target == null) return false;
            bool relation = skill.TargetRule == SkillTargetRule.AnyUnit || (skill.TargetRule == SkillTargetRule.EnemyUnit && !target.IsHero) || (skill.TargetRule == SkillTargetRule.AllyUnit && target.IsHero);
            return relation && (distance <= 1 || skill.HasModifier(SkillModifierType.IgnoreLineOfSight) || state.Map.HasLineOfSight(hero.Position, position));
        }

        public bool IsInMoveRange(CombatState state, GridPosition position)
        {
            if (state == null || state.ActiveUnitId != "hero") return false;
            UnitState hero = state.GetUnit("hero");
            int distance = Distance(hero.Position, position);
            return distance > 0 && distance <= hero.MovementRangeThisTurn && state.Map.IsInside(position) && !state.Map.IsBlocked(position) && !state.IsOccupied(position);
        }

        public bool IsInAttackRange(CombatState state, GridPosition position)
        {
            if (state == null || state.ActiveUnitId != "hero") return false;
            UnitState hero = state.GetUnit("hero");
            int distance = Distance(hero.Position, position);
            return distance > 0 && distance <= hero.MainHand.Range && state.Map.HasLineOfSight(hero.Position, position);
        }

        public static int Distance(GridPosition from, GridPosition to) => Math.Abs(from.X - to.X) + Math.Abs(from.Y - to.Y);
        public static GridPosition StepToward(GridPosition from, GridPosition to) => Math.Abs(to.X - from.X) >= Math.Abs(to.Y - from.Y) ? new GridPosition(from.X + Math.Sign(to.X - from.X), from.Y) : new GridPosition(from.X, from.Y + Math.Sign(to.Y - from.Y));
        public static Facing FacingToward(GridPosition from, GridPosition to) => Math.Abs(to.X - from.X) >= Math.Abs(to.Y - from.Y) ? (to.X >= from.X ? Facing.East : Facing.West) : (to.Y >= from.Y ? Facing.North : Facing.South);
    }
}
