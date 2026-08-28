using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using OCC.Combat.Roguelite;

namespace OCC.Combat.Presentation
{
    // Runtime-built production HUD. It intentionally leaves the 75% tactical board unobstructed.
    public sealed class FormalCombatHud : MonoBehaviour
    {
        private static Color ink => FormalUiTheme.Ink;
        private static Color panel => FormalUiTheme.Panel;
        private static Color line => FormalUiTheme.Rule;
        private static Color muted => FormalUiTheme.Muted;
        private static Color text => FormalUiTheme.Text;
        private readonly Dictionary<string, Button> actionButtons = new Dictionary<string, Button>();
        private readonly Dictionary<string, Sprite> actionIcons = new Dictionary<string, Sprite>();
        private ICombatHudHost bootstrap;
        private Canvas canvas;
        private GameObject root;
        private FormalHoverTooltip tooltip;
        private Text activeLabel;
        private Text decisionLabel;
        private Text phaseLabel;
        private Text weaponLabel;
        private Text statusLabel;
        private Text actionPointLabel;
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
        private Text[] quickbarLabels = new Text[RogueRuntimeConstants.ItemQuickbarSize];
        private Image[] quickbarIcons = new Image[RogueRuntimeConstants.ItemQuickbarSize];
        private Image weaponIcon;
        private float displayedHealth = -1f;
        private float displayedShield = -1f;
        private float displayedMana = -1f;
        private bool wasVisible;
        private bool outcomeWasVisible;
        private bool hasPresentedModel;
        private CombatHudPresentationModel presentedModel;
        private bool presentedTargeting;
        private GridPosition presentedTargetPosition;
        private bool refreshDirty = true;
        public int RefreshCount { get; private set; }

        public void Initialize(ICombatHudHost source)
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
            bool handledTargetInput = bootstrap.IsDeveloperCombatActive && !bootstrap.IsInteractionModalOpen && HandleTargetNavigationInput();
            if (!handledTargetInput && bootstrap.IsDeveloperCombatActive && !bootstrap.IsInteractionModalOpen && RuntimeUiEventSystem.CancelPressedThisFrame()) bootstrap.CancelCombatSelectionOrRequestLeave();
            if (!refreshDirty) return;
            refreshDirty = false;
            CombatHudPresentationModel nextModel = CombatHudPresentationModel.From(bootstrap.CurrentState, bootstrap.SelectedAction, bootstrap.SelectedTargetId, bootstrap.IsCombatOutcomeVisible);
            bool targetingChanged = presentedTargeting != bootstrap.IsKeyboardTargeting || presentedTargetPosition != bootstrap.KeyboardTargetPosition;
            if (hasPresentedModel && presentedModel.Equals(nextModel) && !targetingChanged) return;
            presentedModel = nextModel;
            presentedTargeting = bootstrap.IsKeyboardTargeting;
            presentedTargetPosition = bootstrap.KeyboardTargetPosition;
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

            GameObject top = FormalUiKit.LayoutPanel("战斗抬头", root.transform, "combat.header", FormalUiTheme.SurfaceRaised);
            Label("战术行动", top.transform, new Vector2(20, -10), new Vector2(420, 34), 22, text, TextAnchor.MiddleLeft);
            phaseLabel = Label("准备阶段", top.transform, new Vector2(440, -10), new Vector2(620, 34), 17, line, TextAnchor.MiddleLeft);
            FormalUiKit.PreventAutomaticWrapping(phaseLabel);
            Line(top.transform, new Vector2(18, -53), new Vector2(1836, 2), line);

            GameObject side = FormalUiKit.LayoutPanel("战斗信息", root.transform, "combat.rightConsole", panel);
            GameObject selectedModule = FormalUiKit.LayoutPanel("本轮行动", side.transform, "combat.selected", panel);
            Label("本轮行动", selectedModule.transform, new Vector2(16, -7), new Vector2(174, 28), 18, text, TextAnchor.MiddleLeft);
            activeLabel = Label("等待行动", selectedModule.transform, new Vector2(188, -7), new Vector2(208, 28), 15, text, TextAnchor.MiddleRight);
            FormalUiKit.PreventAutomaticWrapping(activeLabel);
            decisionLabel = Label("行动决策", selectedModule.transform, new Vector2(16, -40), new Vector2(380, 48), 14, muted, TextAnchor.UpperLeft);
            FormalUiKit.ConfigureParagraph(decisionLabel);
            BindTooltip(selectedModule, BuildDecisionTooltip);

            GameObject heroModule = FormalUiKit.LayoutPanel("英雄概况", side.transform, "combat.hero", panel);
            Label("英雄", heroModule.transform, new Vector2(16, -8), new Vector2(360, 28), 18, text, TextAnchor.MiddleLeft);
            weaponLabel = Label("主手装备", heroModule.transform, new Vector2(16, -42), new Vector2(330, 38), 15, muted, TextAnchor.UpperLeft);
            FormalUiKit.PreventAutomaticWrapping(weaponLabel);
            weaponIcon = FormalUiKit.IconSlot("主手装备图标", heroModule.transform, null, Vector2.zero);
            weaponIcon.rectTransform.anchorMin = weaponIcon.rectTransform.anchorMax = new Vector2(0, 1);
            weaponIcon.rectTransform.pivot = new Vector2(0, 1); weaponIcon.rectTransform.anchoredPosition = new Vector2(364, -44);
            statusLabel = Label("状态", heroModule.transform, new Vector2(16, -88), new Vector2(380, 24), 16, muted, TextAnchor.UpperLeft);
            statusLabel.rectTransform.sizeDelta = new Vector2(230, 24);
            actionPointLabel = Label("行动点", heroModule.transform, new Vector2(246, -88), new Vector2(150, 24), 15, line, TextAnchor.UpperRight);
            FormalUiKit.PreventAutomaticWrapping(statusLabel);
            FormalUiKit.ConfigureNumericLabel(actionPointLabel);
            healthFill = ResourceBar(heroModule.transform, "生命", new Vector2(16, -116), FormalUiTheme.Health, out healthValue);
            shieldFill = ResourceBar(heroModule.transform, "护盾", new Vector2(16, -158), FormalUiTheme.Shield, out shieldValue);
            manaFill = ResourceBar(heroModule.transform, "个人魔力", new Vector2(16, -200), FormalUiTheme.Cyan, out manaValue);
            BindTooltip(heroModule, BuildHeroTooltip);

            timelineModule = FormalUiKit.LayoutPanel("行动序列模块", side.transform, "combat.timeline", panel);
            Label("接下来", timelineModule.transform, new Vector2(16, -6), new Vector2(240, 28), 17, text, TextAnchor.MiddleLeft);
            for (int i = 0; i < timelineNames.Length; i++) CreateTimelineSlot(i);

            logModule = FormalUiKit.LayoutPanel("现场记录模块", side.transform, "combat.log", panel);
            Label("现场记录", logModule.transform, new Vector2(16, -6), new Vector2(380, 28), 17, text, TextAnchor.MiddleLeft);
            eventLabel = Label("记录", logModule.transform, new Vector2(16, -38), new Vector2(380, 122), 16, muted, TextAnchor.UpperLeft);
            FormalUiKit.ConfigureParagraph(eventLabel);
            BindTooltip(logModule, BuildLogTooltip);

            GameObject bottom = FormalUiKit.LayoutPanel("战术指令", root.transform, "combat.commands", ink);
            GameObject weaponGroup = Panel("武器组", bottom.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -14), new Vector2(196, 172), panel);
            GameObject spellGroup = Panel("术式组", bottom.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(212, -14), new Vector2(584, 172), panel);
            GameObject interactionGroup = Panel("交互组", bottom.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(804, -14), new Vector2(150, 172), panel);
            GameObject itemGroup = Panel("物品组", bottom.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(962, -14), new Vector2(248, 172), panel);
            Label("移动 / 武器", weaponGroup.transform, new Vector2(8, -6), new Vector2(180, 28), 15, line, TextAnchor.MiddleLeft);
            Label("8 格个人术式", spellGroup.transform, new Vector2(8, -6), new Vector2(560, 28), 15, FormalUiTheme.Magic, TextAnchor.MiddleLeft);
            Label("交互", interactionGroup.transform, new Vector2(8, -6), new Vector2(134, 28), 15, FormalUiTheme.Amber, TextAnchor.MiddleLeft);
            Label("4 格战术栏", itemGroup.transform, new Vector2(8, -6), new Vector2(232, 28), 15, FormalUiTheme.Safe, TextAnchor.MiddleLeft);
            string[] primaryActions = { "移动", "攻击" };
            for (int i = 0; i < primaryActions.Length; i++)
            {
                string action = primaryActions[i];
                Button button = Button(weaponGroup.transform, action, new Vector2(8 + i * 92, -42), new Vector2(88, 116), InitialActionLabel(action), FormalUiTheme.Interactive, 14);
                AddActionIcon(button.transform, action);
                SetCostChips(button, 1, 0);
                button.onClick.AddListener(() => bootstrap.SelectHudAction(action));
                actionButtons.Add(action, button);
                BindTooltip(button.gameObject, () => BuildActionTooltip(action));
            }
            for (int slot = 0; slot < RogueRuntimeConstants.SpellSlotCount; slot++)
            {
                string action = "技能" + (slot + 1); int captured = slot;
                Button button = Button(spellGroup.transform, action, new Vector2(8 + (slot % 4) * 142, -42 - (slot / 4) * 60), new Vector2(138, 56), (slot + 1).ToString(), FormalUiTheme.Interactive, 18);
                Image spellIcon = FormalUiKit.IconSlot("正式图标", button.transform, actionIcons[slot == 1 ? "skill_two" : "skill"], new Vector2(4, 0));
                spellIcon.rectTransform.sizeDelta = new Vector2(42, 42);
                Text spellLabel = button.GetComponentInChildren<Text>();
                if (spellLabel != null)
                {
                    spellLabel.rectTransform.anchorMin = spellLabel.rectTransform.anchorMax = spellLabel.rectTransform.pivot = new Vector2(0, 1);
                    spellLabel.rectTransform.anchoredPosition = new Vector2(46, -2);
                    spellLabel.rectTransform.sizeDelta = new Vector2(22, 20);
                    spellLabel.fontSize = 17;
                    spellLabel.fontStyle = FontStyle.Bold;
                    spellLabel.alignment = TextAnchor.MiddleCenter;
                }
                button.onClick.AddListener(() => bootstrap.SelectHudAction(action)); actionButtons.Add(action, button);
                BindTooltip(button.gameObject, () => BuildActionTooltip("技能" + (captured + 1)));
            }
            string[] interactions = { "搜刮", "互动" };
            for (int i = 0; i < interactions.Length; i++)
            {
                string action = interactions[i];
                Button button = Button(interactionGroup.transform, action, new Vector2(8, -42 - i * 60), new Vector2(134, 56), action, FormalUiTheme.Interactive, 14);
                AddActionIcon(button.transform, action); SetCostChips(button, 1, 0);
                button.onClick.AddListener(() => bootstrap.SelectHudAction(action)); actionButtons.Add(action, button);
                BindTooltip(button.gameObject, () => BuildActionTooltip(action));
            }
            endTurnButton = Button(bottom.transform, "结束行动", new Vector2(1218, -14), new Vector2(178, 140), "结束行动\n未用 AP 作废", FormalUiTheme.Interactive, 18, FormalUiButtonTone.Primary);
            endTurnButton.onClick.AddListener(() => bootstrap.EndHeroTurn());
            BindTooltip(endTurnButton.gameObject, () => new FormalTooltipContent("结束行动", "未用行动点作废，随后轮到敌方。", line));
            restartButton = Button(top.transform, "战术重开", new Vector2(1650, -8), new Vector2(86, 38), "重开", FormalUiTheme.Interactive, 14, FormalUiButtonTone.Warning);
            restartButton.onClick.AddListener(bootstrap.RequestTacticalRestart);
            BindTooltip(restartButton.gameObject, () => new FormalTooltipContent("战术重开", "重新开始本场战斗，当前战斗进度将被撤销。", FormalUiTheme.Amber));
            leaveButton = Button(top.transform, "离开战斗", new Vector2(1744, -8), new Vector2(86, 38), "离开", FormalUiTheme.Interactive, 14, FormalUiButtonTone.Dangerous);
            leaveButton.onClick.AddListener(bootstrap.RequestLeaveCombat);
            BindTooltip(leaveButton.gameObject, () => new FormalTooltipContent("离开战斗", "离开当前战斗并返回行动入口。", FormalUiTheme.Danger));
            for (int i = 0; i < quickbarLabels.Length; i++)
            {
                int slot = i;
                Button quick = Button(itemGroup.transform, "快捷栏" + i, new Vector2(8 + (i % 2) * 116, -42 - (i / 2) * 60), new Vector2(112, 56), "", FormalUiTheme.Surface, 13, FormalUiButtonTone.Neutral);
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
            if (action == "移动") return "移动\n3 格";
            if (action == "攻击") return "攻击\n选目标";
            if (action == "搜刮") return "搜刮\n相邻容器";
            if (action == "互动") return "互动\n相邻物件";
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
            GameObject row = Panel("行动位" + (index + 1), timelineModule.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, y), new Vector2(388, 32), FormalUiTheme.WithAlpha(FormalUiTheme.Surface, .76f));
            timelineRows[index] = row.GetComponent<Image>();
            if (index < timelineNames.Length - 1)
                Line(timelineModule.transform, new Vector2(31, y - 27), new Vector2(2, 12), FormalUiTheme.WithAlpha(line, .28f));
            GameObject node = Panel("行动节点" + (index + 1), row.transform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(10, 0), new Vector2(14, 14), muted);
            timelineNodes[index] = node.GetComponent<Image>();
            timelineNames[index] = Label("行动者" + (index + 1), row.transform, new Vector2(36, -3), new Vector2(220, 26), CombatHudTypography.TimelineNameFontSize, muted, TextAnchor.MiddleLeft);
            timelineDetails[index] = Label("行动摘要" + (index + 1), row.transform, new Vector2(250, -3), new Vector2(124, 26), CombatHudTypography.TimelineDetailFontSize, muted, TextAnchor.MiddleRight);
            FormalUiKit.PreventAutomaticWrapping(timelineNames[index]);
            FormalUiKit.ConfigureNumericLabel(timelineDetails[index]);
        }

        private void Refresh()
        {
            RefreshCount++;
            CombatState state = bootstrap.CurrentState;
            UnitState hero = state.GetUnit("hero");
            UnitState active = state.GetUnit(state.ActiveUnitId);
            phaseLabel.text = bootstrap.IsKeyboardTargeting ? "键盘选点 · 方向键/WASD · Enter 确认 · Esc 取消" : bootstrap.CurrentPhaseText;
            activeLabel.text = active == null ? "等待行动" : active.DisplayName + "  ·  AP " + active.ActionPoints;
            UnitState selectedTarget = string.IsNullOrEmpty(bootstrap.SelectedTargetId) ? null : state.GetUnit(bootstrap.SelectedTargetId);
            CombatActionPreview decision = bootstrap.CurrentActionPreview;
            decisionLabel.text = CombatHudTypography.CompactDecisionSummary(
                CombatInformationPresenter.BuildHudDecisionSummary(decision, selectedTarget, bootstrap.IsKeyboardTargeting),
                state.Ruleset == CombatRuleset.Roguelite ? decision?.DamageBreakdown : null);
            decisionLabel.color = decision != null && !decision.CanSubmit ? FormalUiTheme.Danger : bootstrap.IsKeyboardTargeting ? FormalUiTheme.Cyan : muted;
            weaponLabel.text = hero.MainHand.DisplayName;
            weaponIcon.sprite = Resources.Load<Sprite>(FormalArtRegistry.ItemPath(hero.MainHand.Id));
            if (weaponIcon.sprite == null) throw new KeyNotFoundException("Missing formal item icon: " + hero.MainHand.Id);
            FireSpellDefinition fireOne = bootstrap.FireSpellInSlot(0), fireTwo = bootstrap.FireSpellInSlot(1);
            ArtifactDefinition artifactOne = bootstrap.CurrentArmedArtifact ?? bootstrap.CurrentTrainingRangeArtifact;
            bool rogue = state.Ruleset == CombatRuleset.Roguelite && state.RogueSpells != null;
            for (int slot = 0; slot < RogueRuntimeConstants.SpellSlotCount; slot++) actionButtons["技能" + (slot + 1)].gameObject.SetActive(rogue || slot < 2);
            if (rogue)
                for (int slot = 0; slot < RogueRuntimeConstants.SpellSlotCount; slot++) RefreshRogueSpellButton("技能" + (slot + 1), state.RogueSpells, slot);
            else
            {
                if (artifactOne != null) RefreshArtifactButton("技能1", artifactOne);
                else if (fireOne != null) RefreshFireSpellButton("技能1", fireOne, hero); else RefreshSkillButton("技能1", hero.SkillOne, hero);
                if (fireTwo != null) RefreshFireSpellButton("技能2", fireTwo, hero); else RefreshSkillButton("技能2", hero.SkillTwo, hero);
            }
            statusLabel.text = "状态 · " + StatusText(hero);
            actionPointLabel.text = "AP " + hero.ActionPoints;
            healthValue.text = hero.Health + " / " + hero.MaxHealth;
            shieldValue.text = rogue ? hero.Shield + "（无上限）" : hero.Shield + " / " + hero.MaxShield;
            manaValue.text = hero.Mana + " / " + hero.MaxMana;
            SetBar(healthFill, hero.Health / (float)Math.Max(1, hero.MaxHealth), ref displayedHealth);
            SetBar(shieldFill, rogue ? (hero.Shield > 0 ? 1f : 0f) : hero.Shield / (float)Math.Max(1, hero.MaxShield), ref displayedShield);
            SetBar(manaFill, hero.Mana / (float)Math.Max(1, hero.MaxMana), ref displayedMana);
            IReadOnlyList<CombatTurnTrackEntry> track = CombatTurnTrackPresentation.Build(state, timelineNames.Length);
            for (int i = 0; i < timelineNames.Length; i++)
            {
                bool visible = i < track.Count;
                timelineRows[i].gameObject.SetActive(visible);
                if (!visible) continue;
                CombatTurnTrackEntry entry = track[i];
                Color faction = entry.IsHero ? line : FormalUiTheme.Danger;
                timelineNodes[i].color = entry.IsActive ? faction : FormalUiTheme.WithAlpha(faction, .55f);
                timelineRows[i].color = entry.IsActive ? FormalUiTheme.WithAlpha(faction, .16f) : FormalUiTheme.WithAlpha(FormalUiTheme.Surface, .76f);
                timelineNames[i].text = entry.Order + "  " + entry.DisplayName;
                timelineNames[i].color = entry.IsActive ? text : muted;
                timelineDetails[i].text = entry.IsActive ? "正在行动" : entry.VitalityText;
                timelineDetails[i].color = entry.IsActive ? faction : muted;
            }
            eventLabel.text = state.EventLog.Count == 0 ? "—" : string.Join("\n", state.EventLog.Take(2).Select((entry, index) =>
                (index == 0 ? "▶ " : "   ") + CompactHud(CombatHudTypography.PlayerEventLine(entry), 34)));
            for (int i = 0; i < quickbarLabels.Length; i++)
            {
                if (rogue && state.RogueEquipment != null)
                {
                    string rogueId = state.RogueEquipment.ItemQuickbarInstanceIds[i];
                    RogueTacticalItemInstance tactical = state.RogueEquipment.TacticalItem(rogueId);
                    TacticalItemDefinition tacticalDefinition = tactical == null ? null : RogueContentCatalog.CreateAcademyV01().TacticalItems.First(value => value.DefinitionId == tactical.DefinitionId);
                    quickbarLabels[i].text = tactical == null ? (i + 1) + "·空" : (i + 1) + "  ×" + tactical.ChargesCurrent;
                    quickbarIcons[i].gameObject.SetActive(tactical != null);
                    if (tactical != null)
                    {
                        quickbarIcons[i].sprite = Resources.Load<Sprite>(FormalArtRegistry.ItemPath(tactical.DefinitionId));
                        if (quickbarIcons[i].sprite == null) throw new KeyNotFoundException("Missing rogue tactical icon: " + tactical.DefinitionId);
                    }
                    continue;
                }
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
                outcomeDetail.text = summary?.CompactDetailText ?? "请选择下一步。";
                outcomeBackButton.GetComponentInChildren<Text>().text = bootstrap.CurrentMapRun != null ? "返回地图" : "返回入口";
            }
            foreach (KeyValuePair<string, Button> pair in actionButtons)
            {
                Image image = pair.Value.GetComponent<Image>();
                image.color = pair.Key == bootstrap.SelectedAction ? Color.Lerp(FormalUiTheme.Interactive, FormalUiTheme.Cyan, .36f) : FormalUiTheme.Interactive;
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
            label.text = skill.DisplayName + "\n" + (cooldown > 0 ? "等待 " + cooldown + " 回合" : skill.Range + " 格");
            SetCostChips(button, 1, skill.ManaCost);
            SetNoticeChip(button, false);
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
                icon.sprite = sprite; icon.color = FormalUiTheme.Amber;
            }
            int cooldown = bootstrap.CurrentFireBattle == null ? 0 : bootstrap.CurrentFireBattle.Cooldown(hero.Id, spell.Id);
            string availability = armedItem != null ? armedItem.RemainingUses + "/" + armedItem.MaximumUses + "次" : artifact == null ? (cooldown > 0 ? "等待 " + cooldown + " 回合" : spell.Range + "格") : bootstrap.TrainingRangeArtifactUsesRemaining + "/" + artifact.MaximumUses + "次";
            label.text = spell.DisplayName + "\n" + availability;
            SetCostChips(button, spell.ActionPointCost, spell.ManaCost);
            SetNoticeChip(button, false);
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
            label.text = artifact.DisplayName + "\n" + remaining + "/" + artifact.MaximumUses + " 次";
            SetCostChips(button, artifact.ActionPointCost, 0);
            SetNoticeChip(button, !string.IsNullOrWhiteSpace(artifact.RiskSummary));
        }

        private void RefreshRogueSpellButton(string key, RogueSpellCombatRuntime runtime, int slot)
        {
            if (!actionButtons.TryGetValue(key, out Button button)) return;
            SpellDefinition spell = runtime.DefinitionAtSlot(slot); Text label = button.GetComponentInChildren<Text>();
            if (label == null) return;
            if (spell == null) { label.text = (slot + 1).ToString(); SetCostChips(button, 0, 0); SetNoticeChip(button, true); return; }
            int cooldown = runtime.CooldownRemaining(spell.DefinitionId);
            label.text = new RogueSpellSlotPresentation(slot, spell, cooldown).CompactSlotLabel;
            Image icon = button.GetComponentsInChildren<Image>().FirstOrDefault(image => image.gameObject.name == "正式图标");
            if (icon != null)
            {
                icon.sprite = Resources.Load<Sprite>(RogueSpellIconPath(spell.DefinitionId));
                icon.color = spell.Element == "fire" ? FormalUiTheme.Amber : FormalUiTheme.Magic;
            }
            SetCostChips(button, spell.ActionPointCost, spell.ManaCost); SetNoticeChip(button, cooldown > 0, cooldown);
        }

        private static string RogueSpellIconPath(string definitionId)
        {
            if (!string.IsNullOrEmpty(definitionId) && definitionId.StartsWith("F-P-", StringComparison.Ordinal)) return FormalArtRegistry.FireSpellPath(definitionId);
            if (definitionId == "BASE-AETHER-SHIELD") return FormalArtRegistry.FeedbackPath("shield_restore");
            if (definitionId == "BASE-MANA-RECOVER") return FormalArtRegistry.FeedbackPath("mana_restore");
            return FormalArtRegistry.CommandPath(definitionId == "BASE-FIRE-RANGED" ? "skill_two" : "skill");
        }

        private void SetCostChips(Button button, int actionCost, int aetherCost)
        {
            if (button == null) return;
            bool spellSlot = button.name.StartsWith("技能", StringComparison.Ordinal);
            Text actionValue = button.transform.Find("语义_action/数值")?.GetComponent<Text>();
            if (actionValue == null)
                actionValue = FormalUiKit.SemanticChip("action", actionCost.ToString(), button.transform, CostChipPosition(button, false, aetherCost > 0), tooltip,
                    spellSlot ? 22 : 18, spellSlot ? 18 : CombatHudTypography.CostValueFontSize, line);
            actionValue.text = actionCost.ToString();
            ConfigureCostChip(actionValue.transform.parent, spellSlot, CostChipPosition(button, false, aetherCost > 0), line);

            Transform aetherChip = button.transform.Find("语义_aether");
            if (aetherCost > 0)
            {
                Text aetherValue = aetherChip?.Find("数值")?.GetComponent<Text>();
                if (aetherValue == null)
                    aetherValue = FormalUiKit.SemanticChip("aether", aetherCost.ToString(), button.transform, CostChipPosition(button, true, true), tooltip,
                        spellSlot ? 22 : 18, spellSlot ? 18 : CombatHudTypography.CostValueFontSize, FormalUiTheme.Magic);
                aetherValue.text = aetherCost.ToString();
                aetherValue.transform.parent.gameObject.SetActive(true);
                ConfigureCostChip(aetherValue.transform.parent, spellSlot, CostChipPosition(button, true, true), FormalUiTheme.Magic);
            }
            else if (aetherChip != null) aetherChip.gameObject.SetActive(false);
        }

        private static Vector2 CostChipPosition(Button button, bool second, bool hasSecond)
        {
            RectTransform rect = button.GetComponent<RectTransform>(); float width = rect == null ? 80f : rect.sizeDelta.x; float height = rect == null ? 40f : rect.sizeDelta.y;
            if (button.name.StartsWith("技能", StringComparison.Ordinal))
                return new Vector2(hasSecond ? (second ? 92f : 48f) : 70f, -26f);
            return new Vector2(Mathf.Max(4f, width - (second ? 62f : 98f)), -Mathf.Max(18f, height - 18f));
        }

        private static void ConfigureCostChip(Transform chip, bool spellSlot, Vector2 position, Color accent)
        {
            if (chip == null || !spellSlot) return;
            RectTransform chipRect = chip.GetComponent<RectTransform>();
            chipRect.anchoredPosition = position;
            chipRect.sizeDelta = new Vector2(44, 28);
            Image background = chip.GetComponent<Image>() ?? chip.gameObject.AddComponent<Image>();
            background.color = FormalUiTheme.WithAlpha(FormalUiTheme.Ink, .88f);
            background.raycastTarget = false;

            RectTransform iconRect = chip.GetChild(0).GetComponent<RectTransform>();
            if (iconRect != null)
            {
                iconRect.anchoredPosition = new Vector2(3, 0);
                iconRect.sizeDelta = new Vector2(22, 22);
            }
            Text value = chip.Find("数值")?.GetComponent<Text>();
            if (value == null) return;
            value.rectTransform.anchoredPosition = new Vector2(25, 0);
            value.rectTransform.sizeDelta = new Vector2(17, 28);
            value.fontSize = 18;
            value.fontStyle = FontStyle.Bold;
            value.alignment = TextAnchor.MiddleCenter;
            value.color = accent;
        }

        private void SetNoticeChip(Button button, bool visible, int value = -1)
        {
            if (button == null) return;
            Transform noticeChip = button.transform.Find("语义_notice");
            if (visible && noticeChip == null)
            {
                RectTransform rect = button.GetComponent<RectTransform>();
                float width = rect == null ? 80f : rect.sizeDelta.x;
                FormalUiKit.SemanticChip("notice", value >= 0 ? value.ToString() : string.Empty, button.transform,
                    new Vector2(Mathf.Max(4f, width - 24f), -4f), tooltip, 16, 12, FormalUiTheme.Amber);
                noticeChip = button.transform.Find("语义_notice");
            }
            Text noticeValue = noticeChip?.Find("数值")?.GetComponent<Text>();
            if (noticeValue != null) noticeValue.text = value >= 0 ? value.ToString() : string.Empty;
            if (noticeChip != null) noticeChip.gameObject.SetActive(visible);
        }

        private void CreateOutcomeOverlay()
        {
            outcomeOverlay = FormalUiKit.LayoutPanel("战斗结果", root.transform, "combat.outcome", FormalUiTheme.WithAlpha(FormalUiTheme.SurfaceRaised, .99f));
            outcomeTitle = Label("结果标题", outcomeOverlay.transform, new Vector2(40, -34), new Vector2(640, 58), 36, text, TextAnchor.MiddleCenter);
            outcomeDetail = Label("结果说明", outcomeOverlay.transform, new Vector2(40, -102), new Vector2(640, 100), 16, muted, TextAnchor.UpperCenter);
            outcomeRestartButton = Button(outcomeOverlay.transform, "结果重开", new Vector2(60, -180), new Vector2(280, 64), "战术重开", FormalUiTheme.Interactive, FormalUiTheme.ButtonFontSize, FormalUiButtonTone.Primary);
            outcomeRestartButton.onClick.AddListener(bootstrap.RequestTacticalRestart);
            outcomeBackButton = Button(outcomeOverlay.transform, "结果返回", new Vector2(380, -180), new Vector2(280, 64), "返回入口", FormalUiTheme.Interactive, FormalUiTheme.ButtonFontSize, FormalUiButtonTone.Warning);
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
            int rogueSlot = SkillSlot(action);
            SpellDefinition rogueSpell = bootstrap?.CurrentState?.Ruleset == CombatRuleset.Roguelite && bootstrap.CurrentState.RogueSpells != null && rogueSlot >= 0
                ? bootstrap.CurrentState.RogueSpells.DefinitionAtSlot(rogueSlot) : null;
            if (rogueSpell != null) title = (rogueSlot + 1) + " · " + rogueSpell.DisplayName;
            if (actionButtons.TryGetValue(action, out Button button))
            {
                string label = button.GetComponentInChildren<Text>()?.text;
                if (rogueSpell == null && !string.IsNullOrWhiteSpace(label)) title = label.Split('\n')[0];
            }
            return new FormalTooltipContent(title, CombatInformationPresenter.BuildActionDetails(preview), line);
        }

        private FormalTooltipContent BuildDecisionTooltip()
        {
            CombatState state = bootstrap?.CurrentState;
            UnitState target = state == null || string.IsNullOrEmpty(bootstrap.SelectedTargetId) ? null : state.GetUnit(bootstrap.SelectedTargetId);
            EnemyIntentPresentation intent = target == null || target.IsHero ? null : bootstrap.EnemyIntent(target);
            return new FormalTooltipContent("当前行动详情", CombatInformationPresenter.BuildTargetDetails(bootstrap?.CurrentActionPreview, target, intent,
                bootstrap?.CurrentState?.Ruleset == CombatRuleset.Roguelite), line);
        }

        private bool HandleTargetNavigationInput()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            bool toggle = keyboard?.tKey.wasPressedThisFrame == true || gamepad?.rightShoulder.wasPressedThisFrame == true;
            if (!bootstrap.IsKeyboardTargeting)
            {
                if (!toggle || !bootstrap.BeginKeyboardTargeting()) return false;
                RuntimeUiEventSystem.ClearSelection();
                return true;
            }

            if (RuntimeUiEventSystem.CancelPressedThisFrame() || toggle)
            {
                bootstrap.CancelKeyboardTargeting();
                RestoreActionFocus();
                return true;
            }

            int deltaX = keyboard?.leftArrowKey.wasPressedThisFrame == true || keyboard?.aKey.wasPressedThisFrame == true || gamepad?.dpad.left.wasPressedThisFrame == true ? -1 :
                keyboard?.rightArrowKey.wasPressedThisFrame == true || keyboard?.dKey.wasPressedThisFrame == true || gamepad?.dpad.right.wasPressedThisFrame == true ? 1 : 0;
            int deltaY = keyboard?.downArrowKey.wasPressedThisFrame == true || keyboard?.sKey.wasPressedThisFrame == true || gamepad?.dpad.down.wasPressedThisFrame == true ? -1 :
                keyboard?.upArrowKey.wasPressedThisFrame == true || keyboard?.wKey.wasPressedThisFrame == true || gamepad?.dpad.up.wasPressedThisFrame == true ? 1 : 0;
            if (deltaX != 0 || deltaY != 0)
            {
                bootstrap.MoveKeyboardTarget(deltaX, deltaY);
                return true;
            }

            bool confirm = keyboard?.enterKey.wasPressedThisFrame == true || keyboard?.spaceKey.wasPressedThisFrame == true || gamepad?.buttonSouth.wasPressedThisFrame == true;
            if (!confirm) return false;
            bootstrap.CommitKeyboardTarget();
            RestoreActionFocus();
            return true;
        }

        private void RestoreActionFocus()
        {
            if (actionButtons.TryGetValue(bootstrap.SelectedAction, out Button selected) && selected != null && selected.interactable)
                RuntimeUiEventSystem.Select(selected.gameObject);
            else if (actionButtons.TryGetValue("移动", out Button move) && move != null && move.interactable)
                RuntimeUiEventSystem.Select(move.gameObject);
        }

        private FormalTooltipContent BuildHeroTooltip()
        {
            CombatState state = bootstrap?.CurrentState;
            UnitState hero = state?.GetUnit("hero");
            bool rogue = state?.Ruleset == CombatRuleset.Roguelite;
            string details = rogue ? CombatInformationPresenter.BuildRogueliteHeroDetails(hero) : CombatInformationPresenter.BuildHeroDetails(hero);
            if (rogue && state.RogueShieldEvents.Count > 0)
                details += "\n最近护盾\n" + string.Join("\n", state.RogueShieldEvents.Take(2).Select(RogueShieldLogPresentation.Format));
            return new FormalTooltipContent("英雄详情", details, FormalUiTheme.Safe);
        }

        private FormalTooltipContent BuildLogTooltip()
        {
            CombatState state = bootstrap?.CurrentState;
            string body = state == null || state.EventLog.Count == 0 ? "暂无记录" :
                string.Join("\n", state.EventLog.Take(5).Select(CombatHudTypography.PlayerEventLine));
            return new FormalTooltipContent("最近现场记录", body, FormalUiTheme.Amber);
        }

        private FormalTooltipContent BuildQuickbarTooltip(int slot)
        {
            CombatState state = bootstrap?.CurrentState;
            if (state?.Ruleset == CombatRuleset.Roguelite && state.RogueEquipment != null)
            {
                string id = state.RogueEquipment.ItemQuickbarInstanceIds[slot]; RogueTacticalItemInstance tactical = state.RogueEquipment.TacticalItem(id);
                TacticalItemDefinition tacticalDefinition = state.RogueEquipment.TacticalDefinitionFor(id);
                return new FormalTooltipContent(tactical == null ? "战术栏 " + (slot + 1) : tacticalDefinition.DisplayName,
                    tactical == null ? "空槽" : "剩余 " + tactical.ChargesCurrent + "/" + tactical.ChargesMaximum + " · 使用 1 行动点", FormalUiTheme.Safe);
            }
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
            Label(title, parent, position, new Vector2(200, 24), 15, muted, TextAnchor.MiddleLeft);
            valueLabel = Label(title + "数值", parent, position, new Vector2(374, 24), CombatHudTypography.ResourceValueFontSize,
                text, CombatHudTypography.ResourceValueAlignment);
            FormalUiKit.ConfigureNumericLabel(valueLabel);
            GameObject track = Panel(title + "轨道", parent, new Vector2(0, 1), new Vector2(0, 1), position + new Vector2(0, -28), new Vector2(390, 15), FormalUiTheme.Ink);
            GameObject fill = Panel(title + "填充", track.transform, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero, color);
            FormalUiKit.ApplySkin(track.GetComponent<Image>(), "bar_track", Color.white);
            string fillSkin = title == "结构" ? "bar_segment_health" : title == "护盾" ? "bar_segment_shield" : title == "以太" || title == "个人魔力" ? "bar_segment_mana" : "bar_fill";
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
                int parsedSlot = SkillSlot(pair.Key);
                SpellDefinition rogueSpell = state.Ruleset == CombatRuleset.Roguelite && state.RogueSpells != null && parsedSlot >= 0 ? state.RogueSpells.DefinitionAtSlot(parsedSlot) : null;
                SkillDefinition skill = pair.Key == "技能1" ? hero.SkillOne : pair.Key == "技能2" ? hero.SkillTwo : null;
                int fireSlot = parsedSlot;
                FireSpellDefinition fire = fireSlot < 0 ? null : bootstrap.FireSpellInSlot(fireSlot);
                ArtifactDefinition artifact = fireSlot == 0 ? (bootstrap.CurrentArmedArtifact ?? bootstrap.CurrentTrainingRangeArtifact) : null;
                if (available && artifact != null && hero.ActionPoints < artifact.ActionPointCost) { available = false; reason = "行动点不足：需要 " + artifact.ActionPointCost; }
                if (available && fire != null && bootstrap.CurrentFireBattle != null && bootstrap.CurrentFireBattle.Cooldown(hero.Id, fire.Id) > 0) { available = false; reason = "术式冷却中 · " + bootstrap.CurrentFireBattle.Cooldown(hero.Id, fire.Id) + " 回合"; }
                if (available && fire != null && hero.Mana < fire.ManaCost) { available = false; reason = "能量不足：需要 " + fire.ManaCost; }
                if (available && fire != null && hero.ActionPoints < fire.ActionPointCost) { available = false; reason = "行动点不足：需要 " + fire.ActionPointCost; }
                if (fire != null || artifact != null) skill = null;
                if (available && skill != null && hero.Cooldown(skill) > 0) { available = false; reason = "技能冷却中 · " + hero.Cooldown(skill) + " 回合"; }
                if (available && skill != null && hero.Mana < skill.ManaCost) { available = false; reason = "以太不足：需要 " + skill.ManaCost; }
                if (available && parsedSlot >= 0 && state.Ruleset == CombatRuleset.Roguelite && rogueSpell == null) { available = false; reason = "术式槽为空"; }
                if (available && rogueSpell != null && state.RogueSpells.CooldownRemaining(rogueSpell.DefinitionId) > 0) { available = false; reason = "术式冷却中"; }
                if (available && rogueSpell != null && hero.Mana < rogueSpell.ManaCost) { available = false; reason = "个人魔力不足：需要 " + rogueSpell.ManaCost; }
                if (available && rogueSpell != null && hero.ActionPoints < rogueSpell.ActionPointCost) { available = false; reason = "行动点不足：需要 " + rogueSpell.ActionPointCost; }
                pair.Value.GetComponent<UiButtonFeedback>()?.SetAvailability(available, reason);
            }
            endTurnButton?.GetComponent<UiButtonFeedback>()?.SetAvailability(heroTurn, heroTurn ? string.Empty : "等待敌方行动");
        }

        private static int SkillSlot(string action)
        { return action != null && action.StartsWith("技能", StringComparison.Ordinal) && int.TryParse(action.Substring(2), out int oneBased) && oneBased >= 1 && oneBased <= RogueRuntimeConstants.SpellSlotCount ? oneBased - 1 : -1; }

        private static string CompactHud(string value, int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return value.Length <= maximumLength ? value : value.Substring(0, maximumLength - 1) + "…";
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
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        private static void Line(Transform parent, Vector2 position, Vector2 size, Color color) => FormalUiKit.Line(parent, position, size, color, "细分隔");

        private Button Button(Transform parent, string name, Vector2 position, Vector2 size, string title, Color color, int fontSize = 16, FormalUiButtonTone tone = FormalUiButtonTone.Primary)
        {
            Button button = FormalUiKit.Button(name, title, parent, position, size, color, fontSize);
            Text label = button.GetComponentInChildren<Text>();
            label.verticalOverflow = VerticalWrapMode.Truncate;
            FormalUiKit.PreventAutomaticWrapping(label);
            FormalUiButtonPalette semantic = FormalUiTheme.ButtonPalette(tone);
            FormalUiButtonPalette palette = new FormalUiButtonPalette(color, semantic.Hover, semantic.Pressed, semantic.Selected, semantic.Disabled);
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
