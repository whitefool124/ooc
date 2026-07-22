using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // The HUD hierarchy and icon assets are authored in the scene; this only binds live combat data.
    public sealed class TacticalHudSceneBinder : MonoBehaviour
    {
        private CombatPrototypeBootstrap bootstrap;
        private Transform hudRoot;
        private Text activeText;
        private Text weaponText;
        private Image healthBar;
        private Image shieldBar;
        private Image manaBar;
        private Text[] initiativeRows;
        private Image[] initiativeBars;
        private Text eventText;
        private readonly List<Button> boundButtons = new List<Button>();
        private readonly HashSet<Button> hoveredButtons = new HashSet<Button>();

        private void Awake()
        {
            bootstrap = FindAnyObjectByType<CombatPrototypeBootstrap>();
            hudRoot = transform.Find("战术HUD");
            activeText = FindText("状态/行动文字"); weaponText = FindText("状态/装备文字");
            healthBar = FindImage("状态/生命条/填充"); shieldBar = FindImage("状态/护盾条/填充"); manaBar = FindImage("状态/以太条/填充");
            initiativeRows = Enumerable.Range(0, 4).Select(index => FindText("行动条/行" + index + "/文字")).ToArray();
            initiativeBars = Enumerable.Range(0, 4).Select(index => FindImage("行动条/行" + index + "/填充")).ToArray();
            eventText = FindText("记录/文字");
            EnsureEventSystem();
            BindButtons();
        }

        private void Update()
        {
            RefreshNow();
        }

        private void OnGUI()
        {
            if (bootstrap == null || !bootstrap.IsDeveloperCombatActive || Event.current == null) return;
            Vector2 screenPoint = new Vector2(Event.current.mousePosition.x, Screen.height - Event.current.mousePosition.y);
            foreach (Button button in boundButtons)
            {
                if (button == null || !button.gameObject.activeInHierarchy || !button.interactable) continue;
                bool hovering = RectTransformUtility.RectangleContainsScreenPoint(button.transform as RectTransform, screenPoint);
                SetHover(button, hovering);
                if (hovering && Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    button.onClick.Invoke();
                    Event.current.Use();
                    return;
                }
            }
        }

        public void RefreshNow()
        {
            bool visible = bootstrap != null && bootstrap.IsDeveloperCombatActive && bootstrap.CurrentState != null;
            if (hudRoot.gameObject.activeSelf != visible) hudRoot.gameObject.SetActive(visible);
            if (!visible) return;
            CombatState state = bootstrap.CurrentState; UnitState hero = state.GetUnit("hero"); UnitState active = state.GetUnit(state.ActiveUnitId);
            activeText.text = "行动：" + active.DisplayName + "  /  AP " + active.ActionPoints;
            weaponText.text = "主手：" + hero.MainHand.DisplayName + "  状态：" + StatusText(hero);
            SetBar(healthBar, hero.Health / (float)hero.MaxHealth); SetBar(shieldBar, hero.Shield / (float)Math.Max(1, hero.MaxShield)); SetBar(manaBar, hero.Mana / (float)hero.MaxMana);
            UnitState[] units = state.Units.Values.OrderBy(unit => unit.InitiativeTime).Take(4).ToArray();
            for (int i = 0; i < initiativeRows.Length; i++)
            {
                bool exists = i < units.Length; initiativeRows[i].gameObject.SetActive(exists); initiativeBars[i].transform.parent.gameObject.SetActive(exists);
                if (!exists) continue;
                initiativeRows[i].text = units[i].DisplayName + "  " + units[i].Health + " HP"; SetBar(initiativeBars[i], Mathf.Min(100, units[i].InitiativeTime) / 100f);
            }
            eventText.text = state.EventLog.Count == 0 ? "等待指令" : state.EventLog[0];
        }

        private void BindButtons()
        {
            string[] actions = { "移动", "攻击", "技能1", "技能2", "搜刮", "互动" };
            for (int i = 0; i < actions.Length; i++) { string action = actions[i]; Bind("战术指令/" + action, () => bootstrap.SelectHudAction(action)); }
            for (int i = 0; i < 8; i++) { int slot = i; Bind("快捷栏/槽" + i, () => bootstrap.UseQuickbarSlot(slot)); }
            Bind("构筑与回合/步枪", () => bootstrap.ApplyHudBuild(0)); Bind("构筑与回合/战锤", () => bootstrap.ApplyHudBuild(1)); Bind("构筑与回合/法杖", () => bootstrap.ApplyHudBuild(2));
            Bind("构筑与回合/结束行动", () => bootstrap.EndHeroTurn()); Bind("构筑与回合/战术重开", () => bootstrap.TacticalRestartDeveloperCombat());
        }

        private void Bind(string path, UnityEngine.Events.UnityAction action)
        {
            Button button = hudRoot.Find(path).GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
            boundButtons.Add(button);
            AddHoverFeedback(button);
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            GameObject input = new GameObject("EventSystem");
            input.AddComponent<EventSystem>();
        }

        private void AddHoverFeedback(Button button)
        {
            EventTrigger trigger = button.GetComponent<EventTrigger>();
            if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();
            trigger.triggers.Clear();
            Image image = button.GetComponent<Image>();
            Color normal = image.color;
            Color highlight = new Color(.12f, .20f, .22f, 1f);
            AddTrigger(trigger, EventTriggerType.PointerEnter, _ =>
            {
                SetHover(button, true);
            });
            AddTrigger(trigger, EventTriggerType.PointerExit, _ =>
            {
                SetHover(button, false);
            });
            AddTrigger(trigger, EventTriggerType.PointerDown, _ =>
            {
                button.transform.DOKill(); button.transform.DOScale(.97f, .06f).SetUpdate(true);
            });
            AddTrigger(trigger, EventTriggerType.PointerUp, _ =>
            {
                button.transform.DOKill(); button.transform.DOScale(1.035f, .08f).SetUpdate(true);
            });
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);
        }

        private void SetHover(Button button, bool hovering)
        {
            bool wasHovering = hoveredButtons.Contains(button);
            if (hovering == wasHovering) return;
            Image image = button.GetComponent<Image>();
            Color target = hovering ? new Color(.12f, .20f, .22f, 1f) : new Color(.07f, .075f, .07f, 1f);
            image.DOKill(); button.transform.DOKill();
            DOTween.To(() => image.color, value => image.color = value, target, hovering ? .10f : .12f).SetUpdate(true);
            button.transform.DOScale(hovering ? 1.035f : 1f, hovering ? .10f : .12f).SetUpdate(true);
            if (hovering) hoveredButtons.Add(button); else hoveredButtons.Remove(button);
        }
        private Text FindText(string path) => hudRoot.Find(path).GetComponent<Text>();
        private Image FindImage(string path) => hudRoot.Find(path).GetComponent<Image>();
        private static void SetBar(Image image, float value) { image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Horizontal; image.fillAmount = Mathf.Clamp01(value); }
        private static string StatusText(UnitState unit) => unit.Statuses.Count == 0 ? "无" : string.Join(" ", unit.Statuses.Select(entry => entry.Key + entry.Value));
    }
}
