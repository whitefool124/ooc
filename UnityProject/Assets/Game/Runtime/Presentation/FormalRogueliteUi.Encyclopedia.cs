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
        private readonly Dictionary<string,float> rememberedGalleryPositions=new Dictionary<string,float>();
        private string displayedGalleryKey;
        private string encyclopediaBuildFilter = "全部";
        private string encyclopediaQuery = string.Empty;
        private Transform encyclopediaGallery;
        private Text encyclopediaResultCount;
        private ScrollRect encyclopediaScroll;

        private void DrawEncyclopedia()
        {
            Header("战斗百科", "术式　装备　法宝　战斗档案");
            ActionButton("教学册", string.Empty, content.transform, new Vector2(1580, -12), new Vector2(260, 56), cyan, true, () => Teaching?.OpenReview());
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
                clearLabel.fontSize = FormalUiTheme.BodyFontSize;
                clearLabel.rectTransform.anchoredPosition = new Vector2(16, -6);
                clearLabel.rectTransform.sizeDelta = new Vector2(92, 36);
            }
            encyclopediaResultCount = Label("结果数量", string.Empty, searchPanel.transform,
                new Vector2(1230, -10), new Vector2(330, 44), 18, ArchiveUiStyle.QuietInk, TextAnchor.MiddleRight);

            GameObject scrollObject = Panel("百科卡牌区", content.transform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(280, -156), new Vector2(1596, 860), FormalUiTheme.Surface);
            ArchiveUiStyle.PaperContent(scrollObject);
            displayedGalleryKey=null;
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
            // Search replaces the live list without rebuilding the page. Return
            // its layout before replacement and prepare its new lit faces and
            // clear-reading labels together before exposing the next frame.
            content.GetComponent<Hd2DContentGeometry>()?.RebuildContents();
            if(encyclopediaScroll!=null && !string.IsNullOrEmpty(displayedGalleryKey))
                rememberedGalleryPositions[displayedGalleryKey]=encyclopediaScroll.verticalNormalizedPosition;
            displayedGalleryKey=encyclopediaSection+"|"+encyclopediaBuildFilter+"|"+encyclopediaQuery;
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
            float rowY=24,rowHeight=0;
            for (int i = 0; i < entries.Count; i++)
            {
                if(i>0&&i%columns==0){rowY+=rowHeight+28;rowHeight=0;}
                Vector2 position = new Vector2(26 + (i % columns) * stepX, -rowY);
                float height=fullCards?DrawEncyclopediaContentCard(entries[i], position):DrawEncyclopediaRuleCard(entries[i], position);
                rowHeight=Mathf.Max(rowHeight,height);
            }
            RectTransform rect = encyclopediaGallery as RectTransform;
            if (rect != null) rect.sizeDelta = new Vector2(1552, Math.Max(860f,rowY+rowHeight+24));
            if (encyclopediaScroll != null)
            {
                Canvas.ForceUpdateCanvases();
                encyclopediaScroll.verticalNormalizedPosition=rememberedGalleryPositions.TryGetValue(displayedGalleryKey,out float remembered)?remembered:1;
            }
        }

        private float DrawEncyclopediaContentCard(EncyclopediaEntry entry, Vector2 position)
        {
            FormalTooltipContent info = entry.Card;
            bool isSpell = entry.Category == "术式";
            float cardHeight = isSpell ? 450f : 470f;
            GameObject card = Panel("百科卡_" + entry.Name, encyclopediaGallery, new Vector2(0, 1), new Vector2(0, 1),
                position, new Vector2(480, cardHeight), FormalUiTheme.SurfaceRaised);
            // The native paper rim is physical space, not a writing surface.
            // Keep every label inside one clipped inset, including the cloned
            // high-resolution reading text used by the HD2D page.
            var writing=Create("百科书写区",card.transform);
            var writingRect=writing.AddComponent<RectTransform>();
            writingRect.anchorMin=writingRect.anchorMax=writingRect.pivot=new Vector2(0,1);
            writingRect.anchoredPosition=new Vector2(64,-64);writingRect.sizeDelta=new Vector2(352,cardHeight-128);
            writing.AddComponent<RectMask2D>();
            Transform body=writing.transform;
            Label("类别", info.Category, body, Vector2.zero, new Vector2(160, 40), 16, info.Accent, TextAnchor.MiddleLeft);
            if (!isSpell)
                Label("状态", info.Status, body, new Vector2(170, 0), new Vector2(182, 40), 15, muted, TextAnchor.MiddleRight);
            Sprite sprite = Resources.Load<Sprite>(info.IconPath);
            Image icon = FormalUiKit.TopLeftIconSlot("内容图标", body, sprite, new Vector2(0, -48));
            icon.rectTransform.sizeDelta = new Vector2(64, 64);
            OccItemSurface.AttachByIcon(card,icon,()=>bootstrap.UiPreferences.AnimationIntensity);
            Label("名称", info.Title, body, new Vector2(80, -48), new Vector2(272, 44), 24, text, TextAnchor.MiddleLeft);
            if (!isSpell)
                Label("身份", info.Identity, body, new Vector2(80, -96), new Vector2(272, 40), 16, muted, TextAnchor.MiddleLeft);
            string[] metrics = { info.MetricA, info.MetricB, info.MetricC };
            float metricsY=isSpell?-128:-152;
            for (int i = 0; i < metrics.Length; i++)
            {
                Label("指标" + i, metrics[i], body, new Vector2(i * 117, metricsY),
                    new Vector2(114, 40), 15, text, TextAnchor.MiddleLeft);
            }
            var tags=Label("词条",string.Join(" · ",info.Tags.Take(6)),body,new Vector2(0,metricsY-46),new Vector2(352,44),13,info.Accent,TextAnchor.UpperLeft);
            FormalUiKit.ConfigureParagraph(tags);
            float tagsHeight=FitEncyclopediaParagraph(tags,40);
            float effectY=metricsY-46-tagsHeight-28;
            Line(body, new Vector2(0, effectY+14), new Vector2(352, 2), muted);
            Text effect = Label("效果", info.Effect, body, new Vector2(0,effectY),
                new Vector2(352, 84), 24, text, TextAnchor.UpperLeft);
            FormalUiKit.ConfigureParagraph(effect);
            float effectHeight=FitEncyclopediaParagraph(effect,64);
            float footerY=effectY-effectHeight-28;
            Line(body, new Vector2(0,footerY+14), new Vector2(352, 2), muted);
            var summary=Label("简介", info.Summary, body, new Vector2(0,footerY), new Vector2(352,40), 24, muted, TextAnchor.UpperLeft);
            FormalUiKit.ConfigureParagraph(summary);
            float summaryHeight=FitEncyclopediaParagraph(summary,40);
            cardHeight=64-footerY+summaryHeight+64;
            ((RectTransform)card.transform).sizeDelta=new Vector2(480,cardHeight);
            writingRect.sizeDelta=new Vector2(352,cardHeight-128);
            BindContentHover(card, info.Category, info.Title, info.Body, info.Accent, info.IconPath, info.Tags);
            return cardHeight;
        }

        private static float FitEncyclopediaParagraph(Text label,float minimum)
        {
            // Reserve actual line height plus projection/leading safety; resize
            // the carrier and following rows instead of shrinking long rules.
            label.resizeTextForBestFit=false;
            float width=label.rectTransform.sizeDelta.x;
            // The reading plane is projected at .95 of the authored width.
            // Measure that narrower line before deciding the paper's height.
            label.rectTransform.sizeDelta=new Vector2(width*.94f,label.rectTransform.sizeDelta.y);
            float height=Mathf.Max(minimum,Mathf.Ceil(label.preferredHeight/.94f/8)*8+12);
            label.rectTransform.sizeDelta=new Vector2(width,height);
            label.verticalOverflow=VerticalWrapMode.Truncate;
            return height;
        }

        private float DrawEncyclopediaRuleCard(EncyclopediaEntry entry, Vector2 position)
        {
            FormalTooltipContent info = entry.Card;
            GameObject card = Panel("百科条目_" + entry.Name, encyclopediaGallery, new Vector2(0, 1), new Vector2(0, 1),
                position, new Vector2(480, 132), FormalUiTheme.SurfaceRaised);
            ArchiveUiStyle.PaperPanel(card, ArchiveUiStyle.LightPaper, false);
            Label("名称", info.Title, card.transform, new Vector2(32, -24), new Vector2(416,44), 24, info.Accent, TextAnchor.MiddleLeft);
            Text body = Label("正文", info.Effect, card.transform, new Vector2(32, -80),
                new Vector2(416,68), 24, text, TextAnchor.UpperLeft);
            FormalUiKit.ConfigureParagraph(body);
            float height=80+FitEncyclopediaParagraph(body,64)+24;
            ((RectTransform)card.transform).sizeDelta=new Vector2(480,height);
            Line(card.transform, Vector2.zero, new Vector2(4,height), info.Accent);
            BindContentHover(card, info.Category, info.Title, info.Body, info.Accent, info.IconPath);
            return height;
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
            AddRule(entries, "状态与地形", "火场", "单位进入火场或在自身回合开始时站在火场上，受到8点基础火焰伤害；同一单位每次行动至多触发一次。火势提高来源单位生成的火场伤害，助燃提高目标受到的火场伤害，均在结算时生效。");
            AddRule(entries, "状态与地形", "燃烧", "目标在自身回合结束时受到卡面标注的火焰伤害；重复施加只刷新持续时间，不叠加强度。");
            AddRule(entries, "状态与地形", "破势", "施加时清空目标当前护盾，并使目标在下一次自身回合结束前无法获得护盾。");
            AddRule(entries, "状态与地形", "裂痕", "术式的伤害范围命中可破坏物时，可给它施加裂痕；带有裂痕的物件在持续期间被摧毁时触发回流。");
            AddRule(entries, "状态与地形", "回流", "带有裂痕的物件被摧毁后，恢复2点个人魔力并获得4点普通护盾。");
            AddRule(entries, "状态与地形", "迟缓", "生效期间，单位的可移动步数为3格。");
            AddRule(entries, "状态与地形", "束缚", "生效期间，单位不能主动移动。");
            AddRule(entries, "状态与地形", "破障", "术式伤害会影响作用范围内的物块；带有“破障”的伤害会造成双倍耐久伤害。");
            foreach (EnemyArchetype enemy in EnemyArchetypes.All)
            {
                string attack = enemy.Attack == null ? "无固有攻击" : enemy.Attack.DisplayName + "，伤害 " + enemy.Attack.Damage + "，射程 " + enemy.Attack.Range;
                string ability = enemy.PrimarySkill == null ? "无" : enemy.PrimarySkill.DisplayName;
                string body = "生命 " + enemy.MaxHealth + "，护盾 " + enemy.Shield + "，速度 " + enemy.Speed +
                    "。固有攻击：" + attack + "。主要能力：" + ability + "。战斗中的当前意图以公开预览为准。";
                AddRule(entries, "敌人与机关", enemy.DisplayName, body);
            }
            return entries;
        }

        internal static bool TryGetSharedContentCard(string category, string name, out FormalTooltipContent card)
        {
            EncyclopediaEntry entry = BuildEncyclopediaEntries().FirstOrDefault(value =>
                value.Category == category && value.Name == name);
            card = entry == null ? default : entry.Card;
            return entry != null;
        }

        private static void AddRule(List<EncyclopediaEntry> entries, string category, string name, string body)
        {
            FormalTooltipContent card = new FormalTooltipContent(category, string.Empty, name, category,
                string.Empty, string.Empty, string.Empty, body, string.Empty, FormalUiTheme.Cyan);
            entries.Add(new EncyclopediaEntry(name, category, name + body, card));
        }
    }
}
