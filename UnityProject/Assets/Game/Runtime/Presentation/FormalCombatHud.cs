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
        private Text actionPointBadgeValue;
        private Text headerResourceLabel;
        private Text weaponLabel;
        private Text statusLabel;
        private GameObject heroModule;
        private GameObject heroFront;
        private GameObject heroBack;
        private Text heroStatusDetails;
        private Text heroPassiveDetails;
        private Button heroFlipButton;
        private bool heroShowingBack;
        private bool heroFlipAnimating;
        private GameObject timelineModule;
        private GameObject timelineFront;
        private GameObject timelineBack;
        private Text historyText;
        private ScrollRect historyScroll;
        private Button timelineFlipButton;
        private bool timelineShowingBack;
        private bool timelineFlipAnimating;
        private string displayedHistoryText;
        private Text timelineGlobalValue;
        private Image timelineGlobalFill;
        private readonly Text[] timelineNames = new Text[5];
        private readonly Text[] timelineSpeeds = new Text[5];
        private readonly Text[] timelineDetails = new Text[5];
        private readonly Image[] timelineNodes = new Image[5];
        private readonly Image[] timelineRows = new Image[5];
        private readonly Image[] timelineRetained = new Image[5];
        private readonly Image[] timelineRemoved = new Image[5];
        private readonly Image[] timelineCurrentEndpoint = new Image[5];
        private readonly Image[] timelinePreviewEndpoint = new Image[5];
        private readonly Image[] timelineOrderArrows = new Image[5];
        private readonly TimelineRowInteraction[] timelineInteractions = new TimelineRowInteraction[5];
        // Values belong to a unit, rather than to a visual row. The order can change when a
        // turn is earned, so retaining them by row would make the most important transitions
        // jump as the entries swap places.
        private readonly Dictionary<string, float> displayedTimelineValues = new Dictionary<string, float>();
        private readonly Dictionary<string, float> displayedTimelineWidths = new Dictionary<string, float>();
        private readonly Dictionary<string, int> timelineSlotsByUnit = new Dictionary<string, int>();
        private float displayedTimelineGlobal = -1f;
        private CombatState displayedTimelineState;
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
        private readonly Text[] quickbarKeys = new Text[RogueRuntimeConstants.ItemQuickbarSize];
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
        private int displayedActionVersion = -1;
        private bool combatEntryQueued;
        public int RefreshCount { get; private set; }

        public void Initialize(ICombatHudHost source)
        {
            bootstrap = source;
            bootstrap.UiPresentationVersions.Changed += OnPresentationChanged;
            LoadActionIcons();
            EnsureUi();
        }

        public void QueueCombatEntry() => combatEntryQueued = true;

        private void OnPresentationChanged(UiPresentationChange change)
        {
            if (change.Area == UiPresentationArea.Combat || change.Area == UiPresentationArea.Flow) refreshDirty = true;
        }

        private void Update()
        {
            if (root == null || bootstrap == null) return;
            bool visible = bootstrap.IsDeveloperCombatActive || bootstrap.IsCombatOutcomeVisible;
            if (root.activeSelf != visible) { root.SetActive(visible); refreshDirty = true; }
            if (!visible || bootstrap.CurrentState == null)
            {
                if (wasVisible)
                {
                    ResetCardFlips();
                    ResetTimelineMotionState();
                }
                wasVisible = false;
                hasPresentedModel = false;
                return;
            }
            if (combatEntryQueued)
            {
                combatEntryQueued = false;
                PlayCombatEntry();
            }
            if (!wasVisible)
            {
                wasVisible = true;
                Button defaultButton = outcomeRestartButton;
                if (!bootstrap.IsCombatOutcomeVisible) defaultButton = endTurnButton;
                if (defaultButton != null) RuntimeUiEventSystem.Select(defaultButton.gameObject);
            }
            bool handledCardFlip = bootstrap.IsDeveloperCombatActive && !bootstrap.IsInteractionModalOpen && HandleCardFlipShortcut();
            bool handledShortcut = !handledCardFlip && bootstrap.IsDeveloperCombatActive && !bootstrap.IsInteractionModalOpen && HandleSpellShortcutInput();
            bool handledTargetInput = !handledCardFlip && !handledShortcut && bootstrap.IsDeveloperCombatActive && !bootstrap.IsInteractionModalOpen && HandleTargetNavigationInput();
            if (!handledCardFlip && !handledShortcut && !handledTargetInput && bootstrap.IsDeveloperCombatActive && !bootstrap.IsInteractionModalOpen && RuntimeUiEventSystem.CancelPressedThisFrame()) bootstrap.CancelCombatSelectionOrRequestLeave();
            int actionVersion = (bootstrap as ICombatActionPresentationHost)?.CombatActionPresentationVersion ?? 0;
            bool actionChanged = actionVersion != displayedActionVersion;
            if (actionChanged) refreshDirty = true;
            if (!refreshDirty) return;
            refreshDirty = false;
            CombatHudPresentationModel nextModel = CombatHudPresentationModel.From(bootstrap.CurrentState, bootstrap.SelectedAction, bootstrap.SelectedTargetId, bootstrap.IsCombatOutcomeVisible);
            bool targetingChanged = presentedTargeting != bootstrap.IsKeyboardTargeting || presentedTargetPosition != bootstrap.KeyboardTargetPosition;
            if (hasPresentedModel && presentedModel.Equals(nextModel) && !targetingChanged && !actionChanged) return;
            displayedActionVersion = actionVersion;
            presentedModel = nextModel;
            presentedTargeting = bootstrap.IsKeyboardTargeting;
            presentedTargetPosition = bootstrap.KeyboardTargetPosition;
            hasPresentedModel = true;
            Refresh();
        }

        private void PlayCombatEntry()
        {
            if (root == null) return;
            UiMotionProfile motion = UiMotionProfile.FromIntensity(bootstrap == null ? 1f : bootstrap.UiPreferences.AnimationIntensity);
            CanvasGroup group = root.GetComponent<CanvasGroup>();
            // Unity can retain a managed wrapper for a component destroyed during a UI rebuild;
            // use Unity's null check rather than ?? so that wrapper is not reused.
            if (group == null) group = root.AddComponent<CanvasGroup>();
            if (group == null) return;
            group.DOKill();
            if (motion.IsImmediate)
            {
                group.alpha = 1f;
                return;
            }
            group.alpha = 0f;
            DOTween.To(() => group.alpha, value => group.alpha = value, 1f, motion.StandardDuration)
                .SetDelay(motion.QuickDuration * .5f).SetEase(FormalUiMotionTokens.StandardEase).SetUpdate(true).SetTarget(group);
        }

        private void EnsureUi()
        {
            if (root != null) return;
            canvas = FormalUiKit.CanvasRoot("正式战斗HUD", UiLayoutContract.CombatSortingOrder);
            root = canvas.gameObject;
            tooltip = root.AddComponent<FormalHoverTooltip>();
            tooltip.Initialize(canvas);

            GameObject top = FormalUiKit.LayoutPanel("战斗抬头", root.transform, "combat.header", FormalUiTheme.SurfaceRaised);
            ConfigureOutlinedPanel(top, FormalUiTheme.Panel, FormalUiTheme.Rule);
            headerResourceLabel = Label("战斗资源", top.transform, new Vector2(16, -8), new Vector2(1400, 40), FormalUiTheme.BodyFontSize, text, TextAnchor.MiddleLeft);
            FormalUiKit.PreventAutomaticWrapping(headerResourceLabel);

            GameObject side = FormalUiKit.LayoutPanel("战斗信息", root.transform, "combat.rightConsole", Color.clear);
            Image sideSurface = side.GetComponent<Image>();
            sideSurface.color = Color.clear;
            sideSurface.raycastTarget = false;
            Image sideSkin = FormalUiKit.SkinOverlay(sideSurface);
            if (sideSkin != null) sideSkin.gameObject.SetActive(false);
            heroModule = ConsoleModule("英雄概况", side.transform, "combat.hero");
            heroFront = CardFace("英雄概况正面", heroModule.transform);
            heroBack = CardFace("英雄概况背面", heroModule.transform);
            Label("英雄", heroFront.transform, new Vector2(16, -4), new Vector2(72, 32), FormalUiTheme.BodyFontSize, text, TextAnchor.MiddleLeft);
            weaponLabel = Label("主手装备", heroFront.transform, new Vector2(96, -4), new Vector2(182, 32), FormalUiTheme.BodyFontSize, muted, TextAnchor.MiddleLeft);
            FormalUiKit.PreventAutomaticWrapping(weaponLabel);
            weaponIcon = FormalUiKit.IconSlot("主手装备图标", heroFront.transform, null, Vector2.zero);
            weaponIcon.rectTransform.anchorMin = weaponIcon.rectTransform.anchorMax = new Vector2(0, 1);
            weaponIcon.rectTransform.pivot = new Vector2(0, 1); weaponIcon.rectTransform.anchoredPosition = new Vector2(286, -4);
            statusLabel = Label("状态", heroFront.transform, new Vector2(16, -40), new Vector2(384, 32), FormalUiTheme.BodyFontSize, muted, TextAnchor.MiddleLeft);
            statusLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            statusLabel.verticalOverflow = VerticalWrapMode.Truncate;
            healthFill = ResourceBar(heroFront.transform, "生命", new Vector2(16, -76), FormalUiTheme.Health, out healthValue);
            shieldFill = ResourceBar(heroFront.transform, "护盾", new Vector2(16, -128), FormalUiTheme.Shield, out shieldValue);
            manaFill = ResourceBar(heroFront.transform, "个人魔力", new Vector2(16, -180), FormalUiTheme.Magic, out manaValue);
            BuildHeroBack();
            heroFlipButton = Button(heroModule.transform, "英雄概况翻面", new Vector2(296, -3), new Vector2(104, 36), "状态", FormalUiTheme.Panel, FormalUiTheme.BodyFontSize, FormalUiButtonTone.Neutral);
            ConfigureCompactFrame(heroFlipButton);
            heroFlipButton.onClick.AddListener(ToggleHeroCard);
            Label("英雄翻面提示", heroBack.transform, new Vector2(192, -4), new Vector2(96, 32), 18, muted, TextAnchor.MiddleRight).text = "Tab 双翻";
            BindTooltip(heroModule, BuildHeroTooltip);

            timelineModule = ConsoleModule("行动序列模块", side.transform, "combat.timeline");
            timelineFront = CardFace("行动值正面", timelineModule.transform);
            timelineBack = CardFace("行动历史背面", timelineModule.transform);
            Label("行动值", timelineFront.transform, new Vector2(16, -4), new Vector2(136, 40), FormalUiTheme.BodyFontSize, text, TextAnchor.MiddleLeft);
            timelineGlobalValue = Label("全局行动值", timelineFront.transform, new Vector2(156, -4), new Vector2(150, 40),
                CombatHudTypography.TimelineDetailFontSize, muted, TextAnchor.MiddleRight);
            FormalUiKit.ConfigureNumericLabel(timelineGlobalValue);
            Label("行动值零点", timelineFront.transform, new Vector2(16, -40), new Vector2(72, 28),
                CombatHudTypography.TimelineDetailFontSize, muted, TextAnchor.MiddleLeft).text = "0";
            Label("行动值阈值", timelineFront.transform, new Vector2(160, -40), new Vector2(96, 28),
                CombatHudTypography.TimelineDetailFontSize, FormalUiTheme.Cyan, TextAnchor.MiddleCenter).text = "100";
            Label("行动值上界", timelineFront.transform, new Vector2(328, -40), new Vector2(72, 28),
                CombatHudTypography.TimelineDetailFontSize, muted, TextAnchor.MiddleRight).text = "199";
            GameObject globalTrack = Panel("全局行动值轨道", timelineFront.transform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(16, -70), new Vector2(384, 20), FormalUiTheme.ResourceTrack);
            FormalUiKit.ApplySkin(globalTrack.GetComponent<Image>(), "bar_track", FormalUiTheme.ResourceTrack);
            GameObject globalFill = FormalUiKit.FlatPanel("全局行动值填充", globalTrack.transform,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero, FormalUiTheme.Cyan);
            globalFill.GetComponent<RectTransform>().offsetMin = new Vector2(4, 4);
            globalFill.GetComponent<RectTransform>().offsetMax = new Vector2(-4, -4);
            timelineGlobalFill = globalFill.GetComponent<Image>();
            Line(globalTrack.transform, new Vector2(193, -2), new Vector2(2, 16), FormalUiTheme.WithAlpha(text, .72f));
            for (int i = 0; i < timelineNames.Length; i++) CreateTimelineSlot(i);
            BuildTimelineBack();
            timelineFlipButton = Button(timelineModule.transform, "行动值翻面", new Vector2(296, -3), new Vector2(104, 36), "记录", FormalUiTheme.Panel, FormalUiTheme.BodyFontSize, FormalUiButtonTone.Neutral);
            ConfigureCompactFrame(timelineFlipButton);
            timelineFlipButton.onClick.AddListener(ToggleTimelineCard);
            Label("行动值翻面提示", timelineBack.transform, new Vector2(184, -4), new Vector2(104, 32), 18, muted, TextAnchor.MiddleRight).text = "最新在前";
            heroBack.SetActive(false);
            timelineBack.SetActive(false);

            GameObject apBadge = FormalUiKit.LayoutPanel("行动点徽章", root.transform, "combat.actionPointBadge", Color.clear);
            GameObject outerDiamond = Panel("行动点外菱形", apBadge.transform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(48, -48), new Vector2(64, 64), FormalUiTheme.Cyan);
            outerDiamond.transform.localRotation = Quaternion.Euler(0, 0, -45);
            GameObject innerDiamond = Panel("行动点内菱形", apBadge.transform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(48, -48), new Vector2(48, 48), FormalUiTheme.Ink);
            innerDiamond.transform.localRotation = Quaternion.Euler(0, 0, -45);
            Text apTitle = Label("行动点标题", apBadge.transform, new Vector2(0, -12), new Vector2(96, 28),
                CombatHudTypography.TimelineDetailFontSize, FormalUiTheme.OnInk, TextAnchor.MiddleCenter);
            apTitle.text = "AP";
            SetBadgeTextRect(apTitle, new Vector2(0, -10), new Vector2(96, 28));
            actionPointBadgeValue = Label("行动点数值", apBadge.transform, new Vector2(0, -40), new Vector2(96, 36),
                FormalUiTheme.BodyFontSize, FormalUiTheme.OnInk, TextAnchor.MiddleCenter);
            FormalUiKit.ConfigureNumericLabel(actionPointBadgeValue);
            SetBadgeTextRect(actionPointBadgeValue, new Vector2(0, -42), new Vector2(96, 36));
            BindTooltip(apBadge, () => new FormalTooltipContent("行动点", "大号数字是当前值。\n本回合基础上限为 " + CombatResolver.HeroActionPointsPerTurn + "；额外行动点可以超过基础上限。", FormalUiTheme.Cyan));

            GameObject bottom = FormalUiKit.LayoutPanel("战术指令", root.transform, "combat.commands", ink);
            GameObject spellGroup = Panel("术式组", bottom.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -2), new Vector2(1292, 196), panel);
            GameObject itemGroup = Panel("战术栏", bottom.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(1308, -14), new Vector2(360, 172), panel);
            for (int slot = 0; slot < RogueRuntimeConstants.SpellSlotCount; slot++)
            {
                string action = "技能" + (slot + 1); int captured = slot;
                Button button = Button(spellGroup.transform, action,
                    new Vector2(16 + (slot % 4) * 316, -18 - (slot / 4) * 82),
                    new Vector2(308, 74), "空槽", FormalUiTheme.Interactive, FormalUiTheme.ButtonFontSize);
                Image spellIcon = FormalUiKit.IconSlot("正式图标", button.transform, actionIcons[slot == 1 ? "skill_two" : "skill"], new Vector2(4, 0));
                ConfigureSpellSlotLayout(button, spellIcon, slot);
                button.onClick.AddListener(() => bootstrap.SelectHudAction(action)); actionButtons.Add(action, button);
                BindTooltip(button.gameObject, () => BuildActionTooltip("技能" + (captured + 1)));
            }
            endTurnButton = Button(bottom.transform, "结束行动", new Vector2(1676, -30), new Vector2(204, 140), "结束回合\n行动点会清空", FormalUiTheme.Interactive, FormalUiTheme.ButtonFontSize, FormalUiButtonTone.Primary);
            endTurnButton.onClick.AddListener(() => bootstrap.EndHeroTurn());
            BindTooltip(endTurnButton.gameObject, () => new FormalTooltipContent("结束回合", "剩余行动点会清空，然后轮到敌方。", line));
            Button inventoryButton = Button(top.transform, "打开背包", new Vector2(1484, -4), new Vector2(120, 48), "背包",
                FormalUiTheme.Panel, FormalUiTheme.ButtonFontSize, FormalUiButtonTone.Neutral);
            ConfigureCompactFrame(inventoryButton);
            inventoryButton.onClick.AddListener(() => bootstrap.OpenCombatInventoryPanel());
            BindTooltip(inventoryButton.gameObject, () => new FormalTooltipContent("战斗背包", "消耗本回合的背包开启次数，整理装备或取用战术物品。", FormalUiTheme.Cyan));
            restartButton = Button(top.transform, "战术重开", new Vector2(1612, -4), new Vector2(120, 48), "重开", FormalUiTheme.Panel, FormalUiTheme.ButtonFontSize, FormalUiButtonTone.Warning);
            ConfigureCompactFrame(restartButton);
            restartButton.onClick.AddListener(bootstrap.RequestTacticalRestart);
            BindTooltip(restartButton.gameObject, () => new FormalTooltipContent("重新开始", "这场战斗会从头开始，用掉的道具也会恢复。", FormalUiTheme.Amber));
            leaveButton = Button(top.transform, "离开战斗", new Vector2(1740, -4), new Vector2(120, 48), "离开", FormalUiTheme.Danger, FormalUiTheme.ButtonFontSize, FormalUiButtonTone.Dangerous);
            ConfigureCompactFrame(leaveButton);
            leaveButton.transform.Find("文字").GetComponent<Text>().color = FormalUiTheme.OnInk;
            leaveButton.onClick.AddListener(bootstrap.RequestLeaveCombat);
            BindTooltip(leaveButton.gameObject, () => new FormalTooltipContent("离开战斗", "回到地图。这场战斗的收获和损失都不会保留。", FormalUiTheme.Danger));
            for (int i = 0; i < quickbarLabels.Length; i++)
            {
                int slot = i;
                Button quick = Button(itemGroup.transform, "快捷栏" + i, new Vector2(16 + (i % 2) * 164, -24 - (i / 2) * 62), new Vector2(156, 54), "", FormalUiTheme.Surface, FormalUiTheme.ButtonFontSize, FormalUiButtonTone.Neutral);
                quickbarLabels[i] = quick.GetComponentInChildren<Text>();
                ConfigureCompactFrame(quick);
                quickbarIcons[i] = FormalUiKit.IconSlot("快捷栏正式图标", quick.transform, null, new Vector2(4, 0));
                quickbarKeys[i] = FormalUiKit.Label("槽位", (i + 1).ToString(), quick.transform,
                    new Vector2(130, -2), new Vector2(18, 24), FormalUiTheme.BodyFontSize, muted, TextAnchor.MiddleCenter);
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

        private static void ConfigureCompactFrame(Button button)
        {
            Image surface = button.targetGraphic as Image ?? button.GetComponent<Image>();
            surface.sprite = null;
            surface.type = Image.Type.Simple;
            Image skin = FormalUiKit.SkinOverlay(surface);
            if (skin != null) skin.gameObject.SetActive(false);
            FormalUiKit.ThinFrame(button.transform, button.GetComponent<RectTransform>().sizeDelta, FormalUiTheme.Rule);
        }

        private static void ConfigureOutlinedPanel(GameObject target, Color background, Color stroke)
        {
            Image surface = target.GetComponent<Image>();
            surface.sprite = null;
            surface.type = Image.Type.Simple;
            surface.color = background;
            Image skin = FormalUiKit.SkinOverlay(surface);
            if (skin != null) skin.gameObject.SetActive(false);
            FormalUiKit.ThinFrame(target.transform, target.GetComponent<RectTransform>().sizeDelta, stroke);
        }

        private static void ConfigureCommandRow(Button button, bool hasIcon)
        {
            ConfigureCompactFrame(button);
            float width = button.GetComponent<RectTransform>().sizeDelta.x;
            Text label = button.transform.Find("文字").GetComponent<Text>();
            SetCompactTextRect(label, new Vector2(hasIcon ? 48 : 8, -6), new Vector2(width - (hasIcon ? 120 : 80), 42));
            label.alignment = TextAnchor.MiddleLeft;
            if (hasIcon)
            {
                RectTransform icon = button.transform.Find("正式图标").GetComponent<RectTransform>();
                icon.anchoredPosition = new Vector2(8, 0);
                icon.sizeDelta = new Vector2(32, 32);
            }
        }

        private static void SetCompactTextRect(Text label, Vector2 position, Vector2 size)
        {
            RectTransform rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            label.fontSize = FormalUiTheme.BodyFontSize;
            label.fontStyle = FontStyle.Normal;
            label.resizeTextForBestFit = false;
            FormalUiKit.PreventAutomaticWrapping(label);
        }

        private void RefreshQuickbarReadout(int slot, bool occupied, int charges)
        {
            Text label = quickbarLabels[slot];
            quickbarKeys[slot].gameObject.SetActive(occupied);
            label.text = occupied ? charges.ToString() : (slot + 1) + " 空";
            SetCompactTextRect(label, occupied ? new Vector2(40, -26) : new Vector2(4, -6),
                occupied ? new Vector2(32, 24) : new Vector2(68, 42));
            label.alignment = TextAnchor.MiddleCenter;
        }

        private static GameObject ConsoleModule(string name, Transform parent, string layoutId)
        {
            GameObject module = FormalUiKit.FlatPanel(name, parent, Vector2.zero, Vector2.zero,
                Vector2.zero, Vector2.zero, panel);
            RectTransform rect = module.GetComponent<RectTransform>();
            FormalUiKit.ApplyLayout(rect, layoutId);
            ConfigureOutlinedPanel(module, FormalUiTheme.SurfaceRaised, FormalUiTheme.Rule);
            module.GetComponent<Image>().raycastTarget = true;
            return module;
        }

        private static GameObject CardFace(string name, Transform parent)
        {
            GameObject face = FormalUiKit.Create(name, parent);
            RectTransform rect = face.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return face;
        }

        private void BuildHeroBack()
        {
            Label("状态与被动", heroBack.transform, new Vector2(16, -4), new Vector2(192, 40), FormalUiTheme.BodyFontSize, text, TextAnchor.MiddleLeft);
            Label("玩家状态", heroBack.transform, new Vector2(16, -42), new Vector2(176, 32), FormalUiTheme.BodyFontSize, FormalUiTheme.Cyan, TextAnchor.MiddleLeft);
            Label("被动效果", heroBack.transform, new Vector2(224, -42), new Vector2(176, 32), FormalUiTheme.BodyFontSize, FormalUiTheme.Amber, TextAnchor.MiddleLeft);
            Line(heroBack.transform, new Vector2(208, -46), new Vector2(2, 272), FormalUiTheme.WithAlpha(line, .72f));
            heroStatusDetails = Label("玩家状态内容", heroBack.transform, new Vector2(16, -76), new Vector2(176, 242), FormalUiTheme.BodyFontSize, text, TextAnchor.UpperLeft);
            heroPassiveDetails = Label("被动效果内容", heroBack.transform, new Vector2(224, -76), new Vector2(176, 242), 20, text, TextAnchor.UpperLeft);
            heroStatusDetails.lineSpacing = .88f;
            heroPassiveDetails.lineSpacing = .88f;
            heroStatusDetails.supportRichText = true;
            heroPassiveDetails.supportRichText = true;
        }

        private void BuildTimelineBack()
        {
            Label("行动历史", timelineBack.transform, new Vector2(16, -4), new Vector2(190, 40), FormalUiTheme.BodyFontSize, text, TextAnchor.MiddleLeft);
            GameObject scrollObject = FormalUiKit.Create("战斗历史滚动区", timelineBack.transform);
            RectTransform scrollRect = scrollObject.AddComponent<RectTransform>();
            scrollRect.anchorMin = scrollRect.anchorMax = scrollRect.pivot = new Vector2(0f, 1f);
            scrollRect.anchoredPosition = new Vector2(16f, -52f);
            scrollRect.sizeDelta = new Vector2(384f, 322f);
            Image scrollSurface = scrollObject.AddComponent<Image>();
            scrollSurface.color = Color.clear;
            scrollSurface.raycastTarget = true;
            historyScroll = scrollObject.AddComponent<ScrollRect>();
            historyScroll.horizontal = false;
            historyScroll.vertical = true;
            historyScroll.movementType = ScrollRect.MovementType.Clamped;
            historyScroll.scrollSensitivity = 28f;

            GameObject viewport = FormalUiKit.Create("视口", scrollObject.transform);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<RectMask2D>();

            GameObject content = FormalUiKit.Create("记录内容", viewport.transform);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 322f);
            historyText = Label("战斗记录", content.transform, new Vector2(0f, 0f), new Vector2(376f, 322f), FormalUiTheme.BodyFontSize, text, TextAnchor.UpperLeft);
            historyText.rectTransform.anchorMin = historyText.rectTransform.anchorMax = historyText.rectTransform.pivot = new Vector2(0f, 1f);
            historyText.rectTransform.anchoredPosition = Vector2.zero;
            historyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            historyText.verticalOverflow = VerticalWrapMode.Overflow;
            historyText.supportRichText = true;
            historyText.lineSpacing = .92f;
            historyScroll.viewport = viewportRect;
            historyScroll.content = contentRect;
        }

        private static void SetBadgeTextRect(Text label, Vector2 position, Vector2 size)
        {
            label.rectTransform.anchoredPosition = position;
            label.rectTransform.sizeDelta = size;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private void CreateTimelineSlot(int index)
        {
            const float trackX = 116f;
            const float trackWidth = 254f;
            float y = -96f - index * 56f;
            Transform timelineParent = timelineFront != null ? timelineFront.transform : timelineModule.transform;
            GameObject row = FormalUiKit.FlatPanel("行动位" + (index + 1), timelineParent,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, y), new Vector2(388, 52),
                FormalUiTheme.WithAlpha(FormalUiTheme.Surface, index % 2 == 0 ? .86f : .70f));
            timelineRows[index] = row.GetComponent<Image>();
            timelineRows[index].raycastTarget = true;
            timelineInteractions[index] = row.AddComponent<TimelineRowInteraction>();
            Line(row.transform, new Vector2(16, index == 0 ? -26 : -52), new Vector2(2, index == timelineRows.Length - 1 ? 26 : 52), FormalUiTheme.WithAlpha(line, .82f));
            GameObject node = Panel("行动节点" + (index + 1), row.transform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(10, 8), new Vector2(14, 14), muted);
            timelineNodes[index] = node.GetComponent<Image>();
            timelineNames[index] = Label("行动者" + (index + 1), row.transform, new Vector2(36, -2), new Vector2(164, 26), CombatHudTypography.TimelineNameFontSize, muted, TextAnchor.MiddleLeft);
            timelineDetails[index] = Label("行动摘要" + (index + 1), row.transform, new Vector2(202, -2), new Vector2(150, 26), CombatHudTypography.TimelineDetailFontSize, muted, TextAnchor.MiddleRight);
            timelineSpeeds[index] = Label("行动速度" + (index + 1), row.transform, new Vector2(36, -27), new Vector2(80, 22),
                CombatHudTypography.TimelineDetailFontSize, muted, TextAnchor.MiddleLeft);
            FormalUiKit.PreventAutomaticWrapping(timelineNames[index]);
            FormalUiKit.ConfigureNumericLabel(timelineSpeeds[index]);
            FormalUiKit.ConfigureNumericLabel(timelineDetails[index]);
            GameObject track = FormalUiKit.FlatPanel("单位行动值轨道", row.transform, new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(trackX, 5), new Vector2(trackWidth, 8), FormalUiTheme.ResourceTrack);
            GameObject retained = FormalUiKit.FlatPanel("保留行动值", row.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(trackX, 7), new Vector2(0, 4), FormalUiTheme.Amber);
            timelineRetained[index] = retained.GetComponent<Image>();
            GameObject removed = FormalUiKit.FlatPanel("扣除行动值", row.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(trackX, 7), new Vector2(0, 4), FormalUiTheme.WithAlpha(FormalUiTheme.Danger, .52f));
            timelineRemoved[index] = removed.GetComponent<Image>();
            timelineCurrentEndpoint[index] = FormalUiKit.FlatPanel("当前端点", row.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(trackX, 4), new Vector2(2, 10), FormalUiTheme.Text).GetComponent<Image>();
            timelinePreviewEndpoint[index] = FormalUiKit.FlatPanel("预览端点", row.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(trackX, 4), new Vector2(2, 10), FormalUiTheme.Amber).GetComponent<Image>();
            GameObject arrow = FormalUiKit.Create("预测顺序箭头" + (index + 1), row.transform);
            RectTransform arrowRect = arrow.AddComponent<RectTransform>();
            arrowRect.anchorMin = arrowRect.anchorMax = arrowRect.pivot = new Vector2(0f, 1f);
            arrowRect.anchoredPosition = new Vector2(360f, -6f);
            arrowRect.sizeDelta = new Vector2(16f, 16f);
            timelineOrderArrows[index] = arrow.AddComponent<Image>();
            timelineOrderArrows[index].raycastTarget = false;
            timelineOrderArrows[index].preserveAspect = true;
            timelineOrderArrows[index].gameObject.SetActive(false);
            foreach (Graphic graphic in row.GetComponentsInChildren<Graphic>())
                if (graphic != timelineRows[index]) graphic.raycastTarget = false;
        }

        private void Refresh()
        {
            RefreshCount++;
            CombatState state = bootstrap.CurrentState;
            if (displayedTimelineState != state)
            {
                ResetTimelineMotionState();
                displayedTimelineState = state;
            }
            UnitState hero = state.GetUnit("hero");
            hero = (bootstrap as ICombatActionPresentationHost)?.PresentCombatUnit(hero) ?? hero;
            RogueliteMapRun run = bootstrap.CurrentMapRun;
            headerResourceLabel.text = "生命 " + hero.Health + "／" + hero.MaxHealth
                + "　|　金币 " + (run?.Gold ?? 0)
                + "　|　学院贡献 " + (run?.StageContribution ?? 0)
                + "　|　学期时间 " + (run?.StageTime ?? 0);
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
            statusLabel.text = "状态　" + StatusText(state, hero);
            actionPointBadgeValue.text = ActionPointText(hero.ActionPoints);
            healthValue.text = RatioText(hero.Health, hero.MaxHealth);
            shieldValue.text = rogue ? hero.Shield + "　无上限" : RatioText(hero.Shield, hero.MaxShield);
            manaValue.text = RatioText(hero.Mana, hero.MaxMana);
            SetBar(healthFill, hero.Health / (float)Math.Max(1, hero.MaxHealth), ref displayedHealth);
            SetBar(shieldFill, rogue ? (hero.Shield > 0 ? 1f : 0f) : hero.Shield / (float)Math.Max(1, hero.MaxShield), ref displayedShield);
            SetBar(manaFill, hero.Mana / (float)Math.Max(1, hero.MaxMana), ref displayedMana);
            RefreshHeroBack(state, hero, rogue);
            RefreshHistory(state);
            IReadOnlyList<CombatTurnTrackEntry> track = CombatTurnTrackPresentation.Build(state, timelineNames.Length);
            CombatActionPreview timelinePreview = bootstrap.CurrentActionPreview;
            IReadOnlyDictionary<string, int> orderChanges = timelinePreview != null && timelinePreview.CanSubmit && timelinePreview.HasActionValuePreview
                ? CombatTurnTrackPresentation.PreviewOrderChanges(state, timelinePreview.ActionValueTargetId, timelinePreview.ActionValueDelay)
                : new Dictionary<string, int>();
            CombatTurnTrackEntry? leading = track.Count > 0 ? track[0] : (CombatTurnTrackEntry?)null;
            SetTimelineGlobal(leading);
            ReconcileTimelineSlots(track);
            for (int i = 0; i < timelineRows.Length; i++) timelineRows[i].gameObject.SetActive(false);
            HashSet<int> occupiedTimelineSlots = new HashSet<int>();
            for (int order = 0; order < track.Count; order++)
            {
                CombatTurnTrackEntry entry = track[order];
                bool existingSlot = timelineSlotsByUnit.ContainsKey(entry.UnitId);
                int slot = AcquireTimelineSlot(entry.UnitId, occupiedTimelineSlots);
                PositionTimelineRow(slot, order, existingSlot);
                timelineRows[slot].gameObject.SetActive(true);
                Color faction = entry.IsHero ? line : FormalUiTheme.Danger;
                timelineNodes[slot].color = entry.IsActive ? faction : FormalUiTheme.WithAlpha(faction, .55f);
                timelineRows[slot].color = entry.IsActive ? FormalUiTheme.WithAlpha(faction, .16f) : FormalUiTheme.WithAlpha(FormalUiTheme.Surface, .76f);
                timelineNames[slot].text = entry.Order + " " + CompactHud(entry.DisplayName, 9);
                timelineNames[slot].color = entry.IsActive ? text : muted;
                timelineSpeeds[slot].text = "行动速度 " + entry.EffectiveSpeed;
                timelineSpeeds[slot].color = entry.IsActive ? faction : muted;
                timelineDetails[slot].color = entry.IsActive ? faction : muted;
                timelineInteractions[slot].Configure(entry.UnitId, timelineRows[slot],
                    unitId => bootstrap.SetTimelineHoveredUnit(unitId),
                    unitId => bootstrap.FocusBattlefieldOnUnit(unitId));
                bool previewed = timelinePreview != null && timelinePreview.CanSubmit && timelinePreview.HasActionValuePreview && timelinePreview.ActionValueTargetId == entry.UnitId;
                int previewValue = previewed ? CombatActionTimeline.PreviewDelayedValue(state.GetUnit(entry.UnitId), timelinePreview.ActionValueDelay) : entry.ActionValue;
                float currentWidth = 254f * entry.ActionValue / CombatActionTimeline.MaximumValue;
                float previewWidth = 254f * previewValue / CombatActionTimeline.MaximumValue;
                timelineRetained[slot].gameObject.SetActive(true);
                timelineRemoved[slot].gameObject.SetActive(previewed);
                timelineCurrentEndpoint[slot].gameObject.SetActive(previewed);
                timelinePreviewEndpoint[slot].gameObject.SetActive(previewed);
                timelineRetained[slot].color = entry.IsActive ? faction : FormalUiTheme.WithAlpha(faction, .62f);
                SetTimelineRowValue(slot, entry, previewed ? previewValue : entry.ActionValue, previewed ? previewWidth : currentWidth);
                SetTimelineOrderArrow(slot, orderChanges.TryGetValue(entry.UnitId, out int direction) ? direction : 0);
                if (previewed)
                {
                    timelineRemoved[slot].rectTransform.anchoredPosition = new Vector2(116 + previewWidth, 7);
                    timelineRemoved[slot].rectTransform.sizeDelta = new Vector2(Mathf.Max(2, currentWidth - previewWidth), 4);
                    timelineCurrentEndpoint[slot].rectTransform.anchoredPosition = new Vector2(116 + currentWidth, 4);
                    timelinePreviewEndpoint[slot].rectTransform.anchoredPosition = new Vector2(116 + previewWidth, 4);
                }
            }
            for (int i = 0; i < quickbarLabels.Length; i++)
            {
                if (rogue && state.RogueEquipment != null)
                {
                    string rogueId = state.RogueEquipment.ItemQuickbarInstanceIds[i];
                    RogueTacticalItemInstance tactical = state.RogueEquipment.TacticalItem(rogueId);
                    TacticalItemDefinition tacticalDefinition = tactical == null ? null : RogueContentCatalog.CreateAcademyV01().TacticalItems.First(value => value.DefinitionId == tactical.DefinitionId);
                    RefreshQuickbarReadout(i, tactical != null, tactical?.ChargesCurrent ?? 0);
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
                RefreshQuickbarReadout(i, definition != null, item?.RemainingUses ?? 0);
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
            return trimmed.Length <= 10 ? trimmed : trimmed.Substring(0, 9) + "…";
        }

        private static void ConfigureSpellSlotLayout(Button button, Image spellIcon, int slot)
        {
            if (button == null) return;
            RectTransform cardRect = button.GetComponent<RectTransform>();
            float cardWidth = cardRect == null ? 268f : cardRect.rect.width;
            float resourceBlockX = cardWidth - 66f;
            float nameWidth = resourceBlockX - 80f;
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
            spellLabel.rectTransform.anchoredPosition = new Vector2(74f, -6f);
            spellLabel.rectTransform.sizeDelta = new Vector2(nameWidth, 64f);
            spellLabel.fontSize = FormalUiTheme.BodyFontSize;
            spellLabel.fontStyle = FontStyle.Normal;
            spellLabel.alignment = TextAnchor.MiddleCenter;
            spellLabel.color = FormalUiTheme.Text;
            spellLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            spellLabel.verticalOverflow = VerticalWrapMode.Truncate;
            spellLabel.resizeTextForBestFit = false;
            spellLabel.lineSpacing = .9f;

            if (button.transform.Find("术式资源块") == null)
                FormalUiKit.FlatPanel("术式资源块", button.transform,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(resourceBlockX, -6f), new Vector2(60f, 64f),
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
            RectTransform rect = button.GetComponent<RectTransform>();
            float width = rect == null ? 268f : rect.rect.width;
            float height = rect == null ? 76f : rect.rect.height;
            CreateSpellFrameEdge(button.transform, "术式细框_上", Vector2.zero, new Vector2(width, 2f), frameColor);
            CreateSpellFrameEdge(button.transform, "术式细框_下", new Vector2(0f, -height + 2f), new Vector2(width, 2f), frameColor);
            CreateSpellFrameEdge(button.transform, "术式细框_左", Vector2.zero, new Vector2(2f, height), frameColor);
            CreateSpellFrameEdge(button.transform, "术式细框_右", new Vector2(width - 2f, 0f), new Vector2(2f, height), frameColor);
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
                RectTransform cardRect = button.GetComponent<RectTransform>();
                float cardWidth = cardRect == null ? 268f : cardRect.rect.width;
                label.rectTransform.sizeDelta = new Vector2(cardWidth - 146f, 64f);
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
                return new Vector2(width - 64f, second ? -38f : -6f);
            return new Vector2(width - 64f, -10f);
        }

        private static void ConfigureCostChip(Transform chip, bool spellSlot, Vector2 position, Color accent)
        {
            if (chip == null) return;
            RectTransform chipRect = chip.GetComponent<RectTransform>();
            chipRect.anchoredPosition = position;
            chipRect.sizeDelta = new Vector2(56, 32);
            Image background = chip.GetComponent<Image>() ?? chip.gameObject.AddComponent<Image>();
            background.color = spellSlot ? Color.clear : FormalUiTheme.Ink;
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
                RectTransform cardRect = button.GetComponent<RectTransform>();
                float cardWidth = cardRect == null ? 268f : cardRect.rect.width;
                spellLabel.rectTransform.sizeDelta = new Vector2(cardWidth - (visible ? 184f : 146f), 64f);
                if (visible && spellLabel.text.Length > 6) spellLabel.text = spellLabel.text.Substring(0, 5) + "…";
            }
            Transform noticeChip = button.transform.Find("语义_notice");
            if (visible && noticeChip == null)
            {
                RectTransform rect = button.GetComponent<RectTransform>();
                float width = rect == null ? 80f : rect.sizeDelta.x;
                FormalUiKit.SemanticChip("notice", value >= 0 ? value.ToString() : string.Empty, button.transform,
                    spellSlot ? new Vector2(width - 104f, -24f) : new Vector2(Mathf.Max(4f, width - (value >= 0 ? 56f : 32f)), -4f),
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
            RectTransform cardRect = chip.parent == null ? null : chip.parent.GetComponent<RectTransform>();
            float cardWidth = cardRect == null ? 268f : cardRect.rect.width;
            chipRect.anchoredPosition = new Vector2(cardWidth - 104f, -24f);
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
            if (rogueSpell != null) title = rogueSpell.DisplayName;
            if (actionButtons.TryGetValue(action, out Button button))
            {
                string label = button.GetComponentInChildren<Text>()?.text;
                if (rogueSpell == null && !string.IsNullOrWhiteSpace(label)) title = label.Split('\n')[0];
            }
            string body = rogueSpell == null ? CombatInformationPresenter.BuildActionDetails(preview) :
                (preview == null ? string.Empty : preview.CanSubmit ? "当前　可用\n" : "当前　不可用\n" + preview.FailureReason + "\n") +
                FormalRogueliteUi.SpellTooltipBody(rogueSpell);
            string category = rogueSpell != null ? "个人术式" : SkillSlot(action) >= 0 ? "术式与法宝" : "战斗指令";
            string iconPath = rogueSpell == null ? string.Empty : FormalRogueliteUi.RogueSpellIconPath(rogueSpell.DefinitionId);
            if (rogueSpell == null && rogueSlot >= 0)
            {
                FireSpellDefinition fire = bootstrap?.FireSpellInSlot(rogueSlot);
                ArtifactDefinition artifact = rogueSlot == 0 ? (bootstrap?.CurrentArmedArtifact ?? bootstrap?.CurrentTrainingRangeArtifact) : null;
                iconPath = artifact?.IconPath ?? fire?.IconPath ?? string.Empty;
            }
            return new FormalTooltipContent(category, title, body, line, iconPath);
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

        private bool HandleCardFlipShortcut()
        {
            if (Keyboard.current?.tabKey.wasPressedThisFrame != true) return false;
            ToggleBothCards();
            return true;
        }

        private void ToggleBothCards()
        {
            if (heroFlipAnimating || timelineFlipAnimating) return;
            ToggleHeroCard();
            ToggleTimelineCard();
        }

        private void ToggleHeroCard()
        {
            if (heroFlipAnimating) return;
            bool showBack = !heroShowingBack;
            StartCardFlip(heroFront, heroBack, heroFlipButton, showBack, "概况", "状态",
                value => heroShowingBack = value, value => heroFlipAnimating = value);
        }

        private void ToggleTimelineCard()
        {
            if (timelineFlipAnimating) return;
            bool showBack = !timelineShowingBack;
            StartCardFlip(timelineFront, timelineBack, timelineFlipButton, showBack, "行动值", "记录",
                value => timelineShowingBack = value, value => timelineFlipAnimating = value);
        }

        private void StartCardFlip(GameObject front, GameObject back, Button button, bool showBack,
            string backButtonLabel, string frontButtonLabel, Action<bool> setShowingBack, Action<bool> setAnimating)
        {
            if (front == null || back == null || button == null) return;
            GameObject outgoing = showBack ? front : back;
            GameObject incoming = showBack ? back : front;
            RectTransform outgoingRect = outgoing.GetComponent<RectTransform>();
            RectTransform incomingRect = incoming.GetComponent<RectTransform>();
            UiMotionProfile motion = UiMotionProfile.FromIntensity(bootstrap == null ? 1f : bootstrap.UiPreferences.AnimationIntensity);
            setAnimating(true);
            button.interactable = false;
            outgoing.SetActive(true);
            outgoingRect.localScale = Vector3.one;

            Action swap = () =>
            {
                outgoing.SetActive(false);
                incoming.SetActive(true);
                incomingRect.localScale = new Vector3(0f, 1f, 1f);
                setShowingBack(showBack);
                Text buttonLabel = button.GetComponentInChildren<Text>();
                if (buttonLabel != null) buttonLabel.text = showBack ? backButtonLabel : frontButtonLabel;
            };
            Action finish = () =>
            {
                incomingRect.localScale = Vector3.one;
                button.interactable = true;
                setAnimating(false);
            };

            if (motion.IsImmediate)
            {
                swap();
                finish();
                return;
            }

            float halfDuration = Mathf.Max(.06f, motion.QuickDuration);
            DOTween.Sequence().SetUpdate(true).SetTarget(this)
                .Append(outgoingRect.DOScaleX(0f, halfDuration).SetEase(Ease.InCubic))
                .AppendCallback(() => swap())
                .Append(incomingRect.DOScaleX(1f, halfDuration).SetEase(Ease.OutCubic))
                .OnComplete(() => finish());
        }

        private void ResetCardFlips()
        {
            DOTween.Kill(this);
            ResetCardFace(heroFront, heroBack, heroFlipButton, "状态");
            ResetCardFace(timelineFront, timelineBack, timelineFlipButton, "记录");
            heroShowingBack = false;
            timelineShowingBack = false;
            heroFlipAnimating = false;
            timelineFlipAnimating = false;
        }

        private static void ResetCardFace(GameObject front, GameObject back, Button button, string buttonLabel)
        {
            if (front != null)
            {
                front.transform.localScale = Vector3.one;
                front.SetActive(true);
            }
            if (back != null)
            {
                back.transform.localScale = Vector3.one;
                back.SetActive(false);
            }
            if (button == null) return;
            button.interactable = true;
            Text label = button.GetComponentInChildren<Text>();
            if (label != null) label.text = buttonLabel;
        }

        private void RefreshHeroBack(CombatState state, UnitState hero, bool rogue)
        {
            if (heroStatusDetails == null || heroPassiveDetails == null || state == null || hero == null) return;
            IReadOnlyList<CombatStatusBarEntry> entries = state.PassiveEffects.StatusBarEntriesFor(hero.Id);
            string currentStatus = hero.Statuses.Count == 0
                ? "正常"
                : string.Join(" ", hero.Statuses.Take(2).Select(pair =>
                {
                    CombatFeedbackSemantic semantic = CombatFeedbackCatalog.For(CombatFeedbackCatalog.ForStatus(pair.Key));
                    return semantic.ShortLabel + " " + pair.Value;
                }));
            CombatStatusBarEntry[] ongoing = entries.Where(value => value.Kind == CombatStatusBarEntryKind.OngoingEffect).ToArray();
            if (ongoing.Length > 0) currentStatus += " +" + string.Join(" ", ongoing.Take(2).Select(value => value.DisplayName));
            heroStatusDetails.text = (state.ActiveUnitId == hero.Id ? "行动中　" : "待命　") + hero.ActionPoints + " AP\n" +
                "生命　" + hero.Health + "／" + hero.MaxHealth + "\n" +
                "护盾　" + hero.Shield + (rogue ? string.Empty : "／" + hero.MaxShield) + "\n" +
                "魔力　" + hero.Mana + "／" + hero.MaxMana + "\n" +
                "状态　" + CompactHud(currentStatus, 12);

            CombatStatusBarEntry[] passives = entries.Where(value => value.Kind == CombatStatusBarEntryKind.Passive).Take(3).ToArray();
            heroPassiveDetails.text = passives.Length == 0
                ? "当前没有被动效果"
                : string.Join("\n", passives.Select(value =>
                    CompactHud(value.DisplayName, 8) + "\n<color=#9B8C72>来源　" + PassiveSourceLabel(value) + "　" + CompactHud(value.TimingText, 6) + "</color>"));
        }

        private static string PassiveSourceLabel(CombatStatusBarEntry entry)
        {
            string runtimeId = entry?.RuntimeId ?? string.Empty;
            if (runtimeId.StartsWith("origin:", StringComparison.Ordinal)) return "出身";
            if (runtimeId.StartsWith("equipment:", StringComparison.Ordinal)) return "装备";
            if (runtimeId.StartsWith("spell:", StringComparison.Ordinal)) return "术式";
            if (runtimeId.StartsWith("artifact:", StringComparison.Ordinal)) return "法宝";
            return "战斗效果";
        }

        private void RefreshHistory(CombatState state)
        {
            if (historyText == null || historyScroll == null || state == null) return;
            string next = state.EventLog.Count == 0
                ? "暂无战斗记录"
                : string.Join("\n", state.EventLog.Select((value, index) =>
                    (index == 0 ? "<color=#60D8E8>最新　" : (index + 1).ToString("00") + "　") +
                    CombatHudTypography.PlayerEventLine(value) + (index == 0 ? "</color>" : string.Empty)));
            if (next == displayedHistoryText) return;
            displayedHistoryText = next;
            historyText.text = next;
            Canvas.ForceUpdateCanvases();
            float height = Mathf.Max(historyScroll.viewport.rect.height, historyText.preferredHeight + 8f);
            historyScroll.content.sizeDelta = new Vector2(0f, height);
            historyText.rectTransform.sizeDelta = new Vector2(376f, height);
            historyScroll.verticalNormalizedPosition = 1f;
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
            string details = rogue ? CombatInformationPresenter.BuildRogueliteHeroDetails(state, hero) : CombatInformationPresenter.BuildHeroDetails(hero);
            if (rogue && state.RogueShieldEvents.Count > 0)
                details += "\n最近护盾\n" + string.Join("\n", state.RogueShieldEvents.Take(2).Select(RogueShieldLogPresentation.Format));
            return new FormalTooltipContent("人物状态", "你的情况", details, FormalUiTheme.Safe);
        }

        private FormalTooltipContent BuildQuickbarTooltip(int slot)
        {
            CombatState state = bootstrap?.CurrentState;
            if (state?.Ruleset == CombatRuleset.Roguelite && state.RogueEquipment != null)
            {
                string id = state.RogueEquipment.ItemQuickbarInstanceIds[slot]; RogueTacticalItemInstance tactical = state.RogueEquipment.TacticalItem(id);
                TacticalItemDefinition tacticalDefinition = state.RogueEquipment.TacticalDefinitionFor(id);
                return new FormalTooltipContent("战术道具", tactical == null ? "战术栏 " + (slot + 1) : tacticalDefinition.DisplayName,
                    tactical == null ? "空槽" : FormalRogueliteUi.RogueInventoryDetailBody(state.RogueEquipment, id, false), FormalUiTheme.Safe,
                    tactical == null ? string.Empty : FormalArtRegistry.ItemPath(tactical.DefinitionId));
            }
            ItemInstance item = state == null || slot < 0 || slot >= state.ItemQuickbar.Length ? null : state.ItemInventory.Get(state.ItemQuickbar[slot]);
            ItemDefinition definition = item == null ? null : ItemCatalog.Get(item.DefinitionId);
            string title = definition == null ? "快捷栏 " + (slot + 1) : definition.DisplayName;
            return new FormalTooltipContent("物品", title, CombatInformationPresenter.BuildItemDetails(definition, item, slot), FormalUiTheme.Safe,
                definition?.IconPath ?? string.Empty);
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
            return current + "　上限 " + maximum + "　" + percent + "%";
        }

        private void BindTooltip(GameObject target, Func<FormalTooltipContent> provider)
        {
            if (target == null) return;
            FormalHoverTooltipTrigger trigger = target.GetComponent<FormalHoverTooltipTrigger>() ?? target.AddComponent<FormalHoverTooltipTrigger>();
            trigger.Configure(tooltip, provider);
        }

        private Image ResourceBar(Transform parent, string title, Vector2 position, Color color, out Text valueLabel)
        {
            Label(title, parent, position, new Vector2(200, 32), FormalUiTheme.BodyFontSize, muted, TextAnchor.MiddleLeft);
            valueLabel = Label(title + "数值", parent, position, new Vector2(374, 32), CombatHudTypography.ResourceValueFontSize,
                text, CombatHudTypography.ResourceValueAlignment);
            FormalUiKit.ConfigureNumericLabel(valueLabel);
            GameObject track = Panel(title + "轨道", parent, new Vector2(0, 1), new Vector2(0, 1), position + new Vector2(0, -32), new Vector2(384, 20), FormalUiTheme.ResourceTrack);
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

        private void SetTimelineGlobal(CombatTurnTrackEntry? leading)
        {
            float target = leading.HasValue ? leading.Value.ActionValue : 0f;
            bool direct = displayedTimelineGlobal < 0f;
            timelineGlobalFill.rectTransform.DOKill();
            timelineGlobalValue.DOKill();
            UiMotionProfile motion = UiMotionProfile.FromIntensity(bootstrap == null ? 1f : bootstrap.UiPreferences.AnimationIntensity);
            if (direct || motion.IsImmediate)
            {
                displayedTimelineGlobal = target;
                ApplyTimelineGlobal(leading, displayedTimelineGlobal);
            }
            else
            {
                DOTween.To(() => displayedTimelineGlobal, value =>
                {
                    displayedTimelineGlobal = value;
                    ApplyTimelineGlobal(leading, value);
                }, target, TimelineMotionDuration(displayedTimelineGlobal, target, motion)).SetEase(FormalUiMotionTokens.StandardEase).SetUpdate(true).SetTarget(timelineGlobalFill.rectTransform);
            }
        }

        private void ApplyTimelineGlobal(CombatTurnTrackEntry? leading, float value)
        {
            timelineGlobalValue.text = leading.HasValue ? "全局　值 " + Mathf.RoundToInt(value) : "等待";
            timelineGlobalFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value / CombatActionTimeline.MaximumValue), 1f);
        }

        // The game state always retains the real action value. While an action is previewed,
        // this method instead animates the same row toward its projected value; cancelling the
        // preview animates it back without mutating combat state.
        private void SetTimelineRowValue(int index, CombatTurnTrackEntry entry, float targetValue, float targetWidth)
        {
            timelineDetails[index].DOKill();
            timelineRetained[index].rectTransform.DOKill();
            UiMotionProfile motion = UiMotionProfile.FromIntensity(bootstrap == null ? 1f : bootstrap.UiPreferences.AnimationIntensity);
            if (!displayedTimelineValues.TryGetValue(entry.UnitId, out float displayedValue) ||
                !displayedTimelineWidths.TryGetValue(entry.UnitId, out float displayedWidth) || motion.IsImmediate)
            {
                displayedTimelineValues[entry.UnitId] = targetValue;
                displayedTimelineWidths[entry.UnitId] = targetWidth;
                ApplyTimelineRow(index, entry, targetValue, targetWidth);
                return;
            }
            // A unit may have changed order since the last refresh. Paint its retained value
            // onto the new row before starting the tween, then let the discrete result play out.
            ApplyTimelineRow(index, entry, displayedValue, displayedWidth);
            float duration = TimelineMotionDuration(displayedValue, targetValue, motion);
            DOTween.To(() => displayedTimelineValues[entry.UnitId], value =>
            {
                displayedTimelineValues[entry.UnitId] = value;
                ApplyTimelineDetail(index, entry, value);
            }, targetValue, duration).SetEase(FormalUiMotionTokens.StandardEase).SetUpdate(true).SetTarget(timelineDetails[index]);
            DOTween.To(() => displayedTimelineWidths[entry.UnitId], value =>
            {
                displayedTimelineWidths[entry.UnitId] = value;
                timelineRetained[index].rectTransform.sizeDelta = new Vector2(value, 4f);
            }, targetWidth, duration).SetEase(FormalUiMotionTokens.StandardEase).SetUpdate(true).SetTarget(timelineRetained[index].rectTransform);
        }

        private static float TimelineMotionDuration(float from, float to, UiMotionProfile motion)
        {
            float distance = Mathf.Clamp01(Mathf.Abs(to - from) / CombatActionTimeline.MaximumValue);
            return Mathf.Lerp(motion.StandardDuration, Mathf.Max(.08f, .42f * motion.Intensity), distance);
        }

        private void ReconcileTimelineSlots(IReadOnlyList<CombatTurnTrackEntry> track)
        {
            HashSet<string> visibleUnits = new HashSet<string>(track.Select(entry => entry.UnitId));
            foreach (string unitId in timelineSlotsByUnit.Keys.Where(unitId => !visibleUnits.Contains(unitId)).ToArray())
                timelineSlotsByUnit.Remove(unitId);
        }

        private int AcquireTimelineSlot(string unitId, ISet<int> occupiedSlots)
        {
            if (timelineSlotsByUnit.TryGetValue(unitId, out int existingSlot) && occupiedSlots.Add(existingSlot)) return existingSlot;
            for (int slot = 0; slot < timelineRows.Length; slot++)
            {
                if (!occupiedSlots.Add(slot)) continue;
                timelineSlotsByUnit[unitId] = slot;
                return slot;
            }
            throw new InvalidOperationException("行动条没有可用显示行。");
        }

        private void PositionTimelineRow(int slot, int order, bool animate)
        {
            RectTransform rect = timelineRows[slot].rectTransform;
            Vector2 target = new Vector2(14f, -96f - order * 56f);
            rect.DOKill();
            UiMotionProfile motion = UiMotionProfile.FromIntensity(bootstrap == null ? 1f : bootstrap.UiPreferences.AnimationIntensity);
            if (!animate || motion.IsImmediate)
            {
                rect.anchoredPosition = target;
                return;
            }
            DOTween.To(() => rect.anchoredPosition, value => rect.anchoredPosition = value,
                    target, Mathf.Max(.08f, .32f * motion.Intensity))
                .SetEase(Ease.InOutCubic).SetUpdate(true).SetTarget(rect);
        }

        private void ResetTimelineMotionState()
        {
            displayedTimelineValues.Clear();
            displayedTimelineWidths.Clear();
            timelineSlotsByUnit.Clear();
            displayedTimelineGlobal = -1f;
            displayedTimelineState = null;
            if (timelineGlobalFill != null) timelineGlobalFill.rectTransform.DOKill();
            for (int i = 0; i < timelineNames.Length; i++)
            {
                timelineDetails[i]?.DOKill();
                timelineRetained[i]?.rectTransform.DOKill();
                timelineRows[i]?.rectTransform.DOKill();
            }
        }

        private void ApplyTimelineRow(int index, CombatTurnTrackEntry entry, float value, float width)
        {
            ApplyTimelineDetail(index, entry, value);
            timelineRetained[index].rectTransform.sizeDelta = new Vector2(width, 4f);
        }

        private void ApplyTimelineDetail(int index, CombatTurnTrackEntry entry, float value)
        {
            string state = entry.IsActive ? "　行动中" : entry.IsReady ? "　待行动" : string.Empty;
            timelineDetails[index].text = "值 " + Mathf.RoundToInt(value) + state;
        }

        private void SetTimelineOrderArrow(int index, int direction)
        {
            Image arrow = timelineOrderArrows[index];
            if (arrow == null) return;
            arrow.rectTransform.DOKill();
            arrow.rectTransform.anchoredPosition = new Vector2(360f, -6f);
            if (direction == 0)
            {
                arrow.gameObject.SetActive(false);
                return;
            }
            arrow.sprite = Resources.Load<Sprite>(direction > 0
                ? "Art/FormalSemanticIcons16/turn_order_up"
                : "Art/FormalSemanticIcons16/turn_order_down");
            arrow.color = Color.white;
            arrow.gameObject.SetActive(arrow.sprite != null);
            if (arrow.sprite == null) return;
            UiMotionProfile motion = UiMotionProfile.FromIntensity(bootstrap == null ? 1f : bootstrap.UiPreferences.AnimationIntensity);
            if (!motion.IsImmediate)
                DOTween.To(() => arrow.rectTransform.anchoredPosition.y, value =>
                    arrow.rectTransform.anchoredPosition = new Vector2(360f, value), -6f + direction * 4f,
                    Mathf.Max(.24f, motion.StandardDuration * 1.5f)).SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetTarget(arrow.rectTransform);
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

        private static string ActionPointText(int current) => current.ToString();

        private void RefreshAvailability(CombatState state, UnitState hero)
        {
            bool playing = (bootstrap as ICombatActionPresentationHost)?.IsCombatActionPlaying == true;
            bool heroTurn = !playing && state.ActiveUnitId == "hero" && hero.IsAlive;
            foreach (KeyValuePair<string, Button> pair in actionButtons)
            {
                bool available = heroTurn;
                string reason = heroTurn ? string.Empty : playing ? "正在行动…" : "等待敌方行动";
                int parsedSlot = SkillSlot(pair.Key);
                SpellDefinition rogueSpell = state.Ruleset == CombatRuleset.Roguelite && state.RogueSpells != null && parsedSlot >= 0 ? state.RogueSpells.DefinitionAtSlot(parsedSlot) : null;
                SkillDefinition skill = pair.Key == "技能1" ? hero.SkillOne : pair.Key == "技能2" ? hero.SkillTwo : null;
                int fireSlot = parsedSlot;
                FireSpellDefinition fire = fireSlot < 0 ? null : bootstrap.FireSpellInSlot(fireSlot);
                ArtifactDefinition artifact = fireSlot == 0 ? (bootstrap.CurrentArmedArtifact ?? bootstrap.CurrentTrainingRangeArtifact) : null;
                if (available && artifact != null && hero.ActionPoints < artifact.ActionPointCost) { available = false; reason = "行动点不足：需要 " + artifact.ActionPointCost; }
                if (available && fire != null && bootstrap.CurrentFireBattle != null && bootstrap.CurrentFireBattle.Cooldown(hero.Id, fire.Id) > 0) { available = false; reason = "术式冷却中，还需 " + bootstrap.CurrentFireBattle.Cooldown(hero.Id, fire.Id) + " 回合"; }
                if (available && fire != null && hero.Mana < fire.ManaCost) { available = false; reason = "能量不足：需要 " + fire.ManaCost; }
                if (available && fire != null && hero.ActionPoints < fire.ActionPointCost) { available = false; reason = "行动点不足：需要 " + fire.ActionPointCost; }
                if (fire != null || artifact != null) skill = null;
                if (available && skill != null && hero.Cooldown(skill) > 0) { available = false; reason = "技能冷却中，还需 " + hero.Cooldown(skill) + " 回合"; }
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

        private static string StatusText(CombatState state, UnitState unit)
        {
            IReadOnlyList<CombatStatusBarEntry> entries = state?.PassiveEffects.StatusBarEntriesFor(unit.Id) ?? Array.Empty<CombatStatusBarEntry>();
            CombatStatusBarEntry[] ongoing = entries.Where(value => value.Kind == CombatStatusBarEntryKind.OngoingEffect).ToArray();
            if (unit.Statuses.Count > 0)
            {
                KeyValuePair<StatusType, int> first = unit.Statuses.First();
                CombatFeedbackSemantic semantic = CombatFeedbackCatalog.For(CombatFeedbackCatalog.ForStatus(first.Key));
                int extra = unit.Statuses.Count - 1 + ongoing.Length;
                string remaining = extra > 0 ? "  +" + extra : string.Empty;
                return "<color=" + semantic.ColorHex + ">" + semantic.ShortLabel + " " + first.Value + "</color>" + remaining;
            }
            if (ongoing.Length > 0)
            {
                string remaining = ongoing.Length > 1 ? "  +" + (ongoing.Length - 1) : string.Empty;
                return "<color=#60D8E8>" + ongoing[0].DisplayName + "　待命</color>" + remaining;
            }
            int passiveCount = entries.Count(value => value.Kind == CombatStatusBarEntryKind.Passive);
            return passiveCount > 0 ? "被动 " + passiveCount : "正常";
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
            DOTween.Kill(this);
            if (root != null) root.transform.DOKill();
            if (timelineGlobalFill != null) timelineGlobalFill.rectTransform.DOKill();
            for (int i = 0; i < timelineNames.Length; i++)
            {
                timelineDetails[i]?.DOKill();
                if (timelineRetained[i] != null) timelineRetained[i].rectTransform.DOKill();
                if (timelineOrderArrows[i] != null) timelineOrderArrows[i].rectTransform.DOKill();
            }
        }
    }

    // Kept as a component so every row owns its own pointer lifecycle while the HUD rebinds data.
    internal sealed class TimelineRowInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private string unitId;
        private Image surface;
        private Color restingColor;
        private Action<string> hovered;
        private Action<string> clicked;

        public void Configure(string value, Image rowSurface, Action<string> hover, Action<string> click)
        {
            unitId = value;
            surface = rowSurface;
            restingColor = surface == null ? Color.clear : surface.color;
            hovered = hover;
            clicked = click;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (surface != null) surface.color = Color.Lerp(restingColor, FormalUiTheme.Cyan, .22f);
            hovered?.Invoke(unitId);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (surface != null) surface.color = restingColor;
            hovered?.Invoke(null);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) clicked?.Invoke(unitId);
        }
    }
}
