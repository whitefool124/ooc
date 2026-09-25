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
            int goldGain = 0, int contributionGain = 0, int healthGain = 0, int manaGain = 0)
            => new RogueliteNodeContentChoice(id, name, preview, effect, reward, combat,
                combat ? "relay_event" : null, goldCost: goldCost, contributionCost: contributionCost,
                goldGain: goldGain, contributionGain: contributionGain, healthGain: healthGain,
                manaGain: manaGain);

        public static readonly IReadOnlyList<AcademyEventDefinition> Events = new[]
        {
            E("EV01", "新生实战委托", "中庭",
                C("EV01_defence", "领取防御训练器材", "花 1 学院贡献，带走折盾匣；用时 1。", reward:"G-T01", contributionCost:1),
                C("EV01_drill", "参加侧锋对练", "击倒盾术生与侧锋生即胜；胜利额外获得 1 金币，失败不扣基础资源。", combat:true, goldGain:1)),
            E("EV02", "档案室异常索引", "教学",
                C("EV02_index", "购买索引抄本", "花 1 学院贡献，换得 3 金币；用时 1。", contributionCost:1, goldGain:3),
                C("EV02_leave", "登记后离开", "不花东西，得到 1 学院贡献；用时 1。", contributionGain:1)),
            E("EV03", "被封存的观察记录", "教学",
                C("EV03_fight", "守住档案库", "完成档案库演练，胜利获得 3 金币与 2 学院贡献；输了且存活时只得一半。", combat:true),
                C("EV03_archive", "封存记录", "不花东西，得到 1 学院贡献；用时 1。", contributionGain:1)),
            E("EV04", "器材登记窗口", "工坊",
                C("EV04_medical", "领取复元编架", "花 2 学院贡献，带走复元编架；用时 1。", reward:"G-T06", contributionCost:2),
                C("EV04_calibrate", "帮忙校准导具", "不花东西，恢复 2 个人魔力；用时 1。", manaGain:2)),
            E("EV05", "退货的护盾组件", "市集",
                C("EV05_shield", "购入折盾匣", "花 4 金币，带走折盾匣；用时 1。", reward:"G-T01", goldCost:4),
                C("EV05_exchange", "帮忙处理退货", "不花东西，得到 1 学院贡献；用时 1。", contributionGain:1)),
            E("EV06", "观测塔求援信号", "郊野",
                C("EV06_rescue", "前往救援", "完成救援演练，胜利获得 3 金币与 2 学院贡献；输了且存活时只得一半。", combat:true),
                C("EV06_survey", "购买测绘情报", "花 2 金币，得到 2 学院贡献；用时 1。", goldCost:2, contributionGain:2)),
            E("EV07", "路障与巡查告示", "郊野",
                C("EV07_clear", "协助清障", "会受到 2 点伤害，完成后得到 3 金币和 1 学院贡献；用时 1。", goldGain:3, contributionGain:1, healthGain:-2),
                C("EV07_detour", "绕路测绘", "不花东西，得到 2 金币；用时 1。", goldGain:2)),
            E("EV08", "高塔值守记录", "封存",
                C("EV08_read", "查阅维护记录", "花 1 学院贡献，得到 3 金币；用时 1。", contributionCost:1, goldGain:3),
                C("EV08_recover", "恢复回路", "免费恢复至多 3 点个人魔力；魔力已满时不可选；用时 1。", manaGain:3)),
            E("EV09", "医务室临时征集", "中庭",
                C("EV09_treatment", "接受治疗", "花 3 金币，恢复 6 生命；用时 1。", goldCost:3, healthGain:6),
                C("EV09_escort", "帮忙护送", "护送途中会遇到对手。赢了得复元编架；输了拿不到。", reward:"G-T06", combat:true)),
            E("EV10", "赞助账目追索", "市集",
                C("EV10_pay_gold", "结清账目", "花 3 金币，得到 2 学院贡献；用时 1。", goldCost:3, contributionGain:2),
                C("EV10_exam", "用考核抵账", "打赢可得 4 金币；输了只得 1 金币。", combat:true, goldGain:1)),
            E("EV11", "猎团旧标记", "郊野",
                C("EV11_lens", "回收显迹测镜", "花 2 学院贡献，带走显迹测镜；用时 1。", reward:"G-T04", contributionCost:2),
                C("EV11_tracker", "参加寻迹兽演练", "制服寻迹兽就能拿到奖励；输了没有额外收获。", combat:true, goldGain:0, contributionGain:0)),
            E("EV12", "术式抄录争议", "教学",
                C("EV12_copy", "购买术式抄本", "花 4 金币，学会一个普通火术式；用时 1。", reward:"F-P-U03", goldCost:4),
                C("EV12_audit", "帮忙核对抄本", "不花东西，得到 2 学院贡献；用时 1。", contributionGain:2)),
            E("EV13", "护具压力复核", "工坊",
                C("EV13_parts", "更换折盾匣", "花 3 金币，带走折盾匣；用时 1。", reward:"G-T01", goldCost:3),
                C("EV13_chain", "挑战维护队", "打赢维护队，带走远投定距杖；输了拿不到。", reward:"ACA-EQ-DG01", combat:true)),
            E("EV14", "临时医护征集", "市集",
                C("EV14_donate", "帮忙分拣药材", "不花东西，得到 1 学院贡献；用时 1。", contributionGain:1),
                C("EV14_recover", "接受简单治疗", "花 1 学院贡献，恢复 4 生命和 2 个人魔力；用时 1。", contributionCost:1, healthGain:4, manaGain:2)),
            E("EV15", "导能柱错位读数", "郊野",
                C("EV15_measure", "购买校准记录", "花 1 学院贡献，得到 3 金币；用时 1。", contributionCost:1, goldGain:3),
                C("EV15_objective", "亲自校准导能柱", "破坏失控的导能柱就算完成。赢了得险地冷凝器；输了拿不到。", reward:"G-T11", combat:true)),
            E("EV16", "维护链替班", "封存",
                C("EV16_support", "请人准备护具", "花 2 学院贡献，带走折盾匣；用时 1。", reward:"G-T01", contributionCost:2),
                C("EV16_assessment", "接受维护队考核", "完成维护队考核，胜利获得 3 金币与 2 学院贡献；输了且存活时只得一半。", combat:true)),
            E("T02", "封存管理员匣", "封存",
                C("vault_fire_cache", "领取冒险封签", "获得冒险封签；不是首领门槛或通行凭证。", reward:"G-T19"))
        };

        private static AcademyEventDefinition E(string id, string name, string region, params RogueliteNodeContentChoice[] choices)
            => new AcademyEventDefinition(id, name, region, choices);

        public static AcademyEventDefinition Event(string id) => Events.Single(value => value.Id == id);

        public static IReadOnlyList<RogueliteNodeContentChoice> FunctionChoices(RogueliteMapNode node)
        {
            switch (node.Type)
            {
                case RogueliteMapNodeType.Medical:
                    return new[]
                    {
                        new RogueliteNodeContentChoice("field_repair", "接受治疗", "花 1 学院贡献，恢复 6 生命和 4 个人魔力；不花时间。", RogueliteNodeContentEffect.Recovery,
                            contributionCost:1, healthGain:6, manaGain:4),
                        new RogueliteNodeContentChoice("scan_routes", "帮忙清点补给", "花 2 金币，得到 1 学院贡献；不花时间。", RogueliteNodeContentEffect.Economy,
                            goldCost:2, contributionGain:1)
                    };
                case RogueliteMapNodeType.Workshop:
                    return new[]
                    {
                        new RogueliteNodeContentChoice("wand_calibration", "校准远投定距杖", "花 4 金币，带走远投定距杖；不花时间。", RogueliteNodeContentEffect.Reward, "ACA-EQ-DG01", goldCost:4),
                        new RogueliteNodeContentChoice("supply_strip", "帮忙整理器材", "不花东西，得到 1 学院贡献；不花时间。", RogueliteNodeContentEffect.Economy, contributionGain:1)
                    };
                case RogueliteMapNodeType.Shop:
                    return new[]
                    {
                        new RogueliteNodeContentChoice("medical_cache", "购买复元编架", "花 4 金币，带走复元编架；不花时间。", RogueliteNodeContentEffect.Reward, "G-T06", partsCost:2, goldCost:4),
                        new RogueliteNodeContentChoice("signal_contract", "出售一份情报", "交出 1 学院贡献，得到 3 金币；不花时间。", RogueliteNodeContentEffect.Economy, contributionCost:1, goldGain:3),
                        new RogueliteNodeContentChoice("buy_hazard_condenser", "购入险地冷凝器", "花 5 金币，带走险地冷凝器；不花时间。", RogueliteNodeContentEffect.Reward, "G-T11", goldCost:5)
                    };
                case RogueliteMapNodeType.Event when node.Id == "core_vault" || node.Id == "tower_lift":
                    if (string.Equals(node.Id, "core_vault", StringComparison.Ordinal))
                        return new[]
                        {
                            new RogueliteNodeContentChoice("vault_fire_cache", "拿走冒险封签", "带走冒险封签，险地冷凝器会留在这里；不花时间。", RogueliteNodeContentEffect.Reward, "G-T19"),
                            new RogueliteNodeContentChoice("vault_hazard_cache", "拿走险地冷凝器", "带走险地冷凝器，冒险封签会留在这里；不花时间。", RogueliteNodeContentEffect.Reward, "G-T11")
                        };
                    return new[]
                    {
                        new RogueliteNodeContentChoice("vault_fire_cache", "拿走冒险封签", "领取冒险封签，随后可以继续旅程。", RogueliteNodeContentEffect.Reward, "G-T19")
                    };
                default: return Array.Empty<RogueliteNodeContentChoice>();
            }
        }

        public static IReadOnlyList<AcademyEventAssignment> GenerateAssignments(int seed)
        {
            RogueliteMapNode[] nodes = RogueliteMapCatalog.Nodes.Where(value => value.Type == RogueliteMapNodeType.Event &&
                    value.Id != "core_vault" && value.Id != "tower_lift")
                .OrderBy(value => StableKey(seed, "node|" + value.Id)).ToArray();
            List<AcademyEventDefinition> remaining = Events.Where(value => value.Id != "T02")
                .OrderBy(value => StableKey(seed, "event|" + value.Id)).ToList();
            List<AcademyEventAssignment> assignments = new List<AcademyEventAssignment>();
            foreach (RogueliteMapNode node in nodes)
            {
                AcademyEventDefinition selected = remaining.First();
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
