using System.Collections.Generic;
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

        private sealed class RewardCardInput
        {
            public string RewardId;
            public RectTransform Rect;
            public Image Image;
            public Color Normal;
            public Color Hover;
            public bool IsHovering;
        }

        public void Initialize(CombatPrototypeBootstrap source)
        {
            bootstrap = source;
        }

        public void RefreshNow()
        {
            RogueliteMapRun run = bootstrap == null ? null : bootstrap.CurrentMapRun;
            if (run == null || !run.AwaitingReward)
            {
                Hide();
                return;
            }

            if (panel == null || presentedSeed != run.Seed)
                Show(run);
        }

        private void Update()
        {
            RefreshNow();
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
                    bootstrap.ClaimMapReward(card.RewardId);
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
            presentedSeed = run.Seed;
            panel = CreateObject("肉鸽结算面板", canvas.transform);
            RectTransform root = panel.AddComponent<RectTransform>();
            Stretch(root);
            Image veil = panel.AddComponent<Image>();
            veil.color = new Color(.015f, .02f, .028f, .92f);

            GameObject card = CreateObject("结算卡", panel.transform);
            RectTransform cardRect = card.AddComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(.5f, .5f);
            cardRect.sizeDelta = new Vector2(1420, 650);
            Image cardImage = card.AddComponent<Image>();
            cardImage.color = new Color(.075f, .085f, .095f, 1f);
            Outline outline = card.AddComponent<Outline>(); outline.effectColor = new Color(.36f, .85f, .95f, .75f); outline.effectDistance = new Vector2(1, -1);

            AddLabel(card.transform, "标题", "行动结算  // 目标已清除", new Vector2(54, -48), new Vector2(1280, 54), 38, new Color(.86f, .94f, .96f), TextAnchor.MiddleLeft);
            AddLabel(card.transform, "副标题", "战斗记录已封存。选择一项构筑并同步至下一场战斗。", new Vector2(56, -112), new Vector2(1260, 34), 20, new Color(.60f, .68f, .72f), TextAnchor.MiddleLeft);
            AddLabel(card.transform, "等级", "等级 " + run.Level + "     经验 " + run.Experience + "     奖励选择：三选一", new Vector2(56, -166), new Vector2(1260, 34), 22, new Color(.98f, .77f, .38f), TextAnchor.MiddleLeft);
            AddLabel(card.transform, "分隔", "────────────────────────────────────────────────────────", new Vector2(56, -204), new Vector2(1260, 22), 18, new Color(.34f, .43f, .46f), TextAnchor.MiddleLeft);

            for (int i = 0; i < run.CurrentRewards.Count; i++)
                AddRewardCard(card.transform, run.CurrentRewards[i], i);

            AddLabel(card.transform, "说明", "选择后保存当前推进状态，并返回地图继续行动。奖励不会自动装备。", new Vector2(56, -586), new Vector2(1180, 28), 18, new Color(.58f, .65f, .69f), TextAnchor.MiddleLeft);
            CanvasGroup group = card.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            cardRect.localScale = Vector3.one * .94f;
            DOTween.Sequence().SetUpdate(true)
                .Join(DOTween.To(() => group.alpha, value => group.alpha = value, 1f, .18f))
                .Join(cardRect.DOScale(1f, .24f).SetEase(Ease.OutCubic));
        }

        private void AddRewardCard(Transform parent, RogueliteReward reward, int index)
        {
            GameObject card = CreateObject("奖励_" + reward.Id, parent);
            RectTransform rect = card.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(-470 + index * 470, -62);
            rect.sizeDelta = new Vector2(410, 300);
            Image image = card.AddComponent<Image>();
            bool weapon = reward.Kind == RogueliteRewardKind.Weapon;
            Color accent = weapon ? new Color(.38f, .86f, .96f) : new Color(.98f, .76f, .36f);
            image.color = new Color(.10f, .115f, .125f, 1f);
            Outline outline = card.AddComponent<Outline>(); outline.effectColor = new Color(accent.r, accent.g, accent.b, .9f); outline.effectDistance = new Vector2(1, -1);
            Button button = card.AddComponent<Button>();
            ColorBlock colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f); colors.pressedColor = new Color(.72f, .72f, .72f, 1f); button.colors = colors;
            button.onClick.AddListener(() => bootstrap.ClaimMapReward(reward.Id));
            rewardCards.Add(new RewardCardInput
            {
                RewardId = reward.Id,
                Rect = rect,
                Image = image,
                Normal = image.color,
                Hover = new Color(.16f, .19f, .20f, 1f)
            });

            AddLabel(card.transform, "序号", "0" + (index + 1), new Vector2(24, -24), new Vector2(80, 24), 18, accent, TextAnchor.MiddleLeft);
            AddLabel(card.transform, "类型", weapon ? "武器模块" : "法术模块", new Vector2(24, -58), new Vector2(320, 28), 19, accent, TextAnchor.MiddleLeft);
            AddLabel(card.transform, "名称", reward.DisplayName, new Vector2(24, -100), new Vector2(360, 42), 29, new Color(.92f, .95f, .95f), TextAnchor.MiddleLeft);
            string stat = weapon
                ? "伤害 " + reward.Weapon.Damage + "   射程 " + reward.Weapon.Range + "   穿甲 " + reward.Weapon.ArmorPierce
                : "伤害 " + reward.Spell.Damage + "   射程 " + reward.Spell.Range + "   耗能 " + reward.Spell.ManaCost;
            AddLabel(card.transform, "数值", stat, new Vector2(24, -158), new Vector2(360, 30), 18, new Color(.69f, .75f, .77f), TextAnchor.MiddleLeft);
            AddLabel(card.transform, "效果", weapon ? "替换主手武器" : "装配至技能槽 1", new Vector2(24, -195), new Vector2(360, 28), 18, new Color(.69f, .75f, .77f), TextAnchor.MiddleLeft);
            AddLabel(card.transform, "选择", "点击装配", new Vector2(24, -248), new Vector2(360, 28), 20, accent, TextAnchor.MiddleCenter);
        }

        private void Hide()
        {
            if (panel != null) Destroy(panel);
            panel = null;
            presentedSeed = int.MinValue;
            rewardCards.Clear();
        }

        private void EnsureCanvas()
        {
            if (canvas != null) return;
            GameObject root = new GameObject("肉鸽结算UI");
            DontDestroyOnLoad(root);
            canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 80;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            root.AddComponent<GraphicRaycaster>();
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject events = new GameObject("EventSystem"); DontDestroyOnLoad(events);
                events.AddComponent<EventSystem>();
            }
        }

        private static void SetHover(RewardCardInput card, bool hovering)
        {
            if (card.IsHovering == hovering) return;
            card.IsHovering = hovering;
            card.Image.DOKill(); card.Rect.DOKill();
            DOTween.To(() => card.Image.color, value => card.Image.color = value, hovering ? card.Hover : card.Normal, hovering ? .10f : .12f).SetUpdate(true);
            card.Rect.DOScale(hovering ? 1.025f : 1f, hovering ? .10f : .12f).SetUpdate(true);
        }

        private static GameObject CreateObject(string name, Transform parent)
        {
            GameObject result = new GameObject(name); result.transform.SetParent(parent, false); return result;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }

        private static void AddLabel(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, Color color, TextAnchor alignment)
        {
            GameObject textObject = CreateObject(name, parent);
            RectTransform rect = textObject.AddComponent<RectTransform>(); rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = size;
            Text text = textObject.AddComponent<Text>(); text.font = Resources.Load<Font>("Fonts/SimHei"); text.text = value; text.fontSize = fontSize; text.color = color; text.alignment = alignment; text.horizontalOverflow = HorizontalWrapMode.Overflow; text.verticalOverflow = VerticalWrapMode.Overflow;
        }
    }
}
