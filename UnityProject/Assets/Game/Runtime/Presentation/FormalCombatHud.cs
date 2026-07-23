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
        private readonly Color ink = new Color(.018f, .026f, .034f, .98f);
        private readonly Color panel = new Color(.045f, .058f, .068f, .98f);
        private readonly Color line = new Color(.30f, .78f, .88f, .82f);
        private readonly Color muted = new Color(.57f, .64f, .68f, 1f);
        private readonly Color text = new Color(.90f, .94f, .95f, 1f);
        private readonly Dictionary<string, Button> actionButtons = new Dictionary<string, Button>();
        private CombatPrototypeBootstrap bootstrap;
        private Canvas canvas;
        private GameObject root;
        private Text activeLabel;
        private Text weaponLabel;
        private Text eventLabel;
        private Text targetLabel;
        private Text[] timeline = new Text[4];
        private Image healthFill;
        private Image shieldFill;
        private Image manaFill;
        private GameObject outcomeOverlay;
        private Text outcomeTitle;
        private Text[] quickbarLabels = new Text[4];
        private float displayedHealth = -1f;
        private float displayedShield = -1f;
        private float displayedMana = -1f;

        public void Initialize(CombatPrototypeBootstrap source)
        {
            bootstrap = source;
            EnsureUi();
        }

        private void Update()
        {
            if (root == null || bootstrap == null) return;
            bool visible = bootstrap.IsDeveloperCombatActive || bootstrap.IsCombatOutcomeVisible;
            if (root.activeSelf != visible) root.SetActive(visible);
            if (!visible || bootstrap.CurrentState == null) return;
            Refresh();
        }

        private void EnsureUi()
        {
            if (root != null) return;
            root = new GameObject("正式战斗HUD");
            DontDestroyOnLoad(root);
            canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 45;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            root.AddComponent<GraphicRaycaster>();
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject events = new GameObject("EventSystem");
                DontDestroyOnLoad(events);
                events.AddComponent<EventSystem>();
            }

            GameObject top = Panel("战斗抬头", root.transform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0, -14), new Vector2(1872, 56), ink);
            Label("OCC // 战术行动", top.transform, new Vector2(20, -10), new Vector2(600, 34), 22, text, TextAnchor.MiddleLeft);
            Label("现场链路稳定  /  F1 开发控制台", top.transform, new Vector2(1330, -10), new Vector2(500, 34), 16, muted, TextAnchor.MiddleRight);
            Line(top.transform, new Vector2(18, -53), new Vector2(1836, 2), line);

            GameObject side = Panel("战术读数", root.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-18, -88), new Vector2(438, 770), panel);
            Label("战术读数", side.transform, new Vector2(24, -22), new Vector2(360, 30), 22, text, TextAnchor.MiddleLeft);
            Line(side.transform, new Vector2(24, -60), new Vector2(390, 2), line);
            activeLabel = Label("行动状态", side.transform, new Vector2(24, -78), new Vector2(390, 48), 18, text, TextAnchor.UpperLeft);
            weaponLabel = Label("装备状态", side.transform, new Vector2(24, -132), new Vector2(390, 42), 16, muted, TextAnchor.UpperLeft);
            targetLabel = Label("目标状态", side.transform, new Vector2(24, -176), new Vector2(390, 36), 15, new Color(.95f, .76f, .36f), TextAnchor.UpperLeft);
            healthFill = ResourceBar(side.transform, "结构", new Vector2(24, -224), new Color(.32f, .82f, .56f));
            shieldFill = ResourceBar(side.transform, "护盾", new Vector2(24, -288), new Color(.44f, .72f, .63f));
            manaFill = ResourceBar(side.transform, "以太", new Vector2(24, -352), line);
            Label("行动序列", side.transform, new Vector2(24, -426), new Vector2(390, 26), 17, text, TextAnchor.MiddleLeft);
            for (int i = 0; i < timeline.Length; i++) timeline[i] = Label("序列" + i, side.transform, new Vector2(24, -458 - i * 42), new Vector2(390, 34), 16, muted, TextAnchor.MiddleLeft);
            Label("现场记录", side.transform, new Vector2(24, -638), new Vector2(390, 26), 17, text, TextAnchor.MiddleLeft);
            eventLabel = Label("记录", side.transform, new Vector2(24, -670), new Vector2(390, 70), 15, muted, TextAnchor.UpperLeft);

            GameObject bottom = Panel("战术指令", root.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 18), new Vector2(1400, 148), ink);
            Label("战术指令", bottom.transform, new Vector2(20, -14), new Vector2(300, 26), 17, muted, TextAnchor.MiddleLeft);
            string[] actions = { "移动", "攻击", "技能1", "技能2", "搜刮", "互动" };
            for (int i = 0; i < actions.Length; i++)
            {
                string action = actions[i];
                Button button = Button(bottom.transform, action, new Vector2(20 + i * 152, -48), new Vector2(138, 70), action, new Color(.055f, .08f, .09f, 1f));
                button.onClick.AddListener(() => bootstrap.SelectHudAction(action));
                actionButtons.Add(action, button);
            }
            Button endTurn = Button(bottom.transform, "结束行动", new Vector2(950, -48), new Vector2(190, 70), "结束行动", new Color(.10f, .16f, .17f, 1f));
            endTurn.onClick.AddListener(() => bootstrap.EndHeroTurn());
            Button console = Button(bottom.transform, "开发控制台", new Vector2(1160, -48), new Vector2(210, 70), "开发控制台  F1", new Color(.12f, .105f, .055f, 1f));
            console.onClick.AddListener(bootstrap.ToggleDeveloperConsole);
            for (int i = 0; i < quickbarLabels.Length; i++)
            {
                int slot = i;
                Button quick = Button(bottom.transform, "快捷栏" + i, new Vector2(20 + i * 112, 12), new Vector2(98, 28), "", new Color(.07f, .075f, .07f, 1f));
                quickbarLabels[i] = quick.GetComponentInChildren<Text>();
                quick.onClick.AddListener(() => bootstrap.UseQuickbarSlot(slot));
            }
            CreateOutcomeOverlay();
        }

        private void Refresh()
        {
            CombatState state = bootstrap.CurrentState;
            UnitState hero = state.GetUnit("hero");
            UnitState active = state.GetUnit(state.ActiveUnitId);
            activeLabel.text = "行动单位  " + (active == null ? "等待" : active.DisplayName) + "\n行动点  " + (active == null ? "--" : active.ActionPoints.ToString());
            weaponLabel.text = "主手  " + hero.MainHand.DisplayName + "\n以太回路  " + hero.Mana + " / " + hero.MaxMana + "   " + StatusText(hero);
            UnitState target = state.Units.Values.FirstOrDefault(unit => !unit.IsHero && unit.IsAlive && unit.Id == bootstrap.SelectedTargetId);
            targetLabel.text = target == null ? "目标  未锁定" : "目标  " + target.DisplayName + "  //  " + target.Health + " HP  护盾 " + target.Shield;
            SetBar(healthFill, hero.Health / (float)Math.Max(1, hero.MaxHealth), ref displayedHealth);
            SetBar(shieldFill, hero.Shield / (float)Math.Max(1, hero.MaxShield), ref displayedShield);
            SetBar(manaFill, hero.Mana / (float)Math.Max(1, hero.MaxMana), ref displayedMana);
            UnitState[] units = state.Units.Values.Where(unit => unit.IsAlive).OrderBy(unit => unit.InitiativeTime).Take(4).ToArray();
            for (int i = 0; i < timeline.Length; i++)
            {
                timeline[i].text = i < units.Length ? (units[i].Id == state.ActiveUnitId ? "▶ " : "   ") + units[i].DisplayName + "  // " + units[i].Health + " HP" : "";
                timeline[i].color = i < units.Length && units[i].Id == state.ActiveUnitId ? line : muted;
            }
            eventLabel.text = state.EventLog.Count == 0 ? "等待战术指令。" : state.EventLog[0];
            for (int i = 0; i < quickbarLabels.Length; i++)
                quickbarLabels[i].text = state.Quickbar[i] == null ? (i + 1) + "  空" : (i + 1) + "  " + state.Quickbar[i].DisplayName;
            bool outcome = bootstrap.IsCombatOutcomeVisible;
            outcomeOverlay.SetActive(outcome);
            if (outcome) outcomeTitle.text = bootstrap.CurrentState.IsVictory ? "任务完成" : "行动中止";
            foreach (KeyValuePair<string, Button> pair in actionButtons)
            {
                Image image = pair.Value.GetComponent<Image>();
                image.color = pair.Key == bootstrap.SelectedAction ? new Color(.10f, .31f, .35f, 1f) : new Color(.055f, .08f, .09f, 1f);
            }
        }

        private void CreateOutcomeOverlay()
        {
            outcomeOverlay = Panel("战斗结果", root.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(720, 330), new Color(.012f, .018f, .024f, .97f));
            outcomeTitle = Label("结果标题", outcomeOverlay.transform, new Vector2(40, -34), new Vector2(640, 58), 36, text, TextAnchor.MiddleCenter);
            Label("结果说明", outcomeOverlay.transform, new Vector2(40, -102), new Vector2(640, 34), 17, muted, TextAnchor.MiddleCenter).text = "战术记录已封存。请选择下一步。";
            Button restart = Button(outcomeOverlay.transform, "结果重开", new Vector2(60, -180), new Vector2(280, 64), "战术重开", new Color(.08f, .20f, .22f, 1f));
            restart.onClick.AddListener(bootstrap.TacticalRestartDeveloperCombat);
            Button back = Button(outcomeOverlay.transform, "结果返回", new Vector2(380, -180), new Vector2(280, 64), "返回入口", new Color(.12f, .10f, .06f, 1f));
            back.onClick.AddListener(bootstrap.ReturnToDeveloperMenu);
            outcomeOverlay.SetActive(false);
        }

        private Image ResourceBar(Transform parent, string title, Vector2 position, Color color)
        {
            Label(title, parent, position, new Vector2(200, 22), 15, muted, TextAnchor.MiddleLeft);
            GameObject track = Panel(title + "轨道", parent, new Vector2(0, 1), new Vector2(0, 1), position + new Vector2(0, -28), new Vector2(390, 15), new Color(.015f, .02f, .026f, 1f));
            GameObject fill = Panel(title + "填充", track.transform, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero, color);
            RectTransform rect = fill.GetComponent<RectTransform>();
            rect.anchorMax = new Vector2(1, 1);
            return fill.GetComponent<Image>();
        }

        private static void SetBar(Image fill, float value, ref float displayed)
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(displayed, value)) return;
            RectTransform rect = fill.rectTransform;
            rect.DOKill();
            DOTween.To(() => rect.anchorMax.x, next => rect.anchorMax = new Vector2(next, 1f), value, .16f).SetEase(Ease.OutQuad).SetUpdate(true);
            displayed = value;
        }

        private static string StatusText(UnitState unit)
        {
            return unit.Statuses.Count == 0 ? "状态正常" : string.Join(" ", unit.Statuses.Select(item => item.Key + " " + item.Value));
        }

        private static GameObject Panel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color color)
        {
            GameObject obj = new GameObject(name); obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>(); rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = anchorMax; rect.anchoredPosition = position; rect.sizeDelta = size;
            Image image = obj.AddComponent<Image>(); image.color = color; return obj;
        }

        private static Text Label(string name, Transform parent, Vector2 position, Vector2 size, int fontSize, Color color, TextAnchor alignment)
        {
            GameObject obj = new GameObject(name); obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = size;
            Text label = obj.AddComponent<Text>(); label.font = Resources.Load<Font>("Fonts/SimHei"); label.fontSize = fontSize; label.color = color; label.alignment = alignment; label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Overflow; return label;
        }

        private static void Line(Transform parent, Vector2 position, Vector2 size, Color color) { Panel("细分隔", parent, new Vector2(0, 1), new Vector2(0, 1), position, size, color); }

        private static Button Button(Transform parent, string name, Vector2 position, Vector2 size, string title, Color color)
        {
            GameObject obj = Panel(name, parent, new Vector2(0, 1), new Vector2(0, 1), position, size, color);
            Button button = obj.AddComponent<Button>();
            Text label = Label("文字", obj.transform, new Vector2(0, 0), size, 16, new Color(.90f, .95f, .96f), TextAnchor.MiddleCenter);
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one; label.rectTransform.pivot = new Vector2(.5f, .5f); label.rectTransform.anchoredPosition = Vector2.zero; label.rectTransform.sizeDelta = Vector2.zero; label.text = title;
            return button;
        }
    }
}
