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
        private CombatPrototypeBootstrap bootstrap;
        private Canvas canvas;
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
            public RectTransform Rect;
            public Image Image;
            public Color Normal;
            public Color Hover;
            public bool IsHovering;
            public Button Button;
        }

        public void Initialize(CombatPrototypeBootstrap source)
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

            if (panel == null || presentedSeed != run.Seed)
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
                if (hovering && Event.current.type == EventType.MouseDown && Event.current.button == 0)
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
            veil.color = new Color(.015f, .02f, .028f, .92f);

            GameObject card = FormalUiKit.LayoutPanel("结算卡", panel.transform, "settlement.card", new Color(.82f, .90f, .92f, 1f));
            RectTransform cardRect = card.GetComponent<RectTransform>();

            AddLabel(card.transform, "标题", "行动结算  // 目标已清除", new Vector2(54, -48), new Vector2(1280, 54), 38, new Color(.86f, .94f, .96f), TextAnchor.MiddleLeft);
            AddLabel(card.transform, "副标题", "战斗记录已封存。选择一项构筑并同步至下一场战斗。", new Vector2(56, -112), new Vector2(1260, 34), 20, new Color(.60f, .68f, .72f), TextAnchor.MiddleLeft);
            AddLabel(card.transform, "等级", "等级 " + run.Level + "     经验 " + run.Experience + "     奖励选择：三选一", new Vector2(56, -166), new Vector2(1260, 34), 22, new Color(.98f, .77f, .38f), TextAnchor.MiddleLeft);
            AddLabel(card.transform, "分隔", "────────────────────────────────────────────────────────", new Vector2(56, -204), new Vector2(1260, 22), 18, new Color(.34f, .43f, .46f), TextAnchor.MiddleLeft);

            List<RogueliteReward> choices = run.CurrentFireSpellChoices.Select(AsReward).ToList();
            choices.AddRange(run.CurrentRewards.Take(3 - choices.Count));
            for (int i = 0; i < choices.Count; i++) AddRewardCard(card.transform, choices[i], i, run);

            AddLabel(card.transform, "说明", "选择后保存当前推进状态，并返回地图继续行动。奖励不会自动装备。", new Vector2(56, -586), new Vector2(1180, 28), 18, new Color(.58f, .65f, .69f), TextAnchor.MiddleLeft);
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

            if (rewardCards.Count > 0) RuntimeUiEventSystem.Select(rewardCards[0].Button.gameObject);
        }

        private void AddRewardCard(Transform parent, RogueliteReward reward, int index, RogueliteMapRun run)
        {
            GameObject card = CreateObject("奖励_" + reward.Id, parent);
            RectTransform rect = card.AddComponent<RectTransform>();
            OccPixelUiLayoutEntry rewardLayout = OccPixelUiConfig.Layout("settlement.rewardCard");
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = rewardLayout.Position + new Vector2(index * 470, 0);
            rect.sizeDelta = rewardLayout.Size;
            Image image = card.AddComponent<Image>();
            bool weapon = reward.Kind == RogueliteRewardKind.Weapon;
            bool itemReward = reward.Kind == RogueliteRewardKind.Item;
            FireSpellDefinition fireSpell = FireSpellCatalog.All.FirstOrDefault(spell => spell.Id == reward.Id);
            ArtifactDefinition artifact = itemReward ? ArtifactCatalog.All.FirstOrDefault(candidate => candidate.Id == reward.Id) : null;
            Color accent = weapon ? new Color(.38f, .86f, .96f) : itemReward ? FormalUiTheme.Amber : new Color(.98f, .76f, .36f);
            FormalUiKit.ApplySkin(image, weapon ? "panel_elevated" : "reward", Color.white);
            Button button = card.AddComponent<Button>();
            ColorBlock colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f); colors.pressedColor = new Color(.72f, .72f, .72f, 1f); button.colors = colors;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => TryClaim(reward.Id));
            FormalUiButtonPalette palette = new FormalUiButtonPalette(image.color, new Color(.16f, .19f, .20f, 1f),
                new Color(.07f, .08f, .09f, 1f), new Color(.16f, .19f, .20f, 1f), FormalUiTheme.Disabled);
            FormalUiKit.ConfigureButtonFeedback(button, palette, () => UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity), bootstrap.ShowUiFeedback);
            rewardCards.Add(new RewardCardInput
            {
                RewardId = reward.Id,
                Rect = rect,
                Image = image,
                Button = button,
                Normal = image.color,
                Hover = new Color(.16f, .19f, .20f, 1f)
            });

            UiMotionProfile motion = UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity);
            if (motion.IsImmediate) rect.localScale = Vector3.one;
            else
            {
                rect.localScale = Vector3.one * (1f - motion.ModalScaleOffset);
                rect.DOScale(1f, motion.StandardDuration).SetDelay(index * motion.QuickDuration * FormalUiMotionTokens.RewardStaggerMultiplier).SetEase(FormalUiMotionTokens.StandardEase).SetUpdate(true);
            }

            AddLabel(card.transform, "序号", "0" + (index + 1), new Vector2(24, -24), new Vector2(80, 24), 18, accent, TextAnchor.MiddleLeft);
            string iconRuntimeId = itemReward ? reward.Item.Id : weapon ? reward.Id : reward.Id + "_reward";
            Sprite rewardSprite = itemReward ? Resources.Load<Sprite>(reward.Item.IconPath) : fireSpell == null ? Resources.Load<Sprite>(FormalArtRegistry.ItemPath(iconRuntimeId)) : Resources.Load<Sprite>(fireSpell.IconPath);
            if (rewardSprite == null) throw new KeyNotFoundException("Missing formal reward icon: " + iconRuntimeId);
            GameObject iconObject = CreateObject("正式奖励图标_" + iconRuntimeId, card.transform);
            RectTransform iconRect = iconObject.AddComponent<RectTransform>(); iconRect.anchorMin = iconRect.anchorMax = new Vector2(0, 1);
            iconRect.pivot = new Vector2(0, 1); iconRect.anchoredPosition = new Vector2(326, -20); iconRect.sizeDelta = new Vector2(56, 56);
            Image rewardIcon = iconObject.AddComponent<Image>(); rewardIcon.sprite = rewardSprite; rewardIcon.preserveAspect = true; rewardIcon.raycastTarget = false;
            AddLabel(card.transform, "类型", weapon ? "武器模块" : itemReward ? (reward.Item.Category == ItemCategory.Artifact ? "法宝" : "卷轴") : "法术模块", new Vector2(24, -58), new Vector2(320, 28), 19, accent, TextAnchor.MiddleLeft);
            AddLabel(card.transform, "名称", reward.DisplayName, new Vector2(24, -100), new Vector2(360, 42), 29, new Color(.92f, .95f, .95f), TextAnchor.MiddleLeft);
            string stat = itemReward ? reward.Item.Width + "×" + reward.Item.Height + "   次数 " + reward.Item.MaximumUses + "   重量 " + reward.Item.Weight : weapon
                ? "伤害 " + reward.Weapon.Damage + "   射程 " + reward.Weapon.Range + "   穿甲 " + reward.Weapon.ArmorPierce
                : "伤害 " + reward.Spell.Damage + "   射程 " + reward.Spell.Range + "   耗能 " + reward.Spell.ManaCost;
            if (fireSpell != null) stat = fireSpell.ActionPointCost + " AP   魔力 " + fireSpell.ManaCost + "   射程 " + fireSpell.Range + "   " + ShapeLabel(fireSpell.Shape);
            if (artifact != null) stat = artifact.PublicCost + "   " + artifact.MaximumUses + " 次   " + artifact.Width + "×" + artifact.Height + "   重量 " + artifact.Weight;
            AddLabel(card.transform, "数值", stat, new Vector2(24, -158), new Vector2(360, 30), 18, new Color(.69f, .75f, .77f), TextAnchor.MiddleLeft);
            string effect = weapon ? "工坊更换主手武器" : itemReward ? "放入 6×10 背包" : "收入术式库；仅可在工坊装备";
            if (fireSpell != null)
                effect = AffinityLabel(fireSpell.CombatAffinity) + " / " + DeliveryLabel(fireSpell.DeliveryMode) + " / " + WeaponLabel(fireSpell.WeaponRequirement) +
                    (FireSpellCatalog.IsWeaponCompatible(fireSpell, run.EquippedWeapon) ? " / 当前相容" : " / 当前不相容");
            if (artifact != null) effect = "来源：" + artifact.Provenance + "\n目标：" + artifact.TargetSummary + "\n效果：" + artifact.EffectSummary + "\n风险：" + artifact.RiskSummary;
            AddLabel(card.transform, "效果", effect, new Vector2(24, -195), artifact != null ? new Vector2(360, 76) : new Vector2(360, 42), artifact != null ? 13 : 16, new Color(.69f, .75f, .77f), TextAnchor.UpperLeft);
            AddLabel(card.transform, "选择", "点击收取", new Vector2(24, artifact != null ? -272 : -248), new Vector2(360, 24), artifact != null ? 17 : 20, accent, TextAnchor.MiddleCenter);
        }

        private static string AffinityLabel(FireCombatAffinity value) => value == FireCombatAffinity.MeleeOnly ? "近战亲和" : value == FireCombatAffinity.RangedSpell ? "远程亲和" : "近远程通用";
        private static string DeliveryLabel(FireDeliveryMode value) => value == FireDeliveryMode.WeaponAttachment ? "武器附着" : value == FireDeliveryMode.DetachedProjection ? "远程投射" : value == FireDeliveryMode.BodyEnhancement ? "身体强化" : value == FireDeliveryMode.ContactConduction ? "接触导能" : value == FireDeliveryMode.SelfStance ? "自身架势" : value == FireDeliveryMode.TargetMarking ? "目标标记" : value == FireDeliveryMode.Movement ? "位移" : "火场调度";
        private static string WeaponLabel(FireWeaponRequirement value) => value == FireWeaponRequirement.MeleeWeapon ? "需近战武器" : value == FireWeaponRequirement.RangedWeapon ? "需远程武器" : value == FireWeaponRequirement.AnyWeapon ? "需任意武器" : "无武器要求";
        private static string ShapeLabel(FireSelectionShape value) => value == FireSelectionShape.Single ? "单体" : value == FireSelectionShape.Line ? "直线" : value == FireSelectionShape.ContinuousLine ? "连续线" : value == FireSelectionShape.Cone ? "扇形" : value == FireSelectionShape.Cross ? "十字" : value == FireSelectionShape.OrthogonalRing ? "正交环" : value == FireSelectionShape.CenterAndOrthogonal ? "中心与正交邻格" : value == FireSelectionShape.Square3 ? "三乘三区域" : value == FireSelectionShape.AroundUnit ? "单位周边" : "路径";

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
                Destroy(panel);
            }
            panel = null;
            presentedSeed = int.MinValue;
            rewardCards.Clear();
            claimPending = false;
        }

        private void TryClaim(string rewardId)
        {
            if (claimPending || bootstrap == null || string.IsNullOrWhiteSpace(rewardId)) return;
            claimPending = true;
            foreach (RewardCardInput card in rewardCards)
                card.Button?.GetComponent<UiButtonFeedback>()?.SetAvailability(false, "奖励选择正在结算");
            if (FireSpellCatalog.All.Any(spell => spell.Id == rewardId)) bootstrap.ClaimMapFireSpell(rewardId);
            else bootstrap.ClaimMapReward(rewardId);
        }

        private static RogueliteReward AsReward(FireSpellDefinition spell)
        {
            int damage = spell.Rules.Where(rule => rule.Kind == FireRuleKind.Damage).Select(rule => rule.Amount).FirstOrDefault();
            SkillDefinition adapter = new SkillDefinition(spell.Id, spell.DisplayName, DamageType.Fire, System.Math.Max(1, damage), spell.Range, spell.ManaCost, spell.Cooldown);
            return new RogueliteReward(spell.Id, spell.DisplayName, adapter, spell.Group.ToString());
        }

        private void EnsureCanvas()
        {
            if (canvas != null) return;
            canvas = FormalUiKit.CanvasRoot("肉鸽结算UI", UiLayoutContract.SettlementSortingOrder);
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
            if (canvas != null) Destroy(canvas.gameObject);
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
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }
    }
}
