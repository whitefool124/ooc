using System;
using System.Linq;
namespace OCC.Combat
{
    public sealed partial class AcademyTutorialModelExercise
    {
        private static FireSpellDefinition RuleGeometry(string id, FireSelectionShape shape, bool friendly = false)
            => new FireSpellDefinition(id, "几何规则演练", FireSpellRarity.Common, FireSpellGroup.Universal,
                0, 0, 0, 0, 6, FireTargetKind.Cell, shape, 4, false, true,
                new[] { new FireSpellRule(FireRuleKind.Damage, 4, scope: FireRuleScope.Selection, affectAllies: friendly) });
        private static Model DeliveryVariant(string key)
        {
            if (key == "melee-preview") { var weapon = WeaponPayment(); weapon.Proofs = new[] { key }; weapon.ReadCombat = weapon.ReadCombat ?? (() => null); return weapon; }
            var hero = new UnitState("hero", true, new GridPosition(1, 1)); var first = new UnitState("first", false, new GridPosition(2, 1)); var far = new UnitState("far", false, new GridPosition(4, 1)); first.ConfigureVitality(99); far.ConfigureVitality(99);
            var state = new CombatState(new GridMap(7, 5), new[] { hero, first, far }); state.ConfigureRuleset(CombatRuleset.Roguelite); CombatResolver.BeginTurn(state, hero.Id); hero.ConfigureMana(12);
            var battle = new FireBattleState(state); bool launched = key == "launched-preview"; var spell = RuleGeometry("teaching-" + key, launched ? FireSelectionShape.FirstHitLine : FireSelectionShape.Single);
            var target = FireSpellTarget.At(far.Position, CardinalDirection.East); FireSpellPreview preview = null;
            return new Model
            {
                ReadCombat = () => state,
                Prompt = launched ? "发射沿路径先命中拦截者。检查当前作用格，不凭指向的远格判断实际目标。" : "投射在合法锚点直接结算。中途的单位不会自动拦截这一类投射。",
                Read = () => "近处演练靶生命 " + first.Health + "\n远处演练靶生命 " + far.Health + "\n预计对象 " + (preview == null ? "尚未展开" : string.Join("、", preview.UnitIds.Select(id => id == "first" ? "近处演练靶" : "远处演练靶"))),
                Steps = new Action[] { () => preview = FireSpellEngine.Preview(battle, hero.Id, spell, target), () => FireSpellEngine.Execute(battle, hero.Id, spell, target) },
                Labels = new[] { "查看本类动作的作用格", "执行并核对实际命中对象" },
                Verify = () => preview != null && preview.CanCommit && (launched ? first.Health == 95 && far.Health == 99 : first.Health == 99 && far.Health == 95),
                Fingerprint = () => first.Health + "|" + far.Health, Proofs = new[] { key, "intercept-result" }
            };
        }
        private static Model AreaAndAnchor()
        {
            var hero = new UnitState("hero", true, new GridPosition(0, 1)); var ally = new UnitState("ally", true, new GridPosition(1, 0)); var enemy = new UnitState("enemy", false, new GridPosition(1, 1));
            var state = new CombatState(new GridMap(5, 5), new[] { hero, ally, enemy }); state.ConfigureRuleset(CombatRuleset.Roguelite); CombatResolver.BeginTurn(state, hero.Id); hero.ConfigureMana(12);
            var battle = new FireBattleState(state); var spell = RuleGeometry("teaching-area", FireSelectionShape.Square3, true); var anchor = FireSpellTarget.At(new GridPosition(0, 0), CardinalDirection.East); FireSpellPreview preview = null;
            int allyHp = ally.Health, enemyHp = enemy.Health;
            return new Model
            {
                ReadCombat = () => state,
                Prompt = "锚点是选中的格子，作用范围是展开后的形状。越界部分裁掉，声明友伤的效果也会影响友方。",
                Read = () => "锚点 左下边界格\n展开作用格数 " + (preview?.Cells.Count ?? 0) + "\n友方生命 " + ally.Health + "　敌方生命 " + enemy.Health,
                Steps = new Action[] { () => preview = FireSpellEngine.Preview(battle, hero.Id, spell, anchor), () => FireSpellEngine.Execute(battle, hero.Id, spell, anchor) },
                Labels = new[] { "展开锚点与边界范围", "核对范围内双方的实际结果" },
                Verify = () => preview != null && preview.CanCommit && preview.Cells.Count == 4 && preview.Cells.All(state.Map.IsInside) && preview.FriendlyFireRisk && ally.Health == allyHp - 4 && enemy.Health == enemyHp - 4,
                Fingerprint = () => ally.Health + "|" + enemy.Health, Proofs = new[] { "anchor-preview", "area-preview", "clipped-area", "friendly-fire" }
            };
        }
    }
}
