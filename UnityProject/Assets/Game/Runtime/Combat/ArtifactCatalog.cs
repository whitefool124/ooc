using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public enum ArtifactTargetRule { Self, Enemy, AllyOrSelf, AnyUnit, AnyCell, EmptyCell, Destructible, Device, TwoAllies }
    public enum ArtifactSelectionShape { Single, Cross, RadiusOne }
    public enum ArtifactEffectKind
    {
        Damage, LoseHealth, RestoreHealth, RestoreShield, RestoreMana, ConsumeShield, ApplyStatus, ClearNegativeStatuses,
        MoveSource, ForceMoveTarget, ForceMoveFromCell, CreateLightCover, CreateHeavyCover, DamageObject, DestroyLightCover, CreateFireground, CreateSmoke,
        ClearFireground, Reveal, GrantLightCoverBypass, DelayInitiative, ArmReaction,
        ArmAnchor, DeployDecoy, GrantActionPoints, ReserveResources, BacklashIfTargetSurvives
    }
    public enum ArtifactEffectScope { Source, Primary, Secondary, Selection }
    public enum ArtifactEffectCondition { Always, TargetLightweight, TargetHeavy, TargetSurvives }
    public enum ArtifactReactionTrigger { None, EnemyEnterMarkedCell, ForcedMovement, IncomingRangedDamage }
    [Flags]
    public enum ArtifactContentSource { None = 0, NormalReward = 1, EliteReward = 2, Treasure = 4, BossReward = 8, Shop = 16, Event = 32, Loot = 64 }

    public readonly struct ArtifactEffectDefinition
    {
        public ArtifactEffectKind Kind { get; }
        public ArtifactEffectScope Scope { get; }
        public ArtifactEffectCondition Condition { get; }
        public int Amount { get; }
        public int Duration { get; }
        public DamageType DamageType { get; }
        public StatusType Status { get; }
        public bool AffectAllies { get; }
        public ArtifactReactionTrigger Trigger { get; }

        public ArtifactEffectDefinition(ArtifactEffectKind kind, int amount = 0, int duration = 0,
            ArtifactEffectScope scope = ArtifactEffectScope.Primary,
            ArtifactEffectCondition condition = ArtifactEffectCondition.Always,
            DamageType damageType = DamageType.Arcane, StatusType status = default,
            bool affectAllies = false, ArtifactReactionTrigger trigger = ArtifactReactionTrigger.None)
        {
            Kind = kind; Amount = amount; Duration = duration; Scope = scope; Condition = condition;
            DamageType = damageType; Status = status; AffectAllies = affectAllies; Trigger = trigger;
        }
    }

    public sealed class ArtifactDefinition
    {
        public string Id { get; }
        public string Slug { get; }
        public string DisplayName { get; }
        public string Provenance { get; }
        public ItemRarity Rarity { get; }
        public int Width { get; }
        public int Height { get; }
        public int Weight { get; }
        public int MaximumUses { get; }
        public int ActionPointCost { get; }
        public int ManaCost { get; }
        public int Range { get; }
        public ArtifactTargetRule TargetRule { get; }
        public ArtifactSelectionShape Shape { get; }
        public bool RequiresLineOfSight { get; }
        public string Element { get; }
        public string PublicCost { get; }
        public string TargetSummary { get; }
        public string EffectSummary { get; }
        public string RiskSummary { get; }
        public string BuildUse { get; }
        public string BuildRole => BuildUse;
        public string IconPath { get; }
        public string ActionSemantic { get; }
        public string VfxSemantic { get; }
        public ArtifactContentSource ContentSources { get; }
        public IReadOnlyList<ArtifactEffectDefinition> Effects { get; }
        public FireSpellDefinition Spell { get; }

        public ArtifactDefinition(string id, string slug, string displayName, string provenance, ItemRarity rarity,
            int width, int height, int weight, int maximumUses, int actionPointCost, int manaCost, int range,
            ArtifactTargetRule targetRule, ArtifactSelectionShape shape, bool requiresLineOfSight, string element,
            string publicCost, string targetSummary, string effectSummary, string riskSummary, string buildUse,
            string actionSemantic, string vfxSemantic, ArtifactContentSource contentSources,
            IEnumerable<ArtifactEffectDefinition> effects, FireSpellDefinition compatibilitySpell = null)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Artifact identity is required.");
            if (width < 1 || height < 1 || weight < 0 || maximumUses < 1 || actionPointCost < 0 || manaCost < 0 || range < 0)
                throw new ArgumentOutOfRangeException(nameof(maximumUses));
            Id = id; Slug = slug; DisplayName = displayName; Provenance = provenance; Rarity = rarity;
            Width = width; Height = height; Weight = weight; MaximumUses = maximumUses;
            ActionPointCost = actionPointCost; ManaCost = manaCost; Range = range; TargetRule = targetRule;
            Shape = shape; RequiresLineOfSight = requiresLineOfSight; Element = element ?? string.Empty;
            PublicCost = publicCost; TargetSummary = targetSummary; EffectSummary = effectSummary;
            RiskSummary = riskSummary; BuildUse = buildUse; ActionSemantic = actionSemantic;
            VfxSemantic = vfxSemantic; ContentSources = contentSources;
            IconPath = "Art/FormalArtifactIcons32/" + slug;
            Effects = (effects ?? throw new ArgumentNullException(nameof(effects))).ToArray();
            if (Effects.Count == 0) throw new ArgumentException("Artifact requires at least one effect.", nameof(effects));
            Spell = compatibilitySpell;
        }

        public ItemDefinition ToItemDefinition() => new ItemDefinition(Id, DisplayName,
            EffectSummary + " 代价：" + PublicCost + " 风险：" + RiskSummary,
            ItemCategory.Artifact, Rarity, Width, Height, Weight, MaximumUses, Element, Provenance,
            canQuickEquip: true, iconPath: IconPath, inventoryArtPath: "Art/FormalInventoryFootprints/" + Slug);
    }

    public static class ArtifactCatalog
    {
        private static readonly HashSet<string> RetiredContentIds = new HashSet<string>(Array.Empty<string>(), StringComparer.Ordinal);

        private static ArtifactEffectDefinition E(ArtifactEffectKind kind, int amount = 0, int duration = 0,
            ArtifactEffectScope scope = ArtifactEffectScope.Primary,
            ArtifactEffectCondition condition = ArtifactEffectCondition.Always,
            DamageType damageType = DamageType.Arcane, StatusType status = default, bool allies = false,
            ArtifactReactionTrigger trigger = ArtifactReactionTrigger.None) =>
            new ArtifactEffectDefinition(kind, amount, duration, scope, condition, damageType, status, allies, trigger);

        private static ArtifactDefinition A(string id, string slug, string name, ItemRarity rarity, int uses, int ap,
            int range, ArtifactTargetRule target, ArtifactSelectionShape shape, string source, string element,
            string cost, string targeting, string effect, string risk, string build, string action, string vfx,
            ArtifactContentSource pools, ArtifactEffectDefinition[] effects, int width = 1, int height = 1,
            int weight = 1, bool los = true, FireSpellDefinition spell = null) =>
            new ArtifactDefinition(id, slug, name, source, rarity, width, height, weight, uses, ap, 0, range,
                target, shape, los, element, cost, targeting, effect, risk, build, action, vfx, pools, effects, spell);

        private static readonly ArtifactContentSource CommonPools = ArtifactContentSource.NormalReward |
            ArtifactContentSource.Shop | ArtifactContentSource.Loot;
        private static readonly ArtifactContentSource AdvancedPools = ArtifactContentSource.EliteReward |
            ArtifactContentSource.Treasure | ArtifactContentSource.Shop | ArtifactContentSource.Event;
        private static readonly ArtifactContentSource RarePools = ArtifactContentSource.EliteReward |
            ArtifactContentSource.Treasure | ArtifactContentSource.BossReward;

        private static readonly FireSpellDefinition DemolitionCompatibilitySpell = new FireSpellDefinition(
            "F-T01", "炎脉封装筒", FireSpellRarity.Rare, FireSpellGroup.Breach, 0, 0, 0, 0, 4,
            FireTargetKind.EmptyCell, FireSelectionShape.CenterAndOrthogonal, 1, true, false, new[]
            {
                new FireSpellRule(FireRuleKind.Damage, 16, scope: FireRuleScope.Selection, affectAllies: true),
                new FireSpellRule(FireRuleKind.DestroyLightCover, scope: FireRuleScope.Selection,
                    destructibleMask: FireDestructibleMask.LightCover),
                new FireSpellRule(FireRuleKind.CreateFireground, 8, 3, FireRuleScope.Selection, affectAllies: true)
            }, "artifact_prime", "fire_cross_blast", "fire_burning_ground");

        public static readonly ArtifactDefinition DemolitionCanister = A("F-T01", "demolition_canister", "炎脉封装筒",
            ItemRarity.Rare, 2, 0, 4, ArtifactTargetRule.EmptyCell, ArtifactSelectionShape.Cross, "学院试制", "火",
            "0 行动点；消耗 1 次", "4 格内空地；十字范围", "造成 16 点火焰伤害，摧毁轻掩体；生成火场 3 回合",
            "伤及范围内所有单位；重掩体阻断投放", "破障、封路", "双手旋阀后投放", "火十字爆发与燃烧地格",
            RarePools, new[] { E(ArtifactEffectKind.Damage, 16, scope: ArtifactEffectScope.Selection, damageType: DamageType.Fire, allies: true),
                E(ArtifactEffectKind.DestroyLightCover, scope: ArtifactEffectScope.Selection),
                E(ArtifactEffectKind.CreateFireground, 8, 3, ArtifactEffectScope.Selection) }, 2, 1, 2, spell: DemolitionCompatibilitySpell);

        public static readonly ArtifactDefinition AegisFold = A("G-T01", "aegis_fold", "折盾匣", ItemRarity.Uncommon, 3, 0, 3,
            ArtifactTargetRule.AllyOrSelf, ArtifactSelectionShape.Single, "持证护具工坊", "通用", "不消耗行动点，消耗 1 次",
            "3 格内自身或友军", "目标获得 20 点护盾", "不能治疗或清除异常", "保护自己或救下同伴", "展开折片并压合回路", "青白折面护盾",
            CommonPools | ArtifactContentSource.EliteReward, new[] { E(ArtifactEffectKind.RestoreShield, 20) }, 2, 2, 1);
        public static readonly ArtifactDefinition PhaseSpindle = A("G-T02", "phase_spindle", "移相线轴", ItemRarity.Rare, 2, 0, 3,
            ArtifactTargetRule.EmptyCell, ArtifactSelectionShape.Single, "学院定距器旧型", "通用", "不消耗行动点，消耗 1 次；下一回合延后 4 刻度",
            "3 格内可达空地；重掩体阻挡", "移至目标格", "束缚时不能使用", "脱离包围、抢占位置", "拉出线轴并踏入闭环", "短程相位轨迹",
            AdvancedPools, new[] { E(ArtifactEffectKind.MoveSource), E(ArtifactEffectKind.DelayInitiative, 4, scope: ArtifactEffectScope.Source) }, 1, 2, 1);
        public static readonly ArtifactDefinition BindingFrame = A("G-T03", "binding_frame", "缚位框", ItemRarity.Uncommon, 2, 0, 3,
            ArtifactTargetRule.Enemy, ArtifactSelectionShape.Single, "边境猎团", "通用", "不消耗行动点，消耗 1 次", "3 格内可见敌人",
            "目标束缚 1 回合", "束缚只限制移动；目标仍能攻击、施术和使用物品", "拦住对手、集中攻击", "对准四角并扣合", "四角束带收紧",
            AdvancedPools, new[] { E(ArtifactEffectKind.ApplyStatus, duration: 2, status: StatusType.Bound) }, 2, 2, 2);
        public static readonly ArtifactDefinition SurveyLens = A("G-T04", "survey_lens", "显迹测镜", ItemRarity.Common, 4, 0, 4,
            ArtifactTargetRule.Enemy, ArtifactSelectionShape.Single, "遗迹勘验行", "通用", "不消耗行动点，消耗 1 次", "4 格内可见敌人",
            "清除目标当前护盾，并施加破势至其下一次自己回合结束；期间不能获得护盾", "不造成伤害，也不能阻止目标移动或攻击", "显露护盾接缝，截断持续套盾", "举镜调焦并锁定护盾接缝", "金色轮廓与破势刻度",
            CommonPools | ArtifactContentSource.Event, new[] { E(ArtifactEffectKind.ApplyStatus, duration: 1, status: StatusType.BreakStance) });
        public static readonly ArtifactDefinition FieldSiphon = A("G-T05", "field_siphon", "以太虹吸泵", ItemRarity.Uncommon, 3, 0, 0,
            ArtifactTargetRule.Self, ArtifactSelectionShape.Single, "边境检修工坊", "通用", "不消耗行动点，消耗 1 次；失去 8 生命", "自身；魔力未满且生命大于 8",
            "恢复 4 点个人魔力；失去 8 点生命", "生命损失绕过护盾", "用生命换取魔力", "压泵抽取生命余量", "青色以太回流",
            AdvancedPools, new[] { E(ArtifactEffectKind.RestoreMana, 4, scope: ArtifactEffectScope.Source), E(ArtifactEffectKind.LoseHealth, 8, scope: ArtifactEffectScope.Source) }, 3, 2, 2, los: false);
        public static readonly ArtifactDefinition MendingLattice = A("G-T06", "mending_lattice", "复元编架", ItemRarity.Rare, 2, 0, 3,
            ArtifactTargetRule.AllyOrSelf, ArtifactSelectionShape.Single, "教会救护工坊", "通用", "不消耗行动点，消耗 1 次；目标失去 12 护盾", "3 格内受伤且至少有 12 护盾的自身或友军",
            "恢复 20 点生命；失去 12 点护盾", "目标须有 12 点护盾", "恢复与危急救援", "展开编架覆盖伤处", "柔白网格复元",
            AdvancedPools, new[] { E(ArtifactEffectKind.RestoreHealth, 20), E(ArtifactEffectKind.ConsumeShield, 12) }, 3, 2, 3);
        public static readonly ArtifactDefinition CoverStamp = A("G-T07", "cover_stamp", "掩体压模", ItemRarity.Uncommon, 2, 0, 2,
            ArtifactTargetRule.EmptyCell, ArtifactSelectionShape.Single, "城镇守备石工作坊", "通用", "不消耗行动点，消耗 1 次", "2 格内未占用、有承重表面的空格",
            "生成 24 耐久重掩体", "阻挡双方移动和攻击线", "临时封路、切断攻击线或建立防守角", "压下重型模具并抬起整块土石墙", "厚重土石墙压模成形",
            AdvancedPools, new[] { E(ArtifactEffectKind.CreateHeavyCover, 24) }, 3, 2, 4);
        public static readonly ArtifactDefinition BreachWedge = A("G-T08", "breach_wedge", "解构楔", ItemRarity.Common, 4, 0, 1,
            ArtifactTargetRule.Destructible, ArtifactSelectionShape.Single, "采石行会", "通用", "不消耗行动点，消耗 1 次", "相邻轻型、重型掩体或装置",
            "目标物件失去 24 耐久", "必须贴身", "破障与设备处置", "嵌楔后敲击", "几何裂解纹",
            CommonPools, new[] { E(ArtifactEffectKind.DamageObject, 24) }, 1, 2, 2);
        public static readonly ArtifactDefinition RelayCompass = A("G-T09", "relay_compass", "导位罗盘", ItemRarity.Uncommon, 3, 0, 4,
            ArtifactTargetRule.AnyUnit, ArtifactSelectionShape.Single, "商路救援队", "通用", "不消耗行动点，消耗 1 次", "4 格内可见友军或敌人",
            "将目标拉近至多 2 格", "定锚、阻挡或占位会停止牵引", "友军救援、敌方位移", "拨针锁定落点", "牵引弧线",
            AdvancedPools, new[] { E(ArtifactEffectKind.ForceMoveTarget, 2) });
        public static readonly ArtifactDefinition ReactionBell = A("G-T10", "reaction_bell", "截击铃", ItemRarity.Rare, 2, 0, 3,
            ArtifactTargetRule.EmptyCell, ArtifactSelectionShape.Single, "边境猎团", "通用", "不消耗行动点，消耗 1 次；标记持续到你的下一回合", "3 格内可见空格",
            "首名入格敌人受到 12 点伤害并被推开 1 格", "敌人可绕开标记", "守住路口、阻止接近", "悬铃并标定地格", "铜色截击波纹",
            RarePools, new[] { E(ArtifactEffectKind.ArmReaction, 12, 1, ArtifactEffectScope.Source, trigger: ArtifactReactionTrigger.EnemyEnterMarkedCell) }, 1, 2, 1);
        public static readonly ArtifactDefinition HazardCondenser = A("G-T11", "hazard_condenser", "险地冷凝器", ItemRarity.Common, 4, 0, 3,
            ArtifactTargetRule.AnyCell, ArtifactSelectionShape.Cross, "消防与勘验人员共制", "通用", "不消耗行动点，消耗 1 次", "3 格内目标格及正交邻格；至少含一格燃烧地格或烟尘",
            "移除范围内火场和烟幕", "会清除己方场地", "危险地形清理", "旋开冷凝阀", "水雾冷凝圈",
            CommonPools, new[] { E(ArtifactEffectKind.ClearFireground, scope: ArtifactEffectScope.Selection) }, 2, 2, 2, los: false);
        public static readonly ArtifactDefinition TurnLedger = A("G-T12", "turn_ledger", "行程簿", ItemRarity.Rare, 2, 0, 0,
            ArtifactTargetRule.Self, ArtifactSelectionShape.Single, "失落人类文明行旅遗物", "通用", "不消耗行动点，消耗 1 次；你的下一回合延后 8 刻度", "自身",
            "获得 2 点行动点", "下一回合延后 8 刻度", "现在多做一件事，之后晚些行动", "盖印并合上账页", "蓝色刻度前移并延后行动条",
            RarePools, new[] { E(ArtifactEffectKind.GrantActionPoints, 2, scope: ArtifactEffectScope.Source), E(ArtifactEffectKind.DelayInitiative, 8, scope: ArtifactEffectScope.Source) }, 2, 1, 1, los: false);
        public static readonly ArtifactDefinition AnchorBrace = A("G-T13", "anchor_brace", "定锚支架", ItemRarity.Uncommon, 4, 0, 0,
            ArtifactTargetRule.Self, ArtifactSelectionShape.Single, "驿站装卸工坊", "通用", "不消耗行动点；抵消强制位移时消耗 1 次", "装备于快捷栏的反应器",
            "自动抵消一次推或拉", "不阻止伤害、状态或主动移动", "站稳脚跟，防止被推拉", "三足锚爪自动咬合", "土黄色锚定环",
            CommonPools | ArtifactContentSource.EliteReward, new[] { E(ArtifactEffectKind.ArmAnchor, duration: 1, scope: ArtifactEffectScope.Source) }, 2, 2, 2, los: false);
        public static readonly ArtifactDefinition PrismRegulator = A("G-T14", "prism_regulator", "棱返调节器", ItemRarity.Rare, 2, 0, 0,
            ArtifactTargetRule.Self, ArtifactSelectionShape.Single, "光学师行会", "通用", "不消耗行动点，消耗 1 次；效果持续到你的下一回合", "自身",
            "首次受远程伤害时，获得 8 点护盾并反击 8 点伤害", "近战与环境伤害不触发", "远程反制与护盾转换", "展开棱片校准入射角", "镜面折返射线",
            RarePools, new[] { E(ArtifactEffectKind.ArmReaction, 8, 8, ArtifactEffectScope.Source, trigger: ArtifactReactionTrigger.IncomingRangedDamage) }, 2, 1, 1, los: false);
        public static readonly ArtifactDefinition DecoyLantern = A("G-T15", "decoy_lantern", "诱导灯", ItemRarity.Uncommon, 2, 0, 3,
            ArtifactTargetRule.EmptyCell, ArtifactSelectionShape.Single, "商队驱兽灯改型", "通用", "不消耗行动点，消耗 1 次", "3 格内可见空格",
            "部署 12 耐久诱导灯；5 格内敌人改为接近它", "灯占格且可被摧毁", "引开敌人、拆开夹击并制造友伤角度", "置灯并翻开遮光片", "暖金诱导脉冲与公开牵引线",
            AdvancedPools, new[] { E(ArtifactEffectKind.DeployDecoy, 12, 1) }, 1, 2, 2);
        public static readonly ArtifactDefinition ShieldBalancer = A("G-T16", "shield_balancer", "护盾均衡阀", ItemRarity.Uncommon, 3, 0, 3,
            ArtifactTargetRule.EmptyCell, ArtifactSelectionShape.Single, "医护护具工坊", "通用", "不消耗行动点，另消耗 12 护盾与 1 次", "3 格内未占用、无物件的空格；自身至少有 12 护盾",
            "失去 12 点护盾；生成 12 耐久轻掩体", "掩体双方都能利用或摧毁", "把临时护盾转移到关键站位", "接管护盾流并展开折叠屏", "青白护盾流转入地面屏障",
            AdvancedPools, new[] { E(ArtifactEffectKind.ConsumeShield, 12, scope: ArtifactEffectScope.Source), E(ArtifactEffectKind.CreateLightCover, 12) }, 2, 2, 2);
        public static readonly ArtifactDefinition SeismicPlumb = A("G-T17", "seismic_plumb", "震测铅锤", ItemRarity.Rare, 2, 0, 3,
            ArtifactTargetRule.AnyCell, ArtifactSelectionShape.Cross, "失落人类文明测绘遗物", "通用", "不消耗行动点，消耗 1 次", "3 格内可见地格及正交邻格；范围至少有一个单位",
            "将正交邻格单位向外推开 1 格", "敌我都会受影响", "拆开夹击、推出有效射程、制造友伤角度", "垂直落锤，把地面震波沿四个方向推出", "十字地裂波与逐单位落点",
            RarePools, new[] { E(ArtifactEffectKind.ForceMoveFromCell, 1, scope: ArtifactEffectScope.Selection, allies: true) }, 1, 3, 3);
        public static readonly ArtifactDefinition NullVeil = A("G-T18", "null_veil", "静默幕", ItemRarity.Uncommon, 2, 0, 3,
            ArtifactTargetRule.EmptyCell, ArtifactSelectionShape.Single, "失落人类文明封存布", "通用", "不消耗行动点，消耗 1 次；持续 8 行动刻度", "3 格内可见、未占用且无物件的空格",
            "在目标格生成烟幕，阻断远程攻击线", "敌我双方都会被遮线", "切断夹击火线、制造短时转位窗口", "抛出封存布并展开遮光微粒", "暗紫烟幕与公开消散刻度",
            AdvancedPools, new[] { E(ArtifactEffectKind.CreateSmoke, duration: 8) }, 3, 2, 2);
        public static readonly ArtifactDefinition FortuneSeal = A("G-T19", "fortune_seal", "冒险封签", ItemRarity.Rare, 2, 0, 4,
            ArtifactTargetRule.Enemy, ArtifactSelectionShape.Single, "黑市回收的远古封签", "通用", "不消耗行动点，消耗 1 次；若未击倒目标，自己失去 12 生命", "4 格内可见敌人；使用者生命大于 12",
            "造成 28 点伤害；目标存活时失去 12 点生命", "反噬绕过护盾", "孤注一掷地结束战斗", "撕开封签并按向目标", "黑金封印断裂",
            RarePools | ArtifactContentSource.Event, new[] { E(ArtifactEffectKind.Damage, 28), E(ArtifactEffectKind.BacklashIfTargetSurvives, 12, scope: ArtifactEffectScope.Source, condition: ArtifactEffectCondition.TargetSurvives) }, weight: 1);

        public static readonly IReadOnlyList<ArtifactDefinition> All = new[]
        {
            DemolitionCanister, AegisFold, PhaseSpindle, BindingFrame, SurveyLens, FieldSiphon,
            MendingLattice, CoverStamp, BreachWedge, RelayCompass, ReactionBell, HazardCondenser,
            TurnLedger, AnchorBrace, PrismRegulator, DecoyLantern, ShieldBalancer, SeismicPlumb,
            NullVeil, FortuneSeal
        };

        public static ArtifactDefinition Get(string id) => All.FirstOrDefault(value => value.Id == id) ??
            throw new InvalidOperationException("Unknown artifact definition: " + id);
        public static bool IsCurrentlyUsable(string id) => !string.IsNullOrEmpty(id) && !RetiredContentIds.Contains(id);
    }
}
