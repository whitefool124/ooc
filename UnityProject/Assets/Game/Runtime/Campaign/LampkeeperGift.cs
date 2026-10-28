using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public sealed class LampkeeperGiftDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public LampkeeperGiftDefinition(string id, string name, string description)
        { Id = id; Name = name; Description = description; }
    }

    public static class LampkeeperGiftCatalog
    {
        public static readonly IReadOnlyList<LampkeeperGiftDefinition> All = new[]
        {
            new LampkeeperGiftDefinition("LK-01", "余烬灯芯", "每公共回合首次以个人术式新生成燃烧地格后，恢复1魔力。刷新旧火场不触发。"),
            new LampkeeperGiftDefinition("LK-02", "行路铜钉", "每自身回合主动移动累计3格后，获得3普通护盾，每自身回合一次。推拉、换位与传送不计。"),
            new LampkeeperGiftDefinition("LK-03", "断墙刻印", "每公共回合首次以自己的攻击或个人术式摧毁有耐久物块后，恢复2魔力。连锁爆炸不重复触发。"),
            new LampkeeperGiftDefinition("LK-04", "旧式护符", "每场战斗初始化时获得6普通护盾。续档与重试不额外叠加。"),
            new LampkeeperGiftDefinition("LK-05", "空白批注", "从3项未掌握且可配置的学院个人主动术式中，选择1项掌握。不自动装备。"),
            new LampkeeperGiftDefinition("LK-06", "封存钱袋", "立即获得12金币，本轮只领取一次。")
        };
        public static LampkeeperGiftDefinition Get(string id) => All.FirstOrDefault(x => x.Id == id)
            ?? throw new InvalidOperationException("Unknown lampkeeper gift: " + id);
    }

    /// <summary>Null in pre-gift archives: never backfill an existing round.</summary>
    public sealed class LampkeeperGiftState
    {
        public const int CurrentVersion = 1;
        public string[] CandidateIds { get; private set; }
        public string[] SpellCandidateIds { get; private set; }
        public string SelectedId { get; internal set; } = string.Empty;
        public string SelectedSpellId { get; internal set; } = string.Empty;
        public bool Resolved => !string.IsNullOrEmpty(SelectedId) && (SelectedId != "LK-05" || !string.IsNullOrEmpty(SelectedSpellId));
        public static LampkeeperGiftState Create(int seed, IEnumerable<string> eligibleSpells)
        {
            var random = new Random(unchecked(seed ^ 0x4c4b4731));
            var spells = eligibleSpells.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal)
                .OrderBy(_ => random.Next()).Take(3).ToArray();
            var gifts = LampkeeperGiftCatalog.All.Where(x => x.Id != "LK-05" || spells.Length > 0)
                .OrderBy(_ => random.Next()).Take(3).Select(x => x.Id).ToArray();
            return new LampkeeperGiftState { CandidateIds = gifts, SpellCandidateIds = spells };
        }
        public string Serialize() => string.Join("~", CurrentVersion, string.Join(",", CandidateIds),
            string.Join(",", SpellCandidateIds), SelectedId, SelectedSpellId);
        public static LampkeeperGiftState Deserialize(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;
            var p = raw.Split('~');
            if (p.Length != 5 || p[0] != "1") throw new InvalidOperationException("Invalid lampkeeper version.");
            var state = new LampkeeperGiftState { CandidateIds = p[1].Split(','),
                SpellCandidateIds = p[2].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries), SelectedId = p[3], SelectedSpellId = p[4] };
            if (state.CandidateIds.Length != 3 || state.CandidateIds.Distinct().Count() != 3 ||
                state.SpellCandidateIds.Length > 3 || state.SpellCandidateIds.Distinct().Count() != state.SpellCandidateIds.Length)
                throw new InvalidOperationException("Invalid lampkeeper candidates.");
            foreach (var id in state.CandidateIds) LampkeeperGiftCatalog.Get(id);
            foreach (var id in state.SpellCandidateIds) FireSpellCatalog.Get(id);
            if ((!string.IsNullOrEmpty(state.SelectedId) && !state.CandidateIds.Contains(state.SelectedId)) ||
                (!string.IsNullOrEmpty(state.SelectedSpellId) && (state.SelectedId != "LK-05" || !state.SpellCandidateIds.Contains(state.SelectedSpellId))) ||
                (state.CandidateIds.Contains("LK-05") && state.SpellCandidateIds.Length == 0))
                throw new InvalidOperationException("Invalid lampkeeper selection.");
            return state;
        }
    }

    public sealed partial class RogueliteMapRun
    {
        public LampkeeperGiftState OpeningGift => rogueRunDto?.OpeningGift;
        public bool OpeningGiftPending => OpeningGift != null && !OpeningGift.Resolved && !IsComplete;
        public void ChooseOpeningGift(string id)
        {
            if (!OpeningGiftPending || !string.IsNullOrEmpty(OpeningGift.SelectedId) || !OpeningGift.CandidateIds.Contains(id))
                throw new InvalidOperationException("This opening gift cannot be selected.");
            LampkeeperGiftCatalog.Get(id);
            OpeningGift.SelectedId = id;
            if (id == "LK-06") rogueRunDto.Gold += 12;
        }
        public void ChooseOpeningGiftSpell(string id)
        {
            if (!OpeningGiftPending || OpeningGift.SelectedId != "LK-05" || !OpeningGift.SpellCandidateIds.Contains(id))
                throw new InvalidOperationException("This opening spell cannot be selected.");
            if (!rogueRunDto.MasteredSpellIds.Contains(id)) rogueRunDto.MasteredSpellIds.Add(id);
            if (!ownedFireSpells.Contains(id)) ownedFireSpells.Add(id);
            OpeningGift.SelectedSpellId = id;
        }
    }
}
