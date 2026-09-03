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
    // Runtime-built production HUD. The battlefield keeps its 75% width above a full-width command deck.
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
        private Text modeLabel;
        private Image modePanelImage;
        private Text activeLabel;
        private Text decisionLabel;
        private Text phaseLabel;
        private Text weaponLabel;
        private Text statusLabel;
        private Text actionPointLabel;
        private readonly Image[] actionPointPips = new Image[3];
        private readonly CanvasGroup[] actionPointPipGroups = new CanvasGroup[3];
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
        private readonly Dictionary<Image, Image> resourceChangeMarkers = new Dictionary<Image, Image>();
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
        private int displayedActionPoints = -1;
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
            bool handledShortcut = bootstrap.IsDeveloperCombatActive && !bootstrap.IsInteractionModalOpen && HandleSpellShortcutInput();
            bool handledTargetInput = !handledShortcut && bootstrap.IsDeveloperCombatActive && !bootstrap.IsInteractionModalOpen && HandleTargetNavigationInput();
            if (!handledShortcut && !handledTargetInput && bootstrap.IsDeveloperCombatActive && !bootstrap.IsInteractionModalOpen && RuntimeUiEventSystem.CancelPressedThisFrame()) bootstrap.CancelCombatSelectionOrRequestLeave();
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
            Label("战场", top.transform, new Vector2(20, -8), new Vector2(420, 40), FormalUiTheme.BodyFontSize, text, TextAnchor.MiddleLeft);
            phaseLabel = Label("正在准备", top.transform, new Vector2(440, -8), new Vector2(980, 40), FormalUiTheme.BodyFontSize, line, TextAnchor.MiddleLeft);
            FormalUiKit.PreventAutomaticWrapping(phaseLabel);
            Line(top.transform, new Vector2(18, -53), new Vector2(1836, 2), line);

            GameObject side = FormalUiKit.LayoutPanel("战斗信息", root.transform, "combat.rightConsole", panel);
            GameObject selectedModule = FormalUiKit.LayoutPanel("本轮行动", side.transform, "combat.selected", panel);
            modePanelImage = selectedModule.GetComponent<Image>();
            modeLabel = Label("当前模式 · 移动", selectedModule.transform, new Vector2(16, -4), new Vector2(224, 40), FormalUiTheme.BodyFontSize, FormalUiTheme.Cyan, TextAnchor.MiddleLeft);
            activeLabel = Label("等待行动", selectedModule.transform, new Vector2(242, -4), new Vector2(154, 40), FormalUiTheme.BodyFontSize, text, TextAnchor.MiddleRight);
            FormalUiKit.PreventAutomaticWrapping(modeLabel);
            FormalUiKit.PreventAutomaticWrapping(activeLabel);
            decisionLabel = Label("行动决策", selectedModule.transform, new Vector2(16, -40), new Vector2(380, 72), FormalUiTheme.BodyFontSize, muted, TextAnchor.UpperLeft);
            FormalUiKit.ConfigureParagraph(decisionLabel);
            BindTooltip(selectedModule, BuildDecisionTooltip);

            GameObject heroModule = FormalUiKit.LayoutPanel("英雄概况", side.transform, "combat.hero", panel);
            Label("英雄", heroModule.transform, new Vector2(16, -4), new Vector2(360, 40), FormalUiTheme.BodyFontSize, text, TextAnchor.MiddleLeft);
            weaponLabel = Label("主手装备", heroModule.transform, new Vector2(16, -40), new Vector2(330, 40), FormalUiTheme.BodyFontSize, muted, TextAnchor.MiddleLeft);
            FormalUiKit.PreventAutomaticWrapping(weaponLabel);
            weaponIcon = FormalUiKit.IconSlot("主手装备图标", heroModule.transform, null, Vector2.zero);
            weaponIcon.rectTransform.anchorMin = weaponIcon.rectTransform.anchorMax = new Vector2(0, 1);
            weaponIcon.rectTransform.pivot = new Vector2(0, 1); weaponIcon.rectTransform.anchoredPosition = new Vector2(364, -44);
            statusLabel = Label("状态", heroModule.transform, new Vector2(16, -76), new Vector2(176, 40), FormalUiTheme.BodyFontSize, muted, TextAnchor.MiddleLeft);
            actionPointLabel = Label("行动点", heroModule.transform, new Vector2(192, -76), new Vector2(144, 40), FormalUiTheme.BodyFontSize, FormalUiTheme.Cyan, TextAnchor.MiddleRight);
            for (int i = 0; i < actionPointPips.Length; i++)
            {
                GameObject pipTrack = Panel("行动点轨道_" + i, heroModule.transform, new Vector2(0, 1), new Vector2(0, 1),
                    ActionPointPipPosition(i), new Vector2(20, 20), FormalUiTheme.Ink);
                FormalUiKit.ApplySkin(pipTrack.GetComponent<Image>(), "bar_track", FormalUiTheme.Ink);
                GameObject pipFill = Panel("行动点填充_" + i, pipTrack.transform, Vector2.zero, Vector2.one,
                    Vector2.zero, Vector2.zero, FormalUiTheme.Cyan);
                actionPointPips[i] = pipFill.GetComponent<Image>();
                FormalUiKit.ApplySkin(actionPointPips[i], "bar_fill", FormalUiTheme.Cyan);
                actionPointPipGroups[i] = pipFill.AddComponent<CanvasGroup>();
                actionPointPipGroups[i].alpha = .16f;
            }
            FormalUiKit.PreventAutomaticWrapping(statusLabel);
            FormalUiKit.ConfigureNumericLabel(actionPointLabel);
            healthFill = ResourceBar(heroModule.transform, "生命", new Vector2(16, -104), FormalUiTheme.Health, out healthValue);
            shieldFill = ResourceBar(heroModule.transform, "护盾", new Vector2(16, -150), FormalUiTheme.Shield, out shieldValue);
            manaFill = ResourceBar(heroModule.transform, "个人魔力", new Vector2(16, -196), FormalUiTheme.Magic, out manaValue);
            BindTooltip(heroModule, BuildHeroTooltip);

            timelineModule = FormalUiKit.LayoutPanel("行动序列模块", side.transform, "combat.timeline", panel);
            Label("接下来", timelineModule.transform, new Vector2(16, -4), new Vector2(240, 40), FormalUiTheme.BodyFontSize, text, TextAnchor.MiddleLeft);
            for (int i = 0; i < timelineNames.Length; i++) CreateTimelineSlot(i);

            logModule = FormalUiKit.LayoutPanel("现场记录模块", side.transform, "combat.log", panel);
            Label("刚刚发生", logModule.transform, new Vector2(16, -4), new Vector2(380, 40), FormalUiTheme.BodyFontSize, text, TextAnchor.MiddleLeft);
            eventLabel = Label("记录", logModule.transform, new Vector2(16, -40), new Vector2(380, 80), FormalUiTheme.BodyFontSize, muted, TextAnchor.UpperLeft);
            FormalUiKit.ConfigureReadingParagraph(eventLabel);
            BindTooltip(logModule, BuildLogTooltip);

            GameObject bottom = FormalUiKit.LayoutPanel("战术指令", root.transform, "combat.commands", ink);
            GameObject weaponGroup = Panel("武器组", bottom.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -14), new Vector2(200, 172), panel);
            GameObject spellGroup = Panel("术式组", bottom.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(216, -2), new Vector2(1100, 196), panel);
            GameObject interactionGroup = Panel("交互组", bottom.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(1324, -14), new Vector2(152, 172), panel);
            GameObject itemGroup = Panel("物品组", bottom.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(1484, -14), new Vector2(176, 172), panel);
            Label("移动 / 武器", weaponGroup.transform, new Vector2(8, -4), new Vector2(184, 40), FormalUiTheme.BodyFontSize, line, TextAnchor.MiddleLeft);
            Label("个人术式 · 数字键 1–8 快捷选择", spellGroup.transform, new Vector2(8, -4), new Vector2(1084, 40), FormalUiTheme.BodyFontSize, text, TextAnchor.MiddleLeft);
            Label("交互", interactionGroup.transform, new Vector2(8, -4), new Vector2(136, 40), FormalUiTheme.BodyFontSize, text, TextAnchor.MiddleLeft);
            Label("战术栏", itemGroup.transform, new Vector2(8, -4), new Vector2(160, 40), FormalUiTheme.BodyFontSize, text, TextAnchor.MiddleLeft);
            string[] primaryActions = { "移动", "攻击" };
            for (int i = 0; i < primaryActions.Length; i++)
            {
                string action = primaryActions[i];
                Button button = Button(weaponGroup.transform, action, new Vector2(8 + i * 96, -48), new Vector2(88, 116), InitialActionLabel(action), FormalUiTheme.Interactive, FormalUiTheme.ButtonFontSize);
                AddActionIcon(button.transform, action);
                SetCostChips(button, 1, 0);
                button.onClick.AddListener(() => bootstrap.SelectHudAction(action));
                actionButtons.Add(action, button);
                BindTooltip(button.gameObject, () => BuildActionTooltip(action));
            }
            for (int slot = 0; slot < RogueRuntimeConstants.SpellSlotCount; slot++)
            {
                string action = "技能" + (slot + 1); int captured = slot;
                Button button = Button(spellGroup.transform, action,
                    new Vector2(8 + (slot % 4) * 272, -44 - (slot / 4) * 76),
                    new Vector2(268, 76), "空槽", FormalUiTheme.Interactive, FormalUiTheme.ButtonFontSize);
                Image spellIcon = FormalUiKit.IconSlot("正式图标", button.transform, actionIcons[slot == 1 ? "skill_two" : "skill"], new Vector2(4, 0));
                ConfigureSpellSlotLayout(button, spellIcon, slot);
                button.onClick.AddListener(() => bootstrap.SelectHudAction(action)); actionButtons.Add(action, button);
                BindTooltip(button.gameObject, () => BuildActionTooltip("技能" + (captured + 1)));
            }
            string[] interactions = { "搜刮", "互动" };
            for (int i = 0; i < interactions.Length; i++)
            {
                string action = interactions[i];
                Button button = Button(interactionGroup.transform, action, new Vector2(8, -48 - i * 58), new Vector2(136, 54), action, FormalUiTheme.Interactive, FormalUiTheme.ButtonFontSize);
                AddActionIcon(button.transform, action); SetCostChips(button, 1, 0);
                button.onClick.AddListener(() => bootstrap.SelectHudAction(action)); actionButtons.Add(action, button);
                BindTooltip(button.gameObject, () => BuildActionTooltip(action));
            }
            endTurnButton = Button(bottom.transform, "结束行动", new Vector2(1668, -24), new Vector2(204, 140), "结束回合\n行动点会清空", FormalUiTheme.Interactive, FormalUiTheme.ButtonFontSize, FormalUiButtonTone.Primary);
            endTurnButton.onClick.AddListener(() => bootstrap.EndHeroTurn());
            BindTooltip(endTurnButton.gameObject, () => new FormalTooltipContent("结束回合", "剩余行动点会清空，然后轮到敌方。", line));
            restartButton = Button(top.transform, "战术重开", new Vector2(1650, -4), new Vector2(86, 48), "重开", FormalUiTheme.Interactive, FormalUiTheme.ButtonFontSize, FormalUiButtonTone.Warning);
            restartButton.onClick.AddListener(bootstrap.RequestTacticalRestart);
            BindTooltip(restartButton.gameObject, () => new FormalTooltipContent("重新开始", "这场战斗会从头开始，用掉的道具也会恢复。", FormalUiTheme.Amber));
            leaveButton = Button(top.transform, "离开战斗", new Vector2(1744, -4), new Vector2(86, 48), "离开", FormalUiTheme.Interactive, FormalUiTheme.ButtonFontSize, FormalUiButtonTone.Dangerous);
            leaveButton.onClick.AddListener(bootstrap.RequestLeaveCombat);
            BindTooltip(leaveButton.gameObject, () => new FormalTooltipContent("离开战斗", "回到地图。这场战斗的收获和损失都不会保留。", FormalUiTheme.Danger));
            for (int i = 0; i < quickbarLabels.Length; i++)
            {
                int slot = i;
                Button quick = Button(itemGroup.transform, "快捷栏" + i, new Vector2(8 + (i % 2) * 84, -48 - (i / 2) * 58), new Vector2(76, 54), "", FormalUiTheme.Surface, FormalUiTheme.ButtonFontSize, FormalUiButtonTone.Neutral);
                quickbarLabels[i] = quick.GetComponentInChildren<Text>();
                quickbarIcons[i] = FormalUiKit.IconSlot("快捷栏正式图标", quick.transform, null, new Vector2(2, 0));
                if (quickbarLabels[i] != null) quickbarLabels[i].rectTransform.offsetMin = new Vector2(24, 0);
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
            if (action == "移动") return "移动";
            if (action == "攻击") return "攻击";
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

        private static Vector2 ActionPointPipPosition(int index) => new Vector2(338 + index * 23, -86);

        private void CreateTimelineSlot(int index)
        {
            float y = -48f - index * 42f;
            GameObject row = FormalUiKit.FlatPanel("行动位" + (index + 1), timelineModule.transform,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, y), new Vector2(388, 40),
                FormalUiTheme.WithAlpha(FormalUiTheme.Surface, index % 2 == 0 ? .86f : .70f));
            timelineRows[index] = row.GetComponent<Image>();
            GameObject node = Panel("行动节点" + (index + 1), row.transform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(10, 0), new Vector2(14, 14), muted);
            timelineNodes[index] = node.GetComponent<Image>();
            timelineNames[index] = Label("行动者" + (index + 1), row.transform, new Vector2(36, 0), new Vector2(216, 40), CombatHudTypography.TimelineNameFontSize, muted, TextAnchor.MiddleLeft);
            timelineDetails[index] = Label("行动摘要" + (index + 1), row.transform, new Vector2(260, 0), new Vector2(114, 40), CombatHudTypography.TimelineDetailFontSize, muted, TextAnchor.MiddleRight);
            FormalUiKit.PreventAutomaticWrapping(timelineNames[index]);
            FormalUiKit.ConfigureNumericLabel(timelineDetails[index]);
            Line(row.transform, new Vector2(36, -38), new Vector2(338, 2), FormalUiTheme.WithAlpha(line, .34f));
        }

        private void Refresh()
        {
            RefreshCount++;
            CombatState state = bootstrap.CurrentState;
            UnitState hero = state.GetUnit("hero");
            UnitState active = state.GetUnit(state.ActiveUnitId);
            string modeName = SelectedModeDisplayName(state, hero);
            string clickInstruction = PrimaryClickInstruction(bootstrap.SelectedAction);
            phaseLabel.text = bootstrap.IsKeyboardTargeting
                ? "选择目标：方向键或 WASD 移动，Enter 确认，Esc 取消"
                : bootstrap.CurrentPhaseText + "  ·  " + clickInstruction + " / 右键更多";
            modeLabel.text = "当前模式 · " + modeName;
            modeLabel.color = state.ActiveUnitId == "hero" ? FormalUiTheme.Cyan : FormalUiTheme.Muted;
            if (modePanelImage != null)
                modePanelImage.color = state.ActiveUnitId == "hero"
                    ? Color.Lerp(panel, FormalUiTheme.Cyan, .14f)
                    : Color.Lerp(panel, FormalUiTheme.Danger, .08f);
            activeLabel.text = active == null ? "等待" : "行动点 " + active.ActionPoints;
            UnitState selectedTarget = string.IsNullOrEmpty(bootstrap.SelectedTargetId) ? null : state.GetUnit(bootstrap.SelectedTargetId);
            CombatActionPreview decision = bootstrap.CurrentActionPreview;
            string decisionSummary = CombatHudTypography.CompactDecisionSummary(
                CombatInformationPresenter.BuildHudDecisionSummary(decision, selectedTarget, bootstrap.IsKeyboardTargeting),
                state.Ruleset == CombatRuleset.Roguelite ? decision?.DamageBreakdown : null);
            decisionLabel.text = "左键：" + clickInstruction + "\n" + CompactHud(decisionSummary.Replace("\n", " · "), 15);
            decisionLabel.color = decision != null && !decision.CanSubmit ? FormalUiTheme.Danger : bootstrap.IsKeyboardTargeting ? FormalUiTheme.Cyan : muted;
            weaponLabel.text = hero.MainHand.DisplayName;
            weaponIcon.sprite = Resources.Load<Sprite>(FormalArtRegistry.ItemPath(hero.MainHand.Id));
            if (weaponIcon.sprite == null) throw new KeyNotFoundException("Missing formal item icon: " + hero.MainHand.Id);
            FireSpellDefinition fireOne = bootstrap.FireSpellInSlot(0), fireTwo = bootstrap.FireSpellInSlot(1);
            ArtifactDefinition artifactOne = bootstrap.CurrentArmedArtifact ?? bootstrap.CurrentTrainingRangeArtifact;
            bool rogue = state.Ruleset == CombatRuleset.Roguelite && state.RogueSpells != null;
            for (int slot = 0; slot < RogueRuntimeConstants.SpellSlotCount; slot++) actionButtons["技能" + (slot + 1)].gameObject.SetActive(true);
            if (rogue)
                for (int slot = 0; slot < RogueRuntimeConstants.SpellSlotCount; slot++) RefreshRogueSpellButton("技能" + (slot + 1), state.RogueSpells, slot);
            else
            {
                if (artifactOne != null) RefreshArtifactButton("技能1", artifactOne);
                else if (fireOne != null) RefreshFireSpellButton("技能1", fireOne, hero); else RefreshSkillButton("技能1", hero.SkillOne, hero);
                if (fireTwo != null) RefreshFireSpellButton("技能2", fireTwo, hero); else RefreshSkillButton("技能2", hero.SkillTwo, hero);
                for (int slot = 2; slot < RogueRuntimeConstants.SpellSlotCount; slot++) RefreshEmptySpellButton("技能" + (slot + 1));
            }
            statusLabel.text = "状态 · " + StatusText(hero);
            actionPointLabel.text = "行动点 " + hero.ActionPoints + " / 3";
            RefreshActionPointPips(hero.ActionPoints);
            healthValue.text = RatioText(hero.Health, hero.MaxHealth);
            shieldValue.text = rogue ? hero.Shield + " · 无上限" : RatioText(hero.Shield, hero.MaxShield);
            manaValue.text = RatioText(hero.Mana, hero.MaxMana);
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
                timelineNames[i].text = entry.Order + " " + CompactHud(entry.DisplayName, 7);
                timelineNames[i].color = entry.IsActive ? text : muted;
                timelineDetails[i].text = entry.IsActive ? "行动中" : CompactHud(entry.VitalityText, 5);
                timelineDetails[i].color = entry.IsActive ? faction : muted;
            }
            eventLabel.text = state.EventLog.Count == 0 ? "—" :
                "▶ " + CompactHud(CombatHudTypography.PlayerEventLine(state.EventLog[0]), 28);
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
                quickbarLabels[i].text = definition == null
                    ? (i + 1) + "\n空"
                    : (i + 1) + "\n×" + item.RemainingUses;
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
            label.text = CompactSpellName(skill.DisplayName);
            ConfigurePopulatedSpellCard(button);
            SetCostChips(button, 1, skill.ManaCost);
            int cooldown = hero.Cooldown(skill);
            SetNoticeChip(button, cooldown > 0, cooldown);
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
            label.text = CompactSpellName(armedDefinition?.DisplayName ?? artifact?.DisplayName ?? spell.DisplayName);
            ConfigurePopulatedSpellCard(button);
            SetCostChips(button, spell.ActionPointCost, spell.ManaCost);
            int cooldown = bootstrap.CurrentFireBattle == null ? 0 : bootstrap.CurrentFireBattle.Cooldown(hero.Id, spell.Id);
            SetNoticeChip(button, cooldown > 0, cooldown);
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
            label.text = CompactSpellName(artifact.DisplayName);
            ConfigurePopulatedSpellCard(button);
            SetCostChips(button, artifact.ActionPointCost, 0);
            SetNoticeChip(button, !string.IsNullOrWhiteSpace(artifact.RiskSummary));
        }

        private void RefreshRogueSpellButton(string key, RogueSpellCombatRuntime runtime, int slot)
        {
            if (!actionButtons.TryGetValue(key, out Button button)) return;
            SpellDefinition spell = runtime.DefinitionAtSlot(slot); Text label = button.GetComponentInChildren<Text>();
            if (label == null) return;
            if (spell == null) { RefreshEmptySpellButton(key); return; }
            int cooldown = runtime.CooldownRemaining(spell.DefinitionId);
            label.text = CompactSpellName(spell.DisplayName);
            ConfigurePopulatedSpellCard(button);
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

        private void RefreshEmptySpellButton(string key)
        {
            if (!actionButtons.TryGetValue(key, out Button button)) return;
            Text label = button.transform.Find("文字")?.GetComponent<Text>();
            if (label != null)
            {
                label.text = "空槽";
                label.color = FormalUiTheme.Muted;
                label.rectTransform.sizeDelta = new Vector2(122f, 40f);
            }
            Image icon = button.GetComponentsInChildren<Image>(true).FirstOrDefault(image => image.gameObject.name == "正式图标");
            if (icon != null)
            {
                icon.sprite = actionIcons["skill"];
                icon.color = FormalUiTheme.WithAlpha(FormalUiTheme.Muted, .48f);
            }
            Image resourceBlock = button.transform.Find("术式资源块")?.GetComponent<Image>();
            if (resourceBlock != null) resourceBlock.color = SpellResourceBlockColor(true);
            foreach (string chipName in new[] { "语义_action", "语义_aether", "语义_notice" })
            {
                Transform chip = button.transform.Find(chipName);
                if (chip != null) chip.gameObject.SetActive(false);
            }
        }

        private static string CompactSpellName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "空槽";
            string trimmed = value.Trim();
            return trimmed.Length <= 5 ? trimmed : trimmed.Substring(0, 4) + "…";
        }

        private static void ConfigureSpellSlotLayout(Button button, Image spellIcon, int slot)
        {
            if (button == null) return;
            Image cardSurface = button.targetGraphic as Image ?? button.GetComponent<Image>();
            if (cardSurface != null)
            {
                cardSurface.sprite = null;
                cardSurface.type = Image.Type.Simple;
                Image standardSkin = FormalUiKit.SkinOverlay(cardSurface);
                if (standardSkin != null) standardSkin.gameObject.SetActive(false);
            }
            if (spellIcon != null)
            {
                spellIcon.rectTransform.anchoredPosition = new Vector2(6f, 0f);
                spellIcon.rectTransform.sizeDelta = new Vector2(64f, 64f);
            }
            Text spellLabel = button.transform.Find("文字")?.GetComponent<Text>();
            if (spellLabel == null) return;
            spellLabel.rectTransform.anchorMin = spellLabel.rectTransform.anchorMax = spellLabel.rectTransform.pivot = new Vector2(0f, 1f);
            spellLabel.rectTransform.anchoredPosition = new Vector2(74f, -18f);
            spellLabel.rectTransform.sizeDelta = new Vector2(122f, 40f);
            spellLabel.fontSize = FormalUiTheme.BodyFontSize;
            spellLabel.fontStyle = FontStyle.Normal;
            spellLabel.alignment = TextAnchor.MiddleCenter;
            spellLabel.color = FormalUiTheme.Text;
            FormalUiKit.PreventAutomaticWrapping(spellLabel);

            if (button.transform.Find("术式资源块") == null)
                FormalUiKit.FlatPanel("术式资源块", button.transform,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(202f, -6f), new Vector2(60f, 64f),
                    SpellResourceBlockColor(false));

            Transform keyBadge = button.transform.Find("键位底");
            if (keyBadge == null)
                keyBadge = FormalUiKit.FlatPanel("键位底", button.transform,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(2f, -2f), new Vector2(24f, 24f),
                    FormalUiTheme.Ink).transform;
            Text keyLabel = keyBadge.Find("键位")?.GetComponent<Text>();
            if (keyLabel == null)
                keyLabel = FormalUiKit.Label("键位", (slot + 1).ToString(), keyBadge,
                    new Vector2(0f, 6f), new Vector2(24f, 40f), FormalUiTheme.BodyFontSize,
                    FormalUiTheme.OnInk, TextAnchor.MiddleCenter);
            keyLabel.text = (slot + 1).ToString();
            FormalUiKit.PreventAutomaticWrapping(keyLabel);
            ConfigureSpellCardFrame(button);
        }

        private static void ConfigureSpellCardFrame(Button button)
        {
            if (button == null) return;
            Color frameColor = FormalUiTheme.Rule;
            CreateSpellFrameEdge(button.transform, "术式细框_上", new Vector2(0f, 0f), new Vector2(268f, 2f), frameColor);
            CreateSpellFrameEdge(button.transform, "术式细框_下", new Vector2(0f, -74f), new Vector2(268f, 2f), frameColor);
            CreateSpellFrameEdge(button.transform, "术式细框_左", new Vector2(0f, 0f), new Vector2(2f, 76f), frameColor);
            CreateSpellFrameEdge(button.transform, "术式细框_右", new Vector2(266f, 0f), new Vector2(2f, 76f), frameColor);
        }

        private static void CreateSpellFrameEdge(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            Transform existing = parent.Find(name);
            Image edge = existing?.GetComponent<Image>();
            if (edge == null)
                edge = FormalUiKit.FlatPanel(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), position, size, color).GetComponent<Image>();
            edge.rectTransform.anchoredPosition = position;
            edge.rectTransform.sizeDelta = size;
            edge.color = color;
            edge.raycastTarget = false;
            edge.transform.SetAsLastSibling();
        }

        private static Color SpellResourceBlockColor(bool empty)
        {
            return Color.Lerp(FormalUiTheme.Ink, FormalUiTheme.Magic, empty ? .08f : .18f);
        }

        private static void ConfigurePopulatedSpellCard(Button button)
        {
            if (button == null) return;
            Text label = button.transform.Find("文字")?.GetComponent<Text>();
            if (label != null)
            {
                label.color = FormalUiTheme.Text;
                label.rectTransform.sizeDelta = new Vector2(122f, 40f);
            }
            Image resourceBlock = button.transform.Find("术式资源块")?.GetComponent<Image>();
            if (resourceBlock != null) resourceBlock.color = SpellResourceBlockColor(false);
        }

        private void SetCostChips(Button button, int actionCost, int aetherCost)
        {
            if (button == null) return;
            bool spellSlot = button.name.StartsWith("技能", StringComparison.Ordinal);
            Text actionValue = button.transform.Find("语义_action/数值")?.GetComponent<Text>();
            if (actionValue == null)
                actionValue = FormalUiKit.SemanticChip("action", actionCost.ToString(), button.transform, CostChipPosition(button, false, aetherCost > 0), tooltip,
                    32, spellSlot ? 18 : CombatHudTypography.CostValueFontSize, line);
            actionValue.text = actionCost.ToString();
            actionValue.transform.parent.gameObject.SetActive(true);
            ConfigureCostChip(actionValue.transform.parent, spellSlot, CostChipPosition(button, false, aetherCost > 0), line);

            Transform aetherChip = button.transform.Find("语义_aether");
            if (aetherCost > 0 || spellSlot)
            {
                Text aetherValue = aetherChip?.Find("数值")?.GetComponent<Text>();
                if (aetherValue == null)
                    aetherValue = FormalUiKit.SemanticChip("aether", aetherCost.ToString(), button.transform, CostChipPosition(button, true, true), tooltip,
                        32, spellSlot ? 18 : CombatHudTypography.CostValueFontSize, FormalUiTheme.Magic);
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
                return new Vector2(204f, second ? -38f : -6f);
            return new Vector2(Mathf.Max(4f, width - (second ? 58f : 58f)), -Mathf.Max(24f, height - 24f));
        }

        private static void ConfigureCostChip(Transform chip, bool spellSlot, Vector2 position, Color accent)
        {
            if (chip == null || !spellSlot) return;
            RectTransform chipRect = chip.GetComponent<RectTransform>();
            chipRect.anchoredPosition = position;
            chipRect.sizeDelta = new Vector2(56, 32);
            Image background = chip.GetComponent<Image>() ?? chip.gameObject.AddComponent<Image>();
            background.color = Color.clear;
            background.raycastTarget = false;

            RectTransform iconRect = chip.GetChild(0).GetComponent<RectTransform>();
            if (iconRect != null)
            {
                iconRect.anchoredPosition = Vector2.zero;
                iconRect.sizeDelta = new Vector2(32, 32);
            }
            Text value = chip.Find("数值")?.GetComponent<Text>();
            if (value == null) return;
            Transform valueBackground = chip.Find("费用数值底");
            if (valueBackground != null) valueBackground.gameObject.SetActive(false);
            value.rectTransform.anchoredPosition = new Vector2(32, 4);
            value.rectTransform.sizeDelta = new Vector2(24, 40);
            value.fontSize = FormalUiTheme.BodyFontSize;
            value.fontStyle = FontStyle.Normal;
            value.alignment = TextAnchor.MiddleCenter;
            value.color = FormalUiTheme.OnInk;
        }

        private void SetNoticeChip(Button button, bool visible, int value = -1)
        {
            if (button == null) return;
            bool spellSlot = button.name.StartsWith("技能", StringComparison.Ordinal);
            Text spellLabel = spellSlot ? button.transform.Find("文字")?.GetComponent<Text>() : null;
            if (spellLabel != null)
            {
                spellLabel.rectTransform.sizeDelta = new Vector2(visible ? 84f : 122f, 40f);
                if (visible && spellLabel.text.Length > 3) spellLabel.text = spellLabel.text.Substring(0, 2) + "…";
            }
            Transform noticeChip = button.transform.Find("语义_notice");
            if (visible && noticeChip == null)
            {
                RectTransform rect = button.GetComponent<RectTransform>();
                float width = rect == null ? 80f : rect.sizeDelta.x;
                FormalUiKit.SemanticChip("notice", value >= 0 ? value.ToString() : string.Empty, button.transform,
                    spellSlot ? new Vector2(164f, -24f) : new Vector2(Mathf.Max(4f, width - (value >= 0 ? 56f : 32f)), -4f),
                    tooltip, 32, 16, FormalUiTheme.Amber);
                noticeChip = button.transform.Find("语义_notice");
            }
            if (spellSlot && noticeChip != null) ConfigureSpellNoticeChip(noticeChip);
            Text noticeValue = noticeChip?.Find("数值")?.GetComponent<Text>();
            if (noticeValue != null) noticeValue.text = value >= 0 ? value.ToString() : string.Empty;
            if (noticeChip != null) noticeChip.gameObject.SetActive(visible);
        }

        private static void ConfigureSpellNoticeChip(Transform chip)
        {
            RectTransform chipRect = chip.GetComponent<RectTransform>();
            chipRect.anchoredPosition = new Vector2(164f, -24f);
            chipRect.sizeDelta = new Vector2(32f, 28f);
            Image background = chip.GetComponent<Image>() ?? chip.gameObject.AddComponent<Image>();
            background.color = FormalUiTheme.WithAlpha(FormalUiTheme.Ink, .90f);
            background.raycastTarget = false;
            RectTransform iconRect = chip.GetChild(0).GetComponent<RectTransform>();
            if (iconRect != null)
            {
                iconRect.anchoredPosition = Vector2.zero;
                iconRect.sizeDelta = new Vector2(16f, 16f);
            }
            Text value = chip.Find("数值")?.GetComponent<Text>();
            if (value == null) return;
            value.rectTransform.anchoredPosition = new Vector2(16f, 6f);
            value.rectTransform.sizeDelta = new Vector2(16f, 40f);
            value.fontSize = FormalUiTheme.BodyFontSize;
            value.fontStyle = FontStyle.Normal;
            value.alignment = TextAnchor.MiddleCenter;
            value.color = FormalUiTheme.Amber;
        }

        private void CreateOutcomeOverlay()
        {
            outcomeOverlay = FormalUiKit.LayoutPanel("战斗结果", root.transform, "combat.outcome", FormalUiTheme.WithAlpha(FormalUiTheme.SurfaceRaised, .99f));
            outcomeTitle = Label("结果标题", outcomeOverlay.transform, new Vector2(40, -34), new Vector2(640, 58), 36, text, TextAnchor.MiddleCenter);
            outcomeDetail = Label("结果说明", outcomeOverlay.transform, new Vector2(40, -102), new Vector2(640, 100), 16, muted, TextAnchor.UpperCenter);
            outcomeRestartButton = Button(outcomeOverlay.transform, "结果重开", new Vector2(60, -180), new Vector2(280, 64), "重新挑战", FormalUiTheme.Interactive, FormalUiTheme.ButtonFontSize, FormalUiButtonTone.Primary);
            outcomeRestartButton.onClick.AddListener(bootstrap.RequestTacticalRestart);
            outcomeBackButton = Button(outcomeOverlay.transform, "结果返回", new Vector2(380, -180), new Vector2(280, 64), "返回入口", FormalUiTheme.Interactive, FormalUiTheme.ButtonFontSize, FormalUiButtonTone.Warning);
            outcomeBackButton.onClick.AddListener(bootstrap.ReturnToDeveloperMenu);
            BindTooltip(outcomeOverlay, BuildOutcomeTooltip);
            BindTooltip(outcomeRestartButton.gameObject, () => new FormalTooltipContent("重新挑战", "这场战斗会从头开始。", line));
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
            return new FormalTooltipContent("这一招会怎样", CombatInformationPresenter.BuildTargetDetails(bootstrap?.CurrentActionPreview, target, intent,
                bootstrap?.CurrentState?.Ruleset == CombatRuleset.Roguelite), line);
        }

        private bool HandleSpellShortcutInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return false;
            int slot = keyboard.digit1Key.wasPressedThisFrame ? 0 :
                keyboard.digit2Key.wasPressedThisFrame ? 1 :
                keyboard.digit3Key.wasPressedThisFrame ? 2 :
                keyboard.digit4Key.wasPressedThisFrame ? 3 :
                keyboard.digit5Key.wasPressedThisFrame ? 4 :
                keyboard.digit6Key.wasPressedThisFrame ? 5 :
                keyboard.digit7Key.wasPressedThisFrame ? 6 :
                keyboard.digit8Key.wasPressedThisFrame ? 7 : -1;
            if (slot < 0) return false;
            bootstrap.TrySelectSpellShortcut(slot);
            return true;
        }

        private string SelectedModeDisplayName(CombatState state, UnitState hero)
        {
            int slot = SkillSlot(bootstrap.SelectedAction);
            if (slot < 0) return bootstrap.SelectedAction;
            if (state.Ruleset == CombatRuleset.Roguelite && state.RogueSpells != null)
            {
                SpellDefinition rogue = state.RogueSpells.DefinitionAtSlot(slot);
                return rogue == null ? "术式 " + (slot + 1) + "（空）" :
                    "术式 " + (slot + 1) + " · " + rogue.DisplayName;
            }
            ArtifactDefinition artifact = slot == 0 ? (bootstrap.CurrentArmedArtifact ?? bootstrap.CurrentTrainingRangeArtifact) : null;
            if (artifact != null) return "道具 · " + artifact.DisplayName;
            FireSpellDefinition fire = bootstrap.FireSpellInSlot(slot);
            if (fire != null) return "术式 " + (slot + 1) + " · " + fire.DisplayName;
            SkillDefinition skill = slot == 0 ? hero?.SkillOne : slot == 1 ? hero?.SkillTwo : null;
            return skill == null ? "术式 " + (slot + 1) + "（空）" :
                "术式 " + (slot + 1) + " · " + skill.DisplayName;
        }

        public static string PrimaryClickInstruction(string action)
        {
            if (action == "移动") return "双击空地：快捷移动";
            if (action == "攻击") return "左键敌人：攻击";
            if (action == "搜刮") return "左键战利品：搜刮";
            if (action == "互动") return "左键相邻目标：互动";
            if (SkillSlot(action) >= 0) return "左键合法目标：施放术式";
            return "左键目标：执行当前行动";
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
            return new FormalTooltipContent("你的情况", details, FormalUiTheme.Safe);
        }

        private FormalTooltipContent BuildLogTooltip()
        {
            CombatState state = bootstrap?.CurrentState;
            string body = state == null || state.EventLog.Count == 0 ? "暂无记录" :
                string.Join("\n", state.EventLog.Take(5).Select(CombatHudTypography.PlayerEventLine));
            return new FormalTooltipContent("刚刚发生", body, FormalUiTheme.Amber);
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
            return new FormalTooltipContent("刚刚发生", summary?.RecentEventsText ?? "暂时没有新动静", FormalUiTheme.Amber);
        }

        private static string RatioText(int current, int maximum)
        {
            int safeMaximum = Math.Max(1, maximum);
            int percent = Mathf.RoundToInt(Mathf.Clamp01(current / (float)safeMaximum) * 100f);
            return current + " / " + maximum + " · " + percent + "%";
        }

        private void BindTooltip(GameObject target, Func<FormalTooltipContent> provider)
        {
            if (target == null) return;
            FormalHoverTooltipTrigger trigger = target.GetComponent<FormalHoverTooltipTrigger>() ?? target.AddComponent<FormalHoverTooltipTrigger>();
            trigger.Configure(tooltip, provider);
        }

        private Image ResourceBar(Transform parent, string title, Vector2 position, Color color, out Text valueLabel)
        {
            Label(title, parent, position, new Vector2(200, 40), FormalUiTheme.BodyFontSize, muted, TextAnchor.MiddleLeft);
            valueLabel = Label(title + "数值", parent, position, new Vector2(374, 40), CombatHudTypography.ResourceValueFontSize,
                text, CombatHudTypography.ResourceValueAlignment);
            FormalUiKit.ConfigureNumericLabel(valueLabel);
            GameObject track = Panel(title + "轨道", parent, new Vector2(0, 1), new Vector2(0, 1), position + new Vector2(0, -32), new Vector2(390, 24), FormalUiTheme.ResourceTrack);
            FormalUiKit.ApplySkin(track.GetComponent<Image>(), "bar_track", FormalUiTheme.ResourceTrack);
            GameObject fill = FormalUiKit.FlatPanel(title + "填充", track.transform,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero, color);
            // The formal track skin is the bottom frame; the semantic fill sits above it with a
            // four-pixel inset. Some legacy 16px skins paint a dark center even with fillCenter off.
            // Keeping the fill above the skin prevents that center from hiding the resource color.
            RectTransform rect = fill.GetComponent<RectTransform>();
            rect.offsetMin = new Vector2(4f, 4f);
            rect.offsetMax = new Vector2(-4f, -4f);
            rect.anchorMax = new Vector2(1, 1);
            for (int index = 1; index <= 3; index++)
            {
                float fraction = index / 4f;
                GameObject tick = FormalUiKit.FlatPanel(title + "比例刻度_" + index, track.transform,
                    new Vector2(fraction, 0f), new Vector2(fraction, 1f), Vector2.zero, new Vector2(2f, -6f),
                    FormalUiTheme.WithAlpha(FormalUiTheme.Ink, .58f));
                tick.GetComponent<RectTransform>().pivot = new Vector2(.5f, .5f);
            }
            GameObject marker = FormalUiKit.FlatPanel(title + "变化落点", track.transform,
                new Vector2(1f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(8f, -6f), Color.clear);
            marker.GetComponent<RectTransform>().pivot = new Vector2(.5f, .5f);
            Image fillImage = fill.GetComponent<Image>();
            resourceChangeMarkers[fillImage] = marker.GetComponent<Image>();
            return fillImage;
        }

        private void SetBar(Image fill, float value, ref float displayed)
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(displayed, value)) return;
            float previous = displayed;
            RectTransform rect = fill.rectTransform;
            rect.DOKill();
            UiMotionProfile motion = UiMotionProfile.FromIntensity(bootstrap == null ? 1f : bootstrap.UiPreferences.AnimationIntensity);
            if (motion.IsImmediate) rect.anchorMax = new Vector2(value, 1f);
            else DOTween.To(() => rect.anchorMax.x, next => rect.anchorMax = new Vector2(next, 1f), value, motion.QuickDuration).SetEase(FormalUiMotionTokens.FeedbackEase).SetUpdate(true);
            if (previous >= 0f && resourceChangeMarkers.TryGetValue(fill, out Image marker) && marker != null)
            {
                marker.DOKill();
                float markerPosition = Mathf.Clamp(value, .02f, .98f);
                RectTransform markerRect = marker.rectTransform;
                markerRect.anchorMin = new Vector2(markerPosition, 0f);
                markerRect.anchorMax = new Vector2(markerPosition, 1f);
                Color feedbackColor = value < previous ? FormalUiTheme.Danger : FormalUiTheme.Safe;
                marker.color = FormalUiTheme.WithAlpha(feedbackColor, motion.IsImmediate ? 0f : .95f);
                if (!motion.IsImmediate)
                    DOTween.To(() => marker.color, color => marker.color = color,
                            FormalUiTheme.WithAlpha(feedbackColor, 0f), Mathf.Max(.2f, motion.StandardDuration * 1.5f))
                        .SetEase(FormalUiMotionTokens.FeedbackEase).SetUpdate(true).SetTarget(marker);
            }
            displayed = value;
        }

        private void RefreshActionPointPips(int current)
        {
            current = Mathf.Clamp(current, 0, actionPointPips.Length);
            UiMotionProfile motion = UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity);
            for (int i = 0; i < actionPointPipGroups.Length; i++)
            {
                CanvasGroup group = actionPointPipGroups[i];
                if (group == null) continue;
                float target = i < current ? 1f : .16f;
                group.DOKill();
                bool changedPip = displayedActionPoints >= 0 &&
                    ((i < current) != (i < displayedActionPoints));
                if (motion.IsImmediate || !changedPip)
                {
                    group.alpha = target;
                    continue;
                }

                group.alpha = current > displayedActionPoints ? .16f : 1f;
                DOTween.To(() => group.alpha, value => group.alpha = value, target, motion.QuickDuration)
                    .SetEase(FormalUiMotionTokens.FeedbackEase).SetUpdate(true).SetTarget(group);
            }
            displayedActionPoints = current;
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
                Text spellLabel = parsedSlot < 0 ? null : pair.Value.transform.Find("文字")?.GetComponent<Text>();
                if (available && spellLabel != null && spellLabel.text == "空槽") { available = false; reason = "术式槽为空"; }
                pair.Value.GetComponent<UiButtonFeedback>()?.SetAvailability(available, reason);
                ApplySpellAvailabilityVisual(pair.Value, available, reason, pair.Key == bootstrap.SelectedAction);
            }
            endTurnButton?.GetComponent<UiButtonFeedback>()?.SetAvailability(heroTurn, heroTurn ? string.Empty : "等待敌方行动");
        }

        private static int SkillSlot(string action)
        { return action != null && action.StartsWith("技能", StringComparison.Ordinal) && int.TryParse(action.Substring(2), out int oneBased) && oneBased >= 1 && oneBased <= RogueRuntimeConstants.SpellSlotCount ? oneBased - 1 : -1; }

        private static void ApplySpellAvailabilityVisual(Button button, bool available, string reason, bool selected)
        {
            if (button == null || !button.name.StartsWith("技能", StringComparison.Ordinal)) return;
            bool shortage = !string.IsNullOrEmpty(reason) && reason.Contains("不足", StringComparison.Ordinal);
            bool cooldown = !string.IsNullOrEmpty(reason) && reason.Contains("冷却", StringComparison.Ordinal);
            bool empty = !string.IsNullOrEmpty(reason) && reason.Contains("空", StringComparison.Ordinal);
            Text label = button.transform.Find("文字")?.GetComponent<Text>();
            if (label != null) label.color = available || selected ? FormalUiTheme.Text : FormalUiTheme.Muted;
            Image resourceBlock = button.transform.Find("术式资源块")?.GetComponent<Image>();
            if (resourceBlock != null)
            {
                Color baseColor = SpellResourceBlockColor(empty);
                resourceBlock.color = shortage ? Color.Lerp(baseColor, FormalUiTheme.Danger, .26f) :
                    cooldown ? Color.Lerp(baseColor, FormalUiTheme.Amber, .22f) : baseColor;
            }

            Text actionValue = button.transform.Find("语义_action/数值")?.GetComponent<Text>();
            Text aetherValue = button.transform.Find("语义_aether/数值")?.GetComponent<Text>();
            if (actionValue != null)
                actionValue.color = !available && reason.StartsWith("行动点不足", StringComparison.Ordinal) ? FormalUiTheme.Danger : FormalUiTheme.OnInk;
            if (aetherValue != null)
                aetherValue.color = !available && shortage && !reason.StartsWith("行动点不足", StringComparison.Ordinal) ? FormalUiTheme.Danger : FormalUiTheme.OnInk;
        }

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

        private Button Button(Transform parent, string name, Vector2 position, Vector2 size, string title, Color color, int fontSize = FormalUiTheme.ButtonFontSize, FormalUiButtonTone tone = FormalUiButtonTone.Primary)
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
