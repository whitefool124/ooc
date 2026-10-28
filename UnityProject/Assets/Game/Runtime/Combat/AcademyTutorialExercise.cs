using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    // Pure, disposable examples use the production rule owners. No save store or live-run reference.
    public sealed class AcademyTutorialExercise
    {
        private readonly Func<AcademyTutorialExerciseState> build;
        private AcademyTutorialExerciseState state;
        private string expected;
        public string LessonId { get; }
        public CombatState Combat => state.Combat;
        public bool Executed { get; private set; }
        public bool MatchedPreview { get; private set; }
        public string Before { get; private set; }
        public string Result { get; private set; }
        public string Preview { get; private set; }
        public string Instruction { get; }
        public IReadOnlyList<string> Proofs { get; }

        private AcademyTutorialExercise(string id, string instruction, Func<AcademyTutorialExerciseState> factory, params string[] proofs)
        {
            LessonId = id; Instruction = instruction; build = factory; Proofs = proofs;
            state = build(); Before = Describe(state); var preview = build();
            preview.Apply(); expected = Signature(preview); Preview = Difference(state, preview);
        }
        public void Execute()
        {
            if (Executed) throw new InvalidOperationException("This practice was already submitted.");
            var before = build(); state.Apply(); Executed = true;
            MatchedPreview = Signature(state) == expected;
            Result = Difference(before, state);
            if (!MatchedPreview) throw new InvalidOperationException("Teaching preview differs from the production result.");
        }
        public static bool CanPrepare(string id) => BasicEffects.ContainsKey(id) || FireExamples.ContainsKey(id) || FieldEffects.ContainsKey(id);
        public static AcademyTutorialExercise Prepare(string id)
        {
            if (BasicEffects.TryGetValue(id, out var effect))
                return new AcademyTutorialExercise(id, "查看高亮对象和预计变化，再执行一次。", () =>
                {
                    var combat = CreateSimple();
                    if (id == "X13")
                    {
                        var hero = combat.GetUnit("hero");
                        hero.Equip(hero.MainHand, CombatCatalog.FrostBind, hero.SkillTwo);
                        hero.SetCooldown(hero.SkillOne);
                    }
                    return new AcademyTutorialExerciseState(combat, () => effect(combat));
                }, "effect-preview", "effect-commit", "effect-result");
            if (FieldEffects.TryGetValue(id, out var fieldEffect))
                return new AcademyTutorialExercise(id, "查看格子的来源与持续时间，再操作一次。", () =>
                {
                    var combat = CreateSimple(); var fire = new FireBattleState(combat);
                    if (id == "X15") fire.CreateOrRefreshFireground(new GridPosition(2, 2), 8, 2, "teaching", "hero");
                    return new AcademyTutorialExerciseState(combat, () => fieldEffect(fire), fire);
                }, "effect-preview", "effect-commit", "effect-result");
            if (FireExamples.TryGetValue(id, out string ability))
                return new AcademyTutorialExercise(id, "这是一例独立规则演示。查看作用范围，再执行。", () =>
                {
                    ITrainingRangeCase example = new FireSpellTrainingRangeProvider().Prepare(ability);
                    if (!example.Preview().CanCommit) throw new InvalidOperationException("Teaching example has no legal preview: " + id);
                    return new AcademyTutorialExerciseState(example.Combat, () => example.Execute(), (example as FireSpellTrainingRangeCase)?.Battle);
                }, "effect-preview", "effect-commit", "effect-result");
            throw new InvalidOperationException("No verified practice binding: " + id);
        }
        private static readonly Dictionary<string, Action<CombatState>> BasicEffects = new Dictionary<string, Action<CombatState>>
        {
            ["X01"] = s => CombatEffectExecutor.Execute(s, "hero", CombatEffect.DamageHealth("target", 4)),
            ["X02"] = s => CombatEffectExecutor.Execute(s, "hero", CombatEffect.RestoreShield("hero", 3, "teaching")),
            ["X03"] = s => CombatEffectExecutor.Execute(s, "hero", CombatEffect.RestoreMana("hero", 20)),
            ["X04"] = s => s.ResolveForcedMove(s.GetUnit("target"), new GridPosition(1, 0), 1, "hero"),
            ["X05"] = s => s.ResolveForcedMove(s.GetUnit("target"), new GridPosition(-1, 0), 1, "hero"),
            ["X09"] = s => s.GetUnit("hero").GrantActionPoints(1),
            ["X10"] = s => s.GetUnit("target").ChangeActionValue(-20),
            ["X11"] = s => CombatEffectExecutor.Execute(s, "hero", CombatEffect.ApplyStatus("target", StatusType.Slow, 2)),
            ["X12"] = s => CombatEffectExecutor.Execute(s, "hero", CombatEffect.ClearStatus("hero", StatusType.Burning)),
            ["X13"] = s => s.GetUnit("hero").ReduceCooldowns(1)
        };
        private static readonly Dictionary<string, string> FireExamples = new Dictionary<string, string>
        {
            ["X06"] = "F-P-M04", ["X07"] = "F-P-U18"
        };
        private static readonly Dictionary<string, Action<FireBattleState>> FieldEffects = new Dictionary<string, Action<FireBattleState>>
        {
            ["X14"] = f => f.CreateOrRefreshFireground(new GridPosition(2, 2), 8, 2, "teaching", "hero"),
            ["X15"] = f => f.ClearEffectLayer(new GridPosition(2, 2))
        };
        private static CombatState CreateSimple()
        {
            var hero = new UnitState("hero", true, new GridPosition(1, 2)) { DisplayName = "演练者" };
            hero.ConfigureMana(12, 5); hero.BeginTurn(1); hero.ClearShield();
            hero.ApplyStatus(StatusType.Burning, 2, 2, "teaching");
            var target = new UnitState("target", false, new GridPosition(3, 2)) { DisplayName = "演练靶", Armor = 0 };
            target.ConfigureVitality(20); target.ClearShield(); target.SetActionValue(80);
            var combat = new CombatState(new GridMap(6, 5), new[] { hero, target }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite); return combat;
        }
        private static string Signature(AcademyTutorialExerciseState value) => Describe(value) + "|" +
            string.Join(";", value.Combat.Units.Values.OrderBy(u => u.Id).Select(u => string.Join(",", u.Statuses.Select(s => s.Key + ":" + s.Value)))) + "|" +
            (value.Fire == null ? "" : string.Join(";", value.Fire.Firegrounds.OrderBy(p => p.Key.X).ThenBy(p => p.Key.Y).Select(p => p.Key + ":" + p.Value.RemainingTurns))) ;
        private static string Describe(AcademyTutorialExerciseState value) => string.Join("\n", value.Combat.Units.Values.Select(UnitSummary)) +
            (value.Fire == null ? "" : "\n场地 " + value.Fire.Firegrounds.Count + "　待触发 " + value.Fire.PendingEffects.Count);
        private static string UnitSummary(UnitState u) => u.DisplayName + "　生命 " + u.Health + "　盾 " + u.Shield + "　魔力 " + u.Mana + "　上限 " + u.MaxMana + "　行动点 " + u.ActionPoints + "　行动值 " + u.ActionValue + "　步数 " + u.MovementRangeThisTurn + "　冷却 " + u.Cooldown(u.SkillOne) + "　格 " + u.Position;
        private static string Difference(AcademyTutorialExerciseState before, AcademyTutorialExerciseState after)
        {
            var lines = new List<string>();
            foreach (var unit in after.Combat.Units.Values)
            {
                var old = before.Combat.GetUnit(unit.Id);
                if (old.Health != unit.Health) lines.Add(unit.DisplayName + "　生命 " + old.Health + " → " + unit.Health);
                if (old.Shield != unit.Shield) lines.Add(unit.DisplayName + "　护盾 " + old.Shield + " → " + unit.Shield);
                if (old.Mana != unit.Mana) lines.Add(unit.DisplayName + "　魔力 " + old.Mana + " → " + unit.Mana + "　上限 " + unit.MaxMana);
                if (old.ActionPoints != unit.ActionPoints) lines.Add(unit.DisplayName + "　行动点 " + old.ActionPoints + " → " + unit.ActionPoints);
                if (old.ActionValue != unit.ActionValue) lines.Add(unit.DisplayName + "　行动值 " + old.ActionValue + " → " + unit.ActionValue);
                if (old.MovementRangeThisTurn != unit.MovementRangeThisTurn) lines.Add(unit.DisplayName + "　步数 " + old.MovementRangeThisTurn + " → " + unit.MovementRangeThisTurn);
                if (old.Cooldown(old.SkillOne) != unit.Cooldown(unit.SkillOne)) lines.Add(unit.DisplayName + "　冷却 " + old.Cooldown(old.SkillOne) + " → " + unit.Cooldown(unit.SkillOne));
                if (old.Position != unit.Position) lines.Add(unit.DisplayName + "　位置 " + old.Position + " → " + unit.Position);
                foreach (StatusType type in Enum.GetValues(typeof(StatusType)))
                    if (old.StatusDuration(type) != unit.StatusDuration(type))
                        lines.Add(unit.DisplayName + "　" + CombatInformationPresenter.StatusLabel(type) + " " + old.StatusDuration(type) + " → " + unit.StatusDuration(type));
            }
            if (before.Fire != null && after.Fire != null && before.Fire.Firegrounds.Count != after.Fire.Firegrounds.Count)
                lines.Add("场地格数 " + before.Fire.Firegrounds.Count + " → " + after.Fire.Firegrounds.Count);
            return string.Join("\n", lines.Take(8));
        }
    }
    public sealed class AcademyTutorialExerciseState
    {
        public CombatState Combat { get; }
        public FireBattleState Fire { get; }
        private readonly Action apply;
        public AcademyTutorialExerciseState(CombatState combat, Action operation, FireBattleState fire = null)
        { Combat = combat; apply = operation; Fire = fire; }
        public void Apply() => apply();
    }
}
