using System;
using System.Collections.Generic;
using System.Linq;
using OCC.Combat.Roguelite;

namespace OCC.Combat.Presentation
{
    public sealed class CombatHoverDescriptionRow
    {
        public string ContentId { get; }
        public string Cost { get; }
        public string Cycle { get; }
        public string Target { get; }
        public string Condition { get; }
        public string Effect { get; }
        public string Risk { get; }
        public string Lore { get; }

        public CombatHoverDescriptionRow(string contentId, string cost, string cycle, string target,
            string condition, string effect, string risk, string lore = "")
        {
            ContentId = contentId ?? string.Empty;
            Cost = cost ?? string.Empty;
            Cycle = cycle ?? string.Empty;
            Target = target ?? string.Empty;
            Condition = condition ?? string.Empty;
            Effect = effect ?? string.Empty;
            Risk = risk ?? string.Empty;
            Lore = lore ?? string.Empty;
        }

        public string FormatBody(string inventoryState = null)
        {
            List<string> lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(inventoryState)) lines.Add("次数　" + inventoryState);
            if (!string.IsNullOrWhiteSpace(Cost)) lines.Add("消耗　" + Cost);
            if (!string.IsNullOrWhiteSpace(Cycle)) lines.Add("循环　" + Cycle);
            if (!string.IsNullOrWhiteSpace(Target)) lines.Add("目标　" + Target);
            if (!string.IsNullOrWhiteSpace(Condition)) lines.Add("条件　" + Condition);
            lines.Add("效果\n" + (string.IsNullOrWhiteSpace(Effect) ? "无额外效果" : Effect));
            if (!string.IsNullOrWhiteSpace(Risk)) lines.Add("注意　" + Risk);
            if (!string.IsNullOrWhiteSpace(Lore)) lines.Add("简介　" + Lore);
            return string.Join("\n", lines);
        }
    }

    /// <summary>
    /// Player-facing hover copy derived from the runtime spell and artifact catalogs.
    /// This is the single presentation table used by combat, loadout and inventory hover cards.
    /// </summary>
    public static class CombatHoverDescriptionTable
    {
        private static readonly IReadOnlyDictionary<string, CombatHoverDescriptionRow> SpellRows =
            RogueContentCatalog.CreateAcademyV01().Spells.ToDictionary(
                spell => spell.DefinitionId, BuildSpellRow, StringComparer.Ordinal);

        private static readonly IReadOnlyDictionary<string, CombatHoverDescriptionRow> ArtifactRows =
            ArtifactCatalog.All.ToDictionary(
                artifact => artifact.Id, BuildArtifactRow, StringComparer.Ordinal);

        public static IReadOnlyDictionary<string, CombatHoverDescriptionRow> Spells => SpellRows;
        public static IReadOnlyDictionary<string, CombatHoverDescriptionRow> Artifacts => ArtifactRows;

        public static CombatHoverDescriptionRow Spell(string definitionId)
            => !string.IsNullOrWhiteSpace(definitionId) && SpellRows.TryGetValue(definitionId, out CombatHoverDescriptionRow row)
                ? row : null;

        public static CombatHoverDescriptionRow Artifact(string definitionId)
            => !string.IsNullOrWhiteSpace(definitionId) && ArtifactRows.TryGetValue(definitionId, out CombatHoverDescriptionRow row)
                ? row : null;

        public static string SpellBody(SpellDefinition spell)
            => spell == null ? "尚未获得术式。" : (Spell(spell.DefinitionId) ?? BuildSpellRow(spell)).FormatBody();

        public static string ArtifactBody(ArtifactDefinition artifact, int remainingUses = -1, int maximumUses = -1)
        {
            if (artifact == null) return "尚未获得法宝。";
            string uses = remainingUses < 0 ? null : PlayerFacingCopy.RemainingAndTotal(
                remainingUses, maximumUses < 0 ? artifact.MaximumUses : maximumUses, " 次");
            return (Artifact(artifact.Id) ?? BuildArtifactRow(artifact)).FormatBody(uses);
        }

        private static CombatHoverDescriptionRow BuildSpellRow(SpellDefinition spell)
        {
            FireSpellDefinition fire = FireSpellCatalog.All.FirstOrDefault(candidate => candidate.Id == spell.DefinitionId);
            string passiveEffect = PassiveEffect(spell.DefinitionId);
            bool passive = !string.IsNullOrWhiteSpace(passiveEffect);
            string cost = passive ? "无主动消耗" : spell.ActionPointCost + " 行动点　" + spell.ManaCost + " 个人魔力";
            string cycle = passive ? "装备后持续生效" : spell.CooldownOwnTurns <= 0 ? "无冷却" : spell.CooldownOwnTurns + " 个自身回合";
            string target = passive ? "自身" : RogueliteSettlementPresentation.RogueSpellTargetSummary(spell);
            string condition = string.Empty;
            string effect = passive ? passiveEffect : RogueliteSettlementPresentation.RogueSpellPlayerSummary(spell);
            return new CombatHoverDescriptionRow(spell.DefinitionId, cost, cycle, target, condition, effect, SpellRisk(fire), SpellLore(spell, fire, passive));
        }

        private static CombatHoverDescriptionRow BuildArtifactRow(ArtifactDefinition artifact)
            => new CombatHoverDescriptionRow(artifact.Id, artifact.PublicCost, string.Empty,
                artifact.TargetSummary, string.Empty, artifact.EffectSummary, artifact.RiskSummary, ArtifactLore(artifact.Id));

        private static string ArtifactLore(string definitionId)
        {
            switch (definitionId)
            {
                case "F-T01": return "学院封装的试制炎脉器。";
                case "G-T01": return "护具工坊常见的折叠匣。";
                case "G-T02": return "旧式定距器留下的线轴。";
                case "G-T03": return "猎团用来固定猎物的框架。";
                case "G-T04": return "勘验人员随身携带的测镜。";
                case "G-T05": return "边境检修工坊的手压泵。";
                case "G-T06": return "教会救护工坊的折叠编架。";
                case "G-T07": return "守备石工搬运的重型压模。";
                case "G-T08": return "采石行会沿用的短柄楔。";
                case "G-T09": return "商路救援队校准的罗盘。";
                case "G-T10": return "猎团挂在隘口的铜铃。";
                case "G-T11": return "消防与勘验人员共用的冷凝器。";
                case "G-T12": return "旧文明旅人记录行程的簿册。";
                case "G-T13": return "驿站装卸工使用的三足支架。";
                case "G-T14": return "光学师校准镜片的便携器。";
                case "G-T15": return "商队驱兽灯改装的小灯。";
                case "G-T16": return "医护工坊校准护具的阀门。";
                case "G-T17": return "旧文明测绘队留下的铅锤。";
                case "G-T18": return "旧文明封存库留下的幕布。";
                case "G-T19": return "黑市回收的远古封签。";
                default: return "学院登记的便携式器材。";
            }
        }

        private static string PassiveEffect(string definitionId)
        {
            switch (definitionId)
            {
                case "PASSIVE-ELITE-01": return "每个自身回合第一次用个人火系术式造成伤害后，恢复 1 点个人魔力";
                case "PASSIVE-ELITE-02": return "一次移动至少 3 格后，下一次武器命中追加 4 点火焰伤害";
                case "PASSIVE-ELITE-03": return "自身回合开始时，若当前没有护盾，获得 3 点普通盾";
                default: return string.Empty;
            }
        }

        private static string SpellLore(SpellDefinition spell, FireSpellDefinition fire, bool passive)
        {
            if (passive) return "长期回路依赖个人训练与维护，不会脱离媒介自行运转。";
            if (fire == null)
            {
                return spell?.DefinitionId == "BASE-AETHER-SHIELD"
                    ? "护幕把以太素压成薄层，用来抵消现场飞溅。"
                    : spell?.DefinitionId == "BASE-MANA-RECOVER"
                        ? "调息是短时回路维护程序，不以透支身体换取输出。"
                        : "基础术式先教会施术者测量和约束以太素，再谈威力。";
            }

            switch (fire.DeliveryMode)
            {
                case FireDeliveryMode.BodyEnhancement:
                case FireDeliveryMode.Movement:
                    return "短时热脉增压把以太素导入人体回路，输出受体能与训练限制。";
                case FireDeliveryMode.WeaponAttachment:
                case FireDeliveryMode.ContactConduction:
                    return "近距导热由武器媒介承接，避免人体直接承担热量。";
                case FireDeliveryMode.SelfStance:
                    return "防护回路把散逸能量导回护层，属于可维护的能量工程。";
                case FireDeliveryMode.TargetMarking:
                    return "标记回路把延迟释放固定在可测量的位置，便于现场复核。";
                case FireDeliveryMode.FiregroundManipulation:
                    return "火场是被部署的热工区域，持续时间和危险范围都可测量。";
                default:
                    return "远程投射通过校准回路约束以太素，偏差会留下可复核记录。";
            }
        }

        private static string SpellRisk(FireSpellDefinition fire)
        {
            if (fire == null) return string.Empty;
            List<string> risks = new List<string>();
            if (fire.Rules.Any(rule => rule.AffectAllies)) risks.Add("范围可能伤及自身或友军，以预览为准");
            FireSpellRule healthLoss = fire.Rules.FirstOrDefault(rule => rule.Kind == FireRuleKind.LoseHealth);
            if (healthLoss.Kind == FireRuleKind.LoseHealth && healthLoss.Amount > 0)
                risks.Add("施术者会无视护盾失去 " + healthLoss.Amount + " 点生命");
            if (fire.InitiativeDelay > 0) risks.Add("施放后自身行动顺序延后 " + fire.InitiativeDelay);
            return string.Join("；", risks);
        }
    }
}
