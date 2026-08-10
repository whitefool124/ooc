using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // Runtime-built production HUD. It intentionally leaves the 75% tactical board unobstructed.
    public sealed class FormalCombatHud : MonoBehaviour
    {
        private static Color ink => FormalUiTheme.Ink;
        private static Color panel => FormalUiTheme.Panel;
        private static Color line => new Color(FormalUiTheme.Cyan.r, FormalUiTheme.Cyan.g, FormalUiTheme.Cyan.b, .82f);
        private static Color muted => FormalUiTheme.Muted;
        private static Color text => FormalUiTheme.Text;
        private readonly Dictionary<string, Button> actionButtons = new Dictionary<string, Button>();
        private readonly Dictionary<string, Sprite> actionIcons = new Dictionary<string, Sprite>();
        private CombatPrototypeBootstrap bootstrap;
        private Canvas canvas;
        private GameObject root;
        private FormalHoverTooltip tooltip;
        private Text activeLabel;
        private Text phaseLabel;
        private Text weaponLabel;
        private Text statusLabel;
        private Text eventLabel;
        private GameObject timelineModule;
        private GameObject logModule;
        private readonly Text[] timelineNames = new Text[5];
        private readonly Text[] timelineDetails = new Text[5];
        private readonly Image[] timelineNodes = new Image[5];
        private readonly Image[] timelineRows = new Image[5];
        private Image healthFill;
        private Image shieldFill;
        private Image manaFill;
        private Text healthValue;
        private Text shieldValue;
        private Text manaValue;
        private Button endTurnButton;
        private Button restartButton;
        private Button leaveButton;
        private Button outcomeRestartButton;
        private Button outcomeBackButton;
        private GameObject outcomeOverlay;
        private Text outcomeTitle;
        private Text outcomeDetail;
        private Text[] quickbarLabels = new Text[8];
        private Image[] quickbarIcons = new Image[8];
        private Image weaponIcon;
        private float displayedHealth = -1f;
        private float displayedShield = -1f;
        private float displayedMana = -1f;
        private bool wasVisible;
        private bool outcomeWasVisible;
        private bool hasPresentedModel;
        private CombatHudPresentationModel presentedModel;
        private bool refreshDirty = true;
        public int RefreshCount { get; private set; }

        public void Initialize(CombatPrototypeBootstrap source)
        {
            bootstrap = source;
            bootstrap.UiPresentationVersions.Changed += OnPresentationChanged;
            LoadActionIcons();
            EnsureUi();
        }

        private void OnPresentationChanged(UiPresentationChange change)
        {
            if (change.Area == UiPresentationArea.Combat || change.Area == UiPresentationArea.Flow) refreshDirty = true;
        }

        private void Update()
        {
            if (root == null || bootstrap == null) return;
            bool visible = bootstrap.IsDeveloperCombatActive || bootstrap.IsCombatOutcomeVisible;
            if (root.activeSelf != visible) { root.SetActive(visible); refreshDirty = true; }
            if (!visible || bootstrap.CurrentState == null) { wasVisible = false; hasPresentedModel = false; return; }
            if (!wasVisible)
            {
                wasVisible = true;
                Button defaultButton = outcomeRestartButton;
                if (!bootstrap.IsCombatOutcomeVisible) actionButtons.TryGetValue("移动", out defaultButton);
                if (defaultButton != null) RuntimeUiEventSystem.Select(defaultButton.gameObject);
            }
            if (bootstrap.IsDeveloperCombatActive && !bootstrap.IsInteractionModalOpen && RuntimeUiEventSystem.CancelPressedThisFrame()) bootstrap.RequestLeaveCombat();
            if (!refreshDirty) return;
            refreshDirty = false;
            CombatHudPresentationModel nextModel = CombatHudPresentationModel.From(bootstrap.CurrentState, bootstrap.SelectedAction, bootstrap.SelectedTargetId, bootstrap.IsCombatOutcomeVisible);
            if (hasPresentedModel && presentedModel.Equals(nextModel)) return;
            presentedModel = nextModel;
            hasPresentedModel = true;
            Refresh();
        }

        private void EnsureUi()
        {
            if (root != null) return;
            canvas = FormalUiKit.CanvasRoot("正式战斗HUD", UiLayoutContract.CombatSortingOrder);
            root = canvas.gameObject;
            tooltip = root.AddComponent<FormalHoverTooltip>();
            tooltip.Initialize(canvas);

            GameObject top = FormalUiKit.LayoutPanel("战斗抬头", root.transform, "combat.header", ink);
            Label("OCC // 战术行动", top.transform, new Vector2(20, -10), new Vector2(420, 34), 22, text, TextAnchor.MiddleLeft);
            phaseLabel = Label("准备阶段", top.transform, new Vector2(440, -10), new Vector2(620, 34), 17, line, TextAnchor.MiddleLeft);
            Label("冷静部署  /  随时规划", top.transform, new Vector2(1330, -10), new Vector2(500, 34), 16, muted, TextAnchor.MiddleRight);
            Line(top.transform, new Vector2(18, -53), new Vector2(1836, 2), line);

            GameObject side = FormalUiKit.LayoutPanel("战斗信息", root.transform, "combat.rightConsole", panel);
            GameObject selectedModule = FormalUiKit.LayoutPanel("本轮行动", side.transform, "combat.selected", panel);
            Label("本轮行动", selectedModule.transform, new Vector2(16, -10), new Vector2(360, 26), 18, text, TextAnchor.MiddleLeft);
            activeLabel = Label("等待行动", selectedModule.transform, new Vector2(16, -42), new Vector2(380, 46), 17, text, TextAnchor.UpperLeft);

            GameObject heroModule = FormalUiKit.LayoutPanel("英雄概况", side.transform, "combat.hero", panel);
            Label("英雄", heroModule.transform, new Vector2(16, -10), new Vector2(360, 26), 18, text, TextAnchor.MiddleLeft);
            weaponLabel = Label("主手装备", heroModule.transform, new Vector2(16, -42), new Vector2(330, 38), 15, muted, TextAnchor.UpperLeft);
            weaponIcon = FormalUiKit.IconSlot("主手装备图标", heroModule.transform, null, Vector2.zero);
            weaponIcon.rectTransform.anchorMin = weaponIcon.rectTransform.anchorMax = new Vector2(0, 1);
            weaponIcon.rectTransform.pivot = new Vector2(0, 1); weaponIcon.rectTransform.anchoredPosition = new Vector2(364, -44);
            statusLabel = Label("状态", heroModule.transform, new Vector2(16, -88), new Vector2(380, 24), 14, muted, TextAnchor.UpperLeft);
            healthFill = ResourceBar(heroModule.transform, "生命", new Vector2(16, -116), new Color(.32f, .82f, .56f), out healthValue);
            shieldFill = ResourceBar(heroModule.transform, "护盾", new Vector2(16, -158), new Color(.44f, .72f, .63f), out shieldValue);
            manaFill = ResourceBar(heroModule.transform, "以太", new Vector2(16, -200), line, out manaValue);
            BindTooltip(heroModule, BuildHeroTooltip);

            timelineModule = FormalUiKit.LayoutPanel("行动序列模块", side.transform, "combat.timeline", panel);
            Label("接下来", timelineModule.transform, new Vector2(16, -8), new Vector2(240, 24), 17, text, TextAnchor.MiddleLeft);
            Label("越靠前越先行动", timelineModule.transform, new Vector2(228, -8), new Vector2(168, 24), 12, muted, TextAnchor.MiddleRight);
            for (int i = 0; i < timelineNames.Length; i++) CreateTimelineSlot(i);

            logModule = FormalUiKit.LayoutPanel("现场记录模块", side.transform, "combat.log", panel);
            Label("现场记录", logModule.transform, new Vector2(16, -8), new Vector2(380, 24), 17, text, TextAnchor.MiddleLeft);
            eventLabel = Label("记录", logModule.transform, new Vector2(16, -38), new Vector2(380, 122), 14, muted, TextAnchor.UpperLeft);

            GameObject bottom = FormalUiKit.LayoutPanel("战术指令", root.transform, "combat.commands", ink);
            GameObject weaponGroup = Panel("武器组", bottom.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -14), new Vector2(282, 136), panel);
            GameObject spellGroup = Panel("术式组", bottom.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(302, -14), new Vector2(282, 136), panel);
            GameObject interactionGroup = Panel("交互组", bottom.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(592, -14), new Vector2(282, 136), panel);
            GameObject itemGroup = Panel("物品组", bottom.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(882, -14), new Vector2(294, 136), panel);
            Label("移动 / 武器", weaponGroup.transform, new Vector2(10, -6), new Vector2(260, 24), 14, line, TextAnchor.MiddleLeft);
            Label("个人术式", spellGroup.transform, new Vector2(10, -6), new Vector2(260, 24), 14, new Color(.70f, .48f, .86f), TextAnchor.MiddleLeft);
            Label("交互 / 搜刮", interactionGroup.transform, new Vector2(10, -6), new Vector2(260, 24), 14, FormalUiTheme.Amber, TextAnchor.MiddleLeft);
            Label("物品快捷栏", itemGroup.transform, new Vector2(10, -6), new Vector2(270, 24), 14, FormalUiTheme.Safe, TextAnchor.MiddleLeft);
            string[] actions = { "移动", "攻击", "技能1", "技能2", "搜刮", "互动" };
            for (int i = 0; i < actions.Length; i++)
            {
                string action = actions[i];
                Transform group = i < 2 ? weaponGroup.transform : i < 4 ? spellGroup.transform : interactionGroup.transform;
                int groupIndex = i % 2;
                Button button = Button(group, action, new Vector2(10 + groupIndex * 134, -38), new Vector2(124, 84), InitialActionLabel(action), new Color(.055f, .08f, .09f, 1f), 14);
                AddActionIcon(button.transform, action);
                button.onClick.AddListener(() => bootstrap.SelectHudAction(action));
                actionButtons.Add(action, button);
                BindTooltip(button.gameObject, () => BuildActionTooltip(action));
            }
            endTurnButton = Button(bottom.transform, "结束行动", new Vector2(1188, -14), new Vector2(204, 78), "结束行动\n剩余 AP 作废", new Color(.10f, .16f, .17f, 1f));
            endTurnButton.onClick.AddListener(() => bootstrap.EndHeroTurn());
            BindTooltip(endTurnButton.gameObject, () => new FormalTooltipContent("结束行动", "结束英雄的本轮行动；未使用的行动点会作废，随后敌方依次行动。", line));
            restartButton = Button(bottom.transform, "战术重开", new Vector2(1188, -100), new Vector2(98, 40), "重开", new Color(.12f, .105f, .055f, 1f));
            restartButton.onClick.AddListener(bootstrap.RequestTacticalRestart);
            BindTooltip(restartButton.gameObject, () => new FormalTooltipContent("战术重开", "重新开始本场战斗，当前战斗进度将被撤销。", FormalUiTheme.Amber));
            leaveButton = Button(bottom.transform, "离开战斗", new Vector2(1294, -100), new Vector2(98, 40), "离开", new Color(.16f, .075f, .06f, 1f));
            leaveButton.onClick.AddListener(bootstrap.RequestLeaveCombat);
            BindTooltip(leaveButton.gameObject, () => new FormalTooltipContent("离开战斗", "离开当前战斗并返回行动入口。", FormalUiTheme.Danger));
            for (int i = 0; i < quickbarLabels.Length; i++)
            {
                int slot = i;
                Button quick = Button(itemGroup.transform, "快捷栏" + i, new Vector2(10 + (i % 4) * 68, -38 - (i / 4) * 46), new Vector2(64, 40), "", new Color(.07f, .075f, .07f, 1f), 10);
                quickbarLabels[i] = quick.GetComponentInChildren<Text>();
                quickbarIcons[i] = FormalUiKit.IconSlot("快捷栏正式图标", quick.transform, null, new Vector2(2, 0));
                if (quickbarLabels[i] != null) quickbarLabels[i].rectTransform.offsetMin = new Vector2(27, 0);
                quick.onClick.AddListener(() => bootstrap.ActivateInventoryQuickbar(slot));
                BindTooltip(quick.gameObject, () => BuildQuickbarTooltip(slot));
            }
            CreateOutcomeOverlay();
        }

        private void LoadActionIcons()
        {
            string[] names = { "move", "attack", "skill", "skill_two", "loot", "interact" };
            foreach (string name in names)
            {
                Sprite sprite = Resources.Load<Sprite>(FormalArtRegistry.CommandPath(name));
                if (sprite == null) throw new KeyNotFoundException("Missing formal command icon: " + name);
                actionIcons[name] = sprite;
            }
        }

        private static string InitialActionLabel(string action)
        {
            if (action == "移动") return "移动\n1AP · 3格";
            if (action == "攻击") return "攻击\n1AP · 目标";
            if (action == "搜刮") return "搜刮\n1AP · 相邻";
            if (action == "互动") return "互动\n1AP · 相邻";
            return action;
        }

        private void AddActionIcon(Transform parent, string action)
        {
            string key = action == "移动" ? "move" : action == "攻击" ? "attack" : action == "技能1" ? "skill" : action == "技能2" ? "skill_two" : action == "搜刮" ? "loot" : "interact";
            if (!actionIcons.TryGetValue(key, out Sprite sprite)) return;
            FormalUiKit.IconSlot("正式图标", parent, sprite, new Vector2(9, 0));
            Text label = parent.GetComponentInChildren<Text>();
                if (label != null) { label.rectTransform.offsetMin = new Vector2(FormalUiTheme.IconTextInset, 0); label.alignment = TextAnchor.MiddleCenter; FormalUiKit.PreventAutomaticWrapping(label); }
        }

        private void CreateTimelineSlot(int index)
        {
            float y = -38f - index * 36f;
            GameObject row = Panel("行动位" + (index + 1), timelineModule.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, y), new Vector2(388, 32), new Color(.025f, .035f, .04f, .76f));
            timelineRows[index] = row.GetComponent<Image>();
            if (index < timelineNames.Length - 1)
                Line(timelineModule.transform, new Vector2(31, y - 27), new Vector2(2, 12), new Color(line.r, line.g, line.b, .28f));
            GameObject node = Panel("行动节点" + (index + 1), row.transform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(10, 0), new Vector2(14, 14), muted);
            timelineNodes[index] = node.GetComponent<Image>();
            timelineNames[index] = Label("行动者" + (index + 1), row.transform, new Vector2(36, -3), new Vector2(220, 26), 14, muted, TextAnchor.MiddleLeft);
            timelineDetails[index] = Label("行动摘要" + (index + 1), row.transform, new Vector2(250, -3), new Vector2(124, 26), 12, muted, TextAnchor.MiddleRight);
        }

        private void Refresh()
        {
            RefreshCount++;
            CombatState state = bootstrap.CurrentState;
            UnitState hero = state.GetUnit("hero");
            UnitState active = state.GetUnit(state.ActiveUnitId);
            phaseLabel.text = bootstrap.CurrentPhaseText;
            activeLabel.text = active == null ? "等待下一位行动者" : active.DisplayName + "正在行动\n剩余 " + active.ActionPoints + " 行动点";
            weaponLabel.text = hero.DisplayName + "\n主手 · " + hero.MainHand.DisplayName;
            weaponIcon.sprite = Resources.Load<Sprite>(FormalArtRegistry.ItemPath(hero.MainHand.Id));
            if (weaponIcon.sprite == null) throw new KeyNotFoundException("Missing formal item icon: " + hero.MainHand.Id);
            FireSpellDefinition fireOne = bootstrap.FireSpellInSlot(0), fireTwo = bootstrap.FireSpellInSlot(1);
            ArtifactDefinition artifactOne = bootstrap.CurrentArmedArtifact ?? bootstrap.CurrentTrainingRangeArtifact;
            if (artifactOne != null) RefreshArtifactButton("技能1", artifactOne);
            else if (fireOne != null) RefreshFireSpellButton("技能1", fireOne, hero); else RefreshSkillButton("技能1", hero.SkillOne, hero);
            if (fireTwo != null) RefreshFireSpellButton("技能2", fireTwo, hero); else RefreshSkillButton("技能2", hero.SkillTwo, hero);
            statusLabel.text = "状态 · " + StatusText(hero);
            healthValue.text = hero.Health + " / " + hero.MaxHealth;
            shieldValue.text = hero.Shield + " / " + hero.MaxShield;
            manaValue.text = hero.Mana + " / " + hero.MaxMana;
            SetBar(healthFill, hero.Health / (float)Math.Max(1, hero.MaxHealth), ref displayedHealth);
            SetBar(shieldFill, hero.Shield / (float)Math.Max(1, hero.MaxShield), ref displayedShield);
            SetBar(manaFill, hero.Mana / (float)Math.Max(1, hero.MaxMana), ref displayedMana);
            IReadOnlyList<CombatTurnTrackEntry> track = CombatTurnTrackPresentation.Build(state, timelineNames.Length);
            for (int i = 0; i < timelineNames.Length; i++)
            {
                bool visible = i < track.Count;
                timelineRows[i].gameObject.SetActive(visible);
                if (!visible) continue;
                CombatTurnTrackEntry entry = track[i];
                Color faction = entry.IsHero ? line : FormalUiTheme.Danger;
                timelineNodes[i].color = entry.IsActive ? faction : new Color(faction.r, faction.g, faction.b, .55f);
                timelineRows[i].color = entry.IsActive ? new Color(faction.r, faction.g, faction.b, .16f) : new Color(.025f, .035f, .04f, .76f);
                timelineNames[i].text = entry.Order + "  " + entry.DisplayName;
                timelineNames[i].color = entry.IsActive ? text : muted;
                timelineDetails[i].text = entry.IsActive ? "正在行动" : entry.VitalityText;
                timelineDetails[i].color = entry.IsActive ? faction : muted;
            }
            eventLabel.text = state.EventLog.Count == 0 ? "等待战术指令。" : string.Join("\n", state.EventLog.Take(5).Select((entry, index) => (index == 0 ? "▶ " : "   ") + entry));
            for (int i = 0; i < quickbarLabels.Length; i++)
            {
                ItemInstance item = state.ItemInventory.Get(state.ItemQuickbar[i]);
                ItemDefinition definition = item == null ? null : ItemCatalog.Get(item.DefinitionId);
                string displayName = definition == null ? "空" :
                    (definition.DisplayName.Length <= 4 ? definition.DisplayName : definition.DisplayName.Substring(0, 4));
                quickbarLabels[i].text = definition == null ? (i + 1) + "\n空" : displayName + "\n" + (i + 1) + "·×" + item.RemainingUses;
                quickbarIcons[i].gameObject.SetActive(definition != null);
                if (definition == null) continue;
                quickbarIcons[i].sprite = Resources.Load<Sprite>(definition.IconPath);
                if (quickbarIcons[i].sprite == null) throw new KeyNotFoundException("Missing formal quickbar icon: " + definition.Id);
            }
            bool outcome = bootstrap.IsCombatOutcomeVisible;
            outcomeOverlay.SetActive(outcome);
            if (outcome && !outcomeWasVisible && outcomeRestartButton != null) RuntimeUiEventSystem.Select(outcomeRestartButton.gameObject);
            outcomeWasVisible = outcome;
            if (outcome)
            {
                CombatOutcomePresentation summary = bootstrap.CurrentOutcomePresentation;
                outcomeTitle.text = summary?.Title ?? (bootstrap.CurrentState.IsVictory ? "任务完成" : "行动中止");
                outcomeDetail.text = summary?.CompactDetailText ?? "战术记录已封存。请选择下一步。";
                outcomeBackButton.GetComponentInChildren<Text>().text = bootstrap.CurrentMapRun != null ? "返回地图" : "返回入口";
            }
            foreach (KeyValuePair<string, Button> pair in actionButtons)
            {
                Image image = pair.Value.GetComponent<Image>();
                image.color = pair.Key == bootstrap.SelectedAction ? new Color(.10f, .31f, .35f, 1f) : new Color(.055f, .08f, .09f, 1f);
                pair.Value.GetComponent<UiButtonFeedback>()?.SetSelectedState(pair.Key == bootstrap.SelectedAction);
            }
            RefreshAvailability(state, hero);
        }

        private void RefreshSkillButton(string key, SkillDefinition skill, UnitState hero)
        {
            if (skill == null || !actionButtons.TryGetValue(key, out Button button)) return;
            Text label = button.GetComponentInChildren<Text>();
            if (label == null) return;
            CombatFeedbackSemantic semantic = CombatFeedbackCatalog.For(skill.PresentationKind);
            Image icon = button.GetComponentsInChildren<Image>().FirstOrDefault(image => image.gameObject.name == "正式图标");
            if (icon != null)
            {
                Sprite sprite = Resources.Load<Sprite>(FormalArtRegistry.RuntimeSkillPath(skill.Id));
                if (sprite == null) throw new KeyNotFoundException("Missing formal runtime skill icon: " + skill.Id);
                icon.sprite = sprite;
                if (ColorUtility.TryParseHtmlString(semantic.ColorHex, out Color color)) icon.color = color;
            }
            int cooldown = hero.Cooldown(skill);
            label.text = skill.DisplayName + "\n" + skill.ManaCost + "以太 · " + (cooldown > 0 ? "CD" + cooldown : skill.Range + "格");
        }

        private void RefreshFireSpellButton(string key, FireSpellDefinition spell, UnitState hero)
        {
            if (spell == null || !actionButtons.TryGetValue(key, out Button button)) return;
            ArtifactDefinition artifact = bootstrap.CurrentTrainingRangeArtifact;
            ItemInstance armedItem = bootstrap.CurrentArmedInventoryItem;
            ItemDefinition armedDefinition = armedItem == null ? null : ItemCatalog.Get(armedItem.DefinitionId);
            Text label = button.GetComponentInChildren<Text>(); if (label == null) return;
            Image icon = button.GetComponentsInChildren<Image>().FirstOrDefault(image => image.gameObject.name == "正式图标");
            if (icon != null)
            {
                string iconPath = armedDefinition?.IconPath ?? artifact?.IconPath ?? spell.IconPath;
                Sprite sprite = Resources.Load<Sprite>(iconPath);
                if (sprite == null) throw new KeyNotFoundException("Missing formal fire spell icon: " + spell.Id);
                icon.sprite = sprite; icon.color = new Color(.95f, .45f, .20f, 1f);
            }
            int cooldown = bootstrap.CurrentFireBattle == null ? 0 : bootstrap.CurrentFireBattle.Cooldown(hero.Id, spell.Id);
            string availability = armedItem != null ? armedItem.RemainingUses + "/" + armedItem.MaximumUses + "次" : artifact == null ? (cooldown > 0 ? "CD" + cooldown : spell.Range + "格") : bootstrap.TrainingRangeArtifactUsesRemaining + "/" + artifact.MaximumUses + "次";
            label.text = spell.DisplayName + "\n" + spell.ActionPointCost + "AP " + spell.ManaCost + "魔力 · " + availability;
        }

        private void RefreshArtifactButton(string key, ArtifactDefinition artifact)
        {
            if (!actionButtons.TryGetValue(key, out Button button)) return;
            Text label = button.GetComponentInChildren<Text>(); if (label == null) return;
            Image icon = button.GetComponentsInChildren<Image>().FirstOrDefault(image => image.gameObject.name == "正式图标");
            if (icon != null)
            {
                Sprite sprite = Resources.Load<Sprite>(artifact.IconPath);
                if (sprite == null) throw new KeyNotFoundException("Missing formal artifact icon: " + artifact.Id);
                icon.sprite = sprite; icon.color = Color.white;
            }
            ItemInstance armed = bootstrap.CurrentArmedInventoryItem;
            int remaining = armed?.RemainingUses ?? bootstrap.TrainingRangeArtifactUsesRemaining;
            label.text = artifact.DisplayName + "\n" + artifact.ActionPointCost + "AP · " + remaining + "/" + artifact.MaximumUses + "次";
        }

        private void CreateOutcomeOverlay()
        {
            outcomeOverlay = FormalUiKit.LayoutPanel("战斗结果", root.transform, "combat.outcome", new Color(.012f, .018f, .024f, .97f));
            outcomeTitle = Label("结果标题", outcomeOverlay.transform, new Vector2(40, -34), new Vector2(640, 58), 36, text, TextAnchor.MiddleCenter);
            outcomeDetail = Label("结果说明", outcomeOverlay.transform, new Vector2(40, -102), new Vector2(640, 100), 16, muted, TextAnchor.UpperCenter);
            outcomeRestartButton = Button(outcomeOverlay.transform, "结果重开", new Vector2(60, -180), new Vector2(280, 64), "战术重开", new Color(.08f, .20f, .22f, 1f));
            outcomeRestartButton.onClick.AddListener(bootstrap.RequestTacticalRestart);
            outcomeBackButton = Button(outcomeOverlay.transform, "结果返回", new Vector2(380, -180), new Vector2(280, 64), "返回入口", new Color(.12f, .10f, .06f, 1f));
            outcomeBackButton.onClick.AddListener(bootstrap.ReturnToDeveloperMenu);
            BindTooltip(outcomeOverlay, BuildOutcomeTooltip);
            BindTooltip(outcomeRestartButton.gameObject, () => new FormalTooltipContent("战术重开", "重新挑战本场战斗，当前进度将被撤销。", line));
            BindTooltip(outcomeBackButton.gameObject, () => new FormalTooltipContent("返回入口", "返回地图，并从进入本场战斗前继续。", FormalUiTheme.Amber));
            outcomeOverlay.SetActive(false);
        }

        private FormalTooltipContent BuildActionTooltip(string action)
        {
            CombatActionPreview preview = bootstrap?.ActionPreview(action);
            string title = action;
            if (actionButtons.TryGetValue(action, out Button button))
            {
                string label = button.GetComponentInChildren<Text>()?.text;
                if (!string.IsNullOrWhiteSpace(label)) title = label.Split('\n')[0];
            }
            return new FormalTooltipContent(title + " · 使用说明", CombatInformationPresenter.BuildActionDetails(preview), line);
        }

        private FormalTooltipContent BuildHeroTooltip()
        {
            UnitState hero = bootstrap?.CurrentState?.GetUnit("hero");
            return new FormalTooltipContent("英雄详情", CombatInformationPresenter.BuildHeroDetails(hero), FormalUiTheme.Safe);
        }

        private FormalTooltipContent BuildQuickbarTooltip(int slot)
        {
            CombatState state = bootstrap?.CurrentState;
            ItemInstance item = state == null || slot < 0 || slot >= state.ItemQuickbar.Length ? null : state.ItemInventory.Get(state.ItemQuickbar[slot]);
            ItemDefinition definition = item == null ? null : ItemCatalog.Get(item.DefinitionId);
            string title = definition == null ? "快捷栏 " + (slot + 1) : definition.DisplayName;
            return new FormalTooltipContent(title, CombatInformationPresenter.BuildItemDetails(definition, item, slot), FormalUiTheme.Safe);
        }

        private FormalTooltipContent BuildOutcomeTooltip()
        {
            CombatOutcomePresentation summary = bootstrap?.CurrentOutcomePresentation;
            return new FormalTooltipContent("最近关键事件", summary?.RecentEventsText ?? "最近事件：无", FormalUiTheme.Amber);
        }

        private void BindTooltip(GameObject target, Func<FormalTooltipContent> provider)
        {
            if (target == null) return;
            FormalHoverTooltipTrigger trigger = target.GetComponent<FormalHoverTooltipTrigger>() ?? target.AddComponent<FormalHoverTooltipTrigger>();
            trigger.Configure(tooltip, provider);
        }

        private Image ResourceBar(Transform parent, string title, Vector2 position, Color color, out Text valueLabel)
        {
            Label(title, parent, position, new Vector2(200, 22), 15, muted, TextAnchor.MiddleLeft);
            valueLabel = Label(title + "数值", parent, position, new Vector2(374, 22), 13, muted, TextAnchor.MiddleRight);
            GameObject track = Panel(title + "轨道", parent, new Vector2(0, 1), new Vector2(0, 1), position + new Vector2(0, -28), new Vector2(390, 15), new Color(.015f, .02f, .026f, 1f));
            GameObject fill = Panel(title + "填充", track.transform, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero, color);
            FormalUiKit.ApplySkin(track.GetComponent<Image>(), "bar_track", Color.white);
            string fillSkin = title == "结构" ? "bar_segment_health" : title == "护盾" ? "bar_segment_shield" : title == "以太" ? "bar_segment_mana" : "bar_fill";
            FormalUiKit.ApplySkin(fill.GetComponent<Image>(), fillSkin, color);
            RectTransform rect = fill.GetComponent<RectTransform>();
            rect.anchorMax = new Vector2(1, 1);
            return fill.GetComponent<Image>();
        }

        private void SetBar(Image fill, float value, ref float displayed)
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(displayed, value)) return;
            RectTransform rect = fill.rectTransform;
            rect.DOKill();
            UiMotionProfile motion = UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity);
            if (motion.IsImmediate) rect.anchorMax = new Vector2(value, 1f);
            else DOTween.To(() => rect.anchorMax.x, next => rect.anchorMax = new Vector2(next, 1f), value, motion.QuickDuration).SetEase(FormalUiMotionTokens.FeedbackEase).SetUpdate(true);
            displayed = value;
        }

        private void RefreshAvailability(CombatState state, UnitState hero)
        {
            bool heroTurn = state.ActiveUnitId == "hero" && hero.IsAlive;
            foreach (KeyValuePair<string, Button> pair in actionButtons)
            {
                bool available = heroTurn;
                string reason = heroTurn ? string.Empty : "等待敌方行动";
                SkillDefinition skill = pair.Key == "技能1" ? hero.SkillOne : pair.Key == "技能2" ? hero.SkillTwo : null;
                int fireSlot = pair.Key == "技能1" ? 0 : pair.Key == "技能2" ? 1 : -1;
                FireSpellDefinition fire = fireSlot < 0 ? null : bootstrap.FireSpellInSlot(fireSlot);
                ArtifactDefinition artifact = fireSlot == 0 ? (bootstrap.CurrentArmedArtifact ?? bootstrap.CurrentTrainingRangeArtifact) : null;
                if (available && artifact != null && hero.ActionPoints < artifact.ActionPointCost) { available = false; reason = "行动点不足：需要 " + artifact.ActionPointCost; }
                if (available && fire != null && bootstrap.CurrentFireBattle != null && bootstrap.CurrentFireBattle.Cooldown(hero.Id, fire.Id) > 0) { available = false; reason = "术式冷却中 · " + bootstrap.CurrentFireBattle.Cooldown(hero.Id, fire.Id) + " 回合"; }
                if (available && fire != null && hero.Mana < fire.ManaCost) { available = false; reason = "魔力不足：需要 " + fire.ManaCost; }
                if (available && fire != null && hero.ActionPoints < fire.ActionPointCost) { available = false; reason = "行动点不足：需要 " + fire.ActionPointCost; }
                if (fire != null || artifact != null) skill = null;
                if (available && skill != null && hero.Cooldown(skill) > 0) { available = false; reason = "技能冷却中 · " + hero.Cooldown(skill) + " 回合"; }
                if (available && skill != null && hero.Mana < skill.ManaCost) { available = false; reason = "以太不足：需要 " + skill.ManaCost; }
                pair.Value.GetComponent<UiButtonFeedback>()?.SetAvailability(available, reason);
            }
            endTurnButton?.GetComponent<UiButtonFeedback>()?.SetAvailability(heroTurn, heroTurn ? string.Empty : "等待敌方行动");
        }

        private static string StatusText(UnitState unit)
        {
            if (unit.Statuses.Count == 0) return "正常";
            return string.Join("  ", unit.Statuses.Select(item =>
            {
                CombatFeedbackSemantic semantic = CombatFeedbackCatalog.For(CombatFeedbackCatalog.ForStatus(item.Key));
                return "<color=" + semantic.ColorHex + ">" + semantic.ShortLabel + " " + item.Value + "</color>";
            }));
        }

        private static GameObject Panel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color color)
        {
            return FormalUiKit.Panel(name, parent, anchorMin, anchorMax, position, size, color);
        }

        private static Text Label(string name, Transform parent, Vector2 position, Vector2 size, int fontSize, Color color, TextAnchor alignment)
        {
            Text label = FormalUiKit.Label(name, name, parent, position, size, fontSize, color, alignment);
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private static void Line(Transform parent, Vector2 position, Vector2 size, Color color) => FormalUiKit.Line(parent, position, size, color, "细分隔");

        private Button Button(Transform parent, string name, Vector2 position, Vector2 size, string title, Color color, int fontSize = 16)
        {
            Button button = FormalUiKit.Button(name, title, parent, position, size, color, fontSize);
            Text label = button.GetComponentInChildren<Text>();
            label.verticalOverflow = VerticalWrapMode.Truncate;
            FormalUiKit.PreventAutomaticWrapping(label);
            FormalUiButtonPalette palette = new FormalUiButtonPalette(color, Color.Lerp(color, line, .28f), Color.Lerp(color, Color.black, .25f), Color.Lerp(color, line, .42f), FormalUiTheme.Disabled);
            FormalUiKit.ConfigureButtonFeedback(button, palette, () => UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity), bootstrap.ShowUiFeedback);
            return button;
        }

        private void OnDestroy()
        {
            if (bootstrap != null) bootstrap.UiPresentationVersions.Changed -= OnPresentationChanged;
            if (root != null) root.transform.DOKill();
        }
    }
}
