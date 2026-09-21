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

            bool firstEliteReward = run.IsTutorialPhase && run.FirstRunExperience.Outcome == FirstRunOutcome.EliteVictory && !run.FirstRunExperience.EliteRewardClaimed;
            AddLabel(card.transform, "标题", "战斗胜利", new Vector2(54, -48), new Vector2(1280, 54), 38, FormalUiTheme.Text, TextAnchor.MiddleLeft);
            AddLabel(card.transform, "副标题", firstEliteReward ? "选择一项被动术式；固定奖励会作为同一整包领取。" : "挑一件带走。", new Vector2(56, -112), new Vector2(1260, 34), 20, FormalUiTheme.Muted, TextAnchor.MiddleLeft);
            AddLabel(card.transform, "等级", "等级 " + run.Level + "　经验 " + run.Experience, new Vector2(56, -166), new Vector2(1260, 34), FormalUiTheme.HeadingFontSize, FormalUiTheme.Amber, TextAnchor.MiddleLeft);
            FormalUiKit.Line(card.transform, new Vector2(56, -204), new Vector2(1260, 2), FormalUiTheme.WithAlpha(FormalUiTheme.Muted, .72f), "分隔");

            List<RogueliteReward> choices = run.CurrentFireSpellChoices.Select(AsReward).ToList();
            choices.AddRange(run.CurrentRewards.Take(3 - choices.Count));
            for (int i = 0; i < choices.Count; i++) AddRewardCard(card.transform, choices[i], i, run);
            RogueliteMapNode settlementNode = run.MapNodes.FirstOrDefault(value => value.Id == run.CurrentNodeId);
            bool eventSettlement = settlementNode != null && settlementNode.Type == RogueliteMapNodeType.Event;
            int fixedGold = run.UsesRogue11 ? eventSettlement ? 1 : 3 : 0;
            int fixedContribution = run.UsesRogue11 ? eventSettlement ? 1 : 2 : 0;
            string fixedText = firstEliteReward
                ? "固定获得　低压回路护额 ×1　定锚支架 ×1（4次）　金币 " + (run.UsesRogue11 ? 6 : 0)
                : run.UsesRogue11 ? "固定所得　金币 +" + fixedGold + "　学院贡献 +" + fixedContribution : "固定所得　结算资源已写入行程";
            AddLabel(card.transform, "固定获得", fixedText, new Vector2(56, -566), new Vector2(1260, 34), 18, FormalUiTheme.Amber, TextAnchor.MiddleLeft);
            if (choices.Count == 0)
            {
                FormalUiEffects.AddEmptyIllustration(card.transform, "empty_reward_crate", new Vector2(710, -384), 128f);
                AddLabel(card.transform, "空奖励说明", run.UsesRogue11 ? "本次为资源结算，固定所得已写入行程。" : "这次没有可领取的物品。返回地图继续前进。", new Vector2(430, -476), new Vector2(560, 40),
                    FormalUiTheme.BodyFontSize, FormalUiTheme.Muted, TextAnchor.MiddleCenter);
            }

            bool needsInventory = choices.Any(value => !RogueliteEconomyPresentation.ForReward(run, value).CanExecute &&
                RogueliteEconomyPresentation.ForReward(run, value).Status.Contains("行囊"));
            if (needsInventory && run.UsesRogue11)
            {
                Button inventory = FormalUiKit.Button("整理行囊", "整理行囊", card.transform, new Vector2(56, -598), new Vector2(286, 56), FormalUiTheme.Interactive);
                inventory.onClick.AddListener(bootstrap.OpenRewardInventory);
                FormalUiKit.ConfigureButtonFeedback(inventory, FormalUiTheme.ButtonPalette(FormalUiButtonTone.Primary),
                    () => UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity), bootstrap.ShowUiFeedback);
                AddLabel(card.transform, "空间提示", "奖励会保留在这里；腾出空间后返回即可领取。", new Vector2(356, -598), new Vector2(650, 40), FormalUiTheme.BodyFontSize, FormalUiTheme.Amber, TextAnchor.MiddleLeft);
            }

            AddLabel(card.transform, "说明", "点击想要的奖励。也可以明确放弃本次全部奖励。", new Vector2(56, -606), new Vector2(900, 40), FormalUiTheme.BodyFontSize, FormalUiTheme.Muted, TextAnchor.MiddleLeft);
            Button abandon = FormalUiKit.Button("放弃奖励", "放弃奖励", card.transform, new Vector2(1030, -598), new Vector2(286, 56), FormalUiTheme.Interactive);
            abandon.onClick.AddListener(bootstrap.RequestAbandonMapReward);
            FormalUiKit.ConfigureButtonFeedback(abandon, FormalUiTheme.ButtonPalette(FormalUiButtonTone.Dangerous),
                () => UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity), bootstrap.ShowUiFeedback);
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
            bool tacticalReward = reward.Kind == RogueliteRewardKind.TacticalItem;
            string rewardCategory = weapon ? "武器" : equipmentReward ? "装备" : tacticalReward ? "战术道具" : itemReward ?
                (reward.Item.Category == ItemCategory.Artifact ? "法宝" : "物品") : "个人术式";
            FireSpellDefinition fireSpell = FireSpellCatalog.All.FirstOrDefault(spell => spell.Id == reward.Id);
            ArtifactDefinition artifact = itemReward ? ArtifactCatalog.All.FirstOrDefault(candidate => candidate.Id == reward.Id) : null;
            UiOperationAvailability availability = RogueliteEconomyPresentation.ForReward(run, reward);
            Color accent = weapon ? FormalUiTheme.Cyan : FormalUiTheme.Amber;
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = Color.Lerp(FormalUiTheme.SurfaceRaised, accent, .04f);
            AddRewardCardFrame(card.transform, rect.sizeDelta);
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

            AddLabel(card.transform, "序号", "0" + (index + 1), new Vector2(24, -16), new Vector2(56, 40), 18, accent, TextAnchor.MiddleLeft);
            string iconRuntimeId = itemReward ? reward.Item.Id : reward.Id;
            bool passiveReward = reward.RogueSpell?.Role == "passive";
            string rewardIconPath = itemReward ? reward.Item.IconPath : equipmentReward ? FormalArtRegistry.EquipmentIconPath(reward.Equipment.DefinitionId) : tacticalReward ? FormalArtRegistry.SemanticPath("notice") :
                passiveReward ? FormalArtRegistry.SemanticPath("notice") : fireSpell == null ? FormalArtRegistry.ItemPath(weapon ? reward.Id : reward.Id + "_reward") : fireSpell.IconPath;
            Sprite rewardSprite = Resources.Load<Sprite>(rewardIconPath);
            if (rewardSprite == null) throw new KeyNotFoundException("Missing formal reward icon: " + iconRuntimeId);
            GameObject iconObject = CreateObject("正式奖励图标_" + iconRuntimeId, card.transform);
            RectTransform iconRect = iconObject.AddComponent<RectTransform>(); iconRect.anchorMin = iconRect.anchorMax = new Vector2(0, 1);
            iconRect.pivot = new Vector2(0, 1); iconRect.anchoredPosition = new Vector2(326, -16); iconRect.sizeDelta = new Vector2(64, 64);
            Image rewardIcon = iconObject.AddComponent<Image>(); rewardIcon.sprite = rewardSprite; rewardIcon.preserveAspect = true; rewardIcon.raycastTarget = false;
            string typeLabel = weapon ? "武器" : equipmentReward ? "装备" : tacticalReward ? "战术道具" : itemReward ? (reward.Item.Category == ItemCategory.Artifact ? "法宝" : "卷轴") : fireSpell == null ? "个人术式" : "个人术式　" + FireSpellRarityLabel(fireSpell.Rarity);
            AddLabel(card.transform, "类型", typeLabel, new Vector2(84, -16), new Vector2(226, 40), 19, accent, TextAnchor.MiddleLeft);
            AddLabel(card.transform, "名称", reward.DisplayName, new Vector2(24, -60), new Vector2(286, 40), 29, FormalUiTheme.Text, TextAnchor.MiddleLeft);
            string stat = equipmentReward ? EquipmentSlotLabel(reward.Equipment.Slot) + "　" + HandednessLabel(reward.Equipment.Handedness) + "　重量 " + reward.Equipment.BaseWeight + "　以太负荷 " + reward.Equipment.BaseAetherLoad : tacticalReward ? reward.TacticalItem.Width + "×" + reward.TacticalItem.Height + "　完整次数 " + reward.TacticalItem.MaximumCharges + "　行动消耗 " + reward.TacticalItem.ActionPointCost : itemReward ? reward.Item.Width + "×" + reward.Item.Height + "　" + reward.Item.MaximumUses + " 次　重量 " + reward.Item.Weight : weapon
                ? "伤害 " + reward.Weapon.Damage + "   射程 " + reward.Weapon.Range + "   穿甲 " + reward.Weapon.ArmorPierce
                : reward.RogueSpell != null ? reward.RogueSpell.ActionPointCost + " 行动点　" + reward.RogueSpell.ManaCost + " 个人魔力　射程 " + reward.RogueSpell.Range : "伤害 " + reward.Spell.Damage + "　射程 " + reward.Spell.Range;
            float statX = 24f;
            if (!weapon && !equipmentReward)
            {
                int actionCost = fireSpell?.ActionPointCost ?? artifact?.ActionPointCost ?? reward.RogueSpell?.ActionPointCost ?? 1;
                int aetherCost = fireSpell?.ManaCost ?? reward.RogueSpell?.ManaCost ?? (artifact == null ? reward.Spell.ManaCost : 0);
                FormalUiKit.SemanticChip("action", actionCost.ToString(), card.transform, new Vector2(24, -104), tooltip);
                statX = 84f;
                if (aetherCost > 0)
                {
                    FormalUiKit.SemanticChip("aether", aetherCost.ToString(), card.transform, new Vector2(84, -104), tooltip);
                    statX = 144f;
                }
            }
            if (fireSpell != null) stat = CombatRangeText.RangeLine(fireSpell);
            if (artifact != null)
            {
                string perUseCost = artifact.PublicCost
                    .Replace(artifact.ActionPointCost + " 行动点，", string.Empty)
                    .Replace("消耗 ", string.Empty);
                stat = "每次 " + perUseCost + "　共 " + artifact.MaximumUses + " 次　" + artifact.Width + "×" + artifact.Height;
            }
            AddLabel(card.transform, "数值", stat, new Vector2(statX, -104), new Vector2(384 - statX, 40), FormalUiTheme.BodyFontSize, FormalUiTheme.Muted, TextAnchor.MiddleLeft);
            string effect = weapon ? "带回工坊后可以换成主手武器\n" + RogueliteEconomyPresentation.RewardComparison(run, reward) : equipmentReward || tacticalReward ? "放进行囊；可在战斗外整理" : itemReward ? "放进行囊" : "收进术式册；可在战斗外装入术式栏";
            if (fireSpell != null) effect = FireSpellPlayerSummary(fireSpell);
            if (artifact != null) effect = artifact.EffectSummary + "\n来源：" + artifact.Provenance + "\n目标：" + artifact.TargetSummary;
            AddLabel(card.transform, "完整效果", effect, new Vector2(24, -146), new Vector2(360, 92), 15, FormalUiTheme.Text, TextAnchor.UpperLeft);
            string notice = artifact == null ? null : artifact.RiskSummary;
            if (fireSpell != null && fireSpell.WeaponRequirement != FireWeaponRequirement.None)
                notice = WeaponLabel(fireSpell.WeaponRequirement) + (FireSpellCatalog.IsWeaponCompatible(fireSpell, run.EquippedWeapon) ? "；当前武器可用" : "；当前武器不相容");
            if (!string.IsNullOrWhiteSpace(notice))
            {
                FormalUiKit.SemanticChip("notice", string.Empty, card.transform, new Vector2(24, -252), tooltip);
                AddLabel(card.transform, "注意内容", notice, new Vector2(54, -250), new Vector2(330, 40), 13, FormalUiTheme.Amber, TextAnchor.UpperLeft);
            }
            string availabilityText = string.IsNullOrWhiteSpace(availability.Reason) || availability.Reason == availability.Status
                ? availability.Status : availability.Status + "，" + availability.Reason;
            AddLabel(card.transform, "选择", availabilityText, new Vector2(24, -306), new Vector2(360, 40), artifact != null ? 15 : 17, availability.CanExecute ? accent : FormalUiTheme.Muted, TextAnchor.MiddleCenter);
            // 自适应：标称布局原样保留，只有某行文字真的装不下时才把它和下面的内容一起下移。
            AdaptRewardRow(rect, card.transform, "数值", 40f);
            AdaptRewardRow(rect, card.transform, "完整效果", 92f);
            string tooltipBody = "数据　" + stat + "\n效果\n" + effect;
            if (!string.IsNullOrWhiteSpace(notice)) tooltipBody += "\n注意　" + notice;
            string comparison = RogueliteEconomyPresentation.RewardComparison(run, reward);
            if (!string.IsNullOrWhiteSpace(comparison) && !effect.Contains(comparison)) tooltipBody += "\n比较　" + comparison;
            tooltipBody += "\n领取　" + availabilityText;
            FormalHoverTooltipTrigger tooltipTrigger = card.AddComponent<FormalHoverTooltipTrigger>();
            // 奖励卡悬停同样改用方框词条（个人术式／法宝），不再输出散文式目标行。
            IReadOnlyList<string> cardTags = fireSpell != null ? CombatSpellTags.For(fireSpell)
                : artifact != null ? CombatSpellTags.For(artifact)
                : reward.RogueSpell != null ? CombatSpellTags.For(reward.RogueSpell)
                : CombatSpellTags.ForItem(reward.Item);
            bool hasTags = cardTags != null && cardTags.Count > 0;
            tooltipTrigger.Configure(tooltip, () => hasTags
                ? new FormalTooltipContent(rewardCategory, reward.DisplayName, tooltipBody, accent, rewardIconPath, cardTags)
                : new FormalTooltipContent(rewardCategory, reward.DisplayName, tooltipBody, accent, rewardIconPath));
        }

        private static string AffinityLabel(FireCombatAffinity value) => value == FireCombatAffinity.MeleeOnly ? "近战亲和" : value == FireCombatAffinity.RangedSpell ? "远程亲和" : "近远程通用";
        private static string FireSpellRarityLabel(FireSpellRarity value) => value == FireSpellRarity.Common ? "普通" : value == FireSpellRarity.Uncommon ? "罕见" : "稀有";
        private static string EquipmentSlotLabel(OCC.Combat.Roguelite.EquipmentSlot value)
            => value == OCC.Combat.Roguelite.EquipmentSlot.Weapon ? "武器" : value == OCC.Combat.Roguelite.EquipmentSlot.Head ? "头部" :
                value == OCC.Combat.Roguelite.EquipmentSlot.Chest ? "胸部" : value == OCC.Combat.Roguelite.EquipmentSlot.Feet ? "足部" :
                value == OCC.Combat.Roguelite.EquipmentSlot.Backpack ? "背部" : value == OCC.Combat.Roguelite.EquipmentSlot.Ring1 ? "戒指一" :
                value == OCC.Combat.Roguelite.EquipmentSlot.Ring2 ? "戒指二" : value == OCC.Combat.Roguelite.EquipmentSlot.Necklace ? "项链" : "施法单元";

        private static string HandednessLabel(OCC.Combat.Roguelite.EquipmentHandedness value)
            => value == OCC.Combat.Roguelite.EquipmentHandedness.OneHanded ? "单手" : value == OCC.Combat.Roguelite.EquipmentHandedness.TwoHanded ? "双手" :
                value == OCC.Combat.Roguelite.EquipmentHandedness.OffHand ? "副手" : "无手持要求";

        private static string DeliveryLabel(FireDeliveryMode value) => value == FireDeliveryMode.WeaponAttachment ? "武器附着" : value == FireDeliveryMode.DetachedProjection ? "远程投射" : value == FireDeliveryMode.BodyEnhancement ? "身体强化" : value == FireDeliveryMode.ContactConduction ? "接触导能" : value == FireDeliveryMode.SelfStance ? "自身架势" : value == FireDeliveryMode.TargetMarking ? "目标标记" : value == FireDeliveryMode.Movement ? "位移" : "操纵火场";
        private static string WeaponLabel(FireWeaponRequirement value) => value == FireWeaponRequirement.MeleeWeapon ? "需近战武器" : value == FireWeaponRequirement.RangedWeapon ? "需远程武器" : value == FireWeaponRequirement.AnyWeapon ? "需任意武器" : "无武器要求";
        private static string ShapeLabel(FireSelectionShape value) => CombatRangeText.ShapeText(value, 1);


        public static string FireSpellPlayerSummary(FireSpellDefinition spell)
        {
            if (spell.Id == "F-P-M01") return "立即：本轮额外移动 2 格；待触发：下一次合法近战武器攻击追加 8 点火焰伤害。";
            if (spell.Id == "F-P-M05") return "标记相邻敌人；其首次主动移动后，若已不相邻，施术者进入目标刚离开的格。强制位移不触发。";
            if (spell.Id == "F-P-M07") return "只能攻击相邻的燃烧敌人；造成 20 点武器伤害与 8 点火焰伤害，施加破势，然后消耗燃烧。";
            if (spell.Id == "F-P-M08") return "前方 3 层扇形依次为 1／3／5 格；范围内所有单位包括友军均承受 12 点武器伤害与 4 点火焰伤害。";
            if (spell.Id == "F-P-M12") return "获得 12 点护盾；首次相邻武器或技能攻击结算后，再获得 4 点护盾。";
            if (spell.Id == "F-P-M13") return "首次受相邻攻击后，反击 12 点武器伤害和 4 点火焰伤害。";
            if (spell.Id == "F-P-M14") return "对相邻敌人造成 8 点火焰伤害；自身被束缚时同时解除束缚。";
            if (spell.Id == "F-P-M15") return "对相邻敌人造成 8 点火焰伤害，再沿远离自身的方向推开 1 格；落点被阻挡时只结算伤害。";
            if (spell.Id == "F-P-M16") return "只能攻击相邻的燃烧敌人；造成 16 点武器伤害与 8 点火焰伤害，不消耗燃烧。";
            if (spell.Id == "F-P-M17") return "只能攻击相邻的燃烧敌人；造成 12 点火焰伤害，消耗其燃烧并令自身获得 12 点护盾。";
            if (spell.Id == "F-P-M18") return "沿连续火场移动 3 格，不触发火场伤害。";
            if (spell.Id == "F-P-M19") return "沿同一横线或竖线冲至其前一格；造成 24 点武器伤害与 12 点火焰伤害，最后无视护盾失去 8 点生命。";
            if (spell.Id == "F-P-M20") return "对燃烧或破势敌人造成 28 点武器伤害和 12 点火焰伤害；消耗燃烧。";
            if (spell.Id == "F-P-U12") return "下次命中燃烧目标时，消耗燃烧并获得 12 点护盾。";
            if (spell.Id == "F-P-U13") return "下次命中燃烧目标时，恢复 3 点个人魔力。";
            if (spell.Id == "F-P-U14") return "下次攻击燃烧目标时，追加 12 点火焰伤害。";
            if (spell.Id == "F-P-U15") return "下次攻击燃烧目标时，追加 4 点火焰伤害；燃烧延至 2 回合。";
            if (spell.Id == "F-P-U16") return "下次在当前武器最大射程命中时，武器伤害 +12；较近距离攻击不会消耗窗口。";
            if (spell.Id == "F-P-U20") return "下次攻击燃烧或破势目标时，追加 20 点火焰伤害；两者兼具时改为 28 点。";
            if (spell.Id == "F-P-U04") return "造成 8 点伤害并施加熔障标记 4 回合；摧毁时回 2 魔力和 4 护盾。";
            if (spell.Id == "F-P-U05") return "下次命中后，正交邻格受到 4 点火焰伤害；伤及友军。";
            if (spell.Id == "F-P-U06") return "下次武器攻击结算后，自动向远离攻击目标的方向后撤 1 格。";
            if (spell.Id == "F-P-U08") return "未燃烧时获得 12 点护盾；燃烧时获得 20 点护盾并清除燃烧，两条分支互斥。";
            if (spell.Id == "F-P-U09") return "清除迟缓；本回合移动恢复至 5 格。";
            if (spell.Id == "F-P-U11") return "消耗火场；恢复 2 点个人魔力。";
            if (spell.Id == "F-P-U01") return "沿四向主轴直线突进最多 3 格；起点处于至少一个敌方公开攻击范围且终点离开全部范围时，下回合行动力 +1。";
            if (spell.Id == "F-P-U18") return "直线突进 2 格；相邻单位受到 8 点伤害并被推开 1 格。";
            if (spell.Id == "F-P-U19") return "下次攻击燃烧目标后，造成 8 点火焰伤害并推开 1 格；生成火场 2 回合。";
            if (spell.Id == "F-P-R05") return "造成 16 点火焰伤害；目标已燃烧时，将燃烧提高至至少 2 回合。";
            if (spell.Id == "F-P-R13") return "生成火场 4 回合；获得火势 +4，持续 2 回合。";
            if (spell.Id == "F-P-R20") return "范围内造成 20 点火焰伤害；施加助燃 +4，生成火场 3 回合。";
            if (spell.Id == "F-P-R19") return "对目标造成 16 点火焰伤害，并对其正交邻接 1 格内所有可受击目标各造成 8 点火焰伤害，可伤友军；中心与邻格均不区分单位或物件。";
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
                spell.TargetKind == FireTargetKind.BurningEnemy ? "一名燃烧敌人" :
                spell.TargetKind == FireTargetKind.BurningCell ? "一处燃烧地格" :
                spell.TargetKind == FireTargetKind.Destructible ? "一处可破坏物件" :
                spell.TargetKind == FireTargetKind.Hittable ? "一名敌人或一处可破坏物件" :
                spell.TargetKind == FireTargetKind.AdjacentEnemy ? "一名相邻敌人" :
                spell.TargetKind == FireTargetKind.AdjacentBurningEnemy ? "一名相邻的燃烧敌人" :
                "一名燃烧或已破甲的敌人";
            return target + "　" + CombatRangeText.RangeLine(spell);
        }

        public static string FireSpellRangeText(FireSpellDefinition spell)
            => CombatRangeText.SelectionLine(spell);

        public static string RogueSpellTargetSummary(OCC.Combat.Roguelite.SpellDefinition spell)
        {
            if (spell == null) return "未装备";
            FireSpellDefinition fire = FireSpellCatalog.All.FirstOrDefault(value => value.Id == spell.DefinitionId);
            if (fire != null) return FireSpellTargetSummary(fire);
            switch (spell.DefinitionId)
            {
                case "BASE-FIRE-MELEE": return "相邻可见敌人或可破坏物件";
                case "BASE-FIRE-RANGED": return "4 格内可见敌人或可破坏物件";
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
                case "BASE-FIRE-MELEE": return "对单位或物件造成 8 点火焰伤害";
                case "BASE-FIRE-RANGED": return "对单位或物件造成 6 点火焰伤害";
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
            // 破障是词条标记（总案 3.5.6.1）：卡面只写可读结果，不暴露内部枚举名。
            if (rule.Kind == FireRuleKind.BreakBarrier) return "破障：对物件造成双倍耐久伤害";
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
                rule.Condition == FireCondition.TargetAtWeaponMaxRange ? "若目标位于当前武器最大射程，" :
                rule.Condition == FireCondition.LightCoverDestroyed ? "若轻掩体被摧毁，" :
                rule.Condition == FireCondition.DurabilityDepleted ? "若目标耐久归零，" : string.Empty;
            string effect;
            switch (rule.Kind)
            {
                case FireRuleKind.Damage: effect = "造成 " + rule.Amount + " 点火焰伤害"; break;
                case FireRuleKind.WeaponDamage: effect = "武器伤害 +" + rule.Amount; break;
                case FireRuleKind.ApplyBurning: effect = "施加燃烧 " + rule.Duration + " 回合"; break;
                case FireRuleKind.ExtendBurning: effect = "燃烧至少延至 " + rule.Duration + " 回合"; break;
                case FireRuleKind.ApplyArmorBreak: effect = "施加破甲 " + rule.Duration + " 回合"; break;
                case FireRuleKind.ApplyBreakStance: effect = "清空护盾，施加破势"; break;
                case FireRuleKind.CreateFireground: effect = "生成火场 " + rule.Duration + " 回合"; break;
                case FireRuleKind.ExtendFireground: effect = "火场延长 " + rule.Duration + " 回合"; break;
                case FireRuleKind.ApplyFiregroundBoost: effect = "获得火势 +" + rule.Amount + "，持续 " + rule.Duration + " 回合"; break;
                case FireRuleKind.ApplyFiregroundVulnerability: effect = "施加助燃 +" + rule.Amount + "，持续 " + rule.Duration + " 回合"; break;
                case FireRuleKind.RestoreShield: effect = "恢复 " + rule.Amount + " 点护盾"; break;
                case FireRuleKind.RestoreMana: effect = "恢复 " + rule.Amount + " 点以太"; break;
                case FireRuleKind.RestoreMovement: effect = "恢复 " + rule.Amount + " 步"; break;
                case FireRuleKind.AddMovement: effect = "本轮额外移动 " + rule.Amount + " 格"; break;
                case FireRuleKind.MoveSource: effect = "移至目标格"; break;
                case FireRuleKind.MoveAfterAttack: effect = "攻击后可移动 " + rule.Amount + " 格"; break;
                case FireRuleKind.SwapUnits: effect = "与目标交换位置"; break;
                case FireRuleKind.Push: effect = "将目标推开 " + rule.Amount + " 格"; break;
                case FireRuleKind.PushAllUnits: effect = "将范围内单位推开 " + rule.Amount + " 格"; break;
                case FireRuleKind.ApplyMeltBarrierMark: effect = "施加持续 " + rule.Duration + " 回合的熔障标记"; break;
                case FireRuleKind.ReserveNextTurnAction: effect = "脱离全部敌方攻击范围时，下回合行动力 +" + rule.Amount; break;
                case FireRuleKind.ReduceIncomingDamage: effect = "下一次受到的伤害减少 " + rule.Amount; break;
                case FireRuleKind.GrantShieldBeforeRanged: effect = "首次符合条件的远距伤害前获得 " + rule.Amount + " 点护盾"; break;
                case FireRuleKind.DamageDurability: effect = "对物件造成 " + rule.Amount + " 点耐久伤害"; break;
                case FireRuleKind.DestroyLightCover: effect = "摧毁轻掩体"; break;
                case FireRuleKind.ClearStatus: effect = "清除一个负面状态"; break;
                case FireRuleKind.ClearOneSelfStatus: effect = "清除自身一种负面状态"; break;
                case FireRuleKind.ConsumeBurning: effect = "消耗目标的燃烧"; break;
                case FireRuleKind.ConsumeFireground: effect = "消耗目标地格的火场"; break;
                case FireRuleKind.SetBurningDuration: effect = "将燃烧调整为 " + rule.Duration + " 回合"; break;
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

        public void HideForInventory()
        {
            Hide();
            hasPresentedModel = false;
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
            SkillDefinition adapter = new SkillDefinition(spell.Id, spell.DisplayName, DamageType.Fire, System.Math.Max(1, damage), spell.Range, spell.ManaCost, spell.Cooldown, shape: spell.Shape);
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

        private static void AddRewardCardFrame(Transform parent, Vector2 size)
        {
            AddRewardFrameEdge(parent, "奖励细框_上", Vector2.zero, new Vector2(size.x, 2f));
            AddRewardFrameEdge(parent, "奖励细框_下", new Vector2(0f, -size.y + 2f), new Vector2(size.x, 2f));
            AddRewardFrameEdge(parent, "奖励细框_左", Vector2.zero, new Vector2(2f, size.y));
            AddRewardFrameEdge(parent, "奖励细框_右", new Vector2(size.x - 2f, 0f), new Vector2(2f, size.y));
        }

        /// <summary>
        /// Lets one text row of the settlement card stretch past its nominal height. Everything anchored below
        /// that row moves down by the same delta, the card grows by it with the top edge pinned, and the frame
        /// is recomputed. Nominal content therefore keeps the designed layout untouched; only a row that really
        /// needs more room (a long 选取／作用 line, a three-line artifact effect) raises the card.
        /// </summary>
        private static void AdaptRewardRow(RectTransform card, Transform cardRoot, string rowName, float nominalHeight)
        {
            Transform row = cardRoot.Find(rowName);
            Text text = row == null ? null : row.GetComponent<Text>();
            RectTransform rowRect = row as RectTransform;
            if (text == null || rowRect == null) return;
            rowRect.sizeDelta = new Vector2(rowRect.sizeDelta.x, nominalHeight);
            Canvas.ForceUpdateCanvases();
            float needed = Mathf.Ceil(text.preferredHeight);
            float delta = needed - nominalHeight;
            if (delta <= 0f) return;
            rowRect.sizeDelta = new Vector2(rowRect.sizeDelta.x, needed);
            // Only what sits strictly below this row's bottom edge moves; the action/aether chips that share
            // the 数值 row keep their place.
            float rowBottom = rowRect.anchoredPosition.y - nominalHeight;
            for (int i = 0; i < cardRoot.childCount; i++)
            {
                RectTransform child = cardRoot.GetChild(i) as RectTransform;
                if (child == null || child == rowRect) continue;
                if (child.anchoredPosition.y >= rowBottom) continue;
                child.anchoredPosition -= new Vector2(0f, delta);
            }
            Vector2 size = card.sizeDelta + new Vector2(0f, delta);
            card.sizeDelta = size;
            card.anchoredPosition -= new Vector2(0f, delta * .5f);
            ResizeRewardCardFrame(cardRoot, size);
        }

        private static void ResizeRewardCardFrame(Transform parent, Vector2 size)
        {
            ResizeRewardFrameEdge(parent, "奖励细框_上", Vector2.zero, new Vector2(size.x, 2f));
            ResizeRewardFrameEdge(parent, "奖励细框_下", new Vector2(0f, -size.y + 2f), new Vector2(size.x, 2f));
            ResizeRewardFrameEdge(parent, "奖励细框_左", Vector2.zero, new Vector2(2f, size.y));
            ResizeRewardFrameEdge(parent, "奖励细框_右", new Vector2(size.x - 2f, 0f), new Vector2(2f, size.y));
        }

        private static void ResizeRewardFrameEdge(Transform parent, string name, Vector2 position, Vector2 size)
        {
            RectTransform edge = parent.Find(name) as RectTransform;
            if (edge == null) return;
            edge.anchoredPosition = position;
            edge.sizeDelta = size;
        }

        private static void AddRewardFrameEdge(Transform parent, string name, Vector2 position, Vector2 size)
        {
            Image edge = FormalUiKit.FlatPanel(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f),
                position, size, FormalUiTheme.Rule).GetComponent<Image>();
            edge.raycastTarget = false;
            edge.transform.SetAsLastSibling();
        }
    }
}
