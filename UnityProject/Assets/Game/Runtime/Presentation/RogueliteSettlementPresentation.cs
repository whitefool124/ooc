using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // Runtime settlement layer keeps the authored combat HUD untouched while reward UI is iterated.
    [DefaultExecutionOrder(-100)]
    public sealed class RogueliteSettlementPresentation : MonoBehaviour
    {
        private ISettlementPresentationHost bootstrap;
        private Canvas canvas;
        private FormalHoverTooltip tooltip;
        private GameObject panel;
        private int presentedSeed = int.MinValue;
        private readonly List<RewardCardInput> rewardCards = new List<RewardCardInput>();
        private bool claimPending;
        private bool hasPresentedModel;
        private SettlementPresentationModel presentedModel;
        public int RefreshCount { get; private set; }

        private sealed class RewardCardInput
        {
            public string RewardId;
            public RogueliteReward Reward;
            public bool CanClaim;
            public RectTransform Rect;
            public Image Image;
            public Color Normal;
            public Color Hover;
            public bool IsHovering;
            public Button Button;
        }

        public void Initialize(ISettlementPresentationHost source)
        {
            bootstrap = source;
            bootstrap.UiPresentationVersions.Changed += OnPresentationChanged;
            RefreshNow();
        }

        private void OnPresentationChanged(UiPresentationChange change)
        {
            if (change.Area == UiPresentationArea.Settlement || change.Area == UiPresentationArea.Flow || change.Area == UiPresentationArea.MapStructure)
                RefreshNow();
        }

        public void RefreshNow()
        {
            RogueliteMapRun run = bootstrap == null ? null : bootstrap.CurrentMapRun;
            SettlementPresentationModel nextModel = SettlementPresentationModel.From(run);
            if (hasPresentedModel && presentedModel.Equals(nextModel)) return;
            presentedModel = nextModel;
            hasPresentedModel = true;
            RefreshCount++;
            if (run == null || !run.AwaitingReward)
            {
                Hide();
                return;
            }

            Show(run);
        }

        private void OnGUI()
        {
            if (panel == null || Event.current == null) return;
            Vector2 screenPoint = new Vector2(Event.current.mousePosition.x, Screen.height - Event.current.mousePosition.y);
            foreach (RewardCardInput card in rewardCards)
            {
                if (card.Rect == null || !card.Rect.gameObject.activeInHierarchy) continue;
                bool hovering = RectTransformUtility.RectangleContainsScreenPoint(card.Rect, screenPoint);
                SetHover(card, hovering);
                if (card.CanClaim && hovering && Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    TryClaim(card.RewardId);
                    Event.current.Use();
                    return;
                }
            }
        }

        private void Show(RogueliteMapRun run)
        {
            Hide();
            EnsureCanvas();
            rewardCards.Clear();
            claimPending = false;
            presentedSeed = run.Seed;
            bootstrap.PublishUiVisual(new UiVisualEvent(UiVisualEventKind.SettlementOpened, run.Seed.ToString()));
            panel = CreateObject("肉鸽结算面板", canvas.transform);
            RectTransform root = panel.AddComponent<RectTransform>();
            Stretch(root);
            Image veil = panel.AddComponent<Image>();
            FormalUiEffects.ApplyBackdrop(veil, "settlement");
            FormalUiEffects.AddPageDecorations(panel.transform, "settlement", bootstrap.UiPreferences.AnimationIntensity);

            GameObject card = FormalUiKit.LayoutPanel("结算卡", panel.transform, "settlement.card", FormalUiTheme.SurfaceRaised);
            RectTransform cardRect = card.GetComponent<RectTransform>();

            AddLabel(card.transform, "标题", "战斗胜利", new Vector2(54, -48), new Vector2(1280, 54), 38, FormalUiTheme.Text, TextAnchor.MiddleLeft);
            AddLabel(card.transform, "副标题", "挑一件带走。", new Vector2(56, -112), new Vector2(1260, 34), 20, FormalUiTheme.Muted, TextAnchor.MiddleLeft);
            AddLabel(card.transform, "等级", "等级 " + run.Level + " · 经验 " + run.Experience, new Vector2(56, -166), new Vector2(1260, 34), FormalUiTheme.HeadingFontSize, FormalUiTheme.Amber, TextAnchor.MiddleLeft);
            FormalUiKit.Line(card.transform, new Vector2(56, -204), new Vector2(1260, 2), FormalUiTheme.WithAlpha(FormalUiTheme.Muted, .72f), "分隔");

            List<RogueliteReward> choices = run.CurrentFireSpellChoices.Select(AsReward).ToList();
            choices.AddRange(run.CurrentRewards.Take(3 - choices.Count));
            for (int i = 0; i < choices.Count; i++) AddRewardCard(card.transform, choices[i], i, run);
            if (choices.Count == 0)
            {
                FormalUiEffects.AddEmptyIllustration(card.transform, "empty_reward_crate", new Vector2(710, -384), 128f);
                AddLabel(card.transform, "空奖励说明", "这次没有可领取的物品。返回地图继续前进。", new Vector2(430, -476), new Vector2(560, 40),
                    FormalUiTheme.BodyFontSize, FormalUiTheme.Muted, TextAnchor.MiddleCenter);
            }

            AddLabel(card.transform, "说明", "点击想要的奖励。道具会放进行囊，术式会收进术式册。", new Vector2(56, -586), new Vector2(1180, 28), FormalUiTheme.BodyFontSize, FormalUiTheme.Muted, TextAnchor.MiddleLeft);
            CanvasGroup group = card.AddComponent<CanvasGroup>();
            UiMotionProfile motion = UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity);
            if (motion.IsImmediate)
            {
                group.alpha = 1f;
                cardRect.localScale = Vector3.one;
            }
            else
            {
                group.alpha = 0f;
                cardRect.localScale = Vector3.one * (1f - motion.ModalScaleOffset);
                DOTween.Sequence().SetUpdate(true)
                    .Join(DOTween.To(() => group.alpha, value => group.alpha = value, 1f, motion.StandardDuration))
                    .Join(cardRect.DOScale(1f, motion.ToastDuration).SetEase(FormalUiMotionTokens.StandardEase));
            }

            RewardCardInput firstAvailable = rewardCards.FirstOrDefault(item => item.Button != null && item.Button.interactable);
            if (firstAvailable != null) RuntimeUiEventSystem.Select(firstAvailable.Button.gameObject);
        }

        private void AddRewardCard(Transform parent, RogueliteReward reward, int index, RogueliteMapRun run)
        {
            GameObject card = CreateObject(index == 0 ? "reward.first" : "reward." + index, parent);
            RectTransform rect = card.AddComponent<RectTransform>();
            OccPixelUiLayoutEntry rewardLayout = OccPixelUiConfig.Layout("settlement.rewardCard");
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = rewardLayout.Position + new Vector2(index * 470, 0);
            rect.sizeDelta = rewardLayout.Size;
            Image image = card.AddComponent<Image>();
            bool weapon = reward.Kind == RogueliteRewardKind.Weapon;
            bool itemReward = reward.Kind == RogueliteRewardKind.Item;
            bool equipmentReward = reward.Kind == RogueliteRewardKind.Equipment;
            FireSpellDefinition fireSpell = FireSpellCatalog.All.FirstOrDefault(spell => spell.Id == reward.Id);
            ArtifactDefinition artifact = itemReward ? ArtifactCatalog.All.FirstOrDefault(candidate => candidate.Id == reward.Id) : null;
            UiOperationAvailability availability = RogueliteEconomyPresentation.ForReward(run, reward);
            Color accent = weapon ? FormalUiTheme.Cyan : FormalUiTheme.Amber;
            FormalUiKit.ApplySkin(image, weapon ? "panel_elevated" : "reward", Color.white);
            Button button = card.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.interactable = availability.CanExecute;
            button.onClick.AddListener(() => TryClaim(reward.Id));
            FormalUiButtonPalette palette = FormalUiButtonPalette.ForAccent(image.color, accent);
            FormalUiKit.ConfigureButtonFeedback(button, palette, () => UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity), bootstrap.ShowUiFeedback, availability.Reason);
            rewardCards.Add(new RewardCardInput
            {
                RewardId = reward.Id,
                Reward = reward,
                CanClaim = availability.CanExecute,
                Rect = rect,
                Image = image,
                Button = button,
                Normal = image.color,
                Hover = palette.Hover
            });

            UiMotionProfile motion = UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity);
            if (motion.IsImmediate) rect.localScale = Vector3.one;
            else
            {
                rect.localScale = Vector3.one * (1f - motion.ModalScaleOffset);
                rect.DOScale(1f, motion.StandardDuration).SetDelay(index * motion.QuickDuration * FormalUiMotionTokens.RewardStaggerMultiplier).SetEase(FormalUiMotionTokens.StandardEase).SetUpdate(true);
            }

            AddLabel(card.transform, "序号", "0" + (index + 1), new Vector2(24, -24), new Vector2(80, 24), 18, accent, TextAnchor.MiddleLeft);
            string iconRuntimeId = itemReward ? reward.Item.Id : reward.Id;
            Sprite rewardSprite = itemReward ? Resources.Load<Sprite>(reward.Item.IconPath) : equipmentReward ? Resources.Load<Sprite>(FormalArtRegistry.EquipmentIconPath(reward.Equipment.DefinitionId)) : fireSpell == null ? Resources.Load<Sprite>(FormalArtRegistry.ItemPath(weapon ? reward.Id : reward.Id + "_reward")) : Resources.Load<Sprite>(fireSpell.IconPath);
            if (rewardSprite == null) throw new KeyNotFoundException("Missing formal reward icon: " + iconRuntimeId);
            GameObject iconObject = CreateObject("正式奖励图标_" + iconRuntimeId, card.transform);
            RectTransform iconRect = iconObject.AddComponent<RectTransform>(); iconRect.anchorMin = iconRect.anchorMax = new Vector2(0, 1);
            iconRect.pivot = new Vector2(0, 1); iconRect.anchoredPosition = new Vector2(326, -20); iconRect.sizeDelta = new Vector2(64, 64);
            Image rewardIcon = iconObject.AddComponent<Image>(); rewardIcon.sprite = rewardSprite; rewardIcon.preserveAspect = true; rewardIcon.raycastTarget = false;
            AddLabel(card.transform, "类型", weapon ? "武器" : equipmentReward ? "装备" : itemReward ? (reward.Item.Category == ItemCategory.Artifact ? "法宝" : "卷轴") : "个人术式", new Vector2(24, -58), new Vector2(320, 28), 19, accent, TextAnchor.MiddleLeft);
            AddLabel(card.transform, "名称", reward.DisplayName, new Vector2(24, -100), new Vector2(360, 42), 29, FormalUiTheme.Text, TextAnchor.MiddleLeft);
            string stat = equipmentReward ? EquipmentSlotLabel(reward.Equipment.Slot) + " · " + HandednessLabel(reward.Equipment.Handedness) + " · 重量 " + reward.Equipment.BaseWeight + " · 以太负荷 " + reward.Equipment.BaseAetherLoad : itemReward ? reward.Item.Width + "×" + reward.Item.Height + " · " + reward.Item.MaximumUses + " 次 · 重量 " + reward.Item.Weight : weapon
                ? "伤害 " + reward.Weapon.Damage + "   射程 " + reward.Weapon.Range + "   穿甲 " + reward.Weapon.ArmorPierce
                : reward.RogueSpell != null ? reward.RogueSpell.ActionPointCost + " 行动点 · " + reward.RogueSpell.ManaCost + " 个人魔力 · 射程 " + reward.RogueSpell.Range : "伤害 " + reward.Spell.Damage + " · 射程 " + reward.Spell.Range;
            float statX = 24f;
            if (!weapon && !equipmentReward)
            {
                int actionCost = fireSpell?.ActionPointCost ?? artifact?.ActionPointCost ?? 1;
                int aetherCost = fireSpell?.ManaCost ?? reward.RogueSpell?.ManaCost ?? (artifact == null ? reward.Spell.ManaCost : 0);
                FormalUiKit.SemanticChip("action", actionCost.ToString(), card.transform, new Vector2(24, -158), tooltip);
                statX = 84f;
                if (aetherCost > 0)
                {
                    FormalUiKit.SemanticChip("aether", aetherCost.ToString(), card.transform, new Vector2(84, -158), tooltip);
                    statX = 144f;
                }
            }
            if (fireSpell != null) stat = "射程 " + fireSpell.Range + " · " + ShapeLabel(fireSpell.Shape);
            if (artifact != null)
            {
                string perUseCost = artifact.PublicCost
                    .Replace(artifact.ActionPointCost + " 行动点，", string.Empty)
                    .Replace("消耗 ", string.Empty);
                stat = "每次 " + perUseCost + " · 共 " + artifact.MaximumUses + " 次 · " + artifact.Width + "×" + artifact.Height;
            }
            AddLabel(card.transform, "数值", stat, new Vector2(statX, -158), new Vector2(384 - statX, 30), FormalUiTheme.BodyFontSize, FormalUiTheme.Muted, TextAnchor.MiddleLeft);
            string effect = weapon ? "带回工坊后可以换成主手武器\n" + RogueliteEconomyPresentation.RewardComparison(run, reward) : equipmentReward ? "放进行囊；可在战斗外装备" : itemReward ? "放进行囊" : "收进术式册；可在战斗外装入术式栏";
            if (fireSpell != null) effect = FireSpellPlayerSummary(fireSpell);
            if (artifact != null) effect = artifact.EffectSummary + "\n来源：" + artifact.Provenance + " · 目标：" + artifact.TargetSummary;
            AddLabel(card.transform, "效果", effect, new Vector2(24, -195), new Vector2(360, 44), artifact != null ? 13 : 15, FormalUiTheme.Muted, TextAnchor.UpperLeft);
            string notice = artifact == null ? null : artifact.RiskSummary;
            if (fireSpell != null && fireSpell.WeaponRequirement != FireWeaponRequirement.None)
                notice = WeaponLabel(fireSpell.WeaponRequirement) + (FireSpellCatalog.IsWeaponCompatible(fireSpell, run.EquippedWeapon) ? "；当前武器可用" : "；当前武器不相容");
            if (!string.IsNullOrWhiteSpace(notice))
            {
                FormalUiKit.SemanticChip("notice", string.Empty, card.transform, new Vector2(24, -242), tooltip);
                AddLabel(card.transform, "注意内容", notice, new Vector2(54, -240), new Vector2(330, 34), 13, FormalUiTheme.Amber, TextAnchor.UpperLeft);
            }
            string availabilityText = string.IsNullOrWhiteSpace(availability.Reason) || availability.Reason == availability.Status
                ? availability.Status : availability.Status + " · " + availability.Reason;
            AddLabel(card.transform, "选择", availabilityText, new Vector2(24, artifact != null ? -272 : -248), new Vector2(360, 24), artifact != null ? 15 : 17, availability.CanExecute ? accent : FormalUiTheme.Muted, TextAnchor.MiddleCenter);
        }

        private static string AffinityLabel(FireCombatAffinity value) => value == FireCombatAffinity.MeleeOnly ? "近战亲和" : value == FireCombatAffinity.RangedSpell ? "远程亲和" : "近远程通用";
        private static string EquipmentSlotLabel(OCC.Combat.Roguelite.EquipmentSlot value)
            => value == OCC.Combat.Roguelite.EquipmentSlot.MainHand ? "主手" : value == OCC.Combat.Roguelite.EquipmentSlot.OffHand ? "副手" :
                value == OCC.Combat.Roguelite.EquipmentSlot.Head ? "头部" : value == OCC.Combat.Roguelite.EquipmentSlot.Chest ? "胸甲" :
                value == OCC.Combat.Roguelite.EquipmentSlot.Hands ? "手部" : value == OCC.Combat.Roguelite.EquipmentSlot.Legs ? "腿部" :
                value == OCC.Combat.Roguelite.EquipmentSlot.Backpack ? "背架" : value == OCC.Combat.Roguelite.EquipmentSlot.AetherCore ? "以太核心" :
                value == OCC.Combat.Roguelite.EquipmentSlot.Conduit ? "导器" : "饰品";

        private static string HandednessLabel(OCC.Combat.Roguelite.EquipmentHandedness value)
            => value == OCC.Combat.Roguelite.EquipmentHandedness.OneHanded ? "单手" : value == OCC.Combat.Roguelite.EquipmentHandedness.TwoHanded ? "双手" :
                value == OCC.Combat.Roguelite.EquipmentHandedness.OffHand ? "副手" : "无手持要求";

        private static string DeliveryLabel(FireDeliveryMode value) => value == FireDeliveryMode.WeaponAttachment ? "武器附着" : value == FireDeliveryMode.DetachedProjection ? "远程投射" : value == FireDeliveryMode.BodyEnhancement ? "身体强化" : value == FireDeliveryMode.ContactConduction ? "接触导能" : value == FireDeliveryMode.SelfStance ? "自身架势" : value == FireDeliveryMode.TargetMarking ? "目标标记" : value == FireDeliveryMode.Movement ? "位移" : "操纵火场";
        private static string WeaponLabel(FireWeaponRequirement value) => value == FireWeaponRequirement.MeleeWeapon ? "需近战武器" : value == FireWeaponRequirement.RangedWeapon ? "需远程武器" : value == FireWeaponRequirement.AnyWeapon ? "需任意武器" : "无武器要求";
        private static string ShapeLabel(FireSelectionShape value) => value == FireSelectionShape.Single ? "单体" : value == FireSelectionShape.Line ? "直线" : value == FireSelectionShape.ContinuousLine ? "连续线" : value == FireSelectionShape.Cone ? "扇形" : value == FireSelectionShape.Cross ? "十字" : value == FireSelectionShape.OrthogonalRing ? "正交环" : value == FireSelectionShape.CenterAndOrthogonal ? "中心与正交邻格" : value == FireSelectionShape.Square3 ? "三乘三区域" : value == FireSelectionShape.AroundUnit ? "单位周边" : "路径";

        public static string FireSpellPlayerSummary(FireSpellDefinition spell)
        {
            string timing = FireTimingPlayerText(spell.TriggerWindow);
            string effects = string.Join("；", spell.Rules.Select(FireRulePlayerText));
            return timing + effects;
        }

        public static string FireSpellTargetSummary(FireSpellDefinition spell)
        {
            string target = spell.TargetKind == FireTargetKind.Self ? "自身" :
                spell.TargetKind == FireTargetKind.Enemy ? "一名敌人" :
                spell.TargetKind == FireTargetKind.AllyOrSelf ? "自身或一名友军" :
                spell.TargetKind == FireTargetKind.Unit ? "一个单位" :
                spell.TargetKind == FireTargetKind.EmptyCell ? "一个空地格" :
                spell.TargetKind == FireTargetKind.BurningUnit ? "一名燃烧单位" :
                spell.TargetKind == FireTargetKind.BurningCell ? "一处燃烧地格" :
                spell.TargetKind == FireTargetKind.Destructible ? "一处可破坏物件" :
                spell.TargetKind == FireTargetKind.AdjacentEnemy ? "一名相邻敌人" :
                spell.TargetKind == FireTargetKind.AdjacentBurningEnemy ? "一名相邻的燃烧敌人" :
                "一名燃烧或已破甲的敌人";
            return target + " · " + spell.Range + " 格 · " + ShapeLabel(spell.Shape);
        }

        public static string RogueSpellTargetSummary(OCC.Combat.Roguelite.SpellDefinition spell)
        {
            if (spell == null) return "未装备";
            FireSpellDefinition fire = FireSpellCatalog.All.FirstOrDefault(value => value.Id == spell.DefinitionId);
            if (fire != null) return FireSpellTargetSummary(fire);
            switch (spell.DefinitionId)
            {
                case "BASE-FIRE-MELEE": return "相邻可见敌人";
                case "BASE-FIRE-RANGED": return "4 格内可见敌人";
                case "BASE-AETHER-SHIELD":
                case "BASE-MANA-RECOVER": return "自身";
                default: return spell.Range > 0 ? spell.Range + " 格内亮起的目标" : "自身";
            }
        }

        public static string RogueSpellPlayerSummary(OCC.Combat.Roguelite.SpellDefinition spell)
        {
            if (spell == null) return "术式槽为空";
            FireSpellDefinition fire = FireSpellCatalog.All.FirstOrDefault(value => value.Id == spell.DefinitionId);
            if (fire != null) return FireSpellPlayerSummary(fire);
            switch (spell.DefinitionId)
            {
                case "BASE-FIRE-MELEE": return "造成 8 点近战伤害";
                case "BASE-FIRE-RANGED": return "造成 6 点火焰伤害";
                case "BASE-AETHER-SHIELD": return "自身获得 6 点普通盾";
                case "BASE-MANA-RECOVER": return "恢复 2 点个人魔力，最多恢复至 12";
                default: return "依照术式说明生效";
            }
        }

        private static string FireTimingPlayerText(FireTriggerWindow timing)
        {
            switch (timing)
            {
                case FireTriggerWindow.NextLegalWeaponAttack: return "下一次武器攻击：";
                case FireTriggerWindow.CurrentAction: return "本次行动：";
                case FireTriggerWindow.UntilNextAction: return "持续到下次行动：";
                case FireTriggerWindow.FirstAdjacentAttack: return "首次受到相邻攻击时：";
                case FireTriggerWindow.FirstMarkedTargetMove: return "标记目标首次移动时：";
                case FireTriggerWindow.FirstEnemyEntry: return "首名敌人进入时：";
                case FireTriggerWindow.AfterNextWeaponAttack: return "下一次武器攻击后：";
                default: return string.Empty;
            }
        }

        private static string FireRulePlayerText(FireSpellRule rule)
        {
            string condition = rule.Condition == FireCondition.TargetBurning ? "若目标正在燃烧，" :
                rule.Condition == FireCondition.TargetOnFireground ? "若目标位于火场，" :
                rule.Condition == FireCondition.TargetBurningAndOnFireground ? "若目标燃烧且位于火场，" :
                rule.Condition == FireCondition.TargetArmorBroken ? "若目标已破甲，" :
                rule.Condition == FireCondition.TargetBurningOrArmorBroken ? "若目标燃烧或已破甲，" :
                rule.Condition == FireCondition.SourceBurning ? "若自身正在燃烧，" :
                rule.Condition == FireCondition.SourceNotBurning ? "若自身没有燃烧，" :
                rule.Condition == FireCondition.SourceBound ? "若自身被束缚，" :
                rule.Condition == FireCondition.SourceSlowed ? "若自身处于迟缓，" :
                rule.Condition == FireCondition.SourceNotArmorBroken ? "若自身没有破甲，" :
                rule.Condition == FireCondition.LightCoverDestroyed ? "若轻掩体被摧毁，" :
                rule.Condition == FireCondition.DurabilityDepleted ? "若目标耐久归零，" : string.Empty;
            string effect;
            switch (rule.Kind)
            {
                case FireRuleKind.Damage: effect = "造成 " + rule.Amount + " 点火焰伤害"; break;
                case FireRuleKind.WeaponDamage: effect = "发动武器攻击并追加 " + rule.Amount + " 点伤害"; break;
                case FireRuleKind.ApplyBurning: effect = "施加燃烧 " + rule.Duration + " 回合"; break;
                case FireRuleKind.ExtendBurning: effect = "延长燃烧 " + rule.Duration + " 回合"; break;
                case FireRuleKind.ApplyArmorBreak: effect = "施加破甲 " + rule.Duration + " 回合"; break;
                case FireRuleKind.ApplyBreakStance: effect = "清除目标护盾并施加破势至其下一次自己回合结束"; break;
                case FireRuleKind.CreateFireground: effect = "生成持续 " + rule.Duration + " 刻度的火场"; break;
                case FireRuleKind.ExtendFireground: effect = "延长火场 " + rule.Duration + " 刻度"; break;
                case FireRuleKind.RestoreShield: effect = "恢复 " + rule.Amount + " 点护盾"; break;
                case FireRuleKind.RestoreMana: effect = "恢复 " + rule.Amount + " 点以太"; break;
                case FireRuleKind.RestoreMovement: effect = "恢复 " + rule.Amount + " 格移动力"; break;
                case FireRuleKind.AddMovement: effect = "本轮额外移动 " + rule.Amount + " 格"; break;
                case FireRuleKind.MoveSource: effect = "移动到所选位置"; break;
                case FireRuleKind.MoveAfterAttack: effect = "攻击后可移动 " + rule.Amount + " 格"; break;
                case FireRuleKind.SwapUnits: effect = "与目标交换位置"; break;
                case FireRuleKind.Push: effect = "将目标推开 " + rule.Amount + " 格"; break;
                case FireRuleKind.ReduceIncomingDamage: effect = "下一次受到的伤害减少 " + rule.Amount; break;
                case FireRuleKind.GrantShieldBeforeRanged: effect = "首次符合条件的远距伤害前获得 " + rule.Amount + " 点护盾"; break;
                case FireRuleKind.DamageDurability: effect = "对物件造成 " + rule.Amount + " 点耐久伤害"; break;
                case FireRuleKind.DestroyLightCover: effect = "摧毁轻掩体"; break;
                case FireRuleKind.ClearStatus: effect = "清除一个负面状态"; break;
                case FireRuleKind.ClearOneSelfStatus: effect = "选择并清除自身一种可清洗负面状态"; break;
                case FireRuleKind.ConsumeBurning: effect = "消耗目标的燃烧"; break;
                case FireRuleKind.ConsumeFireground: effect = "消耗目标地格的火场"; break;
                case FireRuleKind.SetBurningDuration: effect = "将燃烧调整为 " + rule.Duration + " 回合"; break;
                case FireRuleKind.ExtendTriggerToAlly: effect = "让友军也获得这次触发效果"; break;
                case FireRuleKind.LoseHealth: effect = "自身失去 " + rule.Amount + " 点生命"; break;
                case FireRuleKind.RepairWeapon: effect = "恢复武器 " + rule.Amount + " 点耐久"; break;
                case FireRuleKind.SpendActionPoints: effect = "额外消耗 " + rule.Amount + " 点行动"; break;
                case FireRuleKind.SpendMana: effect = "额外消耗 " + rule.Amount + " 点以太"; break;
                case FireRuleKind.ArmTrigger: effect = "布置一次待触发效果"; break;
                case FireRuleKind.ConsumeTrigger: effect = "触发后移除该效果"; break;
                case FireRuleKind.OverloadDevice: effect = "使装置过载" + (rule.Amount > 0 ? "，造成 " + rule.Amount + " 点效果" : string.Empty); break;
                default: effect = "产生术式效果"; break;
            }
            if (rule.AlternateAmount > 0) effect += "，满足条件时提高至 " + rule.AlternateAmount;
            return condition + effect;
        }

        private void Hide()
        {
            if (panel != null)
            {
                foreach (RewardCardInput card in rewardCards)
                {
                    card.Image?.DOKill();
                    card.Rect?.DOKill();
                }
                panel.transform.DOKill();
                if (Application.isPlaying) Destroy(panel);
                else DestroyImmediate(panel);
            }
            panel = null;
            presentedSeed = int.MinValue;
            rewardCards.Clear();
            claimPending = false;
        }

        private void TryClaim(string rewardId)
        {
            if (claimPending || bootstrap == null || string.IsNullOrWhiteSpace(rewardId)) return;
            RogueliteMapRun run = bootstrap.CurrentMapRun;
            RewardCardInput selected = rewardCards.FirstOrDefault(card => card.RewardId == rewardId);
            UiOperationAvailability availability = RogueliteEconomyPresentation.ForReward(run, selected?.Reward);
            if (!availability.CanExecute)
            {
                bootstrap.ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, availability.Reason));
                return;
            }
            claimPending = true;
            foreach (RewardCardInput card in rewardCards)
                card.Button?.GetComponent<UiButtonFeedback>()?.SetAvailability(false, "正在收好奖励");
            try
            {
                if (ShouldUseLegacyFireClaim(run, rewardId)) bootstrap.ClaimMapFireSpell(rewardId);
                else bootstrap.ClaimMapReward(rewardId);
            }
            catch (System.InvalidOperationException exception)
            {
                claimPending = false;
                foreach (RewardCardInput card in rewardCards)
                    card.Button?.GetComponent<UiButtonFeedback>()?.SetAvailability(card.CanClaim, card.CanClaim ? string.Empty : "现在不能拿走这件东西");
                bootstrap.ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, "没能拿走这件东西：" + exception.Message));
            }
        }

        private static RogueliteReward AsReward(FireSpellDefinition spell)
        {
            int damage = spell.Rules.Where(rule => rule.Kind == FireRuleKind.Damage).Select(rule => rule.Amount).FirstOrDefault();
            SkillDefinition adapter = new SkillDefinition(spell.Id, spell.DisplayName, DamageType.Fire, System.Math.Max(1, damage), spell.Range, spell.ManaCost, spell.Cooldown);
            return new RogueliteReward(spell.Id, spell.DisplayName, adapter, spell.Group.ToString());
        }

        private static bool ShouldUseLegacyFireClaim(RogueliteMapRun run, string rewardId)
        {
            return run != null && !run.UsesRogue11 &&
                run.CurrentFireSpellChoices.Any(spell => spell.Id == rewardId);
        }

        private void EnsureCanvas()
        {
            if (canvas != null) return;
            canvas = FormalUiKit.CanvasRoot("肉鸽结算UI", UiLayoutContract.SettlementSortingOrder);
            tooltip = canvas.gameObject.AddComponent<FormalHoverTooltip>();
            tooltip.Initialize(canvas);
        }

        private static void SetHover(RewardCardInput card, bool hovering)
        {
            if (card.IsHovering == hovering) return;
            card.IsHovering = hovering;
        }

        private void OnDestroy()
        {
            if (bootstrap != null) bootstrap.UiPresentationVersions.Changed -= OnPresentationChanged;
            Hide();
            if (canvas != null)
            {
                if (Application.isPlaying) Destroy(canvas.gameObject);
                else DestroyImmediate(canvas.gameObject);
            }
        }

        private static GameObject CreateObject(string name, Transform parent)
        {
            return FormalUiKit.Create(name, parent);
        }

        private static void Stretch(RectTransform rect)
        {
            FormalUiKit.Stretch(rect);
        }

        private static void AddLabel(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, Color color, TextAnchor alignment)
        {
            Text text = FormalUiKit.Label(name, value, parent, position, size, fontSize, color, alignment);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }
    }
}
