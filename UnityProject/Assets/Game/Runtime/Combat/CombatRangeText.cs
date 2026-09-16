using System.Collections.Generic;

namespace OCC.Combat
{
    /// <summary>
    /// Single source of truth for range wording. 总案 3.5.1.1 requires every 术式 to record 选取范围
    /// (where the target or anchor may be placed) and 作用范围 (which cells are actually affected), and
    /// requires both to be written as 形状＋距离 rather than a bare reach number.
    /// Shape names come from the sanctioned vocabulary table: 直线／十字／X 形／菱形／方形／锥形／环形,
    /// plus the 自身·单点 forms and the 含中心·沿路径 qualifiers.
    /// Division of labour: 选取范围 is where the player may click, so an area shape's own geometry belongs
    /// to 作用范围; path spells follow 总案's "选取：直线范围 N 格；作用：沿路径" form instead.
    /// Lives in the shared runtime assembly so both the battle adapter and the settlement cards use it,
    /// and mirrors FireSpellRuntime.SelectCells so the wording cannot drift from resolution.
    /// </summary>
    public static class CombatRangeText
    {
        public const string SelfSelection = "自身";
        public const string DeadZoneNote = "（近身死区）";
        private const string PrimaryTargetCell = "目标格";
        private const string NeighbourCells = "目标格及正交邻格";
        private const string LandingNeighbours = "落点相邻 1 格";
        private const string SelectedCells = "选中格";

        /// <summary>选取 + 作用 in one line, e.g. “选取：单点 3 格内；作用：目标格＋正交邻接”.</summary>
        public static string RangeLine(FireSpellDefinition spell) =>
            spell == null ? "范围：—" : SelectionLine(spell) + "；" + EffectLine(spell);

        public static string RangeLine(SkillDefinition skill) =>
            skill == null ? "范围：—" : SelectionLine(skill) + "；" + EffectLine(skill);

        public static string SelectionLine(FireSpellDefinition spell)
        {
            if (spell == null) return "选取：—";
            if (spell.TargetKind == FireTargetKind.Self) return "选取：" + SelfSelection;
            if (spell.Shape == FireSelectionShape.Path)
                return "选取：" + LineExtent(spell.ShapeLength) + DeadZoneSuffix(spell.MinimumRange);
            return "选取：单点" + ReachText(spell.MinimumRange, spell.Range);
        }

        public static string EffectLine(FireSpellDefinition spell)
        {
            if (spell == null) return "作用：—";
            bool alongPath = spell.Shape == FireSelectionShape.Path;
            var names = new List<string>();
            if (alongPath) names.Add("沿路径");
            if (spell.Rules != null)
            {
                foreach (FireSpellRule rule in spell.Rules)
                {
                    string name = ScopeText(rule.Scope, spell.TargetKind);
                    if (name == null) continue;
                    // 选中格 is what a non-path area shape already describes below;
                    // 落点 is what 沿路径 already covers.
                    if (name == SelectedCells) continue;
                    if (alongPath && name == "落点") continue;
                    if (alongPath && name == NeighbourCells) name = LandingNeighbours;
                    if (!names.Contains(name)) names.Add(name);
                }
            }
            if (!alongPath && spell.Shape != FireSelectionShape.Single)
                names.Insert(0, ShapeText(spell.Shape, spell.ShapeLength));
            if (names.Contains(NeighbourCells)) names.Remove(PrimaryTargetCell);
            if (names.Count == 0) names.Add(spell.TargetKind == FireTargetKind.Self ? SelfSelection : PrimaryTargetCell);
            return "作用：" + string.Join("＋", names);
        }

        public static string SelectionLine(SkillDefinition skill)
        {
            if (skill == null) return "选取：—";
            if (skill.TargetRule == SkillTargetRule.Self) return "选取：" + SelfSelection;
            return "选取：单点" + ReachText(skill.MinimumRange, skill.Range);
        }

        public static string EffectLine(SkillDefinition skill)
        {
            if (skill == null) return "作用：—";
            if (skill.TargetRule == SkillTargetRule.Self) return "作用：" + SelfSelection;
            string anchor = skill.TargetRule == SkillTargetRule.GridCell ? "锚点格" : PrimaryTargetCell;
            return skill.Shape == FireSelectionShape.Single
                ? "作用：" + anchor
                : "作用：" + ShapeText(skill.Shape, skill.Range) + "＋" + anchor;
        }

        /// <summary>
        /// Sanctioned shape wording. <paramref name="extent"/> is the shape's own length, not the reach: the
        /// engine resolves Square3 as a 3×3 around the anchor, both ring shapes as the orthogonal neighbours
        /// of the anchor, 十字 as the anchor plus its orthogonal neighbours, and 锥形 as layers of width 2d−1
        /// — see FireSpellRuntime.SelectCells.
        /// </summary>
        public static string ShapeText(FireSelectionShape shape, int extent)
        {
            switch (shape)
            {
                case FireSelectionShape.Line:
                case FireSelectionShape.ContinuousLine:
                case FireSelectionShape.Path: return LineExtent(extent);
                case FireSelectionShape.Cone: return "锥形范围 " + extent + " 格";
                case FireSelectionShape.Cross:
                case FireSelectionShape.CenterAndOrthogonal: return "十字范围 1 格（含中心）";
                case FireSelectionShape.Square3: return "方形范围 1 格（含中心）";
                case FireSelectionShape.OrthogonalRing:
                case FireSelectionShape.AroundUnit: return "环形范围 1 格";
                default: return "单点";
            }
        }

        private static string LineExtent(int extent) => "直线范围 " + extent + " 格";

        /// <summary>“单点” and 自身 carry no shape distance, so the reach is stated separately.</summary>
        private static string ReachText(int minimum, int maximum) =>
            minimum > 0 ? " " + minimum + "–" + maximum + " 格" + DeadZoneNote :
            maximum > 1 ? " " + maximum + " 格内" : string.Empty;

        private static string DeadZoneSuffix(int minimum) => minimum > 0 ? DeadZoneNote : string.Empty;

        private static string ScopeText(FireRuleScope scope, FireTargetKind targetKind)
        {
            switch (scope)
            {
                case FireRuleScope.Primary: return targetKind == FireTargetKind.Self ? SelfSelection : PrimaryTargetCell;
                case FireRuleScope.Selection: return SelectedCells;
                case FireRuleScope.EnemySelection: return "选中敌人";
                case FireRuleScope.AllySelection: return "选中友军";
                case FireRuleScope.OrthogonalNeighbors: return NeighbourCells;
                case FireRuleScope.PathAdjacentEnemies: return "路径邻接的敌方单位";
                case FireRuleScope.Source: return SelfSelection;
                case FireRuleScope.SourceCell: return "起点格";
                case FireRuleScope.Destination: return "落点";
                case FireRuleScope.CoveredCells: return "覆盖格";
                default: return null;
            }
        }
    }

    /// <summary>
    /// Short 词条 for a 术式, meant to be drawn as boxed chips instead of a prose sentence:
    /// 形状 / 距离 / 对象 / 效果. Everything comes from the same data the resolver uses.
    /// </summary>
    public static class CombatSpellTags
    {
        public const int MaximumTags = 6;

        public static IReadOnlyList<string> For(FireSpellDefinition spell)
        {
            var tags = new List<string>();
            if (spell == null) return tags;
            Add(tags, ShapeTag(spell));
            Add(tags, DistanceTag(spell));
            Add(tags, TargetTag(spell.TargetKind));
            if (spell.Rules != null)
                foreach (FireSpellRule rule in spell.Rules) Add(tags, EffectTag(rule.Kind));
            return tags;
        }

        /// <summary>法宝词条, derived from the same TargetRule／Shape／Range／Effects the resolver uses.</summary>
        public static IReadOnlyList<string> For(ArtifactDefinition artifact)
        {
            var tags = new List<string>();
            if (artifact == null) return tags;
            Add(tags, artifact.TargetRule == ArtifactTargetRule.Self ? SelfSelectionTag : ArtifactShapeTag(artifact.Shape));
            if (artifact.TargetRule != ArtifactTargetRule.Self && artifact.Range > 0) Add(tags, artifact.Range + "格");
            Add(tags, ArtifactTargetTag(artifact.TargetRule));
            if (artifact.Effects != null)
                foreach (ArtifactEffectDefinition effect in artifact.Effects) Add(tags, ArtifactEffectTag(effect.Kind));
            if (artifact.RequiresLineOfSight && artifact.Range > 1) Add(tags, "需视线");
            return tags;
        }

        /// <summary>通用术式词条 for everything that is not a personal fire spell.</summary>
        public static IReadOnlyList<string> For(SkillDefinition skill)
        {
            var tags = new List<string>();
            if (skill == null) return tags;
            Add(tags, skill.TargetRule == SkillTargetRule.Self ? SelfSelectionTag : ShapeTag(skill.Shape));
            if (skill.TargetRule != SkillTargetRule.Self && skill.Range > 0)
                Add(tags, skill.MinimumRange > 0 ? skill.MinimumRange + "–" + skill.Range + "格" : skill.Range + "格");
            Add(tags, SkillTargetTag(skill.TargetRule));
            if (skill.Effects != null)
                foreach (SkillEffectDefinition effect in skill.Effects) Add(tags, SkillEffectTag(effect.Type));
            return tags;
        }

        /// <summary>肉鸽术式：命中个人火系术式时复用其完整词条，否则退回声明式目标字段。</summary>
        public static IReadOnlyList<string> For(OCC.Combat.Roguelite.SpellDefinition spell)
        {
            var tags = new List<string>();
            if (spell == null) return tags;
            FireSpellDefinition fire = FireSpellCatalog.Get(spell.DefinitionId);
            if (fire != null) return For(fire);
            Add(tags, TargetingTag(spell.Targeting));
            if (spell.Range > 0) Add(tags, spell.Range + "格");
            if (!string.IsNullOrWhiteSpace(spell.LineOfSightRule) && spell.Range > 1) Add(tags, "需视线");
            return tags;
        }

        /// <summary>法宝以物品形式出现时（ItemDefinition）复用同一套词条。</summary>
        public static IReadOnlyList<string> ForItem(ItemDefinition item)
        {
            if (item == null) return new List<string>();
            foreach (ArtifactDefinition candidate in ArtifactCatalog.All)
                if (candidate.Id == item.Id) return For(candidate);
            return new List<string>();
        }

        private static string TargetingTag(string targeting)
        {
            if (string.IsNullOrWhiteSpace(targeting)) return null;
            if (targeting.Contains("自身")) return SelfSelectionTag;
            if (targeting.Contains("友")) return "友军";
            if (targeting.Contains("空地") || targeting.Contains("空格")) return "空地";
            if (targeting.Contains("物")) return "可破坏";
            return "敌人";
        }

        private static string ArtifactShapeTag(ArtifactSelectionShape shape)
        {
            switch (shape)
            {
                case ArtifactSelectionShape.Cross: return "十字";
                // RadiusOne covers a one-cell radius around the anchor; the exact ring/3×3 split is not
                // asserted here on purpose, so the tag stays true either way.
                case ArtifactSelectionShape.RadiusOne: return "1格范围";
                default: return "单点";
            }
        }

        private static string ArtifactTargetTag(ArtifactTargetRule rule)
        {
            switch (rule)
            {
                case ArtifactTargetRule.Self: return null;
                case ArtifactTargetRule.AllyOrSelf: return "友军";
                case ArtifactTargetRule.TwoAllies: return "两名友军";
                case ArtifactTargetRule.AnyUnit: return "敌我";
                case ArtifactTargetRule.AnyCell:
                case ArtifactTargetRule.EmptyCell: return "空地";
                case ArtifactTargetRule.Destructible: return "可破坏";
                case ArtifactTargetRule.Device: return "装置";
                default: return "敌人";
            }
        }

        private static string ArtifactEffectTag(ArtifactEffectKind kind)
        {
            switch (kind)
            {
                case ArtifactEffectKind.Damage:
                case ArtifactEffectKind.DamageObject: return "伤害";
                case ArtifactEffectKind.LoseHealth:
                case ArtifactEffectKind.BacklashIfTargetSurvives: return "代价";
                case ArtifactEffectKind.RestoreHealth: return "治疗";
                case ArtifactEffectKind.RestoreShield: return "护盾";
                case ArtifactEffectKind.ConsumeShield: return "消耗护盾";
                case ArtifactEffectKind.RestoreMana: return "回魔";
                case ArtifactEffectKind.ApplyStatus: return "状态";
                case ArtifactEffectKind.ClearNegativeStatuses: return "净化";
                case ArtifactEffectKind.MoveSource: return "位移";
                case ArtifactEffectKind.ForceMoveTarget:
                case ArtifactEffectKind.ForceMoveFromCell: return "推拉";
                case ArtifactEffectKind.CreateLightCover:
                case ArtifactEffectKind.CreateHeavyCover:
                case ArtifactEffectKind.GrantLightCoverBypass: return "掩体";
                case ArtifactEffectKind.DestroyLightCover: return "拆掩体";
                case ArtifactEffectKind.CreateFireground: return "火场";
                case ArtifactEffectKind.ClearFireground: return "灭火";
                case ArtifactEffectKind.CreateSmoke: return "烟幕";
                case ArtifactEffectKind.Reveal: return "显影";
                case ArtifactEffectKind.DelayInitiative: return "延后行动条";
                case ArtifactEffectKind.ArmReaction: return "反应";
                case ArtifactEffectKind.ArmAnchor: return "锚点";
                case ArtifactEffectKind.DeployDecoy: return "诱饵";
                case ArtifactEffectKind.GrantActionPoints: return "行动点";
                case ArtifactEffectKind.ReserveResources: return "储备";
                default: return null;
            }
        }

        private static string ShapeTag(FireSelectionShape shape)
        {
            switch (shape)
            {
                case FireSelectionShape.Line:
                case FireSelectionShape.ContinuousLine:
                case FireSelectionShape.Path: return "直线";
                case FireSelectionShape.Cone: return "锥形";
                case FireSelectionShape.Cross:
                case FireSelectionShape.CenterAndOrthogonal: return "十字";
                case FireSelectionShape.Square3: return "方形";
                case FireSelectionShape.OrthogonalRing:
                case FireSelectionShape.AroundUnit: return "环形";
                default: return "单点";
            }
        }

        private static string SkillTargetTag(SkillTargetRule rule)
        {
            switch (rule)
            {
                case SkillTargetRule.Self: return null;
                case SkillTargetRule.AllyUnit: return "友军";
                case SkillTargetRule.AnyUnit: return "敌我";
                case SkillTargetRule.GridCell: return "空地";
                case SkillTargetRule.Destructible: return "可破坏";
                default: return "敌人";
            }
        }

        private static string SkillEffectTag(SkillEffectType type)
        {
            switch (type)
            {
                case SkillEffectType.Damage: return "伤害";
                case SkillEffectType.ApplyStatus: return "状态";
                case SkillEffectType.RestoreHealth: return "治疗";
                case SkillEffectType.RestoreShield: return "护盾";
                case SkillEffectType.MoveSource: return "位移";
                case SkillEffectType.ClearStatus: return "净化";
                default: return null;
            }
        }

        public static string ShapeTag(FireSpellDefinition spell)
        {
            if (spell.TargetKind == FireTargetKind.Self) return SelfSelectionTag;
            switch (spell.Shape)
            {
                case FireSelectionShape.Line:
                case FireSelectionShape.ContinuousLine:
                case FireSelectionShape.Path: return "直线";
                case FireSelectionShape.Cone: return "锥形";
                case FireSelectionShape.Cross:
                case FireSelectionShape.CenterAndOrthogonal: return "十字";
                case FireSelectionShape.Square3: return "方形";
                case FireSelectionShape.OrthogonalRing:
                case FireSelectionShape.AroundUnit: return "环形";
                default: return "单点";
            }
        }

        /// <summary>Distance term: 自身 carries none, and a dead zone is written as the legal band.</summary>
        public static string DistanceTag(FireSpellDefinition spell)
        {
            if (spell.TargetKind == FireTargetKind.Self || spell.Range <= 0) return null;
            return spell.MinimumRange > 0 ? spell.MinimumRange + "–" + spell.Range + "格" : spell.Range + "格";
        }

        private const string SelfSelectionTag = "自身";

        private static void Add(List<string> tags, string tag)
        {
            if (string.IsNullOrEmpty(tag) || tags.Contains(tag) || tags.Count >= MaximumTags) return;
            tags.Add(tag);
        }

        private static string TargetTag(FireTargetKind kind)
        {
            switch (kind)
            {
                case FireTargetKind.Self: return null;
                case FireTargetKind.AllyOrSelf: return "友军";
                case FireTargetKind.Unit: return "敌我";
                case FireTargetKind.EmptyCell:
                case FireTargetKind.BurningCell: return "空地";
                case FireTargetKind.Hittable:
                case FireTargetKind.Destructible: return "可击打";
                default: return "敌人";
            }
        }

        private static string EffectTag(FireRuleKind kind)
        {
            switch (kind)
            {
                case FireRuleKind.Damage:
                case FireRuleKind.WeaponDamage: return "伤害";
                case FireRuleKind.DamageDurability: return null;
                case FireRuleKind.RestoreShield:
                case FireRuleKind.GrantShieldBeforeRanged: return "护盾";
                case FireRuleKind.MoveSource:
                case FireRuleKind.MoveAfterAttack:
                case FireRuleKind.Push:
                case FireRuleKind.PushAllUnits:
                case FireRuleKind.SwapUnits:
                case FireRuleKind.RestoreMovement: return "位移";
                case FireRuleKind.ApplyBurning:
                case FireRuleKind.SetBurningDuration:
                case FireRuleKind.ExtendBurning:
                case FireRuleKind.ConsumeBurning: return "燃烧";
                case FireRuleKind.ApplyBreakStance: return "破势";
                case FireRuleKind.CreateFireground:
                case FireRuleKind.ConsumeFireground:
                case FireRuleKind.ApplyFiregroundBoost:
                case FireRuleKind.ApplyFiregroundVulnerability: return "火场";
                case FireRuleKind.AddMovement:
                case FireRuleKind.ReserveNextTurnAction: return "强化";
                case FireRuleKind.RestoreMana: return "回魔";
                case FireRuleKind.ClearStatus: return "净化";
                case FireRuleKind.LoseHealth: return "代价";
                default: return null;
            }
        }
    }
}
