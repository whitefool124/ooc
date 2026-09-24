using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using OCC.Combat.Roguelite;

namespace OCC.Combat.Presentation
{
    public sealed partial class FormalRogueliteUi
    {
        private sealed class EncyclopediaEntry
        {
            public readonly string Name;
            public readonly string Category;
            public readonly string Search;
            public readonly FormalTooltipContent Card;
            public EncyclopediaEntry(string name, string category, string search, FormalTooltipContent card)
            {
                Name = name; Category = category; Search = search; Card = card;
            }
        }

        private static readonly string[] EncyclopediaSections =
            { "术式", "装备", "法宝", "战斗规则", "状态与地形", "敌人与机关" };
        private string encyclopediaSection = "术式";
        private string encyclopediaBuildFilter = "全部";
        private string encyclopediaQuery = string.Empty;
        private Transform encyclopediaGallery;
        private Text encyclopediaResultCount;
        private ScrollRect encyclopediaScroll;

        private void DrawEncyclopedia()
        {
            Header("战斗百科", "术式 · 装备 · 法宝 · 战斗档案");
            GameObject heading = content.transform.Find("页眉")?.gameObject;
            if (heading != null)
            {
                ArchiveUiStyle.PaperPanel(heading, ArchiveUiStyle.Paper, false);
                foreach (Text label in heading.GetComponentsInChildren<Text>())
                    label.color = label.name == "标题" ? ArchiveUiStyle.Ink : ArchiveUiStyle.QuietInk;
            }
            GameObject sidebar = Panel("百科分类", content.transform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(24, -78), new Vector2(236, 938), FormalUiTheme.SurfaceRaised);
            ArchiveUiStyle.PaperPanel(sidebar, ArchiveUiStyle.Paper, true);
            Label("分类标题", "卷宗目录", sidebar.transform, new Vector2(20, -18), new Vector2(190, 42), 24, ArchiveUiStyle.Ink, TextAnchor.MiddleLeft);
            Line(sidebar.transform, new Vector2(20, -62), new Vector2(196, 1), ArchiveUiStyle.Rule);
            for (int i = 0; i < EncyclopediaSections.Length; i++)
            {
                string section = EncyclopediaSections[i];
                GameObject tab = ActionButton(section, string.Empty, sidebar.transform, new Vector2(14, -76 - i * 76),
                    new Vector2(208, 62), section == encyclopediaSection ? amber : cyan, true,
                    () => { encyclopediaSection = section; Invalidate(false); }, "按钮_百科_" + section);
                ArchiveUiStyle.TabButton(tab.GetComponent<Button>(), section == encyclopediaSection,
                    () => UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity));
            }
            if (encyclopediaSection == "术式")
            {
                Label("构筑标题", "构筑词条", sidebar.transform, new Vector2(18, -528),
                    new Vector2(198, 34), 17, ArchiveUiStyle.QuietInk, TextAnchor.MiddleLeft);
                Line(sidebar.transform, new Vector2(20, -565), new Vector2(196, 1), ArchiveUiStyle.Rule);
                string[] filters = { "全部", SpellBuildTagCatalog.Dash, SpellBuildTagCatalog.Breach, SpellBuildTagCatalog.Fireground };
                for (int i = 0; i < filters.Length; i++)
                {
                    string filter = filters[i];
                    GameObject tab = ActionButton(filter, string.Empty, sidebar.transform, new Vector2(14, -568 - i * 60),
                        new Vector2(208, 56), filter == encyclopediaBuildFilter ? amber : cyan, true,
                        () => { encyclopediaBuildFilter = filter; Invalidate(false); }, "按钮_构筑_" + filter);
                    ArchiveUiStyle.TabButton(tab.GetComponent<Button>(), filter == encyclopediaBuildFilter,
                        () => UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity));
                }
            }
            GameObject back = ActionButton("返回", string.Empty, sidebar.transform, new Vector2(14, -864), new Vector2(208, 58), cyan, true,
                () => SetOverlay(UiOverlay.None), "按钮_返回", FormalArtRegistry.NavigationPath("back"));
            ArchiveUiStyle.TabButton(back.GetComponent<Button>(), false,
                () => UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity));

            GameObject searchPanel = Panel("百科检索", content.transform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(280, -78), new Vector2(1596, 64), FormalUiTheme.SurfaceRaised);
            ArchiveUiStyle.PaperPanel(searchPanel, ArchiveUiStyle.Paper, true);
            Label("搜索标签", "检索", searchPanel.transform, new Vector2(20, -10), new Vector2(80, 44), 20, ArchiveUiStyle.Ink, TextAnchor.MiddleLeft);
            GameObject inputRoot = Panel("百科搜索框", searchPanel.transform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(100, -8), new Vector2(980, 48), FormalUiTheme.Surface);
            ArchiveUiStyle.PaperPanel(inputRoot, ArchiveUiStyle.LightPaper, true);
            InputField input = inputRoot.AddComponent<InputField>();
            Text value = Label("输入", encyclopediaQuery, inputRoot.transform, new Vector2(14, -5),
                new Vector2(950, 38), 18, ArchiveUiStyle.Ink, TextAnchor.MiddleLeft);
            Text placeholder = Label("提示", "输入名称或效果关键词", inputRoot.transform, new Vector2(14, -5),
                new Vector2(950, 38), 18, ArchiveUiStyle.QuietInk, TextAnchor.MiddleLeft);
            input.textComponent = value;
            input.placeholder = placeholder;
            input.text = encyclopediaQuery;
            input.onValueChanged.AddListener(query =>
            {
                encyclopediaQuery = query ?? string.Empty;
                RefreshEncyclopediaGallery();
            });
            GameObject clear = ActionButton("清空", string.Empty, searchPanel.transform, new Vector2(1092, -8),
                new Vector2(124, 48), cyan, true, () => input.text = string.Empty, "按钮_清空检索");
            ArchiveUiStyle.TabButton(clear.GetComponent<Button>(), false,
                () => UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity));
            Text clearLabel = clear.transform.Find("名称")?.GetComponent<Text>();
            if (clearLabel != null)
            {
                clearLabel.fontSize = 18;
                clearLabel.rectTransform.anchoredPosition = new Vector2(16, -6);
                clearLabel.rectTransform.sizeDelta = new Vector2(92, 36);
            }
            encyclopediaResultCount = Label("结果数量", string.Empty, searchPanel.transform,
                new Vector2(1230, -10), new Vector2(330, 44), 18, ArchiveUiStyle.QuietInk, TextAnchor.MiddleRight);

            GameObject scrollObject = Panel("百科卡牌区", content.transform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(280, -156), new Vector2(1596, 860), FormalUiTheme.Surface);
            encyclopediaScroll = scrollObject.AddComponent<ScrollRect>();
            encyclopediaScroll.horizontal = false;
            encyclopediaScroll.vertical = true;
            encyclopediaScroll.scrollSensitivity = 42f;
            GameObject viewport = Create("卡牌视口", scrollObject.transform);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            Stretch(viewportRect);
            Image maskImage = viewport.AddComponent<Image>();
            maskImage.color = Color.white;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            GameObject gallery = Create("卡牌排列", viewport.transform);
            RectTransform galleryRect = gallery.AddComponent<RectTransform>();
            galleryRect.anchorMin = galleryRect.anchorMax = galleryRect.pivot = new Vector2(0, 1);
            galleryRect.anchoredPosition = Vector2.zero;
            galleryRect.sizeDelta = new Vector2(1552, 0);
            encyclopediaGallery = gallery.transform;
            encyclopediaScroll.viewport = viewportRect;
            encyclopediaScroll.content = galleryRect;
            RefreshEncyclopediaGallery();
        }

        private void RefreshEncyclopediaGallery()
        {
            if (encyclopediaGallery == null) return;
            foreach (Transform child in encyclopediaGallery)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            List<EncyclopediaEntry> entries = BuildEncyclopediaEntries()
                .Where(entry => entry.Category == encyclopediaSection &&
                    (encyclopediaSection != "术式" || encyclopediaBuildFilter == "全部" ||
                     entry.Card.Status == encyclopediaBuildFilter) &&
                    (string.IsNullOrWhiteSpace(encyclopediaQuery) ||
                     entry.Search.IndexOf(encyclopediaQuery.Trim(), StringComparison.OrdinalIgnoreCase) >= 0))
                .OrderBy(entry => entry.Name, StringComparer.Ordinal).ToList();
            if (encyclopediaResultCount != null) encyclopediaResultCount.text = "找到 " + entries.Count + " 项";
            if (entries.Count == 0)
                Label("空结果", "没有符合条件的内容。可清空检索或选择其他目录。", encyclopediaGallery,
                    new Vector2(36, -36), new Vector2(900, 56), 24, muted, TextAnchor.MiddleLeft);
            bool fullCards = encyclopediaSection == "术式" || encyclopediaSection == "装备" || encyclopediaSection == "法宝";
            int columns = 3;
            float stepX = 508f;
            float stepY = fullCards ? encyclopediaSection == "术式" ? 480f : 500f : 154f;
            for (int i = 0; i < entries.Count; i++)
            {
                Vector2 position = new Vector2(26 + (i % columns) * stepX, -24 - (i / columns) * stepY);
                if (fullCards) DrawEncyclopediaContentCard(entries[i], position);
                else DrawEncyclopediaRuleCard(entries[i], position);
            }
            RectTransform rect = encyclopediaGallery as RectTransform;
            if (rect != null) rect.sizeDelta = new Vector2(1552, Math.Max(860f, 48f + (float)Math.Ceiling(entries.Count / (double)columns) * stepY));
            if (encyclopediaScroll != null) encyclopediaScroll.verticalNormalizedPosition = 1f;
        }

        private void DrawEncyclopediaContentCard(EncyclopediaEntry entry, Vector2 position)
        {
            FormalTooltipContent info = entry.Card;
            bool isSpell = entry.Category == "术式";
            float cardHeight = isSpell ? 450f : 470f;
            float detailsOffset = isSpell ? 20f : 0f;
            GameObject card = Panel("百科卡_" + entry.Name, encyclopediaGallery, new Vector2(0, 1), new Vector2(0, 1),
                position, new Vector2(480, cardHeight), FormalUiTheme.SurfaceRaised);
            Line(card.transform, Vector2.zero, new Vector2(4, cardHeight), info.Accent);
            Label("类别", info.Category, card.transform, new Vector2(20, -14), new Vector2(220, 28), 16, info.Accent, TextAnchor.MiddleLeft);
            if (!isSpell)
                Label("状态", info.Status, card.transform, new Vector2(256, -14), new Vector2(204, 28), 15, muted, TextAnchor.MiddleRight);
            Sprite sprite = Resources.Load<Sprite>(info.IconPath);
            Image icon = FormalUiKit.TopLeftIconSlot("内容图标", card.transform, sprite, new Vector2(24, -54));
            icon.rectTransform.sizeDelta = new Vector2(64, 64);
            Label("名称", info.Title, card.transform, new Vector2(104, -50), new Vector2(350, 40), 24, text, TextAnchor.MiddleLeft);
            if (!isSpell)
                Label("身份", info.Identity, card.transform, new Vector2(104, -92), new Vector2(350, 36), 16, muted, TextAnchor.MiddleLeft);
            string[] metrics = { info.MetricA, info.MetricB, info.MetricC };
            for (int i = 0; i < metrics.Length; i++)
            {
                Label("指标" + i, metrics[i], card.transform, new Vector2(20 + i * 150, -150 + detailsOffset),
                    new Vector2(140, 40), 15, text, TextAnchor.MiddleLeft);
            }
            for (int i = 0; i < Math.Min(6, info.Tags.Count); i++)
            {
                Label("词条_" + i, info.Tags[i], card.transform, new Vector2(20 + i * 74, -200 + detailsOffset),
                    new Vector2(68, 30), 13, info.Accent, TextAnchor.MiddleCenter);
            }
            Line(card.transform, new Vector2(20, -244 + detailsOffset), new Vector2(440, 2), muted);
            Text effect = Label("效果", info.Effect, card.transform, new Vector2(20, -258 + detailsOffset),
                new Vector2(440, 156), 17, text, TextAnchor.UpperLeft);
            FormalUiKit.ConfigureParagraph(effect);
            effect.verticalOverflow = VerticalWrapMode.Truncate;
            Line(card.transform, new Vector2(20, -420 + detailsOffset), new Vector2(440, 2), muted);
            Label("简介", info.Summary, card.transform, new Vector2(20, -428 + detailsOffset), new Vector2(440, 34), 14, muted, TextAnchor.UpperLeft);
            BindContentHover(card, info.Category, info.Title, info.Body, info.Accent, info.IconPath, info.Tags);
        }

        private void DrawEncyclopediaRuleCard(EncyclopediaEntry entry, Vector2 position)
        {
            FormalTooltipContent info = entry.Card;
            GameObject card = Panel("百科条目_" + entry.Name, encyclopediaGallery, new Vector2(0, 1), new Vector2(0, 1),
                position, new Vector2(480, 132), FormalUiTheme.SurfaceRaised);
            Line(card.transform, Vector2.zero, new Vector2(4, 132), info.Accent);
            Label("名称", info.Title, card.transform, new Vector2(20, -14), new Vector2(440, 32), 22, info.Accent, TextAnchor.MiddleLeft);
            Text body = Label("正文", info.Effect, card.transform, new Vector2(20, -54),
                new Vector2(440, 68), 17, text, TextAnchor.UpperLeft);
            FormalUiKit.ConfigureParagraph(body);
            body.verticalOverflow = VerticalWrapMode.Truncate;
            BindContentHover(card, info.Category, info.Title, info.Body, info.Accent, info.IconPath);
        }

        private static List<EncyclopediaEntry> BuildEncyclopediaEntries()
        {
            List<EncyclopediaEntry> entries = new List<EncyclopediaEntry>();
            RogueContentCatalog catalog = RogueContentCatalog.CreateAcademyV01();
            foreach (SpellDefinition spell in catalog.Spells)
            {
                CombatHoverDescriptionRow row = CombatHoverDescriptionTable.Spell(spell.DefinitionId);
                if (row == null) continue;
                string buildTag = SpellBuildTagCatalog.For(spell.DefinitionId);
                string mainTerm = SpellBuildTagCatalog.MainTerm(spell.DefinitionId);
                string sideTerm = SpellBuildTagCatalog.SideTerm(spell.DefinitionId);
                List<string> terms = new List<string>();
                if (!string.IsNullOrEmpty(mainTerm)) terms.Add(mainTerm);
                if (!string.IsNullOrEmpty(sideTerm)) terms.Add(sideTerm);
                terms.AddRange(CombatSpellTags.For(spell));
                FormalTooltipContent card = new FormalTooltipContent("术式", buildTag, spell.DisplayName,
                    row.Target, spell.Role == "passive" ? "被动" : "行动 " + spell.ActionPointCost,
                    spell.Role == "passive" ? "持续" : "魔力 " + spell.ManaCost, "冷却 " + spell.CooldownOwnTurns,
                    row.Effect, string.Empty, FormalUiTheme.Cyan, RogueSpellIconPath(spell.DefinitionId), terms);
                entries.Add(new EncyclopediaEntry(spell.DisplayName, "术式", spell.DisplayName + row.Effect + row.Target + spell.Role + buildTag + mainTerm + sideTerm, card));
            }
            foreach (EquipmentDefinition equipment in catalog.Equipment)
            {
                string effects = string.Join("；", equipment.FixedEffectIds.Select(PlayerEquipmentEffect)
                    .Concat(equipment.BaseActionIds.Select(PlayerEquipmentEffect)).Where(value => !string.IsNullOrWhiteSpace(value)));
                if (string.IsNullOrEmpty(effects)) effects = "基础装备；具体词条与专精以获得的实例为准。";
                FormalTooltipContent card = new FormalTooltipContent("装备", string.Empty, equipment.DisplayName,
                    EquipmentSlotLabel(equipment.Slot), "占格 " + equipment.Width + "×" + equipment.Height,
                    "重量 " + equipment.BaseWeight, "负荷 " + equipment.BaseAetherLoad, effects,
                    string.Empty, FormalUiTheme.Amber, FormalArtRegistry.EquipmentIconPath(equipment.DefinitionId));
                entries.Add(new EncyclopediaEntry(equipment.DisplayName, "装备", equipment.DisplayName + effects + equipment.Slot, card));
            }
            foreach (ArtifactDefinition artifact in ArtifactCatalog.All)
            {
                FormalTooltipContent card = new FormalTooltipContent("法宝", string.Empty, artifact.DisplayName,
                    artifact.TargetSummary, "行动 " + artifact.ActionPointCost, "次数 " + artifact.MaximumUses,
                    "射程 " + artifact.Range, artifact.EffectSummary + "。" + artifact.RiskSummary,
                    string.Empty, FormalUiTheme.Cyan, artifact.IconPath);
                entries.Add(new EncyclopediaEntry(artifact.DisplayName, "法宝", artifact.DisplayName + artifact.EffectSummary + artifact.TargetSummary, card));
            }
            AddRule(entries, "战斗规则", "行动点", "每个自身回合使用行动点提交移动、攻击、术式和互动；费用在确认命令时支付。");
            AddRule(entries, "战斗规则", "行动条", "单位行动时其他单位的行动条暂停；提前与延后会改变当前行动值，当前命令结算完毕后重新排序。");
            AddRule(entries, "战斗规则", "反击", "反击是单位的被动效果；在其声明的范围与行为满足时即时触发。具体次数和数值以该单位被动为准。");
            AddRule(entries, "战斗规则", "攻击预览", "提交前查看实际路径、作用格、遮挡、友军风险、强制位移终点和场地触发。取消预览不会支付资源。");
            AddRule(entries, "战斗规则", "宝箱", "战斗内宝箱需要相邻搜刮；宝箱被破坏后，尚未领取的内部物品销毁。");
            AddRule(entries, "状态与地形", "燃烧", "单位在自身回合结束时受到标注的火焰伤害；重复施加只刷新持续时间。进入浅水会清除燃烧。");
            AddRule(entries, "状态与地形", "火场", "单位进入火场或在自身回合开始时站在火场上受到伤害；同次行动至多触发一次。浅水可以覆盖火场。");
            AddRule(entries, "状态与地形", "破势", "立即清空目标护盾，并使目标至下一次自身回合结束前不能获得护盾。");
            AddRule(entries, "状态与地形", "破障", "伤害作用范围内的可破坏物承受同值耐久伤害；带破障的伤害对物块耐久翻倍。");
            AddRule(entries, "状态与地形", "裂痕与回流", "带裂痕的物块被摧毁时恢复2点个人魔力并获得4点普通护盾。物块摧毁后立即重算通路和攻击线。");
            foreach (EnemyArchetype enemy in EnemyArchetypes.All)
            {
                string attack = enemy.Weapon == null ? "无武器" : enemy.Weapon.DisplayName + "，伤害 " + enemy.Weapon.Damage + "，射程 " + enemy.Weapon.Range;
                string ability = enemy.PrimarySkill == null ? "无" : enemy.PrimarySkill.DisplayName;
                string body = "生命 " + enemy.MaxHealth + "，护盾 " + enemy.Shield + "，速度 " + enemy.Speed +
                    "。武器：" + attack + "。主要能力：" + ability + "。战斗中的当前意图以公开预览为准。";
                AddRule(entries, "敌人与机关", enemy.DisplayName, body);
            }
            return entries;
        }

        private static void AddRule(List<EncyclopediaEntry> entries, string category, string name, string body)
        {
            FormalTooltipContent card = new FormalTooltipContent(category, string.Empty, name, category,
                string.Empty, string.Empty, string.Empty, body, string.Empty, FormalUiTheme.Cyan);
            entries.Add(new EncyclopediaEntry(name, category, name + body, card));
        }
    }
}
