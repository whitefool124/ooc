using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public sealed class AcademyEventDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Region { get; }
        public IReadOnlyList<RogueliteNodeContentChoice> Choices { get; }

        public AcademyEventDefinition(string id, string displayName, string region, params RogueliteNodeContentChoice[] choices)
        {
            Id = id; DisplayName = displayName; Region = region;
            Choices = choices ?? Array.Empty<RogueliteNodeContentChoice>();
        }
    }

    public readonly struct AcademyEventAssignment
    {
        public string NodeId { get; }
        public string EventId { get; }
        public AcademyEventAssignment(string nodeId, string eventId) { NodeId = nodeId; EventId = eventId; }
    }

    public static class AcademyNodeContentCatalog
    {
        private static RogueliteNodeContentChoice C(string id, string name, string preview,
            RogueliteNodeContentEffect effect = RogueliteNodeContentEffect.Economy,
            string reward = null, bool combat = false, int goldCost = 0, int contributionCost = 0,
            int goldGain = 0, int contributionGain = 0, int healthGain = 0, int manaGain = 0,
            bool permit = false)
            => new RogueliteNodeContentChoice(id, name, preview, effect, reward, combat,
                combat ? "relay_event" : null, goldCost: goldCost, contributionCost: contributionCost,
                goldGain: goldGain, contributionGain: contributionGain, healthGain: healthGain,
                manaGain: manaGain, grantsCorePermit: permit);

        public static readonly IReadOnlyList<AcademyEventDefinition> Events = new[]
        {
            E("EV01", "新生实战委托", "中庭",
                C("EV01_defence", "领取防御训练许可", "成本：1 贡献；收益：折盾匣；时序 +1，结算恢复按时序公开。", reward:"G-T01", contributionCost:1),
                C("EV01_drill", "参加追加演练", "进入已预览的二人演练；胜利得 4 金币，存活失败只得 1 金币且无奖励。", combat:true, goldGain:1)),
            E("EV02", "档案室异常索引", "教学",
                C("EV02_index", "购买索引抄本", "成本：1 贡献；收益：3 金币等价的公开情报；时序 +1。", contributionCost:1, goldGain:3),
                C("EV02_leave", "登记后离开", "无资源成本；收益：1 贡献；时序 +1。", contributionGain:1)),
            E("EV03", "被封存的观察记录", "教学",
                C("EV03_fight", "进入档案库演练", "进入已预览战斗；胜利得核心许可，失败不给许可。", combat:true, permit:true),
                C("EV03_archive", "封存记录", "无资源成本；收益：1 贡献；时序 +1。", contributionGain:1)),
            E("EV04", "器材登记窗口", "工坊",
                C("EV04_medical", "领取复元编架", "成本：2 贡献；收益：复元编架；时序 +1。", reward:"G-T06", contributionCost:2),
                C("EV04_calibrate", "协助基础校准", "无资源成本；收益：2 个人魔力；时序 +1。", manaGain:2)),
            E("EV05", "退货的护盾组件", "市集",
                C("EV05_shield", "购入折盾匣", "成本：4 金币；收益：折盾匣；时序 +1。", reward:"G-T01", goldCost:4),
                C("EV05_exchange", "登记退货", "无资源成本；收益：1 贡献；时序 +1。", contributionGain:1)),
            E("EV06", "观测塔求援信号", "郊野",
                C("EV06_rescue", "进入救援演练", "进入已预览战斗；胜利得核心许可，失败不给许可。", combat:true, permit:true),
                C("EV06_survey", "购买测绘情报", "成本：2 金币；收益：2 贡献；时序 +1。", goldCost:2, contributionGain:2)),
            E("EV07", "路障与巡查告示", "郊野",
                C("EV07_clear", "协助清障", "成本：2 生命；收益：3 金币与 1 贡献；时序 +1。", goldGain:3, contributionGain:1, healthGain:-2),
                C("EV07_detour", "绕行测绘", "无资源成本；收益：2 金币；时序 +1。", goldGain:2)),
            E("EV08", "高塔值守记录", "封存",
                C("EV08_read", "查阅维护记录", "成本：1 贡献；收益：3 金币等价情报；时序 +1。", contributionCost:1, goldGain:3),
                C("EV08_elite", "接受追加核验", "进入已预览精英变体；胜利得冒险封签，失败不给唯一物。", reward:"G-T19", combat:true)),
            E("EV09", "医务室临时征集", "中庭",
                C("EV09_treatment", "支付治疗费用", "成本：3 金币；收益：恢复 6 生命；时序 +1。", goldCost:3, healthGain:6),
                C("EV09_escort", "完成护送演练", "进入已预览战斗；胜利得复元编架，失败不给道具。", reward:"G-T06", combat:true)),
            E("EV10", "赞助账目追索", "市集",
                C("EV10_pay_gold", "结清账目", "成本：3 金币；收益：2 贡献；时序 +1。", goldCost:3, contributionGain:2),
                C("EV10_exam", "参加抵偿考核", "进入已预览战斗；胜利得 4 金币，失败仅得基础失败档。", combat:true, goldGain:1)),
            E("EV11", "猎团旧标记", "郊野",
                C("EV11_lens", "回收显迹测镜", "成本：2 贡献；收益：显迹测镜；时序 +1。", reward:"G-T04", contributionCost:2),
                C("EV11_tracker", "寻迹兽演练", "进入已预览战斗；胜利按战斗档结算，存活失败只得基础失败档。", combat:true, goldGain:0, contributionGain:0)),
            E("EV12", "术式抄录争议", "教学",
                C("EV12_copy", "购买术式抄本", "成本：4 金币；收益：普通火术候选；时序 +1。", reward:"F-P-U03", goldCost:4),
                C("EV12_audit", "协助核对", "无资源成本；收益：2 贡献；时序 +1。", contributionGain:2)),
            E("EV13", "护具压力复核", "工坊",
                C("EV13_parts", "更换折盾匣", "成本：3 金币；收益：折盾匣；时序 +1。", reward:"G-T01", goldCost:3),
                C("EV13_chain", "挑战维护链", "进入已预览战斗；胜利得远投定距杖，存活失败只得基础失败档。", reward:"ACA-EQ-DG01", combat:true)),
            E("EV14", "临时医护征集", "市集",
                C("EV14_donate", "协助分拣", "无资源成本；收益：1 贡献；时序 +1。", contributionGain:1),
                C("EV14_recover", "接受有限治疗", "成本：1 贡献；收益：恢复 4 生命与 2 魔力；时序 +1。", contributionCost:1, healthGain:4, manaGain:2)),
            E("EV15", "导能柱错位读数", "郊野",
                C("EV15_measure", "购买校准记录", "成本：1 贡献；收益：3 金币等价情报；时序 +1。", contributionCost:1, goldGain:3),
                C("EV15_objective", "现场校准", "进入已预览破坏目标战；胜利得险地冷凝器，失败不给唯一物。", reward:"G-T11", combat:true)),
            E("EV16", "维护链替班", "封存",
                C("EV16_support", "购买战前支援", "成本：2 贡献；收益：折盾匣；时序 +1。", reward:"G-T01", contributionCost:2),
                C("EV16_permit", "参加维护链核验", "进入已预览精英战；胜利得核心许可，失败不给许可。", combat:true, permit:true))
        };

        private static AcademyEventDefinition E(string id, string name, string region, params RogueliteNodeContentChoice[] choices)
            => new AcademyEventDefinition(id, name, region, choices);

        public static AcademyEventDefinition Event(string id) => Events.Single(value => value.Id == id);

        public static IReadOnlyList<RogueliteNodeContentChoice> FunctionChoices(RogueliteMapNode node)
        {
            switch (node.Type)
            {
                case RogueliteMapNodeType.Rest:
                    return new[]
                    {
                        new RogueliteNodeContentChoice("field_repair", "有限治疗", "成本：1 贡献；恢复 6 生命与 4 个人魔力；零时序，不免费满血。", RogueliteNodeContentEffect.Recovery,
                            contributionCost:1, healthGain:6, manaGain:4),
                        new RogueliteNodeContentChoice("scan_routes", "补给登记", "成本：2 金币；收益：1 贡献；零时序。", RogueliteNodeContentEffect.Economy,
                            goldCost:2, contributionGain:1)
                    };
                case RogueliteMapNodeType.Workshop:
                    return new[]
                    {
                        new RogueliteNodeContentChoice("wand_calibration", "导具校准", "成本：4 金币；收益：远投定距杖；零时序。", RogueliteNodeContentEffect.Reward, "ACA-EQ-DG01", goldCost:4),
                        new RogueliteNodeContentChoice("supply_strip", "协助整理器材", "无资源成本；收益：1 贡献；零时序。", RogueliteNodeContentEffect.Economy, contributionGain:1)
                    };
                case RogueliteMapNodeType.Shop:
                    return new[]
                    {
                        new RogueliteNodeContentChoice("medical_cache", "医疗补给", "成本：4 金币；收益：复元编架；零时序。", RogueliteNodeContentEffect.Reward, "G-T06", partsCost:2, goldCost:4),
                        new RogueliteNodeContentChoice("signal_contract", "情报登记", "成本：1 贡献；收益：3 金币；零时序。", RogueliteNodeContentEffect.Economy, contributionCost:1, goldGain:3),
                        new RogueliteNodeContentChoice("buy_hazard_condenser", "购入险地冷凝器", "成本：5 金币；收益：险地冷凝器；零时序。", RogueliteNodeContentEffect.Reward, "G-T11", goldCost:5)
                    };
                case RogueliteMapNodeType.Treasure:
                    if (string.Equals(node.Id, "core_vault", StringComparison.Ordinal))
                        return new[]
                        {
                            new RogueliteNodeContentChoice("vault_fire_cache", "领取冒险封签", "收益：冒险封签；与险地冷凝器互斥；零时序。", RogueliteNodeContentEffect.Reward, "G-T19"),
                            new RogueliteNodeContentChoice("vault_hazard_cache", "领取险地冷凝器", "收益：险地冷凝器；与冒险封签互斥；零时序。", RogueliteNodeContentEffect.Reward, "G-T11")
                        };
                    return new[]
                    {
                        new RogueliteNodeContentChoice("vault_fire_cache", "领取稀有法宝", "收益：冒险封签；与核心许可互斥；零时序。", RogueliteNodeContentEffect.Reward, "G-T19"),
                        new RogueliteNodeContentChoice("vault_core_permit", "领取核心许可", "收益：1 枚核心许可；与稀有法宝互斥；零时序。", RogueliteNodeContentEffect.AccessCard, grantsCorePermit:true)
                    };
                default: return Array.Empty<RogueliteNodeContentChoice>();
            }
        }

        public static IReadOnlyList<AcademyEventAssignment> GenerateAssignments(int seed)
        {
            RogueliteMapNode[] nodes = RogueliteMapCatalog.Nodes.Where(value => value.Type == RogueliteMapNodeType.Event)
                .OrderBy(value => StableKey(seed, "node|" + value.Id)).ToArray();
            List<AcademyEventDefinition> remaining = Events.OrderBy(value => StableKey(seed, "event|" + value.Id)).ToList();
            List<AcademyEventAssignment> assignments = new List<AcademyEventAssignment>();
            foreach (RogueliteMapNode node in nodes)
            {
                AcademyEventDefinition selected = remaining.FirstOrDefault(value =>
                    node.GrantedAccessCards <= 0 || value.Choices.All(choice => !choice.GrantsCorePermit));
                if (selected == null) throw new InvalidOperationException("Academy event assignment cannot avoid a duplicate permit source.");
                assignments.Add(new AcademyEventAssignment(node.Id, selected.Id));
                remaining.Remove(selected);
            }
            return assignments;
        }

        private static int StableKey(int seed, string value)
        {
            unchecked { int hash = seed * 397; foreach (char c in value ?? string.Empty) hash = hash * 31 + c; return hash; }
        }
    }
}
